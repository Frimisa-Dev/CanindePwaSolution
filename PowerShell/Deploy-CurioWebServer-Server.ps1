# ==============================================================
# Deploy-CurioWebServer-Server.ps1 (com parada do AppPool)
# Versão: Profissional / Enterprise
# Hospedagem: IIS (recomendado)
# ==============================================================
Write-Host "=== Deploy CurioWebServer (IIS) ===" -ForegroundColor Cyan

# 0) Verificar modo administrador
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERRO: Execute o script como ADMINISTRADOR!" -ForegroundColor Red
    exit 1
}

# 1) Liberar execução de scripts (apenas para esta sessão)
Set-ExecutionPolicy RemoteSigned -Scope Process -Force

# 2) Caminhos reais do servidor (ajuste se necessário)
$source = "C:\Publicar\CurioWebServer\CurioWebServer"
$reportsSource = "C:\Publicar\CurioWebServer\Relatorios"
$dest = "C:\inetpub\wwwroot\CurioWebServer"
$reportsDest = "C:\Data\Curio\Relatorios"

$appPool = "CurioWebServerPool"
$siteName = "CurioWebServer"
$bindingPort = 5099

# 3) Importar módulo IIS logo no início
Import-Module WebAdministration

# 4) Validar pasta de origem
if (-not (Test-Path $source)) {
    Write-Host "ERRO: Pasta publicada NÃO encontrada: $source" -ForegroundColor Red
    exit 1
}

# ✅ CONFIGURAÇÃO DE ROLLBACK
$rollbackEnabled = $true
$originalState = @{
    AppPoolExists = (Test-Path "IIS:\AppPools\$appPool")
    AppPoolState = if ((Test-Path "IIS:\AppPools\$appPool")) { (Get-WebAppPoolState -Name $appPool).Value } else { $null }
}

# 5) PARAR O APPLICATION POOL (CRITICAL!)
if (Test-Path "IIS:\AppPools\$appPool") {
    Write-Host "Parando Application Pool..." -ForegroundColor Yellow
    Stop-WebAppPool -Name $appPool
    
    # Aguardar até que realmente pare
    $timeout = 30
    $elapsed = 0
    while ((Get-WebAppPoolState -Name $appPool).Value -ne "Stopped" -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $elapsed++
        Write-Host "." -NoNewline
    }
    Write-Host ""
    
    if ((Get-WebAppPoolState -Name $appPool).Value -eq "Stopped") {
        Write-Host "Application Pool parado com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "AVISO: Timeout ao parar o AppPool. Tentando continuar..." -ForegroundColor Yellow
    }
} else {
    Write-Host "Application Pool ainda não existe (será criado)." -ForegroundColor Yellow
}

# 6) Criar backup da versão antiga
if (Test-Path $dest) {
    $backupPath = "C:\inetpub\wwwroot\CurioWebServer_backup_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    Write-Host "Criando backup em: $backupPath" -ForegroundColor Yellow
    Copy-Item $dest $backupPath -Recurse
}

# 7) Copiar versão nova
Write-Host "Publicando nova versão..." -ForegroundColor Yellow
if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }

# Tentar copiar com retry se necessário
$maxRetries = 3
$retryCount = 0
$copySuccess = $false

while (-not $copySuccess -and $retryCount -lt $maxRetries) {
    try {
        Copy-Item "$source\*" "$dest" -Recurse -Force -ErrorAction Stop
        $copySuccess = $true
        Write-Host "Arquivos copiados com sucesso!" -ForegroundColor Green
    } catch {
        $retryCount++
        Write-Host "Tentativa $retryCount falhou. Aguardando 2 segundos..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

if (-not $copySuccess) {
    Write-Host "ERRO: Não foi possível copiar os arquivos após $maxRetries tentativas!" -ForegroundColor Red
    Write-Host "Iniciando AppPool novamente..." -ForegroundColor Yellow
    Start-WebAppPool -Name $appPool
    exit 1
}

# 8) Copiar relatórios
Write-Host "Sincronizando relatórios..." -ForegroundColor Yellow
if (-not (Test-Path $reportsDest)) { New-Item -ItemType Directory -Path $reportsDest | Out-Null }
Copy-Item "$reportsSource\*" "$reportsDest" -Recurse -Force

# 9) Criar AppPool (se não existir)
if (-not (Test-Path "IIS:\AppPools\$appPool")) {
    Write-Host "Criando Application Pool $appPool ..." -ForegroundColor Yellow
    New-WebAppPool -Name $appPool
    Set-ItemProperty IIS:\AppPools\$appPool -Name managedRuntimeVersion -Value ""
}

# 10) Criar Site no IIS (se não existir)
if (-not (Test-Path "IIS:\Sites\$siteName")) {
    Write-Host "Criando site no IIS..." -ForegroundColor Yellow
    New-Website -Name $siteName -Port $bindingPort -IPAddress "*" -PhysicalPath $dest -ApplicationPool $appPool
}

# 11) Ajustar permissões
$identity = "IIS AppPool\$appPool"
Write-Host "Ajustando permissões para $identity ..." -ForegroundColor Yellow

$grantWebroot = "$($identity):(OI)(CI)(RX)"
$grantReports = "$($identity):(OI)(CI)(M)"

icacls $dest /grant $grantWebroot /T | Out-Null
icacls $reportsDest /grant $grantReports /T | Out-Null

# 12) ✅ CONFIGURAÇÃO SEGURA - NÃO COMMITAR CREDENCIAIS
Write-Host "Configurando variáveis de ambiente (Machine)..." -ForegroundColor Yellow
Write-Host "AVISO: Configure manualmente as credenciais no servidor!" -ForegroundColor Magenta
Write-Host "Variáveis necessárias:" -ForegroundColor Cyan
Write-Host " - CURIO__DB__DefaultConnection" -ForegroundColor Gray
Write-Host " - CURIO__JWT__Key" -ForegroundColor Gray  
Write-Host " - CURIO__EMAIL__*" -ForegroundColor Gray
Write-Host " - CURIO__APP__*" -ForegroundColor Gray

# Configurar apenas variáveis não sensíveis
setx ASPNETCORE_URLS "http://0.0.0.0:5099" /M

# 13) Firewall
Write-Host "Abrindo porta no Firewall..." -ForegroundColor Yellow
netsh advfirewall firewall add rule name="Curio API 5099" protocol=TCP dir=in localport=$bindingPort action=allow | Out-Null

# 14) INICIAR O APPLICATION POOL
Write-Host "Iniciando Application Pool..." -ForegroundColor Yellow
Start-WebAppPool -Name $appPool

# Aguardar até que realmente inicie
$timeout = 30
$elapsed = 0
while ((Get-WebAppPoolState -Name $appPool).Value -ne "Started" -and $elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++
    Write-Host "." -NoNewline
}
Write-Host ""

if ((Get-WebAppPoolState -Name $appPool).Value -eq "Started") {
    Write-Host "Application Pool iniciado com sucesso!" -ForegroundColor Green
} else {
    Write-Host "AVISO: Timeout ao iniciar o AppPool." -ForegroundColor Yellow
}

# 15) Reiniciar IIS (garante que tudo seja recarregado)
Write-Host "Reiniciando IIS..." -ForegroundColor Yellow
iisreset

# ✅ 16) HEALTH CHECK FINAL
Write-Host "Executando health check..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

try {
    $health = Invoke-WebRequest "http://localhost:5099/health" -UseBasicParsing -TimeoutSec 10
    if ($health.StatusCode -eq 200) {
        Write-Host "✅ HEALTH CHECK: API respondendo corretamente!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ HEALTH CHECK: API retornou status $($health.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ HEALTH CHECK: Não foi possível conectar na API" -ForegroundColor Yellow
    Write-Host "   A API pode estar inicializando..." -ForegroundColor Gray
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "DEPLOY FINALIZADO COM SUCESSO!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Acesse: http://20.206.107.144:5099/swagger" -ForegroundColor Cyan
Write-Host "Lembre-se de configurar as variáveis de ambiente sensíveis!" -ForegroundColor Magenta
Write-Host ""

