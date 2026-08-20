using System.Buffers;
using System.Globalization;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// So hieu phien ban theo CalVer <c>YY.M.DDNN</c> (vi du <c>26.8.1901</c> =
/// ban thu 01 ngay 19/08/2026), theo <c>docs/DESIGN_SYSTEM.md</c> muc 6.
///
/// Vi sao khong dung <see cref="Version"/> co san: chuoi tu GitHub co the mang
/// tien to "v" va co the co hau to (vi du "26.8.2001-rc1"), con so hieu doc tu
/// assembly co the co bam commit ("26.8.1901+9e1f2c"). Lop nay chap nhan moi
/// dang do va CHI so sanh phan so - de mot dang la khong lam hong viec kiem tra
/// cap nhat.
/// </summary>
/// <remarks>
/// MO HINH TRONG BO NHO GIU NGUYEN 4 TRUONG (Year/Month/Day/Revision) - chi
/// cach DOC va cach VIET ra chuoi la doi theo quy uoc moi. Do la ly do viec
/// chuyen quy uoc khong dung toi <see cref="CompareTo"/> mot dong nao.
///
/// <c>Revision</c> = 0 nghia la ban THU NHAT trong ngay (so thu tu hien thi
/// la <c>01</c>); tu ban thu hai tro di <c>Revision</c> chinh la so thu tu do.
/// Giu quy uoc "0 = ban thu nhat" CO CHU DICH: nho vay
/// <c>26.8.18</c> (dang cu, da phat hanh) va <c>26.8.1801</c> (dang moi) doc ra
/// CUNG mot gia tri, nen hai ban da phat hanh khong bi coi la phien ban khac.
///
/// <para>DOC DUOC CA HAI DANG - bat buoc, khong phai tien nghi:</para>
/// <code>
///   26.8.19      dang CU, 3 thanh phan, thanh phan 3 la NGAY   -> (26, 8, 19, 0)
///   26.8.19.2    dang CU, 4 thanh phan, thanh phan 4 la so dem -> (26, 8, 19, 2)
///   26.8.1901    dang MOI, thanh phan 3 la DDNN                -> (26, 8, 19, 0)
///   26.8.1902    dang MOI                                      -> (26, 8, 19, 2)
/// </code>
/// Phan biet khong nhap nhang: ngay chi co toi 2 chu so, con DDNN luon >= 3 chu
/// so (ngay 1 ban 1 = <c>0101</c>, viet gon nhat cung la <c>101</c>). Nen do dai
/// cua thanh phan thu ba la du de biet dang nao.
///
/// Vi sao phai doc duoc dang cu: hai ban <c>26.8.18</c> va <c>26.8.18.2</c> DA
/// PHAT HANH ra ngoai. Ngung doc duoc chung nghia la ngung so sanh duoc voi
/// chinh minh.
///
/// Quy uoc day du: <c>docs/VERSIONING.md</c>.
/// </remarks>
public readonly record struct VersionNumber(int Year, int Month, int Day, int Revision = 0)
    : IComparable<VersionNumber>
{
    public static readonly VersionNumber None = new(0, 0, 0, 0);

    /// <summary>
    /// Cac ky tu bat dau phan hau to can cat bo. Co ca '_' vi ten file phat
    /// hanh dang moi la <c>tsudev-swico_26.8.1901_x64-setup.exe</c>: sau khi bo
    /// tien to thi phan con lai la <c>26.8.1901_x64-setup.exe</c>, va cat tu
    /// dau '_' cho ra dung so hieu. So hieu phien ban khong bao gio chua '_'
    /// nen cat o day khong lam mat gi.
    /// </summary>
    private static readonly SearchValues<char> SuffixStart = SearchValues.Create("-+ _");

    public bool IsValid => this != None;

    /// <summary>
    /// Doc so hieu phien ban. KHONG nem exception - tra ve false khi khong doc
    /// duoc, vi mot chuoi la tu mang KHONG duoc lam hong ca lan quet.
    /// </summary>
    public static bool TryParse(string? text, out VersionNumber value)
    {
        value = None;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Bo tien to cua TEN PHAT HANH day du TRUOC, vi ten do chua dau '-' va
        // '_' ma buoc cat hau to ben duoi se cat nham thanh "tsudev". Ten phat
        // hanh xuat hien o truong `name` cua GitHub Release, o tieu de ban ghi
        // thay doi, va trong bao cao loi ma nguoi dung dan vao - phai doc duoc
        // ca ba. Dung chung ReleaseName.StripPrefix de hai noi khong the lech.
        var s = ReleaseName.StripPrefix(text);

        // Cat moi hau to: "-rc1", "+bam-commit", "_x64-setup.exe"...
        var cut = s.AsSpan().IndexOfAny(SuffixStart);
        if (cut >= 0) s = s[..cut];

        var parts = s.Split('.');
        if (parts.Length < 3) return false;

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var y) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var m) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var third))
            return false;

        if (y < 0 || m is < 1 or > 12) return false;

        int d, rev;

        if (parts[2].Length >= 3)
        {
            // DANG MOI: thanh phan thu ba la DDNN gop lai.
            //
            // Doc bang PHEP CHIA chu khong bang cat chuoi, vi chuoi den day co
            // the da bi chuan hoa mat so 0 dung dau: MSBuild bien
            // <VersionPrefix>26.9.0901</VersionPrefix> thanh AssemblyVersion
            // 26.9.901. Ca "0901" lan "901" deu phai ra ngay 9 ban 1.
            if (parts.Length > 3) return false;   // DDNN thi khong con thanh phan thu tu
            d = third / 100;
            rev = third % 100;
            if (d is < 1 or > 31 || rev < 1) return false;

            // So thu tu 01 = ban thu nhat = Revision 0 trong mo hinh. Nho phep
            // quy doi nay ma 26.8.18 (da phat hanh) va 26.8.1801 doc ra CUNG
            // mot gia tri.
            if (rev == 1) rev = 0;
        }
        else
        {
            // DANG CU: thanh phan thu ba la ngay, so dem nam o thanh phan thu tu.
            d = third;
            if (d is < 1 or > 31) return false;

            rev = 0;
            if (parts.Length >= 4 &&
                !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out rev))
                return false;
            if (rev < 0) return false;
            if (rev == 1) rev = 0;   // ".1" la cach viet khac cua ban thu nhat
        }

        value = new VersionNumber(y, m, d, rev);
        return true;
    }

    /// <summary>
    /// Ban thu may trong ngay, tinh tu 1. Day la con so <c>NN</c> hien ra trong
    /// ten file phat hanh.
    /// </summary>
    public int OrdinalOfDay => Revision == 0 ? 1 : Revision;

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

    /// <summary>
    /// Viet ra dang chuan <c>YY.M.DDNN</c> - dung chuoi nay o MOI noi: ten file
    /// cai dat, tag git, <c>VersionPrefix</c>, manifest winget.
    ///
    /// <c>Day</c> va so thu tu deu duoc dem ve DU HAI CHU SO. Bo phan dem di thi
    /// ngay 9 ban 1 se thanh <c>26.9.91</c> - doc nguoc lai ra ngay 0 ban 91,
    /// va do la mot ten file cai dat khong ai tim thay.
    /// </summary>
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Year}.{Month}.{Day:00}{OrdinalOfDay:00}");
}
