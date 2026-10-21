# ==============================
#  build-dev.ps1
#  Automação DEV – Front + Back
#  Ambos servidos pelo Backend
# ==============================

Write-Host "=== Curio DEV Builder ===" -ForegroundColor Cyan

# Caminhos
$frontend = "CurioPwaClient"
$backend = "CurioWebServer"
$frontendPublish = "$frontend\publish"
$backendWwwroot = "$backend\wwwroot"

# 1. Publicar o FRONTEND
Write-Host "`n[1/5] Publicando o Frontend (Release)..." -ForegroundColor Yellow
dotnet publish "$frontend" -c Release -o "$frontendPublish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao publicar o Frontend!" -ForegroundColor Red
    exit 1
}

# 2. Limpar wwwroot do Backend
Write-Host "`n[2/5] Limpando wwwroot do Backend..." -ForegroundColor Yellow
if (Test-Path $backendWwwroot) {
    Remove-Item $backendWwwroot -Recurse -Force
}
New-Item $backendWwwroot -ItemType Directory | Out-Null

# 3. Copiar o conteúdo do Frontend PURO (sem .gz e .br)
Write-Host "`n[3/5] Copiando arquivos do Frontend -> Backend/wwwroot..." -ForegroundColor Yellow

Copy-Item "$frontendPublish\wwwroot\*" "$backendWwwroot" -Recurse

# Remover arquivos comprimidos
Write-Host "[4/5] Removendo arquivos comprimidos (.gz / .br)..." -ForegroundColor Yellow
Get-ChildItem -Path $backendWwwroot -Recurse -Include *.gz, *.br | Remove-Item -Force

# 5. Iniciar o Backend
Write-Host "`n[5/5] Iniciando Backend (porta 5099)..." -ForegroundColor Yellow
Set-Location $backend
dotnet run
