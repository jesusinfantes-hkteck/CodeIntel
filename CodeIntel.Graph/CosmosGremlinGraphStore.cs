using CodeIntel.Core;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Collections.Generic;       
using System.Threading;
using System.Threading.Tasks;

namespace CodeIntel.Graph;

public sealed class CosmosGremlinGraphStore : IGraphStore
{
    private readonly GremlinClient _client;

    public CosmosGremlinGraphStore(string host, int port, string database, string graph, string key)
    {
        // username según Cosmos Gremlin
        var username = $"/dbs/{database}/colls/{graph}";

        var server = new GremlinServer(host, port, enableSsl: true, username: username, password: key);
        // El SDK puede no exponer la constante GraphSON2MimeType; usar el MIME type correspondiente directamente.
        _client = new GremlinClient(server, new GraphSON2Reader(), new GraphSON2Writer(), "application/vnd.gremlin-v2.0+json");
    }

    public async Task UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct)
    {
        var repoId = $"{req.Owner}/{req.Repo}@{req.Branch}";

        // Create Class vertices
        foreach (var c in model.Classes)
        {
            ct.ThrowIfCancellationRequested();
            var q = "g.V(id).fold().coalesce(unfold(), addV('Class').property('id', id)).property('name', name).property('ns', ns).property('file', file).property('repo', repo)";
            var bindings = new Dictionary<string, object>
            {
                ["id"] = c.Id,
                ["name"] = c.Name,
                ["ns"] = c.Namespace,
                ["file"] = c.FilePath,
                ["repo"] = repoId
            };
            await _client.SubmitAsync<dynamic>(q, bindings);
        }

        // Create Method vertices
        foreach (var m in model.Methods)
        {
            ct.ThrowIfCancellationRequested();
            var q = "g.V(id).fold().coalesce(unfold(), addV('Method').property('id', id)).property('name', name).property('file', file).property('classId', classId).property('repo', repo)";
            var bindings = new Dictionary<string, object>
            {
                ["id"] = m.Id,
                ["name"] = m.Name,
                ["file"] = m.FilePath,
                ["classId"] = m.ClassId,
                ["repo"] = repoId
            };
            await _client.SubmitAsync<dynamic>(q, bindings);

            // Method -> Class relationship
            var q2 = "g.V(mid).as('m').V(cid).coalesce(__.inE('declared_in').where(outV().hasId(mid)), __.addE('declared_in').from('m'))";
            var b2 = new Dictionary<string, object> { ["mid"] = m.Id, ["cid"] = m.ClassId };
            await _client.SubmitAsync<dynamic>(q2, b2);
        }

        // Create ASPX Page vertices
        foreach (var page in model.AspxPages)
        {
            ct.ThrowIfCancellationRequested();
            var q = "g.V(id).fold().coalesce(unfold(), addV('AspxPage').property('id', id)).property('name', name).property('file', file).property('codeBehind', cb).property('inherits', inh).property('repo', repo)";
            var bindings = new Dictionary<string, object>
            {
                ["id"] = page.Id,
                ["name"] = page.Name,
                ["file"] = page.FilePath,
                ["cb"] = page.CodeBehindClass ?? "",
                ["inh"] = page.Inherits ?? "",
                ["repo"] = repoId
            };
            await _client.SubmitAsync<dynamic>(q, bindings);
        }

        // Create ASPX Control vertices
        foreach (var control in model.AspxControls)
        {
            ct.ThrowIfCancellationRequested();
            var q = "g.V(id).fold().coalesce(unfold(), addV('AspxControl').property('id', id)).property('name', name).property('type', type).property('pageId', pageId).property('file', file).property('repo', repo)";
            var bindings = new Dictionary<string, object>
            {
                ["id"] = control.Id,
                ["name"] = control.Name,
                ["type"] = control.Type,
                ["pageId"] = control.PageId,
                ["file"] = control.FilePath,
                ["repo"] = repoId
            };
            await _client.SubmitAsync<dynamic>(q, bindings);

            // Control -> Page relationship
            var q2 = "g.V(cid).as('c').V(pid).coalesce(__.inE('belongs_to').where(outV().hasId(cid)), __.addE('belongs_to').from('c'))";
            var b2 = new Dictionary<string, object> { ["cid"] = control.Id, ["pid"] = control.PageId };
            await _client.SubmitAsync<dynamic>(q2, b2);
        }

        // Create ASPX Event vertices
        foreach (var evt in model.AspxEvents)
        {
            ct.ThrowIfCancellationRequested();
            var q = "g.V(id).fold().coalesce(unfold(), addV('AspxEvent').property('id', id)).property('eventName', name).property('controlId', cid).property('handler', handler).property('repo', repo)";
            var bindings = new Dictionary<string, object>
            {
                ["id"] = evt.Id,
                ["name"] = evt.EventName,
                ["cid"] = evt.ControlId,
                ["handler"] = evt.HandlerMethod,
                ["repo"] = repoId
            };
            await _client.SubmitAsync<dynamic>(q, bindings);

            // Event -> Control relationship
            var q2 = "g.V(eid).as('e').V(cid).coalesce(__.inE('triggered_by').where(outV().hasId(eid)), __.addE('triggered_by').from('e'))";
            var b2 = new Dictionary<string, object> { ["eid"] = evt.Id, ["cid"] = evt.ControlId };
            await _client.SubmitAsync<dynamic>(q2, b2);
        }

        // Create edges/relationships
        foreach (var e in model.Edges)
        {
            ct.ThrowIfCancellationRequested();
            var label = e.Type.ToString().ToLowerInvariant();
            var q = "g.V(from).as('a').V(to).coalesce(__.inE(lbl).where(outV().hasId(from)), __.addE(lbl).from('a'))";
            var bindings = new Dictionary<string, object>
            {
                ["from"] = e.FromId,
                ["to"] = e.ToId,
                ["lbl"] = label
            };
            await _client.SubmitAsync<dynamic>(q, bindings);
        }
    }
}
