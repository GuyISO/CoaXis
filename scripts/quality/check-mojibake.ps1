param(
    [string]$Root = "CoaXisViewer/src",
    [string[]]$Extensions = @("*.cs", "*.md", "*.json", "*.gd", "*.tscn")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$targetRoot = Join-Path $repoRoot $Root

if (-not (Test-Path -LiteralPath $targetRoot)) {
    Write-Error "Target path not found: $targetRoot"
}

# 文字化け事故で実際に出たパターン + 代表的な replacement char を検査する。
$patterns = @(
    [char]0xFFFD,
    "�E",
    "、E",
    "冁E",
    "琁E",
    "チE",
    "斁E",
    "縲",
    "莠",
    "繧"
)

$files = foreach ($extension in $Extensions) {
    Get-ChildItem -Path $targetRoot -Recurse -File -Filter $extension
}

$hits = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    $text = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName

    foreach ($pattern in $patterns) {
        if ($text.Contains([string]$pattern)) {
            $lineMatches = Select-String -LiteralPath $file.FullName -Pattern ([regex]::Escape([string]$pattern)) -SimpleMatch
            foreach ($lineMatch in $lineMatches) {
                $hits.Add([PSCustomObject]@{
                    Path = $file.FullName.Replace($repoRoot + [IO.Path]::DirectorySeparatorChar, "")
                    Line = $lineMatch.LineNumber
                    Pattern = [string]$pattern
                    Preview = $lineMatch.Line.Trim()
                })
            }
        }
    }
}

if ($hits.Count -gt 0) {
    Write-Host "Detected potential mojibake patterns:" -ForegroundColor Red
    foreach ($hit in $hits | Sort-Object Path, Line, Pattern -Unique) {
        Write-Host ("{0}:{1} [{2}] {3}" -f $hit.Path, $hit.Line, $hit.Pattern, $hit.Preview)
    }

    Write-Error "Mojibake check failed. Please fix encoding-corrupted text before merge."
    exit 1
}

Write-Host "Mojibake check passed." -ForegroundColor Green
