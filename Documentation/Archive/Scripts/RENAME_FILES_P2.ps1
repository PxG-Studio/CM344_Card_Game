# PowerShell Script to Rename P2 Class Files
# Run this script from the project root directory

$basePath = "Assets\Scripts\Current\Opposition Scripts"

# Define rename pairs: old name -> new name
$renamePairs = @{
    "CardMoverOpp.cs" = "CardMoverP2.cs"
    "NewDeckManagerOpp.cs" = "NewDeckManagerP2.cs"
    "NewHandOppUI.cs" = "NewHandP2UI.cs"
    "NewCardSystemOpposition.cs" = "NewCardSystemP2.cs"
}

Write-Host "Starting file renaming process..." -ForegroundColor Cyan

foreach ($pair in $renamePairs.GetEnumerator()) {
    $oldName = $pair.Key
    $newName = $pair.Value
    
    $oldPath = Join-Path $basePath $oldName
    $newPath = Join-Path $basePath $newName
    
    $oldMetaPath = "$oldPath.meta"
    $newMetaPath = "$newPath.meta"
    
    if (Test-Path $oldPath) {
        Write-Host "Renaming: $oldName -> $newName" -ForegroundColor Yellow
        
        # Rename .cs file
        Rename-Item -Path $oldPath -NewName $newName -ErrorAction Stop
        Write-Host "  ✓ Renamed .cs file" -ForegroundColor Green
        
        # Rename .meta file if it exists
        if (Test-Path $oldMetaPath) {
            Rename-Item -Path $oldMetaPath -NewName $newMetaPath -ErrorAction Stop
            Write-Host "  ✓ Renamed .meta file" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ No .meta file found (Unity will create one)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ✗ File not found: $oldName" -ForegroundColor Red
    }
}

Write-Host "`nFile renaming complete!" -ForegroundColor Cyan
Write-Host "Please open Unity Editor and verify all files compile correctly." -ForegroundColor Yellow

