# 🔴 Diagrama Visual: Problema de Versionado en Vectores

## **ESTADO ACTUAL (INCORRECTO)** ❌

```
┌─────────────────────────────────────────────────────────────┐
│                    INGESTA #1 (t1)                          │
└─────────────────────────────────────────────────────────────┘

Neo4j después de Ingesta #1:

├── Version v1 [isCurrent: true, timestamp: t1]
│   └── CONTAINS
│       ├── Class "Product" [validFrom: t1, validTo: NULL]
│       └── Method "GetPrice" [validFrom: t1, validTo: NULL]
│
└── Vector Index:
    ├── CodeNode "class:Product" [embedding: [0.1, 0.2, ...]]
    └── CodeNode "method:GetPrice" [embedding: [0.3, 0.4, ...]]


┌─────────────────────────────────────────────────────────────┐
│                    INGESTA #2 (t2)                          │
│                  (Código modificado)                         │
└─────────────────────────────────────────────────────────────┘

Neo4j después de Ingesta #2:

├── Version v1 [isCurrent: false, timestamp: t1]
│   └── CONTAINS
│       ├── Class "Product" [validFrom: t1, validTo: t2] ← CERRADO
│       └── Method "GetPrice" [validFrom: t1, validTo: t2] ← CERRADO
│
├── Version v2 [isCurrent: true, timestamp: t2]
│   └── CONTAINS
│       ├── Class "Product" [validFrom: t2, validTo: NULL] ← NUEVO
│       └── Method "GetPrice" [validFrom: t2, validTo: NULL] ← NUEVO
│
└── Vector Index:
    ├── CodeNode "class:Product" [embedding: [0.5, 0.6, ...]] ← SOBRESCRITO ❌
    └── CodeNode "method:GetPrice" [embedding: [0.7, 0.8, ...]] ← SOBRESCRITO ❌

    // ❌ Se perdió el embedding de v1!


┌─────────────────────────────────────────────────────────────┐
│          TIME TRAVEL: Consulta histórica en t1              │
└─────────────────────────────────────────────────────────────┘

Query Grafo:
  MATCH (c:Class)
  WHERE c.validFrom <= t1 AND (c.validTo IS NULL OR c.validTo > t1)
  RETURN c

  ✅ Devuelve: Class "Product" v1 (código de t1)

Query Vector:
  CALL db.index.vector.queryNodes(...)
  YIELD node
  RETURN node

  ❌ Devuelve: CodeNode con embedding v2 (código de t2)

🔴 INCONSISTENCIA: Grafo de t1, embedding de t2!
```

---

## **SOLUCIÓN PROPUESTA (CORRECTA)** ✅

```
┌─────────────────────────────────────────────────────────────┐
│                    INGESTA #1 (t1)                          │
└─────────────────────────────────────────────────────────────┘

Neo4j después de Ingesta #1:

├── Version v1 [isCurrent: true, timestamp: t1]
│   └── CONTAINS
│       ├── Class "Product" [validFrom: t1, validTo: NULL]
│       ├── Method "GetPrice" [validFrom: t1, validTo: NULL]
│       ├── CodeNode "class:Product" [validFrom: t1, validTo: NULL, embedding: v1]
│       └── CodeNode "method:GetPrice" [validFrom: t1, validTo: NULL, embedding: v1]
│
└── Vector Index (indexa todos los CodeNodes)


┌─────────────────────────────────────────────────────────────┐
│                    INGESTA #2 (t2)                          │
│                  (Código modificado)                         │
└─────────────────────────────────────────────────────────────┘

Neo4j después de Ingesta #2:

├── Version v1 [isCurrent: false, timestamp: t1]
│   └── CONTAINS
│       ├── Class "Product" [validFrom: t1, validTo: t2] ← CERRADO
│       ├── Method "GetPrice" [validFrom: t1, validTo: t2] ← CERRADO
│       ├── CodeNode "class:Product" [validFrom: t1, validTo: t2, embedding: v1] ← CERRADO ✅
│       └── CodeNode "method:GetPrice" [validFrom: t1, validTo: t2, embedding: v1] ← CERRADO ✅
│
├── Version v2 [isCurrent: true, timestamp: t2]
│   └── CONTAINS
│       ├── Class "Product" [validFrom: t2, validTo: NULL] ← NUEVO
│       ├── Method "GetPrice" [validFrom: t2, validTo: NULL] ← NUEVO
│       ├── CodeNode "class:Product" [validFrom: t2, validTo: NULL, embedding: v2] ← NUEVO ✅
│       └── CodeNode "method:GetPrice" [validFrom: t2, validTo: NULL, embedding: v2] ← NUEVO ✅
│
└── Vector Index (indexa TODOS los CodeNodes de TODAS las versiones)

    ✅ Ambos embeddings preservados!


┌─────────────────────────────────────────────────────────────┐
│          TIME TRAVEL: Consulta histórica en t1              │
└─────────────────────────────────────────────────────────────┘

Query Grafo:
  MATCH (c:Class)
  WHERE c.validFrom <= t1 AND (c.validTo IS NULL OR c.validTo > t1)
  RETURN c

  ✅ Devuelve: Class "Product" v1 (código de t1)

Query Vector (CON FILTRO TEMPORAL):
  CALL db.index.vector.queryNodes(...)
  YIELD node
  WHERE node.validFrom <= t1 
    AND (node.validTo IS NULL OR node.validTo > t1)
  RETURN node

  ✅ Devuelve: CodeNode con embedding v1 (código de t1)

✅ CONSISTENTE: Grafo de t1, embedding de t1!
```

---

## **FLUJO DE BÚSQUEDA GRAPHRAG**

### **ANTES (Incorrecto)** ❌

```
Usuario: "¿Cómo funcionaba el pago hace 2 días?"

┌─────────────────────────────────────────────────────────────┐
│ 1. VECTOR SEARCH                                            │
│    queryEmbedding = embed("pago")                           │
│    results = vectorIndex.SearchAsync(queryEmbedding)        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Neo4j devuelve:                                             │
│   CodeNode "PaymentService.Process"                         │
│   embedding: [0.8, 0.9, ...] ← VERSIÓN ACTUAL (hoy)        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. GRAPH EXPANSION                                          │
│    timestamp = hace_2_dias                                  │
│    graph = GetGraphAtTimestampAsync(repoId, timestamp)      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Neo4j devuelve:                                             │
│   Method "Process" [versión de hace 2 días]                │
│   Dependencies de hace 2 días                               │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. COMBINAR RESULTADOS                                      │
│    ❌ PROBLEMA:                                             │
│    • Embedding de HOY                                       │
│    • Código de HACE 2 DÍAS                                  │
│    • INCONSISTENTE!                                         │
└─────────────────────────────────────────────────────────────┘
```

### **DESPUÉS (Correcto)** ✅

```
Usuario: "¿Cómo funcionaba el pago hace 2 días?"

┌─────────────────────────────────────────────────────────────┐
│ 1. VECTOR SEARCH (CON TIMESTAMP)                            │
│    queryEmbedding = embed("pago")                           │
│    timestamp = hace_2_dias                                  │
│    results = vectorIndex.SearchAsync(queryEmbedding,        │
│                                      timestamp: timestamp)  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Neo4j ejecuta:                                              │
│   CALL db.index.vector.queryNodes(...)                      │
│   YIELD node, score                                         │
│   WHERE node.validFrom <= timestamp                         │
│     AND (node.validTo IS NULL OR node.validTo > timestamp) │
│                                                             │
│ Devuelve:                                                   │
│   CodeNode "PaymentService.Process"                         │
│   embedding: [0.5, 0.6, ...] ← VERSIÓN DE HACE 2 DÍAS ✅   │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. GRAPH EXPANSION (MISMO TIMESTAMP)                        │
│    graph = GetGraphAtTimestampAsync(repoId, timestamp)      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ Neo4j devuelve:                                             │
│   Method "Process" [versión de hace 2 días] ✅              │
│   Dependencies de hace 2 días ✅                            │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. COMBINAR RESULTADOS                                      │
│    ✅ CONSISTENTE:                                          │
│    • Embedding de HACE 2 DÍAS                               │
│    • Código de HACE 2 DÍAS                                  │
│    • Dependencias de HACE 2 DÍAS                            │
│    • TODO EN SINCRONÍA!                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## **COMPARACIÓN: MERGE vs CREATE**

### **Implementación ACTUAL (MERGE - Incorrecto)**

```cypher
// Cada ingesta SOBRESCRIBE el mismo nodo
MERGE (n:CodeNode {id: "class:Product"})
SET n.embedding = $newEmbedding,
    n.content = $newContent,
    n.lastUpdated = datetime()

Resultado:
  t1: CodeNode {id: "class:Product", embedding: v1}
  t2: CodeNode {id: "class:Product", embedding: v2} ← SOBRESCRITO
  // v1 se perdió ❌
```

### **Implementación CORRECTA (CREATE - Versionado)**

```cypher
// Cada ingesta CREA un nuevo nodo
// Paso 1: Cerrar versión anterior
MATCH (prev:CodeNode {id: "class:Product", repoId: $repoId})
WHERE prev.validTo IS NULL
SET prev.validTo = $timestamp

// Paso 2: Crear nueva versión
CREATE (n:CodeNode {
    id: "class:Product",
    versionId: $versionId,
    repoId: $repoId,
    validFrom: $timestamp,
    validTo: null,
    embedding: $newEmbedding,
    content: $newContent
})

// Paso 3: Enlazar versiones
MATCH (prev:CodeNode {id: "class:Product", validTo: $timestamp})
MERGE (prev)-[:NEXT_VERSION]->(n)

Resultado:
  t1: CodeNode {id: "class:Product", embedding: v1, validTo: t2}
  t2: CodeNode {id: "class:Product", embedding: v2, validTo: NULL}
  // Ambas versiones preservadas ✅
```

---

## **ESPACIO EN NEO4J**

### **¿Cuánto espacio adicional requiere?**

**Por cada versión:**
- Nodos de grafo: ~1KB por nodo
- Embeddings: ~6KB por embedding (1536 floats × 4 bytes)

**Ejemplo: Proyecto con 1000 clases, 10 versiones:**
- Grafo: 1000 × 10 × 1KB = 10 MB
- Vectores: 1000 × 10 × 6KB = 60 MB
- **Total: ~70 MB** (manejable)

**Para optimizar:**
1. Limpieza periódica (versiones >90 días)
2. Compresión de embeddings antiguos
3. Snapshots semanales en lugar de cada commit

---

## **RESUMEN VISUAL**

```
PREGUNTA: "¿Cómo funcionaba X hace N días?"

┌──────────────────────┐
│ Estado ACTUAL (malo) │
└──────────────────────┘
Vector Search → Devuelve embedding de HOY ❌
Graph Lookup  → Devuelve código de HACE N DÍAS ✅
Resultado: INCONSISTENTE 🔴

┌──────────────────────┐
│ Estado CORRECTO      │
└──────────────────────┘
Vector Search → Devuelve embedding de HACE N DÍAS ✅
Graph Lookup  → Devuelve código de HACE N DÍAS ✅
Resultado: CONSISTENTE ✅
```

---

**Conclusión:**

🚨 El problema es **REAL y CRÍTICO**  
✅ La solución es **CLARA y VIABLE**  
📝 Requiere modificar ~150 líneas de código  
⏱️ Tiempo estimado de implementación: 2-3 horas  

**¿Quieres que proceda con el fix?** 🚀
