using System.Globalization;
using System.Text.RegularExpressions;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;

namespace Tsudev.Audit.Core.Collectors;

/// <summary>Thong tin he dieu hanh + may.</summary>
public static class OsInfoCollector
{
    public static DataTable Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", "Tên máy", "Hệ điều hành", "Phiên bản (Build)", "Kiến trúc",
            "Nhà sản xuất máy", "Model", "BIOS Serial", "Ngày cài đặt OS", "Thời gian quét");

        var os = ctx.Wmi.Query("Win32_OperatingSystem").FirstRowOrNull();
        var cs = ctx.Wmi.Query("Win32_ComputerSystem").FirstRowOrNull();
        var bios = ctx.Wmi.Query("Win32_BIOS").FirstRowOrNull();

        if (os is null) ctx.Warn("Không đọc được Win32_OperatingSystem (thông tin hệ điều hành có thể thiếu).");

        string installDate = "-";
        if (os is not null)
        {
            var raw = os.Str("InstallDate", "");
            var parsed = WmiDateParser.Parse(raw);
            if (parsed.HasValue) installDate = parsed.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        t.AddRow(
            ctx.ComputerName,
            os?.Str("Caption") ?? "-",
            os is null ? "-" : $"{os.Str("Version")} (Build {os.Str("BuildNumber")})",
            os?.Str("OSArchitecture") ?? "-",
            cs?.Str("Manufacturer") ?? "-",
            cs?.Str("Model") ?? "-",
            bios?.Str("SerialNumber") ?? "-",
            installDate,
            ctx.ScanTime.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        return t;
    }
}

/// <summary>Chuyen dinh dang ngay WMI (CIM_DATETIME) sang DateTime.</summary>
public static class WmiDateParser
{
    /// <summary>
    /// Dinh dang CIM_DATETIME: yyyyMMddHHmmss.ffffff+UUU
    /// Tra ve null neu chuoi rong/khong hop le (KHONG nem exception).
    /// </summary>
    public static DateTime? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 14) return null;
        var s = value.Trim();
        if (!int.TryParse(s.AsSpan(0, 4), out var year) ||
            !int.TryParse(s.AsSpan(4, 2), out var month) ||
            !int.TryParse(s.AsSpan(6, 2), out var day) ||
            !int.TryParse(s.AsSpan(8, 2), out var hour) ||
            !int.TryParse(s.AsSpan(10, 2), out var min) ||
            !int.TryParse(s.AsSpan(12, 2), out var sec)) return null;
        if (year is < 1601 or > 9999 || month is < 1 or > 12 || day is < 1 or > 31) return null;
        try { return new DateTime(year, month, day, hour, min, sec); }
        catch { return null; }
    }
}

/// <summary>Trang thai kich hoat ban quyen Windows (SoftwareLicensingProduct).</summary>
public static class WindowsLicenseCollector
{
    /// <summary>Ma trang thai license cua Windows -> mo ta tieng Viet.</summary>
    public static string DescribeStatus(long? code) => code switch
    {
        0 => "Unlicensed (Chưa kích hoạt)",
        1 => "Licensed (Đã kích hoạt)",
        2 => "OOB Grace (Đang trong thời gian dùng thử)",
        3 => "OOT Grace (Hết hạn dùng thử)",
        4 => "Non-Genuine Grace (Nghi ngờ không hợp lệ)",
        5 => "Notification (Chưa kích hoạt - đã hết hạn thông báo)",
        6 => "Extended Grace (Gia hạn mở rộng)",
        _ => "Không xác định"
    };

    /// <summary>
    /// Xep ma trang thai license vao mot trong ba nhom de ket luan.
    /// Phan nhom nay quyet dinh ket luan cuoi cung nen phai ro rang.
    /// </summary>
    public static LicenseHealth Classify(long? code) => code switch
    {
        1 => LicenseHealth.Ok,                                  // Licensed

        // Ba trang thai duoi day deu nghia la KHONG kich hoat hop le.
        0 or 4 or 5 => LicenseHealth.Problem,                   // Unlicensed / Non-Genuine Grace / Notification

        // Con han dung thu: chua sai, nhung cung CHUA phai hop le.
        2 or 3 or 6 => LicenseHealth.Grace,

        _ => LicenseHealth.Unknown
    };

    public static (DataTable Table, WindowsLicenseSummary Summary) Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", "Sản phẩm", "Mô tả", "Trạng thái",
            "Product Key (5 ký tự cuối)", "Kênh license");

        // Chi lay san pham co PartialProductKey (tuc la co license thuc su gan
        // vao may), tranh liet ke hang chuc SKU rong ma Windows luon khai bao.
        var rows = ctx.Wmi.Query("SoftwareLicensingProduct",
            "PartialProductKey IS NOT NULL AND ApplicationID='55c92734-d682-4d71-983e-d6ec3f16059f'");

        if (rows.Count == 0)
            ctx.Warn("Không đọc được SoftwareLicensingProduct (cần quyền Administrator để thấy đầy đủ).");

        int ok = 0, problem = 0, grace = 0, unknown = 0;
        foreach (var r in rows)
        {
            var status = r.Num("LicenseStatus");
            switch (Classify(status))
            {
                case LicenseHealth.Ok: ok++; break;
                case LicenseHealth.Problem: problem++; break;
                case LicenseHealth.Grace: grace++; break;
                default: unknown++; break;
            }

            t.AddRow(
                r.Str("Name"),
                r.Str("Description"),
                DescribeStatus(status),
                r.Str("PartialProductKey"),
                r.Str("ProductKeyChannel"));
        }

        t.AddEmptyNotice("Không tìm thấy sản phẩm Windows nào có license gắn vào máy");
        return (t, new WindowsLicenseSummary(rows.Count, ok, problem, grace, unknown));
    }
}

/// <summary>Nhom trang thai license, dung de ket luan.</summary>
public enum LicenseHealth { Unknown, Ok, Grace, Problem }

/// <summary>
/// Tong hop trang thai license Windows tren may.
///
/// LUU Y VE MOT LOI DA TUNG MAC: ban dau ket luan duoc suy ra bang
/// <c>licensedCount &gt; 0</c> - tuc chi can MOT SKU bat ky o trang thai
/// Licensed la ket luan "hop le". Windows khai bao NHIEU SKU duoi cung mot
/// ApplicationID, nen mot may co SKU chinh dang Notification/Unlicensed nhung
/// co mot SKU phu Licensed van bi cham diem "hop le".
///
/// Voi cong cu nay, bao NHAM LA HOP LE la kieu sai te nhat: bao cao co the bi
/// mang ra lam can cu trong tranh chap lao dong hoac thanh tra. Vi vay quy tac
/// nay co chu dich THAN TRONG - chi ket luan hop le khi KHONG con SKU nao o
/// trang thai co van de.
/// </summary>
public sealed record WindowsLicenseSummary(
    int Total, int Ok, int Problem, int Grace, int Unknown)
{
    /// <summary>Co du lieu de ket luan hay khong.</summary>
    public bool IsKnown => Total > 0;

    /// <summary>
    /// Chi hop le khi co it nhat mot SKU Licensed VA khong con SKU nao o
    /// trang thai co van de hoac dang trong thoi gian dung thu.
    /// </summary>
    public bool IsGenuine => Total > 0 && Ok > 0 && Problem == 0 && Grace == 0;

    /// <summary>Co it nhat mot SKU o trang thai chac chan khong hop le.</summary>
    public bool HasProblem => Problem > 0;

    public LicenseHealth Overall
        => !IsKnown ? LicenseHealth.Unknown
         : HasProblem ? LicenseHealth.Problem
         : Grace > 0 || Unknown > 0 ? LicenseHealth.Grace
         : Ok > 0 ? LicenseHealth.Ok
         : LicenseHealth.Unknown;

    public string Describe()
        => $"{Total} sản phẩm: {Ok} hợp lệ, {Problem} có vấn đề, {Grace} đang dùng thử, {Unknown} không xác định";
}

/// <summary>
/// Ban quyen Microsoft Office/M365 qua cong cu chinh thuc ospp.vbs.
/// Neu may khong cai Office -> tra ve bang rong (KHONG phai loi).
/// </summary>
public static class OfficeLicenseCollector
{
    private static readonly string[] OsppCandidates =
    {
        @"%ProgramFiles%\Microsoft Office\Office16\ospp.vbs",
        @"%ProgramFiles%\Microsoft Office\Office15\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\Office16\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\Office15\ospp.vbs",
    };

    public static DataTable Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", "Sản phẩm", "Mô tả", "Trạng thái", "SKU ID");

        string? ospp = null;
        foreach (var candidate in OsppCandidates)
        {
            var expanded = ctx.Files.ExpandEnvironment(candidate);
            if (ctx.Files.FileExists(expanded)) { ospp = expanded; break; }
        }

        if (ospp is null)
        {
            t.AddEmptyNotice("Không tìm thấy Office/M365 cài đặt trên máy (đây KHÔNG phải lỗi)");
            return t;
        }

        var result = ctx.Process.Run("cscript.exe", $"//Nologo \"{ospp}\" /dstatus", timeoutSeconds: 90);
        if (!result.Success && string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            ctx.Warn("Không chạy được ospp.vbs để kiểm tra bản quyền Office.");
            t.AddEmptyNotice("Không lấy được trạng thái bản quyền Office");
            return t;
        }

        foreach (var item in ParseOsppOutput(result.StandardOutput))
            t.AddRow(item.Name, item.Description, item.Status, item.SkuId);

        t.AddEmptyNotice("Không đọc được sản phẩm Office nào từ ospp.vbs");
        return t;
    }

    public sealed record OfficeLicenseItem(string Name, string Description, string Status, string SkuId);

    /// <summary>
    /// Phan tich output cua "ospp.vbs /dstatus". Tach rieng thanh ham public
    /// de UNIT TEST duoc bang du lieu mau, khong can cai Office.
    /// </summary>
    public static List<OfficeLicenseItem> ParseOsppOutput(string raw)
    {
        var items = new List<OfficeLicenseItem>();
        if (string.IsNullOrWhiteSpace(raw)) return items;

        // Cac khoi san pham cach nhau bang duong ke "-----"
        var blocks = Regex.Split(raw, @"-{5,}");
        foreach (var block in blocks)
        {
            if (!block.Contains("LICENSE NAME", StringComparison.OrdinalIgnoreCase)) continue;

            var name = Match1(block, @"LICENSE NAME:\s*(.+)");
            var desc = Match1(block, @"LICENSE DESCRIPTION:\s*(.+)");
            var sku = Match1(block, @"SKU ID:\s*(\S+)");
            var statusRaw = Match1(block, @"LICENSE STATUS:\s*-*\s*([A-Z_]+)\s*-*");

            items.Add(new OfficeLicenseItem(
                string.IsNullOrWhiteSpace(name) ? "-" : name,
                string.IsNullOrWhiteSpace(desc) ? "-" : desc,
                DescribeOsppStatus(statusRaw),
                string.IsNullOrWhiteSpace(sku) ? "-" : sku));
        }
        return items;
    }

    public static string DescribeOsppStatus(string? statusRaw) => (statusRaw ?? "").ToUpperInvariant() switch
    {
        "LICENSED" => "Licensed (Đã kích hoạt)",
        "OOB_GRACE" => "Grace period (Chưa kích hoạt - còn hạn dùng thử)",
        "OOT_GRACE" => "Out-of-tolerance grace (Cảnh báo - có thể do thay đổi phần cứng)",
        "NOTIFICATIONS" => "Notification (Chưa kích hoạt - đã hết hạn thông báo)",
        "NON_GENUINE_GRACE" => "Non-genuine grace (Nghi ngờ không hợp lệ)",
        "UNLICENSED" => "Unlicensed (Chưa kích hoạt)",
        "" => "Không xác định",
        var other => $"Không xác định ({other})"
    };

    private static string Match1(string input, string pattern)
    {
        var m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }
}

/// <summary>Lay output tho cua slmgr.vbs /dli de hien trong khoi &lt;pre&gt;.</summary>
public static class SlmgrCollector
{
    public static string Collect(SystemContext ctx)
    {
        var slmgr = ctx.Files.ExpandEnvironment(@"%SystemRoot%\System32\slmgr.vbs");
        if (!ctx.Files.FileExists(slmgr))
            return "Không tìm thấy slmgr.vbs trên máy này.";

        var result = ctx.Process.Run("cscript.exe", $"//Nologo \"{slmgr}\" /dli", timeoutSeconds: 90);
        var text = result.CombinedOutput.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? "Không lấy được kết quả từ slmgr.vbs /dli (có thể cần quyền Administrator)."
            : text;
    }
}
