using CodeIntel.Core;
using Microsoft.Extensions.Logging;

namespace CodeIntel.Functions.Mocks;

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

    public async Task UpsertAsync(IEnumerable<VectorDocument> docs, CancellationToken ct)
    {
        _docs.AddRange(docs);
        _logger.LogInformation("[MOCK] Indexed {DocumentCount} documents (total: {TotalCount})", 
            docs.Count(), _docs.Count);
        await Task.Delay(100, ct);
    }
}