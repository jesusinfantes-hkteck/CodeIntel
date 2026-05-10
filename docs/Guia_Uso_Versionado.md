# Versionado y Rollback en AriadnaKnowledgeStore - Guía de Uso

## Resumen

AriadnaKnowledgeStore ahora soporta **versionado completo del Knowledge Store** con capacidad de rollback. Cada vez que se analiza un repositorio (via webhook, manualmente, o por CI/CD), se crea una **nueva versión** que se puede consultar, comparar o restaurar.

---

## 🚀 Quick Start

### 1. Configurar en `appsettings.json`

```json
{
  "GraphStore": {
    "Type": "Neo4jVersioned"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "User": "neo4j",
    "Password": "your-password"
  }
}
```

### 2. Desplegar y configurar webhook de GitHub

```bash
# Desplegar Azure Function
func azure functionapp publish <your-function-app-name>

# Configurar webhook en GitHub
# Settings → Webhooks → Add webhook
# Payload URL: https://<your-function-app>.azurewebsites.net/api/webhook/github?code=<function-key>
# Content type: application/json
# Events: Just the push event
```

### 3. Cada push automáticamente crea una nueva versión

```
Push to GitHub → Webhook → AriadnaKnowledgeStore procesa → Nueva versión en Neo4j
```

---

## 📡 API Endpoints

### 1. Listar versiones de un repositorio

```bash
GET /api/repo/{owner}/{repo}/{branch}/versions

# Ejemplo
curl https://your-function-app.azurewebsites.net/api/repo/microsoft/dotnet/main/versions?code=YOUR_KEY
```

**Respuesta:**
```json
{
  "repoId": "microsoft/dotnet@main",
  "totalVersions": 15,
  "versions": [
    {
      "versionId": "a1b2c3d4",
      "commitHash": "7f8e9a1b",
      "timestamp": "2024-01-15T10:30:00Z",
      "isCurrent": true,
      "age": "00:00:15:23"
    },
    {
      "versionId": "e5f6g7h8",
      "commitHash": "6d5c4b3a",
      "timestamp": "2024-01-14T15:20:00Z",
      "isCurrent": false,
      "age": "1.19:10:00"
    }
  ]
}
```

### 2. Hacer rollback a una versión anterior

```bash
POST /api/repo/{owner}/{repo}/{branch}/rollback
Content-Type: application/json

{
  "versionId": "e5f6g7h8"
}
```

**Respuesta:**
```json
{
  "message": "Rollback successful",
  "repoId": "microsoft/dotnet@main",
  "versionId": "e5f6g7h8"
}
```

### 3. Obtener snapshot del grafo en un punto del tiempo

```bash
GET /api/repo/{owner}/{repo}/{branch}/snapshot?timestamp=1705329600

# Ejemplo: timestamp = 2024-01-15 12:00:00 UTC
curl "https://your-app.net/api/repo/microsoft/dotnet/main/snapshot?timestamp=1705329600&code=YOUR_KEY"
```

**Respuesta:**
```json
{
  "repoId": "microsoft/dotnet@main",
  "timestamp": "2024-01-15T12:00:00Z",
  "snapshot": {
    "classes": 1247,
    "methods": 8542,
    "edges": 15389,
    "topClasses": [
      { "name": "Program", "namespace": "MyApp" },
      { "name": "Startup", "namespace": "MyApp" }
    ]
  }
}
```

---

## 🔍 Queries de Neo4j para Explorar Versiones

### Ver todas las versiones

```cypher
MATCH (r:Repository {id: "owner/repo@main"})-[:HAS_VERSION]->(v:Version)
RETURN v.id AS versionId, 
       v.commitHash AS commit,
       datetime({epochSeconds: v.timestamp}) AS timestamp,
       v.isCurrent AS current
ORDER BY v.timestamp DESC
```

### Comparar dos versiones (qué clases se agregaron)

```cypher
// Clases en versión nueva que no existían en versión anterior
MATCH (v1:Version {id: $versionId1})-[:CONTAINS]->(c1:Class)
WHERE NOT EXISTS {
    MATCH (v2:Version {id: $versionId2})-[:CONTAINS]->(c2:Class)
    WHERE c1.id = c2.id
}
RETURN c1.name, c1.namespace, c1.filePath
```

### Ver qué cambió en una clase específica entre versiones

```cypher
MATCH (c1:Class {id: $classId})-[:NEXT_VERSION]->(c2:Class)
WHERE c1.versionId = $version1 AND c2.versionId = $version2
RETURN c1, c2
```

### Encontrar clases eliminadas (no tienen siguiente versión)

```cypher
MATCH (c:Class {repoId: $repoId})
WHERE c.validTo IS NOT NULL 
  AND NOT EXISTS((c)-[:NEXT_VERSION]->())
RETURN c.name, 
       datetime({epochSeconds: c.validFrom}) AS created,
       datetime({epochSeconds: c.validTo}) AS deleted
```

### Ver estado del código en una fecha específica

```cypher
// Obtener todas las clases válidas el 15 de enero de 2024
MATCH (c:Class {repoId: $repoId})
WHERE c.validFrom <= 1705324800 // Unix timestamp
  AND (c.validTo IS NULL OR c.validTo > 1705324800)
RETURN c.name, c.namespace
```

---

## 🛠️ Uso Programático en C#

### Ejemplo 1: Crear versión manualmente

```csharp
public class VersioningExample
{
    private readonly IVersionedGraphStore _store;

    public async Task CreateNewVersionAsync()
    {
        var repoRequest = new RepoRequest(
            Owner: "microsoft",
            Repo: "dotnet",
            Branch: "main",
            Path: "abc123def456" // CommitHash
        );

        var graphModel = await _analyzer.AnalyzeAsync(localPath, ct);

        // Esto crea automáticamente una nueva versión
        await _store.UpsertAsync(repoRequest, graphModel, ct);
    }
}
```

### Ejemplo 2: Consultar historial de versiones

```csharp
public async Task ShowVersionHistoryAsync(string repoId)
{
    var versions = await _store.GetVersionHistoryAsync(repoId, ct);

    foreach (var v in versions)
    {
        Console.WriteLine($"Version: {v.VersionId}");
        Console.WriteLine($"  Commit: {v.CommitHash}");
        Console.WriteLine($"  Date: {v.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Current: {v.IsCurrent}");
        Console.WriteLine($"  Age: {DateTimeOffset.UtcNow - v.Timestamp}");
        Console.WriteLine();
    }
}
```

### Ejemplo 3: Rollback automático si falla validación

```csharp
public async Task<bool> DeployWithRollbackAsync(RepoRequest req)
{
    // Guardar versión actual antes de actualizar
    var history = await _store.GetVersionHistoryAsync(req.GetRepoId(), ct);
    var previousVersion = history.FirstOrDefault(v => v.IsCurrent);

    try
    {
        // Actualizar Knowledge Store
        var graphModel = await _analyzer.AnalyzeAsync(localPath, ct);
        await _store.UpsertAsync(req, graphModel, ct);

        // Validar que la nueva versión es correcta
        var isValid = await ValidateNewVersionAsync(req.GetRepoId());

        if (!isValid)
        {
            _logger.LogWarning("New version failed validation, rolling back...");

            if (previousVersion != null)
            {
                await _store.RollbackToVersionAsync(req.GetRepoId(), previousVersion.VersionId, ct);
            }

            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Deployment failed, rolling back");

        if (previousVersion != null)
        {
            await _store.RollbackToVersionAsync(req.GetRepoId(), previousVersion.VersionId, ct);
        }

        throw;
    }
}

private async Task<bool> ValidateNewVersionAsync(string repoId)
{
    // Validaciones custom
    // - Número mínimo de clases
    // - No hay errores de análisis
    // - Comparación con versión anterior (no se eliminaron > 50% clases)
    return true;
}
```

### Ejemplo 4: Comparar dos versiones

```csharp
public async Task<VersionDiff> CompareVersionsAsync(string repoId, string v1, string v2)
{
    var graph1 = await _store.GetGraphAtTimestampAsync(repoId, GetTimestamp(v1), ct);
    var graph2 = await _store.GetGraphAtTimestampAsync(repoId, GetTimestamp(v2), ct);

    var addedClasses = graph2.Classes
        .Where(c2 => !graph1.Classes.Any(c1 => c1.Id == c2.Id))
        .ToList();

    var removedClasses = graph1.Classes
        .Where(c1 => !graph2.Classes.Any(c2 => c2.Id == c1.Id))
        .ToList();

    var modifiedClasses = graph2.Classes
        .Where(c2 => graph1.Classes.Any(c1 => c1.Id == c2.Id && c1.FilePath != c2.FilePath))
        .ToList();

    return new VersionDiff(addedClasses, removedClasses, modifiedClasses);
}
```

---

## 🔒 Políticas de Retención

Para evitar que el grafo crezca indefinidamente:

### Opción 1: Limpieza manual

```csharp
public async Task CleanupOldVersionsAsync(string repoId, int daysToKeep = 90)
{
    var cutoffTimestamp = DateTimeOffset.UtcNow.AddDays(-daysToKeep).ToUnixTimeSeconds();

    await using var session = _driver.AsyncSession();
    await session.ExecuteWriteAsync(async tx =>
    {
        // Eliminar nodos con validTo < cutoff (versiones cerradas antiguas)
        await tx.RunAsync(@"
            MATCH (n)
            WHERE n.repoId = $repoId 
              AND n.validTo IS NOT NULL 
              AND n.validTo < $cutoff
            DETACH DELETE n
        ",
        new { repoId, cutoff = cutoffTimestamp });
    });
}
```

### Opción 2: Azure Function con timer trigger

```csharp
[Function("CleanupOldVersions")]
public async Task CleanupScheduled(
    [TimerTrigger("0 0 2 * * *")] TimerInfo timer) // Todos los días a las 2 AM
{
    var retentionDays = int.Parse(_config["VersionManagement:RetentionDays"] ?? "90");

    // Obtener todos los repositorios
    var repos = await GetAllRepositoriesAsync();

    foreach (var repo in repos)
    {
        await CleanupOldVersionsAsync(repo.Id, retentionDays);
    }
}
```

---

## 🎯 Casos de Uso

### 1. CI/CD Pipeline con Validación

```yaml
# .github/workflows/deploy.yml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Trigger AriadnaKnowledgeStore Update
        run: |
          curl -X POST https://your-function-app.net/api/webhook/github \
            -H "Content-Type: application/json" \
            -d @webhook-payload.json

      - name: Wait for Processing
        run: sleep 30

      - name: Validate New Version
        id: validate
        run: |
          # Obtener versión actual
          VERSION=$(curl https://your-app.net/api/repo/owner/repo/main/versions | jq -r '.versions[0].versionId')

          # Validar (custom logic)
          if validate_version $VERSION; then
            echo "valid=true" >> $GITHUB_OUTPUT
          else
            echo "valid=false" >> $GITHUB_OUTPUT
          fi

      - name: Rollback if Invalid
        if: steps.validate.outputs.valid == 'false'
        run: |
          PREVIOUS=$(curl https://your-app.net/api/repo/owner/repo/main/versions | jq -r '.versions[1].versionId')
          curl -X POST https://your-app.net/api/repo/owner/repo/main/rollback \
            -H "Content-Type: application/json" \
            -d "{\"versionId\": \"$PREVIOUS\"}"
```

### 2. Análisis de Impacto de PR

```csharp
// Antes de merge, comparar rama feature vs main
public async Task<PullRequestImpact> AnalyzePRImpactAsync(string prBranch)
{
    var mainRepoId = "owner/repo@main";
    var prRepoId = $"owner/repo@{prBranch}";

    var mainGraph = await GetCurrentGraphAsync(mainRepoId);
    var prGraph = await GetCurrentGraphAsync(prRepoId);

    return new PullRequestImpact
    {
        ClassesAdded = CountAdded(mainGraph.Classes, prGraph.Classes),
        ClassesModified = CountModified(mainGraph.Classes, prGraph.Classes),
        ClassesDeleted = CountDeleted(mainGraph.Classes, prGraph.Classes),
        RiskLevel = CalculateRisk(mainGraph, prGraph)
    };
}
```

### 3. Auditoría de Cambios

```csharp
// "¿Qué código existía cuando surgió el bug X?"
public async Task<AuditReport> AuditCodeAtIncidentAsync(DateTime incidentTime)
{
    var timestamp = new DateTimeOffset(incidentTime).ToUnixTimeSeconds();
    var graph = await _store.GetGraphAtTimestampAsync(repoId, timestamp, ct);

    return new AuditReport
    {
        Timestamp = incidentTime,
        TotalClasses = graph.Classes.Count,
        TotalMethods = graph.Methods.Count,
        TopFiles = graph.Classes.GroupBy(c => c.FilePath).OrderByDescending(g => g.Count())
    };
}
```

---

## 📊 Monitoreo y Métricas

### Application Insights Queries

```kusto
// Tamaño del grafo por versión
traces
| where message contains "Successfully stored versioned graph"
| extend versionId = extract("Version: ([a-z0-9-]+)", 1, message)
| extend classes = extract("(\\d+) classes", 1, message)
| project timestamp, versionId, classes

// Tiempo promedio de creación de versión
traces
| where message contains "Storing versioned graph"
| summarize avg(duration) by bin(timestamp, 1h)

// Rollbacks ejecutados
traces
| where message contains "Rolling back"
| project timestamp, repoId = extract("Rolling back ([^ ]+)", 1, message)
```

---

## ⚠️ Troubleshooting

### Problema: "El grafo crece demasiado"

**Solución:** Configurar limpieza automática

```json
{
  "VersionManagement": {
    "RetentionDays": 30,
    "AutoCleanup": true
  }
}
```

### Problema: "Rollback no funciona"

**Verificar:**
1. ¿La versión target existe?
   ```cypher
   MATCH (v:Version {id: $versionId}) RETURN v
   ```

2. ¿Hay permisos en Neo4j?
   ```cypher
   SHOW USERS
   ```

### Problema: "Queries lentas en versiones antiguas"

**Solución:** Crear índices temporales

```cypher
CREATE INDEX class_temporal FOR (c:Class) ON (c.validFrom, c.validTo);
CREATE INDEX method_temporal FOR (m:Method) ON (m.validFrom, m.validTo);
```

---

## 🔗 Referencias

- [Documentación completa de versionado](./Versionado_y_Rollback_Neo4j.md)
- [Neo4j Temporal Patterns](https://neo4j.com/docs/cypher-manual/current/queries/temporal/)
- [Bitemporal Data Modeling](https://en.wikipedia.org/wiki/Temporal_database)

---

¿Preguntas? Abre un issue en el repositorio.
