namespace CodeIntel.Core;

public interface IGitHubSource
{
    Task<string> DownloadRepositoryAsync(RepoRequest req, CancellationToken ct);
}

public interface ICodeAnalyzer
{
    Task<GraphModel> AnalyzeAsync(string localPath, CancellationToken ct);
}

public interface IGraphStore
{
    Task UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct);
}

/// <summary>
/// Extended graph store with versioning and rollback capabilities
/// </summary>
public interface IVersionedGraphStore : IGraphStore
{
    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    Task RollbackToVersionAsync(string repoId, string versionId, CancellationToken ct);

    /// <summary>
    /// Get the graph state at a specific point in time
    /// </summary>
    Task<GraphModel> GetGraphAtTimestampAsync(string repoId, long timestamp, CancellationToken ct);

    /// <summary>
    /// List all versions for a repository
    /// </summary>
    Task<List<VersionInfo>> GetVersionHistoryAsync(string repoId, CancellationToken ct);
}

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}

public interface IVectorIndex
{
    Task EnsureIndexAsync(CancellationToken ct);
    Task UpsertAsync(IEnumerable<VectorDocument> docs, CancellationToken ct);
}
