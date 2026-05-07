# CodeIntel - El Mapa del Tesoro Escondido en Tu Código Legacy
## Discurso de Presentación - Knowledge Store

---

## INTRODUCCIÓN (2 min)

**¿Cuántos de ustedes tienen código legacy que lleva años, incluso décadas, corriendo en producción? ¿Código que nadie se atreve a tocar porque "si funciona, no lo toques"?**

El problema no es solo técnico. Es un problema de **conocimiento perdido**. Los desarrolladores que escribieron ese código se fueron. La documentación, si existió alguna vez, está desactualizada. Y cada día que pasa, ese código se convierte más en una caja negra que nadie entiende.

**¿El resultado?** Paralización. No pueden modernizar. No pueden migrar a la nube. No pueden aprovechar nuevas tecnologías porque **ni siquiera saben qué tienen**.

Hoy les presento **CodeIntel**: una solución que convierte ese código legacy incomprensible en un **Knowledge Store navegable e inteligente**, listo para ser consultado por agentes de IA.

---

## FASE 1: INGESTA - Conectando con las Fuentes de Conocimiento (3 min)

Miren el diagrama. Todo empieza con **fuentes de conocimiento**: repositorios de código, specs, bases de datos, reglas de desarrollo, y el conocimiento de expertos humanos.

### ¿Qué hace nuestro código?

Nuestro sistema se conecta directamente con **GitHub** usando la clase `OctokitGitHubSource`. Cuando recibimos una solicitud (`RepoRequest`), el sistema:

1. **Descarga automáticamente** el repositorio completo o una carpeta específica
2. Lo hace de forma **recursiva y filtrada** - ignora binarios grandes, se enfoca en el código fuente
3. Usa **autenticación segura** con tokens de GitHub

Pero aquí viene lo revolucionario: no solo guardamos archivos. Usamos **Roslyn**, el compilador de C# de Microsoft, para hacer un análisis **sintáctico y semántico profundo**. El `RoslynAnalyzer`:

- Extrae **cada clase**, **cada método**, **cada namespace**
- Identifica **relaciones de herencia** - ¿qué hereda de qué?
- Detecta **llamadas entre métodos** - ¿quién llama a quién?
- Descubre **dependencias** - ¿qué componentes dependen de otros?

**Esto es extracción de contenido bruto sin transformar**, tal como dice el slide. Pero no es un simple clon de repositorio. Es un análisis estructural profundo que captura la **arquitectura real** de tu código.

---

## FASE 2: NORMALIZACIÓN - Un Lenguaje Común para el Caos (3 min)

El segundo paso es crítico: **Normalización**.

Imaginen que tienen código de tres proyectos diferentes, escritos por equipos diferentes, en épocas diferentes. Algunos usan patrones de diseño, otros no. Algunos tienen documentación, otros están en español, otros en inglés.

### ¿Cómo procesamos todo esto uniformemente?

Nuestro modelo `GraphModel` convierte **todo** a una representación común:

- **CodeClass**: cada clase con su ID único, nombre, namespace y ubicación
- **CodeMethod**: cada método con su cuerpo, asociado a su clase
- **CodeEdge**: relaciones tipadas (`Calls`, `Inherits`, `DependsOn`, `Implements`)

El `CodeChunker` toma estos elementos y los convierte en **documentos estructurados para embeddings**. Por ejemplo, cada método se convierte en:

```
Namespace: [namespace]
Class: [clase]
Method: [método]
File: [ruta del archivo]

[cuerpo del código]
```

**Esto es lo que el motor de IA puede procesar uniformemente**, sin importar de dónde venga el código original.

---

## FASE 3: CORRELACIÓN - El Grafo de Conocimiento (3 min)

Aquí es donde la magia realmente sucede: **Correlación**.

Nuestro sistema identifica entidades, reglas y relaciones, y crea un **enlace cruzado entre las distintas fuentes**.

### ¿Cómo? Con Neo4j.

El `Neo4jGraphStore` construye un **grafo de conocimiento navegable**:

1. Crea un nodo `Repository` para cada repo analizado
2. Crea nodos `Class` conectados al repositorio con relaciones `CONTAINS`
3. Crea nodos `Method` conectados a sus clases con relaciones `HAS_METHOD`
4. Establece relaciones `CALLS`, `INHERITS`, `DEPENDS_ON` entre los elementos

### ¿Por qué es esto revolucionario?

Porque ahora puedes hacer preguntas como:

- "¿Qué clases heredan de esta clase base legacy?"
- "¿Qué métodos llaman a este procedimiento obsoleto?"
- "Si cambio esta clase, ¿qué otros componentes se verán afectados?"

**El grafo contiene las respuestas**, y es **navegable transversalmente** - puedes recorrerlo en cualquier dirección.

---

## FASE 4: CREACIÓN DEL KNOWLEDGE STORE - Vector Search + Graph Traversal (3 min)

El paso final: **Consolidación del conocimiento validado** en un Knowledge Store navegable y vectorizado.

Aquí operan **dos sistemas complementarios**:

### Knowledge Graph + Embeddings

Usamos `AzureOpenAIEmbeddingService` para generar **embeddings vectoriales** de cada fragmento de código. Esto permite:

- **Búsqueda semántica**: "Encuentra código que maneje autenticación JWT"
- **Similitud conceptual**: código que hace cosas parecidas aunque use nombres diferentes

El `Neo4jVectorIndex` o `AzureSearchVectorIndex` almacena estos vectores para **búsqueda ultrarrápida**.

### Vector Search + Graph Traversal

La combinación es poderosa:

1. **Vector Search** encuentra el punto de entrada relevante ("authentication logic")
2. **Graph Traversal** explora las relaciones desde ese punto (¿qué llama a esto? ¿qué hereda de esto?)

### Resultado: un agente de IA puede:

- Entender qué hace tu código legacy
- Navegar por las dependencias
- Recomendar estrategias de modernización
- Identificar código duplicado o patrones obsoletos
- Generar documentación automática

---

## VALIDACIÓN HUMANA Y ACTUALIZACIÓN INCREMENTAL (1 min)

Y algo crucial que muestra el slide: **Human in the loop - Validación**.

El sistema está diseñado para que **expertos humanos validen** el conocimiento extraído antes de consolidarlo.

Además, usando **webhooks**, el sistema puede actualizarse incrementalmente (delta) cada vez que haces un commit en GitHub. **No necesitas re-analizar todo el repositorio** - solo los cambios.

---

## CONCLUSIÓN: Del Miedo a la Confianza (1 min)

Para clientes con código legacy que no saben ni por dónde empezar:

**CodeIntel les da el mapa**. Les muestra:

- ¿Qué tienen?
- ¿Cómo está conectado?
- ¿Por dónde empezar a modernizar?

Ya no están navegando a ciegas. Tienen un **Knowledge Store inteligente** que pueden consultar, que pueden explorar, que pueden entregar a agentes de modernización para que les ayuden a:

- Migrar a .NET moderno
- Refactorizar código problemático
- Documentar automáticamente
- Identificar candidatos para microservicios
- Evaluar riesgos de cambio

**El código legacy deja de ser una amenaza y se convierte en un activo comprensible y modernizable.**

---

## Arquitectura Técnica - Resumen

### Stack Tecnológico:
- **.NET 8** - Framework moderno y de alto rendimiento
- **Roslyn** - Análisis sintáctico y semántico profundo de C#
- **Neo4j** - Base de datos de grafos para Knowledge Graph
- **Azure OpenAI** - Embeddings vectoriales para búsqueda semántica
- **Azure Search** - Índice vectorial de alta velocidad
- **GitHub Octokit** - Integración nativa con repositorios
- **Azure Functions** - Procesamiento escalable y serverless

### Componentes Principales:

1. **CodeIntel.Ingest**
   - `OctokitGitHubSource`: Descarga de repositorios
   - `RoslynAnalyzer`: Análisis de código C#
   - `CodeChunker`: Normalización para embeddings

2. **CodeIntel.Graph**
   - `Neo4jGraphStore`: Persistencia de grafo de conocimiento
   - `CosmosGremlinGraphStore`: Alternativa con Cosmos DB

3. **CodeIntel.Vector**
   - `AzureOpenAIEmbeddingService`: Generación de vectores
   - `Neo4jVectorIndex` / `AzureSearchVectorIndex`: Búsqueda vectorial

4. **CodeIntel.Functions**
   - `IngestOrchestrator`: Coordinación del pipeline completo

---

### Contacto y Demo

Para más información sobre cómo CodeIntel puede ayudar a modernizar tu código legacy, contacta con el equipo de desarrollo.

**¡Gracias!**
