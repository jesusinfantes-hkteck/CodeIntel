using AriadnaKnowledgeStore.Core;
using Microsoft.Extensions.Logging;

namespace AriadnaKnowledgeStore.Functions.Mocks;

public class MockVectorIndex : IVectorIndex
{
    private readonly ILogger<MockVectorIndex> _logger;
    private List<VectorDocument> _docs = new();

    public MockVectorIndex(ILogger<MockVectorIndex> logger)
    {
        _logger = logger;
    }

    public async Task EnsureIndexAsync(CancellationToken ct)
    {
        _logger.LogInformation("[MOCK] Ensuring vector index exists");
        await Task.Delay(50, ct);
    }

    public async Task UpsertAsync(IEnumerable<VectorDocument> docs, VersionContext version, CancellationToken ct)
    {
        _docs.AddRange(docs);
        _logger.LogInformation("[MOCK] Indexed {DocumentCount} documents for version {VersionId} (total: {TotalCount})", 
            docs.Count(), version.VersionId, _docs.Count);
        await Task.Delay(100, ct);
    }
}