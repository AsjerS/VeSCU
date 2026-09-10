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
OutputBaseFilename=VeSCU-{#AppArch}-installer
Compression=lzma2/max
SolidCompression=yes

[Files]
Source: "..\src\VeSCU\bin\Release\net10.0-windows\{#AppArch}\publish\VeSCU.exe"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "VeSCU"; Flags: dontcreatekey uninsdeletevalue

[Icons]
Name: "{autoprograms}\VeSCU"; Filename: "{app}\VeSCU.exe"

[Run]
Filename: "{app}\VeSCU.exe"; Description: "Launch VeSCU now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\VeSCU"
Type: filesandordirs; Name: "{userappdata}\VeSCU"
