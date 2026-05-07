using CodeIntel.Core;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace CodeIntel.Graph;

/// <summary>
/// Estrategia de versionado usando bases de datos separadas en Neo4j.
/// Cada versión vive en su propia base de datos para aislamiento completo.
/// Ventaja: Rollback instantáneo (cambio de puntero)
/// Desventaja: Mayor uso de recursos
/// </summary>
public class Neo4jMultiDatabaseGraphStore : IVersionedGraphStore, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jMultiDatabaseGraphStore> _logger;
    private const string MetadataDatabase = "codeintel_metadata";

    public Neo4jMultiDatabaseGraphStore(string uri, string user, string password, ILogger<Neo4jMultiDatabaseGraphStore> logger)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password), o =>
        {
            o.WithMaxConnectionLifetime(TimeSpan.FromMinutes(30));
            o.WithMaxConnectionPoolSize(50);
            o.WithConnectionTimeout(TimeSpan.FromMinutes(2));
        });
        _logger = logger;
    }

    public async Task UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct)
    {
        var repoId = $"{req.Owner}/{req.Repo}@{req.Branch}";
        var versionId = Guid.NewGuid().ToString("N");
        var databaseName = $"codeintel_{SanitizeDatabaseName(repoId)}_{versionId}";
        var timestamp = DateTimeOffset.UtcNow;

        _logger.LogInformation("Creating new database version: {DatabaseName}", databaseName);

        // 1. Crear nueva base de datos para esta versión
        await CreateDatabaseAsync(databaseName);

        // 2. Guardar el grafo en la nueva base de datos
        await using var session = _driver.AsyncSession(o => o.WithDatabase(databaseName));

        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                // Crear nodo de repositorio
                await tx.RunAsync(@"
                    CREATE (r:Repository {
                        id: $repoId,
                        owner: $owner,
                        name: $repo,
                        branch: $branch,
                        versionId: $versionId,
                        timestamp: $timestamp
                    })
                ",
                new
                {
                    repoId,
                    owner = req.Owner,
                    repo = req.Repo,
                    branch = req.Branch,
                    versionId,
                    timestamp = timestamp.ToUnixTimeSeconds()
                });

                // Crear clases
                foreach (var cls in model.Classes)
                {
                    await tx.RunAsync(@"
                        CREATE (c:Class {
                            id: $id,
                            name: $name,
                            namespace: $namespace,
                            filePath: $filePath
                        })
                        WITH c
                        MATCH (r:Repository {id: $repoId})
                        CREATE (r)-[:CONTAINS]->(c)
                    ",
                    new
                    {
                        id = cls.Id,
                        name = cls.Name,
                        @namespace = cls.Namespace,
                        filePath = cls.FilePath,
                        repoId
                    });
                }

                // Crear métodos
                foreach (var method in model.Methods)
                {
                    await tx.RunAsync(@"
                        CREATE (m:Method {
                            id: $id,
                            name: $name,
                            classId: $classId,
                            filePath: $filePath,
                            body: $body
                        })
                        WITH m
                        MATCH (c:Class {id: $classId})
                        CREATE (c)-[:HAS_METHOD]->(m)
                    ",
                    new
                    {
                        id = method.Id,
                        name = method.Name,
                        classId = method.ClassId,
                        filePath = method.FilePath,
                        body = method.Body
                    });
                }

                // Crear relaciones
                foreach (var edge in model.Edges)
                {
                    var relType = edge.Type.ToString().ToUpper();
                    await tx.RunAsync($@"
                        MATCH (from {{id: $fromId}})
                        MATCH (to {{id: $toId}})
                        CREATE (from)-[:{relType}]->(to)
                    ",
                    new
                    {
                        fromId = edge.FromId,
                        toId = edge.ToId
                    });
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create version in database {DatabaseName}", databaseName);
            throw;
        }

        // 3. Registrar versión en base de datos de metadatos
        await RegisterVersionAsync(repoId, versionId, databaseName, timestamp, req.Path ?? "unknown");

        _logger.LogInformation("Successfully created version {VersionId} in database {DatabaseName}", 
            versionId, databaseName);
    }

    private async Task CreateDatabaseAsync(string databaseName)
    {
        await using var session = _driver.AsyncSession(o => o.WithDatabase("system"));

        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync($"CREATE DATABASE {databaseName} IF NOT EXISTS");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database {DatabaseName}", databaseName);
            throw;
        }
    }

    private async Task RegisterVersionAsync(string repoId, string versionId, string databaseName, 
        DateTimeOffset timestamp, string commitHash)
    {
        await using var session = _driver.AsyncSession(o => o.WithDatabase(MetadataDatabase));

        await session.ExecuteWriteAsync(async tx =>
        {
            // Crear/actualizar repositorio
            await tx.RunAsync(@"
                MERGE (r:Repository {id: $repoId})
                ON CREATE SET r.createdAt = datetime()
            ",
            new { repoId });

            // Desmarcar versión actual anterior
            await tx.RunAsync(@"
                MATCH (r:Repository {id: $repoId})-[:CURRENT]->(v:Version)
                DELETE (r)-[:CURRENT]->(v)
                WITH r, v
                MERGE (r)-[:HAS_VERSION]->(v)
            ",
            new { repoId });

            // Crear nueva versión y marcarla como actual
            await tx.RunAsync(@"
                MATCH (r:Repository {id: $repoId})
                CREATE (v:Version {
                    id: $versionId,
                    databaseName: $databaseName,
                    timestamp: $timestamp,
                    commitHash: $commitHash
                })
                CREATE (r)-[:CURRENT]->(v)
                CREATE (r)-[:HAS_VERSION]->(v)
            ",
            new
            {
                repoId,
                versionId,
                databaseName,
                timestamp = timestamp.ToUnixTimeSeconds(),
                commitHash
            });
        });
    }

    /// <summary>
    /// Rollback a una versión anterior (solo cambia el puntero CURRENT)
    /// </summary>
    public async Task RollbackToVersionAsync(string repoId, string versionId, CancellationToken ct)
    {
        await using var session = _driver.AsyncSession(o => o.WithDatabase(MetadataDatabase));

        await session.ExecuteWriteAsync(async tx =>
        {
            // Desmarcar versión actual
            await tx.RunAsync(@"
                MATCH (r:Repository {id: $repoId})-[rel:CURRENT]->(:Version)
                DELETE rel
            ",
            new { repoId });

            // Marcar la versión target como actual
            await tx.RunAsync(@"
                MATCH (r:Repository {id: $repoId})
                MATCH (r)-[:HAS_VERSION]->(v:Version {id: $versionId})
                MERGE (r)-[:CURRENT]->(v)
            ",
            new { repoId, versionId });
        });

        _logger.LogInformation("Rolled back {RepoId} to version {VersionId}", repoId, versionId);
    }

    /// <summary>
    /// Obtener el nombre de la base de datos de la versión actual
    /// </summary>
    public async Task<string> GetCurrentDatabaseAsync(string repoId)
    {
        await using var session = _driver.AsyncSession(o => o.WithDatabase(MetadataDatabase));

        return await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(@"
                MATCH (r:Repository {id: $repoId})-[:CURRENT]->(v:Version)
                RETURN v.databaseName as databaseName
            ",
            new { repoId });

            var record = await result.SingleAsync();
            return record["databaseName"].As<string>();
        });
    }

    /// <summary>
    /// Listar todas las versiones
    /// </summary>
    public async Task<List<VersionInfo>> GetVersionHistoryAsync(string repoId, CancellationToken ct)
    {
        await using var session = _driver.AsyncSession(o => o.WithDatabase(MetadataDatabase));
        var versions = new List<VersionInfo>();

        await session.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(@"
                MATCH (r:Repository {id: $repoId})-[:HAS_VERSION]->(v:Version)
                OPTIONAL MATCH (r)-[:CURRENT]->(v)
                RETURN v.id as versionId,
                       v.commitHash as commitHash,
                       v.timestamp as timestamp,
                       v.databaseName as databaseName,
                       EXISTS((r)-[:CURRENT]->(v)) as isCurrent
                ORDER BY v.timestamp DESC
            ",
            new { repoId });

            await foreach (var record in result)
            {
                versions.Add(new VersionInfo(
                    record["versionId"].As<string>(),
                    record["commitHash"].As<string>(),
                    DateTimeOffset.FromUnixTimeSeconds(record["timestamp"].As<long>()),
                    record["isCurrent"].As<bool>()
                )
                {
                    DatabaseName = record["databaseName"].As<string>()
                });
            }
        });

        return versions;
    }

    /// <summary>
    /// Eliminar versiones antiguas (limpieza)
    /// </summary>
    public async Task DeleteVersionAsync(string repoId, string versionId, CancellationToken ct)
    {
        // 1. Obtener nombre de la base de datos
        await using var metaSession = _driver.AsyncSession(o => o.WithDatabase(MetadataDatabase));

        var databaseName = await metaSession.ExecuteReadAsync(async tx =>
        {
            var result = await tx.RunAsync(@"
                MATCH (v:Version {id: $versionId})
                RETURN v.databaseName as databaseName
            ",
            new { versionId });

            var record = await result.SingleAsync();
            return record["databaseName"].As<string>();
        });

        // 2. Eliminar base de datos
        await using var sysSession = _driver.AsyncSession(o => o.WithDatabase("system"));
        await sysSession.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync($"DROP DATABASE {databaseName} IF EXISTS");
        });

        // 3. Eliminar nodo de versión en metadatos
        await metaSession.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MATCH (v:Version {id: $versionId})
                DETACH DELETE v
            ",
            new { versionId });
        });

        _logger.LogInformation("Deleted version {VersionId} and database {DatabaseName}", versionId, databaseName);
    }

    private static string SanitizeDatabaseName(string name)
    {
        // Neo4j database names: alphanumeric, -, _
        return string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            .ToLowerInvariant();
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver != null)
        {
            await _driver.DisposeAsync();
        }
    }

    // Implementación del método faltante de IVersionedGraphStore
    public Task<GraphModel> GetGraphAtTimestampAsync(string repoId, long timestamp, CancellationToken ct)
    {
        throw new NotImplementedException(
            "Multi-database strategy doesn't support temporal queries. Use GetCurrentDatabaseAsync() and query that database directly.");
    }
}
