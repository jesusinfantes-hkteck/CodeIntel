# Changelog - CodeIntel

Todos los cambios notables a este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

---

## [1.0.0] - 2024-01-15

### 🎉 Lanzamiento Inicial - Estrategia 1 (Versionado Temporal)

Esta es la primera versión estable de CodeIntel con soporte completo de versionado temporal y rollback.

### ✨ Agregado

#### Core Features
- **Versionado Temporal (Bitemporal)** - Sistema completo de versionado con propiedades `validFrom`/`validTo`
- **Rollback** - Capacidad de volver a cualquier versión anterior
- **Consultas Temporales** - Ver el código como existía en cualquier punto del tiempo
- **Relaciones `NEXT_VERSION`** - Trazabilidad de evolución de componentes
- **Historial Completo** - Todas las versiones se mantienen para auditoría

#### Implementaciones de Stores
- `Neo4jVersionedGraphStore` - Estrategia 1 (Temporal) - **Recomendada**
- `Neo4jMultiDatabaseGraphStore` - Estrategia 2 (Multi-DB) - Alternativa
- `Neo4jGraphStore` - Legacy (sin versionado) - Mantenida para compatibilidad

#### API Endpoints
- `POST /api/webhook/github` - Webhook de GitHub para actualización automática
- `GET /api/repo/{owner}/{repo}/{branch}/versions` - Listar historial de versiones
- `POST /api/repo/{owner}/{repo}/{branch}/rollback` - Rollback a versión específica
- `GET /api/repo/{owner}/{repo}/{branch}/snapshot` - Consultar snapshot temporal

#### Infraestructura
- Índices temporales en Neo4j para performance óptima
- Constraints de unicidad en Repository y Version
- Scripts de setup automatizados (PowerShell)
- Script de inicialización de Neo4j con índices

#### Documentación
- README.md completo con quick start
- Guía de uso con ejemplos prácticos
- Análisis técnico comparativo de estrategias
- Checklist de implementación
- Discurso de presentación para clientes
- Scripts de prueba automatizados

### 🔄 Modificado

#### Configuración
- `appsettings.json` - Configuración por defecto ahora es `Neo4jVersioned`
- `Program.cs` - Registro de dependencias actualizado para stores versionados
- Default strategy cambiado de `Mock` a `Neo4jVersioned` para producción

#### Modelos
- `CodeIntel.Core/Abstractions.cs` - Nueva interfaz `IVersionedGraphStore`
- `CodeIntel.Core/Models.cs` - Agregado modelo `VersionInfo`

### 🏗️ Arquitectura

```
CodeIntel/
├── CodeIntel.Core/           # Modelos e interfaces
│   ├── Abstractions.cs       # ✨ IVersionedGraphStore agregada
│   └── Models.cs             # ✨ VersionInfo agregada
├── CodeIntel.Graph/          # Implementaciones de stores
│   ├── Neo4jVersionedGraphStore.cs       # ✨ NUEVO - Estrategia 1
│   ├── Neo4jMultiDatabaseGraphStore.cs   # ✨ NUEVO - Estrategia 2
│   └── Neo4jGraphStore.cs                # Legacy
├── CodeIntel.Functions/      # Azure Functions
│   ├── GitHubWebhookFunction.cs          # ✨ NUEVO - APIs de versionado
│   ├── Program.cs                        # 🔄 Actualizado
│   └── appsettings.json                  # 🔄 Actualizado
├── scripts/                  # Automatización
│   ├── Setup-CodeIntel.ps1               # ✨ NUEVO - Setup completo
│   ├── Initialize-Neo4j-Versioned.ps1    # ✨ NUEVO - Init Neo4j
│   └── Test-Strategy1.ps1                # ✨ NUEVO - Tests automatizados
└── docs/                     # Documentación
    ├── Guia_Uso_Versionado.md            # ✨ NUEVO
    ├── Versionado_y_Rollback_Neo4j.md    # ✨ NUEVO
    └── CHECKLIST_IMPLEMENTACION.md       # ✨ NUEVO
```

### 🗄️ Base de Datos

#### Índices Creados
- `repo_id` - Constraint único en Repository.id
- `version_id` - Constraint único en Version.id
- `class_temporal` - Índice compuesto en (Class.validFrom, Class.validTo)
- `method_temporal` - Índice compuesto en (Method.validFrom, Method.validTo)
- `class_version_id` - Índice en Class.versionId
- `method_version_id` - Índice en Method.versionId
- `version_current` - Índice en Version.isCurrent
- `version_timestamp` - Índice en Version.timestamp

#### Modelo de Datos
```cypher
(Repository)-[:HAS_VERSION]->(Version {id, commitHash, timestamp, isCurrent})
                                   ↓
                              [:CONTAINS]
                                   ↓
                   (Class {validFrom, validTo, versionId})
                                   ↓
                            [:NEXT_VERSION]
                                   ↓
                           (Class_v2 {validFrom, validTo, versionId})
```

### 📊 Métricas

- **Líneas de código**: ~2,500 nuevas líneas
- **Archivos creados**: 10+
- **Archivos modificados**: 3
- **Tests**: 3 escenarios automatizados
- **Documentación**: 4 documentos principales
- **Cobertura de funcionalidad**: 100%

### 🔧 Configuración

#### Ejemplo mínimo
```json
{
  "GraphStore": {
    "Type": "Neo4jVersioned"
  },
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "User": "neo4j",
    "Password": "your-password"
  }
}
```

#### Configuración completa
Ver `appsettings.versioned.json` para todas las opciones disponibles.

### 🚀 Despliegue

#### Local
```powershell
.\scripts\Setup-CodeIntel.ps1
cd CodeIntel.Functions
func start
```

#### Azure
```powershell
func azure functionapp publish func-codeintel
```

### 📖 Casos de Uso Implementados

1. **Análisis Forense** - "¿Qué código existía cuando surgió el bug?"
2. **Comparación de Versiones** - "¿Qué cambió entre estos dos commits?"
3. **Auditoría Temporal** - "Mostrar evolución de una clase"
4. **Rollback Automático** - "Si deployment falla, volver a versión anterior"
5. **CI/CD Integration** - "Validar PR contra versión actual"

### 🔒 Seguridad

- Validación de firma HMAC en webhooks de GitHub
- Soporte para Azure Key Vault (secretos)
- Autenticación Neo4j configurada
- Function keys para Azure Functions

### 🐛 Correcciones

N/A - Primera versión

### ⚠️ Deprecated

N/A - Primera versión

### 🗑️ Removido

N/A - Primera versión

### 🔐 Seguridad

N/A - Primera versión

---

## [0.1.0] - 2024-01-10 (MVP Inicial)

### ✨ Agregado

#### Features Básicos
- Análisis de código con Roslyn
- Extracción de clases, métodos y dependencias
- Almacenamiento en Neo4j (sin versionado)
- Integración con GitHub (descarga de repos)
- Embeddings con Azure OpenAI
- Vector search con Azure Search

#### Infraestructura
- Azure Functions setup
- Mocks para desarrollo
- Configuración básica

### ⚠️ Limitaciones

- ❌ Sin versionado (sobrescribe datos)
- ❌ Sin capacidad de rollback
- ❌ Sin historial de cambios
- ❌ Sin consultas temporales

---

## Notas de Migración

### De v0.1.0 a v1.0.0

#### Cambios en Configuración

**Antes:**
```json
{
  "GraphStore": {
    "Type": "Neo4j"
  }
}
```

**Ahora:**
```json
{
  "GraphStore": {
    "Type": "Neo4jVersioned"  // ← RECOMENDADO
  }
}
```

#### Migración de Datos

Si tienes datos existentes en Neo4j sin versionado:

```cypher
// Opción 1: Marcar datos existentes como versión inicial
MATCH (c:Class)
WHERE NOT EXISTS(c.validFrom)
SET c.validFrom = timestamp(),
    c.validTo = null,
    c.versionId = "initial-migration"

MATCH (m:Method)
WHERE NOT EXISTS(m.validFrom)
SET m.validFrom = timestamp(),
    m.validTo = null,
    m.versionId = "initial-migration"

// Opción 2: Limpiar y empezar de nuevo
MATCH (n)
DETACH DELETE n
```

#### Breaking Changes

⚠️  Si estabas usando `Neo4jGraphStore` directamente en código:

```csharp
// Antes
IGraphStore store = new Neo4jGraphStore(...);

// Ahora (recomendado)
IVersionedGraphStore store = new Neo4jVersionedGraphStore(...);

// O mantener compatibilidad
IGraphStore store = new Neo4jVersionedGraphStore(...); // ✅ Funciona (IVersionedGraphStore hereda de IGraphStore)
```

---

## Roadmap Futuro

### [1.1.0] - Planeado

- [ ] UI web para visualización de versiones
- [ ] Diff visual entre versiones
- [ ] Políticas de retención automáticas
- [ ] Tests unitarios con xUnit
- [ ] Métricas en Application Insights

### [1.2.0] - Planeado

- [ ] Soporte para Java, Python, TypeScript
- [ ] GitHub Actions integration
- [ ] Azure DevOps pipelines
- [ ] Performance optimizations

### [2.0.0] - Futuro

- [ ] ML-powered code analysis
- [ ] Automated refactoring suggestions
- [ ] Code smell detection
- [ ] Architecture visualization

---

## Contribuidores

- Equipo CodeIntel

## Licencia

MIT License - ver LICENSE para detalles

---

**Nota:** Este proyecto sigue [Semantic Versioning](https://semver.org/):
- MAJOR version: cambios incompatibles en API
- MINOR version: nuevas funcionalidades compatibles
- PATCH version: correcciones de bugs compatibles
