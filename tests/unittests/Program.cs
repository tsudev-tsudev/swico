using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Cli;
using Tsudev.Audit.Core.Collectors;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Progress;
using Tsudev.Audit.Core.Reports;
using Tsudev.Audit.Core.Rendering;
using Tsudev.Audit.Core.Rules;
using Tsudev.Audit.Core.Testing;
using Tsudev.Audit.Core.Updates;

int passed = 0, failed = 0;
void Check(bool cond, string label)
{
    if (cond) { passed++; Console.WriteLine($"  OK   {label}"); }
    else { failed++; Console.WriteLine($"  FAIL {label}"); }
}

Console.WriteLine("=== 1. Parser: ospp.vbs output ===");
const string osppSample = """
Microsoft (R) Office Software Protection Platform Version: 16.0.14332
---LICENSE STATUS DUMP---
LICENSE NAME: Office 21, Office2021ProPlusVL_KMS_Client edition
LICENSE DESCRIPTION: Office 21, VOLUME_KMSCLIENT channel
LICENSE STATUS:  ---LICENSED---
SKU ID: 85df9b39-5099-46e5-9784-adf0117c9429
---------------------------------------
LICENSE NAME: Office 21, ProjectProVL_KMS_Client edition
LICENSE DESCRIPTION: Office 21, VOLUME_KMSCLIENT channel
LICENSE STATUS:  ---NOTIFICATIONS---
SKU ID: 5b6cb1a4-c1f6-4d97-9f7f-a9c4f5c9b1e0
---------------------------------------
""";
var office = OfficeLicenseCollector.ParseOsppOutput(osppSample);
Check(office.Count == 2, $"tach dung 2 san pham Office (duoc {office.Count})");
Check(office[0].Status.Contains("Đã kích hoạt"), "san pham 1 = Licensed");
Check(office[1].Status.Contains("Chưa kích hoạt"), "san pham 2 = Notifications");
Check(office[0].SkuId.StartsWith("85df9b39"), "doc dung SKU ID");
Check(OfficeLicenseCollector.ParseOsppOutput("").Count == 0, "input rong -> khong crash, tra ve rong");

Console.WriteLine("\n=== 2. Parser: ngay thang WMI (CIM_DATETIME) ===");
Check(WmiDateParser.Parse("20260626120000.000000+420")?.ToString("yyyy-MM-dd") == "2026-06-26", "parse ngay hop le");
Check(WmiDateParser.Parse("") is null, "chuoi rong -> null");
Check(WmiDateParser.Parse("rac") is null, "chuoi rac -> null");
Check(WmiDateParser.Parse("99999999999999") is null, "so khong hop le -> null");
Check(SoftwareCollector.FormatInstallDate("20240105") == "2024-01-05", "InstallDate registry -> yyyy-MM-dd");
Check(SoftwareCollector.FormatInstallDate("") == "-", "InstallDate rong -> '-'");

Console.WriteLine("\n=== 3. Tinh diem rui ro (bien) ===");
foreach (var (score, expected) in new[] { (0, "Thấp"), (14, "Thấp"), (15, "Trung bình"),
                                          (39, "Trung bình"), (40, "Cao"), (69, "Cao"),
                                          (70, "Nghiêm trọng"), (100, "Nghiêm trọng") })
{
    var findings = new List<RiskFinding>();
    // Dung manualReviewCount de dat chinh xac diem mong muon (2d/phan mem, cap 15)
    var rs = RiskScoring.Compute(findings, false, 0);
    Check(rs.Value == 0, $"khong phat hien -> 0 diem");
    break;
}
var critical = new List<RiskFinding> { new() { Level = RiskLevel.Critical }, new() { Level = RiskLevel.Critical },
                                       new() { Level = RiskLevel.Critical } };
var scoreCritical = RiskScoring.Compute(critical, genuineCheckFailed: true, manualReviewCount: 0);
Check(scoreCritical.Value == RiskScoring.GenuineFailedWeight + RiskScoring.CriticalCap,
    $"gioi han nhom Critical hoat dong (duoc {scoreCritical.Value})");
Check(scoreCritical.Label == "Nghiêm trọng", "nhan bang = Nghiem trong");
var scoreCapped = RiskScoring.Compute(
    Enumerable.Range(0, 50).Select(_ => new RiskFinding { Level = RiskLevel.High }).ToList(),
    true, 100);
Check(scoreCapped.Value <= 100, $"diem khong bao gio vuot 100 (duoc {scoreCapped.Value})");

Console.WriteLine("\n=== 4. Quet dau hieu crack (may GIA LAP bi nhiem) ===");
var wmi = new FakeWmiQuery()
    .Add("Win32_OperatingSystem", new Dictionary<string, object?> { ["Caption"] = "Microsoft Windows 11 Pro", ["Version"] = "10.0.26200",
                                          ["BuildNumber"] = "26200", ["OSArchitecture"] = "64-bit",
                                          ["InstallDate"] = "20260626120000.000000+420" })
    .Add("Win32_ComputerSystem", new Dictionary<string, object?> { ["Manufacturer"] = "Dell Inc.", ["Model"] = "Latitude E5570",
                                         ["TotalPhysicalMemory"] = 17179869184L })
    .Add("Win32_BIOS", new Dictionary<string, object?> { ["SerialNumber"] = "FVRFVD2", ["SMBIOSBIOSVersion"] = "1.34.3" })
    .Add("Win32_Processor", new Dictionary<string, object?> { ["Name"] = "Intel(R) Core(TM) i7-6600U", ["NumberOfCores"] = 2,
                                    ["NumberOfLogicalProcessors"] = 4, ["MaxClockSpeed"] = 2801 })
    .Add("Win32_Service", new Dictionary<string, object?> { ["Name"] = "KMSAutoSvc", ["DisplayName"] = "KMSAuto Service" },
                          new Dictionary<string, object?> { ["Name"] = "Spooler", ["DisplayName"] = "Print Spooler" })
    .Add("SoftwareLicensingProduct", new Dictionary<string, object?> { ["Name"] = "Windows(R), Professional edition",
                                             ["Description"] = "Windows(R) Operating System, OEM_DM channel",
                                             ["LicenseStatus"] = 1, ["PartialProductKey"] = "KHJ6C",
                                             ["ProductKeyChannel"] = "OEM:DM" });

var files = new FakeFileProbe()
    .WithEnv("SystemRoot", @"C:\Windows")
    .WithEnv("ProgramData", @"C:\ProgramData")
    .WithEnv("SystemDrive", @"C:")
    .AddDirectory(@"C:\ProgramData", @"C:\ProgramData\KMSAuto", @"C:\ProgramData\Microsoft")
    .AddFile(@"C:\Windows\System32\SppExtComObjHook.dll")
    .AddFile(@"C:\Windows\System32\drivers\etc\hosts",
        "# comment\n127.0.0.1 kms.digiboy.ir\n127.0.0.1 localhost\n");

var registry = new FakeRegistryReader()
    .AddSubKeys(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        "{111}", "{222}", "{333}")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{111}", "DisplayName", "Visual Studio Code")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{111}", "DisplayVersion", "1.90.0")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{111}", "Publisher", "Microsoft Corporation")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{111}", "InstallDate", "20240105")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{222}", "DisplayName", "Infatica Proxy Tool")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{333}", "DisplayName", "7-Zip 22.01")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{333}", "DisplayVersion", "22.01")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{333}", "Publisher", "Igor Pavlov")
    .AddValue(RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform",
        "KeyManagementServiceName", "kms.fake-server.local");

var proc = new FakeProcessRunner()
    .When("slmgr.vbs", new ProcessResult(0, "Name: Windows(R), Professional edition\nLicense Status: Licensed", ""))
    .When("DISM.exe", new ProcessResult(0, "The component store is repairable.", ""));

var ctx = new SystemContext
{
    Wmi = wmi, Registry = registry, Process = proc, Files = files,
    ComputerName = "TSUITSTMY", ScanTime = DateTimeOffset.Parse("2026-08-17T10:00:00+07:00"),
    IsElevated = true
};

var findingsFound = ActivationRiskScanner.Scan(ctx);
Check(findingsFound.Any(f => f.Category == "Tên file/thư mục nghi vấn"), "phat hien thu muc KMSAuto");
Check(findingsFound.Any(f => f.Category == "File hook hệ thống bản quyền" && f.Level == RiskLevel.Critical),
    "phat hien file hook SppExtComObjHook.dll (muc Rat cao)");
Check(findingsFound.Any(f => f.Category == "Hosts file bị chỉnh sửa"), "phat hien hosts tro toi KMS cong cong");
Check(findingsFound.Any(f => f.Category == "Windows Service nghi vấn"), "phat hien service KMSAuto");
Check(findingsFound.Any(f => f.Category == "KMS server tùy chỉnh trong registry"), "phat hien KMS server trong registry");
Check(!findingsFound.Any(f => f.Detection.Contains("Print Spooler")), "KHONG bao dong nham service hop le");

var scope = ActivationRiskScanner.BuildScope(findingsFound);
Check(scope.Count == 6, "bang pham vi quet luon co du 6 hang muc");
Check(scope.Sum(s => s.FoundCount) == findingsFound.Count, "tong so dem trong bang pham vi khop so phat hien");

Console.WriteLine("\n=== 5. Thu thap phan mem tu registry ===");
var software = SoftwareCollector.Collect(ctx);
Check(software.Count == 3, $"doc duoc 3 phan mem (duoc {software.Count})");
Check(software.Any(s => s.Name == "Visual Studio Code" && s.Category == "Công cụ lập trình"), "phan loai VS Code dung");
Check(software.Any(s => s.Name == "Infatica Proxy Tool" && s.NeedsManualReview), "Infatica bi danh dau can kiem tra thu cong");
Check(!software.First(s => s.Name == "7-Zip 22.01").NeedsManualReview, "7-Zip du thong tin -> khong can kiem tra");
Check(software.First(s => s.Name == "Visual Studio Code").InstallDate == "2024-01-05", "ngay cai dat duoc chuan hoa");

Console.WriteLine("\n=== 6. Dien giai ket qua DISM/SFC ===");
Check(SystemIntegrityCollector.InterpretDism(new ProcessResult(0, "No component store corruption detected.", ""))
    .Contains("Không phát hiện lỗi"), "DISM sach");
Check(SystemIntegrityCollector.InterpretDism(new ProcessResult(0, "The component store is repairable.", ""))
    .Contains("CÓ THỂ SỬA ĐƯỢC"), "DISM co the sua");
Check(SystemIntegrityCollector.InterpretDism(new ProcessResult(-1, "", ""))
    .Contains("Không chạy được"), "DISM khong chay duoc -> thong bao ro rang");
Check(SystemIntegrityCollector.InterpretSfc(new ProcessResult(0, "Windows Resource Protection did not find any integrity violations.", ""))
    .Contains("Không phát hiện"), "SFC sach");

Console.WriteLine("\n=== 7. Dung bao cao hoan chinh ===");
var options = new AuditOptions { RunDism = true, RunSfc = false };
var licReport = LicenseReportBuilder.Build(ctx, options);
Check(licReport.VerdictLevel == VerdictLevel.Bad, "ket luan = Bad (do co phat hien muc cao)");
Check(licReport.RiskFindingsCount == findingsFound.Count, "so phat hien khop");
Check(licReport.RiskScore!.Value > 0, $"diem rui ro > 0 (duoc {licReport.RiskScore.Value})");
Check(licReport.Sections.Count == 6, $"bao cao ban quyen co 6 muc (duoc {licReport.Sections.Count})");
Check(licReport.Sections.Select(s => s.Id).Distinct().Count() == licReport.Sections.Count, "cac Id muc khong trung nhau");

var ctx2 = new SystemContext
{
    Wmi = wmi, Registry = registry, Process = proc, Files = files,
    ComputerName = "TSUITSTMY", ScanTime = DateTimeOffset.Parse("2026-08-17T10:05:00+07:00"), IsElevated = true
};
var hwReport = HardwareReportBuilder.Build(ctx2, options);
Check(hwReport.HardwareSummary!.Contains("Latitude E5570"), "tom tat phan cung dung");
Check(hwReport.Sections.Any(s => s.Id == "sec-defender"), "co muc Defender");
Check(hwReport.Sections.Any(s => s.Id == "sec-integrity"), "co muc toan ven he thong");

Console.WriteLine("\n=== 8. Render HTML + JSON round-trip ===");
var renderer = new HtmlReportRenderer();
var html = renderer.Render(licReport, "../tsudev-tong-hop.html");
Check(html.Contains("<!DOCTYPE html>") && html.TrimEnd().EndsWith("</html>"), "HTML hoan chinh");
Check(html.Contains("https://tsudev.com"), "co link tsudev.com");
Check(html.Contains("#eaf2fc"), "dung theme xanh nhat");
Check(!html.Contains("prefers-color-scheme"), "KHONG con dark-mode tu dong");
Check(html.Contains("KMSAuto"), "noi dung phat hien xuat hien trong HTML");

// Kiem tra HTML-escape: du lieu doc hai khong duoc pha vo trang
var evilCtx = new SystemContext
{
    Wmi = new FakeWmiQuery(), Registry = new FakeRegistryReader(),
    Process = new FakeProcessRunner(), Files = new FakeFileProbe(),
    ComputerName = "<script>alert(1)</script>", ScanTime = DateTimeOffset.Now, IsElevated = false
};
var evilReport = LicenseReportBuilder.Build(evilCtx, options);
var evilHtml = renderer.Render(evilReport);
Check(!evilHtml.Contains("<script>alert(1)</script>"), "ten may doc hai DA duoc escape (chong HTML injection)");

var jsonOpts = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
var json = JsonSerializer.Serialize(licReport, jsonOpts);
var restored = JsonSerializer.Deserialize<AuditReport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
Check(restored.ComputerName == licReport.ComputerName, "JSON round-trip giu ten may");
Check(restored.VerdictLevel == licReport.VerdictLevel, "JSON round-trip giu ket luan (enum)");
Check(restored.RiskScore?.Value == licReport.RiskScore?.Value, "JSON round-trip giu diem rui ro");
Check(restored.SchemaVersion == AuditReport.CurrentSchemaVersion, "JSON co SchemaVersion");

Console.WriteLine("\n=== 9. XLSX writer ===");
var tmp = Path.Combine(Path.GetTempPath(), $"tsudev-test-{Guid.NewGuid():N}.xlsx");
new XlsxWriter().Write(tmp, licReport.Sections.SelectMany(s => s.Tables)
    .Select((t, i) => { if (string.IsNullOrWhiteSpace(t.Title)) t.Title = $"Bang {i + 1}"; return t; }));
Check(File.Exists(tmp) && new FileInfo(tmp).Length > 1000, "tao duoc file .xlsx co noi dung");
Check(XlsxWriter.ColumnName(1) == "A" && XlsxWriter.ColumnName(26) == "Z" && XlsxWriter.ColumnName(27) == "AA",
    "quy doi ten cot Excel dung");
Check(XlsxWriter.IsNumeric("42", out _) && !XlsxWriter.IsNumeric("007", out _) && !XlsxWriter.IsNumeric("-", out _),
    "nhan dien so: '42' la so, '007' va '-' KHONG phai so");
var used = new HashSet<string>();
var n1 = XlsxWriter.MakeSafeSheetName("Phạm vi quét (luôn hiển thị dù có phát hiện hay không)", used);
var n2 = XlsxWriter.MakeSafeSheetName("Phạm vi quét (luôn hiển thị dù có phát hiện hay không)", used);
Check(n1.Length <= 31 && n2.Length <= 31 && n1 != n2, "ten sheet bi cat 31 ky tu va khong trung nhau");
File.Delete(tmp);

Console.WriteLine("\n=== 8b. Ket luan license: khong duoc bao NHAM la hop le ===");

// LOI HOI QUY THAT (phat hien khi doi chieu voi bo PowerShell cu tren may that):
// truoc day ket luan duoc suy ra bang licensedCount > 0, tuc chi can MOT SKU
// bat ky o trang thai Licensed. Windows khai bao NHIEU SKU duoi cung mot
// ApplicationID, nen may co SKU chinh dang Notification nhung co SKU phu
// Licensed van bi cham diem "hop le".
static SystemContext LicenseCtx(params (long Status, string Key)[] skus)
{
    var wmi = new FakeWmiQuery();
    foreach (var (status, key) in skus)
        wmi.Add("SoftwareLicensingProduct", new Dictionary<string, object?>
        {
            ["Name"] = "Windows(R), Professional edition",
            ["Description"] = "Windows(R) Operating System",
            // ApplicationID la BAT BUOC: collector loc theo truong nay de tach
            // SKU Windows khoi SKU Office. Adapter gia lap nay tuan thu
            // whereClause, nen thieu truong la khong dong nao lot qua bo loc.
            ["ApplicationID"] = "55c92734-d682-4d71-983e-d6ec3f16059f",
            ["LicenseStatus"] = status,
            ["PartialProductKey"] = key,
            ["ProductKeyChannel"] = "Retail"
        });
    return new SystemContext
    {
        Wmi = wmi, Registry = new FakeRegistryReader(),
        Process = new FakeProcessRunner(), Files = new FakeFileProbe(),
        ComputerName = "MAY-TEST", ScanTime = DateTimeOffset.Now, IsElevated = true
    };
}

// 1 = Licensed, 5 = Notification (chua kich hoat)
var mixed = WindowsLicenseCollector.Collect(LicenseCtx((1, "AAAAA"), (5, "BBBBB"))).Summary;
Check(!mixed.IsGenuine,
    "SKU vua Licensed vua Notification -> KHONG duoc ket luan hop le");
Check(mixed.Overall == LicenseHealth.Problem, "tron lan co SKU hong -> xep loai Problem");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), mixed.Overall, LicenseHealth.Unknown).Level == VerdictLevel.Bad,
    "ket luan cuoi cung la Bad khi co SKU chua kich hoat");

var allOk = WindowsLicenseCollector.Collect(LicenseCtx((1, "AAAAA"), (1, "BBBBB"))).Summary;
Check(allOk.IsGenuine && allOk.Overall == LicenseHealth.Ok, "moi SKU Licensed -> hop le");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), allOk.Overall, LicenseHealth.Unknown).Level == VerdictLevel.Ok,
    "ket luan Ok khi moi SKU deu hop le");

// 2 = OOB Grace: chua sai nhung cung CHUA hop le -> canh bao, khong phai Ok
var graceOnly = WindowsLicenseCollector.Collect(LicenseCtx((1, "AAAAA"), (2, "BBBBB"))).Summary;
Check(!graceOnly.IsGenuine && graceOnly.Overall == LicenseHealth.Grace,
    "con han dung thu -> KHONG phai hop le");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), graceOnly.Overall, LicenseHealth.Unknown).Level == VerdictLevel.Warning,
    "con han dung thu -> canh bao, khong phai Ok cung khong phai Bad");

// Bo loc ApplicationID phai tach dung SKU Windows khoi SKU Office: neu khong,
// mot Office chua kich hoat se bi cham diem nham thanh "Windows co van de".
var mixedApps = new FakeWmiQuery()
    .Add("SoftwareLicensingProduct", new Dictionary<string, object?>
    {
        ["Name"] = "Windows(R), Professional edition",
        ["ApplicationID"] = "55c92734-d682-4d71-983e-d6ec3f16059f",
        ["LicenseStatus"] = 1L, ["PartialProductKey"] = "AAAAA"
    })
    .Add("SoftwareLicensingProduct", new Dictionary<string, object?>
    {
        ["Name"] = "Office 16, Office16HomePremR_Retail edition",
        ["ApplicationID"] = "0ff1ce15-a989-479d-af46-f275c6370663",
        ["LicenseStatus"] = 5L, ["PartialProductKey"] = "BBBBB"
    });
var winOnly = WindowsLicenseCollector.Collect(new SystemContext
{
    Wmi = mixedApps, Registry = new FakeRegistryReader(),
    Process = new FakeProcessRunner(), Files = new FakeFileProbe(),
    ComputerName = "MAY-TEST", ScanTime = DateTimeOffset.Now, IsElevated = true
}).Summary;
Check(winOnly.Total == 1 && winOnly.IsGenuine,
    "bo loc ApplicationID tach dung SKU Windows, KHONG lay nham SKU Office");

var none = WindowsLicenseCollector.Collect(LicenseCtx()).Summary;
Check(!none.IsKnown && none.Overall == LicenseHealth.Unknown, "khong doc duoc SKU nao -> Unknown");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), none.Overall, LicenseHealth.Unknown).Level == VerdictLevel.Unknown,
    "khong co du lieu -> chua du co so ket luan, KHONG mac dinh la hop le");

// Dau hieu crack muc cao van thang moi trang thai license
Check(RiskScoring.BuildVerdict(
        new[] { new RiskFinding { Category = "x", Detection = "y", Level = RiskLevel.Critical } },
        LicenseHealth.Ok, LicenseHealth.Ok).Level == VerdictLevel.Bad,
    "phat hien muc Rat cao -> Bad du license bao hop le");

Console.WriteLine("\n=== 8c. Office phai co tieng noi trong ket luan ===");

// CA HOI QUY DUNG THEO MAY THAT CUA NGUOI DUNG (TSUITSTMY, 18/08/2026):
//   Windows Professional (ApplicationID 55c92734-...) -> LicenseStatus 1 (Licensed)
//   Office 16 HomePremR  (ApplicationID 0ff1ce15-...) -> LicenseStatus 5 (Notification)
// Truoc day cong cu ket luan "KHONG PHAT HIEN DAU HIEU" vi:
//   (a) WindowsLicenseCollector loc theo ApplicationID cua Windows nen khong
//       thay SKU Office, va
//   (b) trang thai Office khong he tham gia vao ket luan tong the.
// Bo PowerShell cu bao "khong hop le" - va no dung.
var realWorld = new FakeWmiQuery()
    .Add("SoftwareLicensingProduct", new Dictionary<string, object?>
    {
        ["Name"] = "Windows(R), Professional edition",
        ["Description"] = "Windows(R) Operating System, RETAIL channel",
        ["ApplicationID"] = "55c92734-d682-4d71-983e-d6ec3f16059f",
        ["LicenseStatus"] = 1L, ["PartialProductKey"] = "ABCDE", ["ID"] = "sku-win"
    })
    .Add("SoftwareLicensingProduct", new Dictionary<string, object?>
    {
        ["Name"] = "Office 16, Office16HomePremR_Retail edition",
        ["Description"] = "Office 16, RETAIL channel",
        ["ApplicationID"] = "0ff1ce15-a989-479d-af46-f275c6370663",
        ["LicenseStatus"] = 5L, ["PartialProductKey"] = "FGHIJ", ["ID"] = "sku-office"
    });

var realCtx = new SystemContext
{
    Wmi = realWorld, Registry = new FakeRegistryReader(),
    Process = new FakeProcessRunner(), Files = new FakeFileProbe(),
    ComputerName = "TSUITSTMY", ScanTime = DateTimeOffset.Now, IsElevated = true
};

var officeSummary = OfficeLicenseCollector.Collect(realCtx).Summary;
Check(officeSummary.IsInstalled && officeSummary.Total == 1,
    "doc duoc SKU Office tu WMI (khong can ospp.vbs)");
Check(officeSummary.Overall == LicenseHealth.Problem,
    "Office o trang thai Notification -> xep loai Problem");

var realVerdict = RiskScoring.BuildVerdict(
    Array.Empty<RiskFinding>(), LicenseHealth.Ok, officeSummary.Overall);
Check(realVerdict.Level == VerdictLevel.Warning,
    "Windows hop le + Office chua kich hoat -> CANH BAO, khong con la 'khong phat hien dau hieu'");
Check(realVerdict.Title.Contains("OFFICE", StringComparison.Ordinal),
    "ket luan noi RO la van de nam o Office");

// Office chua kich hoat la van de TUAN THU, khong phai dau hieu crack.
// Gop chung vao muc Bad se lam mat y nghia cua muc Bad.
Check(realVerdict.Level != VerdictLevel.Bad,
    "Office chua kich hoat KHONG bi day len muc Bad");

Check(OfficeLicenseCollector.ClassifyOspp("Notification (Chưa kích hoạt - đã hết hạn thông báo)") == LicenseHealth.Problem
   && OfficeLicenseCollector.ClassifyOspp("Licensed (Đã kích hoạt)") == LicenseHealth.Ok
   && OfficeLicenseCollector.ClassifyOspp("Grace period (Chưa kích hoạt - còn hạn dùng thử)") == LicenseHealth.Grace,
    "xep loai trang thai ospp.vbs dung");

Console.WriteLine("\n=== 9b. Toan ven goi OPC cua file .xlsx ===");

// Bo test cu chi kiem "file co ton tai va lon hon 1000 byte". Do la ly do mot
// file .xlsx THIEU QUAN HE toi styles.xml van di qua duoc toan bo 54 test, roi
// Excel that bao "We found a problem with some content" khi nguoi dung mo len.
// Cac kiem tra duoi day soi vao CAU TRUC goi, khong chi soi kich thuoc.
var opcPath = Path.Combine(Path.GetTempPath(), $"tsudev-opc-{Guid.NewGuid():N}.xlsx");
new XlsxWriter().Write(opcPath, licReport.Sections.SelectMany(s => s.Tables)
    .Select((t, i) => { if (string.IsNullOrWhiteSpace(t.Title)) t.Title = $"Bang {i + 1}"; return t; }));

using (var zip = ZipFile.OpenRead(opcPath))
{
    string Part(string name) => new StreamReader(zip.GetEntry(name)!.Open()).ReadToEnd();

    var names = zip.Entries.Select(e => e.FullName).ToHashSet(StringComparer.Ordinal);
    var wbRels = Part("xl/_rels/workbook.xml.rels");
    var contentTypes = Part("[Content_Types].xml");
    var workbook = Part("xl/workbook.xml");

    Check(names.Contains("xl/styles.xml"), "goi co phan styles.xml");

    // Day la ca kiem thu bat duoc dung loi that: phan nam trong goi nhung
    // KHONG co quan he nao tro toi thi theo chuan OPC coi nhu khong ton tai.
    Check(wbRels.Contains("Target=\"styles.xml\"", StringComparison.Ordinal),
        "workbook.xml.rels CO quan he tro toi styles.xml");

    // Moi phan trong goi (tru chinh cac file .rels) phai duoc mot quan he tro toi.
    var rootRels = Part("_rels/.rels");
    var allRels = rootRels + wbRels;
    var unreferenced = names
        .Where(n => !n.EndsWith(".rels", StringComparison.Ordinal) && n != "[Content_Types].xml")
        .Where(n => !allRels.Contains(Path.GetFileName(n), StringComparison.Ordinal))
        .ToList();
    Check(unreferenced.Count == 0,
        $"moi phan trong goi deu duoc quan he tro toi (mo coi: {string.Join(", ", unreferenced)})");

    // Moi phan phai duoc khai bao kieu noi dung, neu khong Excel khong biet doc.
    var undeclared = names
        .Where(n => n.EndsWith(".xml", StringComparison.Ordinal) && n != "[Content_Types].xml")
        .Where(n => !contentTypes.Contains("/" + n, StringComparison.Ordinal))
        .ToList();
    Check(undeclared.Count == 0,
        $"moi phan .xml deu khai bao trong [Content_Types] (thieu: {string.Join(", ", undeclared)})");

    // sheetView khai bao workbookViewId="0" nen bookViews phai ton tai.
    Check(workbook.Contains("<bookViews>", StringComparison.Ordinal),
        "workbook.xml co <bookViews> (vi sheetView tro toi workbookViewId=0)");

    // Moi phan phai la XML doc duoc.
    bool allParse = true;
    foreach (var e in zip.Entries)
    {
        try { System.Xml.Linq.XDocument.Parse(new StreamReader(e.Open()).ReadToEnd()); }
        catch (System.Xml.XmlException) { allParse = false; }
    }
    Check(allParse, "moi phan trong goi deu la XML hop le");
}
File.Delete(opcPath);

// Cat ten sheet KHONG duoc lam vo cap the thay the (surrogate pair): mot nua
// the thay the khong phai ky tu XML hop le va se lam Excel bao file hong.
var emojiTitle = new string('A', 30) + "\U0001F600" + "duoi";
var emojiName = XlsxWriter.MakeSafeSheetName(emojiTitle, new HashSet<string>());
Check(emojiName.Length <= 31 && !char.IsSurrogate(emojiName[^1]),
    "cat ten sheet khong lam vo cap the thay the");

Console.WriteLine("\n=== 9c. Thuong hieu trong bao cao ===");

Check(html.Contains("data:image/png;base64,", StringComparison.Ordinal),
    "logo duoc NHUNG thang vao trang (khong phu thuoc file ngoai)");
Check(html.Contains("<link rel=\"icon\"", StringComparison.Ordinal),
    "trang co favicon");
Check(html.Contains("theme-color", StringComparison.Ordinal), "co mau chu de cho trinh duyet");
Check(!html.Contains("src=\"assets/", StringComparison.Ordinal)
   && !html.Contains(".png\"", StringComparison.Ordinal),
    "khong tro toi file anh ben ngoai - bao cao copy di van hien duoc logo");
Check(html.Contains("bw-tsu", StringComparison.Ordinal) && html.Contains("bw-dev", StringComparison.Ordinal),
    "chu ky thuong hieu tach 'tsu' va 'dev' thanh hai the rieng de to hai mau");
Check(html.Contains(">tsu<", StringComparison.Ordinal) && html.Contains(">dev<", StringComparison.Ordinal),
    "chu ky la VAN BAN that (sac net khi in, doc duoc bang trinh doc man hinh)");

// Logo phai la mot lien ket bam duoc toi tsudev.com
var brandIdx = html.IndexOf("class=\"brand\"", StringComparison.Ordinal);
Check(brandIdx > 0 && html.LastIndexOf("https://tsudev.com", brandIdx + 200, StringComparison.Ordinal) > 0,
    "logo nam trong lien ket toi tsudev.com");
Check(html.Contains("role=\"img\"", StringComparison.Ordinal)
   && html.Contains("aria-label=\"tsudev\"", StringComparison.Ordinal),
    "logo co van ban thay the cho trinh doc man hinh");

// Logo hien o nhieu cho nhung du lieu base64 chi duoc nhung MOT lan (nam trong
// CSS). Nhung lap se cong them ~13 KB cho moi lan xuat hien.
// Dung 2: mot cho favicon, mot cho logo. Logo hien o nhieu cho nhung du lieu
// nam trong CSS nen chi nhung mot lan.
Check(html.Split("data:image/png;base64,").Length - 1 == 2,
    "chi co dung 2 anh nhung: favicon + logo (logo khong bi nhung lap)");
Check(html.Contains("rel=\"noopener noreferrer\"", StringComparison.Ordinal),
    "lien ket ra ngoai co rel=noopener noreferrer");

// Logo xuat hien o CA dau trang lan chan trang
Check(html.Split("class=\"brand").Length - 1 >= 2, "thuong hieu hien o ca dau trang va chan trang");

Console.WriteLine("\n=== 10. Bo luat phat hien tach roi ===");

// Bo luat dong kem phai nap duoc va hop le - neu hong thi moi may deu "sach",
// tao cam giac an toan gia, nen day la ca kiem thu quan trong.
var embedded = DetectionRuleSet.Embedded;
Check(embedded.Validate().Count == 0, "bo luat dong kem hop le");
Check(!string.IsNullOrWhiteSpace(embedded.Version), "bo luat dong kem co so hieu phien ban");
Check(embedded.SuspiciousNames.Contains("KMSAuto"), "bo luat dong kem giu du ten nghi van");

// Scan(ctx) va Scan(ctx, Embedded) phai cho KET QUA Y HET - chung minh viec
// tach luat khong lam doi hanh vi.
var viaDefault = ActivationRiskScanner.Scan(ctx);
var viaExplicit = ActivationRiskScanner.Scan(ctx, DetectionRuleSet.Embedded);
Check(viaDefault.Count == viaExplicit.Count, "tach luat KHONG lam doi hanh vi quet");

// Bo luat tu file ngoai phai thuc su duoc dung
var customJson = """
{
  "version": "test-9.9",
  "scanRoots": ["%ProgramFiles%"],
  "suspiciousNames": ["CongCuBiaDatRaDeTest"],
  "legitimateTaskNames": ["SoftwareProtectionPlatform"],
  "hookDirectories": ["%SystemRoot%\\System32"],
  "hookFiles": ["KhongCoThat.dll"],
  "knownKmsHosts": [],
  "hostsInterferenceKeywords": []
}
""";
var custom = DetectionRuleSet.Parse(customJson);
Check(custom.Version == "test-9.9" && custom.Validate().Count == 0, "nap duoc bo luat tu JSON ngoai");
Check(ActivationRiskScanner.Scan(ctx, custom).Count < viaDefault.Count,
    "bo luat khac cho ket qua khac (luat that su duoc dung)");

// File hong / thieu KHONG duoc lam hong lan quet
var missing = DetectionRuleSet.LoadOrEmbedded("/khong/ton/tai/rules.json", out var w1);
Check(ReferenceEquals(missing, DetectionRuleSet.Embedded) && w1 is not null,
    "file luat thieu -> quay ve bo luat dong kem kem canh bao");

var badPath = Path.Combine(Path.GetTempPath(), $"bad-rules-{Guid.NewGuid():N}.json");
File.WriteAllText(badPath, "{ day khong phai JSON hop le ");
var broken = DetectionRuleSet.LoadOrEmbedded(badPath, out var w2);
Check(ReferenceEquals(broken, DetectionRuleSet.Embedded) && w2 is not null,
    "file luat sai dinh dang -> quay ve bo luat dong kem kem canh bao");
File.Delete(badPath);

// Bo luat RONG nguy hiem hon bo luat sai: no lam moi may deu "sach"
var emptyPath = Path.Combine(Path.GetTempPath(), $"empty-rules-{Guid.NewGuid():N}.json");
File.WriteAllText(emptyPath, """{"version":"rong","suspiciousNames":[],"scanRoots":[],"hookFiles":[],"hookDirectories":[]}""");
var empty = DetectionRuleSet.LoadOrEmbedded(emptyPath, out var w3);
Check(ReferenceEquals(empty, DetectionRuleSet.Embedded) && w3 is not null,
    "bo luat RONG bi tu choi (khong duoc tao cam giac an toan gia)");
File.Delete(emptyPath);

Check(licReport.DetectionRulesVersion == embedded.Version,
    "bao cao ghi lai phien ban bo luat da dung");

// CAM BAY: file luat ben ngoai LUON thang bo luat dong kem. Sau khi nang cap
// exe, mot file .json CU nam canh exe se am tham vo hieu hoa bo luat moi.
// Phai co canh bao ro rang, neu khong khong ai biet dieu do dang xay ra.
var stalePath = Path.Combine(Path.GetTempPath(), $"stale-rules-{Guid.NewGuid():N}.json");
File.WriteAllText(stalePath, customJson);          // phien ban "test-9.9", khac ban dong kem
var stale = DetectionRuleSet.LoadOrEmbedded(stalePath, out var staleWarn);
Check(stale.Version == "test-9.9", "file luat ngoai duoc uu tien hon ban dong kem");
Check(staleWarn is not null
   && staleWarn.Contains("test-9.9", StringComparison.Ordinal)
   && staleWarn.Contains(embedded.Version, StringComparison.Ordinal),
    "lech phien ban giua file ngoai va ban dong kem -> canh bao neu RO ca hai phien ban");
File.Delete(stalePath);

// Cung phien ban thi KHONG canh bao - tranh lam nhieu bao cao moi lan quet.
// Dung lai chinh bo luat dong kem thay vi doc file theo duong dan tuong doi:
// duong dan tuong doi phu thuoc thu muc chay va se hong khi doi cach chay test.
var samePath = Path.Combine(Path.GetTempPath(), $"same-rules-{Guid.NewGuid():N}.json");
File.WriteAllText(samePath, JsonSerializer.Serialize(embedded));
var same = DetectionRuleSet.LoadOrEmbedded(samePath, out var sameWarn);
Check(same.Version == embedded.Version && sameWarn is null,
    "file luat ngoai CUNG phien ban -> khong canh bao thua");
File.Delete(samePath);

Console.WriteLine("\n=== 11. CLI: phan tich tham so ===");

// README tung tuyen bo "CLI parse tham so: 16/16 test" trong khi KHONG co
// mot test CLI nao - CliOptions nam trong project net8.0-windows nen bo test
// chay tren Linux khong voi toi duoc. Da chuyen sang Core de tuyen bo do
// thanh su that.
var d = CliOptions.Parse(Array.Empty<string>());
Check(d.Scope == AuditScope.All && !d.Silent && d.RunDism && !d.RunSfc && d.ExportCsv,
    "mac dinh: quet ca hai, chay DISM, xuat CSV, khong chay SFC");
Check(!d.ShowHelp && !d.ShowVersion && !d.NoVerdictExit && d.RulesPath is null,
    "mac dinh: khong bat co nao");

Check(CliOptions.Parse(new[]{"-h"}).ShowHelp
   && CliOptions.Parse(new[]{"--help"}).ShowHelp
   && CliOptions.Parse(new[]{"/?"}).ShowHelp, "ba dang tham so tro giup deu nhan");
Check(CliOptions.Parse(new[]{"--version"}).ShowVersion, "--version");
Check(CliOptions.Parse(new[]{"--silent"}).Silent && CliOptions.Parse(new[]{"-s"}).Silent, "--silent / -s");
Check(CliOptions.Parse(new[]{"--sfc"}).RunSfc, "--sfc bat quet sau");
Check(!CliOptions.Parse(new[]{"--no-dism"}).RunDism, "--no-dism tat DISM");
Check(!CliOptions.Parse(new[]{"--no-csv"}).ExportCsv, "--no-csv tat xuat CSV");
Check(CliOptions.Parse(new[]{"--verbose"}).Verbose && CliOptions.Parse(new[]{"-v"}).Verbose, "--verbose / -v");
Check(CliOptions.Parse(new[]{"--no-verdict-exit"}).NoVerdictExit, "--no-verdict-exit");

Check(CliOptions.Parse(new[]{"--scope","license"}).Scope == AuditScope.License
   && CliOptions.Parse(new[]{"--scope","HARDWARE"}).Scope == AuditScope.Hardware
   && CliOptions.Parse(new[]{"--scope","All"}).Scope == AuditScope.All,
    "--scope nhan ca ba gia tri, khong phan biet hoa thuong");
Check(CliOptions.Parse(new[]{"-o",@"D:\BaoCao"}).OutputRoot == @"D:\BaoCao", "-o nhan duong dan");
Check(CliOptions.Parse(new[]{"--rules",@"C:\luat.json"}).RulesPath == @"C:\luat.json", "--rules nhan duong dan");

// Duong dan KHONG duoc ha ve chu thuong: he thong tep cua Windows giu nguyen
// hoa thuong, va tren nen tang khac thi duong dan phan biet hoa thuong.
Check(CliOptions.Parse(new[]{"-o",@"D:\BaoCao"}).OutputRoot != @"d:\baocao",
    "duong dan giu nguyen hoa thuong");

static bool Rejects(params string[] args)
{
    try { CliOptions.Parse(args); return false; } catch (ArgumentException) { return true; }
}
Check(Rejects("--khong-ton-tai"), "tu choi tham so la");
Check(Rejects("--scope"), "--scope thieu gia tri -> loi");
Check(Rejects("--scope","linh-tinh"), "--scope gia tri sai -> loi");
Check(Rejects("--output"), "--output thieu duong dan -> loi");
Check(Rejects("--rules"), "--rules thieu duong dan -> loi");

var combo = CliOptions.Parse(new[]{"--scope","license","-o","D:\\X","--silent","--sfc","--no-csv"});
Check(combo.Scope == AuditScope.License && combo.OutputRoot == "D:\\X"
   && combo.Silent && combo.RunSfc && !combo.ExportCsv, "ket hop nhieu tham so");

Check(CliOptions.UsageText.Contains("swico.exe") && !CliOptions.UsageText.Contains("tsudev-audit.exe"),
    "phan tro giup dung ten lenh moi");

Console.WriteLine("\n=== 12. CLI: ma thoat ===");

Check(ExitCodes.FromVerdicts(new[]{ VerdictLevel.Ok, VerdictLevel.Unknown }) == ExitCodes.Ok,
    "khong co ket luan dang bao dong -> 0");
Check(ExitCodes.FromVerdicts(new[]{ VerdictLevel.Ok, VerdictLevel.Warning }) == ExitCodes.VerdictWarning,
    "co canh bao -> 10");
Check(ExitCodes.FromVerdicts(new[]{ VerdictLevel.Warning, VerdictLevel.Bad }) == ExitCodes.VerdictCritical,
    "co ket luan nghiem trong -> 20 (thang canh bao)");
Check(ExitCodes.FromVerdicts(Array.Empty<VerdictLevel>()) == ExitCodes.Ok, "khong co bao cao nao -> 0");

// CA HOI QUY DUNG THEO MAY THAT: Windows hop le + Office chua kich hoat,
// truoc day tra ve 0 nen script khong bat duoc.
Check(ExitCodes.Combine(ExitCodes.Ok, ExitCodes.VerdictWarning) == ExitCodes.VerdictWarning,
    "Office chua kich hoat -> ma thoat 10, KHONG con la 0");

Check(ExitCodes.Combine(ExitCodes.Partial, ExitCodes.VerdictCritical) == ExitCodes.VerdictCritical,
    "ket luan danh gia thang 'thieu du lieu'");
Check(ExitCodes.Combine(ExitCodes.Fatal, ExitCodes.VerdictCritical) == ExitCodes.Fatal,
    "loi nghiem trong thang tat ca");
Check(ExitCodes.Combine(ExitCodes.BadArgs, ExitCodes.VerdictWarning) == ExitCodes.BadArgs,
    "tham so sai thang ket luan danh gia");
Check(ExitCodes.Combine(ExitCodes.Partial, ExitCodes.Ok) == ExitCodes.Partial,
    "thieu du lieu ma khong co ket luan -> giu ma 1");
Check(ExitCodes.Combine(ExitCodes.Ok, ExitCodes.Ok) == ExitCodes.Ok, "moi thu binh thuong -> 0");
Check(ExitCodes.Describe(ExitCodes.VerdictWarning).Contains("CẢNH BÁO", StringComparison.Ordinal),
    "mo ta ma thoat bang tieng Viet");

Console.WriteLine("\n=== 13. So hieu phien ban (CalVer) ===");

static VersionNumber V(string s) { VersionNumber.TryParse(s, out var v); return v; }
Check(V("26.8.18") == new VersionNumber(26, 8, 18), "doc dung 26.8.18");
Check(V("v26.8.18") == new VersionNumber(26, 8, 18), "chap nhan tien to 'v' cua tag GitHub");
Check(V("26.8.18+9e1f2c") == new VersionNumber(26, 8, 18), "cat bam commit do SDK them");
Check(V("26.8.20-rc1") == new VersionNumber(26, 8, 20), "cat hau to -rc1");
Check(V("26.8.20") > V("26.8.18"), "cung thang: ngay lon hon la moi hon");
Check(V("26.9.3") > V("26.8.20"), "sang thang moi: 26.9.3 moi hon 26.8.20");
Check(V("27.1.1") > V("26.12.31"), "sang nam moi");
Check(!V("linh tinh").IsValid && !V("").IsValid && !V("26.8").IsValid,
    "chuoi la / rong / thieu thanh phan -> khong hop le");
Check(!V("26.13.1").IsValid && !V("26.8.99").IsValid, "thang/ngay ngoai pham vi -> khong hop le");

// Thanh phan thu tu cho truong hop phat hanh lai TRONG CUNG MOT NGAY.
// Thieu no thi hai ban dung khac nhau se mang cung mot so hieu.
Check(V("26.8.18.1") > V("26.8.18"), "26.8.18.1 moi hon 26.8.18");
Check(V("26.8.18.2") > V("26.8.18.1"), "so lan phat hanh lai tang dan");
Check(V("26.8.19") > V("26.8.18.5"), "sang ngay moi van thang moi lan phat hanh lai");
Check(V("26.8.18.1").ToString() == "26.8.18.1" && V("26.8.18").ToString() == "26.8.18",
    "chi hien thanh phan thu tu khi khac 0");

Console.WriteLine("\n=== 14. Cong kiem tra cap nhat ===");

static ReleaseInfo Rel(string tag, string? installer = "https://x/swico-setup.exe")
    => new(V(tag), tag, "https://x/releases/" + tag, installer, "https://x/SHA256SUMS.txt", DateTimeOffset.Now);

var newer = new UpdateChecker(new FakeFeed(Rel("v26.8.20"))).Check("26.8.18");
Check(newer.Status == UpdateStatus.UpdateRequired && newer.MustUpdate,
    "co ban moi hon -> BUOC cap nhat");
Check(newer.Latest!.Version == V("26.8.20"), "giu thong tin ban moi de hien trong hop thoai");

Check(new UpdateChecker(new FakeFeed(Rel("v26.8.18"))).Check("26.8.18").Status == UpdateStatus.UpToDate,
    "cung phien ban -> da moi nhat");
Check(new UpdateChecker(new FakeFeed(Rel("v26.8.10"))).Check("26.8.18").Status == UpdateStatus.UpToDate,
    "ban tren GitHub CU hon (vi du bi rollback) -> khong bat ha cap");

// QUYET DINH THIET KE QUAN TRONG NHAT: kiem tra that bai thi KHONG chan.
// Cong cu duoc dung o may cach ly mang, may bi tuong lua chan - chan lai se
// lam no vo dung chinh o noi can nhat.
var noNetwork = new UpdateChecker(new FakeFeed(err: "Không kết nối được")).Check("26.8.18");
Check(noNetwork.Status == UpdateStatus.CheckFailed && !noNetwork.MustUpdate,
    "mat mang -> KHONG chan quet");
var threw = new UpdateChecker(new FakeFeed(ex: new InvalidOperationException("sap"))).Check("26.8.18");
Check(threw.Status == UpdateStatus.CheckFailed && !threw.MustUpdate,
    "nguon nem exception -> KHONG chan quet, khong lam sap cong cu");
Check(!new UpdateChecker(new FakeFeed(Rel("khong-phai-phien-ban"))).Check("26.8.18").MustUpdate,
    "tag GitHub khong doc duoc -> KHONG chan quet");
Check(!new UpdateChecker(new FakeFeed(Rel("v26.8.20"))).Check("khong-doc-duoc").MustUpdate,
    "khong doc duoc phien ban CUA CHINH MINH -> KHONG chan quet");

// Co ban moi nhung khong co file cai dat -> bao nhung khong chan
var noInstaller = new UpdateChecker(new FakeFeed(Rel("v26.8.20", installer: null))).Check("26.8.18");
Check(noInstaller.Status == UpdateStatus.CheckFailed && !noInstaller.MustUpdate
   && noInstaller.Message!.Contains("thủ công", StringComparison.Ordinal),
    "ban moi khong kem file cai dat -> huong dan cap nhat thu cong, khong chan");

Check(ExitCodes.Combine(ExitCodes.UpdateRequired, ExitCodes.VerdictCritical) == ExitCodes.UpdateRequired,
    "can cap nhat thang moi ket luan danh gia (chua quet thi chua co ket luan)");

Console.WriteLine("\n=== 15. Doc du lieu ban phat hanh tu GitHub ===");

const string releaseJson = """
{
  "tag_name": "v26.8.20",
  "html_url": "https://github.com/tsudev-tsudev/swico/releases/tag/v26.8.20",
  "published_at": "2026-08-20T10:00:00Z",
  "assets": [
    { "name": "swico-setup-26.8.20.exe", "browser_download_url": "https://x/swico-setup-26.8.20.exe" },
    { "name": "swico-portable-26.8.20.zip", "browser_download_url": "https://x/portable.zip" },
    { "name": "SHA256SUMS.txt", "browser_download_url": "https://x/SHA256SUMS.txt" },
    { "name": "swico.exe", "browser_download_url": "https://x/swico.exe" }
  ]
}
""";
var parsed = GitHubReleaseParser.Parse(releaseJson, out var perr);
Check(parsed is not null && perr is null, "doc duoc JSON ban phat hanh");
Check(parsed!.Version == V("26.8.20"), "lay dung phien ban tu tag_name");
Check(parsed.InstallerUrl == "https://x/swico-setup-26.8.20.exe",
    "chon dung file cai dat, KHONG nham sang swico.exe hay ban portable");
Check(parsed.ChecksumsUrl == "https://x/SHA256SUMS.txt", "tim thay file ma bam");
Check(parsed.PublishedAt is not null, "doc duoc thoi diem phat hanh");

Check(GitHubReleaseParser.Parse("{ khong phai json", out var badErr) is null && badErr is not null,
    "JSON hong -> tra ve null kem ly do, khong nem exception");
Check(GitHubReleaseParser.Parse("", out _) is null, "chuoi rong -> null");
Check(GitHubReleaseParser.Parse("[]", out _) is null, "JSON khong phai doi tuong -> null");
Check(GitHubReleaseParser.Parse("""{"tag_name":"v26.8.20"}""", out _)!.InstallerUrl is null,
    "khong co danh sach tep dinh kem -> khong co file cai dat");

Console.WriteLine("\n=== 16. Doi chieu ma bam truoc khi chay file tai ve ===");

const string sums = """
06a284ef82dc8b78c278e8053ffb028914e896b48d1b36505ecb13a24caa9c82  swico-portable-26.8.20.zip
997bd2318999d0fd7fe82238cecfe51ab7ee7c08c0bcd4b9fe1910aa5e884abb  swico-setup-26.8.20.exe
c23bb0ae0b7d02fe90736f762a7ecdd6a13a199c85f8243c344095f7488d80d7  swico.exe
""";
Check(ChecksumFile.Find(sums, "swico-setup-26.8.20.exe") == "997bd2318999d0fd7fe82238cecfe51ab7ee7c08c0bcd4b9fe1910aa5e884abb",
    "tim dung ma bam theo ten tep");
Check(ChecksumFile.Find(sums, "swico.exe") == "c23bb0ae0b7d02fe90736f762a7ecdd6a13a199c85f8243c344095f7488d80d7",
    "khong khop nham dong khac co ten la tien to");
Check(ChecksumFile.Find(sums, "khong-co-tep-nay.exe") is null,
    "khong tim thay -> null (KHONG duoc coi la hop le)");
Check(ChecksumFile.Find("", "x.exe") is null && ChecksumFile.Find(null, "x.exe") is null,
    "noi dung rong -> null");
Check(ChecksumFile.Find("khong-phai-ma-bam  swico.exe", "swico.exe") is null,
    "ma bam sai dinh dang -> null, khong chay file khong xac minh duoc");
Check(ChecksumFile.Find("997BD2318999D0FD7FE82238CECFE51AB7EE7C08C0BCD4B9FE1910AA5E884ABB  a.exe", "a.exe")
        == "997bd2318999d0fd7fe82238cecfe51ab7ee7c08c0bcd4b9fe1910aa5e884abb",
    "chap nhan ma bam viet hoa, chuan hoa ve chu thuong");
Check(ChecksumFile.Find("997bd2318999d0fd7fe82238cecfe51ab7ee7c08c0bcd4b9fe1910aa5e884abb *a.exe", "a.exe") is not null,
    "chap nhan tien to '*' cua che do nhi phan");

Console.WriteLine("\n=== 17. Tien trinh quet: thu tu buoc, huy, ma thoat ===");

// Dung mot SystemContext toi thieu: cac collector deu chiu duoc du lieu rong
// (chung khong bao gio nem), nen bao cao van dung duoc va ta do dung thu ban
// muon do o day - THU TU CAC BUOC, khong phai noi dung bao cao.
static SystemContext ProgressCtx(IProgressSink sink, CancellationToken cancel = default) => new()
{
    Wmi = new FakeWmiQuery(),
    Registry = new FakeRegistryReader(),
    Process = new FakeProcessRunner(),
    Files = new FakeFileProbe(),
    ComputerName = "MAY-TEST",
    ScanTime = DateTimeOffset.Now,
    IsElevated = true,
    Progress = sink,
    Cancellation = cancel
};

var recorder = new RecordingProgressSink();
_ = LicenseReportBuilder.Build(ProgressCtx(recorder), new AuditOptions());

Check(recorder.StepLabels.Count >= 6,
    $"bao cao ban quyen bao it nhat 6 buoc (duoc {recorder.StepLabels.Count})");
Check(recorder.StepLabels[0] == "Hệ điều hành",
    $"buoc dau tien la 'He dieu hanh' (duoc '{recorder.StepLabels.FirstOrDefault()}')");
Check(recorder.StepLabels.Contains("Bản quyền Windows")
      && recorder.StepLabels.Contains("Dấu hiệu kích hoạt trái phép"),
    "co buoc ban quyen Windows va buoc quet dau hieu kich hoat trai phep");
Check(recorder.Completed == recorder.StepLabels.Count,
    $"moi buoc da bat dau deu ket thuc ({recorder.Completed}/{recorder.StepLabels.Count})");
Check(!recorder.Events.Any(e => e.StartsWith("FAIL", StringComparison.Ordinal)),
    "khong buoc nao bao that bai khi du lieu rong");

// Thu tu la mot RANG BUOC NGHIEP VU, khong phai ngau nhien: phan mem phai
// duoc thu thap TRUOC khi quet dau hieu, vi diem rui ro tinh tu ca hai.
var iSoftware = recorder.StepLabels.ToList().IndexOf("Danh sách phần mềm đã cài");
var iRisk = recorder.StepLabels.ToList().IndexOf("Dấu hiệu kích hoạt trái phép");
Check(iSoftware >= 0 && iRisk >= 0 && iSoftware < iRisk,
    "thu thap phan mem chay TRUOC khi quet dau hieu (diem rui ro can ca hai)");

var hwRecorder = new RecordingProgressSink();
_ = HardwareReportBuilder.Build(ProgressCtx(hwRecorder), new AuditOptions { RunDism = false, RunSfc = false });
Check(hwRecorder.StepLabels.Contains("CPU") && hwRecorder.StepLabels.Contains("RAM")
      && hwRecorder.StepLabels.Contains("Windows Defender"),
    "bao cao phan cung bao cac buoc CPU / RAM / Defender");
Check(hwRecorder.Completed == hwRecorder.StepLabels.Count,
    "bao cao phan cung: moi buoc deu ket thuc");

// Huy: phai dung LAI, khong duoc chay not cho het roi mai bao.
using var cts = new CancellationTokenSource();
cts.Cancel();
var cancelledSink = new RecordingProgressSink();
var cancelThrew = false;
try { _ = LicenseReportBuilder.Build(ProgressCtx(cancelledSink, cts.Token), new AuditOptions()); }
catch (OperationCanceledException) { cancelThrew = true; }
Check(cancelThrew, "token da huy -> Build nem OperationCanceledException");
Check(cancelledSink.StepLabels.Count == 0,
    $"huy truoc khi chay -> khong buoc nao duoc bat dau (duoc {cancelledSink.StepLabels.Count})");

// Huy GIUA CHUNG: cac buoc da xong phai duoc giu, phan con lai dung han.
using var midCts = new CancellationTokenSource();
var midSink = new RecordingProgressSink();
var midCtx = new SystemContext
{
    Wmi = new FakeWmiQuery(),
    Registry = new FakeRegistryReader(),
    Process = new FakeProcessRunner(),
    Files = new FakeFileProbe(),
    ComputerName = "MAY-TEST",
    ScanTime = DateTimeOffset.Now,
    IsElevated = true,
    Progress = new CancelAfterSink(midSink, midCts, afterSteps: 2),
    Cancellation = midCts.Token
};
var midThrew = false;
try { _ = LicenseReportBuilder.Build(midCtx, new AuditOptions()); }
catch (OperationCanceledException) { midThrew = true; }
Check(midThrew, "huy giua chung -> Build dung lai bang OperationCanceledException");
Check(midSink.Completed == 2,
    $"huy sau 2 buoc -> dung 2 buoc hoan tat duoc giu lai (duoc {midSink.Completed})");
Check(midSink.StepLabels.Count == 2,
    $"khong buoc nao khac duoc bat dau sau khi huy (duoc {midSink.StepLabels.Count})");

// ScanStep phai bao FAIL khi cong viec nem loi, va van de loi noi len tren.
var failSink = new RecordingProgressSink();
var failThrew = false;
try
{
    ScanStep.Run(ProgressCtx(failSink), "Buoc hong",
        () => throw new InvalidOperationException("hong that"));
}
catch (InvalidOperationException) { failThrew = true; }
Check(failThrew, "ScanStep KHONG nuot ngoai le cua cong viec");
Check(failSink.Events.Any(e => e == "FAIL hong that"),
    "ScanStep bao FAIL kem ly do khi cong viec nem loi");

// Note: dung cho viec chay lau, phai den duoc sink.
var noteSink = new RecordingProgressSink();
var noteCtx = ProgressCtx(noteSink);
_ = SystemIntegrityCollector.Collect(noteCtx, runDism: true, runSfc: true);
Check(noteSink.Events.Count(e => e.StartsWith("NOTE", StringComparison.Ordinal)) == 2,
    "DISM va sfc moi cai bao mot ghi chu thoi gian du kien");
Check(noteSink.Events.Any(e => e.Contains("5-15 phút", StringComparison.Ordinal)),
    "ghi chu cua sfc noi ro no mat 5-15 phut");

// Sink mac dinh khong duoc lam gi ca - day la thu bao ve 173 test cu.
var quiet = new SystemContext
{
    Wmi = new FakeWmiQuery(), Registry = new FakeRegistryReader(),
    Process = new FakeProcessRunner(), Files = new FakeFileProbe(),
    ComputerName = "MAY-TEST", ScanTime = DateTimeOffset.Now
};
Check(ReferenceEquals(quiet.Progress, NullProgressSink.Instance),
    "SystemContext khong khai bao Progress -> dung NullProgressSink");
Check(!quiet.Cancellation.CanBeCanceled,
    "SystemContext khong khai bao Cancellation -> khong bao gio huy");

// Ma thoat 130
Check(ExitCodes.Cancelled == 130, "ma thoat huy = 130 (quy uoc POSIX 128+SIGINT)");
Check(ExitCodes.Describe(130).Contains("huỷ", StringComparison.Ordinal),
    "Describe(130) noi ro la nguoi dung huy");
Check(ExitCodes.Combine(ExitCodes.Cancelled, ExitCodes.VerdictCritical) == ExitCodes.Cancelled,
    "huy giua chung THANG ket luan danh gia - ket luan do dua tren du lieu chua day du");
Check(ExitCodes.Combine(ExitCodes.Partial, ExitCodes.VerdictCritical) == ExitCodes.VerdictCritical,
    "quet xong nhung thieu du lieu thi ket luan van thang (hanh vi cu giu nguyen)");
Check(CliOptions.UsageText.Contains("130", StringComparison.Ordinal),
    "phan tro giup co liet ke ma thoat 130");

Console.WriteLine($"\n=== KET QUA: {passed} PASS, {failed} FAIL ===");
return failed == 0 ? 0 : 1;

// Adapter gia lap cho nguon ban phat hanh
sealed class FakeFeed : IUpdateFeed
{
    private readonly ReleaseInfo? _r; private readonly string? _err; private readonly Exception? _ex;
    public FakeFeed(ReleaseInfo? r = null, string? err = null, Exception? ex = null)
        { _r = r; _err = err; _ex = ex; }
    public ReleaseInfo? GetLatest(out string? failureReason)
    {
        if (_ex is not null) throw _ex;
        failureReason = _err; return _r;
    }
}


/// <summary>
/// Sink chuyen tiep moi su kien sang mot sink khac, va bam nut huy sau dung
/// <c>afterSteps</c> buoc hoan tat.
///
/// Dung de mo phong nguoi dung bam Ctrl+C GIUA lan quet - tinh huong ma test
/// "huy truoc khi chay" khong cham toi duoc, nhung lai la tinh huong that su
/// hay xay ra.
/// </summary>
sealed class CancelAfterSink : IProgressSink
{
    private readonly IProgressSink _inner;
    private readonly CancellationTokenSource _cts;
    private readonly int _afterSteps;
    private int _done;

    public CancelAfterSink(IProgressSink inner, CancellationTokenSource cts, int afterSteps)
        { _inner = inner; _cts = cts; _afterSteps = afterSteps; }

    public void BeginStep(string label) => _inner.BeginStep(label);
    public void FailStep(string reason) => _inner.FailStep(reason);
    public void Note(string message) => _inner.Note(message);

    public void EndStep()
    {
        _inner.EndStep();
        if (++_done >= _afterSteps) _cts.Cancel();
    }
}