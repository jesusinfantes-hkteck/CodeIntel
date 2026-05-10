# AriadnaKnowledgeStore - Setup Script para Estrategia 1 (Versionado Temporal)
# Este script configura todo lo necesario para ejecutar AriadnaKnowledgeStore localmente

param(
    [switch]$SkipNeo4j,
    [switch]$SkipDotnet,
    [string]$Neo4jPassword = "AriadnaKnowledgeStore123"
)

$ErrorActionPreference = "Stop"

Write-Host @"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║           AriadnaKnowledgeStore Setup - Estrategia 1               ║
║           Versionado Temporal (Bitemporal)             ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host ""

# ============================================
# 1. Verificar prerequisitos
# ============================================
Write-Host "📋 Paso 1: Verificando prerequisitos..." -ForegroundColor Yellow
Write-Host ""

# Verificar .NET 8
if (-not $SkipDotnet) {
    Write-Host "  → Verificando .NET 8 SDK..." -NoNewline
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -match "^8\.") {
            Write-Host " ✅ $dotnetVersion" -ForegroundColor Green
        } else {
            Write-Host " ⚠️  Versión $dotnetVersion encontrada" -ForegroundColor Yellow
            Write-Host "     Se recomienda .NET 8. Instalando..." -ForegroundColor Yellow
            winget install Microsoft.DotNet.SDK.8
        }
    }
    catch {
        Write-Host " ❌" -ForegroundColor Red
        Write-Host "     .NET 8 SDK no encontrado. Instalando..." -ForegroundColor Yellow
        winget install Microsoft.DotNet.SDK.8
    }
}

# Verificar Azure Functions Core Tools
Write-Host "  → Verificando Azure Functions Core Tools..." -NoNewline
try {
    $funcVersion = func --version 2>$null
    Write-Host " ✅ v$funcVersion" -ForegroundColor Green
}
catch {
    Write-Host " ❌" -ForegroundColor Red
    Write-Host "     Instalando Azure Functions Core Tools..." -ForegroundColor Yellow
    npm install -g azure-functions-core-tools@4 --unsafe-perm true
}

# Verificar Docker (opcional para Neo4j)
if (-not $SkipNeo4j) {
    Write-Host "  → Verificando Docker..." -NoNewline
    try {
        $dockerVersion = docker --version 2>$null
        Write-Host " ✅ $dockerVersion" -ForegroundColor Green
        $hasDocker = $true
    }
    catch {
        Write-Host " ⚠️  No encontrado" -ForegroundColor Yellow
        Write-Host "     Docker es opcional. Puedes usar Neo4j Desktop." -ForegroundColor Gray
        $hasDocker = $false
    }
}

Write-Host ""

# ============================================
# 2. Clonar/Verificar repositorio
# ============================================
Write-Host "📦 Paso 2: Verificando repositorio..." -ForegroundColor Yellow
Write-Host ""

$repoPath = "C:\proyectos\gh-ariadna-knowledgestore-mvp\src"

if (Test-Path $repoPath) {
    Write-Host "  → Repositorio encontrado en: $repoPath" -ForegroundColor Green
    Set-Location $repoPath
} else {
    Write-Host "  ❌ Repositorio no encontrado en: $repoPath" -ForegroundColor Red
    Write-Host "     Por favor clona el repositorio primero:" -ForegroundColor Yellow
    Write-Host "     git clone https://github.com/jinfanteshk/AriadnaKnowledgeStore C:\proyectos\gh-ariadna-knowledgestore-mvp" -ForegroundColor Cyan
    exit 1
}

Write-Host ""

# ============================================
# 3. Restaurar paquetes NuGet
# ============================================
Write-Host "📥 Paso 3: Restaurando paquetes NuGet..." -ForegroundColor Yellow
Write-Host ""

dotnet restore

Write-Host ""

# ============================================
# 4. Configurar Neo4j
# ============================================
if (-not $SkipNeo4j) {
    Write-Host "🗄️  Paso 4: Configurando Neo4j..." -ForegroundColor Yellow
    Write-Host ""

    if ($hasDocker) {
        Write-Host "  → Iniciando Neo4j en Docker..." -ForegroundColor Cyan

        # Verificar si ya existe contenedor
        $existingContainer = docker ps -a --filter "name=neo4j-AriadnaKnowledgeStore" --format "{{.Names}}"

        if ($existingContainer) {
            Write-Host "     Contenedor existente encontrado. Iniciando..." -ForegroundColor Yellow
            docker start neo4j-AriadnaKnowledgeStore
        } else {
            Write-Host "     Creando nuevo contenedor..." -ForegroundColor Yellow
            docker run -d `
                --name neo4j-AriadnaKnowledgeStore `
                -p 7474:7474 -p 7687:7687 `
                -e NEO4J_AUTH=neo4j/$Neo4jPassword `
                -e NEO4J_ACCEPT_LICENSE_AGREEMENT=yes `
                -v neo4j-data:/data `
                -v neo4j-logs:/logs `
                neo4j:5-community
        }

        Write-Host ""
        Write-Host "  ✅ Neo4j corriendo en:" -ForegroundColor Green
        Write-Host "     Browser: http://localhost:7474" -ForegroundColor Cyan
        Write-Host "     Bolt: bolt://localhost:7687" -ForegroundColor Cyan
        Write-Host "     User: neo4j" -ForegroundColor Cyan
        Write-Host "     Password: $Neo4jPassword" -ForegroundColor Cyan

        # Esperar a que Neo4j esté listo
        Write-Host ""
        Write-Host "  ⏳ Esperando a que Neo4j esté listo..." -NoNewline
        $retries = 0
        $maxRetries = 30

        do {
            Start-Sleep -Seconds 2
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:7474" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Host " ✅" -ForegroundColor Green
                    break
                }
            }
            catch {
                Write-Host "." -NoNewline
                $retries++
            }
        } while ($retries -lt $maxRetries)

        if ($retries -ge $maxRetries) {
            Write-Host " ⚠️  Timeout" -ForegroundColor Yellow
            Write-Host "     Neo4j puede tardar un poco más en iniciarse." -ForegroundColor Yellow
        }

        Write-Host ""

    } else {
        Write-Host "  ⚠️  Docker no disponible." -ForegroundColor Yellow
        Write-Host "     Opciones:" -ForegroundColor Yellow
        Write-Host "     1. Instalar Docker Desktop: https://www.docker.com/products/docker-desktop/" -ForegroundColor Cyan
        Write-Host "     2. Instalar Neo4j Desktop: https://neo4j.com/download/" -ForegroundColor Cyan
        Write-Host ""

        $continue = Read-Host "  ¿Ya tienes Neo4j corriendo? (y/n)"
        if ($continue -ne "y") {
            Write-Host "  ⏸️  Setup pausado. Configura Neo4j y vuelve a ejecutar." -ForegroundColor Yellow
            exit 0
        }
    }
}

Write-Host ""

# ============================================
# 5. Inicializar base de datos Neo4j
# ============================================
Write-Host "🔧 Paso 5: Inicializando esquema de Neo4j..." -ForegroundColor Yellow
Write-Host ""

# Ejecutar script de inicialización
$initScript = Join-Path $repoPath "scripts\Initialize-Neo4j-Versioned.ps1"

if (Test-Path $initScript) {
    Write-Host "  → Ejecutando script de inicialización..." -ForegroundColor Cyan
    & $initScript -Password $Neo4jPassword
} else {
    Write-Host "  ⚠️  Script de inicialización no encontrado" -ForegroundColor Yellow
    Write-Host "     Creando índices manualmente..." -ForegroundColor Yellow

    # Crear archivo .cypher con las queries
    $cypherFile = Join-Path $repoPath "neo4j-init.cypher"

    @"
// Constraints
CREATE CONSTRAINT repo_id IF NOT EXISTS FOR (r:Repository) REQUIRE r.id IS UNIQUE;
CREATE CONSTRAINT version_id IF NOT EXISTS FOR (v:Version) REQUIRE v.id IS UNIQUE;

// Índices temporales
CREATE INDEX class_temporal IF NOT EXISTS FOR (c:Class) ON (c.validFrom, c.validTo);
CREATE INDEX method_temporal IF NOT EXISTS FOR (m:Method) ON (m.validFrom, m.validTo);
CREATE INDEX class_version_id IF NOT EXISTS FOR (c:Class) ON (c.versionId);
CREATE INDEX method_version_id IF NOT EXISTS FOR (m:Method) ON (m.versionId);
CREATE INDEX version_current IF NOT EXISTS FOR (v:Version) ON (v.isCurrent);

// Verificar
SHOW CONSTRAINTS;
SHOW INDEXES;
"@ | Out-File -FilePath $cypherFile -Encoding UTF8

    Write-Host "  ✅ Queries guardadas en: $cypherFile" -ForegroundColor Green
    Write-Host "     Ejecuta manualmente en Neo4j Browser: http://localhost:7474" -ForegroundColor Cyan
}

Write-Host ""

# ============================================
# 6. Configurar appsettings
# ============================================
Write-Host "⚙️  Paso 6: Configurando appsettings..." -ForegroundColor Yellow
Write-Host ""

$functionsPath = Join-Path $repoPath "AriadnaKnowledgeStore.Functions"
$appsettingsPath = Join-Path $functionsPath "appsettings.json"
$appsettingsDevPath = Join-Path $functionsPath "appsettings.Development.json"

# Leer appsettings actual
$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json

# Verificar si tiene GitHub token
if ([string]::IsNullOrWhiteSpace($appsettings.GitHub.Token)) {
    Write-Host "  ⚠️  GitHub Token no configurado" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "     Para analizar repositorios de GitHub necesitas un Personal Access Token:" -ForegroundColor Gray
    Write-Host "     1. Ve a: https://github.com/settings/tokens" -ForegroundColor Cyan
    Write-Host "     2. Generate new token (classic)" -ForegroundColor Cyan
    Write-Host "     3. Permisos: repo (full control)" -ForegroundColor Cyan
    Write-Host "     4. Copia el token generado" -ForegroundColor Cyan
    Write-Host ""

    $githubToken = Read-Host "  Pega tu GitHub token aquí (o Enter para omitir)"

    if (-not [string]::IsNullOrWhiteSpace($githubToken)) {
        $appsettings.GitHub.Token = $githubToken
    }
}

# Asegurar que GraphStore.Type sea Neo4jVersioned
$appsettings.GraphStore.Type = "Neo4jVersioned"

# Configurar Neo4j
$appsettings.Neo4j.Password = $Neo4jPassword

# Guardar appsettings.Development.json
$appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsDevPath

Write-Host "  ✅ Configuración guardada en: appsettings.Development.json" -ForegroundColor Green
Write-Host ""

# ============================================
# 7. Build del proyecto
# ============================================
Write-Host "🔨 Paso 7: Compilando proyecto..." -ForegroundColor Yellow
Write-Host ""

dotnet build --configuration Debug

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "  ❌ Error en compilación" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ============================================
# 8. Resumen y próximos pasos
# ============================================
Write-Host @"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║              ✅  Setup Completado!                     ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

Write-Host ""
Write-Host "📋 Configuración:" -ForegroundColor Cyan
Write-Host "   • Estrategia: Versionado Temporal (Estrategia 1)" -ForegroundColor White
Write-Host "   • Neo4j: bolt://localhost:7687" -ForegroundColor White
Write-Host "   • User: neo4j" -ForegroundColor White
Write-Host "   • Password: $Neo4jPassword" -ForegroundColor White
Write-Host ""

Write-Host "🚀 Próximos pasos:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   1. Iniciar Azure Functions:" -ForegroundColor White
Write-Host "      cd $functionsPath" -ForegroundColor Gray
Write-Host "      func start" -ForegroundColor Cyan
Write-Host ""
Write-Host "   2. Probar análisis de repositorio:" -ForegroundColor White
Write-Host "      `$body = @{ owner='microsoft'; repo='dotnet'; branch='main' } | ConvertTo-Json" -ForegroundColor Gray
Write-Host "      Invoke-RestMethod -Uri http://localhost:7071/api/ingest -Method POST -Body `$body -ContentType 'application/json'" -ForegroundColor Cyan
Write-Host ""
Write-Host "   3. Ver versiones:" -ForegroundColor White
Write-Host "      Invoke-RestMethod -Uri http://localhost:7071/api/repo/microsoft/dotnet/main/versions" -ForegroundColor Cyan
Write-Host ""
Write-Host "   4. Explorar Neo4j Browser:" -ForegroundColor White
Write-Host "      http://localhost:7474" -ForegroundColor Cyan
Write-Host ""

Write-Host "📚 Documentación:" -ForegroundColor Cyan
Write-Host "   • README.md - Guía completa" -ForegroundColor White
Write-Host "   • docs/Guia_Uso_Versionado.md - Ejemplos de uso" -ForegroundColor White
Write-Host "   • docs/Versionado_y_Rollback_Neo4j.md - Detalles técnicos" -ForegroundColor White
Write-Host ""

Write-Host "💡 Tips:" -ForegroundColor Yellow
Write-Host "   • Para ver logs: docker logs -f neo4j-AriadnaKnowledgeStore" -ForegroundColor Gray
Write-Host "   • Para detener Neo4j: docker stop neo4j-AriadnaKnowledgeStore" -ForegroundColor Gray
Write-Host "   • Para reiniciar: docker start neo4j-AriadnaKnowledgeStore" -ForegroundColor Gray
Write-Host ""

Write-Host "¡Listo para usar AriadnaKnowledgeStore! 🎉" -ForegroundColor Green
Write-Host ""
