using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Mot phan mem da cai dat, doc tu registry Uninstall.</summary>
public sealed record InstalledSoftware(
    string Name, string Version, string Publisher, string InstallDate,
    string InstallLocation, string Category, string ProductGuid)
{
    /// <summary>Thieu Publisher hoac Version -> can kiem tra thu cong.</summary>
    public bool NeedsManualReview =>
        Publisher is "-" or "" || Version is "-" or "";
}

/// <summary>
/// Liet ke phan mem da cai tu 3 nhanh registry Uninstall (64-bit, 32-bit,
/// va per-user) - day du hon nhieu so voi Win32_Product (von cham va chi thay
/// phan mem cai qua MSI).
/// </summary>
public static class SoftwareCollector
{
    private static readonly (RegistryRoot Root, string Path)[] UninstallKeys =
    {
        (RegistryRoot.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryRoot.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryRoot.CurrentUser,  @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
    };

    /// <summary>Bang tra phan loai theo tu khoa - khong day du 100%, chi ho tro doc nhanh.</summary>
    private static readonly (string Keyword, string Category)[] CategoryRules =
    {
        ("visual studio", "Công cụ lập trình"), ("vscode", "Công cụ lập trình"),
        ("git", "Công cụ lập trình"), ("python", "Công cụ lập trình"),
        ("node", "Công cụ lập trình"), ("docker", "Công cụ lập trình"),
        ("office", "Văn phòng"), ("word", "Văn phòng"), ("excel", "Văn phòng"),
        ("zoom", "Họp trực tuyến"), ("teams", "Họp trực tuyến"), ("meet", "Họp trực tuyến"),
        ("chrome", "Trình duyệt"), ("firefox", "Trình duyệt"), ("edge", "Trình duyệt"),
        ("7-zip", "Nén/giải nén"), ("winrar", "Nén/giải nén"), ("winzip", "Nén/giải nén"),
        ("photoshop", "Đồ họa"), ("illustrator", "Đồ họa"), ("gimp", "Đồ họa"),
        ("antivirus", "Bảo mật"), ("defender", "Bảo mật"), ("kaspersky", "Bảo mật"),
        ("vpn", "Mạng/VPN"), ("proxy", "Mạng/VPN"),
        ("driver", "Driver/Hệ thống"), ("runtime", "Driver/Hệ thống"), ("redistributable", "Driver/Hệ thống"),
    };

    public static List<InstalledSoftware> Collect(SystemContext ctx)
    {
        var list = new List<InstalledSoftware>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (root, basePath) in UninstallKeys)
        {
            foreach (var sub in ctx.Registry.GetSubKeyNames(root, basePath))
            {
                var keyPath = $@"{basePath}\{sub}";

                var name = Val(ctx, root, keyPath, "DisplayName");
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Bo qua ban va, update, thanh phan he thong an
                var systemComponent = ctx.Registry.GetValue(root, keyPath, "SystemComponent");
                if (systemComponent is int sc && sc == 1) continue;
                if (!string.IsNullOrWhiteSpace(Val(ctx, root, keyPath, "ParentKeyName"))) continue;

                var version = Val(ctx, root, keyPath, "DisplayVersion");
                var publisher = Val(ctx, root, keyPath, "Publisher");

                // Chong trung: cung ten + cung phien ban thi chi lay 1 lan
                var dedupeKey = $"{name}|{version}";
                if (!seen.Add(dedupeKey)) continue;

                list.Add(new InstalledSoftware(
                    Name: Fallback(name),
                    Version: Fallback(version),
                    Publisher: Fallback(publisher),
                    InstallDate: FormatInstallDate(Val(ctx, root, keyPath, "InstallDate")),
                    InstallLocation: Fallback(Val(ctx, root, keyPath, "InstallLocation")),
                    Category: Classify(name),
                    ProductGuid: sub));
            }
        }

        return list.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static string Val(SystemContext ctx, RegistryRoot root, string path, string name)
        => ctx.Registry.GetValue(root, path, name)?.ToString()?.Trim() ?? "";

    private static string Fallback(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

    /// <summary>Registry luu InstallDate dang "yyyyMMdd" -> chuyen sang "yyyy-MM-dd".</summary>
    public static string FormatInstallDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "-";
        raw = raw.Trim();
        if (raw.Length == 8 && raw.All(char.IsDigit))
            return $"{raw[..4]}-{raw.Substring(4, 2)}-{raw.Substring(6, 2)}";
        return raw;
    }

    public static string Classify(string name)
    {
        foreach (var (kw, cat) in CategoryRules)
            if (name.Contains(kw, StringComparison.OrdinalIgnoreCase)) return cat;
        return "Chưa phân loại";
    }

    public static DataTable ToTable(IReadOnlyList<InstalledSoftware> items)
    {
        var t = DataTable.Create("", "Tên phần mềm", "Phiên bản", "Nhà phát hành",
            "Ngày cài đặt", "Thư mục cài đặt", "Phân loại", "Mã sản phẩm (GUID)");
        t.Searchable = true;
        foreach (var s in items)
            t.AddRow(s.Name, s.Version, s.Publisher, s.InstallDate, s.InstallLocation, s.Category, s.ProductGuid);
        t.AddEmptyNotice("Không đọc được phần mềm nào từ registry");
        return t;
    }

    public static DataTable ToManualReviewTable(IReadOnlyList<InstalledSoftware> items)
    {
        var t = DataTable.Create("", "Tên phần mềm", "Phiên bản", "Nhà phát hành", "Thư mục cài đặt", "Mã sản phẩm (GUID)");
        t.Searchable = true;
        foreach (var s in items.Where(x => x.NeedsManualReview))
            t.AddRow(s.Name, s.Version, s.Publisher, s.InstallLocation, s.ProductGuid);
        t.AddEmptyNotice("Không có phần mềm nào thiếu thông tin - tốt");
        return t;
    }
}

/// <summary>Thu thap cau hinh phan cung qua WMI.</summary>
public static class HardwareCollector
{
    public static string FormatBytes(long? bytes)
    {
        if (bytes is null or <= 0) return "-";
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double v = bytes.Value;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return $"{v:N1} {units[i]}";
    }

    public static (DataTable Table, string Summary) CollectOverview(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên máy", "Loại thiết bị", "Hãng sản xuất", "Model",
            "Số Serial (Chassis)", "BIOS Version", "Mainboard - Model", "Tổng RAM");

        var cs = ctx.Wmi.Query("Win32_ComputerSystem").FirstOrDefault();
        var bios = ctx.Wmi.Query("Win32_BIOS").FirstOrDefault();
        var board = ctx.Wmi.Query("Win32_BaseBoard").FirstOrDefault();
        var chassis = ctx.Wmi.Query("Win32_SystemEnclosure").FirstOrDefault();

        var ram = FormatBytes(cs?.Num("TotalPhysicalMemory"));
        var deviceType = DescribeChassis(chassis?.Num("ChassisTypes"));
        var manufacturer = cs?.Str("Manufacturer") ?? "-";
        var model = cs?.Str("Model") ?? "-";

        t.AddRow(ctx.ComputerName, deviceType, manufacturer, model,
            bios?.Str("SerialNumber") ?? "-", bios?.Str("SMBIOSBIOSVersion") ?? "-",
            board?.Str("Product") ?? "-", ram);

        var cpuCount = ctx.Wmi.Query("Win32_Processor").Count;
        var summary = $"{manufacturer} {model} · {cpuCount} CPU · {ram}";
        return (t, summary);
    }

    /// <summary>Ma ChassisTypes cua SMBIOS -> loai thiet bi.</summary>
    public static string DescribeChassis(long? code) => code switch
    {
        3 or 4 or 6 or 7 or 15 => "Desktop",
        8 or 9 or 10 or 14 => "Laptop",
        11 or 12 => "Handheld/Notebook",
        17 or 23 or 28 => "Server",
        30 or 31 or 32 => "Tablet",
        _ => "Không xác định"
    };

    public static DataTable CollectCpu(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên CPU", "Hãng sản xuất", "Số nhân (Core)",
            "Số luồng (Thread)", "Xung nhịp tối đa (MHz)", "Socket");
        foreach (var c in ctx.Wmi.Query("Win32_Processor"))
            t.AddRow(c.Str("Name"), c.Str("Manufacturer"), c.Num("NumberOfCores"),
                c.Num("NumberOfLogicalProcessors"), c.Num("MaxClockSpeed"), c.Str("SocketDesignation"));
        t.AddEmptyNotice("Không đọc được thông tin CPU");
        return t;
    }

    public static (DataTable Table, int Used, string Total) CollectRam(SystemContext ctx)
    {
        var t = DataTable.Create("", "Khe cắm (Slot)", "Dung lượng", "Tốc độ (MHz)",
            "Loại RAM", "Hãng sản xuất", "Serial Number");
        var sticks = ctx.Wmi.Query("Win32_PhysicalMemory");
        foreach (var m in sticks)
            t.AddRow(m.Str("DeviceLocator"), FormatBytes(m.Num("Capacity")), m.Num("Speed"),
                DescribeMemoryType(m.Num("SMBIOSMemoryType") ?? m.Num("MemoryType")),
                m.Str("Manufacturer"), m.Str("SerialNumber"));
        t.AddEmptyNotice("Không đọc được thông tin RAM");

        var array = ctx.Wmi.Query("Win32_PhysicalMemoryArray").FirstOrDefault();
        var totalSlots = array?.Num("MemoryDevices")?.ToString() ?? "-";
        return (t, sticks.Count, totalSlots);
    }

    public static string DescribeMemoryType(long? code) => code switch
    {
        20 => "DDR", 21 => "DDR2", 24 => "DDR3", 26 => "DDR4", 34 => "DDR5",
        _ => "Không xác định"
    };

    public static DataTable CollectDisks(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên / Model", "Chuẩn giao tiếp", "Dung lượng", "Serial Number", "Firmware");
        foreach (var d in ctx.Wmi.Query("Win32_DiskDrive"))
            t.AddRow(d.Str("Model"), d.Str("InterfaceType"), FormatBytes(d.Num("Size")),
                d.Str("SerialNumber"), d.Str("FirmwareRevision"));
        t.AddEmptyNotice("Không đọc được thông tin ổ đĩa");
        return t;
    }

    public static DataTable CollectVolumes(SystemContext ctx)
    {
        var t = DataTable.Create("", "Ổ đĩa", "Nhãn", "Hệ thống file",
            "Tổng dung lượng", "Dung lượng trống", "Tỷ lệ còn trống (%)");
        t.Searchable = true;
        // DriveType=3 : o dia cuc bo (loai bo o mang/USB de tranh nhieu)
        foreach (var v in ctx.Wmi.Query("Win32_LogicalDisk", "DriveType=3"))
        {
            var size = v.Num("Size");
            var free = v.Num("FreeSpace");
            var pct = (size is > 0 && free is not null)
                ? Math.Round(free.Value * 100.0 / size.Value, 1).ToString("0.0")
                : "-";
            t.AddRow(v.Str("DeviceID"), v.Str("VolumeName"), v.Str("FileSystem"),
                FormatBytes(size), FormatBytes(free), pct);
        }
        t.AddEmptyNotice("Không đọc được phân vùng nào");
        return t;
    }

    public static DataTable CollectGpu(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên GPU", "VRAM (ước tính)", "Độ phân giải hiện tại", "Phiên bản Driver");
        foreach (var g in ctx.Wmi.Query("Win32_VideoController"))
        {
            var res = g.Num("CurrentHorizontalResolution") is { } w && g.Num("CurrentVerticalResolution") is { } h
                ? $"{w} x {h}" : "-";
            t.AddRow(g.Str("Name"), FormatBytes(g.Num("AdapterRAM")), res, g.Str("DriverVersion"));
        }
        t.AddEmptyNotice("Không đọc được card đồ họa");
        return t;
    }

    public static DataTable CollectNetwork(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên adapter", "Địa chỉ MAC", "Địa chỉ IP", "Default Gateway", "DHCP");
        t.Searchable = true;
        foreach (var n in ctx.Wmi.Query("Win32_NetworkAdapterConfiguration", "IPEnabled=TRUE"))
        {
            t.AddRow(n.Str("Description"), n.Str("MACAddress"),
                JoinArray(n, "IPAddress"), JoinArray(n, "DefaultIPGateway"),
                n.Bool("DHCPEnabled") == true ? "Có" : "Không");
        }
        t.AddEmptyNotice("Không có card mạng nào đang hoạt động");
        return t;
    }

    private static string JoinArray(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return "-";
        if (v is string[] arr) return arr.Length == 0 ? "-" : string.Join(", ", arr);
        if (v is System.Collections.IEnumerable e and not string)
            return string.Join(", ", e.Cast<object?>().Where(x => x is not null));
        return v.ToString() ?? "-";
    }

    /// <summary>Driver loi trong Device Manager (ConfigManagerErrorCode != 0).</summary>
    public static DataTable CollectDriverErrors(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên thiết bị", "Mã lỗi", "Ý nghĩa", "Nhà sản xuất");
        foreach (var d in ctx.Wmi.Query("Win32_PNPEntity", "ConfigManagerErrorCode <> 0"))
        {
            var code = d.Num("ConfigManagerErrorCode");
            t.AddRow(d.Str("Name"), code, DescribeDeviceError(code), d.Str("Manufacturer"));
        }
        t.AddEmptyNotice("Không phát hiện thiết bị nào bị lỗi driver");
        return t;
    }

    public static string DescribeDeviceError(long? code) => code switch
    {
        1 => "Thiết bị chưa được cấu hình đúng",
        3 => "Driver bị lỗi hoặc thiếu tài nguyên",
        10 => "Thiết bị không khởi động được",
        18 => "Cần cài lại driver",
        19 => "Registry bị lỗi",
        22 => "Thiết bị đã bị vô hiệu hóa (Disabled)",
        24 => "Thiết bị không hiện diện/lỗi/thiếu driver",
        28 => "Chưa cài driver cho thiết bị này",
        31 => "Windows không thể tải driver",
        37 => "Windows không thể khởi tạo driver",
        39 => "Driver bị hỏng hoặc thiếu",
        43 => "Windows đã dừng thiết bị do lỗi",
        45 => "Thiết bị hiện không kết nối",
        _ => "Mã lỗi khác - xem Device Manager"
    };
}

/// <summary>Trang thai bao ve va lich su phat hien cua Windows Defender.</summary>
public static class DefenderCollector
{
    public static (DataTable Status, DataTable Threats, int ThreatCount) Collect(SystemContext ctx)
    {
        var status = DataTable.Create("Trạng thái bảo vệ hiện tại",
            "Bảo vệ thời gian thực", "Chống virus bật", "Phiên bản chữ ký", "Tuổi chữ ký (ngày)", "Quét nhanh gần nhất");

        var s = ctx.Wmi.Query("MSFT_MpComputerStatus", null, "root\\Microsoft\\Windows\\Defender").FirstOrDefault();
        if (s is null)
        {
            ctx.Warn("Không đọc được trạng thái Windows Defender (có thể đã bị thay bằng AV khác, hoặc thiếu quyền Administrator).");
            status.AddRow("Không lấy được", "-", "-", "-", "-");
        }
        else
        {
            var quick = WmiDateParser.Parse(s.Str("QuickScanEndTime", ""));
            status.AddRow(
                s.Bool("RealTimeProtectionEnabled") == true ? "Đang bật" : "ĐANG TẮT - nên bật lại ngay",
                s.Bool("AntivirusEnabled") == true ? "Có" : "Không",
                s.Str("AntivirusSignatureVersion"),
                s.Num("AntivirusSignatureAge"),
                quick?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Chưa từng quét");
        }

        var threats = DataTable.Create("Lịch sử phát hiện mối đe dọa (file .exe, .doc, ... đã bị Defender gắn cờ)",
            "Tên mối đe dọa", "File/Đường dẫn", "Thời gian phát hiện", "Đã xử lý");
        threats.Searchable = true;

        var detections = ctx.Wmi.Query("MSFT_MpThreatDetection", null, "root\\Microsoft\\Windows\\Defender");
        int count = 0;
        foreach (var d in detections)
        {
            count++;
            var resources = d.TryGetValue("Resources", out var res) && res is string[] arr && arr.Length > 0
                ? string.Join("; ", arr) : "-";
            threats.AddRow(
                d.Str("ThreatName", d.Str("ThreatID")),
                resources,
                WmiDateParser.Parse(d.Str("InitialDetectionTime", ""))?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-",
                d.Bool("ActionSuccess") == true ? "Có" : "Chưa/Không rõ");
        }
        threats.AddEmptyNotice("Không phát hiện mối đe dọa nào trong lịch sử quét của Windows Defender");

        return (status, threats, count);
    }
}

/// <summary>Kiem tra toan ven file he thong/driver qua DISM va SFC.</summary>
public static class SystemIntegrityCollector
{
    public static DataTable Collect(SystemContext ctx, bool runDism, bool runSfc)
    {
        var t = DataTable.Create("", "Công cụ kiểm tra", "Kết quả", "Thời gian chạy");

        if (runDism)
        {
            var r = ctx.Process.Run("DISM.exe", "/Online /Cleanup-Image /CheckHealth", timeoutSeconds: 180);
            t.AddRow("DISM CheckHealth", InterpretDism(r), "Nhanh (<5 giây)");
        }

        if (runSfc)
        {
            var r = ctx.Process.Run("sfc.exe", "/verifyonly", timeoutSeconds: 1800);
            t.AddRow("System File Checker (sfc /verifyonly)", InterpretSfc(r), "Chậm (5-15 phút)");
        }

        t.AddEmptyNotice("Đã tắt trong cấu hình - bật lại bằng tham số --dism / --sfc");
        return t;
    }

    public static string InterpretDism(ProcessResult r)
    {
        var text = r.CombinedOutput;
        if (string.IsNullOrWhiteSpace(text))
            return "Không chạy được (có thể cần quyền Administrator)";
        if (text.Contains("No component store corruption", StringComparison.OrdinalIgnoreCase))
            return "Không phát hiện lỗi (Healthy)";
        if (text.Contains("repairable", StringComparison.OrdinalIgnoreCase))
            return "CÓ THỂ SỬA ĐƯỢC - phát hiện lỗi component store, nên chạy DISM /RestoreHealth";
        if (text.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
            return "PHÁT HIỆN LỖI - nên chạy DISM /RestoreHealth";
        return "Không xác định được kết quả";
    }

    public static string InterpretSfc(ProcessResult r)
    {
        var text = r.CombinedOutput;
        if (string.IsNullOrWhiteSpace(text))
            return "Không chạy được (có thể cần quyền Administrator)";
        if (text.Contains("did not find any integrity violations", StringComparison.OrdinalIgnoreCase))
            return "Không phát hiện file hệ thống bị hỏng/thiếu";
        if (text.Contains("found corrupt files", StringComparison.OrdinalIgnoreCase))
            return "PHÁT HIỆN FILE HỆ THỐNG BỊ HỎNG/THIẾU - nên chạy 'sfc /scannow' với quyền Administrator";
        return "Không xác định được kết quả";
    }
}
