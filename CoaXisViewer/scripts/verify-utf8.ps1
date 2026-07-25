param(
    [string[]]$TargetPaths = @("scenes", "src", "docs", ".vscode"),
    [string[]]$IncludeExtensions = @(".cs", ".md", ".json", ".yml", ".yaml", ".txt", ".gd", ".cfg", ".tscn", ".tres")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
$invalidUtf8Files = New-Object System.Collections.Generic.List[string]
$mojibakeFiles = New-Object System.Collections.Generic.List[string]

$mojibakePattern = "�|�E|E��|チE|ノ�E|めE"

$allFiles = foreach ($path in $TargetPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        continue
    }

    Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
        $IncludeExtensions -contains $_.Extension.ToLowerInvariant()
    }
}

foreach ($file in $allFiles) {
    try {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $text = $utf8Strict.GetString($bytes)

        if ($text -match $mojibakePattern) {
            $mojibakeFiles.Add($file.FullName)
        }
    }
    catch [System.Text.DecoderFallbackException] {
        $invalidUtf8Files.Add($file.FullName)
    }
}

if ($invalidUtf8Files.Count -eq 0 -and $mojibakeFiles.Count -eq 0) {
    Write-Output "UTF-8 verification passed."
    exit 0
}

if ($invalidUtf8Files.Count -gt 0) {
    Write-Output "Invalid UTF-8 files:"
    $invalidUtf8Files | ForEach-Object { Write-Output "  $_" }
}

if ($mojibakeFiles.Count -gt 0) {
    Write-Output "Potential mojibake patterns found:"
    $mojibakeFiles | ForEach-Object { Write-Output "  $_" }
}

exit 1
