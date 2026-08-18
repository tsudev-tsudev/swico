using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Rendering;
using Tsudev.Audit.Core.Reports;
using Tsudev.Audit.Windows;

namespace Tsudev.Audit.Cli;

/// <summary>
/// Diem vao DUY NHAT - thay the ca hai file Run-License-Audit.bat va
/// Run-Hardware-Inventory.bat bang mot chuong trinh voi tham so --scope.
/// </summary>
public static class Program
{
    /// <summary>
    /// Ma thoat chuan hoa cho tich hop RMM/giam sat:
    ///   0 = thanh cong hoan toan
    ///   1 = thanh cong nhung co it nhat 1 tinh nang phu bi loi (bao cao chinh van day du)
    ///   2 = loi nghiem trong, khong tao duoc bao cao
    ///   3 = tham so dong lenh khong hop le
    /// </summary>
    public const int ExitOk = 0, ExitPartial = 1, ExitFatal = 2, ExitBadArgs = 3;

    [SupportedOSPlatform("windows")]
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

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

        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Công cụ này chỉ chạy trên Windows.");
            return ExitFatal;
        }

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
        var auditOptions = new AuditOptions { RunDism = opts.RunDism, RunSfc = opts.RunSfc };

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

        Info($"\nMã thoát: {exitCode}");
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
            var jsonOpts = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var jsonName = Path.ChangeExtension(report.HtmlReportFile, ".json");
            File.WriteAllText(Path.Combine(dir, jsonName),
                JsonSerializer.Serialize(report, jsonOpts), utf8NoBom);
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

    private static void TryOpen(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* khong mo duoc trinh duyet KHONG phai loi nghiem trong */ }
    }

    private static void Info(string msg) => Console.WriteLine(msg);
    private static void Warn(string msg)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = prev;
    }
}

public enum AuditScope { All, License, Hardware }

public sealed class CliOptions
{
    public AuditScope Scope { get; private set; } = AuditScope.All;
    public string? OutputRoot { get; private set; }
    public bool Silent { get; private set; }
    public bool RunDism { get; private set; } = true;
    public bool RunSfc { get; private set; }
    public bool ExportCsv { get; private set; } = true;
    public bool Verbose { get; private set; }
    public bool ShowHelp { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i].ToLowerInvariant();
            switch (a)
            {
                case "-h" or "--help" or "/?": o.ShowHelp = true; break;
                case "--silent" or "-s": o.Silent = true; break;
                case "--sfc": o.RunSfc = true; break;
                case "--no-dism": o.RunDism = false; break;
                case "--no-csv": o.ExportCsv = false; break;
                case "--verbose" or "-v": o.Verbose = true; break;

                case "--scope":
                    if (++i >= args.Length) throw new ArgumentException("--scope cần một giá trị (all|license|hardware).");
                    o.Scope = args[i].ToLowerInvariant() switch
                    {
                        "all" => AuditScope.All,
                        "license" => AuditScope.License,
                        "hardware" => AuditScope.Hardware,
                        _ => throw new ArgumentException($"--scope không hợp lệ: '{args[i]}' (chỉ nhận all|license|hardware).")
                    };
                    break;

                case "--output" or "-o":
                    if (++i >= args.Length) throw new ArgumentException("--output cần một đường dẫn thư mục.");
                    o.OutputRoot = args[i];
                    break;

                default:
                    throw new ArgumentException($"Không nhận ra tham số '{args[i]}'.");
            }
        }
        return o;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
tsudev System Audit - Kiểm tra bản quyền Windows & cấu hình phần cứng

CÁCH DÙNG:
  tsudev-audit.exe [tham số]

THAM SỐ:
  --scope <all|license|hardware>  Phạm vi quét (mặc định: all - quét cả hai)
  -o, --output <thư mục>          Thư mục gốc lưu kết quả (mặc định: cạnh file exe)
  -s, --silent                    Không tự mở trình duyệt (dùng cho GPO/RMM/Task Scheduler)
      --sfc                       Chạy thêm sfc /verifyonly (CHẬM 5-15 phút, mặc định tắt)
      --no-dism                   Bỏ qua DISM CheckHealth
      --no-csv                    Không xuất file .csv
  -v, --verbose                   Hiện chi tiết lỗi
  -h, --help                      Hiện trợ giúp này

MÃ THOÁT:
  0  Thành công hoàn toàn
  1  Thành công nhưng có mục thiếu dữ liệu (báo cáo chính vẫn đầy đủ)
  2  Lỗi nghiêm trọng, không tạo được báo cáo
  3  Tham số dòng lệnh không hợp lệ

VÍ DỤ:
  tsudev-audit.exe                             Quét đầy đủ, mở báo cáo khi xong
  tsudev-audit.exe --scope license             Chỉ kiểm tra bản quyền
  tsudev-audit.exe --silent --sfc              Quét sâu, không mở trình duyệt (triển khai hàng loạt)
  tsudev-audit.exe -o D:\BaoCao --silent       Lưu kết quả vào thư mục chỉ định
""");
    }
}
