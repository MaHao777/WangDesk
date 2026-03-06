#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef MySourceDir
  #error MySourceDir must be defined.
#endif

#ifndef MyOutputDir
  #error MyOutputDir must be defined.
#endif

#ifndef MyOutputBaseFilename
  #define MyOutputBaseFilename "setup"
#endif

#define MyAppName "WangDesk"
#define MyAppPublisher "WangDesk"
#define MyAppExeName "WangDesk.App.exe"
#define MyAppId "{{8B50AFD2-88C2-470A-9F96-E1442D7F5A44}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\WangDesk
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
PrivilegesRequired=lowest
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputBaseFilename}
SetupIconFile=..\src\WangDesk.App\assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\WangDesk"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\WangDesk"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,WangDesk}"; Flags: nowait postinstall skipifsilent
