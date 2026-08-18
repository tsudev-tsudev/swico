; ============================================================================
;  tsuowlit SWICO - kich ban dong goi Inno Setup 6
;
;  Bien dich:
;     iscc /DAppVersion=3.0.0 packaging\innosetup\swico.iss
;
;  Phien ban duoc TRUYEN VAO tu ben ngoai (/DAppVersion=...) chu khong viet
;  cung o day. Nguon su that duy nhat ve phien ban la Directory.Build.props;
;  script phat hanh doc tu do roi truyen sang. Viet cung se co ngay lech nhau.
; ============================================================================

#ifndef AppVersion
  #define AppVersion "3.0.0"
#endif

#define AppName        "tsuowlit SWICO"
#define AppPublisher   "tsuowlit"
#define AppUrl         "https://github.com/tsudev-tsudev/swico"
#define AppExeName     "swico.exe"
#define SourceExe      "..\..\publish\" + AppExeName

[Setup]
; AppId KHONG BAO GIO duoc doi. Windows dua vao GUID nay de nhan ra ban cai
; cu khi nang cap va khi go cai dat. Doi GUID = tao ra mot san pham thu hai
; song song tren may nguoi dung.
AppId={{7C4E9B2A-5D31-4F86-9A0C-3E8B1D6F2A47}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoCopyright=Copyright (C) 2026 {#AppPublisher}

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

; Cong cu can quyen Administrator de doc ban quyen/Defender/DISM, nen cai dat
; cho TOAN MAY. Dat o day de Inno hien hop UAC ngay tu dau thay vi de nguoi
; dung cai xong roi moi phat hien khong chay duoc.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

LicenseFile=..\..\EULA.txt
InfoAfterFile=..\..\PRIVACY.md
OutputDir=..\output
OutputBaseFilename=swico-setup-{#AppVersion}
SetupIconFile=swico.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "vi"; MessagesFile: "compiler:Languages\Vietnamese.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
vi.AddToPathTask=Thêm vào biến môi trường PATH (chạy được lệnh "swico" từ mọi thư mục)
en.AddToPathTask=Add to PATH environment variable (run "swico" from any directory)
vi.DesktopIconTask=Tạo lối tắt ngoài Màn hình nền
en.DesktopIconTask=Create a desktop shortcut
vi.LaunchAfterInstall=Chạy thử %1 ngay bây giờ
en.LaunchAfterInstall=Run %1 now
vi.ViewReadme=Xem hướng dẫn sử dụng
en.ViewReadme=View the user guide

[Tasks]
Name: "addtopath"; Description: "{cm:AddToPathTask}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:DesktopIconTask}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceExe}";              DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\LICENSE";             DestDir: "{app}"; DestName: "LICENSE.txt";  Flags: ignoreversion
Source: "..\..\NOTICE";              DestDir: "{app}"; DestName: "NOTICE.txt";   Flags: ignoreversion
Source: "..\..\EULA.txt";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\PRIVACY.md";          DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\README.md";           DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";                 Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:ViewReadme}";            Filename: "{app}\README.md"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";           Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Them {app} vao PATH cua toan may. Kiem tra truoc bang NeedsAddPath() de
; khong noi trung duong dan khi nguoi dung cai de len ban cu.
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
    ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; \
    Tasks: addtopath; Check: NeedsAddPath(ExpandConstant('{app}'))

[Run]
Filename: "{app}\{#AppExeName}"; Parameters: "--help"; \
    Description: "{cm:LaunchAfterInstall,{#AppName}}"; \
    Flags: postinstall nowait skipifsilent runascurrentuser

[UninstallDelete]
; Chi xoa thu muc rong con lai. TUYET DOI khong dong toi cac thu muc ket qua
; ra quet - do la du lieu cua nguoi dung, khong phai cua trinh cai dat.
Type: dirifempty; Name: "{app}"

[Code]
{ Kiem tra duong dan da co trong PATH chua. So sanh co bao boc dau cham phay
  va chu thuong hai dau de tranh khop nham voi duong dan chi trung mot phan. }
function NeedsAddPath(Param: string): Boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then begin
    Result := True;
    Exit;
  end;
  Result := Pos(';' + Lowercase(Param) + ';', ';' + Lowercase(OrigPath) + ';') = 0;
end;

{ Go cai dat: rut duong dan khoi PATH de khong de lai rac. }
procedure RemoveFromPath(Param: string);
var
  OrigPath, NewPath: string;
  P: Integer;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then Exit;

  NewPath := ';' + OrigPath + ';';
  P := Pos(';' + Lowercase(Param) + ';', Lowercase(NewPath));
  if P = 0 then Exit;

  Delete(NewPath, P, Length(Param) + 1);
  NewPath := Copy(NewPath, 2, Length(NewPath) - 2);

  RegWriteExpandStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', NewPath);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RemoveFromPath(ExpandConstant('{app}'));
end;
