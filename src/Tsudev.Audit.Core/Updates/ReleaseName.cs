using System.Globalization;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// Quy uoc dat ten phien ban phat hanh - DUA THANH MA, khong de o dang van ban.
///
/// Nguon quy uoc: <c>docs/DESIGN_SYSTEM.md</c> muc 6 (ap dung cho toan he sinh
/// thai tsudev). Dang chuan:
///
/// <code>
///   {ten-app}_{YY}.{M}.{DD}{NN}_{arch}-setup.{ext}
///
///   tsudev-swico_26.8.1901_x64-setup.exe   ban thu 01 ngay 19/08/2026
///   tsudev-swico_26.8.1902_x64-setup.exe   ban thu 02 CUNG NGAY
///   tsudev-swico_26.8.2001_x64-setup.exe   ban thu 01 ngay 20/08/2026
/// </code>
///
/// Chuoi phien ban trong ma nguon va manifest la <c>26.8.1901</c> - DONG BO voi
/// ten file, dung nhu quy uoc yeu cau.
///
/// <para>VI SAO <c>DD</c> VA <c>NN</c> DEU PHAI DU HAI CHU SO.</para>
/// Thanh phan thu ba duoc so sanh nhu MOT SO NGUYEN. Dem du hai chu so thi gia
/// tri cua no luon bang <c>DD * 100 + NN</c>, nen thu tu so sanh trung khop voi
/// thu tu thoi gian:
/// <code>
///   26.8.1901 (19/8 ban 1)  ->  1901
///   26.8.1902 (19/8 ban 2)  ->  1902
///   26.8.2001 (20/8 ban 1)  ->  2001      1901 &lt; 1902 &lt; 2001  DUNG
/// </code>
/// Bo phan dem di thi ngay 9 ban 1 thanh <c>26.9.91</c>, doc nguoc lai ra ngay 0
/// ban 91 - va do la mot ten file cai dat khong ai tim thay.
///
/// <para>QUAN HE VOI DANG CU (<c>26.8.19</c>, <c>26.8.19.2</c>).</para>
/// Hai ban <c>26.8.18</c> va <c>26.8.18.2</c> DA PHAT HANH ra ngoai theo dang cu.
/// <see cref="VersionNumber.TryParse"/> vi vay van doc duoc dang cu - de dai la
/// bat buoc o do. Con ham <see cref="Validate"/> nay thi KHAT KHE: no gac cong
/// khau phat hanh, chi chap nhan dang moi.
///
/// Toan bo tai lieu: <c>docs/VERSIONING.md</c>.
/// </summary>
public static class ReleaseName
{
    /// <summary>Ten ung dung trong ten file phat hanh.</summary>
    public const string AppName = "tsudev-swico";

    /// <summary>Kien truc mac dinh. Du an hien chi phat hanh win-x64.</summary>
    public const string DefaultArch = "x64";

    /// <summary>Tien to cua ten phat hanh dang moi: <c>tsudev-swico_</c>.</summary>
    public const string NamePrefix = AppName + "_";

    /// <summary>
    /// Tien to cua ten phat hanh DANG CU (<c>tsudev-swico-v26.8.19</c>).
    /// Giu lai CHI de doc duoc cac ban phat hanh cu - khong sinh ra ten moi
    /// theo dang nay nua.
    /// </summary>
    public const string Prefix = AppName + "-v";

    /// <summary>Ten ban phat hanh, vi du <c>tsudev-swico_26.8.1901</c>.</summary>
    public static string For(VersionNumber version) => NamePrefix + version;

    /// <summary>Tag git, vi du <c>v26.8.1901</c>. Ngan hon ten phat hanh co chu
    /// dich: <c>release.yml</c> kich hoat theo mau <c>v*</c>.</summary>
    public static string TagFor(VersionNumber version)
        => string.Create(CultureInfo.InvariantCulture, $"v{version}");

    /// <summary>
    /// Ten file cai dat, vi du <c>tsudev-swico_26.8.1901_x64-setup.exe</c>.
    ///
    /// Chuc nang tu cap nhat tim file cai dat THEO TEN, nen ham nay va
    /// <see cref="GitHubReleaseParser"/> phai luon noi cung mot thu tieng.
    /// </summary>
    public static string InstallerFileName(VersionNumber version, string arch = DefaultArch)
        => string.Create(CultureInfo.InvariantCulture, $"{NamePrefix}{version}_{arch}-setup.exe");

    /// <summary>Ten ban portable, vi du <c>tsudev-swico_26.8.1901_x64-portable.zip</c>.</summary>
    public static string PortableFileName(VersionNumber version, string arch = DefaultArch)
        => string.Create(CultureInfo.InvariantCulture, $"{NamePrefix}{version}_{arch}-portable.zip");

    /// <summary>
    /// Bo moi tien to duoc cong nhan khoi mot chuoi phien ban. Dung chung boi
    /// <see cref="Validate"/> va <see cref="VersionNumber.TryParse"/> de hai noi
    /// khong the lech nhau.
    /// </summary>
    public static string StripPrefix(string text)
    {
        var s = text.Trim();
        if (s.StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase)) return s[NamePrefix.Length..];
        if (s.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) return s[Prefix.Length..];
        if (s.StartsWith('v') || s.StartsWith('V')) return s[1..];
        return s;
    }

    /// <summary>
    /// Kiem tra mot chuoi phien ban co DUNG quy uoc khong.
    ///
    /// Khac voi <see cref="VersionNumber.TryParse"/> - von co tinh de dai vi no
    /// doc du lieu tu mang va mot chuoi la KHONG duoc lam hong ca lan quet -
    /// ham nay CO TINH KHAT KHE, vi no gac cong khau phat hanh: cho lot mot so
    /// hieu sai quy uoc o day la phat tan cai sai do ra may nguoi dung.
    /// </summary>
    /// <param name="text">Chuoi can kiem tra. Chap nhan ca ba dang:
    /// <c>26.8.1901</c>, <c>v26.8.1901</c>, <c>tsudev-swico_26.8.1901</c>.</param>
    /// <param name="version">So hieu doc duoc, khi hop le.</param>
    /// <param name="problem">Ly do khong hop le, bang tieng Viet, khi khong hop le.</param>
    public static bool Validate(string? text, out VersionNumber version, out string? problem)
    {
        version = VersionNumber.None;
        problem = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            problem = "Chuỗi phiên bản rỗng.";
            return false;
        }

        var s = StripPrefix(text);

        // Khong chap nhan hau to o khau phat hanh. VersionNumber.TryParse cat
        // "-rc1" va "+bam-commit" vi no doc du lieu tu ben ngoai; o day thi mot
        // hau to nghia la so hieu chua o dang cuoi cung.
        if (s.AsSpan().IndexOfAny('-', '+', ' ') >= 0)
        {
            problem = $"'{text}' chứa hậu tố. Phiên bản phát hành phải ở dạng thuần số: {NamePrefix}26.8.1901";
            return false;
        }

        var parts = s.Split('.');

        // Dang CU co 4 thanh phan. Bao thang ra cach viet moi tuong duong thay
        // vi chi noi "sai" - nguoi go vao thuong dang chep tu mot ban cu.
        if (parts.Length == 4)
        {
            problem = $"'{text}' viết theo dạng CŨ (YY.M.D.N). Dạng hiện hành là YY.M.DDNN: " +
                      (VersionNumber.TryParse(s, out var conv)
                          ? $"viết {NamePrefix}{conv}."
                          : $"ví dụ {NamePrefix}26.8.1902.");
            return false;
        }

        if (parts.Length != 3)
        {
            problem = $"'{text}' phải có đúng 3 thành phần (YY.M.DDNN), đang có {parts.Length}.";
            return false;
        }

        // So 0 dung dau bi cam o NAM va THANG - "26.08.1901" va "26.8.1901" la
        // cung mot so hieu nhung khac chuoi, se sinh ra hai ten file cai dat cho
        // cung mot ban phat hanh. Rieng thanh phan thu ba thi so 0 dung dau la
        // BAT BUOC (ngay 9 ban 1 = '0901'), nen no khong chiu luat nay.
        for (var i = 0; i < 2; i++)
        {
            var p = parts[i];
            if (p.Length == 0)
            {
                problem = $"'{text}' có thành phần rỗng.";
                return false;
            }
            if (p.Length > 1 && p[0] == '0')
            {
                var fixedParts = (string[])parts.Clone();
                fixedParts[i] = p.TrimStart('0') is { Length: > 0 } t ? t : "0";
                problem = $"'{text}' có số 0 đứng đầu ('{p}') ở {(i == 0 ? "năm" : "tháng")}. " +
                          $"Viết {NamePrefix}{string.Join('.', fixedParts)}.";
                return false;
            }
        }

        // Thanh phan thu ba phai du BON chu so. '901' cung ra ngay 9 ban 1 khi
        // doc bang TryParse, nhung o khau phat hanh thi mot so hieu phai co DUNG
        // MOT cach viet - neu khong, '26.9.901' va '26.9.0901' se thanh hai ten
        // file cai dat khac nhau cho cung mot ban.
        var ddnn = parts[2];
        if (ddnn.Length != 4 || !ddnn.All(char.IsAsciiDigit))
        {
            problem = $"'{text}' có thành phần thứ ba là '{ddnn}'. Phải là DDNN đúng 4 chữ số " +
                      "(DD = ngày 2 chữ số, NN = số thứ tự trong ngày 2 chữ số), ví dụ '1901' hoặc '0901'.";
            return false;
        }

        if (!VersionNumber.TryParse(s, out version))
        {
            problem = $"'{text}' không đọc được thành phiên bản YY.M.DDNN hợp lệ " +
                      "(tháng phải là 1–12, ngày phải là 01–31, số thứ tự phải từ 01).";
            return false;
        }

        if (version.Year is < 0 or > 99)
        {
            problem = $"'{text}' có năm '{version.Year}'. CalVer dùng năm hai chữ số: 26 cho 2026.";
            return false;
        }

        // Sau khi qua het cac buoc tren, chuoi da o DANG CHUAN DUY NHAT. Kiem
        // lai bang chinh ToString de bat moi truong hop con sot - neu viet ra
        // khac voi cai doc vao thi con mot cho nao do chua duoc chuan hoa.
        if (!string.Equals(version.ToString(), s, StringComparison.Ordinal))
        {
            problem = $"'{text}' không ở dạng chuẩn. Viết {NamePrefix}{version}.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ban phat hanh nay la ban thu may trong ngay. Ban thu nhat -> 1.
    /// </summary>
    public static int OrdinalOfDay(VersionNumber version) => version.OrdinalOfDay;

    /// <summary>
    /// So hieu cho ban phat hanh TIEP THEO trong CUNG ngay voi
    /// <paramref name="version"/>. Dung khi phai phat hanh lai trong ngay.
    /// </summary>
    public static VersionNumber NextSameDay(VersionNumber version)
        => version with { Revision = version.OrdinalOfDay + 1 };

    /// <summary>
    /// So hieu cho ban phat hanh dau tien cua mot ngay.
    /// </summary>
    public static VersionNumber ForDate(DateOnly date)
        => new(date.Year % 100, date.Month, date.Day);
}
