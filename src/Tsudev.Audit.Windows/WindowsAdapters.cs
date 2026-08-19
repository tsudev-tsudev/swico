using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Win32;
using Tsudev.Audit.Core.Abstractions;
using Tsudev.Audit.Core.Progress;

namespace Tsudev.Audit.Windows;

/// <summary>
/// Adapter WMI thuc te. Day la lop DUY NHAT trong toan bo giai phap phu thuoc
/// vao goi System.Management - moi logic nghiep vu deu nam trong Tsudev.Audit.Core
/// va khong biet gi ve WMI.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WmiQuery : IWmiQuery
{
    private readonly Action<string> _warn;

    public WmiQuery(Action<string>? warn = null) => _warn = warn ?? (_ => { });

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Query(
        string className, string? whereClause = null, string wmiNamespace = "root\\cimv2")
    {
        var result = new List<IReadOnlyDictionary<string, object?>>();
        var query = string.IsNullOrWhiteSpace(whereClause)
            ? $"SELECT * FROM {className}"
            : $"SELECT * FROM {className} WHERE {whereClause}";

        try
        {
            var scope = new ManagementScope(wmiNamespace);
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
            using var collection = searcher.Get();

            foreach (ManagementBaseObject obj in collection)
            {
                using (obj)
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in obj.Properties)
                    {
                        try { row[prop.Name] = prop.Value; }
                        catch { /* thuoc tinh loi -> bo qua rieng thuoc tinh do */ }
                    }
                    result.Add(row);
                }
            }
        }
        catch (ManagementException ex)
        {
            _warn($"Không truy vấn được WMI '{className}' ({wmiNamespace}): {ex.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            _warn($"Không đủ quyền truy vấn WMI '{className}' - hãy chạy với quyền Administrator.");
        }
        catch (Exception ex)
        {
            _warn($"Lỗi khi truy vấn WMI '{className}': {ex.Message}");
        }

        return result;
    }
}

[SupportedOSPlatform("windows")]
public sealed class WindowsRegistryReader : IRegistryReader
{
    public IReadOnlyList<string> GetSubKeyNames(RegistryRoot root, string path)
    {
        try
        {
            using var key = Root(root).OpenSubKey(path);
            return key?.GetSubKeyNames() ?? (IReadOnlyList<string>)Array.Empty<string>();
        }
        catch { return Array.Empty<string>(); }
    }

    public object? GetValue(RegistryRoot root, string path, string valueName)
    {
        try
        {
            using var key = Root(root).OpenSubKey(path);
            return key?.GetValue(valueName);
        }
        catch { return null; }
    }

    private static RegistryKey Root(RegistryRoot root) => root switch
    {
        RegistryRoot.CurrentUser => Registry.CurrentUser,
        _ => Registry.LocalMachine
    };
}

public sealed class ProcessRunner : IProcessRunner
{
    public ProcessResult Run(string fileName, string arguments, int timeoutSeconds = 60,
        CancellationToken cancellation = default)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return ProcessResult.Failed($"Không khởi chạy được '{fileName}'.");

            // Doc async de tranh deadlock khi buffer output day.
            //
            // CO CHU DICH truyen CancellationToken.None: khi nguoi dung huy, ta
            // GIET tien trinh con roi nem OperationCanceledException - khong he
            // doc toi ket qua hai luong nay nua. Truyen token vao day chi tao ra
            // hai Task hong khong ai quan sat, ma khong dung som duoc mot phan
            // nghin giay nao.
            var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);

            // Cho theo TUNG LAT NGAN thay vi mot lan cho dai, de con ngo toi
            // tin hieu huy. sfc chay toi 15 phut; neu chi WaitForExit(1800s)
            // thi Ctrl+C khong the dung no lai, va nguoi dung thay terminal
            // "khong phan hoi" dung mot phan tu.
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (!process.WaitForExit(200))
            {
                if (cancellation.IsCancellationRequested)
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* da thoat roi */ }
                    throw new OperationCanceledException(cancellation);
                }

                if (DateTime.UtcNow >= deadline)
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* da thoat roi */ }
                    return ProcessResult.Failed($"'{fileName}' chạy quá {timeoutSeconds} giây - đã hủy.");
                }
            }

            return new ProcessResult(process.ExitCode, stdout.Result, stderr.Result);
        }
        catch (OperationCanceledException)
        {
            // Huy KHONG phai loi chay tien trinh - de no noi len tren, neu
            // khong thi Ctrl+C se bi bien thanh mot dong "Loi khi chay" trong
            // bao cao va lan quet van chay tiep nhu chua co gi xay ra.
            throw;
        }
        catch (Exception ex)
        {
            return ProcessResult.Failed($"Lỗi khi chạy '{fileName}': {ex.Message}");
        }
    }
}

public sealed class FileProbe : IFileProbe
{
    public bool FileExists(string path)
    {
        try { return File.Exists(path); } catch { return false; }
    }

    public bool DirectoryExists(string path)
    {
        try { return Directory.Exists(path); } catch { return false; }
    }

    public IReadOnlyList<string> GetDirectories(string path)
    {
        try { return Directory.GetDirectories(path); }
        catch { return Array.Empty<string>(); }  // thieu quyen -> bo qua, khong lam hong ca lan quet
    }

    public string? ReadAllTextOrNull(string path)
    {
        try { return File.ReadAllText(path); } catch { return null; }
    }

    public string ExpandEnvironment(string path) => Environment.ExpandEnvironmentVariables(path);
}

/// <summary>Tien ich mo truong Windows.</summary>
public static class WindowsEnvironment
{
    [SupportedOSPlatform("windows")]
    public static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    /// <summary>
    /// Tao SystemContext day du cho mot lan quet tren may Windows that.
    ///
    /// <paramref name="progress"/> va <paramref name="cancellation"/> deu tuy
    /// chon: bo trong thi lan quet chay im lang va khong huy duoc - dung hanh
    /// vi cu, nen moi cho goi san khong phai sua.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static SystemContext CreateContext(
        DateTimeOffset? scanTime = null,
        IProgressSink? progress = null,
        CancellationToken cancellation = default)
    {
        var warnings = new List<string>();
        var ctx = new SystemContext
        {
            Wmi = new WmiQuery(w => warnings.Add(w)),
            Registry = new WindowsRegistryReader(),
            Process = new ProcessRunner(),
            Files = new FileProbe(),
            ComputerName = Environment.MachineName,
            ScanTime = scanTime ?? DateTimeOffset.Now,
            IsElevated = IsElevated(),
            Progress = progress ?? NullProgressSink.Instance,
            Cancellation = cancellation
        };
        // Chuyen canh bao tich luy tu adapter vao context
        foreach (var w in warnings) ctx.Warn(w);
        return ctx;
    }
}
