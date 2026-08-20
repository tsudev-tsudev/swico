using System.Globalization;

namespace Tsudev.Audit.Core.Reports;

/// <summary>
/// Dinh dang ngay/gio HIEN THI cho nguoi doc - quy uoc bat buoc toan he thong,
/// xem docs/DESIGN_SYSTEM.md muc "Dinh dang ngay gio":
///
///     Ngay      : DD/MM/YYYY          vi du 01/02/2027
///     Ngay gio  : HH:mm DD/MM/YYYY    vi du 14:30 19/08/2026
///
/// Luat nay chi ton tai o MOT cho. Moi noi in ngay ra bao cao HTML, trang tong
/// hop hay man hinh dong lenh deu goi vao day - khong tu viet chuoi dinh dang,
/// vi hai ban cua cung mot luat se lech nhau theo thoi gian.
///
/// KHAC voi <see cref="FileNaming"/>: ten file dung yyyyMMdd_HHmmss de sap xep
/// duoc theo thu tu chu cai. Do la ten may doc, khong phai chu nguoi doc.
/// </summary>
public static class DateDisplay
{
    // BAT BUOC dung CultureInfo.InvariantCulture, cung ly do da ghi o FileNaming:
    // tren may dat ngon ngu Thai hoac A Rap, lich mac dinh KHONG phai Gregorian
    // nen "yyyy" cho ra nam khac han (2569 thay vi 2026). Ngoai ra "/" trong
    // chuoi dinh dang la dau phan cach ngay THEO VAN HOA - voi mot so ngon ngu
    // no thanh "." hoac "-", pha vo quy uoc DD/MM/YYYY.
    private const string DatePattern = "dd/MM/yyyy";
    private const string DateTimePattern = "HH:mm dd/MM/yyyy";

    /// <summary>DD/MM/YYYY - vi du 01/02/2027.</summary>
    public static string Date(DateTime t)
        => t.ToString(DatePattern, CultureInfo.InvariantCulture);

    /// <summary>DD/MM/YYYY theo gio may dang chay.</summary>
    public static string Date(DateTimeOffset t)
        => Date(t.LocalDateTime);

    /// <summary>HH:mm DD/MM/YYYY - vi du 14:30 19/08/2026.</summary>
    public static string DateTimeText(DateTime t)
        => t.ToString(DateTimePattern, CultureInfo.InvariantCulture);

    /// <summary>HH:mm DD/MM/YYYY theo gio may dang chay.</summary>
    public static string DateTimeText(DateTimeOffset t)
        => DateTimeText(t.LocalDateTime);
}
