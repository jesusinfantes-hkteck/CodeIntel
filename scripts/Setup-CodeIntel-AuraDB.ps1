# CodeIntel - Setup Script para Neo4j AuraDB (Cloud)
# Este script configura CodeIntel para usar Neo4j en la nube

param(
    [switch]$SkipDotnet,
    [string]$Neo4jUri = "",
    [string]$Neo4jPassword = ""
)

$ErrorActionPreference = "Stop"

Write-Host @"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║     CodeIntel Setup - Neo4j AuraDB (Cloud)             ║
║     Estrategia 1: Versionado Temporal                  ║
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

Write-Host ""

# ============================================
# 2. Verificar repositorio
# ============================================
Write-Host "📦 Paso 2: Verificando repositorio..." -ForegroundColor Yellow
Write-Host ""

$repoPath = "C:\proyectos\gh-code-intel-mvp\src"

if (Test-Path $repoPath) {
    Write-Host "  → Repositorio encontrado en: $repoPath" -ForegroundColor Green
    Set-Location $repoPath
} else {
    Write-Host "  ❌ Repositorio no encontrado en: $repoPath" -ForegroundColor Red
    Write-Host "     Ajusta la ruta o clona el repositorio" -ForegroundColor Yellow
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
# 4. Configurar Neo4j AuraDB
# ============================================
Write-Host "☁️  Paso 4: Configurando Neo4j AuraDB (Cloud)..." -ForegroundColor Yellow
Write-Host ""

Write-Host "  📖 Neo4j AuraDB - Ventajas:" -ForegroundColor Cyan
Write-Host "     • Sin instalación local (no necesitas Docker)" -ForegroundColor Gray
Write-Host "     • Gratis por 14 días" -ForegroundColor Gray
Write-Host "     • Fully managed por Neo4j" -ForegroundColor Gray
Write-Host "     • Acceso desde cualquier lugar" -ForegroundColor Gray
Write-Host ""

# Si no se proporcionaron credenciales como parámetros, pedirlas
if ([string]::IsNullOrWhiteSpace($Neo4jUri)) {
    Write-Host "  🌐 Necesitas credenciales de Neo4j AuraDB" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  📋 INSTRUCCIONES:" -ForegroundColor Cyan
    Write-Host "     1. Abre: https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home" -ForegroundColor White
    Write-Host "     2. Click en tu instancia (o crea una nueva si no existe)" -ForegroundColor White
    Write-Host "     3. Ve a 'Connect' y copia el Connection URI" -ForegroundColor White
    Write-Host "        (Ejemplo: neo4j+s://abc12345.databases.neo4j.io)" -ForegroundColor Gray
    Write-Host ""

    $Neo4jUri = Read-Host "  Pega el Connection URI aquí"

    Write-Host ""
    Write-Host "     4. Copia el password (generado cuando creaste la instancia)" -ForegroundColor White
    Write-Host "        Si lo perdiste: Settings → Reset password" -ForegroundColor Gray
    Write-Host ""

    $Neo4jPassword = Read-Host "  Pega el password aquí" -AsSecureString
    $Neo4jPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Neo4jPassword)
    )
}

# Validar URI
if (-not $Neo4jUri.StartsWith("neo4j+s://")) {
    Write-Host ""
    Write-Host "  ⚠️  ADVERTENCIA: El URI debe empezar con 'neo4j+s://' para AuraDB" -ForegroundColor Yellow
    Write-Host "     URI proporcionado: $Neo4jUri" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "  ¿Continuar de todas formas? (y/n)"
    if ($continue -ne "y") {
        exit 1
    }
}

Write-Host ""
Write-Host "  ✅ Credenciales de Neo4j AuraDB configuradas" -ForegroundColor Green
Write-Host "     URI: $Neo4jUri" -ForegroundColor Gray
Write-Host "     User: neo4j" -ForegroundColor Gray
Write-Host "     Password: $('*' * $Neo4jPassword.Length)" -ForegroundColor Gray
Write-Host ""

# ============================================
# 5. Probar conexión a Neo4j AuraDB
# ============================================
Write-Host "🔌 Paso 5: Probando conexión a Neo4j AuraDB..." -ForegroundColor Yellow
Write-Host ""

Write-Host "  💡 Para verificar la conexión, usaremos la compilación del proyecto" -ForegroundColor Cyan
Write-Host "     (La conexión real se probará cuando inicies func start)" -ForegroundColor Gray
Write-Host ""

# ============================================
# 6. Configurar appsettings
# ============================================
Write-Host "⚙️  Paso 6: Configurando appsettings.json..." -ForegroundColor Yellow
Write-Host ""

$functionsPath = Join-Path $repoPath "CodeIntel.Functions"
$appsettingsPath = Join-Path $functionsPath "appsettings.json"
$appsettingsDevPath = Join-Path $functionsPath "appsettings.Development.json"

# Leer appsettings actual
$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json

# Configurar Neo4j AuraDB
$appsettings.Neo4j.Uri = $Neo4jUri
$appsettings.Neo4j.User = "neo4j"
$appsettings.Neo4j.Password = $Neo4jPassword

# Asegurar que GraphStore.Type sea Neo4jVersioned
$appsettings.GraphStore.Type = "Neo4jVersioned"

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
# 8. Instrucciones para inicializar Neo4j
# ============================================
Write-Host "📝 Paso 8: Inicializar Neo4j AuraDB..." -ForegroundColor Yellow
Write-Host ""

Write-Host "  Para crear índices en Neo4j AuraDB:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Abre Neo4j Browser en la nube:" -ForegroundColor White
Write-Host "     https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Click en tu instancia → 'Open with' → 'Neo4j Browser'" -ForegroundColor White
Write-Host ""
Write-Host "  3. Copia y pega estas queries:" -ForegroundColor White
Write-Host ""

$cypherQueries = @"
-- Constraints
CREATE CONSTRAINT repo_id IF NOT EXISTS FOR (r:Repository) REQUIRE r.id IS UNIQUE;
CREATE CONSTRAINT version_id IF NOT EXISTS FOR (v:Version) REQUIRE v.id IS UNIQUE;

-- Índices temporales (CRÍTICOS)
CREATE INDEX class_temporal IF NOT EXISTS FOR (c:Class) ON (c.validFrom, c.validTo);
CREATE INDEX method_temporal IF NOT EXISTS FOR (m:Method) ON (m.validFrom, m.validTo);

-- Índices de búsqueda
CREATE INDEX class_version_id IF NOT EXISTS FOR (c:Class) ON (c.versionId);
CREATE INDEX method_version_id IF NOT EXISTS FOR (m:Method) ON (m.versionId);
CREATE INDEX version_current IF NOT EXISTS FOR (v:Version) ON (v.isCurrent);

-- Verificar
SHOW INDEXES;
"@

Write-Host $cypherQueries -ForegroundColor Gray
Write-Host ""

# Guardar queries en archivo
$cypherFile = Join-Path $repoPath "neo4j-auradb-init.cypher"
$cypherQueries | Out-File -FilePath $cypherFile -Encoding UTF8

Write-Host "  ✅ Queries guardadas en: neo4j-auradb-init.cypher" -ForegroundColor Green
Write-Host ""

$openBrowser = Read-Host "  ¿Abrir Neo4j Browser ahora? (y/n)"
if ($openBrowser -eq "y") {
    Start-Process "https://console.neo4j.io/projects/2a4895fc-e83c-4a1e-987f-f918237f8667/home"
}

Write-Host ""

# ============================================
# 9. Resumen y próximos pasos
# ============================================
Write-Host @"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║              ✅  Setup Completado!                     ║
║              (Neo4j AuraDB Cloud)                      ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

Write-Host ""
Write-Host "📋 Configuración:" -ForegroundColor Cyan
Write-Host "   • Estrategia: Versionado Temporal (Estrategia 1)" -ForegroundColor White
Write-Host "   • Neo4j: AuraDB Cloud (14 días gratis)" -ForegroundColor White
Write-Host "   • URI: $Neo4jUri" -ForegroundColor White
Write-Host "   • User: neo4j" -ForegroundColor White
Write-Host ""

Write-Host "🚀 Próximos pasos:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   1. Inicializar índices en Neo4j Browser (ver instrucciones arriba)" -ForegroundColor White
Write-Host ""
Write-Host "   2. Iniciar Azure Functions:" -ForegroundColor White
Write-Host "      cd $functionsPath" -ForegroundColor Gray
Write-Host "      func start" -ForegroundColor Cyan
Write-Host ""
Write-Host "   3. Probar análisis de repositorio:" -ForegroundColor White
Write-Host "      `$body = @{ owner='octocat'; repo='Hello-World'; branch='master' } | ConvertTo-Json" -ForegroundColor Gray
Write-Host "      Invoke-RestMethod -Uri http://localhost:7071/api/ingest -Method POST -Body `$body -ContentType 'application/json'" -ForegroundColor Cyan
Write-Host ""
Write-Host "   4. Ver resultados en Neo4j Browser (en la nube)" -ForegroundColor White
Write-Host ""

Write-Host "📚 Documentación:" -ForegroundColor Cyan
Write-Host "   • CONFIGURACION_NEO4J_AURADB.md - Guía completa de AuraDB" -ForegroundColor White
Write-Host "   • README.md - Documentación general" -ForegroundColor White
Write-Host "   • GETTING_STARTED.md - Primeros pasos" -ForegroundColor White
Write-Host ""

Write-Host "💡 Tips:" -ForegroundColor Yellow
Write-Host "   • Neo4j Browser: Click en tu instancia → 'Open with' → 'Neo4j Browser'" -ForegroundColor Gray
Write-Host "   • Ver métricas: Console → Tu instancia → 'Metrics'" -ForegroundColor Gray
Write-Host "   • Reset password: Console → Tu instancia → Settings → 'Reset password'" -ForegroundColor Gray
Write-Host ""

Write-Host "¡Listo para usar CodeIntel con Neo4j en la nube! ☁️" -ForegroundColor Green
Write-Host ""
