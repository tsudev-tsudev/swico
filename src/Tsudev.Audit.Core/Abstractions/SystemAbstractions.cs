namespace Tsudev.Audit.Core.Abstractions;

/// <summary>
/// Ket qua chay mot tien trinh ngoai (slmgr.vbs, ospp.vbs, DISM, powercfg...).
/// </summary>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
    public string CombinedOutput => string.IsNullOrEmpty(StandardError)
        ? StandardOutput
        : StandardOutput + "\n" + StandardError;

    public static ProcessResult Failed(string message) => new(-1, "", message);
}

/// <summary>
/// Truy van WMI/CIM. Tach thanh interface de:
///  (1) toan bo logic collector nam trong Core (net8.0) va UNIT TEST duoc tren
///      moi nen tang, khong can may Windows;
///  (2) chi mot lop adapter mong duy nhat trong Tsudev.Audit.Windows phu thuoc
///      vao goi System.Management.
/// Moi doi tuong WMI duoc tra ve duoi dang tu dien ten-thuoc-tinh -> gia tri.
/// </summary>
public interface IWmiQuery
{
    /// <summary>
    /// Truy van mot lop WMI. KHONG BAO GIO nem exception - loi duoc ghi vao
    /// <paramref name="warnings"/> va tra ve danh sach rong, vi mot muc thu
    /// thap that bai KHONG duoc lam hong ca bao cao.
    /// </summary>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(
        string className,
        string? whereClause = null,
        string wmiNamespace = "root\\cimv2");
}

/// <summary>Doc registry (chi doc, khong bao gio ghi).</summary>
public interface IRegistryReader
{
    /// <summary>Liet ke ten cac khoa con cua mot duong dan HKLM/HKCU.</summary>
    IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string path);

    /// <summary>Doc mot gia tri; tra ve null neu khong ton tai.</summary>
    object? GetValue(RegistryRoot root, string path, string valueName);
}

public enum RegistryRoot
{
    LocalMachine,
    CurrentUser
}

/// <summary>Chay tien trinh ngoai co gioi han thoi gian.</summary>
public interface IProcessRunner
{
    ProcessResult Run(string fileName, string arguments, int timeoutSeconds = 60);
}

/// <summary>Kiem tra su ton tai cua file/thu muc tren dia.</summary>
public interface IFileProbe
{
    bool FileExists(string path);
    bool DirectoryExists(string path);

    /// <summary>Liet ke thu muc con o LOP TREN CUNG (khong de quy) - tra ve rong neu loi.</summary>
    IReadOnlyList<string> GetDirectories(string path);

    /// <summary>Doc toan bo noi dung file text; tra ve null neu khong doc duoc.</summary>
    string? ReadAllTextOrNull(string path);

    /// <summary>Mo rong bien moi truong (%ProgramFiles%...).</summary>
    string ExpandEnvironment(string path);
}

/// <summary>
/// Gom tat ca cac "cong" (port) truy cap he thong lai mot cho, de collector
/// chi can nhan mot tham so duy nhat.
/// </summary>
public sealed class SystemContext
{
    public required IWmiQuery Wmi { get; init; }
    public required IRegistryReader Registry { get; init; }
    public required IProcessRunner Process { get; init; }
    public required IFileProbe Files { get; init; }

    /// <summary>Ten may. Tach ra de test deterministic.</summary>
    public required string ComputerName { get; init; }

    /// <summary>Thoi diem quet. Tach ra de test deterministic.</summary>
    public required DateTimeOffset ScanTime { get; init; }

    /// <summary>Script co dang chay voi quyen Administrator hay khong.</summary>
    public bool IsElevated { get; init; }

    /// <summary>Canh bao thu thap duoc trong qua trinh chay (hien minh bach trong bao cao).</summary>
    public List<string> Warnings { get; } = new();

    public void Warn(string message)
    {
        if (!Warnings.Contains(message)) Warnings.Add(message);
    }
}

/// <summary>Tien ich doc gia tri tu tu dien WMI mot cach an toan.</summary>
public static class WmiExtensions
{
    public static string Str(this IReadOnlyDictionary<string, object?> row, string key, string fallback = "-")
    {
        if (!row.TryGetValue(key, out var v) || v is null) return fallback;
        var s = v.ToString();
        return string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();
    }

    public static long? Num(this IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return null;
        if (v is long l) return l;
        if (v is int i) return i;
        if (v is uint ui) return ui;
        if (v is ulong ul) return (long)ul;
        if (v is short s) return s;
        if (v is ushort us) return us;
        return long.TryParse(v.ToString(), out var parsed) ? parsed : null;
    }

    public static bool? Bool(this IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return null;
        if (v is bool b) return b;
        return bool.TryParse(v.ToString(), out var parsed) ? parsed : null;
    }
}

/// <summary>
/// Nguon thong tin ban phat hanh moi nhat.
///
/// Tach thanh cong (port) vi hai ly do:
///  (1) toan bo logic quyet dinh "co phai cap nhat khong" nam trong Core va
///      unit-test duoc ma KHONG can mang;
///  (2) day la lan DUY NHAT cong cu cham toi Internet - go gon vao mot giao
///      dien de nguoi ra soat bao mat kiem chung duoc dieu do trong vai giay.
/// </summary>
public interface IUpdateFeed
{
    /// <summary>
    /// Lay ban phat hanh moi nhat. KHONG BAO GIO nem exception - ly do that bai
    /// duoc dat vao <paramref name="failureReason"/> va tra ve null.
    /// </summary>
    Tsudev.Audit.Core.Updates.ReleaseInfo? GetLatest(out string? failureReason);
}

/// <summary>
/// Ham tien ich cho ket qua truy van WMI.
/// </summary>
public static class WmiResultExtensions
{
    /// <summary>
    /// Lay dong dau tien, hoac null neu khong co dong nao.
    ///
    /// Dung ham nay thay cho <c>FirstOrDefault()</c> cua LINQ: ket qua truy van
    /// la <see cref="IReadOnlyList{T}"/> nen truy cap theo chi so truc tiep
    /// duoc, khong can dung toi bo dem lap.
    /// </summary>
    public static IReadOnlyDictionary<string, object?>? FirstRowOrNull(
        this IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows.Count > 0 ? rows[0] : null;
    }
}
