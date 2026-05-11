# 🚨 PROBLEMA CRÍTICO: Inconsistencia en Versionado de Nodos Vectoriales

## ⚠️ **El Problema Identificado**

Has detectado una **inconsistencia arquitectónica seria** entre:
- **Base de datos de Grafo** (versionada) ✅
- **Base de datos Vectorial** (NO versionada) ❌

---

## 🔍 **Análisis del Problema Actual**

### **Estado Actual del Código:**

#### **1. Nodos del Grafo (Class, Method, BlazorComponent, etc.):**
```cypher
// ✅ VERSIONADOS
CREATE (c:Class {
    id: "class:Product",
    versionId: "guid-v2",
    validFrom: t2,
    validTo: t3,
    ...
})
```
**Características:**
- ✅ Tienen `versionId`
- ✅ Tienen `validFrom` y `validTo`
- ✅ Se cierran al crear nueva versión (soft delete)
- ✅ Están enlazados a un nodo `Version`

---

#### **2. Nodos Vectoriales (CodeNode):**
```cypher
// ❌ NO VERSIONADOS (implementación actual)
MERGE (n:CodeNode {id: "class:Product"})
SET n.content = $content,
    n.embedding = $embedding,
    n.type = $type,
    n.lastUpdated = datetime()
```
**Problemas:**
- ❌ NO tiene `versionId`
- ❌ NO tiene `validFrom` ni `validTo`
- ❌ Se hace `MERGE` (actualiza el mismo nodo)
- ❌ **Pierde el historial** al ingerir nueva versión

---

## 💥 **Consecuencias del Problema**

### **Escenario Real:**

```
t1: Ingesta #1
  → Grafo: Class "Product" v1 [validFrom: t1, validTo: t2]
  → Vector: CodeNode "Product" [embedding de v1]

t2: Ingesta #2 (código modificado)
  → Grafo: Class "Product" v2 [validFrom: t2, validTo: NULL]
  → Vector: CodeNode "Product" [embedding de v2] ← SOBRESCRIBE v1

t3: Time Travel a t1
  → Grafo: Devuelve Class v1 ✅
  → Vector: Devuelve CodeNode v2 ❌ INCONSISTENCIA!
```

---

## 🔴 **Comportamiento Actual (Incorrecto):**

### **Cuando haces ingesta:**

```csharp
// Paso 1: Se crea nueva versión en grafo
await graphStore.UpsertAsync(req, model, ct);
// → Crea Version v2
// → Cierra nodos v1 (validTo = t2)
// → Crea nodos v2 (validFrom = t2, validTo = NULL)

// Paso 2: Se actualizan vectores
var chunks = CodeChunker.ToVectorDocs(model);
var embeddings = await embeddingService.EmbedAsync(chunks, ct);
await vectorIndex.UpsertAsync(embeddings, ct);
// → MERGE CodeNode {id: "class:Product"}
// → ❌ SOBRESCRIBE el embedding anterior!
```

### **Resultado:**
```
Neo4j:
├── Version v1 [isCurrent: false]
│   └── Class "Product" v1 [validFrom: t1, validTo: t2]
├── Version v2 [isCurrent: true]
│   └── Class "Product" v2 [validFrom: t2, validTo: NULL]
└── CodeNode "Product" [embedding de v2] ← SOLO LA ÚLTIMA VERSIÓN!
    // ❌ Se perdió el embedding de v1
```

---

## 🎯 **Queries que NO Funcionan Correctamente:**

### **Query 1: Time Travel + Vector Search**
```csharp
// Usuario pregunta: "¿Cómo funcionaba el pago hace 2 días?"

// 1. Vector Search
var results = await vectorIndex.SearchAsync(queryEmbedding, topK: 10);
// Devuelve: CodeNode con embedding ACTUAL (v2)

// 2. Time Travel
var historicalGraph = await graphStore.GetGraphAtTimestampAsync(repoId, timestampT1, ct);
// Devuelve: Nodos de v1

// ❌ PROBLEMA: El embedding es de v2, pero el grafo es de v1!
```

### **Query 2: Rollback**
```csharp
// Hacer rollback a v1
await graphStore.RollbackToVersionAsync(repoId, "version-v1-guid", ct);

// Grafo ahora apunta a v1 ✅
// Pero vector search sigue devolviendo embeddings de v2 ❌
```

---

## ✅ **Solución Correcta: Versionar los CodeNodes**

### **Opción 1: Versionado Completo (Recomendada)**

Modificar `Neo4jVectorIndex.UpsertAsync()` para crear nodos versionados:

```cypher
// ANTES (incorrecto)
MERGE (n:CodeNode {id: $id})
SET n.embedding = $embedding,
    n.lastUpdated = datetime()

// DESPUÉS (correcto)
CREATE (n:CodeNode {
    id: $id,
    versionId: $versionId,        // ← AGREGAR
    validFrom: $timestamp,         // ← AGREGAR
    validTo: null,                 // ← AGREGAR
    repoId: $repoId,              // ← AGREGAR
    embedding: $embedding,
    content: $content,
    type: $type,
    className: $className,
    filePath: $filePath
})

// Cerrar versiones anteriores
WITH n
MATCH (prev:CodeNode {id: $id, repoId: $repoId})
WHERE prev.validTo IS NULL AND prev.versionId <> $versionId
SET prev.validTo = $timestamp

// Enlazar versiones
WITH n
OPTIONAL MATCH (prev:CodeNode {id: $id, repoId: $repoId})
WHERE prev.validTo = $timestamp AND prev.versionId <> $versionId
FOREACH (_ IN CASE WHEN prev IS NOT NULL THEN [1] ELSE [] END |
    MERGE (prev)-[:NEXT_VERSION]->(n)
)

// Enlazar a Version snapshot
WITH n
MATCH (v:Version {id: $versionId})
MERGE (v)-[:CONTAINS]->(n)
```

---

### **Opción 2: Versionado Ligero (Alternativa)**

Si quieres mantener solo las últimas N versiones:

```cypher
CREATE (n:CodeNode {
    id: $id,
    versionId: $versionId,
    isCurrent: true,
    embedding: $embedding,
    ...
})

// Marcar anterior como no-current (sin borrar)
WITH n
MATCH (prev:CodeNode {id: $id, isCurrent: true})
WHERE prev.versionId <> $versionId
SET prev.isCurrent = false

// Limpieza: Mantener solo últimas 5 versiones
WITH n
MATCH (old:CodeNode {id: $id, isCurrent: false})
WITH old ORDER BY old.versionId DESC SKIP 5
DETACH DELETE old
```

---

## 🔧 **Cambios Necesarios en el Código**

### **Archivo: `Neo4jVectorIndex.cs`**

#### **Método `UpsertAsync` (Modificar):**

```csharp
public async Task UpsertAsync(
    IEnumerable<VectorDocument> docs, 
    string versionId,           // ← NUEVO parámetro
    string repoId,              // ← NUEVO parámetro
    long timestamp,             // ← NUEVO parámetro
    CancellationToken ct)
{
    var docList = docs.ToList();
    _logger.LogInformation("Upserting {Count} versioned vector documents", docList.Count);

    await using var session = _driver.AsyncSession();

    try
    {
        await session.ExecuteWriteAsync(async tx =>
        {
            foreach (var doc in docList)
            {
                ct.ThrowIfCancellationRequested();

                // Cerrar versiones anteriores
                await tx.RunAsync(@"
                    MATCH (prev:CodeNode {id: $id, repoId: $repoId})
                    WHERE prev.validTo IS NULL
                    SET prev.validTo = $timestamp
                ",
                new { id = doc.Id, repoId, timestamp });

                // Crear nueva versión del CodeNode
                await tx.RunAsync(@"
                    CREATE (n:CodeNode {
                        id: $id,
                        versionId: $versionId,
                        repoId: $repoId,
                        validFrom: $timestamp,
                        validTo: null,
                        content: $content,
                        embedding: $embedding,
                        type: $type,
                        className: $className,
                        filePath: $filePath
                    })

                    WITH n
                    MATCH (v:Version {id: $versionId})
                    MERGE (v)-[:CONTAINS]->(n)

                    // Enlazar versiones
                    WITH n
                    OPTIONAL MATCH (prev:CodeNode {id: $id, repoId: $repoId})
                    WHERE prev.validTo = $timestamp 
                      AND prev.versionId <> $versionId
                    FOREACH (_ IN CASE WHEN prev IS NOT NULL THEN [1] ELSE [] END |
                        MERGE (prev)-[:NEXT_VERSION]->(n)
                    )

                    // Link to entity node
                    WITH n
                    OPTIONAL MATCH (entity {id: $id, versionId: $versionId})
                    WHERE entity:Class OR entity:Method OR entity:BlazorComponent
                    FOREACH (_ IN CASE WHEN entity IS NOT NULL THEN [1] ELSE [] END |
                        MERGE (n)-[:REPRESENTS]->(entity)
                    )
                ",
                new
                {
                    id = doc.Id,
                    versionId,
                    repoId,
                    timestamp,
                    content = doc.Content,
                    embedding = doc.Embedding.ToArray(),
                    type = doc.Type,
                    className = doc.ClassName,
                    filePath = doc.FilePath
                });
            }
        });

        _logger.LogInformation("Successfully upserted {Count} versioned vector documents", docList.Count);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to upsert versioned vector documents");
        throw;
    }
}
```

---

### **Archivo: `Abstractions.cs` (Modificar Interface)**

```csharp
public interface IVectorIndex
{
    Task EnsureIndexAsync(CancellationToken ct);

    // Firma anterior (deprecated)
    // Task UpsertAsync(IEnumerable<VectorDocument> docs, CancellationToken ct);

    // Nueva firma versionada
    Task UpsertAsync(
        IEnumerable<VectorDocument> docs, 
        string versionId, 
        string repoId, 
        long timestamp, 
        CancellationToken ct);
}
```

---

### **Archivo: `IngestFunction.cs` (Llamada actualizada)**

```csharp
// Antes
await _vectorIndex.UpsertAsync(vectorDocs, ct);

// Después
await _vectorIndex.UpsertAsync(
    vectorDocs, 
    versionId: currentVersionId,      // Del UpsertAsync del grafo
    repoId: $"{req.Owner}/{req.Repo}@{req.Branch}",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    ct
);
```

---

## 🔍 **Vector Search Versionado**

### **Nuevo método: `SearchAsync` con versionado**

```csharp
public async Task<List<VectorSearchResult>> SearchAsync(
    float[] queryEmbedding, 
    int topK = 10,
    long? timestamp = null,        // ← NUEVO: null = actual, valor = histórico
    CancellationToken ct = default)
{
    _logger.LogInformation("Searching for top {TopK} similar code nodes", topK);

    await using var session = _driver.AsyncSession();

    var results = await session.ExecuteReadAsync(async tx =>
    {
        var query = timestamp.HasValue
            ? @"
                // Búsqueda histórica
                CALL db.index.vector.queryNodes($indexName, $topK, $embedding)
                YIELD node, score
                WHERE node.validFrom <= $timestamp 
                  AND (node.validTo IS NULL OR node.validTo > $timestamp)
                RETURN node.id as id, 
                       node.content as content, 
                       node.type as type,
                       score
                ORDER BY score DESC
              "
            : @"
                // Búsqueda actual
                CALL db.index.vector.queryNodes($indexName, $topK, $embedding)
                YIELD node, score
                WHERE node.validTo IS NULL
                RETURN node.id as id, 
                       node.content as content, 
                       node.type as type,
                       score
                ORDER BY score DESC
              ";

        var result = await tx.RunAsync(query, new
        {
            indexName = VectorIndexName,
            topK,
            embedding = queryEmbedding,
            timestamp
        });

        // ... procesar resultados
    });
}
```

---

## ✅ **Beneficios de la Solución:**

1. **Consistencia Total:**
   - Grafo versionado ✅
   - Vector versionado ✅
   - Time travel funciona correctamente ✅

2. **Rollback Completo:**
   - Cambias `isCurrent` en grafo ✅
   - Vector search filtra por `validTo IS NULL` ✅
   - Sistema completamente consistente ✅

3. **Auditoría:**
   - Historial completo de embeddings ✅
   - Puedes comparar cómo cambió la semántica del código ✅
   - Detectar cuándo un cambio introdujo ambigüedad ✅

4. **GraphRAG Temporal:**
   - Vector search en timestamp específico ✅
   - Graph expansion en misma versión ✅
   - Respuestas históricas precisas ✅

---

## 📊 **Comparación: Antes vs Después**

### **ANTES (Estado Actual - Incorrecto):**
```
t1: Ingesta #1
  Grafo: Class v1 [validFrom: t1, validTo: t2]
  Vector: CodeNode [embedding v1]

t2: Ingesta #2
  Grafo: Class v2 [validFrom: t2, validTo: NULL]
  Vector: CodeNode [embedding v2] ← SOBRESCRIBE v1 ❌

Time Travel a t1:
  Grafo: Devuelve v1 ✅
  Vector: Devuelve v2 ❌ INCONSISTENTE!
```

### **DESPUÉS (Con Fix - Correcto):**
```
t1: Ingesta #1
  Grafo: Class v1 [validFrom: t1, validTo: t2]
  Vector: CodeNode v1 [validFrom: t1, validTo: t2]

t2: Ingesta #2
  Grafo: Class v2 [validFrom: t2, validTo: NULL]
  Vector: CodeNode v2 [validFrom: t2, validTo: NULL]

Time Travel a t1:
  Grafo: Devuelve v1 ✅
  Vector: Devuelve v1 ✅ CONSISTENTE!
```

---

## 🚀 **Próximos Pasos:**

1. ✅ Modificar `IVectorIndex` interface
2. ✅ Actualizar `Neo4jVectorIndex.UpsertAsync()`
3. ✅ Agregar parámetros de versionado a `SearchAsync()`
4. ✅ Actualizar `IngestFunction` para pasar versionId
5. ✅ Crear tests de versionado vectorial
6. ✅ Actualizar documentación

---

## ⚠️ **Impacto de NO arreglar esto:**

❌ Time travel NO funciona correctamente  
❌ Rollback del grafo NO afecta los vectores  
❌ Se pierde el historial de embeddings  
❌ Inconsistencia entre grafo y vectores  
❌ GraphRAG temporal NO es fiable  

---

## ✅ **¿Quieres que implemente el fix ahora?**

El fix involucra:
- Modificar `IVectorIndex` interface
- Actualizar `Neo4jVectorIndex.cs` (~100 líneas)
- Actualizar llamadas en `IngestFunction`
- Crear ejemplos de uso

**¿Procedo con la implementación?**

---

**Documentado:** 2024  
**Prioridad:** 🔴 CRÍTICA  
**Impacto:** Alto (Arquitectura fundamental)
