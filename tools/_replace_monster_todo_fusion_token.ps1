$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$oldF = "YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion"
$newF = "YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion"
$oldT = "YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token"
$newT = "YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token"

Get-ChildItem -LiteralPath $root -Recurse -File -Filter "*.cs" | ForEach-Object {
    $p = $_.FullName
    if ($p -match "\\(obj|bin)\\") { return }
    $t = [IO.File]::ReadAllText($p)
    $o = $t
    $t = $t.Replace($oldF, $newF)
    $t = $t.Replace($oldT, $newT)
    if ($t -ne $o) { [IO.File]::WriteAllText($p, $t) }
}

Write-Host "Replaced Todo Fusion/Token FQNs across *.cs"
