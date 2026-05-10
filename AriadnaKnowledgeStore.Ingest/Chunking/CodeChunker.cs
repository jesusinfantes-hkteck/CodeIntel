using AriadnaKnowledgeStore.Core;
using System.Collections.Generic;
using System.Linq;

namespace AriadnaKnowledgeStore.Ingest.Chunking;

public static class CodeChunker
{
    /// <summary>
    /// Converts code elements (classes, methods, ASPX pages/controls) into documents for embedding.
    /// </summary>
    public static IEnumerable<(string id, string content, string type, string className, string filePath)> ToVectorDocs(GraphModel model)
    {
        var classById = model.Classes
            .DistinctBy(c => c.Id)
            .ToDictionary(c => c.Id, c => c);

        // Generate chunks for methods
        foreach (var m in model.Methods)
        {
            if (!classById.TryGetValue(m.ClassId, out var cls)) continue;

            var content = @$"Namespace: {cls.Namespace}
Class: {cls.Name}
Method: {m.Name}
File: {m.FilePath}

{m.Body}";
            yield return (m.Id, content, "method", cls.Name, m.FilePath);
        }

        // Generate chunks for classes
        foreach (var c in model.Classes)
        {
            var content = @$"Namespace: {c.Namespace}
Class: {c.Name}
File: {c.FilePath}";
            yield return (c.Id, content, "class", c.Name, c.FilePath);
        }

        // Generate chunks for ASPX pages
        foreach (var page in model.AspxPages)
        {
            var content = @$"ASPX Page: {page.Name}
CodeBehind Class: {page.CodeBehindClass ?? "N/A"}
Inherits: {page.Inherits ?? "N/A"}
File: {page.FilePath}

This is a web page with server-side controls and event handlers.";
            yield return (page.Id, content, "aspx_page", page.CodeBehindClass ?? page.Name, page.FilePath);
        }

        // Generate chunks for ASPX controls
        foreach (var control in model.AspxControls)
        {
            var eventsText = control.Events != null && control.Events.Any()
                ? string.Join(", ", control.Events.Select(e => $"{e.Key}={e.Value}"))
                : "No events";

            var content = @$"ASPX Control: {control.Name}
Type: {control.Type}
Page: {control.PageId}
Events: {eventsText}
File: {control.FilePath}";
            yield return (control.Id, content, "aspx_control", control.Name, control.FilePath);
        }

        // Generate chunks for ASPX events
        foreach (var evt in model.AspxEvents)
        {
            var content = @$"ASPX Event: {evt.EventName}
Control: {evt.ControlId}
Handler Method: {evt.HandlerMethod}

This event connects UI interaction to server-side code.";
            yield return (evt.Id, content, "aspx_event", evt.HandlerMethod, evt.ControlId);
        }
    }
}
