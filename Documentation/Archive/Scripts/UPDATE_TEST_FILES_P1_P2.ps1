# PowerShell Script to Update All Test Files for P1/P2 Consistency
# This script will update all references in test files

$testFiles = Get-ChildItem -Path "Assets\Tests" -Recurse -Filter "*.cs"
$editorFiles = Get-ChildItem -Path "Assets\Editor" -Recurse -Filter "*.cs"

$allFiles = $testFiles + $editorFiles

$replacements = @{
    # P1 replacements
    'FindObjectOfType<NewDeckManager>' = 'FindObjectOfType<NewDeckManagerP1>'
    'typeof(NewDeckManager)' = 'typeof(NewDeckManagerP1)'
    'NewDeckManager ' = 'NewDeckManagerP1 '
    'NewDeckManager[' = 'NewDeckManagerP1['
    'NewDeckManager)' = 'NewDeckManagerP1)'
    'NewDeckManager.' = 'NewDeckManagerP1.'
    'FindObjectOfType<NewHandUI>' = 'FindObjectOfType<NewHandP1UI>'
    'typeof(NewHandUI)' = 'typeof(NewHandP1UI)'
    'NewHandUI ' = 'NewHandP1UI '
    'NewHandUI[' = 'NewHandP1UI['
    'NewHandUI)' = 'NewHandP1UI)'
    'NewHandUI.' = 'NewHandP1UI.'
    'FindObjectOfType<CardMover>' = 'FindObjectOfType<CardMoverP1>'
    'FindObjectsOfType<CardMover>' = 'FindObjectsOfType<CardMoverP1>'
    'typeof(CardMover)' = 'typeof(CardMoverP1)'
    'CardMover ' = 'CardMoverP1 '
    'CardMover[' = 'CardMoverP1['
    'CardMover)' = 'CardMoverP1)'
    'CardMover.' = 'CardMoverP1.'
    'CardMover>' = 'CardMoverP1>'
    'CardMover,' = 'CardMoverP1,'
    'CardMover<' = 'CardMoverP1<'
    
    # P2 replacements (already done, but ensure consistency)
    'FindObjectOfType<NewDeckManagerOpp>' = 'FindObjectOfType<NewDeckManagerP2>'
    'FindObjectOfType<NewHandOppUI>' = 'FindObjectOfType<NewHandP2UI>'
    'NewCardSystemTester' = 'NewCardSystemP1Tester'
    'NewCardSystemOpposition' = 'NewCardSystemP2'
}

$totalFiles = 0
$totalReplacements = 0

foreach ($file in $allFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $fileReplacements = 0
    
    foreach ($pattern in $replacements.Keys) {
        $replacement = $replacements[$pattern]
        if ($content -match [regex]::Escape($pattern)) {
            $content = $content -replace [regex]::Escape($pattern), $replacement
            $fileReplacements++
        }
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "Updated: $($file.Name) ($fileReplacements replacements)" -ForegroundColor Green
        $totalFiles++
        $totalReplacements += $fileReplacements
    }
}

Write-Host ""
Write-Host "Update complete! Files modified: $totalFiles, Total replacements: $totalReplacements" -ForegroundColor Cyan

