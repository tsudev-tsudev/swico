using Tsudev.Audit.Core.Abstractions;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// Quyet dinh xem co phai cap nhat truoc khi quet hay khong.
///
/// QUYET DINH THIET KE QUAN TRONG NHAT - kiem tra that bai thi KHONG chan:
///
/// Cong cu nay duoc dung dung o nhung noi mang bi han che nhat - may trong
/// mang noi bo cach ly, may trong doanh nghiep chan GitHub, may khong noi
/// mang. Neu "khong kiem tra duoc" cung chan luon lan quet, cong cu se vo dung
/// chinh o noi no can thiet nhat, va nguoi dung khong co cach nao vuot qua.
///
/// Vi vay chi chan khi da XAC DINH CHAC CHAN co ban moi hon. Moi truong hop
/// khong chac chan deu cho di tiep, kem mot ghi chu trong bao cao de nguoi doc
/// biet lan quet nay chua doi chieu duoc phien ban.
/// </summary>
public sealed class UpdateChecker
{
    private readonly IUpdateFeed _feed;

    public UpdateChecker(IUpdateFeed feed)
        => _feed = feed ?? throw new ArgumentNullException(nameof(feed));

    /// <param name="currentVersionText">So hieu cua ban dang chay.</param>
    /// <param name="kind">
    /// Ban dang chay den tu dau. Ban portable KHONG tu cai duoc: chay file
    /// setup se cai mot ban thu hai vao Program Files trong khi file tren USB
    /// van cu, va lan sau chay lai no thi lai bi chan tiep.
    /// </param>
    public UpdateCheckResult Check(string currentVersionText, InstallKind kind = InstallKind.Installed)
    {
        if (!VersionNumber.TryParse(currentVersionText, out var current))
        {
            // Khong doc duoc phien ban cua CHINH minh thi khong co co so so sanh.
            return new UpdateCheckResult(UpdateStatus.CheckFailed, VersionNumber.None,
                Message: $"Không đọc được phiên bản hiện tại ('{currentVersionText}'). Bỏ qua kiểm tra cập nhật.");
        }

        ReleaseInfo? latest;
        string? error;
        try
        {
            latest = _feed.GetLatest(out error);
        }
        catch (Exception ex)   // cong cu KHONG duoc sap vi mot loi mang
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current,
                Message: $"Không kiểm tra được bản cập nhật: {ex.Message}");
        }

        if (latest is null)
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current,
                Message: error ?? "Không lấy được thông tin bản phát hành mới nhất.");
        }

        if (!latest.Version.IsValid)
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current, latest,
                $"Không đọc được số hiệu phiên bản từ bản phát hành '{latest.TagName}'.");
        }

        if (latest.Version <= current)
        {
            return new UpdateCheckResult(UpdateStatus.UpToDate, current, latest,
                $"Đang dùng bản mới nhất ({current}).");
        }

        // BAN PORTABLE: van chan, nhung KHONG tu cai.
        //
        // Chay file setup o day la mot cai bay: no cai mot ban thu hai vao
        // Program Files, con file .exe tren USB thi VAN CU - lan sau chay lai
        // file do se lai bi chan, mai mai. Nguoi dung khong co cach nao thoat
        // ra ma khong hieu chuyen gi dang xay ra.
        if (kind == InstallKind.Portable)
        {
            var where = string.IsNullOrWhiteSpace(latest.PortableUrl) ? latest.PageUrl : latest.PortableUrl;
            return new UpdateCheckResult(UpdateStatus.UpdateRequired, current, latest,
                $"Đã có phiên bản mới {latest.Version} (bạn đang dùng {current}). " +
                $"Đây là bản portable nên phải tự thay thế: tải {where}, " +
                "giải nén rồi ghi đè lên thư mục đang chạy.",
                CanSelfInstall: false);
        }

        // Co ban moi nhung KHONG co file cai dat -> khong the cap nhat tu dong.
        // Bao cho nguoi dung biet nhung khong chan ho lai.
        if (string.IsNullOrWhiteSpace(latest.InstallerUrl))
        {
            return new UpdateCheckResult(UpdateStatus.CheckFailed, current, latest,
                $"Có bản mới {latest.Version} nhưng bản phát hành đó không kèm file cài đặt. " +
                $"Vui lòng cập nhật thủ công tại {latest.PageUrl}");
        }

        return new UpdateCheckResult(UpdateStatus.UpdateRequired, current, latest,
            $"Đã có phiên bản mới {latest.Version} (bạn đang dùng {current}).",
            CanSelfInstall: true);
    }
}
