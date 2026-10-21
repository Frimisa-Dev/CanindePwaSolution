Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  CRIAÇÃO DO USUÁRIO MySQL: curioWeb" -ForegroundColor Yellow
Write-Host "===============================================" -ForegroundColor Cyan

# 1) Caminho do MySQL instalado
$mysqlExe = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"

if (!(Test-Path $mysqlExe)) {
    Write-Host "? MySQL não encontrado no caminho padrão." -ForegroundColor Red
    Write-Host "Informe o caminho completo do mysql.exe e tente novamente." -ForegroundColor Red
    exit
}

# 2) Solicitar senha do usuário root
$rootPassword = Read-Host "Digite a senha do usuário ROOT do MySQL"

# 3) Script SQL
$sql = @"
CREATE USER IF NOT EXISTS 'curioWeb'@'localhost' IDENTIFIED BY 'CurioWeb@9173';
CREATE USER IF NOT EXISTS 'curioWeb'@'%' IDENTIFIED BY 'CurioWeb@9173';

GRANT ALL PRIVILEGES ON *.* TO 'curioWeb'@'localhost';
GRANT ALL PRIVILEGES ON *.* TO 'curioWeb'@'%';

FLUSH PRIVILEGES;
"@

# 4) Execução
Write-Host "`nExecutando comandos SQL..." -ForegroundColor Yellow

$sql | & $mysqlExe -u root -p$rootPassword

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n? USUÁRIO curioWeb CRIADO COM SUCESSO!" -ForegroundColor Green
} else {
    Write-Host "`n? FALHA ao criar usuário!" -ForegroundColor Red
}

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Execute no Workbench o teste: SELECT User, Host FROM mysql.user;" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
