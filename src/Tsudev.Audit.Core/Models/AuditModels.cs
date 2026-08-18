using System.Text.Json.Serialization;

namespace Tsudev.Audit.Core.Models;

/// <summary>
/// Loai bao cao. Dung lam "discriminator" khi Dashboard doc lai cac file JSON
/// nam rai rac trong nhieu thu muc con.
/// </summary>
public enum ReportKind
{
    LicenseAudit,
    HardwareInventory
}

/// <summary>
/// Muc do rui ro chuan hoa dung chung toan he thong. Dinh nghia MOT NOI duy
/// nhat de tranh tinh trang moi cho dung mot chuoi khac nhau (van de cua ban
/// PowerShell cu: "Rat cao"/"Nghiem trong"/"Cao" bi lan lon giua cac module).
/// </summary>
public enum RiskLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Ket luan tong the ve tinh hop phap cua ban quyen.
/// </summary>
public enum VerdictLevel
{
    Unknown,
    Ok,
    Warning,
    Bad
}

/// <summary>
/// Mot bang du lieu tong quat: tieu de cot + cac dong.
/// Thiet ke "generic table" nay cho phep renderer (HTML/XLSX/CSV) xu ly MOI
/// bang theo CUNG mot code path, thay vi phai viet rieng cho tung loai bang
/// nhu ban PowerShell cu.
/// </summary>
public sealed class DataTable
{
    /// <summary>Ten bang - dung lam tieu de section HTML va ten sheet XLSX.</summary>
    public string Title { get; set; } = "";

    /// <summary>Mo ta ngan hien duoi tieu de (tuy chon).</summary>
    public string? Description { get; set; }

    /// <summary>Ten cac cot, theo dung thu tu hien thi.</summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Cac dong du lieu. Moi dong la mang chuoi CUNG DO DAI voi Columns.
    /// Dung string cho moi o de dam bao render nhat quan; viec dinh dang so/
    /// ngay duoc lam TAI NOI THU THAP (collector), khong phai tai renderer.
    /// </summary>
    public List<string[]> Rows { get; set; } = new();

    /// <summary>Bat o tim kiem + sap xep cho bang nay (bang dai).</summary>
    public bool Searchable { get; set; }

    public static DataTable Create(string title, params string[] columns)
        => new() { Title = title, Columns = columns.ToList() };

    public DataTable AddRow(params object?[] values)
    {
        var row = new string[Columns.Count];
        for (int i = 0; i < Columns.Count; i++)
        {
            var v = i < values.Length ? values[i] : null;
            var s = v?.ToString();
            row[i] = string.IsNullOrWhiteSpace(s) ? "-" : s;
        }
        Rows.Add(row);
        return this;
    }

    /// <summary>Dong "khong co du lieu" de bang khong bi trong tron.</summary>
    public DataTable AddEmptyNotice(string message)
    {
        if (Rows.Count > 0) return this;
        var row = new string[Columns.Count];
        for (int i = 0; i < Columns.Count; i++) row[i] = "-";
        if (Columns.Count > 1) row[1] = message; else if (Columns.Count > 0) row[0] = message;
        Rows.Add(row);
        return this;
    }
}

/// <summary>Mot phat hien rui ro cu the (dau hieu kich hoat trai phep...).</summary>
public sealed class RiskFinding
{
    public string Category { get; set; } = "";
    public string Detection { get; set; } = "";
    public RiskLevel Level { get; set; }
    public string Explanation { get; set; } = "";
}

/// <summary>
/// Mot muc trong bang tra cuu "pham vi quet" - luon hien thi DU CO PHAT HIEN
/// HAY KHONG, de nguoi doc bao cao biet script da kiem tra nhung gi.
/// </summary>
public sealed class DetectionScope
{
    public string Category { get; set; } = "";
    public string DataSource { get; set; } = "";
    public RiskLevel LevelIfFound { get; set; }
    public int FoundCount { get; set; }
}

/// <summary>Ket luan danh gia tinh hop phap.</summary>
public sealed class Verdict
{
    public VerdictLevel Level { get; set; } = VerdictLevel.Unknown;
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
}

/// <summary>Diem rui ro tong hop 0-100.</summary>
public sealed class RiskScore
{
    public int Value { get; set; }
    public RiskLevel Band { get; set; }
    public string Label { get; set; } = "";
}

/// <summary>
/// Bao cao hoan chinh cua MOT lan quet. Day chinh la "hop dong" (contract)
/// duoc serialize ra JSON - Dashboard doc lai chinh cau truc nay, nen moi
/// thay doi o day deu phai tang SchemaVersion.
/// </summary>
public sealed class AuditReport
{
    /// <summary>Tang so nay khi thay doi cau truc gay pha vo tuong thich.</summary>
    public const string CurrentSchemaVersion = "3.0";

    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReportKind ReportKind { get; set; }

    public string ComputerName { get; set; } = "";
    public DateTimeOffset ScanTime { get; set; }

    /// <summary>Ten file HTML tuong ung (de Dashboard tao link).</summary>
    public string HtmlReportFile { get; set; } = "";

    /// <summary>Tieu de hien thi tren dau bao cao.</summary>
    public string Title { get; set; } = "";

    /// <summary>Cac the thong ke hien o dau bao cao.</summary>
    public List<SummaryCard> SummaryCards { get; set; } = new();

    /// <summary>Toan bo cac bang du lieu, theo dung thu tu hien thi.</summary>
    public List<ReportSection> Sections { get; set; } = new();

    // --- Cac truong tom tat de Dashboard doc NHANH ma khong phai duyet het Sections ---
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VerdictLevel VerdictLevel { get; set; } = VerdictLevel.Unknown;

    public string? VerdictText { get; set; }
    public int RiskFindingsCount { get; set; }
    public RiskScore? RiskScore { get; set; }

    /// <summary>Tom tat cau hinh phan cung 1 dong (Dashboard hien o cot rieng).</summary>
    public string? HardwareSummary { get; set; }

    /// <summary>
    /// Phien ban bo luat phat hien da dung. Ghi lai de sau nay truy nguoc duoc
    /// mot ket luan la do bo luat nao sinh ra - luat cap nhat doc lap voi exe
    /// nen chi biet phien ban exe la khong du.
    /// </summary>
    public string? DetectionRulesVersion { get; set; }

    /// <summary>Canh bao/loi khong nghiem trong gap khi thu thap (de minh bach).</summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>The thong ke o dau bao cao.</summary>
public sealed class SummaryCard
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

/// <summary>
/// Mot muc (section) trong bao cao: co the chua nhieu bang + ghi chu phuong phap.
/// </summary>
public sealed class ReportSection
{
    /// <summary>Id dung cho anchor #... tren thanh dieu huong.</summary>
    public string Id { get; set; } = "";

    /// <summary>Nhan hien tren thanh dieu huong (ngan).</summary>
    public string NavLabel { get; set; } = "";

    /// <summary>Tieu de day du hien trong section.</summary>
    public string Heading { get; set; } = "";

    public string? Description { get; set; }

    public List<DataTable> Tables { get; set; } = new();

    /// <summary>Khoi "Ve phuong phap va gioi han" cuoi section.</summary>
    public string? MethodNote { get; set; }

    /// <summary>Banner ket luan (chi dung o section danh gia hop phap).</summary>
    public Verdict? Verdict { get; set; }

    /// <summary>Van ban tho hien trong khoi &lt;pre&gt; (vi du output slmgr).</summary>
    public string? PreformattedText { get; set; }

    /// <summary>Cac chip/badge nho hien dau section.</summary>
    public List<Badge> Badges { get; set; } = new();
}

/// <summary>Chip mau nho (badge) hien trang thai.</summary>
public sealed class Badge
{
    public string Text { get; set; } = "";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RiskLevel Level { get; set; } = RiskLevel.None;
}
