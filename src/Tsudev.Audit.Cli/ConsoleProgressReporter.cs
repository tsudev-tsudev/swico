using System.Diagnostics;
using System.Globalization;
using System.Text;
using Tsudev.Audit.Core.Progress;

namespace Tsudev.Audit.Cli;

/// <summary>
/// Ve tien trinh quet ra terminal NGAY KHI no dien ra.
///
/// Truoc day mot lan quet in dung ba dong "[1] Dang kiem tra...", "[2] Dang thu
/// thap...", "[3] Dang cap nhat..." roi im lang hang phut - nguoi dung khong
/// phan biet duoc "dang chay" voi "treo". Lop nay in tung buoc mot, kem con
/// quay bao may van song va thoi gian that cua tung buoc.
///
/// BA RANG BUOC dinh hinh toan bo lop nay:
///
///  1. PHAI xoa bo dem sau moi lan ghi. Khi dau ra bi chuyen huong, .NET dem
///     stdout lai va chi xa khi day bo dem hoac khi thoat - dung luc do thi
///     "thoi gian thuc" khong con nghia gi nua.
///
///  2. KHONG duoc gia dinh terminal doc duoc UTF-8. ConfigureConsole() co chu
///     dich KHONG doi ma trang neu chua can (doi ma trang lam mat mau lich su
///     cuon - nguoi dung da bao cao). Nen o day tu do: UTF-8 thi dung con quay
///     braille va dau tick; khong thi lui ve ASCII.
///
///  3. KHONG duoc de terminal lai o trang thai hong. An con tro ma khong hien
///     lai, hoac dat mau ma khong tra ve, thi phien lam viec cua nguoi dung
///     hong sau khi cong cu da thoat - ke ca khi bi Ctrl+C giua chung.
/// </summary>
internal sealed class ConsoleProgressReporter : IProgressSink, IDisposable
{
    private const int SpinnerPeriodMs = 120;

    private static readonly string[] UnicodeFrames = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    private static readonly string[] AsciiFrames = { "|", "/", "-", "\\" };

    private readonly object _gate = new();
    private readonly Stopwatch _clock = new();
    private readonly bool _interactive;
    private readonly string[] _frames;
    private readonly string _tickMark;
    private readonly string _crossMark;

    private Timer? _spinner;
    private string? _label;
    private int _frame;
    private int _drawnWidth;
    private bool _cursorHidden;

    public ConsoleProgressReporter()
    {
        // Chuyen huong dau ra (ghi ra file log, dua qua ong dan) thi KHONG ve
        // con quay: ky tu \r va ma mau se nam lai trong file, lam ban noi dung.
        _interactive = !Console.IsOutputRedirected;

        var utf8 = SupportsUnicode();
        _frames = utf8 ? UnicodeFrames : AsciiFrames;
        _tickMark = utf8 ? "✓" : "+";
        _crossMark = utf8 ? "✗" : "x";
    }

    private static bool SupportsUnicode()
    {
        try { return Console.OutputEncoding.CodePage == Encoding.UTF8.CodePage; }
        catch (IOException) { return false; }
    }

    public void BeginStep(string label)
    {
        lock (_gate)
        {
            _label = label;
            _frame = 0;
            _clock.Restart();

            if (!_interactive) return;

            HideCursor();
            Draw();
            _spinner = new Timer(_ => Tick(), null, SpinnerPeriodMs, SpinnerPeriodMs);
        }
    }

    public void EndStep() => Finish(_tickMark, null, ConsoleColor.Green);

    public void FailStep(string reason) => Finish(_crossMark, reason, ConsoleColor.Red);

    /// <summary>
    /// Ghi mot dong thong tin phu trong luc mot buoc dang chay.
    ///
    /// Dong nay o LAI tren man hinh (khong bi con quay ghi de) vi no la thong
    /// tin nguoi dung can trong suot thoi gian cho - vi du "sfc mat 5-15 phut".
    /// </summary>
    public void Note(string message)
    {
        lock (_gate)
        {
            if (!_interactive)
            {
                Console.WriteLine($"      {message}");
                Console.Out.Flush();
                return;
            }

            EraseLine();
            WriteColored($"      {message}", ConsoleColor.DarkGray);
            Console.WriteLine();
            Draw();                 // ve lai con quay o dong moi
            Console.Out.Flush();
        }
    }

    private void Finish(string mark, string? reason, ConsoleColor color)
    {
        lock (_gate)
        {
            if (_label is null) return;      // EndStep khong co BeginStep - bo qua

            _spinner?.Dispose();
            _spinner = null;
            _clock.Stop();

            var elapsed = string.Create(CultureInfo.InvariantCulture,
                $"{_clock.Elapsed.TotalSeconds:0.0} s");

            if (_interactive) EraseLine();

            WriteColored($"    {mark} ", color);
            Console.Write(_label);
            WriteColored($"   {elapsed}", ConsoleColor.DarkGray);
            if (reason is not null) WriteColored($"   {reason}", color);
            Console.WriteLine();
            Console.Out.Flush();

            _label = null;
            ShowCursor();
        }
    }

    private void Tick()
    {
        lock (_gate)
        {
            if (_label is null) return;
            _frame = (_frame + 1) % _frames.Length;
            Draw();
        }
    }

    /// <summary>Ve dong con quay hien tai. Nguoi goi PHAI dang giu _gate.</summary>
    private void Draw()
    {
        if (!_interactive || _label is null) return;

        var elapsed = _clock.Elapsed.TotalSeconds >= 1.0
            ? string.Create(CultureInfo.InvariantCulture, $"   {_clock.Elapsed.TotalSeconds:0.0} s")
            : "";

        var line = Truncate($"    {_frames[_frame]} {_label}{elapsed}");

        Console.Write('\r');
        Console.Write(line);

        // Xoa phan thua cua lan ve truoc (nhan cu dai hon nhan moi).
        if (_drawnWidth > line.Length) Console.Write(new string(' ', _drawnWidth - line.Length));
        _drawnWidth = line.Length;

        Console.Out.Flush();
    }

    /// <summary>
    /// Cat cho vua BE NGANG cua so.
    ///
    /// Neu de dong dai hon be ngang, terminal tu xuong dong; luc do ky tu \r
    /// chi dua con tro ve dau dong CUOI, va dong dau tien nam lai vinh vien
    /// tren man hinh. Ket qua la moi nhip con quay de lai mot dong rac.
    /// </summary>
    private static string Truncate(string line)
    {
        int width;
        try { width = Console.WindowWidth; }
        catch (IOException) { return line; }          // khong hoi duoc -> cu de nguyen

        if (width <= 1 || line.Length < width) return line;
        return line[..(width - 1)];
    }

    private void EraseLine()
    {
        if (!_interactive || _drawnWidth == 0) return;
        Console.Write('\r');
        Console.Write(new string(' ', _drawnWidth));
        Console.Write('\r');
        _drawnWidth = 0;
    }

    private void WriteColored(string text, ConsoleColor color)
    {
        if (!_interactive) { Console.Write(text); return; }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }
        finally
        {
            // try/finally de mot ngoai le khi ghi KHONG de console mac ket o mau.
            Console.ForegroundColor = prev;
        }
    }

    private void HideCursor()
    {
        if (_cursorHidden || !_interactive) return;
        try { Console.CursorVisible = false; _cursorHidden = true; }
        catch (IOException) { /* terminal khong cho - khong phai loi */ }
        catch (PlatformNotSupportedException) { }
    }

    private void ShowCursor()
    {
        if (!_cursorHidden) return;
        try { Console.CursorVisible = true; }
        catch (IOException) { }
        catch (PlatformNotSupportedException) { }
        _cursorHidden = false;
    }

    /// <summary>
    /// Don sach man hinh khi bi ngat giua chung: xoa dong con quay do dang va
    /// tra con tro ve. Goi duoc nhieu lan.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _spinner?.Dispose();
            _spinner = null;
            EraseLine();
            _label = null;
            ShowCursor();
            Console.Out.Flush();
        }
    }
}
