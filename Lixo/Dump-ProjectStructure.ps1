<#
.SYNOPSIS
    Dump da estrutura do projeto (pastas + arquivos + tamanho + linhas + DTOs).
.PARAMETER SolutionPath
    Pasta raiz da solution (default: diretorio atual).
.PARAMETER OutputFile
    Se informado, salva em arquivo .txt em vez de so imprimir no console.
.PARAMETER IncludeLineCounts
    Adiciona contagem de linhas para arquivos .cs/.razor/.js/.ts/.css/.html/.cshtml/.json/.sql.
.PARAMETER ListDtos
    Extrai os nomes de classes DTO (*RequestDto / *ResponseDto) e lista agrupados.
.EXAMPLE
    .\Dump-ProjectStructure.ps1 -SolutionPath "C:\Desenvolvimento\VisualStudio\CanindePwaSolution" -OutputFile "caninde-structure.txt" -IncludeLineCounts -ListDtos
#>
param(
    [string]$SolutionPath = ".",
    [string]$OutputFile = "",
    [switch]$IncludeLineCounts,
    [switch]$ListDtos,
    [string[]]$ExcludeFolders = @("bin","obj",".vs",".git","node_modules","wwwroot\lib","wwwroot\css","wwwroot\js","Lixo"),
    [string[]]$ExcludeFiles   = @("*.user","*.suo","*.cache","*.dll","*.pdb")
)

$root = (Resolve-Path $SolutionPath).Path
if (-not (Test-Path $root)) { Write-Error "Path not found: $SolutionPath"; exit 1 }

$script:lines = [System.Collections.Generic.List[string]]::new()
function Out-Line { param([string]$t) $script:lines.Add($t) | Out-Null; if (-not $OutputFile) { Write-Host $t } }

function Get-FileSize {
    param([long]$b)
    if ($b -lt 1KB) { return "$b B" }
    if ($b -lt 1MB) { return "{0:N1} KB" -f ($b / 1KB) }
    return "{0:N1} MB" -f ($b / 1MB)
}

function Get-LineCount {
    param([string]$p)
    try { return (Get-Content -LiteralPath $p -ErrorAction Stop | Measure-Object -Line).Lines } catch { return -1 }
}

function Test-ExcludedFolder { param([string]$n) foreach ($x in $ExcludeFolders) { if ($n -ieq $x -or $n -ieq ($x -replace '.*\\','')) { return $true } } return $false }
function Test-ExcludedFile   { param([string]$n) foreach ($x in $ExcludeFiles)   { if ($n -like $x) { return $true } } return $false }

function Walk-Tree {
    param([string]$path, [string]$indent = "", [int]$depth = 0)
    if ($depth -gt 10) { Out-Line "$indent... (depth limit)"; return }

    $items = Get-ChildItem -LiteralPath $path -ErrorAction SilentlyContinue |
             Sort-Object @{Expression={ if ($_.PSIsContainer) { 0 } else { 1 } }}, Name
    $count = $items.Count; $i = 0
    foreach ($item in $items) {
        $i++
        $last = ($i -eq $count)
        $branch = if ($last) { "+-- " } else { "|-- " }
        $nextIndent = $indent + $(if ($last) { "    " } else { "|   " })

        if ($item.PSIsContainer) {
            if (Test-ExcludedFolder $item.Name) { Out-Line "$indent$branch$($item.Name)/  [skipped]"; continue }
            Out-Line "$indent$branch$($item.Name)/"
            Walk-Tree -path $item.FullName -indent $nextIndent -depth ($depth + 1)
        } else {
            if (Test-ExcludedFile $item.Name) { continue }
            $size = Get-FileSize $item.Length
            $lineInfo = ""
            if ($IncludeLineCounts) {
                $ext = $item.Extension.ToLower()
                if ($ext -in @(".cs",".razor",".js",".ts",".css",".html",".cshtml",".json",".sql",".ps1")) {
                    $lc = Get-LineCount $item.FullName
                    if ($lc -ge 0) { $lineInfo = " | $lc L" }
                }
            }
            Out-Line "$indent$branch$($item.Name)  ($size$lineInfo)"
        }
    }
}

Out-Line ""
Out-Line "======================================================================"
Out-Line " PROJECT STRUCTURE DUMP"
Out-Line "======================================================================"
Out-Line " Root:        $root"
Out-Line " Generated:   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Out-Line " LineCounts:  $IncludeLineCounts"
Out-Line " ListDtos:    $ListDtos"
Out-Line "======================================================================"
Out-Line ""

$slns     = @(Get-ChildItem -LiteralPath $root -Filter "*.sln" -ErrorAction SilentlyContinue)
$projects = @(Get-ChildItem -LiteralPath $root -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|Lixo)\\' })

foreach ($s in $slns) { Out-Line "SOLUTION: $($s.Name)" }
Out-Line ""
foreach ($p in $projects) {
    $sdk = (Select-String -LiteralPath $p.FullName -Pattern 'Sdk="([^"]+)"' -AllMatches |
            Select-Object -First 1).Matches.Groups[1].Value
    Out-Line ("PROJECT:  {0,-55}  [{1}]" -f $p.FullName.Substring($root.Length + 1), $sdk)
}
Out-Line ""

if ($ListDtos) {
    Out-Line "======================================================================"
    Out-Line " DTO CLASSES (*RequestDto / *ResponseDto)"
    Out-Line "======================================================================"
    $dtoFiles = @(Get-ChildItem -LiteralPath $root -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|Lixo)\\' })

    $req = @(); $res = @(); $other = @()
    foreach ($f in $dtoFiles) {
        $txt = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $txt) { continue }
        $m = [regex]::Matches($txt, '\b(?:class|record)\s+(\w+(?:Request|Response|Dto))\b')
        foreach ($mm in $m) {
            $n = $mm.Groups[1].Value
            $rel = $f.FullName.Substring($root.Length + 1)
            if ($n -match 'RequestDto$') { $req += "$n  <-  $rel" }
            elseif ($n -match 'ResponseDto$') { $res += "$n  <-  $rel" }
            else { $other += "$n  <-  $rel" }
        }
    }
    Out-Line "--- RequestDtos ($($req.Count)) ---"
    $req | Sort-Object | ForEach-Object { Out-Line "  $_" }
    Out-Line ""
    Out-Line "--- ResponseDtos ($($res.Count)) ---"
    $res | Sort-Object | ForEach-Object { Out-Line "  $_" }
    Out-Line ""
    Out-Line "--- Outros DTOs ($($other.Count)) ---"
    $other | Sort-Object | ForEach-Object { Out-Line "  $_" }
    Out-Line ""
}

foreach ($p in $projects) {
    $projDir = $p.Directory.FullName
    Out-Line "======================================================================"
    Out-Line " PROJECT: $($p.BaseName)"
    Out-Line " Path:    $projDir"
    Out-Line "======================================================================"
    Out-Line "$($p.BaseName)/"
    Walk-Tree -path $projDir -indent ""
    Out-Line ""
}

$allCs    = @(Get-ChildItem -LiteralPath $root -Recurse -Filter "*.cs"    -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|Lixo)\\' })
$allRazor = @(Get-ChildItem -LiteralPath $root -Recurse -Include "*.razor","*.cshtml" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|Lixo)\\' })
$allJs    = @(Get-ChildItem -LiteralPath $root -Recurse -Include "*.js","*.ts"  -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|Lixo)\\' })
$allSql   = @(Get-ChildItem -LiteralPath $root -Recurse -Filter "*.sql"   -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|Lixo)\\' })

Out-Line "======================================================================"
Out-Line " SUMMARY"
Out-Line "======================================================================"
Out-Line ("  Solutions:       {0}" -f $slns.Count)
Out-Line ("  Projects:        {0}" -f $projects.Count)
Out-Line ("  .cs files:       {0}" -f $allCs.Count)
Out-Line ("  .razor/.cshtml:  {0}" -f $allRazor.Count)
Out-Line ("  .js/.ts files:   {0}" -f $allJs.Count)
Out-Line ("  .sql files:      {0}" -f $allSql.Count)
Out-Line "======================================================================"

if ($OutputFile) {
    $script:lines -join [Environment]::NewLine | Out-File -FilePath $OutputFile -Encoding utf8
    Write-Host ""
    Write-Host "Output: $OutputFile" -ForegroundColor Green
    Write-Host "Tamanho: $((Get-Item $OutputFile).Length) bytes"
}