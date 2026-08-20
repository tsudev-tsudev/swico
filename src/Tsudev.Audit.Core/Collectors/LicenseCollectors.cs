using System.Text.RegularExpressions;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

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
            if (parsed.HasValue) installDate = DateDisplay.Date(parsed.Value);
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
            DateDisplay.DateTimeText(ctx.ScanTime));

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
    /// <summary>ApplicationID cua Microsoft Office trong SoftwareLicensingProduct.</summary>
    public const string OfficeApplicationId = "0ff1ce15-a989-479d-af46-f275c6370663";

    /// <summary>
    /// Cac vi tri co the co ospp.vbs.
    ///
    /// LUU Y: ban Click-to-Run (moi ban Office 2016 tro di, gom ca cac ban le
    /// nhu Office 16 HomePremR) cai vao thu muc "...\Microsoft Office\root\Office16",
    /// KHONG phai "...\Microsoft Office\Office16". Thieu cac duong dan "root"
    /// nay la ly do Office cai san tren may hang thuong bi bao la "khong tim
    /// thay Office" - mot diem mu that su, khong phai truong hop hiem.
    /// </summary>
    private static readonly string[] OsppCandidates =
    {
        @"%ProgramFiles%\Microsoft Office\root\Office16\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\root\Office16\ospp.vbs",
        @"%ProgramFiles%\Microsoft Office\Office16\ospp.vbs",
        @"%ProgramFiles%\Microsoft Office\Office15\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\Office16\ospp.vbs",
        @"%ProgramFiles(x86)%\Microsoft Office\Office15\ospp.vbs",
    };

    public static (DataTable Table, OfficeLicenseSummary Summary) Collect(SystemContext ctx)
    {
        var t = DataTable.Create("", "Sản phẩm", "Mô tả", "Trạng thái", "SKU ID", "Nguồn dữ liệu");
        int ok = 0, problem = 0, grace = 0, unknown = 0;

        void Count(LicenseHealth h)
        {
            switch (h)
            {
                case LicenseHealth.Ok: ok++; break;
                case LicenseHealth.Problem: problem++; break;
                case LicenseHealth.Grace: grace++; break;
                default: unknown++; break;
            }
        }

        // NGUON 1 - WMI SoftwareLicensingProduct.
        // Doc truoc vi day la nguon dang tin nhat: khong phu thuoc vao viec
        // tim thay ospp.vbs, va cac ban Office le/Click-to-Run deu dang ky o day.
        var wmiRows = ctx.Wmi.Query("SoftwareLicensingProduct",
            $"PartialProductKey IS NOT NULL AND ApplicationID='{OfficeApplicationId}'");

        foreach (var r in wmiRows)
        {
            var status = r.Num("LicenseStatus");
            Count(WindowsLicenseCollector.Classify(status));
            t.AddRow(
                r.Str("Name"),
                r.Str("Description"),
                WindowsLicenseCollector.DescribeStatus(status),
                r.Str("ID"),
                "WMI SoftwareLicensingProduct");
        }

        // NGUON 2 - ospp.vbs, cong cu chinh thuc di kem bo cai Office.
        // Bo sung cho nguon 1: co the hien thi them san pham ma WMI khong bat.
        string? ospp = null;
        foreach (var candidate in OsppCandidates)
        {
            var expanded = ctx.Files.ExpandEnvironment(candidate);
            if (ctx.Files.FileExists(expanded)) { ospp = expanded; break; }
        }

        if (ospp is not null)
        {
            var result = ctx.Process.Run("cscript.exe", $"//Nologo \"{ospp}\" /dstatus",
                timeoutSeconds: 90, cancellation: ctx.Cancellation);
            if (!result.Success && string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                ctx.Warn("Không chạy được ospp.vbs để kiểm tra bản quyền Office.");
            }
            else
            {
                foreach (var item in ParseOsppOutput(result.StandardOutput))
                {
                    Count(ClassifyOspp(item.Status));
                    t.AddRow(item.Name, item.Description, item.Status, item.SkuId, "ospp.vbs");
                }
            }
        }
        else if (wmiRows.Count > 0)
        {
            // Co du lieu WMI nhung khong tim thay ospp.vbs: KHONG duoc ket luan
            // la may khong cai Office.
            ctx.Warn("Không tìm thấy ospp.vbs (thường gặp với bản Office Click-to-Run). " +
                     "Trạng thái Office lấy từ WMI.");
        }

        t.AddEmptyNotice("Không tìm thấy Office/M365 cài đặt trên máy (đây KHÔNG phải lỗi)");
        return (t, new OfficeLicenseSummary(ok + problem + grace + unknown, ok, problem, grace, unknown));
    }

    /// <summary>Xep chuoi trang thai cua ospp.vbs vao nhom suc khoe license.</summary>
    public static LicenseHealth ClassifyOspp(string? describedStatus)
    {
        var s = (describedStatus ?? "").ToUpperInvariant();
        if (s.Contains("NON-GENUINE", StringComparison.Ordinal)
            || s.Contains("NOTIFICATION", StringComparison.Ordinal)
            || s.Contains("UNLICENSED", StringComparison.Ordinal)) return LicenseHealth.Problem;
        if (s.Contains("GRACE", StringComparison.Ordinal)) return LicenseHealth.Grace;
        if (s.Contains("LICENSED", StringComparison.Ordinal)) return LicenseHealth.Ok;
        return LicenseHealth.Unknown;
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

        var result = ctx.Process.Run("cscript.exe", $"//Nologo \"{slmgr}\" /dli",
            timeoutSeconds: 90, cancellation: ctx.Cancellation);
        var text = result.CombinedOutput.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? "Không lấy được kết quả từ slmgr.vbs /dli (có thể cần quyền Administrator)."
            : text;
    }
}

/// <summary>
/// Tong hop trang thai ban quyen Office.
///
/// Vi sao Office phai co tieng noi trong ket luan: mot may co Windows hop le
/// nhung Office CHUA KICH HOAT van la may co van de ve ban quyen. Truoc day
/// trang thai Office duoc thu thap va hien thi nhung KHONG he anh huong toi
/// ket luan tong the - nen bao cao ket luan "khong phat hien dau hieu" trong
/// khi Office dang o trang thai Notification.
/// </summary>
public sealed record OfficeLicenseSummary(
    int Total, int Ok, int Problem, int Grace, int Unknown)
{
    public bool IsInstalled => Total > 0;
    public bool HasProblem => Problem > 0;

    public LicenseHealth Overall
        => Total == 0 ? LicenseHealth.Unknown
         : Problem > 0 ? LicenseHealth.Problem
         : Grace > 0 ? LicenseHealth.Grace
         : Ok > 0 ? LicenseHealth.Ok
         : LicenseHealth.Unknown;

    public string Describe()
        => Total == 0
            ? "Không phát hiện Office trên máy"
            : $"{Total} sản phẩm: {Ok} hợp lệ, {Problem} có vấn đề, {Grace} đang dùng thử, {Unknown} không xác định";
}
