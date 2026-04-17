#requires -Version 5.1
<#
.SYNOPSIS
  Regenerates YgoDuelistCode audit artifacts: all-cs-files.txt, grep snapshots under audit/generated/.
  Not run from MSBuild — invoke manually when you want a fresh snapshot. Does not edit Cursor plan files.
  Optional strict mode: fails if Patches/Services/Nodes grep lines are not covered by card-logic-grep-allowlist.txt

.PARAMETER ProjectRoot
  Repository root (folder containing YgoDuelist.csproj).

.PARAMETER Enforce
  When true, every line in patches/services/nodes snapshots must appear as an exact line in card-logic-grep-allowlist.txt
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectRoot,
    [switch] $Enforce
)

$ErrorActionPreference = "Stop"
$YgoCode = Join-Path $ProjectRoot "YgoDuelistCode"
if (-not (Test-Path $YgoCode)) {
    Write-Error "YgoDuelistCode not found under: $ProjectRoot"
}

$Audit = Join-Path $YgoCode "audit"
$Gen = Join-Path $Audit "generated"
New-Item -ItemType Directory -Force -Path $Gen | Out-Null

function Get-RgOrSelect {
    param(
        [string] $Path,
        [string] $Pattern,
        [string] $OutFile
    )
    $lines = [System.Collections.Generic.List[string]]::new()
    if (Get-Command rg -ErrorAction SilentlyContinue) {
        & rg --line-number --no-heading --color never "$Pattern" "$Path" 2>$null | ForEach-Object { $lines.Add($_) }
    }
    else {
        Get-ChildItem -Path $Path -Filter "*.cs" -Recurse -File | ForEach-Object {
            $file = $_.FullName
            $i = 0
            foreach ($line in [System.IO.File]::ReadLines($file)) {
                $i++
                if ($line -match $Pattern) {
                    # Normalize to rg-like: path:line:content
                    $rel = $file
                    $lines.Add("${rel}:${i}:$line")
                }
            }
        }
    }
    $sorted = @($lines | Sort-Object)
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($OutFile, $sorted, $utf8NoBom)
    return $sorted
}

# --- all-cs-files.txt (absolute paths, sorted, exclude bin/obj)
$allCs = Get-ChildItem -Path $YgoCode -Filter "*.cs" -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object FullName
$allCsPath = Join-Path $Audit "all-cs-files.txt"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$allPaths = @($allCs | ForEach-Object { $_.FullName })
[System.IO.File]::WriteAllLines($allCsPath, $allPaths, $utf8NoBom)
$fileCount = $allCs.Count

# Underscore card type pattern (matches manual triage)
$underscorePattern = "\bis [A-Z][a-zA-Z0-9_]*_[a-zA-Z0-9_]+"

$patchDir = Join-Path $YgoCode "Patches"
$svcDir = Join-Path $YgoCode "Services"
$relDir = Join-Path $YgoCode "Relics"
$charDir = Join-Path $YgoCode "Character"
$powDir = Join-Path $YgoCode "Powers"

$patchesOut = Join-Path $Gen "patches-underscore-is.txt"
$servicesOut = Join-Path $Gen "services-underscore-is.txt"
$nodesOut = Join-Path $Gen "nodes-underscore-is.txt"
$rcpOut = Join-Path $Gen "relics-character-powers-underscore-is.txt"

$patchLines = @(Get-RgOrSelect -Path $patchDir -Pattern $underscorePattern -OutFile $patchesOut)
$svcLines = @(Get-RgOrSelect -Path $svcDir -Pattern $underscorePattern -OutFile $servicesOut)

$nodesDir = Join-Path $YgoCode "Nodes"
$nodeLines = @()
if (Test-Path $nodesDir) {
    $nodeLines = @(Get-RgOrSelect -Path $nodesDir -Pattern $underscorePattern -OutFile $nodesOut)
}
else {
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($nodesOut, @(), $utf8NoBom)
}

$rcpCombined = [System.Collections.Generic.List[string]]::new()
foreach ($d in @($relDir, $charDir, $powDir)) {
    if (Test-Path $d) {
        if (Get-Command rg -ErrorAction SilentlyContinue) {
            & rg --line-number --no-heading --color never "$underscorePattern" "$d" 2>$null | ForEach-Object { $rcpCombined.Add($_) }
        }
        else {
            Get-ChildItem -Path $d -Filter "*.cs" -Recurse -File | ForEach-Object {
                $file = $_.FullName
                $i = 0
                foreach ($line in [System.IO.File]::ReadLines($file)) {
                    $i++
                    if ($line -match $underscorePattern) {
                        $rcpCombined.Add("${file}:${i}:$line")
                    }
                }
            }
        }
    }
}
$rcpSorted = @($rcpCombined | Sort-Object)
[System.IO.File]::WriteAllLines($rcpOut, $rcpSorted, $utf8NoBom)

# Keldo (no underscore) in Patches
$keldoOut = Join-Path $Gen "patches-keldo.txt"
if (Get-Command rg -ErrorAction SilentlyContinue) {
    & rg --line-number --no-heading --color never "\bKeldo\b" "$patchDir" 2>$null | Sort-Object | Set-Content -Path $keldoOut -Encoding utf8
}
else {
    @() | Set-Content -Path $keldoOut -Encoding utf8
}

# --- Enforce allowlist (exact line match; optional until allowlist is populated)
if ($Enforce) {
    $allowPath = Join-Path $Audit "card-logic-grep-allowlist.txt"
    if (-not (Test-Path $allowPath)) {
        Write-Error "Enforce mode requires card-logic-grep-allowlist.txt"
    }
    $allowed = @{}
    Get-Content -Path $allowPath -Encoding UTF8 | ForEach-Object {
        $t = $_.Trim()
        if ($t.Length -gt 0 -and -not $t.StartsWith("#")) {
            $allowed[$t] = $true
        }
    }
    if ($allowed.Count -eq 0) {
        Write-Warning "Card-logic audit enforce: allowlist has no data lines — skipping strict check (add merged grep lines from generated/*.txt)."
    }
    else {
        $combined = @()
        $combined += $patchLines
        $combined += $svcLines
        $combined += $nodeLines
        foreach ($line in $combined) {
            if ($null -eq $line -or $line.Trim().Length -eq 0) { continue }
            if (-not $allowed.ContainsKey($line)) {
                Write-Error "Card-logic audit enforce: line not in allowlist:`n$line"
            }
        }
    }
}

# --- last-run stamp (diagnostic only; not a task queue)
$timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$stampPath = Join-Path $Gen "last-run.txt"
$stampText = @"
CardLogicAudit last run: $timestamp
YgoDuelistCode .cs file count (excl. bin/obj): $fileCount
Task queue: edit the Cursor plan YAML todos (card_logic_scoping_next_wave plan), not this folder.
"@
[System.IO.File]::WriteAllText($stampPath, $stampText, $utf8NoBom)

Write-Host "[CardLogicAudit] OK - $fileCount .cs files; snapshots in $Gen"
