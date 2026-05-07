using CodeIntel.Core;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace CodeIntel.Graph;

public class Neo4jGraphStore : IGraphStore, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jGraphStore> _logger;

    public Neo4jGraphStore(string uri, string user, string password, ILogger<Neo4jGraphStore> logger)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password), o =>
        {
            o.WithMaxConnectionLifetime(TimeSpan.FromMinutes(30));
            o.WithMaxConnectionPoolSize(50);
            o.WithConnectionTimeout(TimeSpan.FromMinutes(2));
        });
        _logger = logger;
    }

    public async Task UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct)
    {
        var repoId = $"{req.Owner}/{req.Repo}@{req.Branch}";

        _logger.LogInformation("Storing graph for {RepoId}: {ClassCount} classes, {MethodCount} methods, {EdgeCount} edges",
            repoId, model.Classes.Count, model.Methods.Count, model.Edges.Count);

        await using var session = _driver.AsyncSession();

        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                // 1. Create or merge Repository node
                await tx.RunAsync(@"
                    MERGE (r:Repository {id: $repoId})
                    SET r.owner = $owner,
                        r.name = $repo,
                        r.branch = $branch,
                        r.lastUpdated = datetime()
                ",
                new
                {
                    repoId,
                    owner = req.Owner,
                    repo = req.Repo,
                    branch = req.Branch
                });

                // 2. Create Class nodes
                foreach (var cls in model.Classes)
                {
                    await tx.RunAsync(@"
                        MERGE (c:Class {id: $id})
                        SET c.name = $name,
                            c.namespace = $namespace,
                            c.filePath = $filePath,
                            c.repoId = $repoId,
                            c.lastUpdated = datetime()
                        WITH c
                        MATCH (r:Repository {id: $repoId})
                        MERGE (r)-[:CONTAINS]->(c)
                    ",
                    new
                    {
                        id = cls.Id,
                        name = cls.Name,
                        @namespace = cls.Namespace,
                        filePath = cls.FilePath,
                        repoId
                    });
                }

                // 3. Create Method nodes
                foreach (var method in model.Methods)
                {
                    await tx.RunAsync(@"
                        MERGE (m:Method {id: $id})
                        SET m.name = $name,
                            m.classId = $classId,
                            m.filePath = $filePath,
                            m.body = $body,
                            m.repoId = $repoId,
                            m.lastUpdated = datetime()
                        WITH m
                        MATCH (c:Class {id: $classId})
                        MERGE (c)-[:HAS_METHOD]->(m)
                    ",
                    new
                    {
                        id = method.Id,
                        name = method.Name,
                        classId = method.ClassId,
                        filePath = method.FilePath,
                        body = method.Body,
                        repoId
                    });
                }

                // 4. Create relationships (edges)
                foreach (var edge in model.Edges)
                {
                    var relType = edge.Type.ToString().ToUpper();

                    // Cypher doesn't allow parameterized relationship types, so we use APOC or build dynamically
                    await tx.RunAsync($@"
                        MATCH (from {{id: $fromId}})
                        MATCH (to {{id: $toId}})
                        MERGE (from)-[r:{relType}]->(to)
                        SET r.repoId = $repoId
                    ",
                    new
                    {
                        fromId = edge.FromId,
                        toId = edge.ToId,
                        repoId
                    });
                }

                _logger.LogInformation("Successfully stored graph for {RepoId}", repoId);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store graph for {RepoId}", repoId);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver != null)
        {
            await _driver.DisposeAsync();
        }
    }
}