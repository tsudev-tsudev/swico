using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;

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
    /// <summary>Ten thu muc/file dac trung cua cac cong cu crack pho bien.</summary>
    private static readonly string[] SuspiciousNames =
    {
        "KMSpico", "KMSAuto", "KMSAuto Net", "Microsoft Toolkit", "Re-Loader",
        "HWIDGen", "MAS", "AutoKMS", "KMSELDI", "KMS_VL_ALL", "Ratiborus",
        "W10Digital", "ActivationTool"
    };

    /// <summary>File hook truc tiep thay the thanh phan loi bao ve ban quyen.</summary>
    private static readonly string[] HookFiles =
    {
        "SppExtComObjHook.dll", "SppExtComObjPatcher.exe"
    };

    /// <summary>Ten may chu KMS cong cong da biet (xuat hien trong hosts file).</summary>
    private static readonly string[] KnownKmsHosts =
    {
        "kms.digiboy.ir", "kms8.msguides.com", "kms.msguides.com",
        "kms.lotro.cc", "zh.na.bz", "kms.03k.org", "kms.chinancce.com"
    };

    public static List<RiskFinding> Scan(SystemContext ctx)
    {
        var findings = new List<RiskFinding>();

        ScanFileSystem(ctx, findings);
        ScanScheduledTasks(ctx, findings);
        ScanServices(ctx, findings);
        ScanHostsFile(ctx, findings);
        ScanHookFiles(ctx, findings);
        ScanKmsRegistry(ctx, findings);

        return findings;
    }

    private static void ScanFileSystem(SystemContext ctx, List<RiskFinding> findings)
    {
        string[] roots =
        {
            @"%ProgramFiles%", @"%ProgramFiles(x86)%", @"%ProgramData%",
            @"%SystemDrive%\", @"%TEMP%", @"%USERPROFILE%\Desktop", @"%USERPROFILE%\Downloads"
        };

        foreach (var root in roots)
        {
            var expanded = ctx.Files.ExpandEnvironment(root);
            if (!ctx.Files.DirectoryExists(expanded)) continue;

            foreach (var dir in ctx.Files.GetDirectories(expanded))
            {
                var name = Path.GetFileName(dir.TrimEnd('\\', '/'));
                var hit = SuspiciousNames.FirstOrDefault(s =>
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

    private static void ScanScheduledTasks(SystemContext ctx, List<RiskFinding> findings)
    {
        // Task hop le cua Windows dung de gia han license KMS doanh nghiep -
        // KHONG duoc bao dong nham cho task nay.
        const string legitimate = "SoftwareProtectionPlatform";

        var tasks = ctx.Wmi.Query("MSFT_ScheduledTask", null, "root\\Microsoft\\Windows\\TaskScheduler");
        foreach (var task in tasks)
        {
            var name = task.Str("TaskName", "");
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.Contains(legitimate, StringComparison.OrdinalIgnoreCase)) continue;

            var hit = SuspiciousNames.FirstOrDefault(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));
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

    private static void ScanServices(SystemContext ctx, List<RiskFinding> findings)
    {
        foreach (var svc in ctx.Wmi.Query("Win32_Service"))
        {
            var name = svc.Str("Name", "");
            var display = svc.Str("DisplayName", "");
            var hit = SuspiciousNames.FirstOrDefault(s =>
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

    private static void ScanHostsFile(SystemContext ctx, List<RiskFinding> findings)
    {
        var hostsPath = ctx.Files.ExpandEnvironment(@"%SystemRoot%\System32\drivers\etc\hosts");
        var content = ctx.Files.ReadAllTextOrNull(hostsPath);
        if (content is null) return;

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            var hit = KnownKmsHosts.FirstOrDefault(h =>
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
            else if (trimmed.Contains("microsoft.com", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("activation", StringComparison.OrdinalIgnoreCase))
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

    private static void ScanHookFiles(SystemContext ctx, List<RiskFinding> findings)
    {
        string[] dirs = { @"%SystemRoot%\System32", @"%SystemRoot%\SysWOW64" };
        foreach (var dir in dirs)
        {
            var expanded = ctx.Files.ExpandEnvironment(dir);
            foreach (var file in HookFiles)
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
    {
        (string Cat, string Src, RiskLevel Lvl)[] defs =
        {
            ("Tên file/thư mục nghi vấn", "Program Files, ProgramData, ổ hệ thống, Temp, Desktop, Downloads", RiskLevel.High),
            ("Scheduled Task nghi vấn", "Task Scheduler (trừ task hệ thống hợp lệ SoftwareProtectionPlatform)", RiskLevel.High),
            ("Windows Service nghi vấn", "Danh sách toàn bộ Windows Service (Win32_Service)", RiskLevel.High),
            ("Hosts file bị chỉnh sửa", @"%SystemRoot%\System32\drivers\etc\hosts", RiskLevel.Medium),
            ("File hook hệ thống bản quyền", "System32 / SysWOW64 (SppExtComObjHook.dll, SppExtComObjPatcher.exe)", RiskLevel.Critical),
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
    public static Verdict BuildVerdict(IReadOnlyList<RiskFinding> findings, bool genuineOk, bool genuineKnown)
    {
        bool hasSevere = findings.Any(f => f.Level >= RiskLevel.High);

        if (hasSevere || (genuineKnown && !genuineOk))
        {
            return new Verdict
            {
                Level = VerdictLevel.Bad,
                Title = "❌ CÓ DẤU HIỆU RÕ RÀNG CỦA KÍCH HOẠT TRÁI PHÉP",
                Detail = "Windows API và/hoặc dấu hiệu artifact cho thấy khả năng cao máy đang dùng license không hợp pháp. " +
                         "Cần kiểm tra thủ công ngay và đối chiếu với hồ sơ mua bản quyền của tổ chức."
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

        if (genuineKnown && genuineOk)
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
