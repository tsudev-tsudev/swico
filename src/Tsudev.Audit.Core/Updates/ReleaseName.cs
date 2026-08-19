using System.Globalization;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// Quy uoc dat ten phien ban phat hanh cua du an - DUA THANH MA, khong de o
/// dang van ban.
///
/// Dang chuan:  <c>tsudev-swico-vYY.M.D[.N]</c>
///
/// <code>
///   tsudev-swico-v26.8.19      ban thu 1 ngay 19/08/2026
///   tsudev-swico-v26.8.19.2    ban thu 2 CUNG NGAY
///   tsudev-swico-v26.8.19.3    ban thu 3 CUNG NGAY
///   tsudev-swico-v26.8.20      ban thu 1 ngay 20/08/2026
/// </code>
///
/// VI SAO SO DEM PHAI NAM SAU MOT DAU CHAM. Neu dinh lien vao ngay
/// (<c>26.8.192</c>), thanh phan thu ba bi so sanh nhu MOT SO NGUYEN va
/// <c>192 &gt; 20</c> - ban thu 2 ngay 19/8 se duoc coi la MOI HON ban ngay
/// 20/8. May dang chay ban do se khong bao gio nhan duoc ban cap nhat sau, va
/// Inno Setup cung nhan nham chieu cai de. Voi dau cham thi
/// <c>26.8.19 &lt; 26.8.19.2 &lt; 26.8.19.3 &lt; 26.8.20</c> dung thu tu o MOI
/// noi so sanh - khong can bo giai ma rieng, nen khong co cho nao de lech.
///
/// Toan bo tai lieu: <c>docs/VERSIONING.md</c>.
/// </summary>
public static class ReleaseName
{
    /// <summary>Tien to cua ten phat hanh day du.</summary>
    public const string Prefix = "tsudev-swico-v";

    /// <summary>So dem nho nhat duoc phep. Xem <see cref="Validate"/>.</summary>
    public const int FirstRevisionOfSameDay = 2;

    /// <summary>Ten phat hanh day du, vi du <c>tsudev-swico-v26.8.19.2</c>.</summary>
    public static string For(VersionNumber version) => Prefix + version;

    /// <summary>Tag git, vi du <c>v26.8.19.2</c>. Ngan hon ten phat hanh co chu
    /// dich: <c>release.yml</c> kich hoat theo mau <c>v*</c>.</summary>
    public static string TagFor(VersionNumber version)
        => string.Create(CultureInfo.InvariantCulture, $"v{version}");

    /// <summary>
    /// Kiem tra mot chuoi phien ban co DUNG quy uoc khong.
    ///
    /// Khac voi <see cref="VersionNumber.TryParse"/> - von co tinh de dai vi no
    /// doc du lieu tu mang va mot chuoi la KHONG duoc lam hong ca lan quet -
    /// ham nay CO TINH KHAT KHE, vi no gac cong khau phat hanh: cho lot mot so
    /// hieu sai quy uoc o day la phat tan cai sai do ra may nguoi dung.
    /// </summary>
    /// <param name="text">Chuoi can kiem tra. Chap nhan ca ba dang:
    /// <c>26.8.19.2</c>, <c>v26.8.19.2</c>, <c>tsudev-swico-v26.8.19.2</c>.</param>
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

        var s = text.Trim();
        if (s.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) s = s[Prefix.Length..];
        else if (s.StartsWith('v') || s.StartsWith('V')) s = s[1..];

        // Khong chap nhan hau to o khau phat hanh. VersionNumber.TryParse cat
        // "-rc1" va "+bam-commit" vi no doc du lieu tu ben ngoai; o day thi mot
        // hau to nghia la so hieu chua o dang cuoi cung.
        if (s.AsSpan().IndexOfAny('-', '+', ' ') >= 0)
        {
            problem = $"'{text}' chứa hậu tố. Phiên bản phát hành phải ở dạng thuần số: {Prefix}26.8.19.2";
            return false;
        }

        var parts = s.Split('.');
        if (parts.Length is < 3 or > 4)
        {
            problem = $"'{text}' phải có 3 hoặc 4 thành phần (YY.M.D hoặc YY.M.D.N), đang có {parts.Length}.";
            return false;
        }

        // Bat so 0 dung dau: "26.08.19" va "26.8.19" la CUNG mot so hieu nhung
        // KHAC chuoi - se sinh ra hai ten tag, hai ten file cai dat cho cung
        // mot ban phat hanh. Chan tu day cho khoi phai go ve sau.
        foreach (var p in parts)
        {
            if (p.Length == 0)
            {
                problem = $"'{text}' có thành phần rỗng.";
                return false;
            }
            if (p.Length > 1 && p[0] == '0')
            {
                // Neu duoc thi bao luon cach viet DUNG cua chinh chuoi nguoi
                // dung vua go - de ho sua ngay, khong phai doi chieu vi du.
                var canonical = string.Join('.', parts.Select(x => x.TrimStart('0') is { Length: > 0 } t ? t : "0"));
                problem = $"'{text}' có số 0 đứng đầu ('{p}'). Viết {Prefix}{canonical}.";
                return false;
            }
        }

        if (!VersionNumber.TryParse(s, out version))
        {
            problem = $"'{text}' không đọc được thành phiên bản YY.M.D[.N] hợp lệ " +
                      "(tháng phải là 1–12, ngày phải là 1–31).";
            return false;
        }

        if (version.Year is < 0 or > 99)
        {
            problem = $"'{text}' có năm '{version.Year}'. CalVer dùng năm hai chữ số: 26 cho 2026.";
            return false;
        }

        // .1 bi CAM. Ban thu nhat trong ngay KHONG mang so dem, nen ".1" se la
        // cai ten thu hai cho dung mot ban phat hanh - va hai cai ten cho mot
        // thu la nguon goc cua moi nham lan ve sau.
        if (version.Revision == 1)
        {
            problem = $"'{text}' dùng số đếm '.1'. Bản thứ nhất trong ngày không mang số đếm " +
                      $"({Prefix}{version.Year}.{version.Month}.{version.Day}); " +
                      $"bản thứ hai bắt đầu từ '.{FirstRevisionOfSameDay}'.";
            return false;
        }

        // ".0" cung bi cam vi cung ly do: no la mot cach viet khac cua ban thu nhat.
        if (parts.Length == 4 && version.Revision == 0)
        {
            problem = $"'{text}' dùng số đếm '.0'. Bản thứ nhất trong ngày viết gọn là " +
                      $"{Prefix}{version.Year}.{version.Month}.{version.Day}.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ban phat hanh nay la ban thu may trong ngay. Ban thu nhat -> 1.
    /// </summary>
    public static int OrdinalOfDay(VersionNumber version)
        => version.Revision == 0 ? 1 : version.Revision;

    /// <summary>
    /// So hieu cho ban phat hanh TIEP THEO trong CUNG ngay voi
    /// <paramref name="version"/>. Dung khi phai phat hanh lai trong ngay.
    /// </summary>
    public static VersionNumber NextSameDay(VersionNumber version)
        => version with { Revision = OrdinalOfDay(version) + 1 };

    /// <summary>
    /// So hieu cho ban phat hanh dau tien cua mot ngay.
    /// </summary>
    public static VersionNumber ForDate(DateOnly date)
        => new(date.Year % 100, date.Month, date.Day);
}
