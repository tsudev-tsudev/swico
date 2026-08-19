namespace Tsudev.Audit.Core.Progress;

/// <summary>
/// Cong bao tien trinh quet.
///
/// Vi sao la mot interface trong Core chu khong phai goi thang Console:
///  (1) toan bo thu tu cac buoc quet la LOGIC NGHIEP VU - phai kiem thu duoc
///      tren Linux, giong nhu ExitCodes va CliOptions (xem docs/STATE.md 4.3);
///  (2) lop Cli tu quyet dinh ve gi ra man hinh (spinner, mau, xuong dong),
///      con Core chi noi "dang lam gi" - hai viec khac han nhau.
///
/// Cac collector KHONG duoc nem exception khi bao tien trinh: mot cai sink
/// hong khong duoc lam hong ca lan quet.
/// </summary>
public interface IProgressSink
{
    /// <summary>Bat dau mot buoc. Luon di kem dung mot lan EndStep hoac FailStep.</summary>
    void BeginStep(string label);

    /// <summary>Buoc hien tai da xong.</summary>
    void EndStep();

    /// <summary>Buoc hien tai that bai (kem ly do ngan gon).</summary>
    void FailStep(string reason);

    /// <summary>
    /// Thong tin phu phat ra TRONG LUC mot buoc dang chay - dung cho nhung viec
    /// chay lau (DISM, sfc) de nguoi dung biet may khong bi treo.
    /// </summary>
    void Note(string message);
}

/// <summary>
/// Sink khong lam gi ca - mac dinh cua <see cref="Abstractions.SystemContext"/>.
///
/// Nho co no, moi doan ma cu (va toan bo bo test hien co) chay nguyen ven ma
/// khong phai truyen them tham so nao.
/// </summary>
public sealed class NullProgressSink : IProgressSink
{
    public static readonly NullProgressSink Instance = new();

    private NullProgressSink() { }

    public void BeginStep(string label) { }
    public void EndStep() { }
    public void FailStep(string reason) { }
    public void Note(string message) { }
}
