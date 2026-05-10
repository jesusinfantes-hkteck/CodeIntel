using AriadnaKnowledgeStore.Core;
using Microsoft.Extensions.Logging;

namespace AriadnaKnowledgeStore.Functions.Mocks;

public class MockEmbeddingService : IEmbeddingService
{
    private readonly ILogger<MockEmbeddingService> _logger;

    public MockEmbeddingService(ILogger<MockEmbeddingService> logger)
    {
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        _logger.LogInformation("[MOCK] Creating embedding for: {TextPreview}...", 
            text.Substring(0, Math.Min(50, text.Length)));
        // Return dummy 1536-dimensional vector
        var vec = new float[1536];
        for (int i = 0; i < vec.Length; i++)
            vec[i] = (float)new Random(text.GetHashCode() + i).NextDouble();
        await Task.Delay(50, ct);
        return vec;
    }
}