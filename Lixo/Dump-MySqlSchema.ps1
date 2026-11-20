<#
.SYNOPSIS
    Dump completo do schema MySQL: tabelas, colunas, tipos, PKs, FKs.
.DESCRIPTION
    Resolve o problema do prefixo Cd misturado (tblPessoa vs tblCdPessoaEmbarcador)
    listando o estado REAL do banco, nao o script de 2018.
.PARAMETER Server
    Host do MySQL (default: localhost).
.PARAMETER Port
    Porta (default: 3306).
.PARAMETER User
    Usuario.
.PARAMETER Password
    Senha. Se vazio, tenta ler do Registry HKCU\SOFTWARE\Caninde\Secrets\DbConnection.
.PARAMETER Database
    Nome do banco (default: soletix_caninde).
.PARAMETER OutputFile
    Arquivo de saida.
.PARAMETER MysqlExe
    Caminho do mysql.exe (se nao estiver no PATH).
.EXAMPLE
    .\Dump-MySqlSchema.ps1 -Server localhost -User root -Password "Edu@91737533"
#>
param(
    [string]$Server     = "localhost",
    [int]   $Port       = 3306,
    [string]$User       = "root",
    [string]$Password   = "",
    [string]$Database   = "soletix_caninde",
    [string]$OutputFile = "caninde-db-schema.txt",
    [string]$MysqlExe   = "mysql"
)

# --- 1. Resolver credenciais (Registry como fallback) ---
if (-not $Password) {
    $regPath = "HKCU:\SOFTWARE\Caninde\Secrets"
    if (Test-Path $regPath) {
        $connStr = (Get-ItemProperty -Path $regPath -Name "DbConnection" -ErrorAction SilentlyContinue).DbConnection
        if ($connStr) {
            if ($connStr -match 'server=([^;]+)')   { $Server   = $Matches[1] }
            if ($connStr -match 'port=([^;]+)')     { $Port     = [int]$Matches[1] }
            if ($connStr -match 'database=([^;]+)') { $Database = $Matches[1] }
            if ($connStr -match 'user(id)?=([^;]+)'){ $User     = $Matches[2] }
            if ($connStr -match 'password=([^;]+)') { $Password = $Matches[1] }
            Write-Host "Credenciais lidas do Registry" -ForegroundColor Green
        }
    }
}

# --- 2. Localizar mysql.exe ---
$mysqlPath = $MysqlExe
if (-not (Get-Command $MysqlExe -ErrorAction SilentlyContinue)) {
    $candidates = @(
        "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
        "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe",
        "C:\xampp\mysql\bin\mysql.exe",
        "C:\wamp64\bin\mysql\mysql8.0.31\bin\mysql.exe",
        "C:\wamp\bin\mysql\mysql5.7.36\bin\mysql.exe"
    )
    $mysqlPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $mysqlPath) {
        Write-Error "mysql.exe nao encontrado. Instale MySQL Client ou passe -MysqlExe 'C:\caminho\mysql.exe'"
        exit 1
    }
    Write-Host "mysql.exe: $mysqlPath" -ForegroundColor Green
}

# --- 3. Config temporaria (evita warning de senha na CLI) ---
$tmpCnf = [System.IO.Path]::GetTempFileName()
@"
[client]
host=$Server
port=$Port
user=$User
password=$Password
"@ | Set-Content -Path $tmpCnf -Encoding ASCII

function Invoke-MySql {
    param([string]$Query)
    & $mysqlPath --defaults-extra-file=$tmpCnf --database=$Database `
        --batch --skip-column-names -e "$Query" 2>&1
}

# --- 4. Testar conexao ---
$test = Invoke-MySql "SELECT 1"
if ($LASTEXITCODE -ne 0 -or $test -notmatch '^1') {
    Write-Error "Falha na conexao: $test"
    Remove-Item $tmpCnf -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "Conectado a $Server`:$Port/$Database" -ForegroundColor Green

# --- 5. Listar tabelas e views ---
Write-Host "-> Listando tabelas..." -ForegroundColor Cyan
$allTables = @(Invoke-MySql "SHOW FULL TABLES" | ForEach-Object {
    $p = $_ -split "`t"
    [pscustomobject]@{ Name = $p[0]; Type = $p[1] }
})

$tables = $allTables | Where-Object { $_.Type -eq "BASE TABLE" } | Select-Object -ExpandProperty Name
$views  = $allTables | Where-Object { $_.Type -eq "VIEW" }       | Select-Object -ExpandProperty Name

Write-Host "  Tabelas: $($tables.Count)  |  Views: $($views.Count)" -ForegroundColor Green

# --- 6. Analise Cd-prefix ---
$withCd    = $tables | Where-Object { $_ -match '^tblCd' }
$withoutCd = $tables | Where-Object { $_ -notmatch '^tblCd' }

# --- 7. Coletar colunas ---
Write-Host "-> Coletando colunas (pode demorar)..." -ForegroundColor Cyan
$tableData = @{}
$i = 0
foreach ($t in $tables) {
    $i++
    Write-Progress -Activity "Lendo tabelas" -Status "$i/$($tables.Count): $t" `
                   -PercentComplete (($i / $tables.Count) * 100)
    $cols = @(Invoke-MySql "SHOW FULL COLUMNS FROM ``$t``" | ForEach-Object {
        $p = $_ -split "`t"
        [pscustomobject]@{
            Field   = $p[0]
            Type    = $p[1]
            Null    = $p[2]
            Key     = $p[3]
            Default = if ($p[4] -eq "NULL") { $null } else { $p[4] }
            Extra   = $p[5]
        }
    })
    $tableData[$t] = $cols
}
Write-Progress -Activity "Lendo tabelas" -Completed

# --- 8. Coletar FKs ---
Write-Host "-> Coletando foreign keys..." -ForegroundColor Cyan
$fkRaw = Invoke-MySql @"
SELECT
    kcu.TABLE_NAME,
    kcu.COLUMN_NAME,
    kcu.CONSTRAINT_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE kcu
WHERE kcu.TABLE_SCHEMA = '$Database'
  AND kcu.REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY kcu.TABLE_NAME, kcu.CONSTRAINT_NAME;
"@
$fks = @($fkRaw | ForEach-Object {
    $p = $_ -split "`t"
    if ($p.Count -ge 5) {
        [pscustomobject]@{
            Table      = $p[0]; Column = $p[1]; Constraint = $p[2]
            RefTable   = $p[3]; RefColumn = $p[4]
        }
    }
})

# --- 9. Montar relatorio ---
$sb = [System.Text.StringBuilder]::new()
function W { param([string]$t = "") [void]$sb.AppendLine($t) }

W "======================================================================"
W " CANINDE - DATABASE SCHEMA DUMP"
W "======================================================================"
W " Server:       $Server`:$Port"
W " Database:     $Database"
W " Generated:    $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
W " Tables:       $($tables.Count)"
W " Views:        $($views.Count)"
W " Foreign Keys: $($fks.Count)"
W "======================================================================"
W ""

W "======================================================================"
W " PREFIX ANALYSIS - tblCd* vs tbl*"
W "======================================================================"
W ""
W " > COM prefixo Cd ($($withCd.Count) tabelas):"
$withCd | Sort-Object | ForEach-Object { W "     $_" }
W ""
W " > SEM prefixo Cd ($($withoutCd.Count) tabelas):"
$withoutCd | Sort-Object | ForEach-Object { W "     $_" }
W ""

W "======================================================================"
W " TABLES - ESTRUTURA COMPLETA"
W "======================================================================"
W ""

foreach ($t in ($tables | Sort-Object)) {
    $cols = $tableData[$t]
    $pks  = @($cols | Where-Object { $_.Key -eq "PRI" } | Select-Object -ExpandProperty Field)
    $tFks = @($fks | Where-Object { $_.Table -eq $t })

    W "----------------------------------------------------------------------"
    W " TABLE: $t"
    W " Columns: $($cols.Count)  |  PK: $($pks -join ', ')  |  FKs: $($tFks.Count)"
    W "----------------------------------------------------------------------"
    foreach ($c in $cols) {
        $flags = @()
        if ($c.Key -eq "PRI") { $flags += "PK" }
        if ($c.Key -eq "UNI") { $flags += "UQ" }
        if ($c.Key -eq "MUL") { $flags += "IX" }
        if ($c.Extra -match "auto_increment") { $flags += "AI" }
        $flagStr = if ($flags.Count) { " [$($flags -join ',')]" } else { "" }
        $nullStr = if ($c.Null -eq "NO") { "NOT NULL" } else { "NULL    " }
        W ("   {0,-28} {1,-22} {2} {3}" -f $c.Field, $c.Type, $nullStr, $flagStr)
    }
    if ($tFks.Count -gt 0) {
        W ""
        W "   Foreign Keys:"
        foreach ($fk in $tFks) {
            W ("     {0}.{1}  ->  {2}.{3}  ({4})" -f $fk.Table, $fk.Column, $fk.RefTable, $fk.RefColumn, $fk.Constraint)
        }
    }
    W ""
}

if ($views.Count -gt 0) {
    W "======================================================================"
    W " VIEWS - COLUNAS"
    W "======================================================================"
    W ""
    foreach ($v in ($views | Sort-Object)) {
        $vCols = @(Invoke-MySql "SHOW COLUMNS FROM ``$v``" | ForEach-Object {
            ($_ -split "`t")[0]
        })
        W " VIEW: $v"
        W "   Columns: $($vCols -join ', ')"
        W ""
    }
}

# --- 10. Escrever arquivo ---
$sb.ToString() | Out-File -FilePath $OutputFile -Encoding utf8
Remove-Item $tmpCnf -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Relatorio gerado: $OutputFile" -ForegroundColor Green
Write-Host "Tamanho: $((Get-Item $OutputFile).Length) bytes"
Write-Host ""
Write-Host "RESUMO:" -ForegroundColor Yellow
Write-Host "  Tabelas:       $($tables.Count)"
Write-Host "  Views:         $($views.Count)"
Write-Host "  Com 'Cd':      $($withCd.Count)"
Write-Host "  Sem 'Cd':      $($withoutCd.Count)"
Write-Host "  Foreign Keys:  $($fks.Count)"