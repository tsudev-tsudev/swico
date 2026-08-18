using Tsudev.Audit.Core.Models;

namespace Tsudev.Audit.Core.Cli;

/// <summary>
/// Ma thoat va cach suy ra chung.
///
/// Nam trong Core (khong phu thuoc he dieu hanh) CO CHU DICH: day la logic
/// nghiep vu - no quyet dinh mot he thong giam sat se bao dong hay im lang.
/// Khi con nam trong lop Cli (net8.0-windows), bo test chay tren Linux khong
/// voi toi duoc, va thuc te no da KHONG duoc kiem thu dong nao - trong khi
/// README lai tuyen bo "CLI parse tham so: 16/16 test".
/// </summary>
public static class ExitCodes
{
    // Nhom SUC KHOE CONG CU - cong cu chay co tron ven khong
    public const int Ok = 0;
    public const int Partial = 1;
    public const int Fatal = 2;
    public const int BadArgs = 3;

    // Nhom KET LUAN DANH GIA - may duoc quet co van de khong
    public const int VerdictWarning = 10;
    public const int VerdictCritical = 20;

    /// <summary>Ma thoat suy ra tu ket luan XAU NHAT trong cac bao cao da tao.</summary>
    public static int FromVerdicts(IEnumerable<VerdictLevel> verdicts)
    {
        ArgumentNullException.ThrowIfNull(verdicts);

        var list = verdicts as ICollection<VerdictLevel> ?? verdicts.ToList();
        if (list.Contains(VerdictLevel.Bad)) return VerdictCritical;
        if (list.Contains(VerdictLevel.Warning)) return VerdictWarning;
        return Ok;
    }

    /// <summary>
    /// Gop ma suc khoe cong cu voi ma ket luan danh gia.
    ///
    /// Thu tu uu tien: Fatal &gt; BadArgs &gt; VerdictCritical &gt; VerdictWarning &gt; Partial &gt; Ok.
    /// Ket luan danh gia thang "thieu du lieu" vi mot may co dau hieu kich hoat
    /// trai phep can duoc bao dong hon la mot muc thu thap bi trong.
    /// </summary>
    public static int Combine(int toolHealth, int verdict)
    {
        if (toolHealth is Fatal or BadArgs) return toolHealth;
        return verdict != Ok ? verdict : toolHealth;
    }

    public static string Describe(int code) => code switch
    {
        Ok => "không phát hiện vấn đề",
        Partial => "hoàn tất nhưng thiếu dữ liệu ở một số mục",
        Fatal => "lỗi nghiêm trọng, không tạo được báo cáo",
        BadArgs => "tham số dòng lệnh không hợp lệ",
        VerdictWarning => "kết luận mức CẢNH BÁO",
        VerdictCritical => "kết luận mức NGHIÊM TRỌNG",
        _ => "không xác định"
    };
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
    public bool ShowVersion { get; private set; }

    /// <summary>
    /// Khong cho ket luan danh gia anh huong toi ma thoat.
    /// Danh cho cac he RMM coi MOI ma khac 0 la "script chay loi" - khi do mot
    /// may co Office chua kich hoat se bi bao nham thanh su co cong cu.
    /// </summary>
    public bool NoVerdictExit { get; private set; }

    /// <summary>Duong dan file bo luat phat hien ben ngoai (tuy chon).</summary>
    public string? RulesPath { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i].ToLowerInvariant();
            switch (a)
            {
                case "-h" or "--help" or "/?": o.ShowHelp = true; break;
                case "--version": o.ShowVersion = true; break;
                case "--no-verdict-exit": o.NoVerdictExit = true; break;
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

                case "--rules":
                    if (++i >= args.Length) throw new ArgumentException("--rules cần đường dẫn tới file .json bộ luật.");
                    o.RulesPath = args[i];
                    break;

                default:
                    throw new ArgumentException($"Không nhận ra tham số '{args[i]}'.");
            }
        }
        return o;
    }

    public static void PrintUsage() => Console.WriteLine(UsageText);

    /// <summary>Toan van phan tro giup - tach ra de kiem thu duoc.</summary>
    public static string UsageText => """
tsuowlit SWICO - Kiểm tra bản quyền Windows & cấu hình phần cứng

CÁCH DÙNG:
  swico.exe [tham số]

THAM SỐ:
  --scope <all|license|hardware>  Phạm vi quét (mặc định: all - quét cả hai)
  -o, --output <thư mục>          Thư mục gốc lưu kết quả (mặc định: cạnh file exe)
  -s, --silent                    Không tự mở trình duyệt (dùng cho GPO/RMM/Task Scheduler)
      --sfc                       Chạy thêm sfc /verifyonly (CHẬM 5-15 phút, mặc định tắt)
      --no-dism                   Bỏ qua DISM CheckHealth
      --no-csv                    Không xuất file .csv
      --rules <file.json>         Dùng bộ luật phát hiện từ file ngoài
      --no-verdict-exit           Kết luận đánh giá KHÔNG ảnh hưởng mã thoát
  -v, --verbose                   Hiện chi tiết lỗi
      --version                   Hiện phiên bản rồi thoát
  -h, --help                      Hiện trợ giúp này

BỘ LUẬT PHÁT HIỆN:
  Các dấu hiệu kích hoạt trái phép nằm trong một file dữ liệu riêng, cập nhật
  được độc lập với file exe - không cần cài lại. Thứ tự ưu tiên:
    1. File chỉ định bằng --rules
    2. File detection-rules.json đặt cạnh swico.exe
    3. Bộ luật đóng kèm bên trong exe
  File hỏng hoặc thiếu sẽ tự quay về bộ luật đóng kèm kèm một cảnh báo,
  KHÔNG làm hỏng lần quét.

MÃ THOÁT: chia hai nhóm, KHÔNG gộp chung

  Sức khoẻ công cụ - công cụ chạy có trọn vẹn không:
    0   Hoàn tất, không phát hiện vấn đề
    1   Hoàn tất nhưng thiếu dữ liệu ở một số mục (báo cáo chính vẫn đầy đủ)
    2   Lỗi nghiêm trọng, không tạo được báo cáo
    3   Tham số dòng lệnh không hợp lệ

  Kết luận đánh giá - máy được quét có vấn đề không:
    10  Kết luận mức CẢNH BÁO (ví dụ: Office chưa kích hoạt)
    20  Kết luận mức NGHIÊM TRỌNG (dấu hiệu kích hoạt trái phép)

  Thứ tự ưu tiên khi nhiều điều kiện cùng xảy ra: 2 > 3 > 20 > 10 > 1 > 0

  Hệ thống giám sát cần phân biệt "công cụ đọc thiếu dữ liệu" với "máy này có
  vấn đề bản quyền" - gộp cả hai vào một mã thì mất đúng thông tin quan trọng
  nhất. Nếu hệ RMM của bạn coi MỌI mã khác 0 là script lỗi, thêm
  --no-verdict-exit để chỉ dùng nhóm mã sức khoẻ công cụ.

VÍ DỤ:
  swico.exe                             Quét đầy đủ, mở báo cáo khi xong
  swico.exe --scope license             Chỉ kiểm tra bản quyền
  swico.exe --silent --sfc              Quét sâu, không mở trình duyệt (triển khai hàng loạt)
  swico.exe -o D:\BaoCao --silent       Lưu kết quả vào thư mục chỉ định
  swico.exe --rules .\luat-moi.json     Quét bằng bộ luật cập nhật
  swico.exe --silent --no-verdict-exit  Cho RMM coi mọi mã khác 0 là lỗi
""";
}
