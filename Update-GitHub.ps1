param(
    [string]$Mensagem = "",
    [switch]$SkipBuild,
    [switch]$SkipSecretScan,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
cd C:\Desenvolvimento\VisualStudio\CanindePwaSolution

function Write-Step($msg) { Write-Host "`n▶ $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "  ✅ $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "  ⚠️  $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "  ❌ $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  UPDATE GITHUB — CanindePwaSolution" -ForegroundColor Cyan
if ($DryRun) { Write-Host "  MODO DRY-RUN (nada será alterado)" -ForegroundColor Yellow }
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Step "1/6 Verificações iniciais"

if (-not (Test-Path ".git")) {
    Write-Err "Não é um repositório Git. Abortando."
    exit 1
}

$branch = git branch --show-current
Write-Ok "Branch: $branch"

$remote = git remote get-url origin 2>$null
if (-not $remote) {
    Write-Err "Remote 'origin' não configurado. Execute: git remote add origin https://github.com/Frimisa-Dev/CanindePwaSolution.git"
    exit 1
}
Write-Ok "Remote: $remote"

Write-Step "2/6 Estado do working tree"
$status = git status --short
if (-not $status) {
    Write-Ok "Nada para commitar. Working tree limpo."
    Write-Host "`nDeseja fazer 'git pull' para sincronizar com o remoto? (S/N)" -ForegroundColor Yellow
    $resp = Read-Host
    if ($resp -match '^[Ss]') {
        if (-not $DryRun) { git pull }
        else { Write-Warn "[DRY-RUN] git pull" }
    }
    exit 0
}

Write-Host "  Mudanças detectadas:" -ForegroundColor White
$status | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }

if ($status | Select-String "wwwroot/" -Quiet) {
    Write-Warn "ATENÇÃO: mudanças em wwwroot/ serão incluídas."
}

if (-not $SkipBuild) {
    Write-Step "3/6 dotnet build"
    if ($DryRun) {
        Write-Warn "[DRY-RUN] dotnet build CanindeWebServer"
    } else {
        Push-Location CanindeWebServer
        $buildResult = dotnet build --nologo 2>&1
        Pop-Location
        if ($LASTEXITCODE -ne 0) {
            Write-Err "Build FALHOU. Corrija os erros antes de commitar."
            $buildResult | Select-Object -Last 30 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
            exit 1
        }
        $warnings = ($buildResult | Select-String "warning").Count
        Write-Ok "Build OK ($warnings warnings, 0 erros)"
    }
} else {
    Write-Warn "3/6 dotnet build — PULADO (-SkipBuild)"
}

if (-not $SkipSecretScan) {
    Write-Step "4/6 Scan de segredos"
    if ($DryRun) {
        Write-Warn "[DRY-RUN] scan de segredos"
    } else {
        $arquivos = git diff --name-only
        $padroes = @(
            'password\s*[:=]\s*[''"]?[^''"\s]+',
            'senha\s*[:=]\s*[''"]?[^''"\s]+',
            'secret\s*[:=]\s*[''"]?[^''"\s]+',
            'JwtKey\s*[:=]\s*[''"]?[^''"\s]+',
            'Admin@123'
        )
        $suspeitos = @()
        foreach ($arq in $arquivos) {
            if (-not (Test-Path $arq)) { continue }
            if ($arq -match '\.(ps1|txt|json|cs|csproj|js|html)$') {
                $conteudoCheck = Get-Content $arq -Raw -ErrorAction SilentlyContinue
                foreach ($p in $padroes) {
                    if ($conteudoCheck -match $p) { $suspeitos += $arq; break }
                }
            }
        }
        if ($suspeitos.Count -gt 0) {
            Write-Warn "Possíveis segredos nos arquivos:"
            $suspeitos | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
            Write-Host "`n  Continuar mesmo assim? (S/N)" -ForegroundColor Yellow
            $resp = Read-Host
            if ($resp -notmatch '^[Ss]') {
                Write-Err "Abortado pelo usuário."
                exit 1
            }
        } else {
            Write-Ok "Nenhum segredo óbvio detectado"
        }
    }
} else {
    Write-Warn "4/6 scan de segredos — PULADO (-SkipSecretScan)"
}

Write-Step "5/6 Mensagem do commit"
if (-not $Mensagem) {
    Write-Host "  Digite a mensagem do commit (Enter = usa padrão com data):" -ForegroundColor White
    Write-Host "  Padrão: 'chore: atualização <YYYY-MM-DD HH:MM>'" -ForegroundColor Gray
    $Mensagem = Read-Host "  Mensagem"
    if (-not $Mensagem) {
        $Mensagem = "chore: atualização $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    }
}
Write-Ok "Mensagem: $Mensagem"

if ($DryRun) {
    Write-Warn "[DRY-RUN] git add -A"
    Write-Warn "[DRY-RUN] git commit -m '$Mensagem'"
    Write-Warn "[DRY-RUN] git push origin $branch"
    Write-Host "`n✅ Dry-run concluído. Nada foi alterado." -ForegroundColor Green
    exit 0
}

Write-Step "6/6 Executando add + commit + push"

git add -A
if ($LASTEXITCODE -ne 0) { Write-Err "git add falhou"; exit 1 }
Write-Ok "git add -A"

$commitOutput = git commit -m "$Mensagem" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Err "git commit falhou:"
    Write-Host $commitOutput -ForegroundColor Red
    exit 1
}
Write-Ok "git commit"

$pushOutput = git push origin $branch 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Err "git push falhou:"
    Write-Host $pushOutput -ForegroundColor Red
    Write-Warn "Verifique: PAT expirado? Sem internet? Branch protegida no GitHub?"
    exit 1
}
Write-Ok "git push origin $branch"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ UPDATE CONCLUÍDO" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Branch:   $branch" -ForegroundColor White
Write-Host "  Mensagem: $Mensagem" -ForegroundColor White
Write-Host "  Veja: https://github.com" -ForegroundColor Cyan
Write-Host ""
