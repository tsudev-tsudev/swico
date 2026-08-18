using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Rules;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>
/// Quet dau hieu (heuristic) cua cac cong cu kich hoat trai phep pho bien.
///
/// GIOI HAN CO Y - phai noi ro trong bao cao: day la quet theo dau hieu DA BIET,
/// KHONG the phat hien 100% moi bien the (dac biet cac ban doi ten/tuy bien moi,
/// hoac ky thuat HWID khong de lai artifact). Khong co phat hien KHONG dong
/// nghia may sach tuyet doi; co phat hien cung nen xac minh thu cong truoc khi
/// ket luan vi ten file/service co the trung lap ngau nhien.
/// </summary>
public static class ActivationRiskScanner
{
    /// <summary>
    /// Quet bang bo luat dong kem trong file exe.
    /// Giu chu ky nay de moi noi goi cu khong phai sua.
    /// </summary>
    public static List<RiskFinding> Scan(SystemContext ctx)
        => Scan(ctx, DetectionRuleSet.Embedded);

    /// <summary>Quet bang mot bo luat cu the (cho phep nap luat tu file ngoai).</summary>
    public static List<RiskFinding> Scan(SystemContext ctx, DetectionRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var findings = new List<RiskFinding>();

        ScanFileSystem(ctx, rules, findings);
        ScanScheduledTasks(ctx, rules, findings);
        ScanServices(ctx, rules, findings);
        ScanHostsFile(ctx, rules, findings);
        ScanHookFiles(ctx, rules, findings);
        ScanKmsRegistry(ctx, findings);

        return findings;
    }

    private static void ScanFileSystem(SystemContext ctx, DetectionRuleSet rules, List<RiskFinding> findings)
    {
        foreach (var root in rules.ScanRoots)
        {
            var expanded = ctx.Files.ExpandEnvironment(root);
            if (!ctx.Files.DirectoryExists(expanded)) continue;

            foreach (var dir in ctx.Files.GetDirectories(expanded))
            {
                var name = Path.GetFileName(dir.TrimEnd('\\', '/'));
                var hit = rules.SuspiciousNames.FirstOrDefault(s =>
                    name.Contains(s, StringComparison.OrdinalIgnoreCase));
                if (hit is not null)
                {
                    findings.Add(new RiskFinding
                    {
                        Category = "Tên file/thư mục nghi vấn",
                        Detection = dir,
                        Level = RiskLevel.High,
                        Explanation = $"Tên trùng khớp với công cụ kích hoạt trái phép phổ biến ({hit})."
                    });
                }
            }
        }
    }

    private static void ScanScheduledTasks(SystemContext ctx, DetectionRuleSet rules, List<RiskFinding> findings)
    {
        var tasks = ctx.Wmi.Query("MSFT_ScheduledTask", null, "root\\Microsoft\\Windows\\TaskScheduler");
        foreach (var task in tasks)
        {
            var name = task.Str("TaskName", "");
            if (string.IsNullOrWhiteSpace(name)) continue;
            // Task hop le cua Windows dung de gia han license KMS doanh nghiep -
            // KHONG duoc bao dong nham cho task nay.
            if (rules.LegitimateTaskNames.Any(l => name.Contains(l, StringComparison.OrdinalIgnoreCase)))
                continue;

            var hit = rules.SuspiciousNames.FirstOrDefault(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                findings.Add(new RiskFinding
                {
                    Category = "Scheduled Task nghi vấn",
                    Detection = name,
                    Level = RiskLevel.High,
                    Explanation = $"Scheduled Task mang tên đặc trưng của công cụ kích hoạt trái phép ({hit})."
                });
            }
        }
    }

    private static void ScanServices(SystemContext ctx, DetectionRuleSet rules, List<RiskFinding> findings)
    {
        foreach (var svc in ctx.Wmi.Query("Win32_Service"))
        {
            var name = svc.Str("Name", "");
            var display = svc.Str("DisplayName", "");
            var hit = rules.SuspiciousNames.FirstOrDefault(s =>
                name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                display.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                findings.Add(new RiskFinding
                {
                    Category = "Windows Service nghi vấn",
                    Detection = string.IsNullOrWhiteSpace(display) ? name : $"{name} ({display})",
                    Level = RiskLevel.High,
                    Explanation = $"Windows Service mang tên đặc trưng của công cụ kích hoạt trái phép ({hit})."
                });
            }
        }
    }

    private static void ScanHostsFile(SystemContext ctx, DetectionRuleSet rules, List<RiskFinding> findings)
    {
        var hostsPath = ctx.Files.ExpandEnvironment(@"%SystemRoot%\System32\drivers\etc\hosts");
        var content = ctx.Files.ReadAllTextOrNull(hostsPath);
        if (content is null) return;

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            var hit = rules.KnownKmsHosts.FirstOrDefault(h =>
                trimmed.Contains(h, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                findings.Add(new RiskFinding
                {
                    Category = "Hosts file bị chỉnh sửa",
                    Detection = trimmed,
                    Level = RiskLevel.Medium,
                    Explanation = $"Dòng hosts trỏ tới máy chủ KMS công cộng đã biết ({hit})."
                });
            }
            else if (rules.HostsInterferenceKeywords.Any(k =>
                         trimmed.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(new RiskFinding
                {
                    Category = "Hosts file bị chỉnh sửa",
                    Detection = trimmed,
                    Level = RiskLevel.Medium,
                    Explanation = "Dòng hosts có thể đang chặn máy chủ xác thực bản quyền của Microsoft."
                });
            }
        }
    }

    private static void ScanHookFiles(SystemContext ctx, DetectionRuleSet rules, List<RiskFinding> findings)
    {
        foreach (var dir in rules.HookDirectories)
        {
            var expanded = ctx.Files.ExpandEnvironment(dir);
            foreach (var file in rules.HookFiles)
            {
                var full = Path.Combine(expanded, file);
                if (ctx.Files.FileExists(full))
                {
                    findings.Add(new RiskFinding
                    {
                        Category = "File hook hệ thống bản quyền",
                        Detection = full,
                        Level = RiskLevel.Critical,
                        Explanation = "File hook thay thế trực tiếp thành phần lõi bảo vệ bản quyền của Windows - dấu hiệu rất mạnh của kích hoạt trái phép."
                    });
                }
            }
        }
    }

    private static void ScanKmsRegistry(SystemContext ctx, List<RiskFinding> findings)
    {
        const string path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform";
        var kms = ctx.Registry.GetValue(RegistryRoot.LocalMachine, path, "KeyManagementServiceName")?.ToString();
        if (!string.IsNullOrWhiteSpace(kms))
        {
            findings.Add(new RiskFinding
            {
                Category = "KMS server tùy chỉnh trong registry",
                Detection = $"KeyManagementServiceName = {kms}",
                Level = RiskLevel.High,
                Explanation = "Máy được cấu hình trỏ tới một máy chủ KMS tùy chỉnh. Hợp lệ nếu tổ chức có KMS riêng; bất thường nếu không."
            });
        }
    }

    /// <summary>
    /// Bang tra cuu 6 hang muc quet - LUON hien thi du co phat hien hay khong,
    /// de nguoi doc bao cao biet chinh xac script da kiem tra nhung gi.
    /// </summary>
    public static List<DetectionScope> BuildScope(IReadOnlyList<RiskFinding> findings)
        => BuildScope(findings, DetectionRuleSet.Embedded);

    public static List<DetectionScope> BuildScope(
        IReadOnlyList<RiskFinding> findings, DetectionRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var legit = rules.LegitimateTaskNames.Count > 0
            ? $" (trừ task hệ thống hợp lệ {string.Join(", ", rules.LegitimateTaskNames)})"
            : "";

        (string Cat, string Src, RiskLevel Lvl)[] defs =
        {
            ("Tên file/thư mục nghi vấn", string.Join(", ", rules.ScanRoots), RiskLevel.High),
            ("Scheduled Task nghi vấn", $"Task Scheduler{legit}", RiskLevel.High),
            ("Windows Service nghi vấn", "Danh sách toàn bộ Windows Service (Win32_Service)", RiskLevel.High),
            ("Hosts file bị chỉnh sửa", @"%SystemRoot%\System32\drivers\etc\hosts", RiskLevel.Medium),
            ("File hook hệ thống bản quyền", $"{string.Join(" / ", rules.HookDirectories)} ({string.Join(", ", rules.HookFiles)})", RiskLevel.Critical),
            ("KMS server tùy chỉnh trong registry", @"HKLM\...\SoftwareProtectionPlatform", RiskLevel.High),
        };

        return defs.Select(d => new DetectionScope
        {
            Category = d.Cat,
            DataSource = d.Src,
            LevelIfFound = d.Lvl,
            FoundCount = findings.Count(f => f.Category == d.Cat)
        }).ToList();
    }
}

/// <summary>
/// Tinh diem rui ro tong hop 0-100. Cong thuc don gian, MINH BACH va CO GIOI
/// HAN TUNG NHOM - de khi audit lai co the giai thich duoc vi sao ra diem do,
/// va de mot loai phat hien khong the "thoi phong" diem len 100 mot minh.
/// </summary>
public static class RiskScoring
{
    public const int GenuineFailedWeight = 40;
    public const int CriticalPer = 15, CriticalCap = 30;
    public const int HighPer = 10, HighCap = 30;
    public const int MediumPer = 5, MediumCap = 15;
    public const int ManualReviewPer = 2, ManualReviewCap = 15;

    public static RiskScore Compute(
        IReadOnlyList<RiskFinding> findings,
        bool genuineCheckFailed,
        int manualReviewCount)
    {
        int score = 0;
        if (genuineCheckFailed) score += GenuineFailedWeight;

        score += Math.Min(findings.Count(f => f.Level == RiskLevel.Critical) * CriticalPer, CriticalCap);
        score += Math.Min(findings.Count(f => f.Level == RiskLevel.High) * HighPer, HighCap);
        score += Math.Min(findings.Count(f => f.Level == RiskLevel.Medium) * MediumPer, MediumCap);
        score += Math.Min(manualReviewCount * ManualReviewPer, ManualReviewCap);

        score = Math.Clamp(score, 0, 100);

        var (band, label) = score switch
        {
            >= 70 => (RiskLevel.Critical, "Nghiêm trọng"),
            >= 40 => (RiskLevel.High, "Cao"),
            >= 15 => (RiskLevel.Medium, "Trung bình"),
            _ => (RiskLevel.Low, "Thấp")
        };

        return new RiskScore { Value = score, Band = band, Label = label };
    }

    /// <summary>Ket luan tong the tu ket qua API Genuine + cac phat hien.</summary>
    /// <summary>
    /// Ket luan tong the. <paramref name="license"/> la trang thai license da
    /// duoc tong hop THAN TRONG (xem <c>WindowsLicenseSummary</c>): chi coi la
    /// hop le khi khong con SKU nao co van de.
    /// </summary>
    public static Verdict BuildVerdict(IReadOnlyList<RiskFinding> findings, LicenseHealth license)
    {
        ArgumentNullException.ThrowIfNull(findings);

        bool hasSevere = findings.Any(f => f.Level >= RiskLevel.High);

        if (hasSevere || license == LicenseHealth.Problem)
        {
            return new Verdict
            {
                Level = VerdictLevel.Bad,
                Title = "❌ CÓ DẤU HIỆU RÕ RÀNG CỦA KÍCH HOẠT TRÁI PHÉP",
                Detail = "Windows API và/hoặc dấu hiệu artifact cho thấy khả năng cao máy đang dùng license không hợp pháp. " +
                         "Cần kiểm tra thủ công ngay và đối chiếu với hồ sơ mua bản quyền của tổ chức."
            };
        }

        // Con han dung thu KHONG phai vi pham, nhung cung KHONG phai hop le.
        // Truoc day truong hop nay bi cham diem "hop le" - nay ha xuong canh bao.
        if (license == LicenseHealth.Grace)
        {
            return new Verdict
            {
                Level = VerdictLevel.Warning,
                Title = "⚠️ WINDOWS ĐANG TRONG THỜI GIAN DÙNG THỬ / GIA HẠN",
                Detail = "Máy chưa ở trạng thái kích hoạt vĩnh viễn. Đây chưa phải vi phạm, nhưng license sẽ hết hiệu lực " +
                         "khi hết thời gian gia hạn. Cần kích hoạt bằng license hợp pháp trước thời điểm đó."
            };
        }

        if (findings.Count > 0)
        {
            return new Verdict
            {
                Level = VerdictLevel.Warning,
                Title = "⚠️ CÓ DẤU HIỆU CẦN XEM XÉT THÊM",
                Detail = "Phát hiện một số dấu hiệu mức độ thấp/trung bình. Có thể là trùng lặp ngẫu nhiên, " +
                         "nhưng nên xác minh thủ công để loại trừ khả năng kích hoạt trái phép."
            };
        }

        if (license == LicenseHealth.Ok)
        {
            return new Verdict
            {
                Level = VerdictLevel.Ok,
                Title = "✅ KHÔNG PHÁT HIỆN DẤU HIỆU KÍCH HOẠT TRÁI PHÉP",
                Detail = "Windows API xác nhận bản quyền hợp lệ và không tìm thấy artifact của công cụ crack đã biết. " +
                         "Lưu ý: đây là kết quả quét theo dấu hiệu đã biết, không phải bảo chứng tuyệt đối."
            };
        }

        return new Verdict
        {
            Level = VerdictLevel.Unknown,
            Title = "❔ CHƯA ĐỦ CƠ SỞ KẾT LUẬN",
            Detail = "Không tìm thấy dấu hiệu kích hoạt trái phép, nhưng cũng không xác minh được trạng thái Genuine qua API Windows. " +
                     "Nên kiểm tra thủ công tại Settings → Activation."
        };
    }
}
