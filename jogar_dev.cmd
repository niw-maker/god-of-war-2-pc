@echo off
rem Ferramenta de desenvolvedor: F5 abre o menu de salvar do jogo em qualquer ponto.
rem Uso: jogar_dev.cmd [nome]   (exemplo: jogar_dev.cmd colosso)
rem Cada nome tem um cartao proprio em out\cartoes_dev\<nome>.ps2; o cartao do jogo normal nao e tocado.
rem Com o cartao novo, no menu "Criar novo arquivo", o cursor comeca em Nao: suba para Sim antes de confirmar.
setlocal
set "NOME=%~1"
if "%NOME%"=="" set "NOME=dev"
mkdir "%~dp0out\cartoes_dev" 2>nul
set "PS2X_DEV_SAVE=1"
set "PS2X_MC_SIO2=1"
set "PS2X_MC_SIO2_ARQ=%~dp0out\cartoes_dev\%NOME%.ps2"
call "%~dp0jogar.cmd"
