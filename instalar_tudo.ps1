<#
.SYNOPSIS
  Jeito fácil: instala o que falta (pelo winget) e gera o jogo a partir da sua ISO, numa janela só.
  É o que o botão "Instalar tudo e gerar o jogo" do launcher roda.
.PARAMETER Iso
  A imagem .iso do DVD do God of War II (EUA, SCUS-97481).
.PARAMETER Ffmpeg
  Instala também o ffmpeg (vídeo do modo diagnóstico do launcher).
#>
param([Parameter(Mandatory = $true)][string]$Iso, [switch]$Ffmpeg)
$ErrorActionPreference = 'Continue'
$R = $PSScriptRoot
$Host.UI.RawUI.WindowTitle = 'God of War II - instalando'

function Msg($t, $c = 'Cyan') { Write-Host ''; Write-Host "==> $t" -ForegroundColor $c }
function Fim($t, $c) { Write-Host ''; Write-Host $t -ForegroundColor $c; Write-Host ''; Read-Host 'Aperte Enter para fechar'; exit }

# o PATH desta janela não vê o que o winget acabou de instalar; relê o do Windows
function RecarregaPath {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
    $vk = [Environment]::GetEnvironmentVariable('VULKAN_SDK', 'Machine')
    if ($vk) { $env:VULKAN_SDK = $vk }
}

function TemPython {
    try { $v = (cmd /c 'python -c "import sys; print(sys.version_info[0]*100+sys.version_info[1])" 2>nul') } catch { return $false }
    return ($v -match '^\d+$') -and ([int]$v -ge 311)
}

function TemVS {
    $vw = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vw)) { return $false }
    $p = & $vw -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 Microsoft.VisualStudio.Component.VC.CMake.Project -property installationPath
    return [bool]$p
}

# não pode se chamar Winget: o PowerShell não diferencia maiúsculas, e o & winget de dentro chamaria a própria função
function InstalaPeloWinget($id, $extra = @()) {
    # --source winget: num Windows novo a loja msstore também responde, dá erro e o winget se recusa a escolher
    $a = @('install', '--id', $id, '-e', '--source', 'winget', '--accept-source-agreements', '--accept-package-agreements') + $extra
    & winget.exe @a
}

Write-Host 'God of War II (fork feito com IA) - instalação' -ForegroundColor Yellow
Write-Host 'Esta janela instala o que falta e gera o jogo a partir da sua ISO. Leva de 30 minutos a 1 hora e meia.'
Write-Host 'Pode pedir permissão de administrador algumas vezes: clique em Sim. Não feche esta janela.'

if ($R -match ' ') { Fim "A pasta do projeto tem espaços ($R). Reinstale numa pasta sem espaços, como C:\GoW2." Red }
if (-not (Test-Path -LiteralPath $Iso)) { Fim "Não achei a ISO: $Iso" Red }
if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Fim 'O winget não está neste Windows. Instale o "Instalador de Aplicativo" pela Microsoft Store e tente de novo.' Red
}

Msg '1/4  Python'
if (TemPython) { Write-Host 'já instalado' } else { InstalaPeloWinget 'Python.Python.3.12' @('--scope', 'machine'); RecarregaPath }

Msg '2/4  Compilador C++ (Visual Studio 2022 Build Tools). É o passo mais demorado.'
if (TemVS) { Write-Host 'já instalado' } else {
    InstalaPeloWinget 'Microsoft.VisualStudio.2022.BuildTools' @('--override', '--quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.CMake.Project --add Microsoft.VisualStudio.Component.VC.Llvm.Clang --add Microsoft.VisualStudio.Component.Windows11SDK.22621 --includeRecommended')
}

Msg '3/4  Vulkan SDK (para o AMD FSR 3.1)'
RecarregaPath
if ($env:VULKAN_SDK -and (Test-Path $env:VULKAN_SDK)) { Write-Host 'já instalado' } else { InstalaPeloWinget 'KhronosGroup.VulkanSDK'; RecarregaPath }

if ($Ffmpeg) {
    Msg 'ffmpeg (vídeo do diagnóstico)'
    if (Get-Command ffmpeg -ErrorAction SilentlyContinue) { Write-Host 'já instalado' } else { InstalaPeloWinget 'Gyan.FFmpeg'; RecarregaPath }
}

RecarregaPath
if (-not (TemPython)) { Fim 'O Python não foi instalado (veja as mensagens acima). Reinicie o PC e clique no botão de novo; se repetir, instale o Python 3.12 pelo site python.org.' Red }
if (-not (TemVS)) { Fim 'O compilador C++ não foi instalado (veja as mensagens acima). Reinicie o PC e clique no botão de novo.' Red }

Msg '4/4  Gerando o jogo a partir da sua ISO (30 minutos a 1 hora)'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $R 'gerar.ps1') -Iso $Iso
if ((Test-Path (Join-Path $R 'bin\god-of-war-2.exe')) -and $LASTEXITCODE -eq 0) {
    Fim 'PRONTO! Feche esta janela e clique em JOGAR no launcher.' Green
} else {
    Fim 'A geração parou com erro. Os detalhes estão em out\gerar. Clique no botão de novo: ela continua de onde parou.' Red
}
