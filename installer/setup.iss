#ifndef AppArch
  #error "Please provide an app architecture."
#endif
#ifndef AppVersion
  #error "Please provide an app version."
#endif

[Setup]
AppName=VeSCU
AppVersion={#AppVersion}
DefaultDirName={autopf}\VeSCU
DefaultGroupName=VeSCU
SetupIconFile=..\src\VeSCU\app.ico

ArchitecturesInstallIn64BitMode=x64compatible arm64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

OutputDir=out
OutputBaseFilename=VeSCU-v{#AppVersion}-{#AppArch}-installer
Compression=lzma2/max
SolidCompression=yes

[Tasks]
Name: "startup"; Description: "Start VeSCU automatically with Windows"; Flags: checkedonce

[Files]
Source: "..\src\VeSCU\bin\Release\net10.0-windows\{#AppArch}\publish\VeSCU.exe"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "VeSCU"; ValueData: """{app}\VeSCU.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Icons]
Name: "{autoprograms}\VeSCU"; Filename: "{app}\VeSCU.exe"

[Run]
Filename: "{app}\VeSCU.exe"; Description: "Launch VeSCU now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\VeSCU"
Type: filesandordirs; Name: "{userappdata}\VeSCU"