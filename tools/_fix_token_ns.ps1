$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\YgoDuelistCode"))
Get-ChildItem -Path $root -Filter *.cs -Recurse | ForEach-Object {
    $t = [IO.File]::ReadAllText($_.FullName)
    $n = $t.Replace('YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token', 'YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token')
    if ($n -ne $t) { [IO.File]::WriteAllText($_.FullName, $n); Write-Host $_.FullName }
}
