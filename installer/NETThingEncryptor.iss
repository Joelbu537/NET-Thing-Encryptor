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
OutputBaseFilename=NET-Thing-Encryptor-Setup-{#AppVersion}
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
english.DowngradeBlocked=A newer version (%1) is already installed. Setup %2 cannot downgrade it.
german.DowngradeBlocked=Eine neuere Version (%1) ist bereits installiert. Setup %2 kann kein Downgrade durchführen.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; M6 replaces the WinForms/media package in place. Remove only known obsolete
; dependencies; never delete {app}\Data because it may contain a portable vault.
Type: filesandordirs; Name: "{app}\libvlc"
Type: files; Name: "{app}\LibVLCSharp*.dll"
Type: files; Name: "{app}\Magick*.dll"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
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

function GetInstalledVersion(var InstalledVersion: String): Boolean;
var
  UninstallKey: String;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{{D2AE186D-517D-406F-99D9-8D538AC9607D}_is1';
  Result := RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', InstalledVersion);
  if not Result then
    Result := RegQueryStringValue(HKLM64, UninstallKey, 'DisplayVersion', InstalledVersion);
end;

function InitializeSetup(): Boolean;
var
  InstalledVersion: String;
begin
  Result := True;
  if GetInstalledVersion(InstalledVersion) and
     (CompareVersions(InstalledVersion, '{#AppVersion}') > 0) then
  begin
    MsgBox(
      FmtMessage(CustomMessage('DowngradeBlocked'), [InstalledVersion, '{#AppVersion}']),
      mbError,
      MB_OK);
    Result := False;
  end;
end;
