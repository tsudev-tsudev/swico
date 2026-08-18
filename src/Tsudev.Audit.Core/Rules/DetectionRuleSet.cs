using System.Reflection;
using System.Text.Json;
using Tsudev.Audit.Core.Serialization;

namespace Tsudev.Audit.Core.Rules;

/// <summary>
/// Bo luat phat hien dau hieu kich hoat trai phep, TACH RA KHOI MA NGUON.
///
/// Vi sao tach? Ma nguon du an nay se cong khai (dieu kien bat buoc de duoc
/// cap chung chi ky so mien phi). Nghia la nguoi viet cong cu crack doc duoc
/// chinh xac cac dau hieu dang bi ra soat va ne chung.
///
/// Tach luat thanh DU LIEU CO PHIEN BAN lam viec ne do mat gia tri lau dai:
/// luat cap nhat duoc doc lap voi ban exe - khong can bien dich lai, khong can
/// ky lai, khong can phat hanh lai. Nguoi dung chi can thay mot file JSON.
///
/// Day KHONG phai bao mat bang che giau. Luat van cong khai; cai thay doi la
/// TOC DO cap nhat.
/// </summary>
public sealed class DetectionRuleSet
{
    /// <summary>Ten file luat ben ngoai duoc tim tu dong canh file exe.</summary>
    public const string ConventionalFileName = "detection-rules.json";

    private const string EmbeddedResourceName =
        "Tsudev.Audit.Core.Rules.detection-rules.json";

    /// <summary>Phien ban bo luat, hien trong bao cao de truy nguoc duoc.</summary>
    public string Version { get; set; } = "";

    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>Ghi chu tu do, khong anh huong hanh vi.</summary>
    public string? Notes { get; set; }

    /// <summary>Cac thu muc goc se duyet tim ten nghi van.</summary>
    public List<string> ScanRoots { get; set; } = new();

    /// <summary>Ten dac trung cua cong cu kich hoat trai phep pho bien.</summary>
    public List<string> SuspiciousNames { get; set; } = new();

    /// <summary>
    /// Task hop le cua Windows - phai loai tru de KHONG bao dong nham.
    /// Vi du: task gia han license KMS cua doanh nghiep la hoan toan hop phap.
    /// </summary>
    public List<string> LegitimateTaskNames { get; set; } = new();

    public List<string> HookDirectories { get; set; } = new();

    /// <summary>File thay the truc tiep thanh phan loi bao ve ban quyen.</summary>
    public List<string> HookFiles { get; set; } = new();

    /// <summary>May chu KMS cong cong da biet, xuat hien trong hosts file.</summary>
    public List<string> KnownKmsHosts { get; set; } = new();

    /// <summary>Tu khoa cho thay hosts file dang chan may chu xac thuc.</summary>
    public List<string> HostsInterferenceKeywords { get; set; } = new();

    // ------------------------------------------------------------------ nap

    private static readonly Lazy<DetectionRuleSet> LazyEmbedded = new(LoadEmbedded);

    /// <summary>
    /// Bo luat dong kem trong file exe. Luon dung duoc, khong phu thuoc file
    /// ben ngoai - cong cu phai chay duoc ngay ca khi chi copy moi mot file exe.
    /// </summary>
    public static DetectionRuleSet Embedded => LazyEmbedded.Value;

    private static DetectionRuleSet LoadEmbedded()
    {
        var assembly = typeof(DetectionRuleSet).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Khong tim thay tai nguyen nhung '{EmbeddedResourceName}'. " +
                "Kiem tra muc EmbeddedResource trong Tsudev.Audit.Core.csproj.");

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static DetectionRuleSet Parse(string json)
        => AuditJson.ReadRules(json)
           ?? throw new InvalidOperationException("Nội dung bộ luật rỗng.");

    /// <summary>
    /// Nap bo luat tu file ben ngoai. KHONG BAO GIO nem exception: file luat
    /// hong khong duoc lam hong ca lan quet - se quay ve dung bo luat nhung
    /// va ghi mot canh bao de nguoi doc bao cao biet dieu do da xay ra.
    /// </summary>
    public static DetectionRuleSet LoadOrEmbedded(string? path, out string? warning)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(path)) return Embedded;

        try
        {
            if (!File.Exists(path))
            {
                warning = $"Không tìm thấy file bộ luật '{path}'. Dùng bộ luật đóng kèm ({Embedded.Version}).";
                return Embedded;
            }

            var loaded = Parse(File.ReadAllText(path));
            var problems = loaded.Validate();
            if (problems.Count > 0)
            {
                warning = $"File bộ luật '{path}' không hợp lệ: {string.Join("; ", problems)}. " +
                          $"Dùng bộ luật đóng kèm ({Embedded.Version}).";
                return Embedded;
            }

            // CAM BAY DA NHAN DIEN: file luat ben ngoai LUON thang bo luat dong
            // kem. Sau khi nang cap len ban exe moi, mot file .json CU nam canh
            // exe se am tham VO HIEU HOA bo luat moi - va khong co dau hieu nao
            // cho thay dieu do. Vi vay moi lech phien ban deu phai duoc noi ro
            // trong bao cao, khong chi ghi ra man hinh.
            if (!string.Equals(loaded.Version, Embedded.Version, StringComparison.Ordinal))
            {
                warning = $"Đang dùng bộ luật từ file ngoài '{path}' (phiên bản {loaded.Version}), " +
                          $"KHÁC với bộ luật đóng kèm trong chương trình (phiên bản {Embedded.Version}). " +
                          "File ngoài luôn được ưu tiên. Nếu bạn vừa nâng cấp chương trình, " +
                          "hãy xoá hoặc cập nhật file này để dùng bộ luật mới.";
            }

            return loaded;
        }
        catch (JsonException ex)
        {
            warning = $"File bộ luật '{path}' sai định dạng JSON: {ex.Message}. Dùng bộ luật đóng kèm ({Embedded.Version}).";
            return Embedded;
        }
        catch (IOException ex)
        {
            warning = $"Không đọc được file bộ luật '{path}': {ex.Message}. Dùng bộ luật đóng kèm ({Embedded.Version}).";
            return Embedded;
        }
        catch (UnauthorizedAccessException ex)
        {
            warning = $"Không có quyền đọc file bộ luật '{path}': {ex.Message}. Dùng bộ luật đóng kèm ({Embedded.Version}).";
            return Embedded;
        }
    }

    /// <summary>
    /// Kiem tra bo luat co dung duoc khong. Tra ve danh sach van de; rong nghia
    /// la hop le.
    ///
    /// Bo luat rong nguy hiem hon bo luat sai: no khien moi may deu "sach",
    /// tao cam giac an toan gia. Vi vay thieu du lieu bi coi la LOI, khong
    /// phai canh bao.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(Version))
            problems.Add("thiếu trường 'version'");
        if (SuspiciousNames.Count == 0)
            problems.Add("'suspiciousNames' rỗng (sẽ không phát hiện được gì)");
        if (ScanRoots.Count == 0)
            problems.Add("'scanRoots' rỗng (sẽ không quét thư mục nào)");
        if (HookFiles.Count == 0)
            problems.Add("'hookFiles' rỗng");
        if (HookDirectories.Count == 0)
            problems.Add("'hookDirectories' rỗng");

        if (SuspiciousNames.Any(string.IsNullOrWhiteSpace))
            problems.Add("'suspiciousNames' chứa mục rỗng (sẽ khớp với mọi tên)");

        return problems;
    }

    /// <summary>Mo ta ngan gon de hien trong bao cao.</summary>
    public string Describe()
        => $"{Version} ({SuspiciousNames.Count} tên nghi vấn, {KnownKmsHosts.Count} máy chủ KMS, {HookFiles.Count} file hook)";
}
