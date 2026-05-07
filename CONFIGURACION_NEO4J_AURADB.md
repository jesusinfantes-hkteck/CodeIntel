# 🌐 Guía de Configuración - Neo4j AuraDB (Cloud)

Esta guía te ayuda a configurar CodeIntel con **Neo4j AuraDB Cloud** (14 días gratis) en lugar de Neo4j local.

---

## ✅ Ventajas de Neo4j AuraDB

- ✅ **Sin instalación local** - No necesitas Docker ni Neo4j Desktop
- ✅ **Gratis 14 días** - Ideal para pruebas y desarrollo
- ✅ **Fully managed** - Neo4j se encarga del mantenimiento
- ✅ **Acceso desde cualquier lugar** - Solo necesitas internet
- ✅ **Backup automático** - Tus datos están seguros
- ✅ **Alta disponibilidad** - 99.95% uptime SLA

---

## 🚀 Paso 1: Crear Instancia en Neo4j AuraDB

### 1.1 Ir al Console de Neo4j

Abre tu navegador y ve a:

```
https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home
```

**Nota:** Si no estás logueado, haz login con tu cuenta de Neo4j.

### 1.2 Crear Nueva Instancia (Si no existe)

1. Click en **"New Instance"** o **"Create instance"**
2. Selecciona **"AuraDB Free"** (14-day trial)
3. Configuración:
   - **Name:** `codeintel-dev` (o el nombre que prefieras)
   - **Region:** Selecciona el más cercano a ti
     - Europa: `europe-west1` (Bélgica)
     - USA: `us-east1` (Carolina del Sur)
     - Asia: `asia-southeast1` (Singapur)
   - **Database version:** 5.x (latest)
4. Click **"Create instance"**

### 1.3 ⚠️ IMPORTANTE: Copiar Password

**Cuando se cree la instancia, aparecerá una ventana con el password.**

```
⚠️  ESTE PASSWORD SOLO SE MUESTRA UNA VEZ
    CÓPIALO AHORA Y GUÁRDALO EN UN LUGAR SEGURO
```

Ejemplo de password:
```
VeryLongGeneratedPassword123!@#$%^&*()
```

**Cópialo y pégalo en un archivo temporal o en appsettings.json inmediatamente.**

---

## 🔑 Paso 2: Obtener Credenciales

### 2.1 Connection URI

1. En el console, click en tu instancia
2. Ve a la pestaña **"Connect"**
3. Copia el **Connection URI**

Ejemplo:
```
neo4j+s://abc12345.databases.neo4j.io
```

**Nota el protocolo:** `neo4j+s://` (con "s" de secure/SSL)

### 2.2 Username

El username siempre es:
```
neo4j
```

### 2.3 Password

Es el password que copiaste en el paso 1.3.

**Si lo perdiste:**
1. Ve a tu instancia
2. Settings → **"Reset password"**
3. Copia el NUEVO password inmediatamente

---

## ⚙️ Paso 3: Configurar appsettings.json

### 3.1 Abrir archivo de configuración

```powershell
cd C:\proyectos\gh-code-intel-mvp\src\CodeIntel.Functions
code appsettings.json
```

### 3.2 Actualizar credenciales de Neo4j

Reemplaza la sección `Neo4j` con tus credenciales:

```json
{
  "Neo4j": {
    "Uri": "neo4j+s://abc12345.databases.neo4j.io",
    "User": "neo4j",
    "Password": "TuPasswordGeneradoPorAuraDB"
  }
}
```

**Ejemplo real:**
```json
{
  "Neo4j": {
    "Uri": "neo4j+s://c8f3e2a1.databases.neo4j.io",
    "User": "neo4j",
    "Password": "VeryLongGeneratedPassword123!@#$%"
  }
}
```

### 3.3 Verificar GraphStore Type

Asegúrate de que esté configurado para usar versionado:

```json
{
  "GraphStore": {
    "Type": "Neo4jVersioned"
  }
}
```

### 3.4 Configuración completa de ejemplo

```json
{
  "GitHub": {
    "Token": "ghp_tu_github_token_aqui"
  },
  "GraphStore": {
    "Type": "Neo4jVersioned"
  },
  "Neo4j": {
    "Uri": "neo4j+s://abc12345.databases.neo4j.io",
    "User": "neo4j",
    "Password": "TuPasswordAuraDB"
  },
  "VersionManagement": {
    "RetentionDays": 90,
    "AutoCleanup": false
  }
}
```

---

## 🔧 Paso 4: Inicializar Base de Datos

### 4.1 Usar Neo4j Browser en la nube

1. Ve a tu instancia en el console
2. Click en **"Open with"** → **"Neo4j Browser"**
3. Se abrirá el navegador web con Neo4j Browser
4. Ya está conectado automáticamente

### 4.2 Ejecutar queries de inicialización

Copia y pega estas queries en Neo4j Browser:

```cypher
// ===================================================
// CodeIntel - Índices y Constraints para Neo4j AuraDB
// Estrategia 1: Versionado Temporal
// ===================================================

// 1. CONSTRAINTS - Unicidad
CREATE CONSTRAINT repo_id IF NOT EXISTS
FOR (r:Repository) REQUIRE r.id IS UNIQUE;

CREATE CONSTRAINT version_id IF NOT EXISTS
FOR (v:Version) REQUIRE v.id IS UNIQUE;

// 2. INDICES - Performance (CRÍTICOS para versionado temporal)
CREATE INDEX class_temporal IF NOT EXISTS
FOR (c:Class) ON (c.validFrom, c.validTo);

CREATE INDEX method_temporal IF NOT EXISTS
FOR (m:Method) ON (m.validFrom, m.validTo);

// 3. INDICES - Búsquedas
CREATE INDEX class_version_id IF NOT EXISTS
FOR (c:Class) ON (c.versionId);

CREATE INDEX method_version_id IF NOT EXISTS
FOR (m:Method) ON (m.versionId);

CREATE INDEX class_repo_id IF NOT EXISTS
FOR (c:Class) ON (c.repoId);

CREATE INDEX method_repo_id IF NOT EXISTS
FOR (m:Method) ON (m.repoId);

CREATE INDEX version_current IF NOT EXISTS
FOR (v:Version) ON (v.isCurrent);

CREATE INDEX version_timestamp IF NOT EXISTS
FOR (v:Version) ON (v.timestamp);

// 4. VERIFICACIÓN
SHOW CONSTRAINTS;
SHOW INDEXES;
```

**Ejecuta cada bloque** (o todo junto) y verifica que se crean sin errores.

---

## ✅ Paso 5: Verificar Conexión

### 5.1 Desde Visual Studio / PowerShell

```powershell
cd C:\proyectos\gh-code-intel-mvp\src

# Compilar
dotnet build

# Iniciar Functions
cd CodeIntel.Functions
func start
```

### 5.2 Verificar logs

Deberías ver algo como:

```
[2024-01-15T10:30:00.123Z] Starting Azure Functions...
[2024-01-15T10:30:01.456Z] Connected to Neo4j: neo4j+s://abc12345.databases.neo4j.io
```

**Si ves errores de conexión:**
- Verifica que el URI sea correcto (debe empezar con `neo4j+s://`)
- Verifica que el password sea correcto
- Verifica que la instancia esté "Running" en el console

---

## 🧪 Paso 6: Probar

### 6.1 Análisis de repositorio de prueba

```powershell
$body = @{
    owner = "octocat"
    repo = "Hello-World"
    branch = "master"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri http://localhost:7071/api/ingest `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

### 6.2 Ver resultados en Neo4j Browser

1. Vuelve a Neo4j Browser (en la nube)
2. Ejecuta:

```cypher
// Ver repositorios
MATCH (r:Repository)
RETURN r

// Ver versiones
MATCH (v:Version)
RETURN v.id, v.timestamp, v.isCurrent

// Ver clases
MATCH (c:Class)
RETURN c.name, c.namespace, c.validFrom, c.validTo
LIMIT 10
```

**Si ves datos:** ✅ ¡Todo funciona!

---

## 🔐 Seguridad

### Mejores Prácticas

1. **No commitear passwords en Git**
   ```powershell
   # Crear appsettings.Development.json (gitignored)
   cp appsettings.json appsettings.Development.json
   # Editar appsettings.Development.json con tus credenciales
   ```

2. **Usar variables de entorno en producción**
   ```json
   {
     "Neo4j": {
       "Uri": "%NEO4J_URI%",
       "Password": "%NEO4J_PASSWORD%"
     }
   }
   ```

3. **Rotar password regularmente**
   - Cada 30 días en desarrollo
   - Usar Azure Key Vault en producción

---

## 📊 Monitoreo

### Ver uso de tu instancia AuraDB

1. Ve al console: https://console.neo4j.io
2. Click en tu instancia
3. Pestaña **"Metrics"**

Verás:
- **Storage usado** (max 200k nodes en free tier)
- **Queries ejecutadas**
- **Conexiones activas**

### Límites del Free Tier (14 días)

| Recurso | Límite |
|---------|--------|
| **Duración** | 14 días |
| **Nodes** | ~200,000 |
| **Storage** | ~8 GB |
| **RAM** | 512 MB |
| **Conexiones** | 50 concurrent |
| **Backup** | Automático |

**Nota:** Después de 14 días:
- Puedes actualizar a plan pagado
- O crear nueva instancia free (con otra cuenta)

---

## 🐛 Troubleshooting

### Error: "Unable to connect to Neo4j"

**Causas comunes:**

1. **URI incorrecto**
   ```json
   ❌ "Uri": "bolt://localhost:7687"  // Local, no funciona
   ❌ "Uri": "neo4j://abc.databases.neo4j.io"  // Sin SSL
   ✅ "Uri": "neo4j+s://abc.databases.neo4j.io"  // Correcto
   ```

2. **Password incorrecto**
   - Resetea el password en el console
   - Copia el nuevo password
   - Actualiza appsettings.json

3. **Instancia pausada/detenida**
   - Ve al console
   - Verifica que esté "Running"
   - Si está paused, click "Resume"

4. **Firewall bloqueando conexión**
   - Neo4j AuraDB usa puerto 7687 sobre TLS
   - Verifica que no esté bloqueado

### Error: "Authentication failed"

**Solución:**
```powershell
# 1. Ve al console Neo4j
# 2. Click en tu instancia
# 3. Settings → Reset password
# 4. Copia el nuevo password
# 5. Actualiza appsettings.json
```

### Error: "Database is empty / No indexes found"

**Solución:**
```cypher
// Ejecutar en Neo4j Browser
SHOW INDEXES;

// Si no hay índices, ejecutar script de inicialización
// (ver Paso 4.2 arriba)
```

---

## 💡 Tips

### 1. Acceso rápido a Neo4j Browser

Guarda este bookmark:
```
https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home
```

### 2. Backup de datos

AuraDB Free hace backup automático, pero puedes exportar:

```cypher
// Exportar a CSV
CALL apoc.export.csv.all("backup.csv", {})
```

### 3. Limpiar datos de prueba

```cypher
// Eliminar todo (cuidado!)
MATCH (n)
DETACH DELETE n
```

### 4. Ver estadísticas

```cypher
// Contar nodos por tipo
MATCH (n)
RETURN labels(n) AS type, count(n) AS count
ORDER BY count DESC
```

---

## 📚 Recursos Adicionales

- **Neo4j AuraDB Docs:** https://neo4j.com/docs/aura/
- **Pricing:** https://neo4j.com/pricing/
- **Support:** https://support.neo4j.com/

---

## ✅ Checklist de Configuración

Antes de continuar, verifica:

- [ ] Tengo una instancia creada en Neo4j AuraDB
- [ ] He copiado el Connection URI (neo4j+s://...)
- [ ] He copiado el password (y lo guardé en lugar seguro)
- [ ] He actualizado `appsettings.json` con las credenciales
- [ ] He ejecutado el script de inicialización en Neo4j Browser
- [ ] He verificado que los índices se crearon (SHOW INDEXES)
- [ ] He compilado el proyecto sin errores (dotnet build)
- [ ] He iniciado Functions (func start)
- [ ] He probado un análisis de repositorio
- [ ] Veo datos en Neo4j Browser

---

## 🎉 ¡Listo!

Ahora tienes CodeIntel conectado a **Neo4j AuraDB Cloud** con:

- ✅ Versionado temporal (Estrategia 1)
- ✅ Sin instalación local
- ✅ Accesible desde cualquier lugar
- ✅ Gratis por 14 días

**Siguiente paso:** [GETTING_STARTED.md](GETTING_STARTED.md) - Continuar con el uso normal

---

*Generado para CodeIntel v1.0.0 con Neo4j AuraDB Cloud*
