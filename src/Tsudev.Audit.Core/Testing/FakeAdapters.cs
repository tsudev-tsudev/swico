using Tsudev.Audit.Core.Abstractions;

namespace Tsudev.Audit.Core.Testing;

/// <summary>
/// Cac adapter GIA LAP dung cho unit test / demo.
/// Nho co lop nay, TOAN BO logic collector (quet crack, tinh diem rui ro,
/// phan loai phan mem, dung bao cao) deu kiem thu duoc tren MOI nen tang
/// ma KHONG can may Windows that.
/// </summary>
public sealed class FakeWmiQuery : IWmiQuery
{
    private readonly Dictionary<string, List<Dictionary<string, object?>>> _data = new(StringComparer.OrdinalIgnoreCase);

    public FakeWmiQuery Add(string className, params Dictionary<string, object?>[] rows)
    {
        if (!_data.TryGetValue(className, out var list))
            _data[className] = list = new List<Dictionary<string, object?>>();
        list.AddRange(rows);
        return this;
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(
        string className, string? whereClause = null, string wmiNamespace = "root\\cimv2")
    {
        if (!_data.TryGetValue(className, out var rows))
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        return rows
            .Where(r => Matches(r, whereClause))
            .Cast<IReadOnlyDictionary<string, object?>>()
            .ToList();
    }

    /// <summary>
    /// Danh gia mot menh de WHERE don gian.
    ///
    /// Vi sao adapter gia lap can dieu nay: truoc day no BO QUA hoan toan
    /// whereClause va tra ve moi dong. Nghia la cac bo loc quan trong - dac
    /// biet <c>ApplicationID='...'</c> phan biet SKU Windows voi SKU Office -
    /// CHUA TUNG duoc kiem thu. Do dung la noi phat sinh loi ket luan sai ve
    /// ban quyen tim ra ngay 18/08/2026.
    ///
    /// Chi ho tro dung nhung dang menh de ma cac collector thuc su dung:
    /// <c>Field IS NOT NULL</c> va <c>Field='giá trị'</c>, noi voi nhau bang AND.
    /// Gap dang khac thi tra ve true de khong am tham loai bo du lieu.
    /// </summary>
    private static bool Matches(Dictionary<string, object?> row, string? whereClause)
    {
        if (string.IsNullOrWhiteSpace(whereClause)) return true;

        foreach (var raw in whereClause.Split(" AND ", StringSplitOptions.TrimEntries))
        {
            if (raw.EndsWith(" IS NOT NULL", StringComparison.OrdinalIgnoreCase))
            {
                var field = raw[..^" IS NOT NULL".Length].Trim();
                if (!row.TryGetValue(field, out var v) || v is null) return false;
                if (v is string str && str.Length == 0) return false;
                continue;
            }

            var eq = raw.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0) continue;   // dang khong ho tro -> khong loc

            var name = raw[..eq].Trim();
            var expected = raw[(eq + 1)..].Trim().Trim('\'');
            if (!row.TryGetValue(name, out var actual)) return false;
            if (!string.Equals(actual?.ToString(), expected, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }
}

public sealed class FakeRegistryReader : IRegistryReader
{
    private readonly Dictionary<string, List<string>> _subKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    public FakeRegistryReader AddSubKeys(RegistryRoot root, string path, params string[] names)
    {
        _subKeys[Key(root, path)] = names.ToList();
        return this;
    }

    public FakeRegistryReader AddValue(RegistryRoot root, string path, string name, object? value)
    {
        _values[$"{Key(root, path)}::{name}"] = value;
        return this;
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string path)
        => _subKeys.TryGetValue(Key(root, path), out var v) ? v : Array.Empty<string>();

    public object? GetValue(RegistryRoot root, string path, string valueName)
        => _values.TryGetValue($"{Key(root, path)}::{valueName}", out var v) ? v : null;

    private static string Key(RegistryRoot root, string path) => $"{root}|{path}";
}

public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Dictionary<string, ProcessResult> _responses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Khop theo chuoi con xuat hien trong "fileName arguments".</summary>
    public FakeProcessRunner When(string containsInCommand, ProcessResult result)
    {
        _responses[containsInCommand] = result;
        return this;
    }

    public ProcessResult Run(string fileName, string arguments, int timeoutSeconds = 60,
        CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        var command = $"{fileName} {arguments}";
        foreach (var (key, value) in _responses)
            if (command.Contains(key, StringComparison.OrdinalIgnoreCase)) return value;
        return ProcessResult.Failed("(fake) khong co phan hoi duoc cau hinh");
    }
}

public sealed class FakeFileProbe : IFileProbe
{
    private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _dirs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _contents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _env = new(StringComparer.OrdinalIgnoreCase);

    public FakeFileProbe WithEnv(string token, string value) { _env[token] = value; return this; }
    public FakeFileProbe AddFile(string path, string? content = null)
    {
        _files.Add(Norm(path));
        if (content is not null) _contents[Norm(path)] = content;
        return this;
    }
    public FakeFileProbe AddDirectory(string path, params string[] children)
    {
        _dirs[Norm(path)] = children.ToList();
        return this;
    }

    /// <summary>
    /// Chuan hoa dau phan cach de GIA LAP DUNG hanh vi cua Windows: Windows coi
    /// "\\" va "/" tuong duong va khong phan biet hoa/thuong. Neu khong chuan
    /// hoa, test se that bai gia tren host Linux chi vi Path.Combine sinh ra
    /// "/" - mot khac biet cua MOI TRUONG TEST, khong phai loi cua san pham.
    /// </summary>
    private static string Norm(string path) => path.Replace('/', '\\').TrimEnd('\\');

    public bool FileExists(string path) => _files.Contains(Norm(path));
    public bool DirectoryExists(string path) => _dirs.ContainsKey(Norm(path));
    public IReadOnlyList<string> GetDirectories(string path)
        => _dirs.TryGetValue(Norm(path), out var v) ? v : Array.Empty<string>();
    public string? ReadAllTextOrNull(string path)
        => _contents.TryGetValue(Norm(path), out var v) ? v : null;

    public string ExpandEnvironment(string path)
    {
        var result = path;
        foreach (var (token, value) in _env)
            result = result.Replace($"%{token}%", value, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
