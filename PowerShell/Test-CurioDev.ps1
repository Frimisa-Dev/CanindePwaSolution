# ============================================================================
# Test-CurioDev.ps1
# Valida se o ambiente Development do Curio esta 100% configurado
# ============================================================================

[CmdletBinding()]
param()

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " VALIDACAO CURIO DEV " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$errors   = 0
$warnings = 0

function Check-OK   { param($msg) Write-Host "  OK  $msg" -ForegroundColor Green }
function Check-Fail { param($msg) Write-Host "  XX  $msg" -ForegroundColor Red; $script:errors++ }
function Check-Warn { param($msg) Write-Host "  !!  $msg" -ForegroundColor Yellow; $script:warnings++ }
function Check-Info { param($msg) Write-Host "      $msg" -ForegroundColor DarkGray }

# ============================================================================
# [1/7] CERTIFICADOS
# ============================================================================
Write-Host "`n[1/7] CERTIFICADOS" -ForegroundColor Yellow

# 1.1 CA Raiz em CurrentUser\My
$rootCA_CU = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -eq "CN=CURIO-ROOT-CA" }
if ($rootCA_CU) {
    Check-OK "CA Raiz em CurrentUser\My"
    Check-Info "Thumbprint: $($rootCA_CU.Thumbprint)"
    Check-Info "Validade:   $($rootCA_CU.NotAfter.ToString('dd/MM/yyyy'))"
} else {
    Check-Fail "CA Raiz NAO encontrada em CurrentUser\My"
}

# 1.2 CA Raiz em LocalMachine\Root (trusted)
$rootCA_LM = Get-ChildItem Cert:\LocalMachine\Root -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -eq "CN=CURIO-ROOT-CA" }
if ($rootCA_LM) {
    Check-OK "CA Raiz em LocalMachine\Root (trusted)"
} else {
    Check-Fail "CA Raiz NAO esta em LocalMachine\Root (navegador vai reclamar)"
}

# 1.3 Certificado folha em CurrentUser\My
$leafCU = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
    Where-Object { $_.FriendlyName -eq "CURIO-DEV-HTTPS" }
if ($leafCU) {
    Check-OK "Certificado CURIO-DEV-HTTPS em CurrentUser\My"
    Check-Info "Thumbprint: $($leafCU.Thumbprint)"
    Check-Info "Validade:   $($leafCU.NotAfter.ToString('dd/MM/yyyy'))"
    Check-Info "DNS Names:  $($leafCU.DnsNameList -join ', ')"
    if (-not $leafCU.HasPrivateKey) {
        Check-Fail "Certificado SEM chave privada!"
    }
} else {
    Check-Fail "Certificado CURIO-DEV-HTTPS NAO em CurrentUser\My"
}

# 1.4 Certificado folha em LocalMachine\My
$leafLM = Get-ChildItem Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
    Where-Object { $_.FriendlyName -eq "CURIO-DEV-HTTPS" }
if ($leafLM) {
    Check-OK "Certificado CURIO-DEV-HTTPS em LocalMachine\My (Kestrel le daqui)"
    if (-not $leafLM.HasPrivateKey) {
        Check-Fail "Certificado SEM chave privada em LocalMachine!"
    }
    $thumbprint_leaf = $leafLM.Thumbprint
} else {
    Check-Fail "Certificado CURIO-DEV-HTTPS NAO em LocalMachine\My"
    $thumbprint_leaf = $null
}

# 1.5 Arquivo PFX
$pfxPath = "C:\Certificados\CURIO\CURIO-HTTPS.pfx"
if (Test-Path $pfxPath) {
    Check-OK "Arquivo PFX existe: $pfxPath"
} else {
    Check-Warn "Arquivo PFX NAO encontrado (nao e critico)"
}

# ============================================================================
# [2/7] ENV VAR CURIO__ENVIRONMENT
# ============================================================================
Write-Host "`n[2/7] ENV VAR CURIO__ENVIRONMENT" -ForegroundColor Yellow

$envMachine = [Environment]::GetEnvironmentVariable("CURIO__ENVIRONMENT", "Machine")
$envProcess = $env:CURIO__ENVIRONMENT

if ($envMachine -eq "Development") {
    Check-OK "CURIO__ENVIRONMENT=Development (Machine)"
} elseif ($envMachine) {
    Check-Fail "CURIO__ENVIRONMENT na Machine esta como '$envMachine' (esperado: Development)"
} else {
    Check-Fail "CURIO__ENVIRONMENT NAO definida na Machine"
}

if ($envProcess) {
    Check-OK "CURIO__ENVIRONMENT visivel no processo atual: $envProcess"
} else {
    Check-Warn "CURIO__ENVIRONMENT NAO visivel no processo atual - FECHE e REABRA o PowerShell"
}

# ============================================================================
# [3/7] REGISTRY HKCU:\SOFTWARE\Curio\Secrets
# ============================================================================
Write-Host "`n[3/7] REGISTRY HKCU:\SOFTWARE\Curio\Secrets" -ForegroundColor Yellow

$regPath = "HKCU:\SOFTWARE\Curio\Secrets"
if (-not (Test-Path $regPath)) {
    Check-Fail "Registry NAO existe: $regPath"
} else {
    Check-OK "Registry existe: $regPath"

    $reg = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
    $regKeys = $reg.PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' }
    Check-Info "Total de chaves: $($regKeys.Count)"

    # Chaves esperadas (as 26 que o setup cria)
    $expectedKeys = @(
        "BackendHttpsPort", "ServerHealthPort", "BackendUseHttps",
        "BackendUseHttpsRedirection", "BackendCertificateThumbprint",
        "BackendCertificateStoreLocation",
        "DbConnection",
        "JwtKey", "JwtIssuer", "JwtAudience", "JwtAccessTokenMinutes", "JwtRefreshTokenDays",
        "EmailHost", "EmailPort", "EmailUser", "EmailPassword", "EmailFrom",
        "EmailUseSsl", "EmailUseStartTls",
        "FrontendBackendUrl",
        "AdminUsername", "AdminEmail", "AdminPassword", "AdminTipoUsuario"
    )

    $missing = @()
    foreach ($key in $expectedKeys) {
        if (-not $reg.$key) {
            $missing += $key
        }
    }

    if ($missing.Count -eq 0) {
        Check-OK "Todas as $($expectedKeys.Count) chaves esperadas presentes"
    } else {
        Check-Fail "Chaves ausentes ($($missing.Count)):"
        foreach ($m in $missing) { Check-Info "  - $m" }
    }

    # Validacoes especificas
    if ($thumbprint_leaf -and $reg.BackendCertificateThumbprint -eq $thumbprint_leaf) {
        Check-OK "BackendCertificateThumbprint bate com certificado instalado"
    } elseif ($thumbprint_leaf) {
        Check-Fail "BackendCertificateThumbprint ($($reg.BackendCertificateThumbprint)) != cert instalado ($thumbprint_leaf)"
    }

    if ($reg.BackendHttpsPort -eq "7199") {
        Check-OK "BackendHttpsPort = 7199"
    } else {
        Check-Warn "BackendHttpsPort = $($reg.BackendHttpsPort) (esperado: 7199)"
    }

    if ($reg.ServerHealthPort -eq "7198") {
        Check-OK "ServerHealthPort = 7198"
    } else {
        Check-Warn "ServerHealthPort = $($reg.ServerHealthPort) (esperado: 7198)"
    }

    if ($reg.DbConnection -like "*database=curio*") {
        Check-OK "DbConnection aponta para database=curio"
    } else {
        Check-Fail "DbConnection NAO aponta para 'curio'"
        Check-Info "Valor atual: $($reg.DbConnection)"
    }

    if ($reg.JwtKey -and $reg.JwtKey.Length -ge 32) {
        Check-OK "JwtKey tem >= 32 caracteres ($($reg.JwtKey.Length))"
    } else {
        Check-Fail "JwtKey curta demais (minimo 32 bytes)"
    }
}

# ============================================================================
# [4/7] REGISTRY HKLM (nao deve existir em Dev)
# ============================================================================
Write-Host "`n[4/7] REGISTRY HKLM (deve estar vazio em Dev)" -ForegroundColor Yellow

$regLM = "HKLM:\SOFTWARE\Curio\Secrets"
if (Test-Path $regLM) {
    Check-Warn "HKLM:\SOFTWARE\Curio\Secrets EXISTE (nao esperado em Dev)"
} else {
    Check-OK "HKLM:\SOFTWARE\Curio\Secrets ausente (correto para Dev)"
}

# ============================================================================
# [5/7] BANCO DE DADOS
# ============================================================================
Write-Host "`n[5/7] BANCO DE DADOS" -ForegroundColor Yellow

$mysqlExe = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
if (Test-Path $mysqlExe) {
    Check-OK "mysql.exe encontrado"
} else {
    Check-Fail "mysql.exe NAO encontrado no caminho padrao"
}

$mysqlService = Get-Service MySQL80 -ErrorAction SilentlyContinue
if ($mysqlService -and $mysqlService.Status -eq "Running") {
    Check-OK "MySQL80 rodando"
} else {
    Check-Fail "MySQL80 NAO esta rodando"
}

# ============================================================================
# [6/7] PROJETO .NET
# ============================================================================
Write-Host "`n[6/7] PROJETO .NET" -ForegroundColor Yellow

$projectPath = "C:\Desenvolvimento\VisualStudio\CurioPwaSolution\CurioWebServer"
if (Test-Path $projectPath) {
    Check-OK "Projeto existe: $projectPath"
} else {
    Check-Fail "Projeto NAO encontrado em: $projectPath"
}

$csproj = Join-Path $projectPath "CurioWebServer.csproj"
if (Test-Path $csproj) {
    Check-OK "CurioWebServer.csproj presente"
} else {
    Check-Fail "CurioWebServer.csproj NAO encontrado"
}

$sdk = dotnet --list-sdks 2>$null
if ($sdk) {
    Check-OK "SDK .NET disponivel:"
    $sdk | ForEach-Object { Check-Info $_ }
} else {
    Check-Fail "dotnet SDK NAO encontrado no PATH"
}

# ============================================================================
# [7/7] PORTAS LIVRES
# ============================================================================
Write-Host "`n[7/7] PORTAS LIVRES" -ForegroundColor Yellow

$portsToCheck = @(7199, 7198, 5099, 7201)
foreach ($port in $portsToCheck) {
    $conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($conn) {
        Check-Warn "Porta $port JA em uso (PIDs: $($conn.OwningProcess -join ','))"
    } else {
        Check-OK "Porta $port livre"
    }
}

# ============================================================================
# RESUMO FINAL
# ============================================================================
Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host " RESUMO " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

if ($errors -eq 0 -and $warnings -eq 0) {
    Write-Host " AMBIENTE 100% OK " -ForegroundColor Green
    Write-Host ""
    Write-Host "PROXIMO PASSO:" -ForegroundColor Yellow
    Write-Host "  1. FECHE este PowerShell e abra um NOVO (para ler env var)" -ForegroundColor White
    Write-Host "  2. cd $projectPath" -ForegroundColor White
    Write-Host "  3. dotnet run" -ForegroundColor White
    Write-Host "  4. Acesse: https://localhost:7199/api-docs" -ForegroundColor White
}
elseif ($errors -eq 0) {
    Write-Host " AMBIENTE OK COM $warnings AVISO(S) " -ForegroundColor Yellow
    Write-Host "  Nenhum erro critico. Pode prosseguir." -ForegroundColor Gray
    Write-Host ""
    Write-Host "PROXIMO PASSO:" -ForegroundColor Yellow
    Write-Host "  1. FECHE este PowerShell e abra um NOVO" -ForegroundColor White
    Write-Host "  2. cd $projectPath" -ForegroundColor White
    Write-Host "  3. dotnet run" -ForegroundColor White
}
else {
    Write-Host " AMBIENTE COM $errors ERRO(S) E $warnings AVISO(S) " -ForegroundColor Red
    Write-Host "  Corrija os itens marcados com XX antes de rodar" -ForegroundColor Gray
    Write-Host "  Rode novamente: .\Setup-CurioDev.ps1" -ForegroundColor Gray
}
Write-Host ""
