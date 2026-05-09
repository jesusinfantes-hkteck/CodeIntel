# Modificaciones para Soporte de Archivos ASPX Legacy .NET

## Resumen de Cambios

Este documento detalla las modificaciones implementadas para permitir el análisis de archivos ASPX/ASCX de aplicaciones legacy .NET Framework en el proyecto CodeIntel.

## 📋 Archivos Modificados

### 1. **CodeIntel.Core\Models.cs**
**Cambios:**
- ✨ Agregados nuevos modelos de dominio:
  - `AspxPage`: Representa páginas .aspx o controles de usuario .ascx
  - `AspxControl`: Representa controles ASP.NET dentro de una página (ej: Button, GridView)
  - `AspxEvent`: Representa eventos conectados a métodos manejadores (ej: OnClick -> btnSubmit_Click)

- 🔄 Actualizado `EdgeType` con nuevas relaciones:
  - `CodeBehind`: Conecta página ASPX con su clase code-behind
  - `HasControl`: Conecta página con sus controles
  - `HandlesEvent`: Conecta evento con método manejador

- 📦 Actualizado `GraphModel`:
  - Ahora incluye `AspxPages`, `AspxControls` y `AspxEvents`

### 2. **CodeIntel.Ingest\CodeIntel.Ingest.csproj**
**Cambios:**
- ➕ Agregada dependencia `HtmlAgilityPack` v1.11.54 para parsear HTML/ASPX

### 3. **CodeIntel.Ingest\Aspx\AspxAnalyzer.cs** ✨ NUEVO
**Funcionalidad:**
- Analiza archivos .aspx y .ascx
- Extrae directivas `@Page` y `@Control` usando expresiones regulares
- Identifica atributos `Inherits` y `CodeBehind`
- Parsea controles server-side (runat="server")
- Detecta eventos comunes (OnClick, OnLoad, OnTextChanged, etc.)
- Genera nodos y relaciones para el grafo

**Eventos detectados:**
- OnClick, OnLoad, OnInit
- OnTextChanged, OnSelectedIndexChanged
- OnCommand, OnRowDataBound, OnItemDataBound
- OnCheckedChanged

### 4. **CodeIntel.Ingest\Roslyn\RoslynAnalyzer.cs**
**Cambios:**
- Integra `AspxAnalyzer` para análisis combinado
- Busca archivos `*.aspx` y `*.ascx` además de `*.cs`
- Excluye archivos `.designer.cs` (code-behind auto-generado)
- Consolida resultados de análisis C# + ASPX en un solo `GraphModel`
- Manejo robusto de errores para ASPX mal formados

### 5. **CodeIntel.Ingest\Chunking\CodeChunker.cs**
**Cambios:**
- ➕ Genera chunks para páginas ASPX
- ➕ Genera chunks para controles ASPX
- ➕ Genera chunks para eventos ASPX
- Los chunks incluyen información contextual (code-behind, tipo de control, eventos)

### 5. **CodeIntel.Graph\Neo4jVersionedGraphStore.cs**
**Cambios:**
- Crea nodos `AspxPage` en Neo4j con propiedades:
  - id, name, filePath, codeBehindClass, inherits
- Crea nodos `AspxControl` con propiedades:
  - id, name, type, pageId, filePath, events (JSON)
- Crea nodos `AspxEvent` con propiedades:
  - id, eventName, controlId, handlerMethod
- Nuevas relaciones:
  - `(Repository)-[:CONTAINS]->(AspxPage)`
  - `(AspxPage)-[:HAS_CONTROL]->(AspxControl)`
  - `(AspxControl)-[:TRIGGERS]->(AspxEvent)`
  - `(AspxPage)-[:CODE_BEHIND]->(CodeClass)`
  - `(AspxEvent)-[:HANDLES_EVENT]->(CodeMethod)`
- Soporte completo de versionado temporal para elementos ASPX
- Actualizado logging con contadores ASPX

### 6. **CodeIntel.Functions\Program.cs**
**Cambios:**
- Actualizado logging para incluir contadores ASPX
- Respuesta JSON ahora incluye:
  - `aspxPages`, `aspxControls`, `aspxEvents`

### 7. **CodeIntel.Functions\GitHubWebhookFunction.cs**
**Cambios:**
- Endpoint de snapshot ahora incluye contadores ASPX en la respuesta

## 🔍 Patrones ASPX Detectados

### Directivas
```aspx
<%@ Page Language="C#" Inherits="MyApp.Pages.Default" CodeBehind="Default.aspx.cs" %>
<%@ Control Language="C#" Inherits="MyApp.Controls.Header" CodeBehind="Header.ascx.cs" %>
```

### Controles Server-Side
```aspx
<asp:Button ID="btnSubmit" runat="server" OnClick="btnSubmit_Click" Text="Submit" />
<asp:GridView ID="gvUsers" runat="server" OnRowDataBound="gvUsers_RowDataBound" />
<asp:TextBox ID="txtName" runat="server" OnTextChanged="txtName_TextChanged" AutoPostBack="true" />
```

### UserControls
```aspx
<%@ Register Src="~/Controls/Header.ascx" TagName="Header" TagPrefix="uc" %>
<uc:Header ID="ucHeader" runat="server" />
```

## 📊 Modelo de Grafo

```
┌─────────────┐
│ Repository  │
└──────┬──────┘
       │ CONTAINS
       ├────────────┐
       │            │
       ▼            ▼
┌──────────┐   ┌──────────┐
│  Class   │   │ AspxPage │
└────┬─────┘   └────┬─────┘
     │ HAS_METHOD   │ CODE_BEHIND ──┐
     │              │                │
     ▼              │ HAS_CONTROL    │
┌──────────┐       │                │
│  Method  │◄──────┼────────────────┘
└──────────┘       │
                   ▼
             ┌─────────────┐
             │ AspxControl │
             └──────┬──────┘
                    │ TRIGGERS
                    ▼
             ┌─────────────┐
             │  AspxEvent  │──HANDLES_EVENT──► Method
             └─────────────┘
```

## 🎯 Casos de Uso

### 1. Encontrar todas las páginas de un repositorio legacy
```cypher
MATCH (r:Repository)-[:CONTAINS]->(p:AspxPage)
WHERE r.id = 'owner/repo@main'
RETURN p.name, p.codeBehindClass, p.filePath
```

### 2. Encontrar controles con eventos específicos
```cypher
MATCH (p:AspxPage)-[:HAS_CONTROL]->(c:AspxControl)-[:TRIGGERS]->(e:AspxEvent)
WHERE e.eventName = 'OnClick'
RETURN p.name, c.name, e.handlerMethod
```

### 3. Rastrear flujo desde UI hasta código
```cypher
MATCH path = (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
             -[:TRIGGERS]->(event:AspxEvent)
             -[:HANDLES_EVENT]->(method:Method)
             -[:CALLS*0..3]->(called:Method)
WHERE page.name = 'Default.aspx'
RETURN path
```

### 4. Encontrar páginas huérfanas (sin code-behind)
```cypher
MATCH (p:AspxPage)
WHERE NOT EXISTS((p)-[:CODE_BEHIND]->(:Class))
RETURN p.name, p.filePath
```

## 🚀 Próximos Pasos (Mejoras Opcionales)

1. **Versionado ASPX Completo**: Implementar soporte temporal para nodos ASPX en `Neo4jVersionedGraphStore`

2. **Análisis más profundo**:
   - Detectar data binding expressions (`<%# Eval("Field") %>`)
   - Parsear inline code blocks (`<script runat="server">`)
   - Analizar MasterPages y herencia de páginas

3. **Validaciones**:
   - Detectar event handlers que no existen en code-behind
   - Identificar controles sin ID (no accesibles desde code-behind)
   - Warnings para páginas sin code-behind

4. **Integración con migraciones**:
   - Sugerir conversión a Razor Pages / Blazor
   - Identificar patrones anti-modernos

## ✅ Verificación

Para validar que todo funciona correctamente:

1. **Compilar el proyecto:**
   ```bash
   dotnet build
   ```

2. **Apuntar a un repositorio legacy con ASPX:**
   ```json
   {
     "owner": "tu-org",
     "repo": "legacy-webforms-app",
     "branch": "master"
   }
   ```

3. **Verificar en Neo4j:**
   ```cypher
   MATCH (p:AspxPage) RETURN count(p)
   MATCH (c:AspxControl) RETURN count(c)
   MATCH (e:AspxEvent) RETURN count(e)
   ```

## 📝 Notas Técnicas

- **HtmlAgilityPack** se eligió por su robustez con HTML mal formado (común en ASPX legacy)
- Los archivos `.designer.cs` se excluyen automáticamente (contienen código auto-generado)
- El análisis es tolerante a fallos: si un ASPX no se puede parsear, se registra un warning y continúa
- Los eventos se serializan como JSON en Neo4j para preservar todos los handlers
- El análisis es sintáctico (no semántico), suficiente para grafo de dependencias

## 🎉 Resultado

Ahora el sistema puede:
- ✅ Leer archivos ASPX/ASCX de repositorios legacy
- ✅ Extraer controles, eventos y relaciones code-behind
- ✅ Generar embeddings para búsqueda semántica de UI
- ✅ Visualizar flujos de interacción UI → Code
- ✅ Soportar migraciones informadas a tecnologías modernas
