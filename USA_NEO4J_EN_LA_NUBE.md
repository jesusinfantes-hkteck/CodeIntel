# ☁️ AHORA USAS NEO4J EN LA NUBE

```
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║   CodeIntel → Neo4j AuraDB Cloud (14 días gratis)              ║
║   ¡Ya NO necesitas Docker ni Neo4j local!                      ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
```

---

## 🎯 ¿Qué Cambió?

### ❌ ANTES (Local)

```
Tu PC → Docker → Neo4j Local (localhost:7687)

Problemas:
- Necesitabas instalar Docker
- Consumía recursos de tu PC
- Solo accesible localmente
- Backup manual
```

### ✅ AHORA (Cloud)

```
Tu PC → Internet → Neo4j AuraDB Cloud

Ventajas:
- ☁️  Sin instalación
- 🆓 Gratis 14 días
- 🌍 Acceso desde cualquier lugar
- 💾 Backup automático
```

---

## 🚀 TU SIGUIENTE PASO (3 minutos)

### 1️⃣ Obtener Credenciales

Abre tu navegador:
```
https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home
```

Click en tu instancia → Pestaña "Connect"

Copia:
- **Connection URI** (neo4j+s://abc123.databases.neo4j.io)
- **Password** (el que generó AuraDB)

---

### 2️⃣ Ejecutar Setup

```powershell
cd C:\proyectos\gh-code-intel-mvp\src
.\scripts\Setup-CodeIntel-AuraDB.ps1
```

El script te pedirá:
```
Pega el Connection URI aquí: ▌
Pega el password aquí: ▌
```

---

### 3️⃣ Inicializar Índices

El script te dará las queries para ejecutar en Neo4j Browser (cloud).

O abre:
```
Console → Tu instancia → "Open with" → "Neo4j Browser"
```

Ejecuta:
```cypher
CREATE CONSTRAINT repo_id IF NOT EXISTS FOR (r:Repository) REQUIRE r.id IS UNIQUE;
CREATE INDEX class_temporal IF NOT EXISTS FOR (c:Class) ON (c.validFrom, c.validTo);
// ... (el script te da todas las queries)
```

---

### 4️⃣ Iniciar y Probar

```powershell
cd CodeIntel.Functions
func start

# Deberías ver:
# Connected to Neo4j: neo4j+s://tu-instancia.databases.neo4j.io ✅
```

---

## 📖 Documentación Detallada

Si necesitas más ayuda:

**[CONFIGURACION_NEO4J_AURADB.md](CONFIGURACION_NEO4J_AURADB.md)** ⭐

Incluye:
- Paso a paso completo
- Screenshots conceptuales
- Troubleshooting
- Tips y trucos

---

## 🔧 Si Ya Tenías Docker/Neo4j Local

**Ya NO los necesitas.**

Puedes:
```powershell
# Detener contenedor Neo4j (si existe)
docker stop neo4j-codeintel

# O desinstalar Docker si no lo usas para otra cosa
```

---

## ⚙️ Configuración

**Archivo:** `CodeIntel.Functions/appsettings.json`

**Antes:**
```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",  // ❌ Local
    "Password": "codeintel123"
  }
}
```

**Ahora:**
```json
{
  "Neo4j": {
    "Uri": "neo4j+s://abc123.databases.neo4j.io",  // ✅ Cloud
    "Password": "TuPasswordDeAuraDB"
  }
}
```

⚠️ **IMPORTANTE:** El URI debe empezar con `neo4j+s://` (con "s")

---

## 💰 Costo

- **Primeros 14 días:** 🆓 **GRATIS**
- **Después de 14 días:**
  - Opción 1: Actualizar a plan pagado (~$0.10/hora = ~$70/mes)
  - Opción 2: Crear nueva instancia con otra cuenta (otro trial de 14 días)
  - Opción 3: Exportar datos y usar Neo4j local

---

## 🎓 Diferencias Clave

| Característica | Antes (Local) | Ahora (AuraDB) |
|----------------|---------------|----------------|
| **Instalación** | Docker/Neo4j Desktop | ☁️  Ninguna |
| **URI** | `bolt://localhost:7687` | `neo4j+s://xxx.databases.neo4j.io` |
| **Acceso** | Solo local | 🌍 Desde cualquier lugar |
| **Setup** | ~15 min | ~5 min |
| **Browser** | http://localhost:7474 | Console → "Open with" |
| **Backup** | Manual | 🔄 Automático |

---

## ✅ Checklist Rápido

Haz esto en orden:

1. [ ] Obtener URI de AuraDB (neo4j+s://...)
2. [ ] Obtener password de AuraDB
3. [ ] Ejecutar `.\scripts\Setup-CodeIntel-AuraDB.ps1`
4. [ ] Pegar credenciales cuando te las pida
5. [ ] Ejecutar queries en Neo4j Browser (cloud)
6. [ ] Verificar: `SHOW INDEXES` → Debe mostrar ~8 índices
7. [ ] Compilar: `dotnet build`
8. [ ] Iniciar: `func start`
9. [ ] Probar: Analizar un repo
10. [ ] Ver datos en Neo4j Browser (cloud)

---

## 🐛 Problema Común

### "Authentication failed"

**Causa:** Password incorrecto

**Solución:**
1. Ve al console Neo4j
2. Tu instancia → Settings → **"Reset password"**
3. Copia el NUEVO password
4. Actualiza `appsettings.json`
5. Reinicia `func start`

---

## 📞 Ayuda

**Guía completa:**
- [CONFIGURACION_NEO4J_AURADB.md](CONFIGURACION_NEO4J_AURADB.md)

**Console Neo4j:**
- https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home

**Documentación Neo4j AuraDB:**
- https://neo4j.com/docs/aura/

---

```
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║  ¡Ejecuta esto AHORA!                                          ║
║                                                                ║
║  .\scripts\Setup-CodeIntel-AuraDB.ps1                          ║
║                                                                ║
║  Duración: ~5 minutos                                          ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝
```

---

**Resumen en 3 puntos:**

1. 🌐 **Obtén credenciales:** https://console.neo4j.io (tu proyecto)
2. ⚙️  **Ejecuta setup:** `.\scripts\Setup-CodeIntel-AuraDB.ps1`
3. ✅ **Listo para usar:** Todo funciona igual, pero en la nube

**¡Disfruta de Neo4j sin complicaciones!** ☁️
