# PowerShell Script to Rename P1 and P2 Class Files for Full Parity
# Run this script from the project root directory

$p1Path = "Assets\Scripts\Current\Player 1 scripts"
$p2Path = "Assets\Scripts\Current\Opposition Scripts"

Write-Host "Starting P1/P2 file renaming for complete parity..." -ForegroundColor Cyan
Write-Host ""

# P1 File Renames
Write-Host "=== RENAMING P1 FILES ===" -ForegroundColor Yellow
$p1RenamePairs = @{
    "CardMover.cs" = "CardMoverP1.cs"
    "NewDeckManager.cs" = "NewDeckManagerP1.cs"
    "NewHandUI.cs" = "NewHandP1UI.cs"
    "NewCardSystemTester.cs" = "NewCardSystemP1Tester.cs"
}

foreach ($pair in $p1RenamePairs.GetEnumerator()) {
    $oldName = $pair.Key
    $newName = $pair.Value
    
    $oldPath = Join-Path $p1Path $oldName
    $newPath = Join-Path $p1Path $newName
    
    $oldMetaPath = "$oldPath.meta"
    $newMetaPath = "$newPath.meta"
    
    if (Test-Path $oldPath) {
        Write-Host "Renaming P1: $oldName -> $newName" -ForegroundColor Yellow
        
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

Write-Host ""

# P2 File Renames
Write-Host "=== RENAMING P2 FILES ===" -ForegroundColor Yellow
$p2RenamePairs = @{
    "CardMoverOpp.cs" = "CardMoverP2.cs"
    "NewDeckManagerOpp.cs" = "NewDeckManagerP2.cs"
    "NewHandOppUI.cs" = "NewHandP2UI.cs"
    "NewCardSystemOpposition.cs" = "NewCardSystemP2.cs"
}

foreach ($pair in $p2RenamePairs.GetEnumerator()) {
    $oldName = $pair.Key
    $newName = $pair.Value
    
    $oldPath = Join-Path $p2Path $oldName
    $newPath = Join-Path $p2Path $newName
    
    $oldMetaPath = "$oldPath.meta"
    $newMetaPath = "$newPath.meta"
    
    if (Test-Path $oldPath) {
        Write-Host "Renaming P2: $oldName -> $newName" -ForegroundColor Yellow
        
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

Write-Host ""
Write-Host "File renaming complete!" -ForegroundColor Cyan
Write-Host "Please open Unity Editor and verify all files compile correctly." -ForegroundColor Yellow
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  P1 Files Renamed: 4" -ForegroundColor Green
Write-Host "  P2 Files Renamed: 4" -ForegroundColor Green
Write-Host "  Total Files: 8" -ForegroundColor Green

