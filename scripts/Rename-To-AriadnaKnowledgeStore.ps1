# ============================================================================
# Script de Renombrado: AriadnaKnowledgeStore -> AriadnaKnowledgeStore
# ============================================================================
param(
    [switch]$DryRun,
    [switch]$SkipBackup
)

$ErrorActionPreference = 'Stop'

# Configuracion
$OldName = "AriadnaKnowledgeStore"
$NewName = "AriadnaKnowledgeStore"
$OldRepoSlug = "ariadna-knowledgestore"
$NewRepoSlug = "ariadna-knowledgestore"
$RootPath = "C:\proyectos\gh-ariadna-knowledgestore-mvp"
$NewRootPath = "C:\proyectos\AriadnaKnowledgeStore"
$BackupPath = "C:\proyectos\BACKUP-AriadnaKnowledgeStore-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$OldRepoUrl = "https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore"
$NewRepoUrl = "https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore"

# Colores
$ColorInfo = "Cyan"
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"

# Funciones helper
function Write-StepHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor $ColorInfo
    Write-Host " $Message" -ForegroundColor $ColorInfo
    Write-Host "============================================================" -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "  + $Message" -ForegroundColor $ColorSuccess
}

function Write-Progress {
    param([string]$Message)
    Write-Host "  -> $Message" -ForegroundColor Gray
}

# ============================================================================
# VERIFICACIONES PREVIAS
# ============================================================================

Write-StepHeader "PASO 0: Verificaciones Previas"

if (-not (Test-Path $RootPath)) {
    Write-Host "ERROR: No se encuentra el proyecto en $RootPath" -ForegroundColor $ColorError
    exit 1
}

Write-Success "Proyecto encontrado: $RootPath"

# Verificar Visual Studio
$vsProcess = Get-Process devenv -ErrorAction SilentlyContinue
if ($vsProcess) {
    Write-Warning "Visual Studio esta abierto. Se recomienda cerrarlo antes de continuar."
    $response = Read-Host "Deseas continuar de todas formas? (S/N)"
    if ($response -ne "S") {
        exit 0
    }
}

# Verificar Azure Functions
$funcProcess = Get-Process func -ErrorAction SilentlyContinue
if ($funcProcess) {
    Write-Warning "Azure Functions esta corriendo. Deteniendolo..."
    Stop-Process -Name "func" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Verificar Git
Set-Location "$RootPath\src"
$gitStatus = git status --short
if ($gitStatus) {
    Write-Warning "Hay cambios sin commitear en Git:"
    Write-Host $gitStatus
    $response = Read-Host "Deseas continuar de todas formas? (S/N)"
    if ($response -ne "S") {
        Write-Host "Haz commit de tus cambios primero: git add . ; git commit -m 'Pre-rename checkpoint'"
        exit 0
    }
}

if ($DryRun) {
    Write-Warning "MODO DRY-RUN: No se realizaran cambios reales"
}

# ============================================================================
# PASO 1: CREAR BACKUP
# ============================================================================

Write-StepHeader "PASO 1: Creando Backup"

if (-not $SkipBackup -and -not $DryRun) {
    Write-Progress "Copiando $RootPath a $BackupPath..."
    Copy-Item -Path $RootPath -Destination $BackupPath -Recurse -Force
    Write-Success "Backup creado exitosamente"
} else {
    if ($SkipBackup) {
        Write-Warning "Backup omitido (parametro -SkipBackup)"
    } else {
        Write-Warning "Backup omitido (dry-run)"
    }
}

# ============================================================================
# PASO 2: REEMPLAZAR CONTENIDO EN ARCHIVOS
# ============================================================================

Write-StepHeader "PASO 2: Reemplazando Contenido en Archivos"

$extensions = @("*.cs", "*.csproj", "*.sln", "*.json", "*.md", "*.ps1", "*.txt", "*.yml", "*.yaml")
$files = Get-ChildItem -Path "$RootPath\src" -Include $extensions -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.vs|node_modules)\\' }

Write-Progress "Encontrados $($files.Count) archivos para procesar"

$changedFiles = 0
$replacements = @(
    @{ Old = "AriadnaKnowledgeStore"; New = "AriadnaKnowledgeStore" },
    @{ Old = "AriadnaKnowledgeStore"; New = "ariadna-knowledgestore" },
    @{ Old = "ariadna-knowledgestore"; New = "ariadna-knowledgestore" },
    @{ Old = "ariadna_knowledgestore"; New = "ariadna_knowledgestore" },
    @{ Old = "gh-ariadna-knowledgestore-mvp"; New = "AriadnaKnowledgeStore" },
    @{ Old = $OldRepoUrl; New = $NewRepoUrl }
)

foreach ($file in $files) {
    if (-not $DryRun) {
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        $originalContent = $content

        foreach ($replacement in $replacements) {
            $content = $content -replace [regex]::Escape($replacement.Old), $replacement.New
        }

        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            $changedFiles++
        }
    }
}

if ($DryRun) {
    Write-Warning "Se procesarian $($files.Count) archivos"
} else {
    Write-Success "$changedFiles archivos actualizados"
}

# ============================================================================
# PASO 3: RENOMBRAR ARCHIVOS .CSPROJ
# ============================================================================

Write-StepHeader "PASO 3: Renombrando Archivos .csproj"

# Obtener todos los archivos .csproj de una sola vez para evitar problemas de renombrado
$csprojFiles = @(Get-ChildItem -Path "$RootPath\src" -Filter "*.csproj" -Recurse | 
    Where-Object { $_.Name -match [regex]::Escape($OldName) } |
    Sort-Object FullName)

Write-Progress "Encontrados $($csprojFiles.Count) archivos .csproj"

foreach ($file in $csprojFiles) {
    $renamedFileName = $file.Name -replace [regex]::Escape($OldName), $NewName

    Write-Progress "  $($file.Name) -> $renamedFileName"

    if (-not $DryRun) {
        Rename-Item -Path $file.FullName -NewName $renamedFileName -Force
    }
}

Write-Success "$($csprojFiles.Count) archivos .csproj procesados"

# ============================================================================
# PASO 4: RENOMBRAR ARCHIVO .SLN
# ============================================================================

Write-StepHeader "PASO 4: Renombrando Archivo .sln"

$slnFile = Get-ChildItem -Path "$RootPath\src" -Filter "*$OldName.sln"

if ($slnFile) {
    $renamedSlnName = $slnFile.Name -replace [regex]::Escape($OldName), $NewName
    Write-Progress "$($slnFile.Name) -> $renamedSlnName"

    if (-not $DryRun) {
        Rename-Item -Path $slnFile.FullName -NewName $renamedSlnName -Force
    }

    Write-Success "Archivo .sln renombrado"
} else {
    Write-Warning "No se encontro archivo .sln"
}

# ============================================================================
# PASO 5: RENOMBRAR CARPETAS DE PROYECTOS
# ============================================================================

Write-StepHeader "PASO 5: Renombrando Carpetas de Proyectos"

# Obtener todas las carpetas de una vez y ordenarlas
$projectFolders = @(Get-ChildItem -Path "$RootPath\src" -Directory |
    Where-Object { $_.Name -match [regex]::Escape($OldName) } |
    Sort-Object FullName)

Write-Progress "Encontradas $($projectFolders.Count) carpetas"

foreach ($folder in $projectFolders) {
    $renamedFolderName = $folder.Name -replace [regex]::Escape($OldName), $NewName

    Write-Progress "  $($folder.Name) -> $renamedFolderName"

    if (-not $DryRun) {
        Rename-Item -Path $folder.FullName -NewName $renamedFolderName -Force
    }
}

Write-Success "$($projectFolders.Count) carpetas renombradas"

# ============================================================================
# PASO 6: RENOMBRAR DIRECTORIO RAiZ
# ============================================================================

Write-StepHeader "PASO 6: Renombrando Directorio Raiz"

if (Test-Path $NewRootPath) {
    Write-Warning "El directorio $NewRootPath ya existe"
    $response = Read-Host "Deseas eliminarlo y continuar? (S/N)"
    if ($response -eq "S" -and -not $DryRun) {
        Remove-Item $NewRootPath -Recurse -Force
    } elseif ($response -ne "S") {
        Write-Host "Operacion cancelada" -ForegroundColor $ColorError
        exit 1
    }
}

Write-Progress "gh-ariadna-knowledgestore-mvp -> AriadnaKnowledgeStore"

if (-not $DryRun) {
    Rename-Item -Path $RootPath -NewName "AriadnaKnowledgeStore" -Force
    Write-Success "Directorio raiz renombrado exitosamente"
} else {
    Write-Warning "Renombrado omitido (dry-run)"
}

# ============================================================================
# PASO 7: ACTUALIZAR GIT REMOTE
# ============================================================================

Write-StepHeader "PASO 7: Actualizando Git Remote"

if (-not $DryRun) {
    Set-Location "$NewRootPath\src"
    git remote set-url origin $NewRepoUrl
    Write-Success "Git remote actualizado a: $NewRepoUrl"
} else {
    Write-Warning "Git remote no modificado (dry-run)"
}

# ============================================================================
# PASO 8: LIMPIAR BINARIOS Y CACHe
# ============================================================================

Write-StepHeader "PASO 8: Limpiando Binarios y Cache"

if (-not $DryRun) {
    $foldersToDelete = @("bin", "obj", ".vs")

    foreach ($folderName in $foldersToDelete) {
        $folders = Get-ChildItem -Path "$NewRootPath\src" -Directory -Recurse -Filter $folderName -ErrorAction SilentlyContinue
        foreach ($folder in $folders) {
            Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Write-Success "Carpetas bin/, obj/, .vs/ eliminadas"
} else {
    Write-Warning "Limpieza omitida (dry-run)"
}

# ============================================================================
# PASO 9: RECONSTRUIR SOLUCIoN
# ============================================================================

Write-StepHeader "PASO 9: Reconstruyendo Solucion"

if (-not $DryRun) {
    Set-Location "$NewRootPath\src"

    Write-Progress "Ejecutando: dotnet restore..."
    dotnet restore "$NewName.sln" 2>&1 | Out-Null

    Write-Progress "Ejecutando: dotnet build..."
    $buildResult = dotnet build "$NewName.sln" --no-restore 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Compilacion exitosa +"
    } else {
        Write-Warning "La compilacion tuvo errores:"
        Write-Host $buildResult -ForegroundColor Yellow
    }
} else {
    Write-Warning "Compilacion omitida (dry-run)"
}

# ============================================================================
# PASO 10: GENERAR REPORTE
# ============================================================================

Write-StepHeader "PASO 10: Generando Reporte de Cambios"

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = "$NewRootPath\RENAME_REPORT_$timestamp.txt"

if (-not $DryRun) {
    $timestampFull = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $report = "============================================================`n"
    $report += "REPORTE DE RENOMBRADO: AriadnaKnowledgeStore -> AriadnaKnowledgeStore`n"
    $report += "============================================================`n"
    $report += "Fecha: $timestampFull`n`n"
    $report += "CAMBIOS REALIZADOS:`n"
    $report += "$changedFiles archivos actualizados`n"
    $report += "$($csprojFiles.Count) archivos .csproj renombrados`n"
    $report += "1 archivo .sln renombrado`n"
    $report += "$($projectFolders.Count) carpetas de proyectos renombradas`n"
    $report += "Directorio raiz renombrado`n"
    $report += "Git remote actualizado`n`n"
    $report += "RUTAS ACTUALES:`n"
    $report += "Proyecto: $NewRootPath`n"
    $report += "Solucion: $NewRootPath\src\$NewName.sln`n"
    $report += "Backup: $BackupPath`n`n"
    $report += "PROXIMOS PASOS:`n"
    $report += "1. Renombrar repositorio en GitHub`n"
    $report += "2. Abrir la solucion en Visual Studio`n"
    $report += "3. Verificar que compila: dotnet build`n"
    $report += "4. Ejecutar Azure Functions: func start`n"
    $report += "5. Probar ingesta`n"
    $report += "6. Hacer commit final: git push origin master --force`n"

    Set-Content -Path $reportPath -Value $report -Encoding UTF8
    Write-Success "Reporte generado: $reportPath"
}

# ============================================================================
# RESUMEN FINAL
# ============================================================================

Write-Host ""
Write-Host "============================================================" -ForegroundColor $ColorSuccess
Write-Host "            RENOMBRADO COMPLETADO EXITOSAMENTE" -ForegroundColor $ColorSuccess
Write-Host "============================================================" -ForegroundColor $ColorSuccess
Write-Host ""

if (-not $DryRun) {
    Write-Host "+ Proyecto renombrado de:" -ForegroundColor $ColorSuccess
    Write-Host "    $RootPath" -ForegroundColor Gray
    Write-Host "  a:" -ForegroundColor $ColorSuccess
    Write-Host "    $NewRootPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "+ Backup creado en:" -ForegroundColor $ColorSuccess
    Write-Host "    $BackupPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "+ Git remote actualizado a:" -ForegroundColor $ColorSuccess
    Write-Host "    $NewRepoUrl" -ForegroundColor Gray
    Write-Host ""
    Write-Host "PROXIMOS PASOS:" -ForegroundColor Yellow
    Write-Host "  1. Renombra el repo en GitHub: https://github.com/jesusinfantes-hkteck/AriadnaKnowledgeStore/settings" -ForegroundColor Gray
    Write-Host "  2. Abre VS: code $NewRootPath\src\$NewName.sln" -ForegroundColor Gray
    Write-Host "  3. Compila: dotnet build" -ForegroundColor Gray
    Write-Host "  4. Push: git push origin master --force" -ForegroundColor Gray
} else {
    Write-Host "MODO DRY-RUN: No se realizaron cambios reales" -ForegroundColor Yellow
    Write-Host "Ejecuta sin -DryRun para aplicar los cambios" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor $ColorInfo
