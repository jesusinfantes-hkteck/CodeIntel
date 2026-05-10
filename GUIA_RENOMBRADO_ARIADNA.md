# 🚀 Guía: Renombrado Completo AriadnaKnowledgeStore → AriadnaKnowledgeStore

## ⚡ Ejecución Rápida

### **Opción 1: Ejecutar directamente (Recomendado)**
```powershell
cd C:\proyectos\gh-ariadna-knowledgestore-mvp\src\scripts
.\Rename-To-AriadnaKnowledgeStore.ps1
```

### **Opción 2: Modo Dry-Run (Simulación)**
Prueba primero sin hacer cambios reales:
```powershell
.\Rename-To-AriadnaKnowledgeStore.ps1 -DryRun
```

### **Opción 3: Sin Backup (NO recomendado)**
```powershell
.\Rename-To-AriadnaKnowledgeStore.ps1 -SkipBackup
```

---

## 📋 Qué Hace el Script

El script realiza **10 pasos** automáticamente:

### **Paso 0: Verificaciones Previas**
- ✅ Verifica que existe el directorio del proyecto
- ✅ Detecta si Visual Studio está abierto (te avisa)
- ✅ Detiene Azure Functions si está corriendo
- ✅ Verifica el estado de Git (te avisa si hay cambios sin commit)

### **Paso 1: Crear Backup**
- ✅ Copia completa del proyecto a: `C:\proyectos\BACKUP-AriadnaKnowledgeStore-yyyyMMdd-HHmmss`
- ⏱️ Tiempo estimado: 1-2 minutos

### **Paso 2: Reemplazar Contenido en Archivos**
Busca y reemplaza en **todos** los archivos (`.cs`, `.csproj`, `.sln`, `.json`, `.md`, `.ps1`, etc.):
- `AriadnaKnowledgeStore` → `AriadnaKnowledgeStore`
- `AriadnaKnowledgeStore` → `ariadna-knowledgestore`
- `ariadna-knowledgestore` → `ariadna-knowledgestore`
- `ariadna_knowledgestore` → `ariadna_knowledgestore`
- URLs del repositorio
- ⏱️ Tiempo estimado: 2-3 minutos (~60-80 archivos)

### **Paso 3: Renombrar Archivos .csproj**
```
AriadnaKnowledgeStore.Core.csproj       → AriadnaKnowledgeStore.Core.csproj
AriadnaKnowledgeStore.Functions.csproj  → AriadnaKnowledgeStore.Functions.csproj
AriadnaKnowledgeStore.Graph.csproj      → AriadnaKnowledgeStore.Graph.csproj
AriadnaKnowledgeStore.Ingest.csproj     → AriadnaKnowledgeStore.Ingest.csproj
AriadnaKnowledgeStore.Vector.csproj     → AriadnaKnowledgeStore.Vector.csproj
```

### **Paso 4: Renombrar Archivo .sln**
```
AriadnaKnowledgeStore.sln → AriadnaKnowledgeStore.sln
```

### **Paso 5: Renombrar Carpetas de Proyectos**
```
AriadnaKnowledgeStore.Core       → AriadnaKnowledgeStore.Core
AriadnaKnowledgeStore.Functions  → AriadnaKnowledgeStore.Functions
AriadnaKnowledgeStore.Graph      → AriadnaKnowledgeStore.Graph
AriadnaKnowledgeStore.Ingest     → AriadnaKnowledgeStore.Ingest
AriadnaKnowledgeStore.Vector     → AriadnaKnowledgeStore.Vector
```

### **Paso 6: Renombrar Directorio Raíz**
```
C:\proyectos\gh-ariadna-knowledgestore-mvp → C:\proyectos\AriadnaKnowledgeStore
```

### **Paso 7: Actualizar Git Remote**
```
ANTES: https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore
DESPUÉS: https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore
```

### **Paso 8: Limpiar Binarios y Caché**
Elimina carpetas: `bin/`, `obj/`, `.vs/`

### **Paso 9: Reconstruir Solución**
- `dotnet restore AriadnaKnowledgeStore.sln`
- `dotnet build AriadnaKnowledgeStore.sln`
- ⏱️ Tiempo estimado: 1-2 minutos

### **Paso 10: Generar Reporte**
Crea archivo: `RENAME_REPORT_yyyyMMdd-HHmmss.txt` con resumen completo

---

## ⏱️ Tiempo Total Estimado

- **Dry-Run (simulación):** ~30 segundos
- **Ejecución completa:** ~5-7 minutos

---

## ✅ Después de Ejecutar el Script

### **1. Renombrar Repositorio en GitHub**
```
1. Ve a: https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore/settings
2. En "Repository name" cambia: AriadnaKnowledgeStore → AriadnaKnowledgeStore
3. Click en "Rename"
4. GitHub redirigirá automáticamente el viejo nombre al nuevo
```

### **2. Abrir en Visual Studio**
```powershell
cd C:\proyectos\AriadnaKnowledgeStore\src
code AriadnaKnowledgeStore.sln
```

O desde Visual Studio:
```
File → Open → Project/Solution
Navega a: C:\proyectos\AriadnaKnowledgeStore\src\AriadnaKnowledgeStore.sln
```

### **3. Verificar Compilación**
```powershell
cd C:\proyectos\AriadnaKnowledgeStore\src
dotnet build
```

Deberías ver:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### **4. Probar Azure Functions**
```powershell
cd C:\proyectos\AriadnaKnowledgeStore\src\AriadnaKnowledgeStore.Functions
func start
```

### **5. Probar Ingesta**
```powershell
cd C:\proyectos\AriadnaKnowledgeStore\src\scripts
.\Test-Ingest-Simple.ps1
```

### **6. Hacer Commit y Push**
```powershell
cd C:\proyectos\AriadnaKnowledgeStore\src
git add .
git commit -m "Rename: AriadnaKnowledgeStore → AriadnaKnowledgeStore - Complete project rebrand"
git push origin master --force
```

**⚠️ Nota:** Usamos `--force` porque estamos reemplazando completamente el proyecto.

---

## 🔄 Revertir Cambios (Si Algo Sale Mal)

Si necesitas volver atrás, ejecuta:

```powershell
# 1. Eliminar el proyecto renombrado
Remove-Item 'C:\proyectos\AriadnaKnowledgeStore' -Recurse -Force

# 2. Restaurar desde backup
$backupPath = "C:\proyectos\BACKUP-AriadnaKnowledgeStore-20260509-180000"  # Usa la fecha correcta
Rename-Item $backupPath 'C:\proyectos\gh-ariadna-knowledgestore-mvp'

# 3. Restaurar Git remote
cd C:\proyectos\gh-ariadna-knowledgestore-mvp\src
git remote set-url origin https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore

# 4. Limpiar y recompilar
dotnet clean
dotnet build
```

---

## 🎯 Checklist Post-Renombrado

- [ ] ✅ Script ejecutado sin errores
- [ ] ✅ Repositorio renombrado en GitHub
- [ ] ✅ Solución abre correctamente en Visual Studio
- [ ] ✅ `dotnet build` compila sin errores
- [ ] ✅ Azure Functions inicia correctamente (`func start`)
- [ ] ✅ Ingesta funciona (`.\Test-Ingest-Simple.ps1`)
- [ ] ✅ Neo4j Browser muestra datos correctamente
- [ ] ✅ Scripts PowerShell ejecutan sin errores
- [ ] ✅ Documentación actualizada (README.md, etc.)
- [ ] ✅ Git push exitoso a GitHub
- [ ] ✅ Backup guardado en lugar seguro

---

## 📊 Estadísticas Esperadas

Después de ejecutar el script verás algo como:

```
============================================================
            RENOMBRADO COMPLETADO EXITOSAMENTE
============================================================

✓ Proyecto renombrado de:
    C:\proyectos\gh-ariadna-knowledgestore-mvp
  a:
    C:\proyectos\AriadnaKnowledgeStore

✓ Backup creado en:
    C:\proyectos\BACKUP-AriadnaKnowledgeStore-20260509-180530

✓ Git remote actualizado a:
    https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore

Archivos procesados:
  - 67 archivos actualizados
  - 5 archivos .csproj renombrados
  - 1 archivo .sln renombrado
  - 5 carpetas renombradas
  - 1 directorio raíz renombrado

PRÓXIMOS PASOS:
  1. Renombra el repo en GitHub
  2. Abre VS: code 'C:\proyectos\AriadnaKnowledgeStore\src\AriadnaKnowledgeStore.sln'
  3. Compila: dotnet build
  4. Push: git push origin master --force
```

---

## ⚠️ Errores Comunes y Soluciones

### **Error: "Visual Studio está abierto"**
**Solución:** Cierra Visual Studio y vuelve a ejecutar el script.

### **Error: "func.exe está corriendo"**
**Solución:** El script lo detiene automáticamente. Si persiste:
```powershell
Stop-Process -Name "func" -Force
```

### **Error: "Hay cambios sin commitear en Git"**
**Solución:** Haz commit primero:
```powershell
git add .
git commit -m "Pre-rename checkpoint"
```

### **Error de compilación después del renombrado**
**Solución:**
```powershell
cd C:\proyectos\AriadnaKnowledgeStore\src
dotnet clean
dotnet restore
dotnet build
```

### **Error: "El directorio AriadnaKnowledgeStore ya existe"**
**Solución:** El script te preguntará si quieres eliminarlo. Responde `S` para continuar.

---

## 🔒 Seguridad

El script **NO modifica:**
- ❌ Datos en Neo4j
- ❌ Configuración de Azure
- ❌ Tu cuenta de GitHub
- ❌ Archivos fuera del directorio del proyecto

El script **SÍ crea:**
- ✅ Backup completo antes de cualquier cambio
- ✅ Reporte detallado de todos los cambios
- ✅ Logs en consola de cada paso

---

## 💡 Tips

1. **Ejecuta primero con `-DryRun`** para ver qué cambiará sin hacer cambios reales
2. **Cierra Visual Studio** antes de ejecutar
3. **Haz commit de tus cambios** antes de ejecutar
4. **Guarda el backup** en un lugar seguro (puedes comprimirlo después)
5. **Lee el reporte** generado para ver todos los cambios realizados

---

## 📞 Soporte

Si algo sale mal:
1. **NO borres el backup**
2. Revisa el reporte generado: `RENAME_REPORT_*.txt`
3. Usa los comandos de reversión descritos arriba
4. Si compilación falla, ejecuta: `dotnet clean && dotnet restore && dotnet build`

---

## 🎉 Próximos Pasos Después del Renombrado

1. **Actualizar secretos** en GitHub (si usas GitHub Actions)
2. **Actualizar URLs** en documentación externa
3. **Notificar** a colaboradores del cambio
4. **Actualizar** enlaces en README.md si apuntan a URLs externas
5. **Celebrar** 🎊 - ¡Tu proyecto ahora se llama AriadnaKnowledgeStore!

---

**Última actualización:** Mayo 2026
**Versión del script:** 1.0
