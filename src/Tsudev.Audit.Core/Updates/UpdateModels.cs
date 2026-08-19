namespace Tsudev.Audit.Core.Updates;

/// <summary>Thong tin mot ban phat hanh doc tu GitHub Releases.</summary>
public sealed record ReleaseInfo(
    VersionNumber Version,
    string TagName,
    string PageUrl,
    string? InstallerUrl,
    string? ChecksumsUrl,
    DateTimeOffset? PublishedAt,
    string? PortableUrl = null);

/// <summary>
/// Ban dang chay den tu dau. Quyet dinh cach cap nhat, va do la mot khac biet
/// THAT SU chu khong phai chi tiet trinh bay.
/// </summary>
public enum InstallKind
{
    /// <summary>Cai qua file setup - cap nhat duoc bang cach chay file setup moi.</summary>
    Installed,

    /// <summary>
    /// Ban portable (giai nen tu .zip, thuong nam tren USB).
    ///
    /// Chay file setup o day la SAI: no se cai mot ban thu hai vao Program
    /// Files, con file tren USB VAN CU. Lan sau chay lai ban tren USB thi lai
    /// bi chan tiep - mot vong lap khong loi thoat.
    /// </summary>
    Portable
}

/// <summary>Ket qua kiem tra cap nhat.</summary>
public enum UpdateStatus
{
    /// <summary>Da la ban moi nhat.</summary>
    UpToDate,

    /// <summary>Co ban moi hon - phai cap nhat truoc khi quet.</summary>
    UpdateRequired,

    /// <summary>
    /// Khong kiem tra duoc (mat mang, tuong lua chan, GitHub loi...).
    /// KHONG chan nguoi dung - xem <see cref="UpdateCheckResult"/>.
    /// </summary>
    CheckFailed,

    /// <summary>Nguoi dung hoac chinh sach da tat viec kiem tra.</summary>
    Skipped
}

/// <summary>Ket qua day du cua mot lan kiem tra cap nhat.</summary>
public sealed record UpdateCheckResult(
    UpdateStatus Status,
    VersionNumber Current,
    ReleaseInfo? Latest = null,
    string? Message = null,
    bool CanSelfInstall = false)
{
    /// <summary>
    /// Co chan lan quet lai khong.
    ///
    /// CHI chan khi da xac dinh CHAC CHAN co ban moi hon. Kiem tra that bai thi
    /// KHONG chan - xem ghi chu thiet ke trong <see cref="UpdateChecker"/>.
    /// </summary>
    public bool MustUpdate => Status == UpdateStatus.UpdateRequired;

    /// <summary>
    /// Co chan quet, nhung cong cu KHONG tu cai duoc - nguoi dung phai tu tai
    /// va thay the. Dung cho ban portable.
    ///
    /// Tach rieng khoi <see cref="MustUpdate"/> co chu dich: van chan (bo luat
    /// cu van la bo luat cu), chi khac o chuyen cai dat duoc hay khong. Hien
    /// hop thoai mot nut "Cap nhat" o day se la noi doi - bam vao khong cai
    /// duoc gi.
    /// </summary>
    public bool MustUpdateManually => MustUpdate && !CanSelfInstall;
}
