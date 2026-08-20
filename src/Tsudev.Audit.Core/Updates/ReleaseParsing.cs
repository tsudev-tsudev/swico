using System.Globalization;
using System.Text.Json;

namespace Tsudev.Audit.Core.Updates;

/// <summary>
/// Phan tich JSON tra ve tu GitHub Releases, va tra ma bam trong
/// <c>SHA256SUMS.txt</c>.
///
/// Nam trong Core CO CHU DICH: day la logic thuan, khong dung API nao cua
/// Windows. De trong lop adapter thi bo test chay tren Linux khong voi toi
/// duoc - va bai hoc trong phien nay da cho thay dieu do dan toi dau: lop
/// CliOptions tung khong duoc kiem thu mot dong nao trong khi README tuyen bo
/// nguoc lai.
/// </summary>
public static class GitHubReleaseParser
{
    /// <summary>
    /// Tien to ten file cai dat theo DANG CU (<c>swico-setup-26.8.19.exe</c>).
    /// </summary>
    public const string InstallerPrefix = "swico-setup-";

    /// <summary>Tien to ten ban portable theo DANG CU (<c>swico-portable-26.8.19.zip</c>).</summary>
    public const string PortablePrefix = "swico-portable-";

    public const string ChecksumsFileName = "SHA256SUMS.txt";

    /// <summary>
    /// Mot tep dinh kem co phai FILE CAI DAT khong - nhan ca hai dang ten.
    ///
    /// <code>
    ///   swico-setup-26.8.19.exe                 dang CU
    ///   tsudev-swico_26.8.1901_x64-setup.exe    dang MOI (docs/DESIGN_SYSTEM.md muc 6)
    /// </code>
    ///
    /// VI SAO PHAI NHAN CA HAI. Chuc nang tu cap nhat tim file cai dat THEO TEN.
    /// Bo dang cu di thi mot ban phat hanh cu - hoac mot ban phat hanh moi ma
    /// vi ly do nao do con kem tep ten cu - se bi coi la "khong co file cai
    /// dat", va nguoi dung nhan duoc thong bao phai tu cap nhat thay vi duoc
    /// cap nhat. Do la kieu hong IM LANG, khong ai bao loi.
    /// </summary>
    public static bool IsInstallerAsset(string name)
        => name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase)
               ? name.StartsWith(ReleaseName.NamePrefix, StringComparison.OrdinalIgnoreCase)
               : name.StartsWith(InstallerPrefix, StringComparison.OrdinalIgnoreCase) &&
                 name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Mot tep dinh kem co phai BAN PORTABLE khong - nhan ca hai dang ten.
    ///
    /// <code>
    ///   swico-portable-26.8.19.zip                 dang CU
    ///   tsudev-swico_26.8.1901_x64-portable.zip    dang MOI
    /// </code>
    /// </summary>
    public static bool IsPortableAsset(string name)
        => name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
           (name.StartsWith(PortablePrefix, StringComparison.OrdinalIgnoreCase) ||
            (name.StartsWith(ReleaseName.NamePrefix, StringComparison.OrdinalIgnoreCase) &&
             name.EndsWith("-portable.zip", StringComparison.OrdinalIgnoreCase)));

    public static ReleaseInfo? Parse(string json, out string? failureReason)
    {
        failureReason = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            failureReason = "Dữ liệu bản phát hành rỗng.";
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                failureReason = "Dữ liệu bản phát hành không đúng định dạng.";
                return null;
            }

            var tag = Text(root, "tag_name");
            var page = Text(root, "html_url");

            if (!VersionNumber.TryParse(tag, out var version))
                version = VersionNumber.None;

            DateTimeOffset? published = null;
            if (root.TryGetProperty("published_at", out var p) &&
                DateTimeOffset.TryParse(p.GetString(), CultureInfo.InvariantCulture,
                                        DateTimeStyles.RoundtripKind, out var pub))
                published = pub;

            string? installer = null, checksums = null, portable = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = Text(a, "name");
                    var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    // Thu tu kiem tra: ban portable TRUOC file cai dat. Ten dang
                    // moi cua ca hai deu bat dau bang "tsudev-swico_", nen phai
                    // loai .zip ra truoc thi phep thu ".exe" moi khong nhap nhang.
                    if (IsPortableAsset(name))
                        portable = url;
                    else if (IsInstallerAsset(name))
                        installer = url;
                    else if (name.Equals(ChecksumsFileName, StringComparison.OrdinalIgnoreCase))
                        checksums = url;
                }
            }

            return new ReleaseInfo(version, tag, page, installer, checksums, published, portable);
        }
        catch (JsonException ex)
        {
            failureReason = $"Không đọc được dữ liệu bản phát hành: {ex.Message}";
            return null;
        }
    }

    private static string Text(JsonElement e, string name)
        => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";
}

/// <summary>Doc file <c>SHA256SUMS.txt</c> theo dinh dang cua lenh sha256sum.</summary>
public static class ChecksumFile
{
    /// <summary>
    /// Tim ma bam cua mot tep. Tra ve null khi khong co - va KHONG duoc coi
    /// "khong tim thay" la "hop le": khong xac minh duoc thi phai tu choi chay.
    /// </summary>
    public static string? Find(string? content, string fileName)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(fileName)) return null;

        foreach (var line in content.Split('\n'))
        {
            var parts = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            // Dinh dang: "<bam>  <ten tep>" - ten tep co the co tien to '*'
            // khi duoc tao o che do nhi phan.
            if (parts.Length < 2) continue;
            var name = parts[^1].TrimStart('*');
            if (!name.Equals(fileName, StringComparison.OrdinalIgnoreCase)) continue;

            var hash = parts[0].Trim().ToLowerInvariant();
            return hash.Length == 64 && hash.All(Uri.IsHexDigit) ? hash : null;
        }
        return null;
    }
}
