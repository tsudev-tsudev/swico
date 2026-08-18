using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsudev.Audit.Core.Cli;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Rendering;
using Tsudev.Audit.Core.Reports;
using Tsudev.Audit.Core.Rules;
using Tsudev.Audit.Core.Updates;
using Tsudev.Audit.Windows;

namespace Tsudev.Audit.Cli;

/// <summary>
/// Diem vao DUY NHAT - thay the ca hai file Run-License-Audit.bat va
/// Run-Hardware-Inventory.bat bang mot chuong trinh voi tham so --scope.
/// </summary>
public static class Program
{
    // Ma thoat nay o Tsudev.Audit.Core.Cli.ExitCodes de kiem thu duoc tren
    // moi nen tang. Cac ten duoi day chi la but danh cho de doc.
    private const int ExitOk = ExitCodes.Ok;
    private const int ExitPartial = ExitCodes.Partial;
    private const int ExitFatal = ExitCodes.Fatal;
    private const int ExitBadArgs = ExitCodes.BadArgs;

    /// <summary>
    /// Dung lai mot the hien duy nhat: JsonSerializerOptions xay bo dem noi tai
    /// khi dung lan dau, nen tao moi moi lan goi la vut bo bo dem do.
    /// </summary>
    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [SupportedOSPlatform("windows")]
    public static int Main(string[] args)
    {
        ConfigureConsole();

        CliOptions opts;
        try
        {
            opts = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Tham số không hợp lệ: {ex.Message}\n");
            CliOptions.PrintUsage();
            return ExitBadArgs;
        }

        if (opts.ShowHelp) { CliOptions.PrintUsage(); return ExitOk; }

        if (opts.ShowVersion) { PrintVersion(); return ExitOk; }

        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Công cụ này chỉ chạy trên Windows.");
            return ExitFatal;
        }

        // Kiem tra cap nhat TRUOC khi quet. Dat o day, ngoai Run(), de ro rang
        // day la mot cong doan rieng va de doc lai khi ra soat bao mat.
        var gate = CheckForUpdate(opts);
        if (gate is not null) return gate.Value;

        try
        {
            return Run(opts);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LỖI NGHIÊM TRỌNG: {ex.Message}");
            if (opts.Verbose) Console.Error.WriteLine(ex.ToString());
            return ExitFatal;
        }
    }

    /// <summary>
    /// Cong kiem tra phien ban. Tra ve ma thoat khi phai dung lai, null khi
    /// duoc phep quet tiep.
    ///
    /// BA TINH HUONG, BA CACH XU LY KHAC NHAU:
    ///
    ///  1. Co ban moi + dang chay tuong tac -> hien hop thoai mot nut, cap nhat
    ///     xong roi thoat. Nguoi dung se chay lai ban moi.
    ///  2. Co ban moi + dang chay --silent  -> KHONG hien hop thoai (se treo
    ///     cung tien trinh trien khai tu dong), thoat voi ma 30.
    ///  3. Khong kiem tra duoc            -> QUET TIEP nhu binh thuong.
    ///     Cong cu nay duoc dung dung o nhung noi mang bi han che nhat; chan
    ///     lai se lam no vo dung chinh o do.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static int? CheckForUpdate(CliOptions opts)
    {
        if (opts.NoUpdateCheck)
        {
            Info("Đã bỏ qua kiểm tra phiên bản mới (--no-update-check).");
            return null;
        }

        var current = CurrentVersion();
        Info("Đang kiểm tra phiên bản mới...");

        var result = new UpdateChecker(new GitHubUpdateFeed(current)).Check(current);

        switch (result.Status)
        {
            case UpdateStatus.UpToDate:
                Info($"  {result.Message}");
                return null;

            case UpdateStatus.CheckFailed:
                Warn($"  Không kiểm tra được bản cập nhật: {result.Message}");
                Warn("  Vẫn tiếp tục quét. Hãy tự đối chiếu phiên bản mới nhất khi có mạng.");
                return null;

            case UpdateStatus.UpdateRequired:
                return HandleUpdateRequired(opts, result);

            default:
                return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static int HandleUpdateRequired(CliOptions opts, UpdateCheckResult result)
    {
        Warn($"\n=== {result.Message} ===");

        if (opts.Silent)
        {
            // Khong bao gio hien hop thoai o che do tu dong: khong ai ngoi do
            // bam nut, va tien trinh trien khai se treo vo thoi han.
            Warn($"Chế độ --silent: không hiện hộp thoại. Cập nhật rồi chạy lại.");
            Warn($"Tải bản mới tại: {result.Latest?.PageUrl}");
            Info($"\nMã thoát: {ExitCodes.UpdateRequired} ({ExitCodes.Describe(ExitCodes.UpdateRequired)})");
            return ExitCodes.UpdateRequired;
        }

        if (!UpdatePrompt.AskToUpdate(result))
        {
            Warn("Bạn đã đóng hộp thoại mà không cập nhật. Không thể tiếp tục quét.");
            Warn($"Tải bản mới tại: {result.Latest?.PageUrl}");
            return ExitCodes.UpdateRequired;
        }

        var installer = UpdateInstaller.DownloadAndVerify(result.Latest!, Info, out var failure);
        if (installer is null)
        {
            Warn($"Cập nhật KHÔNG thành công: {failure}");
            Warn($"Vui lòng tải và cài thủ công tại: {result.Latest?.PageUrl}");
            return ExitCodes.UpdateRequired;
        }

        if (!UpdateInstaller.Launch(installer, out var launchError))
        {
            Warn($"Không chạy được file cài đặt: {launchError}");
            Warn($"File đã tải và đã xác minh nằm tại: {installer}");
            return ExitCodes.UpdateRequired;
        }

        Info("\nĐã khởi động trình cài đặt. Sau khi cài xong, hãy chạy lại công cụ.");
        return ExitCodes.UpdateRequired;
    }

    /// <summary>Phien ban hien tai, da cat phan bam commit do SDK them vao.</summary>
    private static string CurrentVersion()
    {
        var asm = typeof(Program).Assembly;
        var v = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? asm.GetName().Version?.ToString() ?? "";
        var plus = v.IndexOf('+', StringComparison.Ordinal);
        return plus > 0 ? v[..plus] : v;
    }

    [SupportedOSPlatform("windows")]
    private static int Run(CliOptions opts)
    {
        var exitCode = ExitOk;
        var scanTime = DateTimeOffset.Now;

        var root = opts.OutputRoot ?? AppContext.BaseDirectory;
        var computerName = Environment.MachineName;

        // Cau truc 3 cap: <root>/tsudev-bao-cao-ra-quet-<May>-<Ngay>/tsudev-ket-qua-ra-quet-<May>-<Ngay>/
        var level2 = FileNaming.Level2(root, computerName, scanTime);
        var level3 = FileNaming.Level3(level2, computerName, scanTime);
        Directory.CreateDirectory(level3);

        Info($"=== tsudev System Audit ===");
        Info($"Máy: {computerName}   |   Thời gian: {scanTime.LocalDateTime:dd/MM/yyyy HH:mm:ss}");
        Info($"Thư mục kết quả: {level3}");

        if (!WindowsEnvironment.IsElevated())
            Warn("CẢNH BÁO: đang chạy KHÔNG có quyền Administrator - một số mục sẽ thiếu dữ liệu.");

        var renderer = new HtmlReportRenderer();
        var xlsx = new XlsxWriter();

        // Uu tien: --rules -> file canh exe -> bo luat dong kem.
        // Nho vay cap nhat luat chi can tha them mot file JSON canh exe, khong
        // phai bien dich lai va ky lai.
        var rulesPath = opts.RulesPath ?? ProbeConventionalRulesFile();
        var rules = DetectionRuleSet.LoadOrEmbedded(rulesPath, out var rulesWarning);
        if (rulesWarning is not null) Warn($"CẢNH BÁO: {rulesWarning}");
        Info($"Bộ luật phát hiện: {rules.Describe()}");

        var auditOptions = new AuditOptions
        {
            RunDism = opts.RunDism,
            RunSfc = opts.RunSfc,
            Rules = rules
        };

        var produced = new List<AuditReport>();

        if (opts.Scope is AuditScope.License or AuditScope.All)
        {
            Info("\n[1] Đang kiểm tra bản quyền Windows & phần mềm...");
            var ctx = WindowsEnvironment.CreateContext(scanTime);
            var report = LicenseReportBuilder.Build(ctx, auditOptions);
            if (!Save(report, level3, renderer, xlsx, opts)) exitCode = ExitPartial;
            if (report.Warnings.Count > 0) exitCode = ExitPartial;
            produced.Add(report);
        }

        if (opts.Scope is AuditScope.Hardware or AuditScope.All)
        {
            Info("\n[2] Đang thu thập cấu hình phần cứng...");
            var ctx = WindowsEnvironment.CreateContext(scanTime.AddSeconds(1));
            var report = HardwareReportBuilder.Build(ctx, auditOptions);
            if (!Save(report, level3, renderer, xlsx, opts)) exitCode = ExitPartial;
            if (report.Warnings.Count > 0) exitCode = ExitPartial;
            produced.Add(report);
        }

        // Trang tong hop - doc SAU vao moi thu muc con cap 3 (ke ca thu muc
        // ket qua copy tu may khac vao)
        Info("\n[3] Đang cập nhật tsudev-tong-hop.html...");
        string? dashboardPath = null;
        try
        {
            dashboardPath = new DashboardBuilder(renderer).BuildAndSave(level2);
            Info($"    → {dashboardPath}");
        }
        catch (Exception ex)
        {
            Warn($"Không tạo được trang tổng hợp: {ex.Message}");
            exitCode = ExitPartial;
        }

        Info("\n=== HOÀN TẤT ===");
        foreach (var r in produced)
        {
            Info($"  {r.Title}");
            Info($"    {Path.Combine(level3, r.HtmlReportFile)}");
            if (r.Warnings.Count > 0)
                foreach (var w in r.Warnings) Warn($"    ! {w}");
        }

        if (!opts.Silent && dashboardPath is not null)
            TryOpen(dashboardPath);
        else if (opts.Silent)
            Info("[Chế độ --silent] Bỏ qua tự động mở trình duyệt.");

        // Ket luan danh gia thang "thieu du lieu": mot may co dau hieu kich hoat
        // trai phep can duoc bao dong hon la mot muc thu thap bi trong.
        var verdictCode = opts.NoVerdictExit
            ? ExitCodes.Ok
            : ExitCodes.FromVerdicts(produced.Select(r => r.VerdictLevel));
        exitCode = ExitCodes.Combine(exitCode, verdictCode);

        Info($"\nMã thoát: {exitCode} ({ExitCodes.Describe(exitCode)})");
        return exitCode;
    }

    private static bool Save(AuditReport report, string dir, HtmlReportRenderer renderer,
        XlsxWriter xlsx, CliOptions opts)
    {
        bool allOk = true;
        var utf8NoBom = new UTF8Encoding(false);

        try
        {
            var html = renderer.Render(report, dashboardRelativeLink: "../tsudev-tong-hop.html");
            File.WriteAllText(Path.Combine(dir, report.HtmlReportFile), html, utf8NoBom);
        }
        catch (Exception ex) { Warn($"Không ghi được HTML: {ex.Message}"); allOk = false; }

        try
        {
            var jsonName = Path.ChangeExtension(report.HtmlReportFile, ".json");
            File.WriteAllText(Path.Combine(dir, jsonName),
                JsonSerializer.Serialize(report, ReportJsonOptions), utf8NoBom);
        }
        catch (Exception ex) { Warn($"Không ghi được JSON: {ex.Message}"); allOk = false; }

        try
        {
            var tables = report.Sections.SelectMany(s => s.Tables)
                .Select((t, i) => { if (string.IsNullOrWhiteSpace(t.Title)) t.Title = $"Bảng {i + 1}"; return t; })
                .ToList();
            xlsx.Write(Path.Combine(dir, Path.ChangeExtension(report.HtmlReportFile, ".xlsx")), tables);
        }
        catch (Exception ex) { Warn($"Không ghi được XLSX: {ex.Message}"); allOk = false; }

        if (opts.ExportCsv)
        {
            try
            {
                foreach (var (table, idx) in report.Sections.SelectMany(s => s.Tables).Select((t, i) => (t, i)))
                {
                    if (table.Rows.Count == 0) continue;
                    var csvName = Path.ChangeExtension(report.HtmlReportFile, null) + $"_{idx + 1}.csv";
                    WriteCsv(Path.Combine(dir, csvName), table);
                }
            }
            catch (Exception ex) { Warn($"Không ghi được CSV: {ex.Message}"); allOk = false; }
        }

        return allOk;
    }

    /// <summary>Ghi CSV voi BOM de Excel mo truc tiep hien dung tieng Viet co dau.</summary>
    private static void WriteCsv(string path, DataTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", table.Columns.Select(Escape)));
        foreach (var row in table.Rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

        static string Escape(string v)
            => v.Contains(',') || v.Contains('"') || v.Contains('\n')
                ? $"\"{v.Replace("\"", "\"\"")}\""
                : v;
    }

    /// <summary>
    /// Tim file luat dat canh file exe theo quy uoc. Tra ve null neu khong co -
    /// khi do dung bo luat dong kem.
    /// </summary>
    private static string? ProbeConventionalRulesFile()
    {
        try
        {
            var candidate = Path.Combine(AppContext.BaseDirectory, DetectionRuleSet.ConventionalFileName);
            return File.Exists(candidate) ? candidate : null;
        }
        catch (IOException) { return null; }
    }

    private static void PrintVersion()
    {
        var asm = typeof(Program).Assembly;
        var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString()
                      ?? "khong xac dinh";

        // Cat phan bam commit ma SDK tu them (vi du "3.0.0+9e1f2c...").
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        if (plus > 0) version = version[..plus];

        Console.WriteLine($"tsudev SWICO {version}");
        Console.WriteLine($"Lược đồ dữ liệu: {AuditReport.CurrentSchemaVersion}");
        Console.WriteLine($"Bộ luật phát hiện đóng kèm: {DetectionRuleSet.Embedded.Version}");
    }

    private static void TryOpen(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* khong mo duoc trinh duyet KHONG phai loi nghiem trong */ }
    }

    /// <summary>
    /// Chuan bi console MA KHONG lam hong phien lam viec cua nguoi dung.
    ///
    /// Gan Console.OutputEncoding se goi SetConsoleOutputCP xuong Windows, va
    /// conhost/Windows Terminal DUNG LAI screen buffer khi ma trang doi - lam
    /// toan bo lich su cuon truoc do bi mat mau, trong nhu bi xoa man hinh.
    /// Nguoi dung da bao cao dung hien tuong nay.
    ///
    /// Nen chi gan KHI THAT SU CAN. Windows Terminal, PowerShell 7 va phan lon
    /// moi truong hien dai da o UTF-8 san, nen nhanh nay thuong khong chay.
    /// </summary>
    private static void ConfigureConsole()
    {
        try
        {
            if (Console.IsOutputRedirected) return;                 // ghi ra file/ong dan: khong dong toi console
            if (Console.OutputEncoding.CodePage == Encoding.UTF8.CodePage) return;  // da dung roi

            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException) { /* console khong cho doi ma trang - khong phai loi */ }
        catch (System.Security.SecurityException) { /* bi chinh sach chan */ }
    }

    private static void Info(string msg) => Console.WriteLine(msg);

    private static void Warn(string msg)
    {
        // Khi dau ra bi chuyen huong (file log, ong dan), KHONG dat mau: ma mau
        // se lot vao file lam ban noi dung.
        if (Console.IsOutputRedirected) { Console.WriteLine(msg); return; }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
        }
        finally
        {
            // try/finally de mot ngoai le khi ghi KHONG de lai console mac ket
            // o mau vang sau khi chuong trinh ket thuc.
            Console.ForegroundColor = prev;
        }
    }
}
