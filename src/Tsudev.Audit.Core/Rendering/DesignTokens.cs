using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Tsudev.Audit.Core.Rendering;

/// <summary>
/// Doc tokens/design-tokens.json (NHUNG san trong assembly) va sinh ra khoi bien
/// CSS de nhung thang vao the &lt;style&gt; cua bao cao.
///
/// VI SAO PHAI LAM VAY thay vi chep gia tri vao ma nguon:
/// AGENTS.md muc 6 cam hard-code mau / co chu / radius. tokens/design-tokens.json
/// la nguon chan ly duy nhat cua ca he sinh thai tsudev - sua mot gia tri o do
/// thi bao cao doi theo o lan build sau, khong ai phai di tim trong CSS.
///
/// VI SAO NHUNG vao assembly thay vi doc file: bao cao phai xem duoc khi copy
/// sang may khac, va swico.exe phai chay duoc khi chi copy moi mot file exe.
/// Do cung la ly do Rules/detection-rules.json duoc nhung.
///
/// KHONG duoc de lot mot mau nao ra ngoai file nay. Muc 19 cua bo test quet CSS
/// va bao FAIL neu tim thay ma mau viet cung o HtmlReportRenderer.
/// </summary>
public static class DesignTokens
{
    private const string ResourceName = "Tsudev.Audit.Core.Rendering.design-tokens.json";

    // Nhung khoa la loi giai thich cho nguoi doc, khong phai gia tri.
    private static readonly string[] NotValues = { "usage" };

    private static readonly Lazy<string> _rootCss = new(BuildRootCss, isThreadSafe: true);
    private static readonly Lazy<JsonDocument> _doc = new(Load, isThreadSafe: true);

    /// <summary>Phien ban bo token dang dung - de ghi vao chan trang bao cao.</summary>
    public static string Version => Str("meta", "version") ?? "?";

    /// <summary>
    /// Khoi <c>:root{...}</c> + bien the toi + bien the in, sinh tu file token.
    /// Ghep vao dau the &lt;style&gt;, TRUOC phan CSS bo cuc.
    /// </summary>
    public static string RootCss => _rootCss.Value;

    /// <summary>Mot ma mau theo che do (light | warm | dark). Nem neu khong co.</summary>
    public static string Color(string mode, string role)
    {
        var v = Str("color", mode, role);
        return v ?? throw new InvalidOperationException(
            $"tokens/design-tokens.json khong co color.{mode}.{role}");
    }

    private static JsonDocument Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Khong tim thay tai nguyen nhung '{ResourceName}'. Kiem tra muc EmbeddedResource " +
                "trong Tsudev.Audit.Core.csproj - duong dan la ../../tokens/design-tokens.json.");
        return JsonDocument.Parse(stream);
    }

    private static string? Str(params string[] path)
    {
        var el = _doc.Value.RootElement;
        foreach (var key in path)
        {
            if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(key, out el)) return null;
        }
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    private static string BuildRootCss()
    {
        var root = _doc.Value.RootElement;
        var sb = new StringBuilder();

        sb.Append("/* Sinh tu tokens/design-tokens.json v").Append(Version)
          .Append(" - KHONG sua tay o day, sua trong file token. */\n:root{\n");

        // --- Mau che do sang (mac dinh)
        AppendColors(sb, root, "light");

        // --- Phan khong doi theo che do
        AppendGroup(sb, root, "typography", "font-family", "--font-sans");
        AppendGroup(sb, root, "typography", "font-family-mono", "--font-mono");
        AppendMap(sb, root, "--fs-", "typography", "size");
        AppendMap(sb, root, "--lh-", "typography", "line-height");
        AppendMap(sb, root, "--fw-", "typography", "weight");
        AppendMap(sb, root, "--ls-", "typography", "letter-spacing");
        AppendMap(sb, root, "--radius-", "radius");
        AppendMap(sb, root, "--sp-", "spacing");
        AppendMap(sb, root, "--shadow-", "shadow");
        AppendMap(sb, root, "--z-", "z-index");
        AppendMap(sb, root, "--motion-", "motion");

        // Khoi ma nguon trong bao cao luon dung nen toi, o CA HAI che do - chu
        // trang tren nen toi la cach doc log/khoa registry de nhat. Lay tu bang
        // mau toi cua chinh bo token, khong tu bia mau moi.
        sb.Append("  --c-code-bg:").Append(Color("dark", "bg-base")).Append(";\n");
        sb.Append("  --c-code-ink:").Append(Color("dark", "text-primary")).Append(";\n");

        // Dau trang giu MOT dien mao o ca hai che do, nen luon lay tu bang mau
        // SANG va KHONG bi ghi de trong khoi che do toi.
        //
        // Ly do khong de no lat theo che do nhu phan con lai: o che do toi,
        // primary la #66A3F2 - dau trang se thanh nen SANG. Chu ky thuong hieu
        // duoc chinh cho nen dam se tut xuong 2,4:1, duoi ca nguong 3:1 danh cho
        // chu co lon. Dau trang la mang thuong hieu chu khong phai mot be mat
        // doc noi dung, nen giu co dinh la dung hon la ep no doi mau.
        sb.Append("  --c-hero-from:").Append(Color("light", "primary-active")).Append(";\n");
        sb.Append("  --c-hero-to:").Append(Color("light", "primary")).Append(";\n");
        sb.Append("  --c-hero-ink:").Append(Color("light", "on-primary")).Append(";\n");

        AppendBrand(sb, forDarkScheme: false);
        sb.Append("}\n");

        // --- Che do toi theo he dieu hanh (DESIGN_SYSTEM.md muc 1)
        sb.Append("@media (prefers-color-scheme:dark){\n  :root{\n");
        AppendColors(sb, root, "dark", indent: "    ");
        AppendBrand(sb, forDarkScheme: true, indent: "    ");
        sb.Append("  }\n}\n");

        // --- In ra giay: LUON dung bang mau sang. Khong co dieu nay thi may dat
        //     che do toi se in ra trang giay den kin muc.
        sb.Append("@media print{\n  :root{\n");
        AppendColors(sb, root, "light", indent: "    ");
        AppendBrand(sb, forDarkScheme: false, indent: "    ");
        sb.Append("  }\n}\n");

        return sb.ToString();
    }

    // ---------------------------------------------------------------------
    // MAU CHU KY THUONG HIEU tsudev - 4 gia tri duy nhat con viet cung trong ma.
    //
    // CO Y nam ngoai tokens/design-tokens.json: bo token mo ta VAI TRO giao dien
    // (nen, chu, nguy hiem...) va duoc phep doi theo san pham; con day la ban sac
    // thuong hieu - doi mau primary thi chu ky "tsudev" van phai giu nguyen mau.
    // Bo quy uoc hien CHUA co o cho thuong hieu. Neu chu project muon dua chung
    // vao token thi day dung la 4 gia tri can chuyen - docs/STATE.md muc QU-4.
    //
    // Moi sac co hai ban: mot cho nen DAM, mot cho nen SANG. Dau trang luon la
    // nen dam nen dung ban on-dark co dinh; chan trang nam trong nen trang nen
    // phai lat khi he dieu hanh o che do toi.
    private const string BrandTsuOnDark = "#8FD0FF";
    private const string BrandDevOnDark = "#FFA94D";
    private const string BrandTsuOnLight = "#1C5FBF";
    private const string BrandDevOnLight = "#D2690A";

    private static void AppendBrand(StringBuilder sb, bool forDarkScheme, string indent = "  ")
    {
        // Dau trang luon la nen DAM (xem --c-hero-* o tren) nen sac chu ky tren
        // do khong doi. Chan trang thi nam trong nen trang, phai lat theo che do.
        if (!forDarkScheme)
        {
            sb.Append(indent).Append("--brand-tsu-hero:").Append(BrandTsuOnDark).Append(";\n");
            sb.Append(indent).Append("--brand-dev-hero:").Append(BrandDevOnDark).Append(";\n");
        }

        sb.Append(indent).Append("--brand-tsu-foot:")
          .Append(forDarkScheme ? BrandTsuOnDark : BrandTsuOnLight).Append(";\n");
        sb.Append(indent).Append("--brand-dev-foot:")
          .Append(forDarkScheme ? BrandDevOnDark : BrandDevOnLight).Append(";\n");
    }

    private static void AppendColors(StringBuilder sb, JsonElement root, string mode, string indent = "  ")
    {
        if (!root.TryGetProperty("color", out var colors) || !colors.TryGetProperty(mode, out var set))
            throw new InvalidOperationException($"tokens/design-tokens.json khong co color.{mode}");

        foreach (var p in set.EnumerateObject())
        {
            if (IsNotValue(p)) continue;
            sb.Append(indent).Append("--c-").Append(p.Name).Append(':').Append(p.Value.GetString()).Append(";\n");
        }
    }

    private static void AppendGroup(StringBuilder sb, JsonElement root, string group, string key, string cssName)
    {
        if (root.TryGetProperty(group, out var g) && g.TryGetProperty(key, out var v))
            sb.Append("  ").Append(cssName).Append(':').Append(v.GetString()).Append(";\n");
    }

    private static void AppendMap(StringBuilder sb, JsonElement root, string prefix,
                                  string group, string? sub = null)
    {
        if (!root.TryGetProperty(group, out var el)) return;
        if (sub is not null && !el.TryGetProperty(sub, out el)) return;

        foreach (var p in el.EnumerateObject())
        {
            if (IsNotValue(p)) continue;
            var value = p.Value.ValueKind == JsonValueKind.Number
                ? p.Value.GetRawText()
                : p.Value.GetString();
            sb.Append("  ").Append(prefix).Append(p.Name).Append(':').Append(value).Append(";\n");
        }
    }

    private static bool IsNotValue(JsonProperty p)
        => Array.Exists(NotValues, n => string.Equals(n, p.Name, StringComparison.Ordinal))
           || p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
}
