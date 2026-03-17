#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppName=Docker Manager
AppVersion={#AppVersion}
AppPublisher=Docker Manager
DefaultDirName={autopf}\DockerManager
DefaultGroupName=Docker Manager
OutputDir=..\output
OutputBaseFilename=DockerManager-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\DockerManager.App.exe

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "Crea icona sul Desktop"; GroupDescription: "Icone aggiuntive:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Docker Manager"; Filename: "{app}\DockerManager.App.exe"
Name: "{autodesktop}\Docker Manager"; Filename: "{app}\DockerManager.App.exe"; Tasks: desktopicon
Name: "{group}\Disinstalla Docker Manager"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\DockerManager.App.exe"; Description: "Avvia Docker Manager"; Flags: nowait postinstall skipifsilent
