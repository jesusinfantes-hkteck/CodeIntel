namespace CodeIntel.Core;

public record RepoRequest(string Owner, string Repo, string Branch = "main", string Path = "");

public record CodeClass(string Id, string Name, string Namespace, string FilePath);
public record CodeMethod(string Id, string Name, string ClassId, string FilePath, string? Body);

// ASPX-specific models
public record AspxPage(string Id, string Name, string FilePath, string? CodeBehindClass, string? Inherits);
public record AspxControl(string Id, string Name, string Type, string PageId, string FilePath, Dictionary<string, string>? Events = null);
public record AspxEvent(string Id, string EventName, string ControlId, string HandlerMethod);

public enum EdgeType
{
    DependsOn,
    Calls,
    Inherits,
    Implements,
    CodeBehind,      // ASPX page -> code-behind class
    HasControl,      // ASPX page -> Control
    HandlesEvent     // Control -> Method handler
}

public record CodeEdge(string FromId, string ToId, EdgeType Type);

public record GraphModel(
    IReadOnlyList<CodeClass> Classes,
    IReadOnlyList<CodeMethod> Methods,
    IReadOnlyList<CodeEdge> Edges,
    IReadOnlyList<AspxPage> AspxPages,
    IReadOnlyList<AspxControl> AspxControls,
    IReadOnlyList<AspxEvent> AspxEvents);

public record VectorDocument(
    string Id,
    string Content,
    float[] Embedding,
    string Type,
    string ClassName,
    string FilePath);

public record VersionInfo(
    string VersionId, 
    string CommitHash, 
    DateTimeOffset Timestamp, 
    bool IsCurrent)
{
    public string? DatabaseName { get; init; }
}
