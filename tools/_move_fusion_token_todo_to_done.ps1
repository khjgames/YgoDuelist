$ErrorActionPreference = "Stop"
$monsterRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\YgoDuelistCode\Cards\Monster"))

foreach ($sub in @("Fusion", "Token")) {
    $src = Join-Path $monsterRoot "Todo\$sub"
    $dst = Join-Path $monsterRoot "Done\$sub"
    if (-not (Test-Path $src)) { continue }
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    Get-ChildItem -LiteralPath $src -File | ForEach-Object {
        Move-Item -LiteralPath $_.FullName -Destination (Join-Path $dst $_.Name) -Force
    }
}

# Namespace + usings inside moved trees only
$nsTodoFusion = "YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion"
$nsDoneFusion = "YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion"
$nsTodoToken = "YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token"
$nsDoneToken = "YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token"

foreach ($sub in @("Fusion", "Token")) {
    $dir = Join-Path $monsterRoot "Done\$sub"
    if (-not (Test-Path $dir)) { continue }
    Get-ChildItem -LiteralPath $dir -Filter "*.cs" -File | ForEach-Object {
        $t = [IO.File]::ReadAllText($_.FullName)
        $t = $t.Replace($nsTodoFusion, $nsDoneFusion)
        $t = $t.Replace($nsTodoToken, $nsDoneToken)
        [IO.File]::WriteAllText($_.FullName, $t)
    }
}

Write-Host "Moved Fusion+Token to Done and updated namespaces in those files."
