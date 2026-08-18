using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Tsudev.Audit.Core.Updates;

namespace Tsudev.Audit.Windows;

/// <summary>
/// Hop thoai bao co ban moi, CHI co mot nut "Cập nhật".
///
/// Vi sao dung TaskDialog cua Windows thay vi WinForms: WinForms se keo theo
/// ca mot bo thu vien giao dien vao ban self-contained (them hang chuc MB cho
/// mot hop thoai duy nhat). TaskDialog nam san trong comctl32.dll cua moi ban
/// Windows tu Vista, cho phep DAT TEN NUT tuy y - dieu ma MessageBox khong lam
/// duoc - va co giao dien dung chuan he dieu hanh.
/// </summary>
[SupportedOSPlatform("windows")]
public static class UpdatePrompt
{
    private const int TD_WARNING_ICON = -1;
    private const int ButtonUpdateId = 100;

    [Flags]
    private enum TdFlags
    {
        AllowDialogCancellation = 0x0008,
        PositionRelativeToWindow = 0x1000,
        SizeToContent = 0x1000000
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    private struct TaskDialogButton
    {
        public int ButtonId;
        [MarshalAs(UnmanagedType.LPWStr)] public string ButtonText;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    private struct TaskDialogConfig
    {
        public uint Size;
        public IntPtr Parent, Instance;
        public uint Flags, CommonButtons;
        [MarshalAs(UnmanagedType.LPWStr)] public string WindowTitle;
        public IntPtr MainIcon;
        [MarshalAs(UnmanagedType.LPWStr)] public string MainInstruction;
        [MarshalAs(UnmanagedType.LPWStr)] public string Content;
        public uint ButtonCount;
        public IntPtr Buttons;
        public int DefaultButton;
        public uint RadioButtonCount;
        public IntPtr RadioButtons;
        public int DefaultRadioButton;
        [MarshalAs(UnmanagedType.LPWStr)] public string VerificationText;
        [MarshalAs(UnmanagedType.LPWStr)] public string ExpandedInformation;
        [MarshalAs(UnmanagedType.LPWStr)] public string ExpandedControlText;
        [MarshalAs(UnmanagedType.LPWStr)] public string CollapsedControlText;
        public IntPtr FooterIcon;
        [MarshalAs(UnmanagedType.LPWStr)] public string Footer;
        public IntPtr Callback;
        public IntPtr CallbackData;
        public uint Width;
    }

    [DllImport("comctl32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int TaskDialogIndirect(
        ref TaskDialogConfig config, out int button, out int radioButton, out bool verificationChecked);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    /// <summary>
    /// Hien hop thoai. Tra ve true khi nguoi dung bam "Cập nhật".
    ///
    /// Hop thoai CO the dong bang phim Esc hoac nut X - dat co
    /// AllowDialogCancellation co chu dich. Mot hop thoai khong the dong duoc
    /// se khoa cung phien lam viec cua nguoi dung neu co gi do truc trac; viec
    /// CHAN quet duoc thuc thi bang logic chuong trinh, khong phai bang cach
    /// giam nguoi dung trong mot cua so.
    /// </summary>
    public static bool AskToUpdate(UpdateCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var latest = result.Latest;

        var title = "tsudev SWICO - Đã có phiên bản mới";
        var heading = $"Phiên bản {latest?.Version} đã sẵn sàng";
        var body =
            $"Bạn đang dùng phiên bản {result.Current}.\n\n" +
            "Công cụ cần được cập nhật trước khi tiếp tục rà quét, để kết quả " +
            "kiểm tra dựa trên bộ luật phát hiện mới nhất.\n\n" +
            "Bấm “Cập nhật” để tải và cài phiên bản mới. File tải về sẽ được " +
            "đối chiếu mã băm SHA-256 trước khi chạy.";
        var footer = latest is null ? "" : $"Chi tiết bản phát hành: {latest.PageUrl}";

        try
        {
            var buttons = new[] { new TaskDialogButton { ButtonId = ButtonUpdateId, ButtonText = "Cập nhật" } };
            var handle = GCHandle.Alloc(buttons, GCHandleType.Pinned);
            try
            {
                var config = new TaskDialogConfig
                {
                    Size = (uint)Marshal.SizeOf<TaskDialogConfig>(),
                    Flags = (uint)(TdFlags.AllowDialogCancellation | TdFlags.PositionRelativeToWindow | TdFlags.SizeToContent),
                    CommonButtons = 0,
                    WindowTitle = title,
                    MainIcon = new IntPtr(TD_WARNING_ICON),
                    MainInstruction = heading,
                    Content = body,
                    ButtonCount = (uint)buttons.Length,
                    Buttons = handle.AddrOfPinnedObject(),
                    DefaultButton = ButtonUpdateId,
                    VerificationText = "",
                    ExpandedInformation = "",
                    ExpandedControlText = "",
                    CollapsedControlText = "",
                    Footer = footer,
                    Width = 0
                };

                var hr = TaskDialogIndirect(ref config, out var pressed, out _, out _);
                if (hr == 0) return pressed == ButtonUpdateId;
            }
            finally { handle.Free(); }
        }
        catch (DllNotFoundException) { /* rat kho xay ra - roi ve MessageBox */ }
        catch (EntryPointNotFoundException) { }

        // Du phong: MessageBox khong doi duoc ten nut nen chi con OK/Cancel.
        const uint MB_OKCANCEL = 0x00000001, MB_ICONWARNING = 0x00000030;
        return MessageBoxW(IntPtr.Zero,
            $"{heading}\n\n{body}\n\nBấm OK để cập nhật.",
            title, MB_OKCANCEL | MB_ICONWARNING) == 1;
    }
}
