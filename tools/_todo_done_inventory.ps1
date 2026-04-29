$root = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "YgoDuelistCode") "Cards"
$cardRoots = @(
    (Join-Path (Join-Path $root "Monster") "Todo"),
    (Join-Path (Join-Path $root "Spell") "Todo"),
    (Join-Path (Join-Path $root "Trap") "Todo")
)
$placeholderPattern = 'ExecuteSpellEffectPlaceholder|ExecuteSpellUpgradePlaceholder|ExecuteTrapEffectPlaceholder|ExecuteTrapUpgradePlaceholder'

$moves = [System.Collections.Generic.List[object]]::new()
foreach ($cr in $cardRoots) {
    if (-not (Test-Path $cr)) { continue }
    Get-ChildItem -Path $cr -Filter "*.cs" -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($root.Length + 1) -replace '\\','/'
        $txt = [System.IO.File]::ReadAllText($_.FullName)
        if ($txt -notmatch '(?m)^[ \t]*public\s+override\s+YgoCardPackTags\s+PackTags\s*=>') { return }
        $kind = if ($rel.StartsWith("Monster/")) { "Monster" } elseif ($rel.StartsWith("Spell/")) { "Spell" } else { "Trap" }
        if ($kind -eq "Spell" -or $kind -eq "Trap") {
            if ($txt -match $placeholderPattern) { return }
        }
        $linked = $false
        if ($kind -eq "Trap" -and $txt -match "IYgoSpellTrapEquipLink") { $linked = $true }
        $moves.Add([pscustomobject]@{ Path = $_.FullName; Rel = $rel; Kind = $kind; Linked = $linked })
    }
}
$moves | Sort-Object Rel | Export-Csv -Path (Join-Path $PSScriptRoot "_todo_done_moves.csv") -NoTypeInformation
Write-Host "Count:" $moves.Count
$moves | Select-Object -First 5 | Format-Table
