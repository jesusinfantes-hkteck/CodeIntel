using AriadnaKnowledgeStore.Core;
using Microsoft.Extensions.Logging;

namespace AriadnaKnowledgeStore.Functions.Mocks;

public class MockGraphStore : IGraphStore
{
    private readonly ILogger<MockGraphStore> _logger;

    public MockGraphStore(ILogger<MockGraphStore> logger)
    {
        _logger = logger;
    }

    public async Task<VersionContext> UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct)
    {
        var versionContext = new VersionContext(
            VersionId: Guid.NewGuid().ToString(),
            RepoId: $"{req.Owner}/{req.Repo}@{req.Branch}",
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CommitHash: req.Path
        );

        _logger.LogInformation("[MOCK] Storing graph: {ClassCount} classes, {MethodCount} methods, Version: {VersionId}", 
            model.Classes.Count, model.Methods.Count, versionContext.VersionId);
        await Task.Delay(100, ct);

        return versionContext;
    }
}