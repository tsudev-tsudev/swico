using System.Globalization;
using Tsudev.Audit.Core.Progress;

namespace Tsudev.Audit.Core.Testing;

/// <summary>
/// Sink ghi lai moi su kien tien trinh duoi dang chuoi, de bo test khang dinh
/// duoc THU TU CAC BUOC ma khong can may Windows va khong can doc man hinh.
///
/// Vi sao dang chuoi chu khong phai doi tuong: mot danh sach chuoi so sanh
/// duoc bang mot dong lenh, va khi test do thi thong bao loi doc ra ngay duoc
/// van de - khong phai mo debugger.
/// </summary>
public sealed class RecordingProgressSink : IProgressSink
{
    private readonly List<string> _events = new();

    /// <summary>Chuoi su kien theo dung thu tu phat sinh.</summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>Chi rieng nhan cua cac buoc da BAT DAU, theo thu tu.</summary>
    public IReadOnlyList<string> StepLabels => _events
        .Where(e => e.StartsWith("BEGIN ", StringComparison.Ordinal))
        .Select(e => e["BEGIN ".Length..])
        .ToList();

    /// <summary>So buoc da ket thuc tron ven.</summary>
    public int Completed => _events.Count(e => e == "END");

    public void BeginStep(string label) => _events.Add($"BEGIN {label}");
    public void EndStep() => _events.Add("END");
    public void FailStep(string reason) => _events.Add($"FAIL {reason}");
    public void Note(string message) => _events.Add($"NOTE {message}");

    public override string ToString()
        => string.Join(Environment.NewLine, _events.Select((e, i)
            => string.Create(CultureInfo.InvariantCulture, $"{i,3}: {e}")));
}
