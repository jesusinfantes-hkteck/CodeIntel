# Neo4j GraphRAG Architecture

## Overview

**AriadnaKnowledgeStore** now uses **Neo4j as a unified Vector Graph Database** for both:
- 🔗 **Graph relationships** (classes, methods, calls, dependencies)
- 🧠 **Vector embeddings** (semantic code search)

This eliminates the "two-database problem" and enables **GraphRAG** (Graph Retrieval-Augmented Generation).

---

## Why Neo4j for Both?

### ❌ Old Architecture (Dual Database)
```
User Query
    ↓
Azure Search (vector) → Top-K code chunks
    ↓
Extract IDs → Query Neo4j (graph) → Get related entities
    ↓
Merge results → Return to LLM
```

**Problems:**
- Two databases to sync
- Extra network hop
- Complex ID mapping
- Consistency issues

### ✅ New Architecture (Single Database)
```
User Query
    ↓
Neo4j Vector Search → Top-K similar code
    ↓  (same transaction)
Neo4j Graph Traversal → Related entities (1-2 hops)
    ↓
Return enriched context → LLM
```

**Benefits:**
- ✅ Atomic consistency
- ✅ Single transaction
- ✅ Reduced latency
- ✅ Simpler architecture

---

## How It Works

### 1️⃣ Vector Index Creation

```cypher
CREATE VECTOR INDEX code_embeddings IF NOT EXISTS
FOR (n:CodeNode)
ON (n.embedding)
OPTIONS {indexConfig: {
  `vector.dimensions`: 1536,
  `vector.similarity_function`: 'cosine'
}}
```

### 2️⃣ Vector + Graph Query (GraphRAG)

```cypher
// Find top-5 similar methods and their dependencies
CALL db.index.vector.queryNodes('code_embeddings', 5, $userPromptEmbedding)
YIELD node AS startNode, score

// Traverse to related entities
MATCH (startNode)-[:REPRESENTS]->(method:Method)
OPTIONAL MATCH (method)-[:CALLS|DEPENDS_ON*1..2]-(related)

RETURN 
  startNode.content as code,
  score,
  method.name as methodName,
  collect(related.name) as relatedEntities
ORDER BY score DESC
```

### 3️⃣ Implementation in C#

```csharp
// Neo4jVectorIndex.cs

// Standard vector search
var results = await vectorIndex.SearchAsync(queryEmbedding, topK: 5);

// GraphRAG: Vector + Graph traversal
var graphRAGResults = await vectorIndex.GraphRAGSearchAsync(
    queryEmbedding, 
    topK: 5, 
    maxHops: 2  // Traverse 1-2 levels of relationships
);

// Returns:
// - Top-5 semantically similar code chunks
// - Their parent classes/methods
// - Dependencies, callers, inherited classes
// - Relationship types (CALLS, DEPENDS_ON, etc.)
```

---

## Configuration

### appsettings.json

```json
{
  "GraphStore": {
    "Type": "Neo4jVersioned"
  },
  "Neo4j": {
    "Uri": "neo4j+s://your-aura-instance.databases.neo4j.io",
    "User": "neo4j",
    "Password": "your-password"
  }
}
```

**Note:** Azure Search configuration section has been **removed** ✅

---

## Workflow Example

### Ingestion

```
GitHub Repo
    ↓
Roslyn Analysis → Extract classes/methods/calls
    ↓
Neo4jVersionedGraphStore → Store graph (nodes + edges)
    ↓
CodeChunker → Split code into chunks
    ↓
AzureOpenAI → Generate embeddings (1536-dim)
    ↓
Neo4jVectorIndex → Store embeddings on CodeNode.embedding
```

### Query (GraphRAG)

```
User: "Show me authentication methods in the API layer"
    ↓
Embed prompt → [0.123, -0.456, ...]
    ↓
Neo4j Vector Search → Top-5 similar methods
    ↓  (same query)
Graph Traversal → Find:
  - Parent classes
  - Called methods
  - Dependencies
  - Inherited classes
    ↓
Return enriched context → LLM generates answer
```

---

## Key Advantages

### 1. **Atomic Consistency**
When you update a code entity, both its properties and embedding are updated in the **same transaction**.

### 2. **Reduced Latency**
No network hop between vector store and graph store.

### 3. **Rich Context**
Instead of returning isolated code chunks, you get:
- The matching code
- Its parent class/namespace
- Methods it calls
- Methods that call it
- Dependencies it uses

### 4. **Graph Filtering**
Combine vector search with graph constraints:

```cypher
// Find similar code, but only in Legacy.Namespace
CALL db.index.vector.queryNodes('code_embeddings', 10, $embedding)
YIELD node, score
MATCH (node)-[:REPRESENTS]->(m:Method)-[:HAS_METHOD]-(c:Class)
WHERE c.namespace STARTS WITH 'Legacy.Namespace'
RETURN node, score, c.name
```

---

## Comparison: Neo4j vs. Azure Search

| Feature | Azure Search | Neo4j |
|---------|-------------|--------|
| Vector Search | ✅ Optimized | ✅ Native (`db.index.vector`) |
| Graph Relationships | ❌ None | ✅ Native Cypher |
| Hybrid Search | 🟡 Manual merge | ✅ Single query |
| Consistency | 🟡 Eventual | ✅ ACID transactions |
| Filtering | 🟡 Basic | ✅ Rich graph patterns |
| Latency | 🟡 Two calls | ✅ One query |
| Cost | 💰 Separate service | 💰 Single database |

---

## Next Steps

### Enhance GraphRAG Queries

Add business logic filters:

```csharp
// Find authentication methods that call external APIs
var results = await vectorIndex.GraphRAGSearchAsync(
    queryEmbedding, 
    topK: 5, 
    maxHops: 2,
    filter: "MATCH (method)-[:CALLS]->(external:Method) WHERE external.namespace STARTS WITH 'System.Net'"
);
```

### Add Caching

Cache frequent queries:

```csharp
// Warm up common searches
await vectorIndex.WarmUpCacheAsync(new[] {
    "authentication",
    "database connection",
    "API endpoint"
});
```

### Monitoring

Track vector search performance:

```cypher
// Check index usage
SHOW INDEXES YIELD name, type, state, populationPercent
WHERE type = 'VECTOR'
```

---

## References

- [Neo4j Vector Search Documentation](https://neo4j.com/docs/cypher-manual/current/indexes-for-vector-search/)
- [GraphRAG Pattern](https://neo4j.com/developer/graph-data-science/graph-algorithms/)
- [Neo4j AuraDB (Cloud)](https://console.neo4j.io)

---

## Summary

By using **Neo4j for both graph and vector**, AriadnaKnowledgeStore can:
1. Perform semantic search (vector similarity)
2. Immediately traverse to related entities (graph)
3. Return rich, contextual code subsets to the LLM
4. All in **one query**, **one transaction**, **one database** ✅
