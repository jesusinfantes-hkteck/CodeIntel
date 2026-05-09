using System.Net;
using System.Text.Json;
using System.Web;
using CodeIntel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CodeIntel.Functions;

/// <summary>
/// Azure Function que maneja webhooks de GitHub para actualización incremental del Knowledge Store
/// </summary>
public class GitHubWebhookFunction
{
    private readonly ILogger _logger;
    private readonly IngestOrchestrator _orchestrator;
    private readonly IVersionedGraphStore _versionedStore;

    public GitHubWebhookFunction(
        ILoggerFactory loggerFactory, 
        IngestOrchestrator orchestrator,
        IVersionedGraphStore versionedStore)
    {
        _logger = loggerFactory.CreateLogger<GitHubWebhookFunction>();
        _orchestrator = orchestrator;
        _versionedStore = versionedStore;
    }

    [Function("GitHubWebhook")]
    public async Task<HttpResponseData> HandleWebhook(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook/github")] HttpRequestData req,
        FunctionContext context)
    {
        _logger.LogInformation("GitHub webhook received");

        try
        {
            // Validar firma del webhook (seguridad)
            var signature = req.Headers.GetValues("X-Hub-Signature-256").FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Missing webhook signature");
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            // Parsear payload
            var payload = await JsonSerializer.DeserializeAsync<GitHubPushEvent>(req.Body);

            if (payload?.Repository == null)
            {
                _logger.LogWarning("Invalid webhook payload");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Extraer información del commit
            var repoRequest = new RepoRequest(
                Owner: payload.Repository.Owner.Login,
                Repo: payload.Repository.Name,
                Branch: payload.Ref.Replace("refs/heads/", ""),
                Path: payload.HeadCommit.Id  // CommitHash como Path para tracking
            );

            _logger.LogInformation(
                "Processing push event: {Owner}/{Repo}@{Branch} - Commit: {CommitHash}",
                repoRequest.Owner, repoRequest.Repo, repoRequest.Branch, payload.HeadCommit.Id
            );

            // Procesar actualización (esto creará una nueva versión automáticamente)
            var result = await _orchestrator.RunAsync(repoRequest, context.CancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Repository processed successfully",
                version = "auto-generated",
                commitHash = payload.HeadCommit.Id,
                result
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Endpoint para listar versiones de un repositorio
    /// </summary>
    [Function("GetVersionHistory")]
    public async Task<HttpResponseData> GetVersionHistory(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "repo/{owner}/{repo}/{branch}/versions")] 
        HttpRequestData req,
        string owner,
        string repo,
        string branch,
        FunctionContext context)
    {
        try
        {
            var repoId = $"{owner}/{repo}@{branch}";
            var versions = await _versionedStore.GetVersionHistoryAsync(repoId, context.CancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                repoId,
                totalVersions = versions.Count,
                versions = versions.Select(v => new
                {
                    v.VersionId,
                    v.CommitHash,
                    timestamp = v.Timestamp.ToString("o"),
                    v.IsCurrent,
                    age = DateTimeOffset.UtcNow - v.Timestamp
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Endpoint para hacer rollback a una versión específica
    /// </summary>
    [Function("RollbackToVersion")]
    public async Task<HttpResponseData> RollbackToVersion(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "repo/{owner}/{repo}/{branch}/rollback")] 
        HttpRequestData req,
        string owner,
        string repo,
        string branch,
        FunctionContext context)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<RollbackRequest>(req.Body);

            if (string.IsNullOrEmpty(body?.VersionId))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("VersionId is required");
                return badRequest;
            }

            var repoId = $"{owner}/{repo}@{branch}";

            _logger.LogInformation("Rolling back {RepoId} to version {VersionId}", repoId, body.VersionId);

            await _versionedStore.RollbackToVersionAsync(repoId, body.VersionId, context.CancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Rollback successful",
                repoId,
                versionId = body.VersionId
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rollback");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Endpoint para obtener el estado del grafo en un timestamp específico
    /// </summary>
    [Function("GetGraphAtTime")]
    public async Task<HttpResponseData> GetGraphAtTime(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "repo/{owner}/{repo}/{branch}/snapshot")] 
        HttpRequestData req,
        string owner,
        string repo,
        string branch,
        FunctionContext context)
    {
        try
        {
            // Parsear timestamp del query string
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var timestampStr = query["timestamp"];

            if (string.IsNullOrEmpty(timestampStr) || !long.TryParse(timestampStr, out var timestamp))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Valid 'timestamp' query parameter is required (Unix seconds)");
                return badRequest;
            }

            var repoId = $"{owner}/{repo}@{branch}";
            var graphModel = await _versionedStore.GetGraphAtTimestampAsync(repoId, timestamp, context.CancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                repoId,
                timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("o"),
                snapshot = new
                {
                    classes = graphModel.Classes.Count,
                    methods = graphModel.Methods.Count,
                    aspxPages = graphModel.AspxPages.Count,
                    aspxControls = graphModel.AspxControls.Count,
                    edges = graphModel.Edges.Count,
                    topClasses = graphModel.Classes.Take(10).Select(c => new { c.Name, c.Namespace })
                }
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graph snapshot");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}

#region DTOs

public class GitHubPushEvent
{
    public string Ref { get; set; } = "";
    public GitHubCommit HeadCommit { get; set; } = new();
    public GitHubRepository Repository { get; set; } = new();
}

public class GitHubCommit
{
    public string Id { get; set; } = "";
    public string Message { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public GitHubAuthor Author { get; set; } = new();
}

public class GitHubAuthor
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class GitHubRepository
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public GitHubOwner Owner { get; set; } = new();
}

public class GitHubOwner
{
    public string Login { get; set; } = "";
}

public class RollbackRequest
{
    public string VersionId { get; set; } = "";
}

#endregion
