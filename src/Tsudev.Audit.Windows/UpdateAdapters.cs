using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Updates;

namespace Tsudev.Audit.Windows;

/// <summary>
/// Doc ban phat hanh moi nhat tu GitHub Releases.
///
/// DAY LA LAN DUY NHAT TOAN BO CONG CU CHAM TOI INTERNET. Go gon vao mot lop
/// de nguoi ra soat bao mat kiem chung duoc dieu do trong vai giay - cung
/// nguyen tac voi viec moi lenh goi WMI/Registry nam gon trong WindowsAdapters.
///
/// Khong gui bat ky thong tin nao ve may duoc quet: chi mot yeu cau GET den
/// GitHub. GitHub, nhu moi may chu web, se thay dia chi IP va phien ban trong
/// chuoi User-Agent - dieu nay duoc noi ro trong PRIVACY.md.
/// </summary>
public sealed class GitHubUpdateFeed : IUpdateFeed
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/tsudev-tsudev/swico/releases/latest";

    private readonly TimeSpan _timeout;
    private readonly string _userAgent;

    public GitHubUpdateFeed(string currentVersion, TimeSpan? timeout = null)
    {
        // Thoi gian cho NGAN co chu dich: nguoi dung dang doi de quet may, khong
        // duoc bat ho ngoi nhin man hinh trong khi ta doi mot may chu co the
        // dang bi tuong lua chan im lang.
        _timeout = timeout ?? TimeSpan.FromSeconds(6);
        _userAgent = $"tsudev-SWICO/{currentVersion}";
    }

    public ReleaseInfo? GetLatest(out string? failureReason)
    {
        failureReason = null;
        try
        {
            using var http = new HttpClient { Timeout = _timeout };
            http.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            using var response = http.GetAsync(LatestReleaseApi).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                failureReason = $"GitHub trả về mã {(int)response.StatusCode}.";
                return null;
            }

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return GitHubReleaseParser.Parse(json, out failureReason);
        }
        catch (HttpRequestException ex) { failureReason = $"Không kết nối được tới GitHub: {ex.Message}"; }
        catch (TaskCanceledException) { failureReason = $"Quá thời gian chờ ({_timeout.TotalSeconds:0} giây)."; }
        catch (JsonException ex) { failureReason = $"Dữ liệu trả về không hợp lệ: {ex.Message}"; }
        return null;
    }

}

/// <summary>
/// Xac dinh ban dang chay la ban DA CAI hay ban PORTABLE.
///
/// KHONG CHAM TOI MANG - chi mot phep kiem tra su ton tai cua mot tep tren dia.
/// De o day de moi thu lien quan toi cap nhat nam canh nhau; loi cam ket trong
/// PRIVACY.md la "moi ma CHAM MANG nam trong tep nay", khong phai chieu nguoc lai.
///
/// Cach nhan biet: Inno Setup luon dat trinh go cai <c>unins000.exe</c> canh
/// ung dung khi cai. Ban portable giai nen tu .zip thi khong co tep do.
///
/// Doan nham ve phia nao thi hong the nao:
/// - doan nham ban CAI thanh PORTABLE -> nguoi dung phai tu tai, phien hon
///   nhung khong hong gi;
/// - doan nham ban PORTABLE thanh CAI -> chay file setup, cai them mot ban vao
///   Program Files, ban tren USB van cu, va nguoi dung bi chan mai mai.
/// Nen khi khong chac chan, mac dinh la PORTABLE.
/// </summary>
[SupportedOSPlatform("windows")]
public static class InstallKindDetector
{
    /// <summary>Ten trinh go cai do Inno Setup sinh ra trong thu muc ung dung.</summary>
    public const string UninstallerName = "unins000.exe";

    public static InstallKind Detect()
    {
        try
        {
            var exe = Environment.ProcessPath;
            var dir = string.IsNullOrWhiteSpace(exe) ? null : Path.GetDirectoryName(exe);
            if (string.IsNullOrWhiteSpace(dir)) return InstallKind.Portable;

            return File.Exists(Path.Combine(dir, UninstallerName))
                ? InstallKind.Installed
                : InstallKind.Portable;
        }
        catch (IOException) { return InstallKind.Portable; }
        catch (UnauthorizedAccessException) { return InstallKind.Portable; }
    }
}

/// <summary>Tai file cai dat va XAC MINH truoc khi chay.</summary>
[SupportedOSPlatform("windows")]
public static class UpdateInstaller
{
    /// <summary>
    /// Tai file cai dat ve thu muc tam va doi chieu ma bam.
    ///
    /// XAC MINH LA BAT BUOC: tai mot file .exe tu mang roi chay no voi quyen
    /// Administrator ma khong kiem tra gi la dung mo ta cua mot cuoc tan cong.
    ///
    /// GIOI HAN CAN BIET: SHA256SUMS.txt hien CHUA duoc ky. Viec doi chieu nay
    /// chan duoc file hong hoac tai thieu, nhung KHONG chan duoc ke da chiem
    /// duoc quyen phat hanh tren GitHub. Khi da co chu ky Authenticode, phai
    /// kiem tra them chu ky cua chinh file .exe.
    /// </summary>
    public static string? DownloadAndVerify(ReleaseInfo release, Action<string> report, out string? failureReason)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(report);
        failureReason = null;

        if (string.IsNullOrWhiteSpace(release.InstallerUrl))
        {
            failureReason = "Bản phát hành không kèm file cài đặt.";
            return null;
        }

        var fileName = Path.GetFileName(new Uri(release.InstallerUrl).LocalPath);
        var target = Path.Combine(Path.GetTempPath(), $"swico-update-{release.Version}", fileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            http.DefaultRequestHeaders.Add("User-Agent", $"tsudev-SWICO/{release.Version}");

            report($"Đang tải {fileName}...");
            using (var stream = http.GetStreamAsync(release.InstallerUrl).GetAwaiter().GetResult())
            using (var file = File.Create(target))
                stream.CopyTo(file);

            if (string.IsNullOrWhiteSpace(release.ChecksumsUrl))
            {
                failureReason = "Bản phát hành không kèm SHA256SUMS.txt nên KHÔNG xác minh được file tải về.";
                return null;
            }

            report("Đang đối chiếu mã băm SHA-256...");
            var sums = http.GetStringAsync(release.ChecksumsUrl).GetAwaiter().GetResult();
            var expected = ChecksumFile.Find(sums, fileName);
            if (expected is null)
            {
                failureReason = $"Không tìm thấy '{fileName}' trong SHA256SUMS.txt.";
                return null;
            }

            using var fs = File.OpenRead(target);
            var actual = Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant();
            if (!actual.Equals(expected, StringComparison.Ordinal))
            {
                failureReason = $"Mã băm KHÔNG khớp. Mong đợi {expected[..16]}…, nhận được {actual[..16]}…. " +
                                "File tải về đã bị hỏng hoặc bị can thiệp - KHÔNG chạy file này.";
                TryDelete(target);
                return null;
            }

            report("Đã xác minh xong.");
            return target;
        }
        catch (HttpRequestException ex) { failureReason = $"Lỗi tải về: {ex.Message}"; }
        catch (TaskCanceledException) { failureReason = "Quá thời gian chờ khi tải file cài đặt."; }
        catch (IOException ex) { failureReason = $"Lỗi ghi file: {ex.Message}"; }
        catch (UnauthorizedAccessException ex) { failureReason = $"Không có quyền ghi: {ex.Message}"; }

        TryDelete(target);
        return null;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>Chay file cai dat roi tra ve - nguoi goi phai thoat ngay sau do.</summary>
    public static bool Launch(string installerPath, out string? failureReason)
    {
        failureReason = null;
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true,     // can thiet de Windows hien hop UAC
                Verb = "runas"
            });
            return p is not null;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            failureReason = $"Không chạy được file cài đặt: {ex.Message}";
            return false;
        }
    }
}
