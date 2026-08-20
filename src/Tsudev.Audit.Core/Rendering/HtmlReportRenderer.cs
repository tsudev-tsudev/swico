using System.Net;
using System.Text;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Reports;

namespace Tsudev.Audit.Core.Rendering;

/// <summary>
/// Dung trang HTML tu mot <see cref="AuditReport"/>.
///
/// Nguyen tac bat buoc: MOI chuoi co nguon goc tu du lieu thu thap deu phai di
/// qua <see cref="E"/> truoc khi ghep vao HTML. Ten may, ten phan mem, duong dan
/// file... deu do nguoi khac dat ten - coi chung nhu du lieu KHONG dang tin.
/// Chi CSS/HTML do chinh lop nay sinh ra moi duoc ghep truc tiep.
/// </summary>
public sealed class HtmlReportRenderer
{
    public const string BrandUrl = "https://tsudev.com";
    public const string BrandName = "tsudev";

    /// <summary>Ten san pham day du, hien o chan trang va the &lt;meta generator&gt;.</summary>
    public const string ProductName = "tsudev SWICO";

    /// <summary>Mau nen chu dao - theme xanh nhat, dung THONG NHAT ca bao cao lan dashboard.</summary>
    public const string ThemeBackground = "#eaf2fc";

    /// <summary>
    /// Favicon 32x32 nhung san. Bao cao thuong duoc mo tu file tren dia
    /// (giao thuc file://) nen KHONG the tro toi mot file favicon ben ngoai -
    /// trinh duyet se khong tim thay. Nhung thang la cach duy nhat de tab
    /// trinh duyet co bieu tuong khi nguoi dung mo nhieu bao cao cung luc.
    /// Dung ban 32px (~2,3 KB) chu khong dung logo day du.
    /// </summary>
    private const string FaviconDataUri = "data:image/png;base64," +
        "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" +
        "YII=";

    /// <summary>
    /// Logo tsudev NHUNG THANG vao trang duoi dang data URI.
    ///
    /// Vi sao khong tro toi mot file anh ben ngoai: bao cao phai xem duoc khi
    /// KHONG co mang, va phai copy sang may khac duoc ma khong keo theo file
    /// phu nao - do la cach nguoi dung thuc su gom ket qua tu nhieu may.
    /// Mot the &lt;img src="logo.png"&gt; se thanh o anh vo ngay khi ai do copy
    /// mot minh file .html di.
    ///
    /// Ban nhung la ban da thu nho con cao 72px (~10 KB) chu KHONG phai file
    /// goc 115 KB - sinh boi packaging/tools/make-assets.py.
    /// </summary>
    private const string LogoDataUri = "data:image/png;base64," +
        "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" +
        "/+XX/T/E9hwVyP+3/AAAAABJRU5ErkJggg==";

    /// <summary>
    /// Ghi chu ve giao dien: bao cao nay CO CHU DICH khong dung
    /// dark-mode tu dong. Ly do: bao cao thuong duoc in ra giay hoac xuat PDF
    /// de luu ho so kiem tra; mot trang tu doi sang nen den theo thiet lap may
    /// nguoi xem se cho ra ban in khong dung y va ton muc.
    /// </summary>
    public string Render(AuditReport report, string? dashboardRelativeLink = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder(64 * 1024);
        sb.Append("<!DOCTYPE html>\n<html lang=\"vi\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<link rel=\"icon\" type=\"image/png\" sizes=\"32x32\" href=\"")
          .Append(FaviconDataUri).Append("\">\n");
        // Mau thanh dia chi trinh duyet lay tu token, khong viet cung. Dung ban
        // SANG vi day la mau dau trang o che do mac dinh.
        sb.Append("<meta name=\"theme-color\" content=\"")
          .Append(DesignTokens.Color("light", "primary-active")).Append("\">\n");
        sb.Append("<meta name=\"generator\" content=\"").Append(E(ProductName)).Append("\">\n");
        sb.Append("<title>").Append(E(report.Title)).Append(" - ").Append(E(report.ComputerName)).Append("</title>\n");
        sb.Append("<style>\n").Append(DesignTokens.RootCss).Append(Css)
          .Append("</style>\n</head>\n<body>\n");

        AppendHeader(sb, report, dashboardRelativeLink);
        AppendNav(sb, report);

        sb.Append("<main class=\"wrap\">\n");
        AppendWarnings(sb, report);
        AppendSummaryCards(sb, report);
        foreach (var section in report.Sections) AppendSection(sb, section);
        sb.Append("</main>\n");

        AppendFooter(sb, report);
        sb.Append("</body>\n</html>");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- header

    private static void AppendHeader(StringBuilder sb, AuditReport report, string? dashboardLink)
    {
        sb.Append("<header class=\"hero\">\n<div class=\"wrap\">\n");
        sb.Append("<div class=\"hero-top\">\n");
        AppendBrand(sb, "brand");
        if (!string.IsNullOrWhiteSpace(dashboardLink))
            sb.Append("<a class=\"back\" href=\"").Append(E(dashboardLink)).Append("\">&larr; Trang tổng hợp</a>\n");
        sb.Append("</div>\n");

        sb.Append("<h1>").Append(E(report.Title)).Append("</h1>\n");
        sb.Append("<dl class=\"meta\">");
        AppendMeta(sb, "Máy", report.ComputerName);
        AppendMeta(sb, "Thời điểm quét",
            DateDisplay.DateTimeText(report.ScanTime));
        AppendMeta(sb, "Phiên bản dữ liệu", report.SchemaVersion);
        sb.Append("</dl>\n</div>\n</header>\n");
    }

    /// <summary>
    /// Logo + chu ky thuong hieu, ca hai deu la mot lien ket duy nhat toi
    /// tsudev.com. Chu "tsu" mau xanh, "dev" mau cam - dat bang the &lt;span&gt;
    /// rieng thay vi mot anh chu, de chu van sac net o moi do phan giai va van
    /// doc duoc khi in ra giay.
    /// </summary>
    private static void AppendBrand(StringBuilder sb, string cssClass)
    {
        sb.Append("<a class=\"").Append(cssClass).Append("\" href=\"").Append(BrandUrl)
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"")
          .Append(E(BrandName)).Append(" - ").Append(BrandUrl).Append("\">");
        // Anh nen thay vi the <img>: du lieu base64 nam trong CSS nen chi xuat
        // hien MOT lan trong ca trang, du thuong hieu duoc hien o nhieu cho.
        // role+aria-label giu nguyen y nghia cho trinh doc man hinh.
        sb.Append("<span class=\"brand-logo\" role=\"img\" aria-label=\"")
          .Append(E(BrandName)).Append("\"></span>");
        sb.Append("<span class=\"brand-word\"><span class=\"bw-tsu\">tsu</span><span class=\"bw-dev\">dev</span></span>");
        sb.Append("</a>\n");
    }

    private static void AppendMeta(StringBuilder sb, string label, string value)
        => sb.Append("<div><dt>").Append(E(label)).Append("</dt><dd>").Append(E(value)).Append("</dd></div>");

    private static void AppendNav(StringBuilder sb, AuditReport report)
    {
        var items = report.Sections.Where(s => !string.IsNullOrWhiteSpace(s.Id)).ToList();
        if (items.Count == 0) return;

        sb.Append("<nav class=\"nav\"><div class=\"wrap nav-inner\">");
        foreach (var s in items)
            sb.Append("<a href=\"#").Append(E(s.Id)).Append("\">")
              .Append(E(string.IsNullOrWhiteSpace(s.NavLabel) ? s.Heading : s.NavLabel)).Append("</a>");
        sb.Append("</div></nav>\n");
    }

    // -------------------------------------------------------------- warnings

    private static void AppendWarnings(StringBuilder sb, AuditReport report)
    {
        if (report.Warnings.Count == 0) return;

        sb.Append("<section class=\"card warn-box\">\n<h2>Mục thiếu dữ liệu</h2>\n");
        sb.Append("<p class=\"muted\">Các mục dưới đây không thu thập được. Báo cáo vẫn đầy đủ ở phần còn lại; ")
          .Append("liệt kê ra đây để người đọc biết chỗ nào là khoảng trống chứ không phải kết luận \"sạch\".</p>\n<ul>\n");
        foreach (var w in report.Warnings) sb.Append("<li>").Append(E(w)).Append("</li>\n");
        sb.Append("</ul>\n</section>\n");
    }

    private static void AppendSummaryCards(StringBuilder sb, AuditReport report)
    {
        if (report.SummaryCards.Count == 0) return;

        sb.Append("<section class=\"cards\">\n");
        foreach (var c in report.SummaryCards)
            sb.Append("<div class=\"stat\"><span class=\"stat-value\">").Append(E(c.Value))
              .Append("</span><span class=\"stat-label\">").Append(E(c.Label)).Append("</span></div>\n");
        sb.Append("</section>\n");
    }

    // -------------------------------------------------------------- sections

    private static void AppendSection(StringBuilder sb, ReportSection section)
    {
        sb.Append("<section class=\"card\" id=\"").Append(E(section.Id)).Append("\">\n");
        sb.Append("<h2>").Append(E(section.Heading)).Append("</h2>\n");

        if (section.Badges.Count > 0)
        {
            sb.Append("<div class=\"badges\">");
            foreach (var b in section.Badges)
                sb.Append("<span class=\"badge ").Append(RiskClass(b.Level)).Append("\">").Append(E(b.Text)).Append("</span>");
            sb.Append("</div>\n");
        }

        if (!string.IsNullOrWhiteSpace(section.Description))
            sb.Append("<p class=\"desc\">").Append(E(section.Description)).Append("</p>\n");

        if (section.Verdict is { } v)
        {
            sb.Append("<div class=\"verdict ").Append(VerdictClass(v.Level)).Append("\">");
            sb.Append("<strong>").Append(E(v.Title)).Append("</strong>");
            if (!string.IsNullOrWhiteSpace(v.Detail)) sb.Append("<p>").Append(E(v.Detail)).Append("</p>");
            sb.Append("</div>\n");
        }

        foreach (var t in section.Tables) AppendTable(sb, t);

        if (!string.IsNullOrWhiteSpace(section.PreformattedText))
            sb.Append("<pre class=\"raw\">").Append(E(section.PreformattedText)).Append("</pre>\n");

        if (!string.IsNullOrWhiteSpace(section.MethodNote))
            sb.Append("<details class=\"method\"><summary>Về phương pháp và giới hạn</summary><p>")
              .Append(E(section.MethodNote)).Append("</p></details>\n");

        sb.Append("</section>\n");
    }

    private static void AppendTable(StringBuilder sb, DataTable table)
    {
        sb.Append("<div class=\"table-block\">\n");

        if (!string.IsNullOrWhiteSpace(table.Title))
            sb.Append("<h3>").Append(E(table.Title)).Append("</h3>\n");
        if (!string.IsNullOrWhiteSpace(table.Description))
            sb.Append("<p class=\"desc\">").Append(E(table.Description)).Append("</p>\n");

        // O tim kiem dung JS thuan, khong phu thuoc thu vien ngoai: bao cao phai
        // mo duoc tren may khong co internet.
        if (table.Searchable && table.Rows.Count > 0)
            sb.Append("<input class=\"filter\" type=\"search\" placeholder=\"Lọc trong bảng...\" ")
              .Append("oninput=\"tsudevFilter(this)\" aria-label=\"Lọc trong bảng\">\n");

        sb.Append("<div class=\"table-scroll\"><table>\n<thead><tr>");
        foreach (var col in table.Columns) sb.Append("<th>").Append(E(col)).Append("</th>");
        sb.Append("</tr></thead>\n<tbody>\n");

        foreach (var row in table.Rows)
        {
            sb.Append("<tr>");
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var cell = i < row.Length ? row[i] : "-";
                sb.Append("<td>").Append(E(cell)).Append("</td>");
            }
            sb.Append("</tr>\n");
        }

        sb.Append("</tbody>\n</table></div>\n</div>\n");
    }

    private static void AppendFooter(StringBuilder sb, AuditReport report)
    {
        sb.Append("<footer class=\"foot\"><div class=\"wrap\">\n");
        sb.Append("<div class=\"foot-brand\">");
        AppendBrand(sb, "brand brand-sm");
        sb.Append("<p>Báo cáo sinh tự động bởi <a href=\"").Append(BrandUrl)
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">").Append(E(ProductName))
          .Append("</a> &middot; lược đồ dữ liệu ").Append(E(report.SchemaVersion)).Append("</p>");
        sb.Append("</div>\n");
        sb.Append("<p class=\"muted\">Số liệu phản ánh trạng thái máy tại thời điểm quét. ")
          .Append("Đây là dữ liệu kỹ thuật để tham khảo, không phải kết luận pháp lý.</p>\n");
        sb.Append("</div></footer>\n");
        sb.Append("<script>\n").Append(FilterScript).Append("</script>\n");
    }

    // ----------------------------------------------------------------- utils

    /// <summary>
    /// Escape HTML. Day la HANG RAO DUY NHAT chong HTML injection - moi du lieu
    /// thu thap deu phai di qua day. Co unit test bao ve hanh vi nay.
    /// </summary>
    internal static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    internal static string RiskClass(RiskLevel level) => level switch
    {
        RiskLevel.Critical => "lv-critical",
        RiskLevel.High => "lv-high",
        RiskLevel.Medium => "lv-medium",
        RiskLevel.Low => "lv-low",
        _ => "lv-none"
    };

    internal static string VerdictClass(VerdictLevel level) => level switch
    {
        VerdictLevel.Ok => "vd-ok",
        VerdictLevel.Warning => "vd-warn",
        VerdictLevel.Bad => "vd-bad",
        _ => "vd-unknown"
    };

    private const string FilterScript = """
        function tsudevFilter(input) {
          var q = input.value.toLowerCase();
          var block = input.closest('.table-block');
          if (!block) return;
          var rows = block.querySelectorAll('tbody tr');
          for (var i = 0; i < rows.length; i++) {
            rows[i].style.display = rows[i].textContent.toLowerCase().indexOf(q) === -1 ? 'none' : '';
          }
        }
        """;

    /// <summary>
    /// CSS nhung thang vao trang: bao cao phai xem duoc khi khong co mang va khi
    /// duoc copy sang may khac ma khong keo theo file phu nao.
    /// </summary>
    private const string Css = $$"""
        *{box-sizing:border-box}
        body{margin:0;background:var(--c-bg-base);color:var(--c-text-primary);
             font-family:var(--font-sans);font-size:var(--fs-body-web);line-height:var(--lh-body)}
        .wrap{max-width:1180px;margin:0 auto;padding:0 var(--sp-5)}
        a{color:var(--c-text-link)}
        .muted{color:var(--c-text-muted)}
        :focus-visible{outline:2px solid var(--c-focus-ring);outline-offset:2px}

        /* Dau trang GIU nguyen dien mao o ca hai che do - xem DesignTokens. */
        .hero{background:linear-gradient(160deg,var(--c-hero-from),var(--c-hero-to));
              color:var(--c-hero-ink);padding:var(--sp-6) 0}
        .hero-top{display:flex;justify-content:space-between;align-items:center;
                  gap:var(--sp-4);margin-bottom:var(--sp-4)}

        /* Thuong hieu: logo + chu ky. Ca khoi la MOT lien ket toi tsudev.com. */
        .brand{display:inline-flex;align-items:center;gap:var(--sp-3);text-decoration:none;line-height:1}
        .brand-logo{display:block;height:44px;width:35px;flex:none;
                    background:url("{{LogoDataUri}}") no-repeat center/contain}
        .brand-word{font-size:var(--fs-h2);font-weight:var(--fw-bold);letter-spacing:var(--ls-heading)}
        /* Hai sac chu ky DOI CHO nhau giua che do sang va toi - o che do toi dau
           trang thanh nen sang con chan trang thanh nen toi. Xem DesignTokens. */
        .hero .bw-tsu{color:var(--brand-tsu-hero)}
        .hero .bw-dev{color:var(--brand-dev-hero)}
        .brand:hover .brand-word{filter:brightness(1.12)}

        .brand-sm .brand-logo{height:30px}
        .brand-sm .brand-word{font-size:var(--fs-h4)}
        .foot .bw-tsu{color:var(--brand-tsu-foot)}
        .foot .bw-dev{color:var(--brand-dev-foot)}
        .foot-brand{display:flex;align-items:center;gap:var(--sp-4);flex-wrap:wrap}
        .foot-brand p{margin:0}
        .back{color:var(--c-hero-ink);opacity:.85;text-decoration:none;font-size:var(--fs-body-desktop)}
        .back:hover{text-decoration:underline;opacity:1}
        .hero h1{margin:0 0 var(--sp-3);font-size:var(--fs-h1);line-height:var(--lh-heading);
                 letter-spacing:var(--ls-heading)}
        .meta{display:flex;flex-wrap:wrap;gap:var(--sp-3) var(--sp-8);margin:0}
        /* KHONG viet hoa toan bo: nhan o day dai hon 2 tu ("Thoi diem quet") va
           tieng Viet co dau viet hoa het thi kho doc - DESIGN_SYSTEM.md muc 4. */
        .meta dt{font-size:var(--fs-xs);font-weight:var(--fw-medium);
                 color:var(--c-hero-ink);opacity:.78;margin:0}
        .meta dd{margin:0;font-weight:var(--fw-semibold)}

        .nav{position:sticky;top:0;z-index:var(--z-sticky);background:var(--c-bg-surface);
             border-bottom:1px solid var(--c-border);box-shadow:var(--shadow-sm)}
        .nav-inner{display:flex;flex-wrap:wrap;gap:var(--sp-1);overflow-x:auto}
        .nav a{padding:var(--sp-3);text-decoration:none;font-size:var(--fs-body-desktop);
               white-space:nowrap;border-bottom:2px solid transparent}
        .nav a:hover{border-bottom-color:var(--c-primary);background:var(--c-bg-hover)}

        .cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));
               gap:var(--sp-4);margin:var(--sp-6) 0}
        .stat{background:var(--c-bg-surface);border:1px solid var(--c-border);
              border-radius:var(--radius-lg);padding:var(--sp-4);
              display:flex;flex-direction:column;gap:var(--sp-1)}
        .stat-value{font-size:var(--fs-h2);font-weight:var(--fw-bold);line-height:var(--lh-heading)}
        .stat-label{font-size:var(--fs-sm);color:var(--c-text-muted)}

        .card{background:var(--c-bg-surface);border:1px solid var(--c-border);
              border-radius:var(--radius-lg);padding:var(--sp-6);margin:var(--sp-5) 0;
              box-shadow:var(--shadow-sm)}
        .card h2{margin:0 0 var(--sp-4);font-size:var(--fs-h3);padding-bottom:var(--sp-3);
                 line-height:var(--lh-heading);border-bottom:1px solid var(--c-border)}
        .card h3{margin:var(--sp-5) 0 var(--sp-2);font-size:var(--fs-body-web-lg);
                 line-height:var(--lh-heading);color:var(--c-primary-active)}
        /* 72ch: gioi han chieu rong khoi van ban dai de mat khong moi khi dao dong. */
        .desc{margin:0 0 var(--sp-3);max-width:72ch;color:var(--c-text-secondary);
              font-size:var(--fs-body-desktop)}

        .warn-box{border-left:5px solid var(--c-warning)}
        .warn-box ul{margin:0;padding-left:var(--sp-5)}
        .warn-box li{margin:var(--sp-1) 0}

        .badges{display:flex;flex-wrap:wrap;gap:var(--sp-2);margin-bottom:var(--sp-3)}
        /* Nen SURFACE chu khong phai bg-subtle: mau danger tren nen subtle chi dat
           4,3:1, duoi nguong AA 4,5:1 ma DESIGN_SYSTEM.md muc 1 bat buoc. */
        .badge{display:inline-block;padding:var(--sp-1) var(--sp-3);border-radius:var(--radius-sm);
               font-size:var(--fs-xs);font-weight:var(--fw-semibold);
               background:var(--c-bg-surface);border:1px solid var(--c-border)}
        .lv-none{color:var(--c-text-muted)}
        .lv-low{color:var(--c-success)}
        .lv-medium{color:var(--c-warning)}
        .lv-high{color:var(--c-danger)}
        .lv-critical{background:var(--c-danger);color:var(--c-on-status);border-color:var(--c-danger)}

        .verdict{border-radius:var(--radius-lg);padding:var(--sp-4);margin:0 0 var(--sp-4);
                 background:var(--c-bg-subtle);border-left:5px solid}
        .verdict p{margin:var(--sp-2) 0 0;font-size:var(--fs-body-desktop)}
        .vd-ok{border-color:var(--c-success)}
        .vd-warn{border-color:var(--c-warning)}
        .vd-bad{border-color:var(--c-danger)}
        .vd-unknown{border-color:var(--c-border-strong)}

        .filter{width:100%;max-width:340px;margin:0 0 var(--sp-3);
                padding:var(--sp-2) var(--sp-3);font-family:inherit;font-size:var(--fs-body-desktop);
                border:1px solid var(--c-border);border-radius:var(--radius-md);
                background:var(--c-bg-surface);color:inherit}
        .table-scroll{overflow-x:auto;border:1px solid var(--c-border);border-radius:var(--radius-lg)}
        table{width:100%;border-collapse:collapse;font-size:var(--fs-body-desktop)}
        th,td{padding:var(--sp-2) var(--sp-4);text-align:left;
              border-bottom:1px solid var(--c-border);vertical-align:top}
        th{background:var(--c-bg-subtle);font-weight:var(--fw-semibold);
           white-space:nowrap;position:sticky;top:0}
        tbody tr:nth-child(even){background:var(--c-bg-base)}
        tbody tr:hover{background:var(--c-bg-hover)}

        /* Khoi du lieu tho luon dung nen toi o CA HAI che do - chu sang tren nen
           toi la cach doc log va khoa registry de nhat. Mau lay tu bang mau toi
           cua chinh bo token, khong bia them mau moi. */
        pre.raw{background:var(--c-code-bg);color:var(--c-code-ink);padding:var(--sp-4);
                border-radius:var(--radius-md);overflow-x:auto;
                font-family:var(--font-mono);font-size:var(--fs-sm);line-height:var(--lh-body);
                white-space:pre-wrap;word-break:break-word}
        details.method{margin-top:var(--sp-4);font-size:var(--fs-body-desktop);
                       color:var(--c-text-secondary)}
        details.method summary{cursor:pointer;font-weight:var(--fw-semibold);color:var(--c-primary-active)}

        .foot{border-top:1px solid var(--c-border);margin-top:var(--sp-8);
              padding:var(--sp-5) 0 var(--sp-10);font-size:var(--fs-sm)}
        .foot p{margin:var(--sp-1) 0}

        @media print{
          .nav,.filter{display:none}
          /* In ra giay: giu logo va chu ky de ban in van co nhan dien */
          .brand-logo{-webkit-print-color-adjust:exact;print-color-adjust:exact}
          /* Bang mau in da bi ep ve che do SANG o khoi bien do DesignTokens sinh,
             nen khong can ep mau o day nua - chi bo nen mau cua trang. */
          body{background:var(--c-bg-surface)}
          .card{break-inside:avoid;box-shadow:none}
          .hero{background:var(--c-hero-from) !important;
                -webkit-print-color-adjust:exact;print-color-adjust:exact}
        }
        """;
}
