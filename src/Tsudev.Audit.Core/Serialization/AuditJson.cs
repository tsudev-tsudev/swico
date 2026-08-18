using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsudev.Audit.Core.Models;
using Tsudev.Audit.Core.Rules;

namespace Tsudev.Audit.Core.Serialization;

/// <summary>
/// Doc/ghi JSON qua MA DUOC SINH LUC BIEN DICH, khong dung phan chieu.
///
/// Vi sao doi: ban phat hanh duoc cat tia (trimmed) de giam kich thuoc -
/// tu ~29 MB xuong ~9 MB cho file cai dat. Trinh cat tia loai bo moi thanh
/// phan khong duoc tham chieu TRUC TIEP, ma phan chieu thi khong de lai tham
/// chieu nao de nhin thay. Neu giu cach cu, JSON van bien dich duoc nhung khi
/// chay se sinh ra doi tuong RONG - va JSON chinh la thu ma trang tong hop doc
/// lai. Loi kieu do khong bao gio lo ra luc build.
///
/// Sinh ma luc bien dich vua an toan khi cat tia, vua nhanh hon phan chieu.
///
/// TAP TRUNG MOT NOI: moi cho doc/ghi JSON trong du an deu di qua lop nay, de
/// khong con cho nao lot luoi khi doi cach lam.
/// </summary>
public static class AuditJson
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly AuditJsonContext Writer = new(WriteOptions);
    private static readonly AuditJsonContext Reader = new(ReadOptions);

    public static string WriteReport(AuditReport report)
        => JsonSerializer.Serialize(report, Writer.AuditReport);

    public static AuditReport? ReadReport(string json)
        => JsonSerializer.Deserialize(json, Reader.AuditReport);

    public static DetectionRuleSet? ReadRules(string json)
        => JsonSerializer.Deserialize(json, Reader.DetectionRuleSet);

    public static string WriteRules(DetectionRuleSet rules)
        => JsonSerializer.Serialize(rules, Writer.DetectionRuleSet);
}

/// <summary>
/// Ngu canh sinh ma. Moi kieu can doc/ghi JSON phai duoc liet ke o day - neu
/// thieu, trinh bien dich se bao loi ngay thay vi de loi lo ra luc chay.
/// </summary>
[JsonSerializable(typeof(AuditReport))]
[JsonSerializable(typeof(DetectionRuleSet))]
internal sealed partial class AuditJsonContext : JsonSerializerContext;
