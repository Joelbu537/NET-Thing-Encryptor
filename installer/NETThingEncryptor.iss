#define AppName "NET Thing Encryptor"
#define AppPublisher "Joelbu537"
#define AppExeName "NET Thing Encryptor.exe"

#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

#ifndef AppFileVersion
#define AppFileVersion "0.0.0.0"
#endif

#ifndef SourceDir
#define SourceDir "..\artifacts\publish\NET Thing Encryptor\win-x64"
#endif

#ifndef OutputDir
#define OutputDir "..\artifacts\installer"
#endif

#ifndef OutputBaseFilename
#define OutputBaseFilename "NET-Thing-Encryptor-Setup-{#AppVersion}"
#endif

#ifndef IconPath
#define IconPath "..\Nte.Desktop\image.ico"
#endif

[Setup]
AppId={{D2AE186D-517D-406F-99D9-8D538AC9607D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/Joelbu537/NET-Thing-Encryptor
AppSupportURL=https://github.com/Joelbu537/NET-Thing-Encryptor/issues
AppUpdatesURL=https://github.com/Joelbu537/NET-Thing-Encryptor/releases
AppCopyright=Copyright (C) 2026 {#AppPublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableWelcomePage=no
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile={#IconPath}
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoVersion={#AppFileVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex={#AppName}
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
UsePreviousAppDir=yes
UsePreviousLanguage=yes
UsePreviousTasks=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[CustomMessages]
english.UpdateNotice=NET Thing Encryptor version %1 is already installed. Setup will update it to version %2.
german.UpdateNotice=NET Thing Encryptor Version %1 ist bereits installiert. Das Setup aktualisiert die Anwendung auf Version %2.
english.RepairNotice=NET Thing Encryptor version %1 is already installed. Setup will repair this version.
german.RepairNotice=NET Thing Encryptor Version %1 ist bereits installiert. Das Setup repariert diese Version.
english.DowngradeBlocked=A newer version (%1) is already installed. Setup %2 cannot downgrade it.
german.DowngradeBlocked=Eine neuere Version (%1) ist bereits installiert. Setup %2 kann kein Downgrade durchführen.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Replace the bundled VLC runtime and managed wrappers before [Files] installs
; the current versions. Retire ImageMagick, but never delete portable {app}\Data.
Type: filesandordirs; Name: "{app}\libvlc"
Type: files; Name: "{app}\LibVLCSharp*.dll"
Type: files; Name: "{app}\Magick*.dll"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
var
  HasInstalledVersion: Boolean;
  InstalledVersion: String;

function NextVersionPart(var VersionText: String): Integer;
var
  SeparatorPosition: Integer;
  Part: String;
begin
  SeparatorPosition := Pos('.', VersionText);
  if SeparatorPosition = 0 then
  begin
    Part := VersionText;
    VersionText := '';
  end
  else
  begin
    Part := Copy(VersionText, 1, SeparatorPosition - 1);
    Delete(VersionText, 1, SeparatorPosition);
  end;
  Result := StrToIntDef(Part, 0);
end;

function CompareVersions(LeftVersion, RightVersion: String): Integer;
var
  Index: Integer;
  LeftPart: Integer;
  RightPart: Integer;
begin
  Result := 0;
  for Index := 1 to 4 do
  begin
    LeftPart := NextVersionPart(LeftVersion);
    RightPart := NextVersionPart(RightVersion);
    if LeftPart > RightPart then
    begin
      Result := 1;
      Exit;
    end;
    if LeftPart < RightPart then
    begin
      Result := -1;
      Exit;
    end;
  end;
end;

procedure ConsiderInstalledVersion(
  RootKey: Integer;
  UninstallKey: String;
  var FoundVersion: Boolean;
  var InstalledVersion: String);
var
  CandidateVersion: String;
begin
  if not RegQueryStringValue(RootKey, UninstallKey, 'DisplayVersion', CandidateVersion) then
    Exit;

  if (not FoundVersion) or
     (CompareVersions(CandidateVersion, InstalledVersion) > 0) then
    InstalledVersion := CandidateVersion;
  FoundVersion := True;
end;

function GetInstalledVersion(var InstalledVersion: String): Boolean;
var
  UninstallKey: String;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D2AE186D-517D-406F-99D9-8D538AC9607D}_is1';
  Result := False;
  InstalledVersion := '';
  if IsWin64 then
  begin
    ConsiderInstalledVersion(HKCU64, UninstallKey, Result, InstalledVersion);
    ConsiderInstalledVersion(HKCU32, UninstallKey, Result, InstalledVersion);
    ConsiderInstalledVersion(HKLM64, UninstallKey, Result, InstalledVersion);
    ConsiderInstalledVersion(HKLM32, UninstallKey, Result, InstalledVersion);
  end
  else
  begin
    ConsiderInstalledVersion(HKCU, UninstallKey, Result, InstalledVersion);
    ConsiderInstalledVersion(HKLM, UninstallKey, Result, InstalledVersion);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  HasInstalledVersion := GetInstalledVersion(InstalledVersion);
  if HasInstalledVersion and
     (CompareVersions(InstalledVersion, '{#AppVersion}') > 0) then
  begin
    SuppressibleMsgBox(
      FmtMessage(CustomMessage('DowngradeBlocked'), [InstalledVersion, '{#AppVersion}']),
      mbError,
      MB_OK,
      IDOK);
    Result := False;
  end;
end;

procedure InitializeWizard;
var
  Notice: String;
begin
  if not HasInstalledVersion then
    Exit;

  if CompareVersions(InstalledVersion, '{#AppVersion}') < 0 then
    Notice := FmtMessage(CustomMessage('UpdateNotice'), [InstalledVersion, '{#AppVersion}'])
  else
    Notice := FmtMessage(CustomMessage('RepairNotice'), [InstalledVersion]);

  WizardForm.WelcomeLabel2.Caption :=
    Notice + #13#10#13#10 + WizardForm.WelcomeLabel2.Caption;
end;
