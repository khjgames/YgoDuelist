# Updates references after Todo->Done migration: FQN map first, then Todo->Done prefix (skip namespace lines in remaining Todo cards).
$ErrorActionPreference = "Stop"
$codeRoot = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $PSScriptRoot "..") "YgoDuelistCode"))
$mapPath = Join-Path $PSScriptRoot "_todo_done_type_map.tsv"
$pairs = Get-Content $mapPath | ForEach-Object {
    $cols = $_ -split "`t", 2
    [pscustomobject]@{ Old = $cols[0]; New = $cols[1] }
}
$pairs = $pairs | Sort-Object { -$_.Old.Length }

function Apply-TodoToDonePrefixes([string]$text, [bool]$inCardTodo) {
    # Remaining Todo card files reference many types that still live under Todo; only FQN map applies there.
    if ($inCardTodo) { return $text }
    $monsterSubs = @('Effect', 'Fusion', 'Ritual', 'Normal', 'TrapMonster')
    $spellSubs = @('Normal', 'Continuos', 'Field', 'Equip')
    $trapSubs = @('Normal', 'Continuos')
    foreach ($sub in $monsterSubs) {
        $text = $text -replace "YgoDuelist\.YgoDuelistCode\.Cards\.Monster\.Todo\.$sub\.", "YgoDuelist.YgoDuelistCode.Cards.Monster.Done.$sub."
    }
    foreach ($sub in $spellSubs) {
        $text = $text -replace "YgoDuelist\.YgoDuelistCode\.Cards\.Spell\.Todo\.$sub\.", "YgoDuelist.YgoDuelistCode.Cards.Spell.Done.$sub."
    }
    foreach ($sub in $trapSubs) {
        $text = $text -replace "YgoDuelist\.YgoDuelistCode\.Cards\.Trap\.Todo\.$sub\.", "YgoDuelist.YgoDuelistCode.Cards.Trap.Done.$sub."
    }
    return $text
}

function Update-FileContent([string]$path, [string]$text) {
    $rel = $path.Substring($codeRoot.Length)
    $inCardTodo = $rel -match '[\\/]Cards[\\/](Monster|Spell|Trap)[\\/]Todo[\\/]'
    foreach ($p in $pairs) {
        if ($p.Old.Length -lt 10) { continue }
        $text = $text.Replace($p.Old, $p.New)
    }
    $text = Apply-TodoToDonePrefixes $text $inCardTodo
    return $text
}

$files = Get-ChildItem -Path $codeRoot -Filter "*.cs" -Recurse -File
$n = 0
foreach ($f in $files) {
    $t = [System.IO.File]::ReadAllText($f.FullName)
    $t2 = Update-FileContent $f.FullName $t
    if ($t2 -ne $t) {
        [System.IO.File]::WriteAllText($f.FullName, $t2)
        $n++
    }
}
Write-Host "Updated" $n "files"
