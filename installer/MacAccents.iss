; Inno Setup script for MacAccents.
; Build with: installer\build.ps1  (publishes self-contained, then compiles this).

#define MyAppName "MacAccents"
; Version may be overridden from the command line: ISCC /DMyAppVersion=1.2.3
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Brielmayer Consulting GmbH"
#define MyAppExeName "MacAccents.exe"

[Setup]
; AppId uniquely identifies the app across versions — keep it STABLE so that
; installing a newer version upgrades in place instead of installing twice.
AppId={{B7A3F2E1-9C4D-4E8A-BF12-6D5A0C3E9F84}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Program Files needs administrator rights to write.
PrivilegesRequired=admin
; Matches the app's single-instance mutex: Setup detects a running instance and
; asks the user to close it before upgrading (the .exe is locked while running).
AppMutex=MacAccents_SingleInstance
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=dist
OutputBaseFilename=MacAccentsSetup-{#MyAppVersion}
SetupIconFile=..\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The whole self-contained publish output (app + .NET runtime).
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Launch after install as the normal (non-elevated) user, matching asInvoker.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent runasoriginaluser
