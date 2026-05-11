# 📚 Documentación del Sistema de Versionado

Este directorio contiene la documentación completa del sistema de versionado temporal implementado en AriadnaKnowledgeStore.

---

## 📖 **Guías de Lectura Recomendadas:**

### **Para empezar:**
1. 🚀 **`VERSIONING_QUICKSTART.md`** - Resumen rápido (5 min de lectura)
   - Respuestas directas a preguntas frecuentes
   - Ejemplos de código mínimos
   - TL;DR del sistema

### **Para entender en profundidad:**
2. 📘 **`VERSIONING_SYSTEM_EXPLAINED.md`** - Explicación completa (20 min)
   - Conceptos fundamentales
   - Arquitectura detallada
   - Casos de uso reales
   - Queries Neo4j explicadas
   - FAQ completo

### **Para visualizar:**
3. 📊 **`VERSIONING_DIAGRAMS.md`** - Diagramas visuales ASCII (15 min)
   - Árbol de versiones
   - Flujos de ingesta
   - Timelines
   - Comparaciones
   - Arquitectura completa

### **Para practicar:**
4. 💻 **`examples/VersioningExamples.cs`** - Código ejecutable (30 min)
   - 8 ejemplos prácticos
   - Código C# completo
   - Comentarios detallados
   - Casos de uso reales

---

## 🎯 **Lee según tu necesidad:**

### **"Solo quiero saber si depende de GitHub"**
→ Lee **`VERSIONING_QUICKSTART.md`** (sección 1)

### **"¿Cómo hago rollback?"**
→ Lee **`VERSIONING_QUICKSTART.md`** (sección 3) y **`examples/VersioningExamples.cs`** (Ejemplo 4)

### **"Quiero entender cómo funciona internamente"**
→ Lee **`VERSIONING_SYSTEM_EXPLAINED.md`** completo + **`VERSIONING_DIAGRAMS.md`**

### **"Necesito implementar webhooks de GitHub"**
→ Lee **`VERSIONING_SYSTEM_EXPLAINED.md`** (sección "Integración con GitHub") + **`examples/VersioningExamples.cs`** (Ejemplo 8)

### **"¿Cómo comparar versiones para debugging?"**
→ Lee **`examples/VersioningExamples.cs`** (Ejemplos 5 y 6)

### **"Quiero queries Cypher para consultar versiones"**
→ Lee **`VERSIONING_SYSTEM_EXPLAINED.md`** (sección "Queries de Versionado Útiles")

---

## 📂 **Estructura de Archivos:**

```
docs/
├── VERSIONING_QUICKSTART.md          ⚡ Resumen ejecutivo
├── VERSIONING_SYSTEM_EXPLAINED.md    📘 Guía completa
├── VERSIONING_DIAGRAMS.md            📊 Diagramas visuales
├── BLAZOR_NEO4J_QUERIES.md           🔷 Queries específicas Blazor
└── examples/
    └── VersioningExamples.cs         💻 Código ejecutable
```

---

## 🔑 **Conceptos Clave (para referencia rápida):**

### **Versionado Temporal:**
Cada nodo tiene `validFrom` y `validTo` timestamps que indican su período de validez.

### **Soft Delete:**
Los nodos NO se eliminan, solo se marca `validTo = timestamp`.

### **NEXT_VERSION:**
Relación que enlaza versiones consecutivas del mismo elemento.

### **isCurrent:**
Flag en nodo `Version` que indica cuál es la versión activa.

### **Time Travel:**
Consultar el estado del grafo en cualquier momento del pasado.

### **Rollback:**
Cambiar el flag `isCurrent` para apuntar a una versión anterior.

---

## 🎓 **Ejemplos Rápidos:**

### **Listar versiones:**
```csharp
var versions = await store.GetVersionHistoryAsync("acme/shop@main", ct);
foreach (var v in versions)
    Console.WriteLine($"{v.Timestamp} | {v.CommitHash} | {(v.IsCurrent ? "ACTUAL" : "")}");
```

### **Time travel:**
```csharp
var oldGraph = await store.GetGraphAtTimestampAsync("acme/shop@main", timestamp, ct);
```

### **Rollback:**
```csharp
await store.RollbackToVersionAsync("acme/shop@main", versionId, ct);
```

### **Query actual:**
```cypher
MATCH (c:Class)
WHERE c.validTo IS NULL
RETURN c
```

### **Query histórico:**
```cypher
MATCH (c:Class)
WHERE c.validFrom <= $timestamp
  AND (c.validTo IS NULL OR c.validTo > $timestamp)
RETURN c
```

---

## 💡 **Tips y Best Practices:**

1. **No dependas de GitHub commits:** El sistema funciona independientemente
2. **Rollback es seguro:** No elimina versiones futuras
3. **Vector search funciona en todas las versiones:** Los chunks persisten
4. **Audita antes de rollback:** Usa `GetVersionHistoryAsync()` primero
5. **Mantén logs de ingesta:** Para correlacionar con deployments

---

## 🔗 **Documentación Relacionada:**

- **`../README.md`** - Arquitectura general del sistema
- **`BLAZOR_NEO4J_QUERIES.md`** - Queries específicas para componentes Blazor
- **`neo4j-graphrag-architecture.md`** - Detalles de implementación GraphRAG

---

## ❓ **Preguntas Frecuentes:**

**P: ¿Se pierde información al hacer rollback?**  
R: ❌ NO. Solo cambias qué versión es "actual". Todo persiste.

**P: ¿Cuánto espacio ocupa el versionado?**  
R: Cada versión completa ocupa espacio. Para optimizar, considera:
   - Eliminar versiones antiguas (>90 días)
   - Snapshots semanales en lugar de cada commit
   - Compresión de versiones (diff storage)

**P: ¿Puedo integrar con CI/CD?**  
R: ✅ SÍ. Usa webhooks de GitHub para ingestar automáticamente cada push.

**P: ¿Funciona con branches?**  
R: ✅ SÍ. Cada branch tiene su propio historial (`owner/repo@branch`).

**P: ¿Puedo comparar entre branches?**  
R: ✅ SÍ. Consulta dos `repoId` diferentes y compara los `GraphModel`.

---

## 🚀 **Próximos Pasos:**

1. Lee **`VERSIONING_QUICKSTART.md`** (5 minutos)
2. Ejecuta ejemplos de **`examples/VersioningExamples.cs`**
3. Experimenta con queries Neo4j
4. Implementa webhook de GitHub (opcional)
5. Configura limpieza de versiones antiguas (opcional)

---

**¿Tienes dudas?** Lee la documentación completa o contacta al equipo de desarrollo.

**Última actualización:** 2024  
**Versión:** 1.0
