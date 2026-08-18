using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Collectors;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;
using Tsudev.Audit.Core.Rendering;
using Tsudev.Audit.Core.Rules;
using Tsudev.Audit.Core.Testing;

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
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), mixed.Overall).Level == VerdictLevel.Bad,
    "ket luan cuoi cung la Bad khi co SKU chua kich hoat");

var allOk = WindowsLicenseCollector.Collect(LicenseCtx((1, "AAAAA"), (1, "BBBBB"))).Summary;
Check(allOk.IsGenuine && allOk.Overall == LicenseHealth.Ok, "moi SKU Licensed -> hop le");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), allOk.Overall).Level == VerdictLevel.Ok,
    "ket luan Ok khi moi SKU deu hop le");

// 2 = OOB Grace: chua sai nhung cung CHUA hop le -> canh bao, khong phai Ok
var graceOnly = WindowsLicenseCollector.Collect(LicenseCtx((1, "AAAAA"), (2, "BBBBB"))).Summary;
Check(!graceOnly.IsGenuine && graceOnly.Overall == LicenseHealth.Grace,
    "con han dung thu -> KHONG phai hop le");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), graceOnly.Overall).Level == VerdictLevel.Warning,
    "con han dung thu -> canh bao, khong phai Ok cung khong phai Bad");

var none = WindowsLicenseCollector.Collect(LicenseCtx()).Summary;
Check(!none.IsKnown && none.Overall == LicenseHealth.Unknown, "khong doc duoc SKU nao -> Unknown");
Check(RiskScoring.BuildVerdict(Array.Empty<RiskFinding>(), none.Overall).Level == VerdictLevel.Unknown,
    "khong co du lieu -> chua du co so ket luan, KHONG mac dinh la hop le");

// Dau hieu crack muc cao van thang moi trang thai license
Check(RiskScoring.BuildVerdict(
        new[] { new RiskFinding { Category = "x", Detection = "y", Level = RiskLevel.Critical } },
        LicenseHealth.Ok).Level == VerdictLevel.Bad,
    "phat hien muc Rat cao -> Bad du license bao hop le");

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

Console.WriteLine($"\n=== KET QUA: {passed} PASS, {failed} FAIL ===");
return failed == 0 ? 0 : 1;
