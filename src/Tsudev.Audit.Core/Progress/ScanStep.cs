using Tsudev.Audit.Core.Abstractions;

namespace Tsudev.Audit.Core.Progress;

/// <summary>
/// Boc mot buoc thu thap: bao bat dau, chay, bao xong (hoac bao hong).
///
/// Day cung la NOI DUY NHAT kiem tra tin hieu huy. Dat kiem tra o ranh gioi
/// giua cac buoc - khong rai rac trong long collector - vi hai le:
///  (1) huy giua chung mot buoc chi de lai du lieu do dang, khong dung duoc;
///  (2) mot cho kiem tra thi doc ma con biet no chay dung; rai rac muoi cho
///      thi khong ai dam chac cho nao con thieu.
///
/// Rieng nhung tien trinh ngoai chay lau (DISM, sfc) nhan token truc tiep va
/// bi giet ngay khi nguoi dung bam Ctrl+C - xem <see cref="IProcessRunner"/>.
/// </summary>
public static class ScanStep
{
    /// <summary>Chay mot buoc co ket qua tra ve.</summary>
    public static T Run<T>(SystemContext ctx, string label, Func<T> work)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(work);

        ctx.Cancellation.ThrowIfCancellationRequested();
        ctx.Progress.BeginStep(label);
        try
        {
            var value = work();
            ctx.Progress.EndStep();
            return value;
        }
        catch (OperationCanceledException)
        {
            // Huy KHONG phai loi cua buoc - dung bao no thanh that bai.
            ctx.Progress.FailStep("đã huỷ");
            throw;
        }
        catch (Exception ex)
        {
            ctx.Progress.FailStep(ex.Message);
            throw;
        }
    }

    /// <summary>Chay mot buoc khong tra ve gi.</summary>
    public static void Run(SystemContext ctx, string label, Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        Run(ctx, label, () => { work(); return true; });
    }
}
