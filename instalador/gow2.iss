; Instalador do fork (Inno Setup 6). Gere com: ISCC instalador\gow2.iss /DFonte=<pasta com os arquivos do projeto>
; Não leva nada do jogo: só o gerador, os patches e o launcher. O jogo é gerado da ISO de quem instala.
#define Versao "0.1.3-ia.2"
#ifndef Fonte
  #define Fonte ".."
#endif

[Setup]
AppId={{6C4B3E2A-9F1D-4E7B-A2C5-0D8E9F1A2B3C}
AppName=God of War II - port nativo (fork IA)
AppVersion={#Versao}
AppPublisher=niw-maker (feito com IA)
AppPublisherURL=https://github.com/niw-maker/god-of-war-2-pc
DefaultDirName=C:\GoW2
DefaultGroupName=God of War II
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\out\instalador
OutputBaseFilename=GoW2-fork-IA-setup-{#Versao}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
InfoBeforeFile=aviso.txt
ExtraDiskSpaceRequired=26843545600
UninstallDisplayIcon={app}\GoW2 Launcher.exe
UninstallDisplayName=God of War II - port nativo (fork IA)

[Languages]
Name: "pt"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:"
Name: "startmenu"; Description: "Criar atalho no Menu Iniciar"; GroupDescription: "Atalhos:"

[Files]
Source: "{#Fonte}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "\docs,\instalador,.git*"

[Icons]
Name: "{group}\God of War II"; Filename: "{app}\GoW2 Launcher.exe"; WorkingDir: "{app}"; Tasks: startmenu
Name: "{group}\Desinstalar"; Filename: "{uninstallexe}"; Tasks: startmenu
Name: "{autodesktop}\God of War II"; Filename: "{app}\GoW2 Launcher.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\GoW2 Launcher.exe"; Description: "Abrir o launcher agora"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; o que o gerador criou (dá para gerar de novo); a pasta "trabalho" fica, porque guarda os seus saves (trabalho\disc\mc0)
Type: filesandordirs; Name: "{app}\bin"
Type: filesandordirs; Name: "{app}\ps2recomp"
Type: filesandordirs; Name: "{app}\dl"
Type: filesandordirs; Name: "{app}\out\gerar"
Type: files; Name: "{app}\jogar.cmd"

[Code]
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = wpSelectDir) and (Pos(' ', WizardDirValue) > 0) then
  begin
    MsgBox('Escolha uma pasta sem espaços no caminho (por exemplo C:\GoW2). O compilador do jogo não aceita espaços.', mbError, MB_OK);
    Result := False;
  end;
end;
