# ==============================================================
# Publish-CurioWebServer-Local.ps1 
# Versão: Profissional / Enterprise  
# Propósito: Build e publicação local para deploy
# ==============================================================
param()

# Requisitos:
# - PowerShell como Administrador
# - .NET SDK 9 instalado e no PATH
# - Execute: Set-ExecutionPolicy RemoteSigned -Scope Process (se necessário)

Write-Host "=== Publish-CurioWebServer-Local.ps1 ===" -ForegroundColor Cyan

# Caminhos (ajuste se necessário)
$projectPath = "C:\Desenvolvimento\VisualStudio\CurioPwaSolution\CurioWebServer"
$publishOutput = "C:\Publicar\CurioWebServer"

Write-Host "Projeto: $projectPath" -ForegroundColor Yellow
Write-Host "Saida de publicacao: $publishOutput" -ForegroundColor Yellow

# Validar se projeto existe
if (-not (Test-Path $projectPath)) {
    Write-Host "ERRO: Projeto não encontrado em: $projectPath" -ForegroundColor Red
    exit 1
}

# Limpa pasta de publicação anterior (se existir)
if (Test-Path $publishOutput) {
    Write-Host "Removendo pasta antiga de publicacao..." -ForegroundColor Gray
    Remove-Item $publishOutput -Recurse -Force
}

# Rodar dotnet publish (Release x64)
Write-Host "Compilando e publicando (Release, win-x64)..." -ForegroundColor Yellow
dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $publishOutput

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha na compilacao/publicacao. Verifique logs do dotnet." -ForegroundColor Red
    exit 1
}

# VALIDAÇÃO DE ARQUIVOS ESSENCIAIS
Write-Host "Validando arquivos essenciais..." -ForegroundColor Yellow
$essentialFiles = @("CurioWebServer.dll", "appsettings.json", "web.config")
foreach ($file in $essentialFiles) {
    if (-not (Test-Path "$publishOutput\$file")) {
        Write-Host "ERRO: Arquivo essencial faltando: $file" -ForegroundColor Red
        exit 1
    }
}
Write-Host "Todos os arquivos essenciais verificados!" -ForegroundColor Green

Write-Host "`nPublicacao concluida com sucesso em: $publishOutput" -ForegroundColor Green
Write-Host "Agora copie C:\Publicar\CurioWebServer para o servidor (ex: via RDP, SCP, FTP)." -ForegroundColor Cyan

