# Moves eligible Todo card .cs (+.uid) to Done; Linked traps -> Trap/Done/Linked; updates namespace in each moved file.
$ErrorActionPreference = "Stop"
$base = [System.IO.Path]::GetFullPath((Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "YgoDuelistCode") "Cards"))
$placeholderPattern = 'ExecuteSpellEffectPlaceholder|ExecuteSpellUpgradePlaceholder|ExecuteTrapEffectPlaceholder|ExecuteTrapUpgradePlaceholder'
$packTagsPattern = '(?m)^[ \t]*public\s+override\s+YgoCardPackTags\s+PackTags\s*=>'

function Get-RelativePathFromBase([string]$fileFullPath) {
    $f = [System.IO.Path]::GetFullPath($fileFullPath)
    $b = $base.TrimEnd([char]'\', [char]'/')
    if ($f.Length -lt $b.Length -or -not $f.StartsWith($b, [StringComparison]::OrdinalIgnoreCase)) {
        throw "File not under base: $f | base=$b"
    }
    $tail = $f.Substring($b.Length).TrimStart([char]'\', [char]'/')
    return $tail -replace '/', '\'
}

function Get-MoveList {
    $cardRoots = @(
        (Join-Path (Join-Path $base "Monster") "Todo"),
        (Join-Path (Join-Path $base "Spell") "Todo"),
        (Join-Path (Join-Path $base "Trap") "Todo")
    )
    $moves = [System.Collections.Generic.List[object]]::new()
    foreach ($cr in $cardRoots) {
        if (-not (Test-Path $cr)) { continue }
        Get-ChildItem -Path $cr -Filter "*.cs" -Recurse -File | ForEach-Object {
            $txt = [System.IO.File]::ReadAllText($_.FullName)
            if ($txt -notmatch $packTagsPattern) { return }
            $relOs = Get-RelativePathFromBase $_.FullName
            $norm = $relOs -replace '\\', '/'
            $kind = if ($norm.StartsWith("Monster/")) { "Monster" } elseif ($norm.StartsWith("Spell/")) { "Spell" } else { "Trap" }
            if ($kind -eq "Spell" -or $kind -eq "Trap") {
                if ($txt -match $placeholderPattern) { return }
            }
            $linked = ($kind -eq "Trap" -and $txt -match "IYgoSpellTrapEquipLink")
            $moves.Add([pscustomobject]@{ FullPath = $_.FullName; RelOs = $relOs; Norm = $norm; Kind = $kind; Linked = $linked })
        }
    }
    return $moves
}

$moves = Get-MoveList
Write-Host "Moving" $moves.Count "files"

foreach ($m in $moves) {
    $relOs = $m.RelOs
    $norm = $m.Norm
    $parts = $norm -split '/'
    $kind = $parts[0]
    $subtype = $parts[2]
    $file = $parts[-1]
    $src = $m.FullPath
    if ($m.Linked) {
        $destDir = Join-Path (Join-Path (Join-Path $base "Trap") "Done") "Linked"
        $oldNs = "YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.$subtype"
        $newNs = "YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked"
    } else {
        $destRelOs = $relOs -creplace '\\Todo\\', '\Done\'
        $destFull = Join-Path $base $destRelOs
        $destDir = Split-Path $destFull -Parent
        $oldNs = "YgoDuelist.YgoDuelistCode.Cards.$kind.Todo.$subtype"
        $newNs = "YgoDuelist.YgoDuelistCode.Cards.$kind.Done.$subtype"
    }
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    $dest = Join-Path $destDir $file
    if (Test-Path $dest) { throw "Dest exists: $dest" }
    Move-Item -LiteralPath $src -Destination $dest
    $uid = $src + ".uid"
    if (Test-Path $uid) {
        Move-Item -LiteralPath $uid -Destination ($dest + ".uid")
    }
    $c = [System.IO.File]::ReadAllText($dest)
    $c2 = $c -replace [regex]::Escape($oldNs), $newNs
    if ($c2 -eq $c) { Write-Warning "Namespace replace failed: $dest (oldNs=$oldNs)" }
    [System.IO.File]::WriteAllText($dest, $c2)
}

$mapLines = foreach ($m in $moves) {
    $parts = $m.Norm -split '/'
    $kind = $parts[0]
    $subtype = $parts[2]
    $file = $parts[-1]
    $cls = [System.IO.Path]::GetFileNameWithoutExtension($file)
    if ($m.Linked) {
        $oldNs = "YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.$subtype"
        $newNs = "YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked"
    } else {
        $oldNs = "YgoDuelist.YgoDuelistCode.Cards.$kind.Todo.$subtype"
        $newNs = "YgoDuelist.YgoDuelistCode.Cards.$kind.Done.$subtype"
    }
    "$oldNs.$cls`t$newNs.$cls"
}
$mapPath = Join-Path $PSScriptRoot "_todo_done_type_map.tsv"
$mapLines | Set-Content -Path $mapPath -Encoding utf8
Write-Host "Wrote type map:" $mapPath
Write-Host "Done."
