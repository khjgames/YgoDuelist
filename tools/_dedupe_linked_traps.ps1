$trapTodo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\YgoDuelistCode\Cards\Trap\Todo"))
$linkedDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\YgoDuelistCode\Cards\Trap\Done\Linked"))
if (-not (Test-Path $linkedDir)) { exit 0 }
Get-ChildItem $linkedDir -Filter *.cs -File | ForEach-Object {
    $name = $_.Name
    foreach ($sub in @("Normal", "Continuos")) {
        $p = Join-Path (Join-Path $trapTodo $sub) $name
        if (Test-Path -LiteralPath $p) {
            Remove-Item -LiteralPath $p -Force
            if (Test-Path -LiteralPath ($p + ".uid")) { Remove-Item -LiteralPath ($p + ".uid") -Force }
            Write-Host "Removed linked duplicate:" $p
        }
    }
}
