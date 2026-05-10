using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AriadnaKnowledgeStore.Core;
using HtmlAgilityPack;

namespace AriadnaKnowledgeStore.Ingest.Aspx;

/// <summary>
/// Analyzes ASPX/ASCX files from legacy .NET Framework applications
/// </summary>
public sealed class AspxAnalyzer
{
    private static readonly Regex DirectiveRegex = new Regex(
        @"<%@\s+(?<directive>\w+)\s+(?<attrs>[^%]+)%>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AttributeRegex = new Regex(
        @"(?<name>\w+)\s*=\s*""(?<value>[^""]*)""",
        RegexOptions.Compiled);

    public async Task<AspxAnalysisResult> AnalyzeAsync(string filePath, string rootPath, CancellationToken ct)
    {
        var content = await File.ReadAllTextAsync(filePath, ct);
        var relativePath = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
        var fileName = Path.GetFileName(filePath);
        var isUserControl = filePath.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase);

        // Extract directive (@Page or @Control)
        var directive = ExtractDirective(content, isUserControl ? "Control" : "Page");
        var inherits = directive.GetValueOrDefault("Inherits");
        var codeBehind = directive.GetValueOrDefault("CodeBehind");

        // Create page/control node
        var pageId = $"aspx:{relativePath}";
        var page = new AspxPage(
            Id: pageId,
            Name: fileName,
            FilePath: relativePath,
            CodeBehindClass: inherits,
            Inherits: inherits
        );

        // Parse HTML to find ASP.NET controls
        var controls = new List<AspxControl>();
        var events = new List<AspxEvent>();
        var edges = new List<CodeEdge>();

        ParseControls(content, pageId, relativePath, controls, events, edges);

        // Create code-behind relationship if class is specified
        if (!string.IsNullOrEmpty(inherits))
        {
            var classId = $"class:{inherits}";
            edges.Add(new CodeEdge(pageId, classId, EdgeType.CodeBehind));
        }

        return new AspxAnalysisResult(page, controls, events, edges);
    }

    private Dictionary<string, string> ExtractDirective(string content, string directiveName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var match = DirectiveRegex.Match(content);
        while (match.Success)
        {
            var directive = match.Groups["directive"].Value;
            if (directive.Equals(directiveName, StringComparison.OrdinalIgnoreCase))
            {
                var attrs = match.Groups["attrs"].Value;
                var attrMatches = AttributeRegex.Matches(attrs);

                foreach (Match attrMatch in attrMatches)
                {
                    var name = attrMatch.Groups["name"].Value;
                    var value = attrMatch.Groups["value"].Value;
                    result[name] = value;
                }
                break;
            }
            match = match.NextMatch();
        }

        return result;
    }

    private void ParseControls(
        string content,
        string pageId,
        string filePath,
        List<AspxControl> controls,
        List<AspxEvent> events,
        List<CodeEdge> edges)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            // Find all server controls (runat="server")
            var serverControls = doc.DocumentNode.Descendants()
                .Where(n => n.Attributes["runat"]?.Value.Equals("server", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var node in serverControls)
            {
                var id = node.Attributes["id"]?.Value;
                if (string.IsNullOrEmpty(id))
                    continue;

                var controlType = node.Name;
                var controlId = $"control:{pageId}#{id}";

                // Extract event handlers
                var eventHandlers = new Dictionary<string, string>();
                var commonEvents = new[] { "OnClick", "OnLoad", "OnInit", "OnTextChanged", "OnSelectedIndexChanged", 
                                          "OnCommand", "OnRowDataBound", "OnItemDataBound", "OnCheckedChanged" };

                foreach (var eventName in commonEvents)
                {
                    var handler = node.Attributes[eventName]?.Value;
                    if (!string.IsNullOrEmpty(handler))
                    {
                        eventHandlers[eventName] = handler;

                        // Create event node and relationships
                        var eventId = $"event:{controlId}#{eventName}";
                        events.Add(new AspxEvent(eventId, eventName, controlId, handler));

                        // Control -> Event
                        edges.Add(new CodeEdge(controlId, eventId, EdgeType.HasControl));

                        // Event -> Method handler (will be resolved if method exists)
                        var methodId = $"method:{handler}"; // simplified, should match actual method
                        edges.Add(new CodeEdge(eventId, methodId, EdgeType.HandlesEvent));
                    }
                }

                var control = new AspxControl(
                    Id: controlId,
                    Name: id,
                    Type: controlType,
                    PageId: pageId,
                    FilePath: filePath,
                    Events: eventHandlers.Count > 0 ? eventHandlers : null
                );

                controls.Add(control);

                // Page -> Control relationship
                edges.Add(new CodeEdge(pageId, controlId, EdgeType.HasControl));
            }
        }
        catch (Exception ex)
        {
            // If HTML parsing fails, log but don't crash
            Console.WriteLine($"Warning: Failed to parse HTML in {filePath}: {ex.Message}");
        }
    }
}

public record AspxAnalysisResult(
    AspxPage Page,
    List<AspxControl> Controls,
    List<AspxEvent> Events,
    List<CodeEdge> Edges);
