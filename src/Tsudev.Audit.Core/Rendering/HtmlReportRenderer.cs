using System.Globalization;
using System.Net;
using System.Text;
using Tsudev.Audit.Core.Models;

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
    public const string BrandName = "tsuowlit";

    /// <summary>Ten san pham day du, hien o chan trang va the &lt;meta generator&gt;.</summary>
    public const string ProductName = "tsuowlit SWICO";

    /// <summary>Mau nen chu dao - theme xanh nhat, dung THONG NHAT ca bao cao lan dashboard.</summary>
    public const string ThemeBackground = "#eaf2fc";

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
        sb.Append("<meta name=\"generator\" content=\"").Append(E(ProductName)).Append("\">\n");
        sb.Append("<title>").Append(E(report.Title)).Append(" - ").Append(E(report.ComputerName)).Append("</title>\n");
        sb.Append("<style>\n").Append(Css).Append("</style>\n</head>\n<body>\n");

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
        sb.Append("<a class=\"brand\" href=\"").Append(BrandUrl)
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">").Append(E(BrandName)).Append("</a>\n");
        if (!string.IsNullOrWhiteSpace(dashboardLink))
            sb.Append("<a class=\"back\" href=\"").Append(E(dashboardLink)).Append("\">&larr; Trang tổng hợp</a>\n");
        sb.Append("</div>\n");

        sb.Append("<h1>").Append(E(report.Title)).Append("</h1>\n");
        sb.Append("<dl class=\"meta\">");
        AppendMeta(sb, "Máy", report.ComputerName);
        AppendMeta(sb, "Thời điểm quét",
            report.ScanTime.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.GetCultureInfo("vi-VN")));
        AppendMeta(sb, "Phiên bản dữ liệu", report.SchemaVersion);
        sb.Append("</dl>\n</div>\n</header>\n");
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
        sb.Append("<p>Báo cáo sinh tự động bởi <a href=\"").Append(BrandUrl)
          .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">").Append(E(ProductName))
          .Append("</a> &middot; lược đồ dữ liệu ").Append(E(report.SchemaVersion)).Append("</p>\n");
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
    private const string Css = """
        :root{
          --bg:#eaf2fc; --surface:#ffffff; --ink:#132033; --muted:#5b6b80;
          --line:#cbd9ea; --accent:#1c5fbf; --accent-dark:#14417f;
          --ok:#16794a; --ok-bg:#e3f5ec; --warn:#8a5a00; --warn-bg:#fdf3dc;
          --bad:#a51f2b; --bad-bg:#fdeaec; --neutral-bg:#eef2f7;
        }
        *{box-sizing:border-box}
        body{margin:0;background:var(--bg);color:var(--ink);
             font:15px/1.6 "Segoe UI",system-ui,-apple-system,Roboto,Arial,sans-serif}
        .wrap{max-width:1180px;margin:0 auto;padding:0 20px}
        a{color:var(--accent)}
        .muted{color:var(--muted)}

        .hero{background:linear-gradient(160deg,var(--accent-dark),var(--accent));color:#fff;padding:22px 0 26px}
        .hero-top{display:flex;justify-content:space-between;align-items:center;gap:16px;margin-bottom:14px}
        .brand{font-weight:700;letter-spacing:.4px;color:#fff;text-decoration:none;font-size:18px}
        .back{color:#dceafd;text-decoration:none;font-size:14px}
        .back:hover{text-decoration:underline}
        .hero h1{margin:0 0 12px;font-size:26px;line-height:1.25}
        .meta{display:flex;flex-wrap:wrap;gap:10px 28px;margin:0}
        .meta dt{font-size:12px;text-transform:uppercase;letter-spacing:.6px;color:#c7dcfa;margin:0}
        .meta dd{margin:0;font-weight:600}

        .nav{position:sticky;top:0;z-index:5;background:var(--surface);border-bottom:1px solid var(--line);
             box-shadow:0 1px 3px rgba(19,32,51,.06)}
        .nav-inner{display:flex;flex-wrap:wrap;gap:4px;overflow-x:auto}
        .nav a{padding:11px 12px;text-decoration:none;font-size:14px;white-space:nowrap;
               border-bottom:2px solid transparent}
        .nav a:hover{border-bottom-color:var(--accent);background:var(--neutral-bg)}

        .cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:14px;margin:22px 0}
        .stat{background:var(--surface);border:1px solid var(--line);border-radius:10px;padding:16px 18px;
              display:flex;flex-direction:column;gap:4px}
        .stat-value{font-size:24px;font-weight:700;line-height:1.2}
        .stat-label{font-size:13px;color:var(--muted)}

        .card{background:var(--surface);border:1px solid var(--line);border-radius:12px;
              padding:22px 24px;margin:20px 0;box-shadow:0 1px 2px rgba(19,32,51,.04)}
        .card h2{margin:0 0 14px;font-size:19px;padding-bottom:10px;border-bottom:2px solid var(--line)}
        .card h3{margin:20px 0 8px;font-size:15px;color:var(--accent-dark)}
        .desc{margin:0 0 12px;color:var(--muted);font-size:14px}

        .warn-box{border-left:5px solid var(--warn)}
        .warn-box ul{margin:0;padding-left:20px}
        .warn-box li{margin:4px 0}

        .badges{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:12px}
        .badge{display:inline-block;padding:3px 11px;border-radius:999px;font-size:12.5px;font-weight:600;
               border:1px solid transparent}
        .lv-none{background:var(--neutral-bg);color:var(--muted);border-color:var(--line)}
        .lv-low{background:var(--ok-bg);color:var(--ok);border-color:#b6e0cb}
        .lv-medium{background:var(--warn-bg);color:var(--warn);border-color:#efd8a4}
        .lv-high{background:var(--bad-bg);color:var(--bad);border-color:#f2c3c8}
        .lv-critical{background:var(--bad);color:#fff;border-color:var(--bad)}

        .verdict{border-radius:10px;padding:14px 18px;margin:0 0 16px;border-left:5px solid}
        .verdict p{margin:6px 0 0;font-size:14px}
        .vd-ok{background:var(--ok-bg);border-color:var(--ok)}
        .vd-warn{background:var(--warn-bg);border-color:var(--warn)}
        .vd-bad{background:var(--bad-bg);border-color:var(--bad)}
        .vd-unknown{background:var(--neutral-bg);border-color:var(--muted)}

        .filter{width:100%;max-width:340px;margin:0 0 10px;padding:8px 12px;font-size:14px;
                border:1px solid var(--line);border-radius:8px;background:#fff;color:inherit}
        .table-scroll{overflow-x:auto}
        table{width:100%;border-collapse:collapse;font-size:14px}
        th,td{padding:9px 12px;text-align:left;border-bottom:1px solid var(--line);vertical-align:top}
        th{background:var(--neutral-bg);font-weight:600;white-space:nowrap;position:sticky;top:0}
        tbody tr:nth-child(even){background:#f7fafd}
        tbody tr:hover{background:#e9f1fb}

        pre.raw{background:#0f1b2b;color:#dbe7f5;padding:14px 16px;border-radius:8px;overflow-x:auto;
                font:13px/1.5 Consolas,"Courier New",monospace;white-space:pre-wrap;word-break:break-word}
        details.method{margin-top:14px;font-size:14px;color:var(--muted)}
        details.method summary{cursor:pointer;font-weight:600;color:var(--accent-dark)}

        .foot{border-top:1px solid var(--line);margin-top:30px;padding:20px 0 34px;font-size:13.5px}
        .foot p{margin:4px 0}

        @media print{
          .nav,.filter{display:none}
          body{background:#fff}
          .card{break-inside:avoid;box-shadow:none}
          .hero{background:var(--accent-dark) !important;-webkit-print-color-adjust:exact;print-color-adjust:exact}
        }
        """;
}
