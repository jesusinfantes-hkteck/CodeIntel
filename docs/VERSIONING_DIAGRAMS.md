# 📊 Versionado Temporal - Diagramas Visuales

Este documento complementa `VERSIONING_SYSTEM_EXPLAINED.md` con diagramas visuales.

---

## 🌳 **Diagrama 1: Árbol de Versiones**

```
Repository: acme/shop@main
│
├── 📦 Version 1 [isCurrent: false]
│   ├── Timestamp: 2024-01-01 10:00:00
│   ├── CommitHash: abc123
│   └── CONTAINS →
│       ├── Class: Product [validFrom: t1, validTo: t2]
│       └── Class: Order   [validFrom: t1, validTo: t2]
│
├── 📦 Version 2 [isCurrent: false]
│   ├── Timestamp: 2024-01-02 15:30:00
│   ├── CommitHash: def456
│   └── CONTAINS →
│       ├── Class: Product [validFrom: t2, validTo: t3] ←─┐
│       ├── Class: Order   [validFrom: t2, validTo: t3]    │ NEXT_VERSION
│       └── Class: Payment [validFrom: t2, validTo: t3]    │
│                                                           │
└── 📦 Version 3 [isCurrent: true] ⭐ ACTUAL               │
    ├── Timestamp: 2024-01-03 09:00:00                     │
    ├── CommitHash: ghi789                                 │
    └── CONTAINS →                                         │
        ├── Class: Product [validFrom: t3, validTo: NULL] ─┘
        └── Class: Payment [validFrom: t3, validTo: NULL]
        // Order eliminado en esta versión
```

---

## 🔄 **Diagrama 2: Flujo de Ingesta con Versionado**

```
┌─────────────────────────────────────────────────────────────┐
│ 1. PUSH A GITHUB                                            │
│    Commit: ghi789                                           │
│    Mensaje: "Refactor payment module"                      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. WEBHOOK TRIGGER                                          │
│    GitHub → Azure Function                                  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. DESCARGAR CÓDIGO                                         │
│    OctokitGitHubSource.DownloadRepositoryAsync()           │
│    → C:\temp\AriadnaKnowledgeStore\{guid}\                 │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. ANALIZAR CÓDIGO                                          │
│    RoslynAnalyzer.AnalyzeAsync()                           │
│    → GraphModel (clases, métodos, componentes)            │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. CREAR NUEVA VERSIÓN EN NEO4J                            │
│    Neo4jVersionedGraphStore.UpsertAsync()                  │
│                                                             │
│    A. Crear nodo Version                                   │
│       versionId: {new-guid}                                │
│       commitHash: "ghi789"                                 │
│       timestamp: {now}                                     │
│       isCurrent: true                                      │
│                                                             │
│    B. Marcar versión anterior como no-current              │
│       SET oldVersion.isCurrent = false                     │
│                                                             │
│    C. Cerrar nodos anteriores (soft delete)                │
│       SET Class.validTo = {now}                            │
│                                                             │
│    D. Crear nuevos nodos con versionado                    │
│       CREATE (c:Class {validFrom: {now}, validTo: NULL})  │
│                                                             │
│    E. Enlazar versiones (historial)                        │
│       (prevClass)-[:NEXT_VERSION]->(newClass)             │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. INDEXACIÓN VECTORIAL                                    │
│    CodeChunker → AzureOpenAI → Neo4j Vector Index         │
└─────────────────────────────────────────────────────────────┘
```

---

## ⏰ **Diagrama 3: Timeline de Versionado**

```
Tiempo →

t1: 10:00 ────────┬────────────────────────────────────────────
               Ingesta #1                                      
               Version 1 creada                                
               │                                               
               ├─ Product.cs ────────────────────────────────►
               │  [validFrom: t1, validTo: t2]                
               │                                               
               └─ Order.cs ──────────────────────────────────►
                  [validFrom: t1, validTo: t2]                

t2: 15:30 ────────┬────────────────────────────────────────────
               Ingesta #2                                      
               Version 2 creada                                
               │                                               
               ├─ Product.cs ────────────────────────────────►
               │  [validFrom: t2, validTo: t3]                
               │                                               
               ├─ Order.cs ──────────────────────────────────►
               │  [validFrom: t2, validTo: t3]                
               │                                               
               └─ Payment.cs (NUEVO) ────────────────────────►
                  [validFrom: t2, validTo: t3]                

t3: 09:00 ────────┬────────────────────────────────────────────
               Ingesta #3                                      
               Version 3 creada (ACTUAL)                       
               │                                               
               ├─ Product.cs ────────────────────────────────►
               │  [validFrom: t3, validTo: NULL] ⭐          
               │                                               
               ├─ Order.cs (ELIMINADO)                        
               │  [no existe en v3]                           
               │                                               
               └─ Payment.cs ────────────────────────────────►
                  [validFrom: t3, validTo: NULL] ⭐          

Leyenda:
  ──► : Nodo existe
  ⭐  : Versión actual (validTo: NULL)
```

---

## 🔍 **Diagrama 4: Query - Consulta por Timestamp**

### **Pregunta: ¿Qué clases existían en t2 (15:30)?**

```
Query: GetGraphAtTimestampAsync(repoId, t2, ct)

Neo4j busca:
┌──────────────────────────────────────────────────┐
│ MATCH (c:Class {repoId: "acme/shop@main"})      │
│ WHERE c.validFrom <= t2                          │
│   AND (c.validTo IS NULL OR c.validTo > t2)     │
│ RETURN c                                         │
└──────────────────────────────────────────────────┘

Análisis por clase:

Product v1:
  validFrom: t1 (10:00) ✅ <= t2
  validTo: t2 (15:30)   ❌ NO > t2
  → NO devuelto

Product v2:
  validFrom: t2 (15:30) ✅ <= t2
  validTo: t3 (09:00)   ✅ > t2
  → ✅ DEVUELTO

Order v1:
  validFrom: t1         ✅ <= t2
  validTo: t2           ❌ NO > t2
  → NO devuelto

Order v2:
  validFrom: t2         ✅ <= t2
  validTo: t3           ✅ > t2
  → ✅ DEVUELTO

Payment v2:
  validFrom: t2         ✅ <= t2
  validTo: t3           ✅ > t2
  → ✅ DEVUELTO

Payment v3:
  validFrom: t3         ❌ NO <= t2
  → NO devuelto

Resultado:
  • Product (v2)
  • Order (v2)
  • Payment (v2)
```

---

## 🔙 **Diagrama 5: Rollback - Antes y Después**

### **Antes del Rollback:**

```
Repository: acme/shop@main
│
├── Version 1 [isCurrent: false]
│
├── Version 2 [isCurrent: false]  ← Queremos volver aquí
│
└── Version 3 [isCurrent: true] ⭐ ← Versión actual (problemática)

Queries actuales devuelven:
  WHERE validTo IS NULL → Nodos de v3
```

### **Después del Rollback:**

```
RollbackToVersionAsync("acme/shop@main", "version-2-guid", ct)

┌──────────────────────────────────────────────────┐
│ MATCH (v2:Version {id: "version-2-guid"})       │
│ SET v2.isCurrent = true                          │
│                                                   │
│ MATCH (v3:Version {isCurrent: true})            │
│ WHERE v3.id <> "version-2-guid"                  │
│ SET v3.isCurrent = false                         │
└──────────────────────────────────────────────────┘

Repository: acme/shop@main
│
├── Version 1 [isCurrent: false]
│
├── Version 2 [isCurrent: true] ⭐ ← Ahora es la actual
│
└── Version 3 [isCurrent: false]  ← Ya no es actual (pero existe)

Queries actuales ahora devuelven:
  WHERE validTo IS NULL → Nodos de v2

IMPORTANTE:
  ✅ Version 3 NO fue eliminada
  ✅ Puedes volver a v3 cuando quieras
  ✅ Solo cambió el flag "isCurrent"
```

---

## 🔗 **Diagrama 6: Cadena de Versiones (NEXT_VERSION)**

```
Product.cs - Historial completo:

v1 ──[NEXT_VERSION]──► v2 ──[NEXT_VERSION]──► v3
│                       │                       │
│                       │                       │
class Product {        class Product {        class Product {
  decimal Price;         decimal Price;         decimal Price;
}                        string SKU;            string SKU;
                       }                        string Barcode;
                                               }

validFrom: t1          validFrom: t2          validFrom: t3
validTo: t2            validTo: t3            validTo: NULL ⭐

Puedes navegar:
  • Forward: v1 → v2 → v3 (ver evolución)
  • Backward: v3 → v2 → v1 (rastrear origen)

Query para ver historial completo:
┌──────────────────────────────────────────────────┐
│ MATCH (c:Class {id: "class:Product"})           │
│ OPTIONAL MATCH (c)-[:NEXT_VERSION*]->(newer)    │
│ OPTIONAL MATCH (older)-[:NEXT_VERSION*]->(c)    │
│ RETURN older, c, newer                           │
│ ORDER BY c.validFrom                             │
└──────────────────────────────────────────────────┘
```

---

## 📊 **Diagrama 7: GraphRAG con Versionado**

```
┌────────────────────────────────────────────────────────────┐
│ Usuario: "¿Cómo funcionaba el pago hace 2 días?"         │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ 1. IDENTIFICAR TIMESTAMP                                   │
│    "hace 2 días" → timestamp: 1704067200                  │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ 2. VECTOR SEARCH (en índice completo)                     │
│    Query: "payment processing"                            │
│    Resultado: Encuentra chunks de todas las versiones    │
│      • Payment v1 (similarity: 0.88)                      │
│      • Payment v2 (similarity: 0.91) ⭐                   │
│      • Payment v3 (similarity: 0.85)                      │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ 3. FILTRAR POR TIMESTAMP                                   │
│    GetGraphAtTimestampAsync(repoId, 1704067200, ct)       │
│    Solo devuelve nodos válidos en ese momento             │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ 4. GRAPH EXPANSION (versión histórica)                    │
│    MATCH (bc:BlazorComponent {name: "Payment"})           │
│    WHERE bc.validFrom <= 1704067200                       │
│      AND (bc.validTo IS NULL OR bc.validTo > 1704067200) │
│    MATCH (bc)-[:USES_SERVICE]->(svc)                      │
│    WHERE svc.validFrom <= 1704067200...                   │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ 5. CONTEXTO ENRIQUECIDO                                    │
│    • Código de Payment v2 (hace 2 días)                   │
│    • Servicios inyectados en ese momento                  │
│    • Componentes relacionados en esa versión              │
│    • Cambios desde entonces (v2 → v3)                     │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ 6. RESPUESTA LLM                                           │
│    "Hace 2 días, el componente Payment usaba...           │
│     Desde entonces cambió: se añadió validación de CVV"   │
└────────────────────────────────────────────────────────────┘
```

---

## 🔄 **Diagrama 8: Comparación de Versiones**

```
Comparar: v2 (2024-01-02) vs v3 (2024-01-03)

┌─────────────────┬─────────────────┬─────────────────┐
│   Version 2     │   Cambio        │   Version 3     │
├─────────────────┼─────────────────┼─────────────────┤
│ Product.cs      │ ═══════════════►│ Product.cs      │
│ (sin cambios)   │                 │ (sin cambios)   │
├─────────────────┼─────────────────┼─────────────────┤
│ Order.cs        │ ────── X ───────│ (eliminado)     │
│                 │                 │                 │
├─────────────────┼─────────────────┼─────────────────┤
│ Payment.cs      │ ───── ✏️ ───────│ Payment.cs      │
│ (30 líneas)     │ modificado      │ (35 líneas)     │
├─────────────────┼─────────────────┼─────────────────┤
│                 │ ────── + ───────│ Validator.cs    │
│                 │ nuevo           │ (15 líneas)     │
└─────────────────┴─────────────────┴─────────────────┘

Query para detectar cambios:
┌──────────────────────────────────────────────────┐
│ // Clases eliminadas                             │
│ MATCH (v2:Version)-[:CONTAINS]->(c2:Class)      │
│ WHERE NOT EXISTS {                               │
│   MATCH (v3:Version)-[:CONTAINS]->              │
│         (:Class {id: c2.id})                     │
│ }                                                │
│ RETURN c2.name as deleted                        │
│                                                   │
│ // Clases nuevas                                 │
│ MATCH (v3:Version)-[:CONTAINS]->(c3:Class)      │
│ WHERE NOT EXISTS {                               │
│   MATCH (v2:Version)-[:CONTAINS]->              │
│         (:Class {id: c3.id})                     │
│ }                                                │
│ RETURN c3.name as added                          │
└──────────────────────────────────────────────────┘
```

---

## 🎯 **Diagrama 9: Flujo de Decisión - ¿Cuándo usar cada método?**

```
                    ┌─────────────────────┐
                    │ ¿Qué necesitas?     │
                    └──────────┬──────────┘
                               │
           ┌───────────────────┼───────────────────┐
           │                   │                   │
           ▼                   ▼                   ▼
    ┌──────────┐        ┌──────────┐        ┌──────────┐
    │ Ver todas│        │ Consultar│        │ Cambiar  │
    │ versiones│        │ historial│        │ versión  │
    │          │        │          │        │ actual   │
    └────┬─────┘        └────┬─────┘        └────┬─────┘
         │                   │                    │
         ▼                   ▼                    ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│ GetVersionHistory│ │GetGraphAtTimestamp│ │RollbackToVersion│
│ Async()          │ │Async()            │ │Async()           │
└──────────────────┘ └──────────────────┘ └──────────────────┘
         │                   │                    │
         ▼                   ▼                    ▼
    Devuelve:           Devuelve:            Efecto:
    List<VersionInfo>   GraphModel           Cambia isCurrent

    Uso:                Uso:                 Uso:
    • Listar cambios    • Time travel        • Rollback
    • Ver commits       • Auditoría          • Restauración
    • Buscar fecha      • Comparación        • Deployment
```

---

## 🚀 **Diagrama 10: Arquitectura Completa con Versionado**

```
┌─────────────────────────────────────────────────────────────┐
│                        GITHUB                               │
│  Commit: abc123 → Commit: def456 → Commit: ghi789         │
└────────────┬────────────────────────────────────────────────┘
             │ Webhook
             ▼
┌─────────────────────────────────────────────────────────────┐
│                   AZURE FUNCTIONS                           │
│  • GitHubWebhookFunction                                    │
│  • IngestFunction                                           │
└────────────┬────────────────────────────────────────────────┘
             │
    ┌────────┴────────┐
    │                 │
    ▼                 ▼
┌─────────┐     ┌──────────────────┐
│ GitHub  │     │ Code Analysis    │
│ Source  │     │ • RoslynAnalyzer │
└────┬────┘     │ • AspxAnalyzer   │
     │          │ • RazorAnalyzer  │
     │          │ • BlazorAnalyzer │
     │          └────────┬─────────┘
     │                   │
     └────────┬──────────┘
              │ GraphModel
              ▼
┌─────────────────────────────────────────────────────────────┐
│              NEO4J VERSIONED GRAPH STORE                    │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Repository                                          │  │
│  │   ├── Version 1 [v1-guid] [isCurrent: false]       │  │
│  │   │     └── CONTAINS → Classes, Methods, Blazor... │  │
│  │   ├── Version 2 [v2-guid] [isCurrent: false]       │  │
│  │   │     └── CONTAINS → Classes, Methods, Blazor... │  │
│  │   └── Version 3 [v3-guid] [isCurrent: true] ⭐     │  │
│  │         └── CONTAINS → Classes, Methods, Blazor... │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  Propiedades temporales:                                   │
│    • validFrom: timestamp inicio                           │
│    • validTo: timestamp fin (NULL = actual)                │
│    • versionId: enlace a snapshot                          │
│                                                             │
│  Relaciones de versionado:                                 │
│    • HAS_VERSION: Repository → Version                     │
│    • CONTAINS: Version → Nodos                             │
│    • NEXT_VERSION: Nodo anterior → Nodo nuevo              │
└────────────┬────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│                  NEO4J VECTOR INDEX                         │
│  (Contiene chunks de TODAS las versiones)                  │
│    • product_v1_chunk                                       │
│    • product_v2_chunk                                       │
│    • product_v3_chunk                                       │
└────────────┬────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│                      GRAPHRAG QUERY                         │
│  1. Vector Search → Encuentra chunks relevantes            │
│  2. Extract node ID → Identifica nodo en grafo             │
│  3. Check version → Filtra por validFrom/validTo           │
│  4. Graph Expansion → Navega dependencias versionadas      │
│  5. LLM Generation → Respuesta con contexto histórico      │
└─────────────────────────────────────────────────────────────┘
```

---

**Notas Finales:**

- Todos los diagramas representan el estado REAL del sistema implementado
- Los timestamps son ejemplos pero la lógica es exacta
- Puedes copiar los queries Cypher y ejecutarlos en Neo4j Browser
- Para más ejemplos de código, ver `VersioningExamples.cs`

---

**Creado:** 2024
**Versión:** 1.0
