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

ArchitecturesInstallIn64BitMode=x64compatible arm64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

OutputDir=out
OutputBaseFilename=VeSCU-Setup-{#AppArch}-v{#AppVersion}
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
Type: files; Name: "{app}\config_init.toml"

[Code]
type
  TCodecMeta = record
    Name: String;
    Ext: String;
    Exe: String;
    Format: String;
    Size: String;
    Compat: String;
  end;

  TEncoderPreset = record
    Title: String;
    Ext: String;
    Exe: String;
    Args: String;
    Format: String;
  end;

  TPresetList = array of TEncoderPreset;

var
  QualityPage: TInputOptionWizardPage;
  LossyPage: TInputOptionWizardPage;
  LosslessPage: TInputOptionWizardPage;
  LossyPresets: TPresetList;
  LosslessPresets: TPresetList;

function MakeCodec(Name, Ext, Exe, Format, Size, Compat: String): TCodecMeta;
begin
  Result.Name := Name;
  Result.Ext := Ext;
  Result.Exe := Exe;
  Result.Format := Format;
  Result.Size := Size;
  Result.Compat := Compat;
end;

procedure AddPreset(var List: TPresetList; Codec: TCodecMeta; Args: String);
var
  I: Integer;
begin
  I := GetArrayLength(List);
  SetArrayLength(List, I + 1);

  List[I].Title := Codec.Name + ' (' + Codec.Ext + ', using ' + Codec.Exe + ') - ' +
    '[File size: ' + Codec.Size + ' | Compatibility: ' + Codec.Compat + ']';

  List[I].Ext := Codec.Ext;
  List[I].Exe := Codec.Exe;
  List[I].Args := Args;
  List[I].Format := Codec.Format;
end;

procedure OpenBenchmarkUrl(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://jpegxl.info/resources/battle-of-codecs.html', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure InitEncoders;
var
  JXL, AVIF, WebP, JPEG, PNG: TCodecMeta;
begin
  JXL  := MakeCodec('JPEG XL', '.jxl',  'cjxl.exe',    'Ppm', 'Tiny',  'Low');
  WebP := MakeCodec('WebP',    '.webp', 'cwebp.exe',   'Ppm', 'Mid',   'High');
  AVIF := MakeCodec('AVIF',    '.avif', 'avifenc.exe', 'Ppm', 'Small', 'Mid');
  JPEG := MakeCodec('JPEG',    '.jpg',  'cjpeg.exe',   'Ppm', 'Large', 'Max');
  PNG  := MakeCodec('PNG',     '.png',  'oxipng.exe',  'Png', 'Large', 'Max');

  AddPreset(LossyPresets, JXL,  '-d 1 - {Output}');
  AddPreset(LossyPresets, WebP, '-q 85 -m 6 -o {Output} -- -');
  AddPreset(LossyPresets, AVIF, '-s 6 -q 80 - {Output}');
  AddPreset(LossyPresets, JPEG, '-quality 85 -outfile {Output}');

  AddPreset(LosslessPresets, JXL,  '-d 0 - {Output}');
  AddPreset(LosslessPresets, WebP, '-z 9 -o {Output} -- -');
  AddPreset(LosslessPresets, PNG,  '-o 2 --out {Output} -');
end;

function CreateEncoderPage(AfterID: Integer; ModeTitle: String; const Presets: TPresetList): TInputOptionWizardPage;
var
  I: Integer;
  Note: TNewStaticText;
  Link: TNewStaticText;
begin
  Result := CreateInputOptionPage(AfterID,
    'Default Encoder',
    'In what format should screenshots be saved?',
    'Select your preferred image format.',
    True, False);

  for I := 0 to GetArrayLength(Presets) - 1 do
    Result.Add(Presets[I].Title);

  Result.SelectedValueIndex := 0;

  Link := TNewStaticText.Create(Result);
  Link.Parent := Result.Surface;
  Link.Caption := 'View detailed comparison ↗';
  Link.Cursor := crHand;
  Link.Font.Color := clHighlight;
  Link.Font.Style := [fsUnderline];
  Link.OnClick := @OpenBenchmarkUrl;
  Link.Top := Result.SubCaptionLabel.Top;
  Link.Left := Result.CheckListBox.Left + Result.CheckListBox.Width - Link.Width;

  Result.CheckListBox.Height := Result.CheckListBox.Height - ScaleY(22);

  Note := TNewStaticText.Create(Result);
  Note.Parent := Result.Surface;
  Note.Top := Result.CheckListBox.Top + Result.CheckListBox.Height + ScaleY(6);
  Note.Left := Result.CheckListBox.Left;
  Note.Caption := 'Note: you can change this setting in the config';
end;

procedure InitializeWizard;
var
  QualityNote: TNewStaticText;
begin
  InitEncoders;

  QualityPage := CreateInputOptionPage(wpSelectDir,
    'Quality Preset',
    'At what quality should screenshots be taken?',
    'Select at which quality your screenshots should be compressed.',
    True, False);
  QualityPage.Add('Near-lossless (recommended)');
  QualityPage.Add('Lossless (pixel perfect)');
  QualityPage.SelectedValueIndex := 0;

  QualityPage.CheckListBox.Height := QualityPage.CheckListBox.Height - ScaleY(22);

  QualityNote := TNewStaticText.Create(QualityPage);
  QualityNote.Parent := QualityPage.Surface;
  QualityNote.Top := QualityPage.CheckListBox.Top + QualityPage.CheckListBox.Height + ScaleY(6);
  QualityNote.Left := QualityPage.CheckListBox.Left;
  QualityNote.Caption := 'Note: you can change this setting in the config';

  LossyPage := CreateEncoderPage(QualityPage.ID, 'near-lossless', LossyPresets);
  LosslessPage := CreateEncoderPage(LossyPage.ID, 'lossless', LosslessPresets);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  if PageID = LossyPage.ID then
    Result := (QualityPage.SelectedValueIndex = 1)
  else if PageID = LosslessPage.ID then
    Result := (QualityPage.SelectedValueIndex = 0)
  else
    Result := False;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Selected: TEncoderPreset;
  Toml: String;
begin
  if CurStep = ssPostInstall then
  begin
    if QualityPage.SelectedValueIndex = 1 then
      Selected := LosslessPresets[LosslessPage.SelectedValueIndex]
    else
      Selected := LossyPresets[LossyPage.SelectedValueIndex];

    Toml :=
      '# VeSCU Configuration File' + #13#10 +
      '# You can change these settings or switch encoders at any time.' + #13#10#13#10 +
      '[Saving]' + #13#10 +
      'Extension = "' + Selected.Ext + '"' + #13#10#13#10 +
      '[Encoder]' + #13#10 +
      'Path = "' + Selected.Exe + '"' + #13#10 +
      'Arguments = "' + Selected.Args + '"' + #13#10 +
      'InputFormat = "' + Selected.Format + '"' + #13#10;

    SaveStringToFile(ExpandConstant('{app}\config_init.toml'), Toml, False);
  end;
end;