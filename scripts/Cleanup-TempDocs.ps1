# Script de Limpieza de Archivos Markdown Temporales
# Ejecutar: .\Cleanup-TempDocs.ps1

Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     🧹 LIMPIEZA DE ARCHIVOS MARKDOWN TEMPORALES 🧹           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Lista de archivos temporales a eliminar
$tempFiles = @(
    "COMMIT_CLEANUP_SUMMARY.md",
    "COMMIT_NEO4J_SIMPLIFICATION.md",
    "SIGUIENTE_ACCION.md",
    "CLEANUP_AZURE_GREMLIN.md",
    "SIMPLIFICATION_NEO4J_STRATEGY.md",
    "FIX_AZURE_SEARCH_ERROR.md"
)

Write-Host "📋 Archivos temporales identificados para eliminación:`n" -ForegroundColor Yellow

foreach ($file in $tempFiles) {
    if (Test-Path $file) {
        Write-Host "   ❌ $file" -ForegroundColor Red
    }
    else {
        Write-Host "   ⚪ $file (ya eliminado)" -ForegroundColor Gray
    }
}

Write-Host "`n⚠️  IMPORTANTE: Estos archivos son resúmenes de commits que ya están en Git history.`n" -ForegroundColor Yellow

$confirmation = Read-Host "¿Deseas eliminar estos archivos? (S/N)"

if ($confirmation -eq 'S' -or $confirmation -eq 's') {
    Write-Host "`n🗑️  Eliminando archivos...`n" -ForegroundColor Cyan

    $deletedCount = 0

    foreach ($file in $tempFiles) {
        if (Test-Path $file) {
            try {
                Remove-Item $file -ErrorAction Stop
                Write-Host "   ✅ Eliminado: $file" -ForegroundColor Green
                $deletedCount++
            }
            catch {
                Write-Host "   ❌ Error al eliminar: $file" -ForegroundColor Red
            }
        }
    }

    Write-Host "`n✅ Limpieza completada: $deletedCount archivos eliminados`n" -ForegroundColor Green

    # Mostrar archivos MD restantes
    Write-Host "📚 Archivos Markdown restantes en el directorio:`n" -ForegroundColor Cyan
    Get-ChildItem -Filter "*.md" | Where-Object { $_.Name -ne "GUIA_ARCHIVOS_MARKDOWN.md" } | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 1)
        Write-Host "   📄 $($_.Name) ($size KB)" -ForegroundColor White
    }

    Write-Host "`n💡 Próximos pasos sugeridos:`n" -ForegroundColor Yellow
    Write-Host "   1. Revisar GUIA_ARCHIVOS_MARKDOWN.md para consolidación" -ForegroundColor White
    Write-Host "   2. Consolidar archivos redundantes según la guía" -ForegroundColor White
    Write-Host "   3. Commit de la limpieza: git add . && git commit -m 'docs: cleanup temporary markdown files'" -ForegroundColor White
    Write-Host ""
}
else {
    Write-Host "`n❌ Operación cancelada. No se eliminó ningún archivo.`n" -ForegroundColor Yellow
}
