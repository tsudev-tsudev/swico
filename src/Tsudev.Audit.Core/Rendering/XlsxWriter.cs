using System.Globalization;
using System.IO.Compression;
using System.Text;
using Tsudev.Audit.Core.Models;

namespace Tsudev.Audit.Core.Rendering;

/// <summary>
/// Ghi file .xlsx theo chuan OOXML (SpreadsheetML) bang tay.
///
/// Vi sao KHONG dung ClosedXML/EPPlus? Cong cu nay se duoc ky so va phat hanh
/// rong rai; moi goi phu thuoc them vao la them mot giay phep phai ra soat, mot
/// nguon lo hong phai theo doi, va them dung luong cho file exe don. Dinh dang
/// can dung o day rat don gian - vai bang phang, khong cong thuc, khong bieu do
/// - nen tu ghi la danh doi hop ly.
///
/// File .xlsx thuc chat la mot file ZIP chua cac phan XML. Cau truc toi thieu:
///   [Content_Types].xml      khai bao kieu MIME cho tung phan
///   _rels/.rels              tro toi workbook
///   xl/workbook.xml          danh sach sheet
///   xl/_rels/workbook.xml.rels  anh xa sheet -> file
///   xl/styles.xml            dinh dang (in dam cho dong tieu de)
///   xl/worksheets/sheetN.xml du lieu
///
/// Chuoi duoc ghi dang "inline string" thay vi bang sharedStrings: ton dung
/// luong hon mot chut nhung bo han mot phan XML va mot lop danh chi muc.
/// </summary>
public sealed class XlsxWriter
{
    private const int MaxSheetNameLength = 31;

    /// <summary>Ky tu Excel CAM dat trong ten sheet.</summary>
    private static readonly char[] InvalidSheetChars = { '\\', '/', '?', '*', '[', ']', ':' };

    public void Write(string path, IEnumerable<DataTable> tables)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(tables);

        var list = tables.ToList();
        if (list.Count == 0)
            list.Add(DataTable.Create("Không có dữ liệu", "Thông báo").AddRow("Báo cáo không có bảng nào."));

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sheets = list.Select((t, i) => (
            Table: t,
            Index: i + 1,
            Name: MakeSafeSheetName(string.IsNullOrWhiteSpace(t.Title) ? $"Bảng {i + 1}" : t.Title, usedNames)
        )).ToList();

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        AddEntry(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
        AddEntry(zip, "_rels/.rels", RootRels);
        AddEntry(zip, "xl/workbook.xml", Workbook(sheets.Select(s => s.Name).ToList()));
        AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
        AddEntry(zip, "xl/styles.xml", Styles);

        foreach (var s in sheets)
            AddEntry(zip, $"xl/worksheets/sheet{s.Index}.xml", Sheet(s.Table));
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    // ------------------------------------------------------------- cac phan

    private static string ContentTypes(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        sb.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        sb.Append("""<Default Extension="xml" ContentType="application/xml"/>""");
        sb.Append("""<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        sb.Append("""<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i)
              .Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        sb.Append("</Types>");
        return sb.ToString();
    }

    private const string RootRels = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
        """;

    private static string Workbook(IReadOnlyList<string> sheetNames)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>""");
        for (int i = 0; i < sheetNames.Count; i++)
            sb.Append("<sheet name=\"").Append(X(sheetNames[i]))
              .Append("\" sheetId=\"").Append(i + 1)
              .Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
        sb.Append("</sheets></workbook>");
        return sb.ToString();
    }

    private static string WorkbookRels(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (int i = 1; i <= sheetCount; i++)
            sb.Append("<Relationship Id=\"rId").Append(i)
              .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet")
              .Append(i).Append(".xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    /// <summary>
    /// Bang dinh dang: chi so 0 = mac dinh, chi so 1 = tieu de (in dam, nen xam,
    /// co vien duoi). Cac o du lieu tham chieu s="0", o tieu de tham chieu s="1".
    /// </summary>
    private const string Styles = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><color rgb="FF14417F"/><name val="Calibri"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FFEAF2FC"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="2"><border><left/><right/><top/><bottom/><diagonal/></border><border><left/><right/><top/><bottom style="thin"><color rgb="FFCBD9EA"/></bottom><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf><xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment vertical="center"/></xf></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>
        """;

    private static string Sheet(DataTable table)
    {
        var sb = new StringBuilder(8 * 1024);
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        // Dong bang dong tieu de. LUU Y THU TU: theo luoc do SpreadsheetML,
        // <sheetViews> phai dung TRUOC <cols> va <sheetData>. Dat sai cho thi
        // Excel bao file hong va doi sua - LibreOffice thi bo qua, nen loi nay
        // rat de lot luoi neu chi thu tren Linux.
        sb.Append("""<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/><selection pane="bottomLeft" activeCell="A2" sqref="A2"/></sheetView></sheetViews>""");

        // Do rong cot uoc luong theo noi dung, chan tren 60 de cot khong bi keo
        // dai vo han khi gap mot o duy nhat qua dai (vi du duong dan file).
        if (table.Columns.Count > 0)
        {
            sb.Append("<cols>");
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var width = table.Columns[c].Length;
                foreach (var row in table.Rows)
                    if (c < row.Length && row[c] is { } cell) width = Math.Max(width, cell.Length);
                sb.Append("<col min=\"").Append(c + 1).Append("\" max=\"").Append(c + 1)
                  .Append("\" width=\"").Append(Math.Clamp(width + 2, 10, 60).ToString(CultureInfo.InvariantCulture))
                  .Append("\" customWidth=\"1\"/>");
            }
            sb.Append("</cols>");
        }

        sb.Append("<sheetData>");

        // Dong 1: tieu de cot, dong bang de cuon van thay
        sb.Append("<row r=\"1\">");
        for (int c = 0; c < table.Columns.Count; c++)
            AppendInlineCell(sb, ColumnName(c + 1), 1, table.Columns[c], styleIndex: 1);
        sb.Append("</row>");

        int rowNumber = 2;
        foreach (var row in table.Rows)
        {
            sb.Append("<row r=\"").Append(rowNumber).Append("\">");
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var value = c < row.Length ? row[c] ?? "" : "";
                var reference = ColumnName(c + 1);

                // Chuoi trong nhu "42" duoc ghi thanh SO de Excel sap xep/tinh
                // duoc. Nhung "007" giu nguyen la chuoi - do la ma so, mat so 0
                // dau la mat thong tin.
                if (IsNumeric(value, out var number))
                    sb.Append("<c r=\"").Append(reference).Append(rowNumber).Append("\" s=\"0\"><v>")
                      .Append(number.ToString(CultureInfo.InvariantCulture)).Append("</v></c>");
                else
                    AppendInlineCell(sb, reference, rowNumber, value, styleIndex: 0);
            }
            sb.Append("</row>");
            rowNumber++;
        }

        sb.Append("</sheetData>");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static void AppendInlineCell(StringBuilder sb, string column, int row, string value, int styleIndex)
        => sb.Append("<c r=\"").Append(column).Append(row)
             .Append("\" s=\"").Append(styleIndex)
             .Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
             .Append(X(value)).Append("</t></is></c>");

    // ------------------------------------------------------- ham dung chung

    /// <summary>
    /// Quy doi chi so cot (bat dau tu 1) sang ten cot Excel: 1 -&gt; A,
    /// 26 -&gt; Z, 27 -&gt; AA. Day la he co so 26 KHONG co chu so 0, nen phai
    /// tru 1 truoc moi phep chia - cho nay rat de viet sai.
    /// </summary>
    public static string ColumnName(int index)
    {
        if (index < 1) throw new ArgumentOutOfRangeException(nameof(index), index, "Chỉ số cột bắt đầu từ 1.");

        var sb = new StringBuilder(3);
        while (index > 0)
        {
            index--;
            sb.Insert(0, (char)('A' + index % 26));
            index /= 26;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Chuoi nay co nen ghi vao Excel duoi dang SO khong?
    ///
    /// Quy tac co chu dich chat hon <c>double.TryParse</c>: chuoi co so 0 o dau
    /// ("007") bi coi la CHUOI, vi trong bao cao kiem toan nhung chuoi kieu do
    /// gan nhu luon la ma so - phien ban, ma san pham, so seri. Bien chung thanh
    /// so se lam mat cac so 0 dau va lam sai du lieu.
    /// </summary>
    public static bool IsNumeric(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.Trim();
        if (s.Length == 0) return false;

        // Chi chap nhan dang so toi gian: dau tru tuy chon, chu so, mot dau cham
        // thap phan. Khong chap nhan dau cong, dau phan cach nghin, ky hieu mu.
        int i = s[0] == '-' ? 1 : 0;
        if (i >= s.Length) return false;              // rieng chuoi "-"

        // Loai bo so 0 dan dau: "007" la ma so, khong phai so 7.
        if (s[i] == '0' && i + 1 < s.Length && s[i + 1] != '.') return false;

        bool seenDot = false, seenDigit = false;
        for (; i < s.Length; i++)
        {
            if (s[i] == '.')
            {
                if (seenDot) return false;
                seenDot = true;
            }
            else if (char.IsAsciiDigit(s[i])) seenDigit = true;
            else return false;
        }

        return seenDigit && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Bien tieu de bang thanh ten sheet hop le: bo ky tu cam, cat con 31 ky tu,
    /// va bao dam KHONG TRUNG voi ten da dung (Excel se tu choi mo file neu
    /// trung). Khi phai cat ngan, hau to " (2)", " (3)"... duoc them vao va phan
    /// than bi cat bot tuong ung de tong do dai van trong gioi han.
    /// </summary>
    public static string MakeSafeSheetName(string? title, HashSet<string> usedNames)
    {
        ArgumentNullException.ThrowIfNull(usedNames);

        var cleaned = new string((title ?? "").Where(ch => !InvalidSheetChars.Contains(ch)).ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "Bảng";

        // Excel cam dau nhay don o dau va cuoi ten sheet.
        cleaned = cleaned.Trim('\'');
        if (cleaned.Length == 0) cleaned = "Bảng";

        var candidate = cleaned.Length <= MaxSheetNameLength ? cleaned : cleaned[..MaxSheetNameLength];

        int suffix = 2;
        while (!usedNames.Add(candidate))
        {
            var tag = $" ({suffix++})";
            var keep = Math.Min(cleaned.Length, MaxSheetNameLength - tag.Length);
            candidate = cleaned[..keep] + tag;
        }

        return candidate;
    }

    /// <summary>Escape XML. Ky tu dieu khien bi loai bo vi XML 1.0 khong chap nhan.</summary>
    private static string X(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var sb = new StringBuilder(value.Length + 16);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '&': sb.Append("&amp;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&apos;"); break;
                default:
                    if (ch is '\t' or '\n' or '\r' || ch >= ' ') sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}
