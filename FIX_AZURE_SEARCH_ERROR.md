# 🔧 FIX: Error "Invalid URI: The hostname could not be parsed"

## ❌ Error Original

```
[2026-05-07T14:27:29.880Z] System.UriFormatException: Invalid URI: The hostname could not be parsed.
[2026-05-07T14:27:29.881Z]    at System.Uri..ctor(String uriString)
[2026-05-07T14:27:29.883Z]    at CodeIntel.Vector.AzureSearchVectorIndex..ctor(String endpoint, ...)
[2026-05-07T14:27:29.885Z]    at Program.cs:line 151
```

---

## 🔍 Causa del Problema

El código en `Program.cs` intentaba crear `AzureSearchVectorIndex` **siempre** que `AzureOpenAI:Endpoint` estuviera configurado, **pero** el `Search:Endpoint` estaba vacío en `appsettings.json`.

### Código Problemático (ANTES):

```csharp
// Línea ~134
bool useRealAzure = !string.IsNullOrEmpty(cfg["AzureOpenAI:Endpoint"]);

if (useRealAzure)
{
    // Configurar Embeddings
    services.AddSingleton<IEmbeddingService>(...);

    // ❌ PROBLEMA: Intenta usar Azure Search incluso si Endpoint está vacío
    services.AddSingleton<IVectorIndex>(_ => new AzureSearchVectorIndex(
        endpoint: cfg["Search:Endpoint"]!,  // ← Esto estaba vacío: ""
        ...
    ));
}
```

---

## ✅ Solución Aplicada

Separar la lógica de **Embeddings (Azure OpenAI)** y **Vector Index (Azure Search)**:

### Código Corregido (AHORA):

```csharp
// Verificar separadamente cada servicio
bool useRealAzureOpenAI = !string.IsNullOrEmpty(cfg["AzureOpenAI:Endpoint"]);
bool useRealAzureSearch = !string.IsNullOrEmpty(cfg["Search:Endpoint"]);

// Embedding Service (Azure OpenAI o Mock)
if (useRealAzureOpenAI)
{
    services.AddSingleton<IEmbeddingService>(...);
}
else
{
    services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
}

// Vector Index (Azure Search o Mock) ← NUEVO
if (useRealAzureSearch)
{
    services.AddSingleton<IVectorIndex>(_ => new AzureSearchVectorIndex(...));
}
else
{
    services.AddSingleton<IVectorIndex, MockVectorIndex>(); // ✅ Usa Mock
}
```

---

## 🎯 Resultado

Ahora el sistema funciona con **cualquier combinación**:

| Azure OpenAI | Azure Search | Resultado |
|--------------|--------------|-----------|
| ❌ Vacío | ❌ Vacío | ✅ Usa Mock para ambos |
| ❌ Vacío | ✅ Configurado | ✅ Mock Embeddings + Azure Search |
| ✅ Configurado | ❌ Vacío | ✅ Azure OpenAI + Mock Vector Index |
| ✅ Configurado | ✅ Configurado | ✅ Ambos reales |

---

## 🧪 Para Probar

```powershell
# 1. Recompilar
cd C:\proyectos\gh-code-intel-mvp\src
dotnet build

# 2. Iniciar Functions
cd CodeIntel.Functions
func start

# 3. Probar análisis
$body = @{ owner='octocat'; repo='Hello-World'; branch='master' } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:7071/api/ingest -Method POST -Body $body -ContentType 'application/json'
```

Deberías ver algo como:

```
[2026-05-07T14:30:00.123Z] Executing 'Functions.ingest'
[2026-05-07T14:30:01.456Z] Using MockEmbeddingService (no Azure OpenAI configured)
[2026-05-07T14:30:01.789Z] Using MockVectorIndex (no Azure Search configured)
[2026-05-07T14:30:05.123Z] Executed 'Functions.ingest' (Succeeded, ...)
```

---

## 📝 Configuración de appsettings.json

### Para usar SOLO Neo4j (sin Azure OpenAI ni Azure Search):

```json
{
  "GitHub": {
    "Token": "ghp_tu_token"
  },
  "GraphStore": {
    "Type": "Neo4jVersioned"
  },
  "Neo4j": {
    "Uri": "neo4j+s://tu-instancia.databases.neo4j.io",
    "User": "neo4j",
    "Password": "tu-password"
  },
  "AzureOpenAI": {
    "Endpoint": "",  // ← Vacío = usa Mock
    "ApiKey": "",
    "EmbeddingDeployment": "text-embedding-ada-002",
    "ApiVersion": "2024-06-01"
  },
  "Search": {
    "Endpoint": "",  // ← Vacío = usa Mock
    "ApiKey": "",
    "IndexName": "codeintel",
    "VectorDimensions": 1536
  }
}
```

✅ **Esto funciona perfectamente para desarrollo y pruebas.**

---

## 💡 Configuración Opcional de Azure Services

### Si quieres usar Azure OpenAI para embeddings reales:

1. **Crear recurso Azure OpenAI:**
   - Portal Azure → Create Resource → Azure OpenAI
   - Deploy modelo: `text-embedding-ada-002`

2. **Actualizar appsettings.json:**
   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "https://tu-recurso.openai.azure.com/",
       "ApiKey": "tu-api-key",
       "EmbeddingDeployment": "text-embedding-ada-002"
     }
   }
   ```

### Si quieres usar Azure AI Search para vector search:

1. **Crear recurso Azure AI Search:**
   - Portal Azure → Create Resource → Azure AI Search
   - Tier: Basic o superior (Free no soporta vector search)

2. **Actualizar appsettings.json:**
   ```json
   {
     "Search": {
       "Endpoint": "https://tu-search.search.windows.net",
       "ApiKey": "tu-admin-key",
       "IndexName": "codeintel"
     }
   }
   ```

---

## ✅ Resumen

**Problema:** Azure Search endpoint vacío causaba excepción  
**Solución:** Detectar configuración vacía y usar Mock automáticamente  
**Estado:** ✅ **RESUELTO**  
**Archivo modificado:** `CodeIntel.Functions/Program.cs`

---

## 🚀 Siguiente Paso

```powershell
# Reiniciar Functions
func start

# Probar
$body = @{ owner='octocat'; repo='Hello-World'; branch='master' } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:7071/api/ingest -Method POST -Body $body -ContentType 'application/json'
```

Ahora debería funcionar sin errores. 🎉

---

**Fecha del fix:** 7 de mayo de 2026  
**Versión:** 1.0.1
