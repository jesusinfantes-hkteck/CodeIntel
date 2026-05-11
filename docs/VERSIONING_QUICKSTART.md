# ⚡ Sistema de Versionado - Resumen Rápido

**TL;DR:** Neo4j mantiene TODAS las versiones de tu código. Puedes viajar en el tiempo, hacer rollback y comparar cambios.

---

## 🎯 **Respuesta a tu Pregunta:**

### **¿Depende del número de versión en GitHub?**
❌ **NO directamente**. El sistema usa:
- **GUIDs** propios para identificar versiones
- Puede **almacenar** el SHA de commit de GitHub como metadata
- Funciona **independiente** de GitHub (pero puede integrarse)

### **¿Cómo funciona el rollback?**
✅ **Cambio de puntero lógico** (no borra nada):
```csharp
await store.RollbackToVersionAsync("repo-id", "version-id", ct);
// Solo marca esa versión como "current"
// Las versiones futuras siguen existiendo
```

---

## 📊 **Concepto Clave: Versionado Temporal**

Cada nodo en Neo4j tiene:
```json
{
  "id": "class:Product",
  "validFrom": 1704067200,  // Desde cuándo existe
  "validTo": 1704153600,    // Hasta cuándo existe (NULL = actual)
  "versionId": "guid-123"   // A qué snapshot pertenece
}
```

**Query para obtener nodos actuales:**
```cypher
MATCH (c:Class)
WHERE c.validTo IS NULL  // Solo nodos sin fecha de fin
RETURN c
```

**Query para obtener nodos en el pasado:**
```cypher
MATCH (c:Class)
WHERE c.validFrom <= timestamp
  AND (c.validTo IS NULL OR c.validTo > timestamp)
RETURN c
```

---

## 🔄 **Los 3 Métodos Principales:**

### **1. GetVersionHistoryAsync()**
```csharp
var versions = await store.GetVersionHistoryAsync("acme/shop@main", ct);
// Devuelve: List<VersionInfo>
//   - VersionId (GUID)
//   - CommitHash (de GitHub, opcional)
//   - Timestamp (cuándo se ingirió)
//   - IsCurrent (¿es la versión activa?)
```

**Uso:** Ver todas las versiones disponibles.

---

### **2. GetGraphAtTimestampAsync()**
```csharp
var oldGraph = await store.GetGraphAtTimestampAsync(
    "acme/shop@main", 
    timestamp: 1704067200,  // Unix timestamp
    ct
);
// Devuelve: GraphModel con nodos válidos en ese momento
```

**Uso:** "Time travel" - ver cómo era el código en el pasado.

---

### **3. RollbackToVersionAsync()**
```csharp
await store.RollbackToVersionAsync(
    "acme/shop@main", 
    "version-guid", 
    ct
);
// Efecto: Marca esa versión como "current"
// NO elimina versiones futuras
```

**Uso:** Rollback después de deployment problemático.

---

## 🎬 **Ejemplo Real - Flujo Completo:**

```csharp
// ========================================
// PASO 1: Listar versiones
// ========================================
var versions = await store.GetVersionHistoryAsync("acme/shop@main", ct);

foreach (var v in versions)
{
    Console.WriteLine($"{v.Timestamp:yyyy-MM-dd HH:mm:ss} " +
                      $"| Commit: {v.CommitHash} " +
                      $"| {(v.IsCurrent ? "← ACTUAL" : "")}");
}

// Output:
// 2024-01-01 10:00:00 | Commit: abc123
// 2024-01-02 15:30:00 | Commit: def456
// 2024-01-03 09:00:00 | Commit: ghi789 ← ACTUAL

// ========================================
// PASO 2: ¡Deployment de ghi789 causó bugs!
// ========================================
Console.WriteLine("⚠️ Deployment problemático detectado!");

// ========================================
// PASO 3: Rollback a versión anterior (def456)
// ========================================
var stableVersion = versions[1]; // def456
await store.RollbackToVersionAsync("acme/shop@main", stableVersion.VersionId, ct);

Console.WriteLine("✅ Rollback completado - Sistema restaurado a def456");

// ========================================
// PASO 4: Verificar que funcionó
// ========================================
versions = await store.GetVersionHistoryAsync("acme/shop@main", ct);

foreach (var v in versions)
{
    Console.WriteLine($"{v.Timestamp:yyyy-MM-dd HH:mm:ss} " +
                      $"| Commit: {v.CommitHash} " +
                      $"| {(v.IsCurrent ? "← ACTUAL" : "")}");
}

// Output DESPUÉS del rollback:
// 2024-01-01 10:00:00 | Commit: abc123
// 2024-01-02 15:30:00 | Commit: def456 ← ACTUAL (restaurado)
// 2024-01-03 09:00:00 | Commit: ghi789

// ========================================
// PASO 5: Time Travel - ¿Cómo era en abc123?
// ========================================
var firstVersion = versions[0];
var oldGraph = await store.GetGraphAtTimestampAsync(
    "acme/shop@main", 
    firstVersion.Timestamp.ToUnixTimeSeconds(), 
    ct
);

Console.WriteLine($"Clases en {firstVersion.Timestamp:yyyy-MM-dd}:");
foreach (var cls in oldGraph.Classes)
{
    Console.WriteLine($"  • {cls.Name}");
}
```

---

## 🔐 **Características Importantes:**

### ✅ **NO Destructivo**
- Rollback NO elimina versiones futuras
- Puedes ir y volver entre versiones
- Es como `git checkout` para el grafo

### ✅ **Auditable**
- Cada versión guarda timestamp
- Puedes rastrear cuándo cambió algo
- Historial completo preservado

### ✅ **GraphRAG Compatible**
- Vector search funciona en todas las versiones
- Graph expansion respeta versionado temporal
- Puedes preguntar "¿cómo funcionaba hace 2 días?"

---

## 🚀 **Integración con GitHub:**

### **Opción 1: Manual (actual)**
```csharp
var request = new RepoRequest(
    Owner: "acme",
    Repo: "shop",
    Branch: "main",
    Path: "commit-sha-from-github"  // ← Pasas el SHA manualmente
);
await ingestFunction.IngestAsync(request, ct);
```

### **Opción 2: Webhook (futuro)**
```csharp
[Function("GitHubWebhook")]
public async Task<HttpResponseData> Run(HttpRequestData req)
{
    var payload = await req.ReadFromJsonAsync<GitHubPayload>();

    var request = new RepoRequest(
        Owner: payload.Repository.Owner,
        Repo: payload.Repository.Name,
        Branch: payload.Ref.Replace("refs/heads/", ""),
        Path: payload.After  // ← SHA del commit automáticamente
    );

    await ingestFunction.IngestAsync(request, ct);
    return req.CreateResponse(HttpStatusCode.OK);
}
```

---

## 📚 **Documentación Completa:**

1. **`VERSIONING_SYSTEM_EXPLAINED.md`** - Explicación detallada (20+ páginas)
2. **`VERSIONING_DIAGRAMS.md`** - Diagramas visuales ASCII
3. **`examples/VersioningExamples.cs`** - 8 ejemplos de código ejecutables
4. **`BLAZOR_NEO4J_QUERIES.md`** - Queries específicas para Blazor

---

## 🎯 **Caso de Uso Principal:**

**Pregunta:** "¿Cómo arreglar un bug en el servicio de pago?"

**Con versionado:**
1. 🔍 Vector search encuentra código actual de pago
2. 🕐 Consultas versión donde se introdujo el bug
3. 📊 Compara versión actual vs versión estable
4. 🔙 Rollback temporal para probar fix
5. ✅ Restaura versión correcta

**Sin versionado:**
❌ Solo verías el código actual, sin contexto histórico.

---

## ⚡ **Conclusión:**

El sistema de versionado temporal te da **superpoderes** para:
- 🕐 Viajar en el tiempo
- 🔙 Hacer rollback seguro
- 📊 Auditar cambios
- 🔍 GraphRAG histórico
- 🎯 Debugging con contexto

**Todo automático, sin configuración manual de versiones de GitHub.**

---

**Siguiente paso:** Lee `docs/examples/VersioningExamples.cs` para ver código ejecutable.

---

**Creado:** 2024
**Versión:** 1.0 (Quick Reference)
