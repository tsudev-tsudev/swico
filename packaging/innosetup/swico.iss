; ============================================================================
;  tsudev SWICO - kich ban dong goi Inno Setup 6
;
;  Bien dich:
;     iscc /DAppVersion=3.0.0 packaging\innosetup\swico.iss
;
;  Phien ban duoc TRUYEN VAO tu ben ngoai (/DAppVersion=...) chu khong viet
;  cung o day. Nguon su that duy nhat ve phien ban la Directory.Build.props;
;  script phat hanh doc tu do roi truyen sang. Viet cung se co ngay lech nhau.
; ============================================================================

#ifndef AppVersion
  #define AppVersion "26.8.18.2"
#endif

#define AppName        "tsudev SWICO"
#define AppPublisher   "tsudev"
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
; "x64compatible" chi co tu Inno Setup 6.3 tro len; ban cu hon dung "x64".
; Runner cua GitHub co the mang bat ky ban 6.x nao, nen phai do phien ban thay
; vi gia dinh - viet cung mot trong hai gia tri se lam ISCC bao loi o ban kia.
#if Ver >= EncodeVer(6,3,0,0)
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#else
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
#endif
MinVersion=10.0

LicenseFile=..\..\EULA.txt
; Trinh cai dat hien file nay nhu VAN BAN THUAN. Dung .md o day se hien nguyen
; cac dau markdown, nen cho tro toi ban .txt sinh rieng cho trinh cai dat.
InfoAfterFile=SAU-KHI-CAI.txt
OutputDir=..\output
OutputBaseFilename=swico-setup-{#AppVersion}
; Icon di kem trong repo. Neu thieu, ISCC bao loi va dung han - nen kiem tra
; su ton tai thay vi tin la no luon o do.
#if FileExists(AddBackslash(SourcePath) + "..\..\assets\swico.ico")
SetupIconFile=..\..\assets\swico.ico
#endif

; Anh thuong hieu trong trinh thuat si. Inno Setup CHI nhan dinh dang BMP -
; khong nhan PNG - nen hai file nay duoc sinh tu logo goc boi
; packaging/tools/make-assets.py.
#if FileExists(AddBackslash(SourcePath) + "..\..\assets\wizard-large.bmp")
WizardImageFile=..\..\assets\wizard-large.bmp
#endif
#if FileExists(AddBackslash(SourcePath) + "..\..\assets\wizard-small.bmp")
WizardSmallImageFile=..\..\assets\wizard-small.bmp
#endif
; Payload (swico.exe) nay KHONG con duoc nen san ben trong nua - xem ghi chu
; trong Tsudev.Audit.Cli.csproj - nen buoc nen o day moi thuc su co tac dung.
; lzma2/max cham khi DONG GOI nhung giai nen nhanh nhu cac muc khac, ma nguoi
; dung chi cam nhan toc do giai nen.
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
RestartApplications=no

[Languages]
; Vietnamese.isl la ban dich DO CONG DONG DONG GOP, KHONG nam trong ban cai
; Inno Setup 6 mac dinh (chi tai rieng tu jrsoftware.org/files/istrans/).
; Vi vay file duoc kem thang trong repo - neu tro toi "compiler:Languages\..."
; thi ISCC se bao loi tren may sach va tren runner cua GitHub.
Name: "en"; MessagesFile: "compiler:Default.isl"
#if FileExists(AddBackslash(SourcePath) + "Vietnamese.isl")
Name: "vi"; MessagesFile: "Vietnamese.isl"
#endif

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
Source: "SAU-KHI-CAI.txt";           DestDir: "{app}"; Flags: ignoreversion

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
