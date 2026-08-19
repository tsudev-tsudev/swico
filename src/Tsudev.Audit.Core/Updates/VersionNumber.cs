using System.Buffers;
using System.Globalization;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// So hieu phien ban theo CalVer <c>yy.M.d</c> (vi du <c>26.8.18</c>).
///
/// Vi sao khong dung <see cref="Version"/> co san: chuoi tu GitHub co the mang
/// tien to "v" va co the co hau to (vi du "26.8.20-rc1"), con so hieu doc tu
/// assembly co the co bam commit ("26.8.18+9e1f2c"). Lop nay chap nhan moi dang
/// do va CHI so sanh phan so - de mot dang la khong lam hong viec kiem tra
/// cap nhat.
/// </summary>
/// <remarks>
/// Thanh phan thu tu (<c>Revision</c>) la TUY CHON, dung khi phai phat hanh
/// lai trong CUNG MOT NGAY, va no chinh la THU TU cua ban do trong ngay:
/// <c>26.8.19.2</c> la ban thu HAI ngay 19/08/2026. Ban thu nhat khong mang so
/// dem (<c>26.8.19</c>), nen <c>.1</c> khong duoc dung - xem
/// <see cref="ReleaseName.Validate"/>.
///
/// Danh so theo ngay ma khong co thanh phan nay thi hai ban dung khac nhau se
/// mang cung mot so hieu - dieu khong bao gio duoc phep xay ra voi phan mem da
/// phat hanh.
///
/// Quy uoc day du: <c>docs/VERSIONING.md</c>.
/// </remarks>
public readonly record struct VersionNumber(int Year, int Month, int Day, int Revision = 0)
    : IComparable<VersionNumber>
{
    public static readonly VersionNumber None = new(0, 0, 0, 0);

    /// <summary>Cac ky tu bat dau phan hau to can cat bo.</summary>
    private static readonly SearchValues<char> SuffixStart = SearchValues.Create("-+ ");

    public bool IsValid => this != None;

    /// <summary>
    /// Doc so hieu phien ban. KHONG nem exception - tra ve false khi khong doc
    /// duoc, vi mot chuoi la tu mang KHONG duoc lam hong ca lan quet.
    /// </summary>
    public static bool TryParse(string? text, out VersionNumber value)
    {
        value = None;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.Trim();

        // Bo tien to cua TEN PHAT HANH day du truoc, vi ten do co chua dau '-'
        // ma buoc cat hau to ben duoi se cat nham thanh "tsudev". Ten phat hanh
        // xuat hien o truong `name` cua GitHub Release, o tieu de ban ghi thay
        // doi, va trong bao cao loi ma nguoi dung dan vao - phai doc duoc ca ba.
        if (s.StartsWith(ReleaseName.Prefix, StringComparison.OrdinalIgnoreCase))
            s = s[ReleaseName.Prefix.Length..];
        else if (s.StartsWith('v') || s.StartsWith('V'))
            s = s[1..];

        // Cat moi hau to: "-rc1", "+bam-commit"...
        var cut = s.AsSpan().IndexOfAny(SuffixStart);
        if (cut >= 0) s = s[..cut];

        var parts = s.Split('.');
        if (parts.Length < 3) return false;

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var y) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var m) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var d))
            return false;

        if (y < 0 || m is < 1 or > 12 || d is < 1 or > 31) return false;

        var rev = 0;
        if (parts.Length >= 4 &&
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out rev))
            return false;
        if (rev < 0) return false;

        value = new VersionNumber(y, m, d, rev);
        return true;
    }

    public int CompareTo(VersionNumber other)
    {
        var c = Year.CompareTo(other.Year);
        if (c != 0) return c;
        c = Month.CompareTo(other.Month);
        if (c != 0) return c;
        c = Day.CompareTo(other.Day);
        return c != 0 ? c : Revision.CompareTo(other.Revision);
    }

    public static bool operator <(VersionNumber a, VersionNumber b) => a.CompareTo(b) < 0;
    public static bool operator >(VersionNumber a, VersionNumber b) => a.CompareTo(b) > 0;
    public static bool operator <=(VersionNumber a, VersionNumber b) => a.CompareTo(b) <= 0;
    public static bool operator >=(VersionNumber a, VersionNumber b) => a.CompareTo(b) >= 0;

    public override string ToString()
        => Revision == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{Year}.{Month}.{Day}")
            : string.Create(CultureInfo.InvariantCulture, $"{Year}.{Month}.{Day}.{Revision}");
}
