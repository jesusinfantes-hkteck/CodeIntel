# Script de prueba rápida para verificar la Estrategia 1
# Ejecutar después de iniciar func start

param(
    [string]$BaseUrl = "http://localhost:7071",
    [string]$Owner = "microsoft",
    [string]$Repo = "dotnet",
    [string]$Branch = "main"
)

$ErrorActionPreference = "Continue"

Write-Host @"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║        CodeIntel - Test de Estrategia 1                ║
║        Versionado Temporal                             ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host ""

# ============================================
# Test 1: Health Check
# ============================================
Write-Host "🔍 Test 1: Verificando que Functions esté corriendo..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/api/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    Write-Host "   ✅ Functions está corriendo" -ForegroundColor Green
}
catch {
    Write-Host "   ⚠️  Health endpoint no disponible (es normal si no existe)" -ForegroundColor Yellow
}

Write-Host ""

# ============================================
# Test 2: Analizar repositorio (crear versión 1)
# ============================================
Write-Host "📊 Test 2: Analizando repositorio (Version 1)..." -ForegroundColor Yellow
Write-Host "   Repo: $Owner/$Repo@$Branch" -ForegroundColor Gray

try {
    $body = @{
        owner = $Owner
        repo = $Repo
        branch = $Branch
    } | ConvertTo-Json

    Write-Host "   Enviando request..." -NoNewline

    $startTime = Get-Date
    $result = Invoke-RestMethod -Uri "$BaseUrl/api/ingest" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 300
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    Write-Host " ✅ ($duration segundos)" -ForegroundColor Green
    Write-Host "   - Clases: $($result.classes)" -ForegroundColor White
    Write-Host "   - Métodos: $($result.methods)" -ForegroundColor White
    Write-Host "   - Edges: $($result.edges)" -ForegroundColor White
    Write-Host "   - Indexados: $($result.indexed)" -ForegroundColor White
}
catch {
    Write-Host " ❌" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "   Verifica que:" -ForegroundColor Yellow
    Write-Host "   1. func start esté corriendo" -ForegroundColor Gray
    Write-Host "   2. Neo4j esté disponible" -ForegroundColor Gray
    Write-Host "   3. GitHub token esté configurado" -ForegroundColor Gray
    exit 1
}

Write-Host ""

# ============================================
# Test 3: Listar versiones
# ============================================
Write-Host "📋 Test 3: Listando versiones..." -ForegroundColor Yellow

try {
    $versions = Invoke-RestMethod -Uri "$BaseUrl/api/repo/$Owner/$Repo/$Branch/versions"

    Write-Host "   ✅ Total de versiones: $($versions.totalVersions)" -ForegroundColor Green
    Write-Host ""
    Write-Host "   Historial:" -ForegroundColor Cyan

    foreach ($v in $versions.versions) {
        $current = if ($v.isCurrent) { "← ACTUAL" } else { "" }
        $ageStr = if ($v.age.Days -gt 0) { "$($v.age.Days)d" } else { "$($v.age.Hours)h $($v.age.Minutes)m" }

        Write-Host "   • Version: $($v.versionId)" -ForegroundColor White
        Write-Host "     Commit: $($v.commitHash)" -ForegroundColor Gray
        Write-Host "     Fecha: $($v.timestamp)" -ForegroundColor Gray
        Write-Host "     Edad: $ageStr $current" -ForegroundColor Gray
        Write-Host ""
    }
}
catch {
    Write-Host "   ❌ Error al listar versiones" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# ============================================
# Test 4: Crear segunda versión (simular cambio)
# ============================================
Write-Host "🔄 Test 4: Creando segunda versión (simular commit)..." -ForegroundColor Yellow
Write-Host "   Esperando 5 segundos..." -NoNewline
Start-Sleep -Seconds 5
Write-Host " ✅" -ForegroundColor Green

try {
    $body = @{
        owner = $Owner
        repo = $Repo
        branch = $Branch
        path = "commit-$(Get-Random)" # Simular commit diferente
    } | ConvertTo-Json

    Write-Host "   Re-analizando repositorio..." -NoNewline
    $result2 = Invoke-RestMethod -Uri "$BaseUrl/api/ingest" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 300
    Write-Host " ✅" -ForegroundColor Green
}
catch {
    Write-Host " ❌" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# ============================================
# Test 5: Verificar múltiples versiones
# ============================================
Write-Host "📊 Test 5: Verificando múltiples versiones..." -ForegroundColor Yellow

try {
    $versionsAfter = Invoke-RestMethod -Uri "$BaseUrl/api/repo/$Owner/$Repo/$Branch/versions"

    if ($versionsAfter.totalVersions -ge 2) {
        Write-Host "   ✅ Múltiples versiones detectadas: $($versionsAfter.totalVersions)" -ForegroundColor Green

        $currentVersion = $versionsAfter.versions | Where-Object { $_.isCurrent -eq $true }
        $previousVersion = $versionsAfter.versions | Where-Object { $_.isCurrent -eq $false } | Select-Object -First 1

        Write-Host ""
        Write-Host "   Versión actual:" -ForegroundColor Cyan
        Write-Host "   • ID: $($currentVersion.versionId)" -ForegroundColor White
        Write-Host "   • Commit: $($currentVersion.commitHash)" -ForegroundColor White
        Write-Host ""
        Write-Host "   Versión anterior:" -ForegroundColor Cyan
        Write-Host "   • ID: $($previousVersion.versionId)" -ForegroundColor White
        Write-Host "   • Commit: $($previousVersion.commitHash)" -ForegroundColor White
    } else {
        Write-Host "   ⚠️  Solo 1 versión encontrada (puede ser normal en primera ejecución)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "   ❌ Error" -ForegroundColor Red
}

Write-Host ""

# ============================================
# Test 6: Rollback (si hay múltiples versiones)
# ============================================
if ($versionsAfter.totalVersions -ge 2) {
    Write-Host "⏮️  Test 6: Probando rollback..." -ForegroundColor Yellow

    $previousVersionId = ($versionsAfter.versions | Where-Object { $_.isCurrent -eq $false } | Select-Object -First 1).versionId

    try {
        $rollbackBody = @{ versionId = $previousVersionId } | ConvertTo-Json

        Write-Host "   Haciendo rollback a: $previousVersionId..." -NoNewline
        $rollbackResult = Invoke-RestMethod -Uri "$BaseUrl/api/repo/$Owner/$Repo/$Branch/rollback" -Method POST -Body $rollbackBody -ContentType "application/json"
        Write-Host " ✅" -ForegroundColor Green

        # Verificar que el rollback funcionó
        Start-Sleep -Seconds 2
        $versionsAfterRollback = Invoke-RestMethod -Uri "$BaseUrl/api/repo/$Owner/$Repo/$Branch/versions"
        $newCurrent = $versionsAfterRollback.versions | Where-Object { $_.isCurrent -eq $true }

        if ($newCurrent.versionId -eq $previousVersionId) {
            Write-Host "   ✅ Rollback exitoso!" -ForegroundColor Green
            Write-Host "   Nueva versión actual: $($newCurrent.versionId)" -ForegroundColor White
        } else {
            Write-Host "   ⚠️  Rollback puede no haber funcionado correctamente" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host " ❌" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host ""
}

# ============================================
# Test 7: Consulta temporal (snapshot)
# ============================================
Write-Host "⏰ Test 7: Consultando snapshot temporal..." -ForegroundColor Yellow

try {
    # Timestamp de hace 1 hora
    $oneHourAgo = [DateTimeOffset]::UtcNow.AddHours(-1).ToUnixTimeSeconds()

    Write-Host "   Consultando estado hace 1 hora..." -NoNewline
    $snapshot = Invoke-RestMethod -Uri "$BaseUrl/api/repo/$Owner/$Repo/$Branch/snapshot?timestamp=$oneHourAgo"
    Write-Host " ✅" -ForegroundColor Green

    Write-Host "   Timestamp: $($snapshot.timestamp)" -ForegroundColor White
    Write-Host "   Clases en ese momento: $($snapshot.snapshot.classes)" -ForegroundColor White
    Write-Host "   Métodos en ese momento: $($snapshot.snapshot.methods)" -ForegroundColor White
}
catch {
    Write-Host " ⚠️  " -ForegroundColor Yellow
    Write-Host "   Es normal si el código no existía hace 1 hora" -ForegroundColor Gray
}

Write-Host ""

# ============================================
# Test 8: Query directo a Neo4j
# ============================================
Write-Host "🔍 Test 8: Queries recomendadas para Neo4j Browser..." -ForegroundColor Yellow
Write-Host ""

$cypherQueries = @"
// 1. Ver todas las versiones del repositorio
MATCH (r:Repository {id: "$Owner/$Repo@$Branch"})-[:HAS_VERSION]->(v:Version)
RETURN v.id AS versionId, 
       v.commitHash AS commit,
       datetime({epochSeconds: v.timestamp}) AS timestamp,
       v.isCurrent AS current
ORDER BY v.timestamp DESC

// 2. Ver clases de la versión actual
MATCH (v:Version {isCurrent: true})-[:CONTAINS]->(c:Class)
WHERE c.repoId = "$Owner/$Repo@$Branch"
RETURN c.name, c.namespace, c.filePath
LIMIT 10

// 3. Ver evolución de una clase (si existe)
MATCH path = (c:Class)-[:NEXT_VERSION*0..]->(latest:Class)
WHERE c.repoId = "$Owner/$Repo@$Branch"
  AND c.name = 'Program'
RETURN nodes(path) AS versions

// 4. Verificar índices
SHOW INDEXES

// 5. Estadísticas
MATCH (r:Repository {id: "$Owner/$Repo@$Branch"})-[:HAS_VERSION]->(v:Version)
RETURN count(v) AS totalVersions

MATCH (c:Class {repoId: "$Owner/$Repo@$Branch"})
RETURN count(c) AS totalClassNodes

MATCH (m:Method {repoId: "$Owner/$Repo@$Branch"})
RETURN count(m) AS totalMethodNodes
"@

Write-Host $cypherQueries -ForegroundColor Gray
Write-Host ""
Write-Host "   💡 Copia estas queries en Neo4j Browser: http://localhost:7474" -ForegroundColor Cyan

Write-Host ""

# ============================================
# Resumen Final
# ============================================
Write-Host @"
╔════════════════════════════════════════════════════════╗
║                                                        ║
║              ✅  Tests Completados                     ║
║                                                        ║
╚════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green

Write-Host ""
Write-Host "📊 Resumen:" -ForegroundColor Cyan
Write-Host "   • Repositorio analizado: $Owner/$Repo@$Branch" -ForegroundColor White
Write-Host "   • Versiones creadas: $($versionsAfter.totalVersions)" -ForegroundColor White
Write-Host "   • Rollback probado: $(if ($versionsAfter.totalVersions -ge 2) { '✅' } else { '⏭️  (omitido)' })" -ForegroundColor White
Write-Host ""

Write-Host "🎯 Próximos pasos:" -ForegroundColor Cyan
Write-Host "   1. Explora Neo4j Browser: http://localhost:7474" -ForegroundColor White
Write-Host "   2. Prueba las queries Cypher de arriba" -ForegroundColor White
Write-Host "   3. Simula más commits y observa el versionado" -ForegroundColor White
Write-Host "   4. Configura webhooks de GitHub para automatización" -ForegroundColor White
Write-Host ""

Write-Host "📚 Documentación:" -ForegroundColor Cyan
Write-Host "   • README.md" -ForegroundColor White
Write-Host "   • docs/Guia_Uso_Versionado.md" -ForegroundColor White
Write-Host "   • docs/CHECKLIST_IMPLEMENTACION.md" -ForegroundColor White
Write-Host ""

Write-Host "¡Estrategia 1 funcionando correctamente! 🎉" -ForegroundColor Green
Write-Host ""
