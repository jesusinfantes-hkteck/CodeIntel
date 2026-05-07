# Estrategias de Versionado y Rollback en Neo4j para CodeIntel

## Problema

El código actual usa `MERGE` que sobrescribe nodos y relaciones, **imposibilitando el rollback** a versiones anteriores cuando:
- Un commit/PR introduce errores en el análisis
- Se despliega código defectuoso a producción
- Un cliente necesita volver a un estado anterior del Knowledge Store

## Soluciones Implementadas

### ✅ Estrategia 1: Versionado Temporal (Bitemporal) - **RECOMENDADA**

**Archivo:** `Neo4jVersionedGraphStore.cs`

#### Cómo Funciona
- Cada nodo (Class, Method) tiene propiedades `validFrom` y `validTo` (timestamps)
- Las versiones antiguas NO se eliminan, se marcan como cerradas (`validTo` se setea)
- Cada versión tiene un nodo `Version` con metadata (commitHash, timestamp)
- Relaciones `NEXT_VERSION` conectan versiones consecutivas del mismo elemento

#### Modelo de Datos
```cypher
// Estructura
(Repository)-[:HAS_VERSION]->(Version)-[:CONTAINS]->(Class)
                                                      |
                                         [:NEXT_VERSION]
                                                      ↓
                                                   (Class_v2)

// Propiedades de nodos versionados
{
  id: "class:MyClass",
  versionId: "abc123",
  validFrom: 1704067200,  // Unix timestamp
  validTo: null,          // null = versión actual
  // ... otros datos
}
```

#### Ventajas
✅ **Historial completo**: Puedes consultar el estado en cualquier punto del tiempo  
✅ **Diff entre versiones**: Comparar qué cambió entre commits  
✅ **Compacto**: Todo en una sola base de datos  
✅ **Queries temporales**: `WHERE validFrom <= $timestamp AND (validTo IS NULL OR validTo > $timestamp)`  
✅ **Auditabilidad**: Trazabilidad completa de cambios  

#### Desventajas
❌ El grafo crece con el tiempo (mitigable con políticas de retención)  
❌ Queries deben filtrar por validez temporal  

#### Casos de Uso
- ✅ Análisis forense: "¿Qué código existía cuando surgió el bug?"
- ✅ Compliance: Auditorías de cambios en el tiempo
- ✅ CI/CD: Comparar impacto de PR antes de merge
- ✅ Rollback quirúrgico: Volver solo algunos componentes

#### Ejemplo de Uso
```csharp
var store = new Neo4jVersionedGraphStore(uri, user, pass, logger);

// Guardar nueva versión (automáticamente cierra la anterior)
await store.UpsertAsync(repoRequest, graphModel, ct);

// Rollback completo a versión específica
await store.RollbackToVersionAsync(repoId, versionId, ct);

// Consultar estado histórico
var graphAtTime = await store.GetGraphAtTimestampAsync(repoId, timestamp, ct);

// Ver historial
var versions = await store.GetVersionHistoryAsync(repoId, ct);
// Output:
// - Version abc123 @ 2024-01-15 10:30 (current)
// - Version def456 @ 2024-01-14 15:20
// - Version ghi789 @ 2024-01-13 09:00
```

---

### ✅ Estrategia 2: Múltiples Bases de Datos - **Para alta disponibilidad**

**Archivo:** `Neo4jMultiDatabaseGraphStore.cs`

#### Cómo Funciona
- Cada versión se almacena en una **base de datos Neo4j separada**
- Una BD de metadatos (`codeintel_metadata`) mantiene el índice de versiones
- El "rollback" es instantáneo: solo cambia el puntero `CURRENT`

#### Modelo de Datos
```cypher
// Base de datos: codeintel_metadata
(Repository {id: "owner/repo@main"})
    -[:CURRENT]-> (Version {
                      id: "v1",
                      databaseName: "codeintel_owner_repo_main_abc123",
                      timestamp: 1704067200
                   })
    -[:HAS_VERSION]-> (Version {id: "v2", databaseName: "..."})
    -[:HAS_VERSION]-> (Version {id: "v3", databaseName: "..."})

// Base de datos: codeintel_owner_repo_main_abc123
// (Grafo completo de esa versión, sin propiedades temporales)
(Repository)-[:CONTAINS]->(Class)-[:HAS_METHOD]->(Method)
```

#### Ventajas
✅ **Aislamiento total**: Cada versión es independiente  
✅ **Rollback instantáneo**: Cambio de puntero en metadatos (< 1ms)  
✅ **Queries simples**: No hay filtros temporales, consultas directas  
✅ **Testing seguro**: Probar contra versión antigua sin afectar actual  
✅ **Eliminación fácil**: `DROP DATABASE` libera espacio completamente  

#### Desventajas
❌ **Mayor overhead**: N versiones = N bases de datos  
❌ **Límites Neo4j**: Neo4j Enterprise tiene límite de BDs (configurable)  
❌ **No hay queries cross-version**: No puedes comparar entre versiones fácilmente  

#### Casos de Uso
- ✅ Blue-Green deployments
- ✅ A/B testing de análisis de código
- ✅ Rollback instantáneo en producción
- ✅ Ambientes aislados (dev/staging/prod cada uno con su BD)

#### Ejemplo de Uso
```csharp
var store = new Neo4jMultiDatabaseGraphStore(uri, user, pass, logger);

// Crear nueva versión (nueva base de datos)
await store.UpsertAsync(repoRequest, graphModel, ct);
// Crea: codeintel_owner_repo_main_abc123

// Rollback (solo cambia puntero CURRENT)
await store.RollbackToVersionAsync(repoId, versionId, ct);

// Obtener BD actual
var currentDb = await store.GetCurrentDatabaseAsync(repoId);
// Output: "codeintel_owner_repo_main_abc123"

// Limpieza de versiones antiguas
await store.DeleteVersionAsync(repoId, oldVersionId, ct);
// Elimina la BD completa
```

---

### ⚡ Estrategia 3: Snapshots con Neo4j Dump (Manual)

No implementada en código, pero es una opción válida:

#### Cómo Funciona
```bash
# Antes de actualizar, crear snapshot
neo4j-admin database dump codeintel --to-path=/backups/

# Si algo falla, restaurar
neo4j-admin database load codeintel --from-path=/backups/snapshot-2024-01-15.dump
```

#### Ventajas
✅ Backups completos del sistema  
✅ Recuperación ante desastres  

#### Desventajas
❌ Manual (no programático)  
❌ Downtime durante restore  
❌ No permite queries a versiones antiguas  

---

## Comparación Rápida

| Característica | Versionado Temporal | Múltiples BDs | Snapshots |
|---------------|---------------------|---------------|-----------|
| **Rollback instantáneo** | ❌ (requiere update) | ✅ (cambio puntero) | ❌ (restore manual) |
| **Queries históricas** | ✅ | ❌ | ❌ |
| **Overhead espacial** | ⚠️ Moderado | ❌ Alto | ⚠️ Moderado |
| **Complejidad queries** | ⚠️ Filtros temporales | ✅ Simples | N/A |
| **Diff entre versiones** | ✅ | ⚠️ Complejo | ❌ |
| **Escalabilidad** | ✅ | ⚠️ (límite de BDs) | ✅ |
| **Auditabilidad** | ✅ | ✅ | ⚠️ |

---

## Recomendación Final

### Para CodeIntel, usa **Estrategia 1 (Versionado Temporal)** porque:

1. **Trazabilidad completa**: Los clientes quieren saber "¿qué código existía cuando...?"
2. **Análisis de impacto**: Comparar cómo un commit cambió la arquitectura
3. **CI/CD integration**: Validar PRs contra versión actual antes de merge
4. **Compliance**: Empresas necesitan auditoría de cambios en código legacy
5. **Diff entre versiones**: "Mostrar qué clases se agregaron/modificaron/eliminaron"

### Considera **Estrategia 2 (Múltiples BDs)** si:
- Necesitas rollback en < 1 segundo (alta disponibilidad extrema)
- Tienes pocos repositorios pero alto tráfico de consultas
- Quieres ambientes completamente aislados

---

## Integración con Webhooks GitHub

Para actualización incremental via webhooks:

```csharp
// GitHubWebhookFunction.cs
[Function("GitHubWebhook")]
public async Task<HttpResponseData> HandleWebhook(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
{
    // Parsear webhook payload
    var payload = await JsonSerializer.DeserializeAsync<GitHubPushEvent>(req.Body);

    // Crear nueva versión con commitHash
    var repoRequest = new RepoRequest(
        payload.Repository.Owner.Login,
        payload.Repository.Name,
        payload.Ref.Replace("refs/heads/", ""),
        Path: payload.HeadCommit.Id  // <-- CommitHash aquí
    );

    var graphModel = await _analyzer.AnalyzeAsync(localPath, ct);

    // Esto creará automáticamente una nueva versión
    await _versionedStore.UpsertAsync(repoRequest, graphModel, ct);

    return req.CreateResponse(HttpStatusCode.OK);
}
```

---

## Políticas de Retención

Para evitar que el grafo crezca indefinidamente:

```csharp
// Eliminar versiones más antiguas que 90 días
public async Task CleanupOldVersionsAsync(string repoId, int daysToKeep = 90)
{
    var cutoffTimestamp = DateTimeOffset.UtcNow.AddDays(-daysToKeep).ToUnixTimeSeconds();

    await using var session = _driver.AsyncSession();
    await session.ExecuteWriteAsync(async tx =>
    {
        // Eliminar nodos con validTo < cutoff
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

---

## Próximos Pasos

1. ✅ Implementar `Neo4jVersionedGraphStore` como default
2. ⬜ Agregar endpoint en Functions para listar versiones
3. ⬜ Crear UI para visualizar diff entre versiones
4. ⬜ Implementar políticas de retención configurables
5. ⬜ Agregar métricas: tamaño del grafo por versión, tiempo de rollback

---

## Ejemplo de Queries Útiles

### Ver qué clases cambiaron entre dos versiones
```cypher
MATCH (c1:Class {id: $classId})-[:NEXT_VERSION]->(c2:Class)
WHERE c1.versionId = $version1 AND c2.versionId = $version2
RETURN c1, c2
```

### Contar elementos por versión
```cypher
MATCH (v:Version {id: $versionId})-[:CONTAINS]->(c:Class)
RETURN count(c) as classCount
```

### Encontrar clases eliminadas
```cypher
MATCH (c:Class {repoId: $repoId})
WHERE c.validTo IS NOT NULL 
  AND NOT EXISTS((c)-[:NEXT_VERSION]->())
RETURN c.name, c.validTo
```
