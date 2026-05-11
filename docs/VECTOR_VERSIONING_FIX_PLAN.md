# 📋 PLAN DETALLADO: Fix de Versionado Vectorial

## ⚠️ ESTE DOCUMENTO ES UN PLAN - NO SE HA MODIFICADO CÓDIGO TODAVÍA

---

## 🎯 **OBJETIVO DEL FIX**

Hacer que los `CodeNode` (nodos vectoriales) se versionen igual que los nodos del grafo (Class, Method, BlazorComponent, etc.) para mantener consistencia entre vector search y graph traversal en operaciones de time travel y rollback.

---

## 📊 **ANÁLISIS DE IMPACTO**

### **Nivel de Invasividad: 🟡 MEDIO-ALTO**

| Categoría | Impacto | Detalles |
|-----------|---------|----------|
| **Cambios en interfaces** | 🔴 Breaking Change | Modificar `IVectorIndex` |
| **Cambios en implementaciones** | 🟡 Moderado | 2 clases a modificar |
| **Cambios en llamadas** | 🟢 Bajo | 1 lugar de llamada |
| **Tests afectados** | 🔴 Alto | Todos los tests de vectores |
| **Neo4j schema** | 🟢 Bajo | Solo agregar propiedades |
| **Datos existentes** | 🔴 Alto | Migración necesaria |
| **Backward compatibility** | 🔴 Breaking | NO compatible con versión anterior |

---

## 📂 **ARCHIVOS A MODIFICAR**

### **Archivos Core (Breaking Changes):**

1. ✏️ **`AriadnaKnowledgeStore.Core\Abstractions.cs`**
   - Modificar interface `IVectorIndex`
   - **Tipo:** Breaking change
   - **Líneas afectadas:** 2-3 líneas
   - **Riesgo:** 🔴 Alto (interface pública)

---

### **Archivos de Implementación:**

2. ✏️ **`AriadnaKnowledgeStore.Graph\Neo4jVectorIndex.cs`**
   - Modificar método `UpsertAsync()`
   - Modificar método `SearchAsync()` (agregar filtro temporal)
   - Agregar nuevo método `SearchAtTimestampAsync()`
   - **Tipo:** Implementación
   - **Líneas a modificar:** ~150 líneas
   - **Líneas a agregar:** ~100 líneas
   - **Riesgo:** 🟡 Medio (lógica compleja)

3. ✏️ **`AriadnaKnowledgeStore.Functions\Mocks\MockVectorIndex.cs`**
   - Modificar método `UpsertAsync()`
   - Agregar simulación de versionado
   - **Tipo:** Mock
   - **Líneas a modificar:** ~20 líneas
   - **Riesgo:** 🟢 Bajo (solo para testing)

---

### **Archivos de Orquestación:**

4. ✏️ **`AriadnaKnowledgeStore.Functions\Program.cs`**
   - Modificar clase `IngestOrchestrator`
   - Pasar metadata de versión a `UpsertAsync()`
   - **Tipo:** Orquestación
   - **Líneas a modificar:** ~10 líneas
   - **Riesgo:** 🟡 Medio (flujo crítico)

---

### **Archivos Nuevos (Opcional):**

5. 🆕 **`AriadnaKnowledgeStore.Graph\Neo4jVectorMigration.cs`**
   - Script de migración para datos existentes
   - **Tipo:** Herramienta de migración
   - **Líneas:** ~150 líneas
   - **Riesgo:** 🟢 Bajo (opcional, para migrar datos antiguos)

---

## 🔧 **CAMBIOS DETALLADOS POR ARCHIVO**

### **1. `Abstractions.cs` - Interface IVectorIndex**

#### **Estado Actual:**
```csharp
public interface IVectorIndex
{
    Task EnsureIndexAsync(CancellationToken ct);
    Task UpsertAsync(IEnumerable<VectorDocument> docs, CancellationToken ct);
}
```

#### **Estado Propuesto:**
```csharp
public interface IVectorIndex
{
    Task EnsureIndexAsync(CancellationToken ct);

    // Firma nueva (versionada)
    Task UpsertAsync(
        IEnumerable<VectorDocument> docs, 
        string versionId,           // ← NUEVO
        string repoId,              // ← NUEVO
        long timestamp,             // ← NUEVO
        CancellationToken ct);

    // Búsqueda con soporte temporal (opcional)
    Task<List<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int topK = 10,
        long? timestamp = null,     // ← NUEVO (null = actual)
        CancellationToken ct = default);
}
```

**Impacto:**
- 🔴 Breaking change
- ❌ Rompe implementaciones existentes
- ✅ Todas las implementaciones deben actualizarse

---

### **2. `Neo4jVectorIndex.cs` - Implementación Real**

#### **Cambios en `UpsertAsync()`:**

**Estado Actual (líneas 84-136):**
```cypher
MERGE (n:CodeNode {id: $id})
SET n.content = $content,
    n.embedding = $embedding,
    n.type = $type,
    n.className = $className,
    n.filePath = $filePath,
    n.lastUpdated = datetime()
```
**Problema:** `MERGE` sobrescribe el mismo nodo.

---

**Estado Propuesto:**
```cypher
// 1. Cerrar versiones anteriores (soft delete)
MATCH (prev:CodeNode {id: $id, repoId: $repoId})
WHERE prev.validTo IS NULL
SET prev.validTo = $timestamp

// 2. Crear nueva versión
CREATE (n:CodeNode {
    id: $id,
    versionId: $versionId,        // ← NUEVO
    repoId: $repoId,              // ← NUEVO
    validFrom: $timestamp,         // ← NUEVO
    validTo: null,                 // ← NUEVO
    content: $content,
    embedding: $embedding,
    type: $type,
    className: $className,
    filePath: $filePath
})

// 3. Enlazar a Version snapshot
WITH n
MATCH (v:Version {id: $versionId})
MERGE (v)-[:CONTAINS]->(n)

// 4. Enlazar versiones (historial)
WITH n
OPTIONAL MATCH (prev:CodeNode {id: $id, repoId: $repoId})
WHERE prev.validTo = $timestamp 
  AND prev.versionId <> $versionId
FOREACH (_ IN CASE WHEN prev IS NOT NULL THEN [1] ELSE [] END |
    MERGE (prev)-[:NEXT_VERSION]->(n)
)

// 5. Link to entity node (versionado)
WITH n
OPTIONAL MATCH (entity {id: $id, versionId: $versionId})
WHERE entity:Class OR entity:Method OR entity:BlazorComponent
FOREACH (_ IN CASE WHEN entity IS NOT NULL THEN [1] ELSE [] END |
    MERGE (n)-[:REPRESENTS]->(entity)
)
```

**Líneas a modificar:** ~60 líneas  
**Complejidad:** 🟡 Media  
**Riesgo:** 🟡 Medio (lógica crítica de Neo4j)

---

#### **Cambios en `SearchAsync()` (líneas 141-193):**

**Estado Actual:**
```cypher
CALL db.index.vector.queryNodes($indexName, $topK, $queryVector)
YIELD node, score
RETURN node.id, node.content, ...
ORDER BY score DESC
```
**Problema:** No filtra por versión.

---

**Estado Propuesto:**
```cypher
CALL db.index.vector.queryNodes($indexName, $topK, $queryVector)
YIELD node, score
WHERE CASE 
    WHEN $timestamp IS NULL THEN node.validTo IS NULL  // Actual
    ELSE node.validFrom <= $timestamp 
      AND (node.validTo IS NULL OR node.validTo > $timestamp)  // Histórico
END
RETURN node.id, node.content, ...
ORDER BY score DESC
```

**Líneas a modificar:** ~10 líneas  
**Complejidad:** 🟢 Baja  
**Riesgo:** 🟢 Bajo

---

#### **Nuevo método: `GetVersionedCodeNodeAsync()`**

Para recuperar un CodeNode específico en una versión:
```csharp
public async Task<CodeNode?> GetVersionedCodeNodeAsync(
    string nodeId, 
    string versionId, 
    CancellationToken ct)
{
    // Query: MATCH (n:CodeNode {id: $nodeId, versionId: $versionId})
}
```

**Líneas a agregar:** ~30 líneas  
**Complejidad:** 🟢 Baja  
**Riesgo:** 🟢 Bajo (método nuevo)

---

### **3. `MockVectorIndex.cs` - Mock para Testing**

#### **Cambios en `UpsertAsync()`:**

**Estado Actual (líneas 22-28):**
```csharp
public async Task UpsertAsync(IEnumerable<VectorDocument> docs, CancellationToken ct)
{
    _docs.AddRange(docs);
    _logger.LogInformation("[MOCK] Indexed {DocumentCount} documents", docs.Count());
    await Task.Delay(100, ct);
}
```

---

**Estado Propuesto:**
```csharp
// Agregar campos privados
private Dictionary<string, List<VersionedVectorDocument>> _versionedDocs = new();
private string? _currentVersion;

public async Task UpsertAsync(
    IEnumerable<VectorDocument> docs, 
    string versionId, 
    string repoId, 
    long timestamp, 
    CancellationToken ct)
{
    _currentVersion = versionId;

    if (!_versionedDocs.ContainsKey(versionId))
        _versionedDocs[versionId] = new List<VersionedVectorDocument>();

    _versionedDocs[versionId].AddRange(docs.Select(d => new VersionedVectorDocument
    {
        Document = d,
        VersionId = versionId,
        Timestamp = timestamp
    }));

    _logger.LogInformation("[MOCK] Indexed {Count} documents for version {Version}", 
        docs.Count(), versionId);
    await Task.Delay(100, ct);
}
```

**Líneas a modificar:** ~30 líneas  
**Complejidad:** 🟢 Baja  
**Riesgo:** 🟢 Bajo (solo mock)

---

### **4. `Program.cs - IngestOrchestrator`**

#### **Cambios en `RunAsync()` (líneas 116-156):**

**Estado Actual (línea 141):**
```csharp
await _index.UpsertAsync(toIndex, ct);
```

---

**Estado Propuesto:**
```csharp
// Necesitamos obtener el versionId del UpsertAsync del grafo
// Opción 1: Modificar IGraphStore para devolver versionId
// Opción 2: Generar versionId aquí y pasarlo a ambos

// Opción 2 (menos invasiva):
var versionId = Guid.NewGuid().ToString();
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var repoId = $"{req.Owner}/{req.Repo}@{req.Branch}";

// Pasar versionId al grafo (requiere modificar UpsertAsync)
await _graph.UpsertAsync(req, graphModel, versionId, timestamp, ct);

// Pasar versionId a vectores
await _index.UpsertAsync(toIndex, versionId, repoId, timestamp, ct);
```

**Problema:** También requiere modificar `IGraphStore.UpsertAsync()` para aceptar versionId.

**Alternativa (menos invasiva):**
Usar un servicio de versionado compartido:
```csharp
// Crear VersionContext
var versionContext = new VersionContext
{
    VersionId = Guid.NewGuid().ToString(),
    RepoId = $"{req.Owner}/{req.Repo}@{req.Branch}",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    CommitHash = req.Path
};

await _graph.UpsertAsync(req, graphModel, versionContext, ct);
await _index.UpsertAsync(toIndex, versionContext, ct);
```

**Líneas a modificar:** ~15 líneas  
**Complejidad:** 🟡 Media  
**Riesgo:** 🟡 Medio (flujo crítico)

---

## 🔄 **CAMBIOS EN IGraphStore (Adicional)**

### **Problema Detectado:**

El `IGraphStore` genera el `versionId` internamente, pero el `IVectorIndex` necesita ese mismo `versionId` para mantener consistencia.

### **Solución 1: Modificar IGraphStore (más invasivo):**

```csharp
public interface IGraphStore
{
    // Devolver VersionInfo con el versionId generado
    Task<VersionInfo> UpsertAsync(RepoRequest req, GraphModel model, CancellationToken ct);
}
```

**Impacto:**
- 🔴 Breaking change en `IGraphStore`
- ❌ Afecta todas las implementaciones
- ✅ Más limpio arquitectónicamente

---

### **Solución 2: Usar un VersionContext (menos invasivo):**

```csharp
// Nuevo record en Models.cs
public record VersionContext(
    string VersionId,
    string RepoId,
    long Timestamp,
    string? CommitHash
);

// Modificar interfaces
public interface IGraphStore
{
    Task UpsertAsync(RepoRequest req, GraphModel model, VersionContext version, CancellationToken ct);
}

public interface IVectorIndex
{
    Task UpsertAsync(IEnumerable<VectorDocument> docs, VersionContext version, CancellationToken ct);
}
```

**Impacto:**
- 🟡 Breaking change (pero con objeto compartido)
- ✅ Consistencia garantizada
- ✅ Más fácil de extender en el futuro

---

## 📦 **MIGRACIÓN DE DATOS EXISTENTES**

### **Problema:**

Los `CodeNode` existentes en Neo4j NO tienen:
- `versionId`
- `repoId`
- `validFrom`
- `validTo`

### **Solución: Script de Migración**

```cypher
// 1. Etiquetar nodos antiguos
MATCH (n:CodeNode)
WHERE NOT EXISTS(n.versionId)
SET n:LegacyCodeNode

// 2. Crear versión "legacy"
MERGE (v:Version {id: "legacy-version-v0"})
SET v.repoId = "unknown",
    v.timestamp = 0,
    v.isCurrent = false,
    v.commitHash = "legacy"

// 3. Migrar nodos
MATCH (legacy:LegacyCodeNode)
CREATE (n:CodeNode {
    id: legacy.id,
    versionId: "legacy-version-v0",
    repoId: "unknown/unknown@main",
    validFrom: 0,
    validTo: timestamp(),  // Marcar como cerrado
    content: legacy.content,
    embedding: legacy.embedding,
    type: legacy.type,
    className: legacy.className,
    filePath: legacy.filePath
})
WITH legacy, n
MATCH (v:Version {id: "legacy-version-v0"})
MERGE (v)-[:CONTAINS]->(n)

// 4. Eliminar nodos legacy
MATCH (legacy:LegacyCodeNode)
DETACH DELETE legacy
```

**Líneas:** ~50 líneas de Cypher  
**Riesgo:** 🟡 Medio (migración de datos)  
**Tiempo estimado:** ~5-10 minutos en DBs pequeñas

---

## 📈 **FASES DE IMPLEMENTACIÓN**

### **Fase 1: Cambios Core (Breaking Changes)**
1. ✏️ Modificar `IVectorIndex` en `Abstractions.cs`
2. ✏️ Modificar `IGraphStore` para devolver/aceptar `VersionContext`
3. ✏️ Crear record `VersionContext` en `Models.cs`

**Impacto:** 🔴 Alto  
**Duración:** 30 minutos  
**Riesgo:** Alto (rompe todo)

---

### **Fase 2: Implementación Neo4j**
4. ✏️ Modificar `Neo4jVectorIndex.UpsertAsync()`
5. ✏️ Modificar `Neo4jVectorIndex.SearchAsync()`
6. ✏️ Agregar `SearchAtTimestampAsync()`
7. ✏️ Modificar `Neo4jVersionedGraphStore.UpsertAsync()` (devolver VersionInfo)

**Impacto:** 🟡 Medio  
**Duración:** 2-3 horas  
**Riesgo:** Medio (lógica compleja)

---

### **Fase 3: Actualizar Orquestador**
8. ✏️ Modificar `IngestOrchestrator.RunAsync()`
9. ✏️ Pasar `VersionContext` a graph y vector

**Impacto:** 🟡 Medio  
**Duración:** 30 minutos  
**Riesgo:** Medio (flujo crítico)

---

### **Fase 4: Actualizar Mocks**
10. ✏️ Modificar `MockVectorIndex.UpsertAsync()`
11. ✏️ Modificar `MockGraphStore.UpsertAsync()` (si existe)

**Impacto:** 🟢 Bajo  
**Duración:** 30 minutos  
**Riesgo:** Bajo (solo testing)

---

### **Fase 5: Migración de Datos (Opcional)**
12. 🆕 Crear `Neo4jVectorMigration.cs`
13. 🆕 Ejecutar script de migración

**Impacto:** 🟡 Medio  
**Duración:** 1 hora + tiempo de migración  
**Riesgo:** Medio (datos existentes)

---

### **Fase 6: Testing y Validación**
14. ✅ Tests unitarios de versionado
15. ✅ Tests de integración Neo4j
16. ✅ Tests de time travel
17. ✅ Tests de rollback

**Impacto:** 🟢 Bajo  
**Duración:** 2-3 horas  
**Riesgo:** Bajo

---

## ⏱️ **ESTIMACIÓN DE TIEMPO TOTAL**

| Fase | Duración | Riesgo |
|------|----------|--------|
| Fase 1: Core Changes | 30 min | 🔴 Alto |
| Fase 2: Neo4j Implementation | 2-3 horas | 🟡 Medio |
| Fase 3: Orchestrator | 30 min | 🟡 Medio |
| Fase 4: Mocks | 30 min | 🟢 Bajo |
| Fase 5: Migration | 1 hora | 🟡 Medio |
| Fase 6: Testing | 2-3 horas | 🟢 Bajo |
| **TOTAL** | **7-9 horas** | **🟡 Medio** |

---

## 🚨 **RIESGOS IDENTIFICADOS**

### **Riesgo 1: Breaking Changes en Interfaces Públicas**
- **Impacto:** 🔴 Alto
- **Probabilidad:** 100%
- **Mitigación:** 
  - Versionar la librería (bump major version)
  - Documentar cambios en CHANGELOG
  - Crear guía de migración

---

### **Riesgo 2: Datos Existentes sin Versionar**
- **Impacto:** 🔴 Alto
- **Probabilidad:** Alta (si hay datos)
- **Mitigación:**
  - Script de migración incluido
  - Backup antes de migrar
  - Opción de mantener legacy nodes

---

### **Riesgo 3: Complejidad de Queries Neo4j**
- **Impacto:** 🟡 Medio
- **Probabilidad:** Media
- **Mitigación:**
  - Tests extensivos
  - Queries optimizadas
  - Índices apropiados

---

### **Riesgo 4: Performance de Vector Search con Versionado**
- **Impacto:** 🟡 Medio
- **Probabilidad:** Media
- **Mitigación:**
  - Índice vectorial cubre TODOS los nodos
  - Filtro temporal post-query (eficiente)
  - Monitoreo de performance

---

### **Riesgo 5: Inconsistencia Durante la Transición**
- **Impacto:** 🔴 Alto
- **Probabilidad:** Baja (con plan correcto)
- **Mitigación:**
  - Hacer todos los cambios en una sola PR
  - Testing exhaustivo antes de merge
  - Feature flag para activar/desactivar

---

## ✅ **BENEFICIOS DEL FIX**

1. ✅ **Consistencia Total:** Grafo y vectores sincronizados
2. ✅ **Time Travel Funcional:** Búsquedas históricas precisas
3. ✅ **Rollback Completo:** Incluye embeddings
4. ✅ **Auditoría:** Historial completo de cambios semánticos
5. ✅ **GraphRAG Temporal:** Queries en cualquier punto del tiempo

---

## 📝 **ALTERNATIVAS CONSIDERADAS**

### **Alternativa 1: No Hacer Nada (Status Quo)**
- ✅ No requiere cambios
- ❌ Time travel NO funciona correctamente
- ❌ Rollback incompleto
- ❌ Inconsistencia permanente

**Conclusión:** ❌ No recomendado

---

### **Alternativa 2: Versionado Ligero (Solo isCurrent)**
```cypher
CREATE (n:CodeNode {
    id: $id,
    isCurrent: true,  // Solo este flag
    embedding: $embedding
})
MATCH (old:CodeNode {id: $id, isCurrent: true})
SET old.isCurrent = false
```

- ✅ Más simple
- ✅ Menos espacio
- ❌ NO permite time travel
- ❌ Solo soporta rollback binario (actual vs anterior)

**Conclusión:** 🟡 Opción válida pero limitada

---

### **Alternativa 3: Versionado Completo (Propuesto)**
```cypher
CREATE (n:CodeNode {
    id: $id,
    versionId: $versionId,
    validFrom: $timestamp,
    validTo: null,
    embedding: $embedding
})
```

- ✅ Consistencia total
- ✅ Time travel completo
- ✅ Auditoría completa
- ❌ Más complejo
- ❌ Más espacio

**Conclusión:** ✅ Recomendado para arquitectura robusta

---

## 🎯 **DECISIÓN RECOMENDADA**

### **Opción A: Implementación Completa (Recomendada)**
- Versionado temporal completo
- Breaking changes aceptados
- Migración de datos incluida
- **Tiempo:** 7-9 horas
- **Riesgo:** 🟡 Medio
- **Beneficio:** 🟢 Alto

---

### **Opción B: Implementación Progresiva**
1. **Semana 1:** Cambiar interfaces (breaking)
2. **Semana 2:** Implementar Neo4j
3. **Semana 3:** Migración de datos
4. **Semana 4:** Testing y validación

- **Tiempo:** Distribuido en 4 semanas
- **Riesgo:** 🟢 Bajo (más controlado)
- **Beneficio:** 🟢 Alto

---

### **Opción C: Feature Flag (Más Segura)**
```csharp
public class Neo4jVectorIndex
{
    private readonly bool _enableVersioning;

    public Neo4jVectorIndex(..., bool enableVersioning = false)
    {
        _enableVersioning = enableVersioning;
    }

    public async Task UpsertAsync(...)
    {
        if (_enableVersioning)
            await UpsertVersionedAsync(...);
        else
            await UpsertLegacyAsync(...);
    }
}
```

- **Tiempo:** +1 hora (implementar flag)
- **Riesgo:** 🟢 Bajo (rollback fácil)
- **Beneficio:** 🟢 Alto + seguridad

---

## 📊 **RESUMEN EJECUTIVO**

| Aspecto | Evaluación |
|---------|------------|
| **Invasividad** | 🟡 Media-Alta (breaking changes) |
| **Complejidad** | 🟡 Media (queries complejas) |
| **Duración** | 7-9 horas de desarrollo |
| **Riesgo** | 🟡 Medio (mitigable) |
| **Beneficio** | 🟢 Alto (consistencia total) |
| **Breaking Changes** | ✅ Sí (2 interfaces) |
| **Data Migration** | ✅ Necesaria (script incluido) |
| **Backward Compatibility** | ❌ NO (requiere actualización) |

---

## ✅ **RECOMENDACIÓN FINAL**

**Proceder con Opción C: Feature Flag + Implementación Completa**

1. Implementar versionado completo
2. Añadir feature flag para activar/desactivar
3. Hacer migración de datos como paso opcional
4. Testing exhaustivo antes de activar

**Ventajas:**
- ✅ Implementación completa y robusta
- ✅ Rollback fácil si hay problemas
- ✅ Testing en producción sin riesgo
- ✅ Migración gradual posible

---

## 🚀 **PRÓXIMOS PASOS**

**Si apruebas el plan:**

1. Crear branch: `feature/vector-versioning`
2. Implementar Fase 1-6 en orden
3. Crear PR con documentación
4. Testing en staging
5. Merge cuando esté validado

**Si necesitas ajustes:**

- ¿Prefieres Opción A, B o C?
- ¿Hay algún riesgo que te preocupe más?
- ¿Prefieres que explique alguna parte en más detalle?

---

**Creado:** 2024  
**Autor:** GitHub Copilot  
**Estado:** 📋 Plan - Pendiente de Aprobación
