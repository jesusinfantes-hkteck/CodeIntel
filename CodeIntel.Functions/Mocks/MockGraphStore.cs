using CodeIntel.Core;
using Microsoft.Extensions.Logging;

namespace CodeIntel.Functions.Mocks;

public class MockGraphStore : IGraphStore
{
    private readonly ILogger<MockGraphStore> _logger;

    public MockGraphStore(ILogger<MockGraphStore> logger)
    {
        _logger = logger;
    }

    public async Task UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct)
    {
        _logger.LogInformation("[MOCK] Storing graph: {ClassCount} classes, {MethodCount} methods", 
            model.Classes.Count, model.Methods.Count);
        await Task.Delay(100, ct);
    }
}