<#
.SYNOPSIS
    Fluxo de commit/push do CanindePwaSolution com build e scan de segredos.

.DESCRIPTION
    O QUE FAZ
      1. Verifica se está em repositório Git com remote 'origin'.
      2. Roda dotnet build (opcional) — aborta se falhar.
      3. Faz scan regex nos arquivos com mudança à procura de senhas/tokens.
         Se encontrar, pede confirmação antes de commitar.
      4. Faz git add -A, git commit -m e git push origin <branch>.
      5. Se working tree está limpo, oferece git pull para sincronizar.

    QUANDO DEVE SER UTILIZADO
      • Após qualquer mudança de código, antes de encerrar o dia.
      • Em commits sensíveis, primeiro rodar -DryRun para conferir.
      • NÃO usar em CI/CD (é interativo).

    PRÉ-REQUISITOS
      • git instalado e no PATH
      • Repositório com remote 'origin' configurado
      • .NET SDK 10 instalado (para o build)
      • PAT válido para push (se remoto for HTTPS)

    O QUE PRODUZ
      • Commit + push no GitHub (branch atual)
      • Efeito colateral: nada se -DryRun; nada se working tree limpo sem pull

    PARÂMETROS
      -Mensagem <string>       Mensagem do commit (senão pergunta)
      -SkipBuild               Pula dotnet build
      -SkipSecretScan          Pula o scan de segredos
      -DryRun                  Mostra o que faria, não executa

    AVISOS
      • O scan de segredos é caseiro (regex), não é garantia — para garantia
        real, considerar gitleaks/trufflehog em CI.
      • Inclui wwwroot/ no add -A (só avisa, não bloqueia).

.EXAMPLE
    .\Update-GitHub.ps1 -DryRun
    .\Update-GitHub.ps1 -Mensagem "feat: fallback HTML para /rifa/{token}"
    .\Update-GitHub.ps1 -SkipBuild -SkipSecretScan

.NOTES
    Versão: 1.0
#>

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

# ── Verificações iniciais ──
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

# ── Estado do working tree ──
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

# ── Verificação de wwwroot ──
if ($status | Select-String "wwwroot/" -Quiet) {
    Write-Warn "ATENÇÃO: mudanças em wwwroot/ serão incluídas."
}

# ── Build ──
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

# ── Scan de segredos ──
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

# ── Mensagem do commit ──
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

# ── Add + Commit + Push ──
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
Write-Host "  Veja: https://github.com/Frimisa-Dev/CanindePwaSolution/tree/$branch" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Veja: https://github.com" -ForegroundColor Cyan
