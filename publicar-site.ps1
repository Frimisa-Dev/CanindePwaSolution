# =====================================================================
#  Publica a pasta do site da AA Frimisa no Netlify
#  Uso: clique com o botao direito > "Executar com o PowerShell"
#       ou:  powershell -ExecutionPolicy Bypass -File .\publicar-site.ps1
#  Guarde este arquivo FORA da pasta do site (ex.: em CanindePwaSolution),
#  para ele nao ser publicado junto.
# =====================================================================

$ErrorActionPreference = "Stop"

# ---- CONFIGURACAO ----------------------------------------------------
$Pasta = "C:\Desenvolvimento\VisualStudio\CanindePwaSolution\SiteFrimisa"
$Site  = "effervescent-crisp-43a137"   # nome ou ID do projeto no Netlify
# ----------------------------------------------------------------------

function Parar($texto) {
    Write-Host ""
    Write-Host "ERRO: $texto" -ForegroundColor Red
    Read-Host "Pressione ENTER para fechar"
    exit 1
}

Write-Host "== Publicacao do site AA Frimisa ==" -ForegroundColor Cyan

# 1) Confere a pasta e os arquivos
if (-not (Test-Path $Pasta))                       { Parar "Pasta nao encontrada: $Pasta" }
if (-not (Test-Path (Join-Path $Pasta "index.html"))) { Parar "index.html nao encontrado em $Pasta" }

$pastaImg = Join-Path $Pasta "image"
if (-not (Test-Path $pastaImg)) {
    Write-Host "AVISO: pasta 'image' nao existe. O site sera publicado sem imagens." -ForegroundColor Yellow
} else {
    foreach ($nome in @("logo.png","logo-slogan.png","tela1.png","tela2.png")) {
        if (-not (Test-Path (Join-Path $pastaImg $nome))) {
            Write-Host "AVISO: image\$nome nao encontrado." -ForegroundColor Yellow
        }
    }
    $duplos = Get-ChildItem $pastaImg -Filter "*.png.png" -ErrorAction SilentlyContinue
    foreach ($d in $duplos) {
        Write-Host "AVISO: $($d.Name) tem extensao duplicada (.png.png). Renomeie." -ForegroundColor Yellow
    }
}

# 2) Confere o Node.js
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Parar "Node.js nao esta instalado. Baixe em https://nodejs.org (versao LTS), instale e rode de novo."
}

# 3) Instala o Netlify CLI se precisar
if (-not (Get-Command netlify -ErrorAction SilentlyContinue)) {
    Write-Host "Instalando o Netlify CLI (so na primeira vez)..." -ForegroundColor Yellow
    npm install -g netlify-cli
    if ($LASTEXITCODE -ne 0) { Parar "Falha ao instalar o netlify-cli." }
}

# 4) Login no Netlify
if (-not $env:NETLIFY_AUTH_TOKEN) {
    $status = (netlify status 2>&1 | Out-String)
    if ($status -match "(?i)not logged in|nao esta logado") {
        Write-Host ""
        Write-Host "Voce ainda nao fez login no Netlify. Abrindo o navegador..." -ForegroundColor Yellow
        Write-Host "Entre com aafrimisa@gmail.com e clique em Authorize."
        netlify login
        if ($LASTEXITCODE -ne 0) { Parar "Falha no login do Netlify." }
    }
}

# 4b) Procura o ID do projeto
if ($Site -notmatch '^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$') {
    Write-Host "Procurando o ID do projeto '$Site'..." -ForegroundColor Cyan
    try {
        $lista = (netlify sites:list --json 2>$null | Out-String) | ConvertFrom-Json
    } catch {
        Parar "Nao consegui listar os projetos. Rode 'netlify sites:list' e coloque o ID em `$Site."
    }
    $achado = @($lista | Where-Object { $_.name -eq $Site -or $_.id -eq $Site })
    if ($achado.Count -eq 0) {
        Write-Host "Projetos encontrados na conta:" -ForegroundColor Yellow
        $lista | ForEach-Object { Write-Host ("  {0}  ->  {1}" -f $_.name, $_.id) }
        Parar "Projeto '$Site' nao encontrado. Copie o ID correto para `$Site no inicio do script."
    }
    $Site = $achado[0].id
    Write-Host "ID encontrado: $Site"
}

# 5) Publica em producao
$msg = "Atualizacao em " + (Get-Date -Format "dd/MM/yyyy HH:mm")
Write-Host ""
Write-Host "Entrando na pasta do site e enviando para o Netlify..." -ForegroundColor Cyan

Set-Location $Pasta

netlify deploy --prod --site "$Site" --message "$msg"
if ($LASTEXITCODE -ne 0) { Parar "O deploy falhou. Veja a mensagem acima." }

Write-Host ""
Write-Host "Pronto! Site publicado com sucesso!" -ForegroundColor Green
Write-Host "Confira: https://www.aafrimisa.com.br  (Ctrl+F5 para limpar o cache)"
Read-Host "Pressione ENTER para fechar"