namespace Tsudev.Audit.Core.Updates;

/// <summary>Thong tin mot ban phat hanh doc tu GitHub Releases.</summary>
public sealed record ReleaseInfo(
    VersionNumber Version,
    string TagName,
    string PageUrl,
    string? InstallerUrl,
    string? ChecksumsUrl,
    DateTimeOffset? PublishedAt);

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
    string? Message = null)
{
    /// <summary>
    /// Co chan lan quet lai khong.
    ///
    /// CHI chan khi da xac dinh CHAC CHAN co ban moi hon. Kiem tra that bai thi
    /// KHONG chan - xem ghi chu thiet ke trong <see cref="UpdateChecker"/>.
    /// </summary>
    public bool MustUpdate => Status == UpdateStatus.UpdateRequired;
}
