// ============================================================================
// EJEMPLO PRÁCTICO: Sistema de Versionado y Rollback
// ============================================================================
// Este archivo demuestra cómo usar el sistema de versionado en escenarios reales

using AriadnaKnowledgeStore.Core;
using AriadnaKnowledgeStore.Graph;
using Microsoft.Extensions.Logging;

namespace AriadnaKnowledgeStore.Examples;

/// <summary>
/// Ejemplos de uso del sistema de versionado
/// </summary>
public class VersioningExamples
{
    private readonly IVersionedGraphStore _graphStore;
    private readonly ILogger _logger;

    public VersioningExamples(IVersionedGraphStore graphStore, ILogger logger)
    {
        _graphStore = graphStore;
        _logger = logger;
    }

    // ========================================================================
    // EJEMPLO 1: Ingesta Simple con Versionado Automático
    // ========================================================================

    /// <summary>
    /// Cada vez que ingieres código, se crea automáticamente una nueva versión
    /// </summary>
    public async Task Example1_SimpleIngest()
    {
        _logger.LogInformation("=== Ejemplo 1: Ingesta Simple ===");

        // Simular 3 ingestas en diferentes momentos
        var repoRequest = new RepoRequest(
            Owner: "acme",
            Repo: "ecommerce",
            Branch: "main",
            Path: "commit-abc123"  // Commit SHA de GitHub
        );

        // Primera ingesta (Versión 1)
        _logger.LogInformation("Ingesta #1 - Código inicial");
        var model1 = new GraphModel(
            Classes: new List<CodeClass>
            {
                new("class:Product", "Product", "Models", "Models/Product.cs"),
                new("class:Order", "Order", "Models", "Models/Order.cs")
            },
            Methods: new List<CodeMethod>(),
            Edges: new List<CodeEdge>(),
            AspxPages: new List<AspxPage>(),
            AspxControls: new List<AspxControl>(),
            AspxEvents: new List<AspxEvent>(),
            RazorViews: new List<RazorView>(),
            ViewComponents: new List<ViewComponent>(),
            ControllerActions: new List<ControllerAction>(),
            BlazorComponents: new List<BlazorComponent>(),
            BlazorParameters: new List<BlazorParameter>(),
            BlazorEventCallbacks: new List<BlazorEventCallback>(),
            BlazorChildComponents: new List<BlazorChildComponent>()
        );

        await _graphStore.UpsertAsync(repoRequest, model1, CancellationToken.None);
        _logger.LogInformation("✅ Versión 1 creada");

        // Segunda ingesta (Versión 2) - Agregar Payment.cs
        await Task.Delay(2000); // Simular paso del tiempo
        _logger.LogInformation("Ingesta #2 - Agregar Payment.cs");

        repoRequest = repoRequest with { Path = "commit-def456" };
        var model2 = new GraphModel(
            Classes: new List<CodeClass>
            {
                new("class:Product", "Product", "Models", "Models/Product.cs"),
                new("class:Order", "Order", "Models", "Models/Order.cs"),
                new("class:Payment", "Payment", "Models", "Models/Payment.cs") // NUEVO
            },
            Methods: new List<CodeMethod>(),
            Edges: new List<CodeEdge>(),
            AspxPages: new List<AspxPage>(),
            AspxControls: new List<AspxControl>(),
            AspxEvents: new List<AspxEvent>(),
            RazorViews: new List<RazorView>(),
            ViewComponents: new List<ViewComponent>(),
            ControllerActions: new List<ControllerAction>(),
            BlazorComponents: new List<BlazorComponent>(),
            BlazorParameters: new List<BlazorParameter>(),
            BlazorEventCallbacks: new List<BlazorEventCallback>(),
            BlazorChildComponents: new List<BlazorChildComponent>()
        );

        await _graphStore.UpsertAsync(repoRequest, model2, CancellationToken.None);
        _logger.LogInformation("✅ Versión 2 creada");

        // Tercera ingesta (Versión 3) - Eliminar Order.cs
        await Task.Delay(2000);
        _logger.LogInformation("Ingesta #3 - Eliminar Order.cs");

        repoRequest = repoRequest with { Path = "commit-ghi789" };
        var model3 = new GraphModel(
            Classes: new List<CodeClass>
            {
                new("class:Product", "Product", "Models", "Models/Product.cs"),
                new("class:Payment", "Payment", "Models", "Models/Payment.cs")
                // Order.cs ELIMINADO
            },
            Methods: new List<CodeMethod>(),
            Edges: new List<CodeEdge>(),
            AspxPages: new List<AspxPage>(),
            AspxControls: new List<AspxControl>(),
            AspxEvents: new List<AspxEvent>(),
            RazorViews: new List<RazorView>(),
            ViewComponents: new List<ViewComponent>(),
            ControllerActions: new List<ControllerAction>(),
            BlazorComponents: new List<BlazorComponent>(),
            BlazorParameters: new List<BlazorParameter>(),
            BlazorEventCallbacks: new List<BlazorEventCallback>(),
            BlazorChildComponents: new List<BlazorChildComponent>()
        );

        await _graphStore.UpsertAsync(repoRequest, model3, CancellationToken.None);
        _logger.LogInformation("✅ Versión 3 creada");

        _logger.LogInformation("Resultado: 3 versiones almacenadas en Neo4j");
    }

    // ========================================================================
    // EJEMPLO 2: Listar Historial de Versiones
    // ========================================================================

    /// <summary>
    /// Ver todas las versiones de un repositorio
    /// </summary>
    public async Task Example2_ListVersionHistory()
    {
        _logger.LogInformation("=== Ejemplo 2: Listar Historial ===");

        var repoId = "acme/ecommerce@main";
        var versions = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);

        _logger.LogInformation($"Encontradas {versions.Count} versiones:");

        foreach (var version in versions)
        {
            var currentFlag = version.IsCurrent ? "← ACTUAL" : "";
            _logger.LogInformation(
                $"  • Version {version.VersionId[..8]}... " +
                $"| Commit: {version.CommitHash} " +
                $"| Fecha: {version.Timestamp:yyyy-MM-dd HH:mm:ss} " +
                $"{currentFlag}"
            );
        }

        // Resultado esperado:
        // Encontradas 3 versiones:
        //   • Version 12345678... | Commit: commit-abc123 | Fecha: 2024-01-01 10:00:00
        //   • Version 23456789... | Commit: commit-def456 | Fecha: 2024-01-01 10:00:02
        //   • Version 34567890... | Commit: commit-ghi789 | Fecha: 2024-01-01 10:00:04 ← ACTUAL
    }

    // ========================================================================
    // EJEMPLO 3: Time Travel (Consultar Versión Pasada)
    // ========================================================================

    /// <summary>
    /// Ver cómo era el código en un momento específico del pasado
    /// </summary>
    public async Task Example3_TimeTravel()
    {
        _logger.LogInformation("=== Ejemplo 3: Time Travel ===");

        var repoId = "acme/ecommerce@main";
        var versions = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);

        // Obtener la versión 2 (cuando existían Product, Order y Payment)
        var version2 = versions[1]; // Segunda versión
        var timestamp = version2.Timestamp.ToUnixTimeSeconds();

        _logger.LogInformation($"Viajando al pasado: {version2.Timestamp:yyyy-MM-dd HH:mm:ss}");

        var historicalGraph = await _graphStore.GetGraphAtTimestampAsync(
            repoId, 
            timestamp, 
            CancellationToken.None
        );

        _logger.LogInformation("Clases que existían en ese momento:");
        foreach (var cls in historicalGraph.Classes)
        {
            _logger.LogInformation($"  • {cls.Name} ({cls.FilePath})");
        }

        // Resultado esperado:
        // Clases que existían en ese momento:
        //   • Product (Models/Product.cs)
        //   • Order (Models/Order.cs)
        //   • Payment (Models/Payment.cs)
    }

    // ========================================================================
    // EJEMPLO 4: Rollback de Emergencia
    // ========================================================================

    /// <summary>
    /// Hacer rollback a una versión anterior después de un deployment problemático
    /// </summary>
    public async Task Example4_EmergencyRollback()
    {
        _logger.LogInformation("=== Ejemplo 4: Rollback de Emergencia ===");

        var repoId = "acme/ecommerce@main";

        // Escenario: La versión 3 causó problemas en producción
        _logger.LogWarning("⚠️ ALERTA: La versión 3 causó errores críticos!");

        // 1. Listar versiones
        var versions = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);

        _logger.LogInformation("Versiones disponibles:");
        for (int i = 0; i < versions.Count; i++)
        {
            var v = versions[i];
            var label = v.IsCurrent ? "ACTUAL (problemática)" : "disponible";
            _logger.LogInformation($"  {i + 1}. Version {v.VersionId[..8]}... [{label}]");
        }

        // 2. Identificar la última versión estable (versión 2)
        var stableVersion = versions[1];
        _logger.LogInformation($"🔄 Haciendo rollback a versión estable: {stableVersion.VersionId[..8]}...");

        // 3. Ejecutar rollback
        await _graphStore.RollbackToVersionAsync(repoId, stableVersion.VersionId, CancellationToken.None);

        _logger.LogInformation("✅ Rollback completado!");
        _logger.LogInformation("El sistema ahora apunta a la versión 2 (estable)");
        _logger.LogInformation("La versión 3 sigue existiendo y puede restaurarse después");

        // 4. Verificar que el rollback funcionó
        var versionsAfter = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);
        var currentVersion = versionsAfter.First(v => v.IsCurrent);

        _logger.LogInformation($"Versión actual ahora: {currentVersion.VersionId[..8]}... (commit: {currentVersion.CommitHash})");
    }

    // ========================================================================
    // EJEMPLO 5: Comparar Cambios Entre Versiones
    // ========================================================================

    /// <summary>
    /// Detectar qué cambió entre dos versiones para encontrar cuándo se introdujo un bug
    /// </summary>
    public async Task Example5_CompareVersions()
    {
        _logger.LogInformation("=== Ejemplo 5: Comparar Versiones ===");

        var repoId = "acme/ecommerce@main";
        var versions = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);

        // Comparar versión 2 vs versión 3
        var version2 = versions[1];
        var version3 = versions[0];

        _logger.LogInformation($"Comparando:");
        _logger.LogInformation($"  V2: {version2.Timestamp:yyyy-MM-dd HH:mm:ss} (commit: {version2.CommitHash})");
        _logger.LogInformation($"  V3: {version3.Timestamp:yyyy-MM-dd HH:mm:ss} (commit: {version3.CommitHash})");

        // Obtener grafos de ambas versiones
        var graph2 = await _graphStore.GetGraphAtTimestampAsync(
            repoId, 
            version2.Timestamp.ToUnixTimeSeconds(), 
            CancellationToken.None
        );

        var graph3 = await _graphStore.GetGraphAtTimestampAsync(
            repoId, 
            version3.Timestamp.ToUnixTimeSeconds(), 
            CancellationToken.None
        );

        // Detectar clases añadidas
        var addedClasses = graph3.Classes
            .Where(c3 => !graph2.Classes.Any(c2 => c2.Id == c3.Id))
            .ToList();

        // Detectar clases eliminadas
        var removedClasses = graph2.Classes
            .Where(c2 => !graph3.Classes.Any(c3 => c3.Id == c2.Id))
            .ToList();

        // Detectar clases modificadas (mismo ID pero diferente contenido)
        var modifiedClasses = graph3.Classes
            .Where(c3 => graph2.Classes.Any(c2 => c2.Id == c3.Id && c2.FilePath != c3.FilePath))
            .ToList();

        _logger.LogInformation($"\n📊 Cambios detectados:");
        _logger.LogInformation($"  ✅ Clases añadidas: {addedClasses.Count}");
        foreach (var cls in addedClasses)
            _logger.LogInformation($"     + {cls.Name}");

        _logger.LogInformation($"  ❌ Clases eliminadas: {removedClasses.Count}");
        foreach (var cls in removedClasses)
            _logger.LogInformation($"     - {cls.Name}");

        _logger.LogInformation($"  ✏️ Clases modificadas: {modifiedClasses.Count}");
        foreach (var cls in modifiedClasses)
            _logger.LogInformation($"     ~ {cls.Name}");

        // Resultado esperado:
        // 📊 Cambios detectados:
        //   ✅ Clases añadidas: 0
        //   ❌ Clases eliminadas: 1
        //      - Order
        //   ✏️ Clases modificadas: 0
    }

    // ========================================================================
    // EJEMPLO 6: Auditoría - ¿Cuándo se eliminó una clase?
    // ========================================================================

    /// <summary>
    /// Rastrear cuándo desapareció una clase específica del código
    /// </summary>
    public async Task Example6_AuditClassDeletion()
    {
        _logger.LogInformation("=== Ejemplo 6: Auditoría de Eliminación ===");

        var repoId = "acme/ecommerce@main";
        var targetClass = "Order";

        _logger.LogInformation($"🔍 Buscando cuándo se eliminó la clase '{targetClass}'...\n");

        var versions = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);

        VersionInfo? lastSeenVersion = null;
        VersionInfo? firstMissingVersion = null;

        // Recorrer versiones de la más antigua a la más nueva
        foreach (var version in versions.OrderBy(v => v.Timestamp))
        {
            var graph = await _graphStore.GetGraphAtTimestampAsync(
                repoId, 
                version.Timestamp.ToUnixTimeSeconds(), 
                CancellationToken.None
            );

            var classExists = graph.Classes.Any(c => c.Name == targetClass);

            if (classExists)
            {
                lastSeenVersion = version;
                _logger.LogInformation($"✅ V{versions.IndexOf(version) + 1} ({version.Timestamp:yyyy-MM-dd HH:mm:ss}): Clase existe");
            }
            else
            {
                if (firstMissingVersion == null)
                {
                    firstMissingVersion = version;
                    _logger.LogInformation($"❌ V{versions.IndexOf(version) + 1} ({version.Timestamp:yyyy-MM-dd HH:mm:ss}): Clase NO existe");
                }
            }
        }

        _logger.LogInformation($"\n📋 Resultado de auditoría:");
        if (lastSeenVersion != null && firstMissingVersion != null)
        {
            _logger.LogInformation($"  Última vez visto: {lastSeenVersion.Timestamp:yyyy-MM-dd HH:mm:ss} (commit: {lastSeenVersion.CommitHash})");
            _logger.LogInformation($"  Primera vez ausente: {firstMissingVersion.Timestamp:yyyy-MM-dd HH:mm:ss} (commit: {firstMissingVersion.CommitHash})");
            _logger.LogInformation($"  ⚠️ La clase '{targetClass}' fue eliminada entre estas dos versiones");
        }
        else if (lastSeenVersion == null)
        {
            _logger.LogInformation($"  La clase '{targetClass}' nunca existió en este repositorio");
        }
        else
        {
            _logger.LogInformation($"  La clase '{targetClass}' todavía existe en la versión actual");
        }
    }

    // ========================================================================
    // EJEMPLO 7: Rollback y Restauración (Reversible)
    // ========================================================================

    /// <summary>
    /// Demostrar que el rollback es reversible - puedes ir y volver
    /// </summary>
    public async Task Example7_ReversibleRollback()
    {
        _logger.LogInformation("=== Ejemplo 7: Rollback Reversible ===");

        var repoId = "acme/ecommerce@main";
        var versions = await _graphStore.GetVersionHistoryAsync(repoId, CancellationToken.None);

        var v1 = versions[2]; // Versión más antigua
        var v2 = versions[1];
        var v3 = versions[0]; // Versión más nueva

        _logger.LogInformation("Estado inicial: Versión 3 (actual)");

        // Rollback a V1
        _logger.LogInformation("\n🔄 Rollback a V1...");
        await _graphStore.RollbackToVersionAsync(repoId, v1.VersionId, CancellationToken.None);
        _logger.LogInformation("✅ Ahora en V1");

        // Avanzar a V2
        _logger.LogInformation("\n⏩ Avanzar a V2...");
        await _graphStore.RollbackToVersionAsync(repoId, v2.VersionId, CancellationToken.None);
        _logger.LogInformation("✅ Ahora en V2");

        // Regresar a V3 (la versión original)
        _logger.LogInformation("\n⏩ Restaurar V3 (versión original)...");
        await _graphStore.RollbackToVersionAsync(repoId, v3.VersionId, CancellationToken.None);
        _logger.LogInformation("✅ Restaurado a V3");

        _logger.LogInformation("\n📊 Resultado:");
        _logger.LogInformation("  • Ninguna versión fue eliminada");
        _logger.LogInformation("  • Puedes moverte libremente entre versiones");
        _logger.LogInformation("  • Es como 'git checkout' para el grafo de conocimiento");
    }

    // ========================================================================
    // EJEMPLO 8: Integración con GitHub Webhooks (Simulado)
    // ========================================================================

    /// <summary>
    /// Simular cómo funcionaría con webhooks de GitHub
    /// </summary>
    public async Task Example8_GitHubWebhookSimulation()
    {
        _logger.LogInformation("=== Ejemplo 8: Simulación GitHub Webhook ===");

        // Simular un push a GitHub
        var githubPayload = new
        {
            Repository = new { Owner = "acme", Name = "shop" },
            Ref = "refs/heads/main",
            After = "abc123def456789",  // SHA del nuevo commit
            Commits = new[]
            {
                new { Message = "Add payment processing", Author = "developer@acme.com" }
            }
        };

        _logger.LogInformation("📥 Webhook recibido de GitHub:");
        _logger.LogInformation($"  Repositorio: {githubPayload.Repository.Owner}/{githubPayload.Repository.Name}");
        _logger.LogInformation($"  Branch: {githubPayload.Ref.Replace("refs/heads/", "")}");
        _logger.LogInformation($"  Commit: {githubPayload.After[..7]}");
        _logger.LogInformation($"  Mensaje: {githubPayload.Commits[0].Message}");

        // Crear request de ingesta
        var repoRequest = new RepoRequest(
            Owner: githubPayload.Repository.Owner,
            Repo: githubPayload.Repository.Name,
            Branch: githubPayload.Ref.Replace("refs/heads/", ""),
            Path: githubPayload.After  // ← Commit SHA
        );

        _logger.LogInformation("\n🔄 Iniciando ingesta automática...");

        // En una implementación real, aquí llamarías a tu función de ingesta
        // que descarga el código de GitHub, lo analiza y lo guarda
        // await _ingestFunction.IngestAsync(repoRequest, ct);

        _logger.LogInformation("✅ Nueva versión creada automáticamente");
        _logger.LogInformation($"   Commit SHA: {githubPayload.After}");
        _logger.LogInformation("   Timestamp: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        _logger.LogInformation("\n📊 Resultado:");
        _logger.LogInformation("  • Cada push a GitHub crea una nueva versión en Neo4j");
        _logger.LogInformation("  • El commit SHA se guarda como metadata");
        _logger.LogInformation("  • Puedes rastrear cambios de GitHub a Neo4j");
    }
}

// ============================================================================
// PROGRAMA PRINCIPAL PARA EJECUTAR LOS EJEMPLOS
// ============================================================================

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configurar dependencias (simulado)
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<VersioningExamples>();

        var graphStore = new Neo4jVersionedGraphStore(
            uri: "neo4j+s://your-instance.neo4j.io",
            user: "neo4j",
            password: "your-password",
            logger: LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Neo4jVersionedGraphStore>()
        );

        var examples = new VersioningExamples(graphStore, logger);

        // Ejecutar todos los ejemplos
        try
        {
            await examples.Example1_SimpleIngest();
            await examples.Example2_ListVersionHistory();
            await examples.Example3_TimeTravel();
            await examples.Example4_EmergencyRollback();
            await examples.Example5_CompareVersions();
            await examples.Example6_AuditClassDeletion();
            await examples.Example7_ReversibleRollback();
            await examples.Example8_GitHubWebhookSimulation();
        }
        finally
        {
            await graphStore.DisposeAsync();
        }
    }
}
