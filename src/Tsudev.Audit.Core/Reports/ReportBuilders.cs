using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Collectors;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Rules;

namespace Tsudev.Audit.Core.Reports;

/// <summary>Tuy chon dieu khien do sau cua lan quet.</summary>
public sealed class AuditOptions
{
    /// <summary>Chay DISM CheckHealth (nhanh, ~5 giay).</summary>
    public bool RunDism { get; set; } = true;

    /// <summary>Chay sfc /verifyonly (CHAM, 5-15 phut) - mac dinh TAT.</summary>
    public bool RunSfc { get; set; }

    /// <summary>Duong dan file CSV whitelist phan mem da duoc duyet.</summary>
    public string? WhitelistCsvPath { get; set; }

    /// <summary>
    /// Bo luat phat hien. De null = dung bo luat dong kem trong exe.
    /// Cho phep cap nhat luat ma khong phai bien dich va ky lai ban exe.
    /// </summary>
    public DetectionRuleSet? Rules { get; set; }
}

/// <summary>Dung bao cao kiem tra ban quyen Windows + phan mem.</summary>
public static class LicenseReportBuilder
{
    public static AuditReport Build(SystemContext ctx, AuditOptions options)
    {
        var report = new AuditReport
        {
            ReportKind = ReportKind.LicenseAudit,
            ComputerName = ctx.ComputerName,
            ScanTime = ctx.ScanTime,
            Title = "Báo cáo kiểm tra bản quyền Windows & phần mềm đã cài đặt",
            HtmlReportFile = FileNaming.Html("Windows_License_Audit", ctx)
        };

        if (!ctx.IsElevated)
            ctx.Warn("Đang chạy KHÔNG có quyền Administrator - một số mục (bản quyền, Defender, DISM) có thể thiếu dữ liệu.");

        // --- 1. He dieu hanh ---
        report.Sections.Add(new ReportSection
        {
            Id = "sec-os", NavLabel = "1. Hệ điều hành", Heading = "1. Thông tin hệ điều hành",
            Tables = { OsInfoCollector.Collect(ctx) }
        });

        // --- 2. Ban quyen Windows ---
        var (licTable, licensedCount) = WindowsLicenseCollector.Collect(ctx);
        report.Sections.Add(new ReportSection
        {
            Id = "sec-license", NavLabel = "2. Bản quyền Windows",
            Heading = "2. Trạng thái bản quyền Windows (Activation License)",
            Tables = { licTable },
            PreformattedText = SlmgrCollector.Collect(ctx)
        });

        // --- 2b. Office / M365 ---
        report.Sections.Add(new ReportSection
        {
            Id = "sec-office", NavLabel = "2b. Office/M365",
            Heading = "2b. Bản quyền Microsoft Office / Microsoft 365",
            Description = "Kiểm tra qua công cụ chính thức <code>ospp.vbs</code> đi kèm bộ cài Office. " +
                          "Nếu máy không cài Office, mục này sẽ trống - đây KHÔNG phải lỗi.",
            Tables = { OfficeLicenseCollector.Collect(ctx) }
        });

        // --- 4. Phan mem (thu thap truoc de tinh diem rui ro o muc 3) ---
        var software = SoftwareCollector.Collect(ctx);
        var manualReview = software.Where(s => s.NeedsManualReview).ToList();

        // --- 3. Danh gia tinh hop phap ---
        var rules = options.Rules ?? DetectionRuleSet.Embedded;
        var findings = ActivationRiskScanner.Scan(ctx, rules);
        var scope = ActivationRiskScanner.BuildScope(findings, rules);
        report.DetectionRulesVersion = rules.Version;

        // Trang thai Genuine: suy ra tu LicenseStatus cua Windows.
        bool genuineKnown = licensedCount > 0 || licTable.Rows.Count > 0;
        bool genuineOk = licensedCount > 0;

        var verdict = RiskScoring.BuildVerdict(findings, genuineOk, genuineKnown);
        var score = RiskScoring.Compute(findings, genuineCheckFailed: genuineKnown && !genuineOk, manualReview.Count);

        report.VerdictLevel = verdict.Level;
        report.VerdictText = verdict.Title;
        report.RiskFindingsCount = findings.Count;
        report.RiskScore = score;

        var scopeTable = DataTable.Create("Phạm vi quét (6 hạng mục cố định - luôn hiển thị dù có phát hiện hay không)",
            "Hạng mục quét", "Nguồn dữ liệu", "Mức độ nếu phát hiện", "Số phát hiện trên máy này");
        foreach (var s in scope)
            scopeTable.AddRow(s.Category, s.DataSource, RiskLabels.Vietnamese(s.LevelIfFound), s.FoundCount);

        var findingsTable = DataTable.Create("Danh sách chi tiết từng dấu hiệu phát hiện được",
            "Hạng mục", "Phát hiện", "Mức độ", "Diễn giải");
        findingsTable.Searchable = findings.Count > 10;
        foreach (var f in findings.OrderByDescending(f => f.Level))
            findingsTable.AddRow(f.Category, f.Detection, RiskLabels.Vietnamese(f.Level), f.Explanation);
        findingsTable.AddEmptyNotice("Không phát hiện dấu hiệu nào ở cả 6 hạng mục quét");

        var verdictSection = new ReportSection
        {
            Id = "sec-verdict", NavLabel = "3. Đánh giá hợp pháp",
            Heading = "3. Đánh giá tính hợp pháp bản quyền Windows",
            Verdict = verdict,
            Tables = { scopeTable, findingsTable },
            MethodNote =
                "<b>Về phương pháp và giới hạn:</b> Đây là quét heuristic theo các dấu hiệu ĐÃ BIẾT của những công cụ " +
                "kích hoạt trái phép phổ biến - KHÔNG thể phát hiện 100% mọi biến thể, đặc biệt các bản tùy biến/đổi tên mới " +
                "hoặc kỹ thuật kích hoạt qua Digital License/HWID không để lại artifact rõ ràng. " +
                "Không có phát hiện KHÔNG đồng nghĩa máy sạch tuyệt đối; ngược lại có phát hiện cũng nên xác minh thủ công " +
                "trước khi kết luận, vì một số tên file/service có thể trùng lặp ngẫu nhiên. " +
                "Kết quả này mang tính hỗ trợ ra quyết định, KHÔNG phải bằng chứng pháp lý thay thế cho việc đối chiếu " +
                "hồ sơ mua bản quyền chính thức của tổ chức.<br><br>" +
                $"<b>Cách tính điểm rủi ro:</b> API Genuine thất bại +{RiskScoring.GenuineFailedWeight}đ; " +
                $"mỗi phát hiện Rất cao +{RiskScoring.CriticalPer}đ (tối đa {RiskScoring.CriticalCap}đ); " +
                $"Cao +{RiskScoring.HighPer}đ (tối đa {RiskScoring.HighCap}đ); " +
                $"Trung bình +{RiskScoring.MediumPer}đ (tối đa {RiskScoring.MediumCap}đ); " +
                $"mỗi phần mềm cần kiểm tra thủ công +{RiskScoring.ManualReviewPer}đ (tối đa {RiskScoring.ManualReviewCap}đ)."
        };
        verdictSection.Badges.Add(new Badge { Text = $"Điểm rủi ro: {score.Value}/100 ({score.Label})", Level = score.Band });
        foreach (var lvl in new[] { RiskLevel.Critical, RiskLevel.High, RiskLevel.Medium, RiskLevel.Low })
        {
            var n = findings.Count(f => f.Level == lvl);
            if (n > 0) verdictSection.Badges.Add(new Badge { Text = $"{RiskLabels.Vietnamese(lvl)}: {n}", Level = lvl });
        }
        report.Sections.Add(verdictSection);

        // --- 4. Danh sach phan mem ---
        report.Sections.Add(new ReportSection
        {
            Id = "sec-software", NavLabel = "4. Phần mềm đã cài",
            Heading = $"4. Danh sách phần mềm đã cài đặt ({software.Count})",
            Description = "Đọc từ 3 nhánh registry Uninstall (64-bit, 32-bit và per-user) - đầy đủ hơn nhiều so với " +
                          "<code>Win32_Product</code>. Cột \"Phân loại\" là bảng tra theo từ khóa, không đầy đủ 100%.",
            Tables = { SoftwareCollector.ToTable(software) }
        });

        // --- 5. Can kiem tra thu cong ---
        report.Sections.Add(new ReportSection
        {
            Id = "sec-manual", NavLabel = "5. Cần kiểm tra thủ công",
            Heading = $"5. Phần mềm cần kiểm tra thủ công ({manualReview.Count})",
            Description = "Các phần mềm thiếu thông tin Nhà phát hành hoặc Phiên bản trong registry - thường là phần mềm " +
                          "portable, tool nội bộ, hoặc cài đặt không đúng chuẩn. Nên kiểm tra nguồn gốc bản quyền thủ công.",
            Tables = { SoftwareCollector.ToManualReviewTable(software) }
        });

        report.SummaryCards.Add(new SummaryCard { Value = software.Count.ToString(), Label = "Tổng số phần mềm đã cài" });
        report.SummaryCards.Add(new SummaryCard { Value = licensedCount.ToString(), Label = "Sản phẩm Windows có license" });
        report.SummaryCards.Add(new SummaryCard { Value = findings.Count.ToString(), Label = "Dấu hiệu kích hoạt trái phép" });
        report.SummaryCards.Add(new SummaryCard { Value = $"{score.Value}/100", Label = $"Điểm rủi ro ({score.Label})" });

        report.Warnings.AddRange(ctx.Warnings);
        return report;
    }
}

/// <summary>Dung bao cao cau hinh phan cung thiet bi.</summary>
public static class HardwareReportBuilder
{
    public static AuditReport Build(SystemContext ctx, AuditOptions options)
    {
        var report = new AuditReport
        {
            ReportKind = ReportKind.HardwareInventory,
            ComputerName = ctx.ComputerName,
            ScanTime = ctx.ScanTime,
            Title = "Báo cáo cấu hình phần cứng thiết bị",
            HtmlReportFile = FileNaming.Html("Windows_Hardware_Inventory", ctx)
        };

        if (!ctx.IsElevated)
            ctx.Warn("Đang chạy KHÔNG có quyền Administrator - một số mục (Defender, DISM, Serial Number) có thể thiếu dữ liệu.");

        var (overview, summary) = HardwareCollector.CollectOverview(ctx);
        report.HardwareSummary = summary;
        report.Sections.Add(new ReportSection
        {
            Id = "sec-overview", NavLabel = "1. Tổng quan", Heading = "1. Tổng quan thiết bị",
            Tables = { overview }
        });

        report.Sections.Add(new ReportSection
        {
            Id = "sec-cpu", NavLabel = "2. CPU", Heading = "2. CPU (Bộ xử lý)",
            Tables = { HardwareCollector.CollectCpu(ctx) }
        });

        var (ramTable, usedSlots, totalSlots) = HardwareCollector.CollectRam(ctx);
        report.Sections.Add(new ReportSection
        {
            Id = "sec-ram", NavLabel = "3. RAM",
            Heading = $"3. RAM (Bộ nhớ) - {usedSlots} thanh đang cắm / {totalSlots} khe cắm",
            Tables = { ramTable }
        });

        report.Sections.Add(new ReportSection
        {
            Id = "sec-disk", NavLabel = "4. Ổ đĩa", Heading = "4. Ổ đĩa & phân vùng",
            Tables = { HardwareCollector.CollectDisks(ctx), HardwareCollector.CollectVolumes(ctx) }
        });

        report.Sections.Add(new ReportSection
        {
            Id = "sec-gpu", NavLabel = "5. GPU", Heading = "5. Card đồ họa (GPU)",
            Tables = { HardwareCollector.CollectGpu(ctx) }
        });

        report.Sections.Add(new ReportSection
        {
            Id = "sec-net", NavLabel = "6. Mạng", Heading = "6. Card mạng (đang hoạt động)",
            Tables = { HardwareCollector.CollectNetwork(ctx) }
        });

        report.Sections.Add(new ReportSection
        {
            Id = "sec-drivers", NavLabel = "7. Driver lỗi", Heading = "7. Driver lỗi trong Device Manager",
            Description = "Liệt kê thiết bị có mã lỗi (ConfigManagerErrorCode khác 0) - tương đương dấu chấm than vàng " +
                          "hoặc dấu X đỏ trong Device Manager.",
            Tables = { HardwareCollector.CollectDriverErrors(ctx) }
        });

        var integrity = new ReportSection
        {
            Id = "sec-integrity", NavLabel = "8. Toàn vẹn hệ thống",
            Heading = "8. Toàn vẹn file hệ thống & driver",
            Description = "Dùng 2 công cụ CHÍNH THỨC của Windows: <b>DISM CheckHealth</b> kiểm tra nhanh kho thành phần " +
                          "hệ thống, và <b>System File Checker</b> xác minh từng file hệ thống/driver so với bản gốc.",
            Tables = { SystemIntegrityCollector.Collect(ctx, options.RunDism, options.RunSfc) },
            MethodNote = "Nếu kết quả báo lỗi, chạy với quyền Administrator: <code>DISM /Online /Cleanup-Image /RestoreHealth</code> " +
                         "rồi <code>sfc /scannow</code>. Mục System File Checker mặc định TẮT (mất 5-15 phút) - bật bằng tham số <code>--sfc</code>."
        };
        report.Sections.Add(integrity);

        var (defStatus, defThreats, threatCount) = DefenderCollector.Collect(ctx);
        var defSection = new ReportSection
        {
            Id = "sec-defender", NavLabel = "9. Chống mã độc",
            Heading = "9. Phát hiện mã độc - Windows Defender",
            Description = "Đọc trực tiếp trạng thái và <b>lịch sử phát hiện thật</b> từ Windows Defender - antivirus engine " +
                          "mặc định của Windows với cơ sở dữ liệu chữ ký được Microsoft cập nhật liên tục. Đây là phương pháp " +
                          "đáng tin cậy nhất để liệt kê file (.exe, .doc, ...) đã bị phát hiện nhiễm mã độc, thay vì tự dò " +
                          "quét bằng heuristic tự viết (vốn kém chính xác và dễ gây hiểu lầm).",
            Tables = { defStatus, defThreats },
            MethodNote = "Danh sách trên chỉ hiện các mối đe dọa Windows Defender ĐÃ TỪNG quét thấy. Để chắc chắn máy sạch " +
                         "tại thời điểm hiện tại, hãy chạy Quick/Full Scan trong Windows Security trước khi chạy lại tool này."
        };
        defSection.Badges.Add(new Badge
        {
            Text = $"{threatCount} mối đe dọa",
            Level = threatCount > 0 ? RiskLevel.High : RiskLevel.None
        });
        report.Sections.Add(defSection);

        report.SummaryCards.Add(new SummaryCard { Value = ctx.Wmi.Query("Win32_Processor").Count.ToString(), Label = "CPU vật lý" });
        report.SummaryCards.Add(new SummaryCard { Value = $"{usedSlots} / {totalSlots}", Label = "Khe RAM đang dùng / tổng khe" });
        report.SummaryCards.Add(new SummaryCard { Value = threatCount.ToString(), Label = "Mối đe dọa Defender đã ghi nhận" });

        report.Warnings.AddRange(ctx.Warnings);
        return report;
    }
}

/// <summary>Nhan tieng Viet cho muc do rui ro - dinh nghia MOT NOI duy nhat.</summary>
public static class RiskLabels
{
    public static string Vietnamese(RiskLevel level) => level switch
    {
        RiskLevel.Critical => "Rất cao",
        RiskLevel.High => "Cao",
        RiskLevel.Medium => "Trung bình",
        RiskLevel.Low => "Thấp",
        _ => "Không"
    };
}

/// <summary>Quy tac dat ten file va thu muc dau ra (cau truc 3 cap).</summary>
public static class FileNaming
{
    public static string Stamp(SystemContext ctx) => ctx.ScanTime.LocalDateTime.ToString("yyyyMMdd_HHmmss");
    public static string DateOnly(DateTimeOffset t) => t.LocalDateTime.ToString("yyyyMMdd");

    public static string Html(string prefix, SystemContext ctx)
        => $"{prefix}_{ctx.ComputerName}_{Stamp(ctx)}.html";

    /// <summary>Thu muc cap 2: chua tsudev-tong-hop.html</summary>
    public static string Level2(string root, string computerName, DateTimeOffset scanTime)
        => Path.Combine(root, $"tsudev-bao-cao-ra-quet-{computerName}-{DateOnly(scanTime)}");

    /// <summary>Thu muc cap 3: chua HTML/JSON/XLSX/CSV cua lan quet nay</summary>
    public static string Level3(string level2, string computerName, DateTimeOffset scanTime)
        => Path.Combine(level2, $"tsudev-ket-qua-ra-quet-{computerName}-{DateOnly(scanTime)}");
}
