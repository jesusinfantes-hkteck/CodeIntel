# 📚 Guía de Archivos Markdown - ¿Cuáles Guardar en Git?

## ✅ **ESENCIALES** (Guardar siempre)

### 1. **README.md** ⭐⭐⭐
**Estado:** MANTENER  
**Razón:** Documentación principal del proyecto. Primer punto de entrada para cualquier desarrollador.

### 2. **CHANGELOG.md** ⭐⭐⭐
**Estado:** MANTENER  
**Razón:** Registro oficial de cambios del proyecto. Estándar de la industria.

### 3. **GETTING_STARTED.md** ⭐⭐⭐
**Estado:** MANTENER  
**Razón:** Guía de inicio rápido. Crítica para nuevos desarrolladores.

### 4. **.gitignore** ⭐⭐⭐
**Estado:** MANTENER  
**Razón:** Configuración de Git (no es MD pero es esencial).

---

## 📖 **DOCUMENTACIÓN IMPORTANTE** (Guardar)

### 5. **INDICE_DOCUMENTACION.md** ⭐⭐
**Estado:** MANTENER  
**Razón:** Índice navegable de toda la documentación. Ayuda a encontrar información.

### 6. **README_ASPX.md** ⭐⭐
**Estado:** MANTENER  
**Razón:** Documentación específica de la feature ASPX. Importante para usuarios de legacy .NET.

### 7. **ASPX_SUPPORT_IMPLEMENTATION.md** ⭐⭐
**Estado:** MANTENER  
**Razón:** Detalles técnicos de implementación ASPX. Útil para mantenimiento.

### 8. **ASPX_QUERY_EXAMPLES.md** ⭐⭐
**Estado:** MANTENER  
**Razón:** 30 ejemplos de consultas Cypher para ASPX. Muy útil para usuarios.

---

## 🔧 **GUÍAS TÉCNICAS** (Guardar selectivamente)

### 9. **CONFIGURACION_NEO4J_AURADB.md** ⭐
**Estado:** MANTENER  
**Razón:** Guía de configuración de Neo4j en la nube. Útil para deployment.

### 10. **docs/Versionado_y_Rollback_Neo4j.md** ⭐
**Estado:** MANTENER (ya está en /docs)  
**Razón:** Explica el versionado temporal, característica clave del proyecto.

---

## ⚠️ **REDUNDANTES / CONSOLIDAR** (Revisar)

### 11. **RESUMEN_EJECUTIVO.md**
**Estado:** CONSIDERAR ELIMINAR  
**Razón:** Contenido redundante con README.md  
**Acción:** Consolidar en README.md

### 12. **RESUMEN_VISUAL.md**
**Estado:** CONSIDERAR ELIMINAR  
**Razón:** Contenido visual ya está en otros documentos  
**Acción:** Consolidar en GETTING_STARTED.md

### 13. **IMPLEMENTACION_COMPLETADA.md**
**Estado:** CONSIDERAR ELIMINAR  
**Razón:** Es un snapshot histórico, no documentación activa  
**Acción:** Mover información relevante a CHANGELOG.md

### 14. **ASPX_ARCHITECTURE.md**
**Estado:** CONSOLIDAR  
**Razón:** Duplica información de README_ASPX.md  
**Acción:** Consolidar en README_ASPX.md o mover a /docs

### 15. **ASPX_TEST_CASE.md**
**Estado:** MOVER o ELIMINAR  
**Razón:** Es un ejemplo específico  
**Acción:** Mover a /docs/examples/ o eliminar si está en ASPX_QUERY_EXAMPLES.md

---

## 🗑️ **TEMPORALES / NO NECESARIOS** (No guardar en Git)

### 16. **COMMIT_CLEANUP_SUMMARY.md** ❌
**Estado:** ELIMINAR  
**Razón:** Resumen temporal para un commit específico. Ya está en Git history.

### 17. **COMMIT_NEO4J_SIMPLIFICATION.md** ❌
**Estado:** ELIMINAR  
**Razón:** Resumen temporal para un commit específico. Ya está en Git history.

### 18. **SIGUIENTE_ACCION.md** ❌
**Estado:** ELIMINAR  
**Razón:** Lista temporal de tareas. Debe estar en Issues de GitHub.

---

## 🔄 **DEPRECADOS** (Marcar o eliminar)

### 19. **FIX_AZURE_SEARCH_ERROR.md**
**Estado:** YA MARCADO COMO DEPRECATED  
**Razón:** Azure Search ya no se usa  
**Acción:** Eliminar o dejar como referencia histórica

### 20. **CLEANUP_AZURE_GREMLIN.md**
**Estado:** PUEDE ELIMINARSE  
**Razón:** Documento temporal de limpieza ya completada  
**Acción:** Eliminar (información ya está en CHANGELOG.md)

### 21. **SIMPLIFICATION_NEO4J_STRATEGY.md**
**Estado:** PUEDE ELIMINARSE  
**Razón:** Documento temporal de simplificación ya completada  
**Acción:** Eliminar (información ya está en CHANGELOG.md y docs/)

### 22. **CAMBIOS_NEO4J_AURADB.md**
**Estado:** CONSOLIDAR  
**Razón:** Duplica información de CONFIGURACION_NEO4J_AURADB.md  
**Acción:** Consolidar en un solo documento

### 23. **USA_NEO4J_EN_LA_NUBE.md**
**Estado:** CONSOLIDAR  
**Razón:** Duplica información de CONFIGURACION_NEO4J_AURADB.md  
**Acción:** Consolidar en CONFIGURACION_NEO4J_AURADB.md

### 24. **Discurso_AriadnaKnowledgeStore_Presentacion.md**
**Estado:** OPCIONAL  
**Razón:** Material de presentación, no documentación técnica  
**Acción:** Mover a carpeta /presentations/ o eliminar

---

## 📋 **RESUMEN DE ACCIONES RECOMENDADAS**

### ✅ **Mantener en Git (11 archivos):**
```
├── README.md                              ⭐⭐⭐
├── CHANGELOG.md                           ⭐⭐⭐
├── GETTING_STARTED.md                     ⭐⭐⭐
├── INDICE_DOCUMENTACION.md                ⭐⭐
├── README_ASPX.md                         ⭐⭐
├── ASPX_SUPPORT_IMPLEMENTATION.md         ⭐⭐
├── ASPX_QUERY_EXAMPLES.md                 ⭐⭐
├── CONFIGURACION_NEO4J_AURADB.md          ⭐
└── docs/
    ├── Versionado_y_Rollback_Neo4j.md     ⭐
    ├── README-Neo4j-Vector-Graph.md       ⭐
    └── local-testing-guide.md             ⭐
```

### 🗑️ **Eliminar (7 archivos):**
```
❌ COMMIT_CLEANUP_SUMMARY.md
❌ COMMIT_NEO4J_SIMPLIFICATION.md
❌ SIGUIENTE_ACCION.md
❌ CLEANUP_AZURE_GREMLIN.md
❌ SIMPLIFICATION_NEO4J_STRATEGY.md
❌ FIX_AZURE_SEARCH_ERROR.md
❌ Discurso_AriadnaKnowledgeStore_Presentacion.md (o mover a /presentations)
```

### 🔄 **Consolidar y luego eliminar originales (6 archivos):**
```
RESUMEN_EJECUTIVO.md          → Consolidar en README.md
RESUMEN_VISUAL.md             → Consolidar en GETTING_STARTED.md
IMPLEMENTACION_COMPLETADA.md  → Extracto relevante a CHANGELOG.md
ASPX_ARCHITECTURE.md          → Consolidar en README_ASPX.md
ASPX_TEST_CASE.md             → Mover a docs/examples/ o eliminar
CAMBIOS_NEO4J_AURADB.md       → Consolidar en CONFIGURACION_NEO4J_AURADB.md
USA_NEO4J_EN_LA_NUBE.md       → Consolidar en CONFIGURACION_NEO4J_AURADB.md
```

---

## 🚀 **COMANDOS PARA LIMPIAR**

```powershell
# 1. Eliminar archivos temporales/commit
Remove-Item COMMIT_CLEANUP_SUMMARY.md
Remove-Item COMMIT_NEO4J_SIMPLIFICATION.md
Remove-Item SIGUIENTE_ACCION.md
Remove-Item CLEANUP_AZURE_GREMLIN.md
Remove-Item SIMPLIFICATION_NEO4J_STRATEGY.md
Remove-Item FIX_AZURE_SEARCH_ERROR.md

# 2. Consolidar y eliminar redundantes (después de consolidar manualmente)
# Remove-Item RESUMEN_EJECUTIVO.md
# Remove-Item RESUMEN_VISUAL.md
# Remove-Item IMPLEMENTACION_COMPLETADA.md
# Remove-Item ASPX_ARCHITECTURE.md
# Remove-Item ASPX_TEST_CASE.md
# Remove-Item CAMBIOS_NEO4J_AURADB.md
# Remove-Item USA_NEO4J_EN_LA_NUBE.md

# 3. Opcional: Crear carpeta para archivos históricos
# New-Item -ItemType Directory -Path "archive"
# Move-Item Discurso_AriadnaKnowledgeStore_Presentacion.md archive/
```

---

## 📁 **ESTRUCTURA RECOMENDADA FINAL**

```
gh-ariadna-knowledgestore-mvp/src/
├── README.md                              # Documentación principal
├── CHANGELOG.md                           # Historial de cambios
├── GETTING_STARTED.md                     # Guía de inicio rápido
├── INDICE_DOCUMENTACION.md                # Índice de toda la doc
│
├── README_ASPX.md                         # Feature: Análisis ASPX
├── ASPX_SUPPORT_IMPLEMENTATION.md         # Detalles técnicos ASPX
├── ASPX_QUERY_EXAMPLES.md                 # 30 ejemplos de consultas
│
├── CONFIGURACION_NEO4J_AURADB.md          # Setup Neo4j en la nube
│
├── docs/                                  # Documentación detallada
│   ├── Versionado_y_Rollback_Neo4j.md
│   ├── README-Neo4j-Vector-Graph.md
│   ├── local-testing-guide.md
│   ├── neo4j-migration-checklist.md
│   └── examples/                          # Ejemplos adicionales
│
├── scripts/                               # Scripts de utilidad
│   ├── Test-Strategy1.ps1
│   └── ...
│
└── [Código fuente]
```

---

## 💡 **RECOMENDACIÓN FINAL**

### **Fase 1: Limpiar archivos temporales (AHORA)**
Elimina los 7 archivos temporales listados arriba. Son resúmenes de commits que ya están en Git history.

### **Fase 2: Consolidar documentación (PRONTO)**
Consolida los 6-7 archivos redundantes en documentos principales para evitar confusión.

### **Fase 3: Mantener disciplina (FUTURO)**
- Documentación técnica → `/docs`
- Ejemplos → `/docs/examples`
- Material de marketing/presentaciones → `/presentations` o fuera de Git
- Resúmenes de commits → Solo en mensajes de commit, no archivos

---

## ✅ **ACCIÓN INMEDIATA SUGERIDA**

```powershell
# Ejecutar esto para limpiar archivos temporales
Remove-Item COMMIT_CLEANUP_SUMMARY.md -ErrorAction SilentlyContinue
Remove-Item COMMIT_NEO4J_SIMPLIFICATION.md -ErrorAction SilentlyContinue
Remove-Item SIGUIENTE_ACCION.md -ErrorAction SilentlyContinue
Remove-Item CLEANUP_AZURE_GREMLIN.md -ErrorAction SilentlyContinue
Remove-Item SIMPLIFICATION_NEO4J_STRATEGY.md -ErrorAction SilentlyContinue
Remove-Item FIX_AZURE_SEARCH_ERROR.md -ErrorAction SilentlyContinue

Write-Host "✅ Archivos temporales eliminados" -ForegroundColor Green
Write-Host "📝 Revisa manualmente los archivos a consolidar" -ForegroundColor Yellow
```

---

**Fecha:** 2026-05-09  
**Total archivos MD actuales:** 24  
**Recomendados mantener:** 11  
**Recomendados eliminar:** 7  
**Recomendados consolidar:** 6
