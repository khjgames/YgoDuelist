$cards = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\YgoDuelistCode\Cards"))
Get-ChildItem -Path $cards -Recurse -Filter *.cs -File | Where-Object { $_.FullName -match '\\Done\\' } | ForEach-Object {
    $donePath = $_.FullName
    $todoPath = $donePath -replace '\\Done\\', '\Todo\'
    if (Test-Path -LiteralPath $todoPath) {
        Remove-Item -LiteralPath $todoPath -Force
        if (Test-Path -LiteralPath ($todoPath + ".uid")) {
            Remove-Item -LiteralPath ($todoPath + ".uid") -Force
        }
        Write-Host "Removed duplicate:" $todoPath
    }
}
