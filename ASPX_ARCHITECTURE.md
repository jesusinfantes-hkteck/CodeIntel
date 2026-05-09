# Arquitectura de Soporte ASPX en CodeIntel

## 🏗️ Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub Repository                             │
│                    (Legacy .NET Framework App)                       │
└────────────┬────────────────────────────────────────────────────────┘
             │
             │ Clone/Download
             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     IGitHubSource                                    │
│                  (OctokitGitHubSource)                              │
└────────────┬────────────────────────────────────────────────────────┘
             │
             │ Local Path
             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      ICodeAnalyzer                                   │
│                    (RoslynAnalyzer)                                  │
│                                                                      │
│  ┌──────────────────────┐        ┌──────────────────────┐         │
│  │   C# Analysis        │        │   ASPX Analysis      │ ✨ NEW  │
│  │                      │        │                      │         │
│  │ • Parse .cs files    │        │ • Parse .aspx/.ascx  │         │
│  │ • Extract classes    │        │ • Extract @Page/@Control         │
│  │ • Extract methods    │        │ • Extract controls   │         │
│  │ • Build call graph   │        │ • Extract events     │         │
│  │                      │        │ • Map code-behind    │         │
│  └──────────┬───────────┘        └──────────┬───────────┘         │
│             │                               │                      │
│             └───────────┬───────────────────┘                      │
│                         │                                          │
│                         ▼                                          │
│              ┌─────────────────────┐                              │
│              │     GraphModel      │ ✨ UPDATED                   │
│              │                     │                              │
│              │ • Classes           │                              │
│              │ • Methods           │                              │
│              │ • Edges             │                              │
│              │ • AspxPages     ✨  │                              │
│              │ • AspxControls  ✨  │                              │
│              │ • AspxEvents    ✨  │                              │
│              └─────────┬───────────┘                              │
└────────────────────────┼──────────────────────────────────────────┘
                         │
                         ├──────────────────┬─────────────────────┐
                         ▼                  ▼                     ▼
           ┌─────────────────────┐  ┌──────────────┐  ┌──────────────────┐
           │   IGraphStore       │  │CodeChunker   │  │ IVectorIndex     │
           │                     │  │              │  │                  │
           │ Neo4j / Gremlin     │  │ ✨ UPDATED   │  │ Neo4j Vectors    │
           │                     │  │              │  │                  │
           │ ✨ NEW Node Types:  │  │ Generates:   │  │ Indexes:         │
           │  • AspxPage         │  │  • aspx_page │  │  • All chunks    │
           │  • AspxControl      │  │  • aspx_ctrl │  │  • Semantic      │
           │  • AspxEvent        │  │  • aspx_evt  │  │    search        │
           │                     │  │              │  │                  │
           │ ✨ NEW Relationships│  │              │  │                  │
           │  • CODE_BEHIND      │  │              │  │                  │
           │  • HAS_CONTROL      │  │              │  │                  │
           │  • TRIGGERS         │  │              │  │                  │
           │  • HANDLES_EVENT    │  │              │  │                  │
           └─────────────────────┘  └──────────────┘  └──────────────────┘
```

## 🔄 Flujo de Datos Detallado

### 1️⃣ Ingesta de Archivo ASPX

```
Default.aspx
    │
    ▼
AspxAnalyzer.AnalyzeAsync()
    │
    ├─► ExtractDirective()
    │   └─► Regex: <%@ Page ... %>
    │       └─► Parse: Inherits, CodeBehind
    │
    ├─► ParseControls()
    │   └─► HtmlAgilityPack
    │       └─► Find: runat="server"
    │           └─► Extract: ID, Type, Events
    │
    └─► AspxAnalysisResult
        ├─► AspxPage
        ├─► List<AspxControl>
        ├─► List<AspxEvent>
        └─► List<CodeEdge>
```

### 2️⃣ Integración con Análisis C#

```
RoslynAnalyzer.AnalyzeAsync()
    │
    ├─► Scan *.cs files
    │   └─► Parse classes & methods
    │
    ├─► Scan *.aspx/*.ascx files  ✨ NEW
    │   └─► Call AspxAnalyzer
    │       └─► Get ASPX elements
    │
    └─► Consolidate
        └─► GraphModel (complete)
```

### 3️⃣ Generación de Graph

```
GraphModel
    │
    ▼
Neo4jGraphStore.UpsertAsync()
    │
    ├─► CREATE Repository node
    │
    ├─► CREATE Class nodes
    │   └─► MERGE (Repository)-[:CONTAINS]->(Class)
    │
    ├─► CREATE Method nodes
    │   └─► MERGE (Class)-[:HAS_METHOD]->(Method)
    │
    ├─► ✨ CREATE AspxPage nodes
    │   ├─► MERGE (Repository)-[:CONTAINS]->(AspxPage)
    │   └─► MERGE (AspxPage)-[:CODE_BEHIND]->(Class)
    │
    ├─► ✨ CREATE AspxControl nodes
    │   └─► MERGE (AspxPage)-[:HAS_CONTROL]->(AspxControl)
    │
    ├─► ✨ CREATE AspxEvent nodes
    │   ├─► MERGE (AspxControl)-[:TRIGGERS]->(AspxEvent)
    │   └─► MERGE (AspxEvent)-[:HANDLES_EVENT]->(Method)
    │
    └─► CREATE all other edges
        (Calls, DependsOn, Inherits, etc.)
```

### 4️⃣ Generación de Embeddings

```
GraphModel
    │
    ▼
CodeChunker.ToVectorDocs()
    │
    ├─► For each Class
    │   └─► Generate class chunk
    │
    ├─► For each Method
    │   └─► Generate method chunk
    │
    ├─► ✨ For each AspxPage
    │   └─► Generate page chunk
    │       (includes CodeBehind info)
    │
    ├─► ✨ For each AspxControl
    │   └─► Generate control chunk
    │       (includes events info)
    │
    └─► ✨ For each AspxEvent
        └─► Generate event chunk
            (includes handler method)
```

## 🎯 Modelo de Relaciones

```
┌──────────────┐
│ Repository   │
└──────┬───────┘
       │ CONTAINS
       ├────────────────────┬─────────────────┐
       │                    │                 │
       ▼                    ▼                 ▼
┌──────────┐         ┌──────────┐     ┌──────────┐
│  Class   │         │ AspxPage │     │ AspxPage │
│          │◄────────┤          │     │          │
│          │CODE_BEHIND         │     │          │
└────┬─────┘         └────┬─────┘     └────┬─────┘
     │                    │                 │
     │HAS_METHOD          │HAS_CONTROL      │HAS_CONTROL
     │                    │                 │
     ▼                    ▼                 ▼
┌──────────┐      ┌───────────────┐  ┌───────────────┐
│  Method  │      │ AspxControl   │  │ AspxControl   │
│          │      │  (btnSubmit)  │  │   (gvUsers)   │
│          │      └───────┬───────┘  └───────┬───────┘
│          │              │                  │
│          │              │TRIGGERS          │TRIGGERS
│          │              │                  ├─────────────┐
│          │              ▼                  ▼             ▼
│          │      ┌─────────────┐    ┌─────────────┐ ┌─────────────┐
│          │      │ AspxEvent   │    │ AspxEvent   │ │ AspxEvent   │
│          │      │  OnClick    │    │OnRowDataBound│ │OnSelected...│
│          │      └──────┬──────┘    └──────┬──────┘ └──────┬──────┘
│          │             │                  │                │
│          │◄────────────┴──────────────────┴────────────────┘
│          │           HANDLES_EVENT
└──────────┘
```

## 🔍 Ejemplo de Consulta End-to-End

```
Usuario: "¿Qué pasa cuando hago clic en el botón Submit?"

    ↓

1️⃣ Neo4j Query:
   MATCH path = (btn:AspxControl {name: 'btnSubmit'})
                -[:TRIGGERS]->(event:AspxEvent)
                -[:HANDLES_EVENT]->(handler:Method)
                -[:CALLS*1..5]->(called:Method)
   RETURN path

    ↓

2️⃣ Graph Traversal:
   btnSubmit → OnClick → btnSubmit_Click → SaveData → DataAccess.Insert

    ↓

3️⃣ Respuesta:
   "El botón Submit ejecuta btnSubmit_Click, que llama a SaveData,
    que a su vez invoca DataAccess.Insert para persistir los datos."
```

## 🛠️ Tecnologías Utilizadas

| Componente | Tecnología | Propósito |
|------------|------------|-----------|
| **Parser ASPX** | Regex + HtmlAgilityPack | Extraer directivas y controles |
| **Parser C#** | Roslyn | Análisis sintáctico de code-behind |
| **Graph Store** | Neo4j / Gremlin | Almacenar grafo de código |
| **Vector Index** | Neo4j Vector Search | Búsqueda semántica |
| **Embeddings** | Azure OpenAI | Vectorización de texto |
| **Orquestación** | Azure Functions | Procesamiento serverless |

## 📊 Estadísticas de Nodos y Relaciones

Para un proyecto legacy típico (ej: 50 páginas ASPX):

```
Nodos:
  Repository:      1
  Classes:        50-100
  Methods:       300-600
  AspxPages:      50-80    ✨
  AspxControls:  200-400   ✨
  AspxEvents:    150-300   ✨

Relaciones:
  CONTAINS:       130-180
  HAS_METHOD:     300-600
  CODE_BEHIND:     50-80   ✨
  HAS_CONTROL:    200-400   ✨
  TRIGGERS:       150-300   ✨
  HANDLES_EVENT:  150-300   ✨
  CALLS:          500-1000
  DEPENDS_ON:     300-600

Total Nodos:    ~700-1480
Total Edges:    ~1780-3460
```

## 🎨 Visualización en Neo4j Browser

### Vista de Página Individual
```cypher
MATCH path = (page:AspxPage {name: 'Default.aspx'})
             -[*1..2]->()
RETURN path
LIMIT 30
```

**Resultado Visual:**
- Nodo central: Default.aspx (verde)
- Nodos conectados: Controles (azul)
- Nodos eventos: (naranja)
- Nodo clase: DefaultPage (rojo)
- Nodos métodos: (amarillo)

### Vista de Arquitectura Global
```cypher
MATCH (r:Repository)-[:CONTAINS]->(page:AspxPage)
WITH page
MATCH (page)-[:CODE_BEHIND]->(class:Class)
RETURN page.name, class.name
LIMIT 50
```

## 🚀 Performance y Escalabilidad

| Métrica | Valor | Notas |
|---------|-------|-------|
| Análisis por archivo ASPX | ~50-200ms | Depende de tamaño |
| Análisis por proyecto (50 páginas) | ~5-15 seg | Paralelo |
| Ingesta completa | ~30-60 seg | Incluye embeddings |
| Consultas Cypher | ~10-100ms | Con índices |
| Búsqueda vectorial | ~20-50ms | Top 10 resultados |

### Optimizaciones Implementadas
- ✅ Análisis paralelo de archivos
- ✅ Uso de `ConcurrentBag<T>` para thread-safety
- ✅ Exclusión de archivos generados (.designer.cs)
- ✅ Manejo robusto de errores (no bloquea por 1 archivo)
- ✅ Batching de operaciones Neo4j

---

**Documento técnico de arquitectura**  
*Generado: 2026-05-08*
