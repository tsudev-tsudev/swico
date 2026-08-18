# Kịch bản kiểm chứng trên Windows thật

> **Dành cho bạn chạy, không phải cho tôi.** Môi trường phát triển là Linux nên
> mọi thứ dưới đây chưa từng được thực thi lần nào.
>
> Đây là hạng mục **rủi ro cao nhất** của cả dự án: lớp `WindowsAdapters.cs`
> chạm trực tiếp vào WMI/Registry và **chưa bao giờ chạy trên Windows**.
> Toàn bộ logic nghiệp vụ đã có 64 test bảo vệ, nhưng lớp chạm hệ điều hành thì chưa.

Chép kết quả từng mục vào cột **Kết quả thật** rồi gửi lại. Chỗ nào lệch, gần
như chắc chắn lỗi nằm gọn trong `src/Tsudev.Audit.Windows/WindowsAdapters.cs`.

---

## Chuẩn bị — đọc kỹ mục này trước

> ⚠️ **Repo `github.com/tsudev-tsudev/swico` CHƯA TỒN TẠI.** Đó là URL điền tạm,
> dùng trước cho tài liệu và manifest. Mã nguồn hiện **chỉ nằm trên máy dev
> Linux**, chưa từng được đẩy lên GitHub. `git clone` sẽ báo không tìm thấy.

### Phần lớn kịch bản này KHÔNG cần mã nguồn

Chỉ mục **A1** và **A2** cần build trên Windows. Mọi mục còn lại — gồm cả ba
mục then chốt **B2, C3, D1** — chỉ cần **một file `swico.exe`**.

### Cách 1 (nhanh nhất) — chỉ copy file exe

Trên máy dev, file đã publish sẵn tại `dist/swico-portable.zip` (~29 MB).
Copy sang Windows bằng USB hoặc chia sẻ mạng, giải nén, rồi:

```powershell
cd <thư mục đã giải nén>
.\swico.exe --help
```

Không cần cài .NET Runtime — bản publish là self-contained.

**Đối chiếu sau khi copy** (phòng file hỏng trên đường truyền):

```powershell
Get-FileHash .\swico.exe -Algorithm SHA256
```

So với giá trị trong `dist/SHA256SUMS.txt` trên máy dev.

Bỏ qua A1, A2. Làm được toàn bộ phần còn lại.

### Cách 2 — chuyển cả kho mã kèm lịch sử git

Nếu muốn làm cả A1/A2, hoặc muốn sửa mã trên Windows. Cần **.NET 8 SDK**.

Trên máy dev đã tạo sẵn `dist/swico-repo.bundle` (116 KB) — một file duy nhất
chứa **nguyên vẹn cả 7 commit và toàn bộ lịch sử**. Copy sang Windows rồi:

```powershell
git clone swico-repo.bundle swico
cd swico
.\build.ps1
```

Kỳ vọng: test báo `64 PASS, 0 FAIL`, rồi sinh ra `publish\swico.exe`.

> Dùng `git bundle` thay vì zip thư mục vì nó giữ nguyên lịch sử git. Nếu sau
> này bạn tạo repo GitHub thật, chỉ cần `git remote add` rồi `git push` là toàn
> bộ 7 commit lên đúng như cũ — không mất mốc lùi nào.

### Cách 3 — tạo repo GitHub thật

Sớm muộn cũng phải làm, vì **SignPath Foundation bắt buộc repo công khai**
(xem `docs/SIGNING.md`). Tôi không tạo repo thay bạn được — cần tài khoản của
bạn. Sau khi bạn tạo repo rỗng trên GitHub:

```bash
# chạy trên máy dev Linux
git remote add origin https://github.com/<tài-khoản>/<tên-repo>.git
git branch -M main
git push -u origin main
```

Rồi trên Windows `git clone` như bình thường.

> Nếu đổi tên repo, phải sửa lại URL ở: `README.md`,
> `Directory.Build.props` (`RepositoryUrl`), `packaging/winget/...` (3 file),
> `packaging/innosetup/swico.iss` (`AppUrl`), `EULA.txt` mục 7, `PRIVACY.md`.

---

## A. Những thứ phải đúng trước tiên

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| A1 | `dotnet build Tsudev.SystemAudit.sln -c Release` | Không lỗi. Gói `System.Management` restore được. | |
| A2 | `dotnet run --project tests/unittests -c Release` | `64 PASS, 0 FAIL` | |
| A3 | Nháy đúp `publish\swico.exe` | **Hiện hộp thoại UAC** (do `app.manifest`) | |
| A4 | `.\publish\swico.exe --help` | In danh sách tham số bằng tiếng Việt có dấu, không lỗi font | |
| A5 | Chạy **không** quyền Administrator | Hiện cảnh báo, vẫn chạy, không sập | |

## B. Tên thuộc tính WMI — chỗ dễ sai nhất

> ⚠️ **Các lệnh dưới đây là PowerShell, chạy TRÊN MÁY WINDOWS.** Không phải bash,
> không phải trên máy Linux. Dấu hiệu nhận biết bạn đang ở sai chỗ:
>
> | Dấu nhắc | Đây là gì | `Get-CimInstance` |
> |---|---|---|
> | `PS C:\Users\...>` | PowerShell trên Windows ✅ | chạy được |
> | `~/projects/tsudev-swico$` | bash trên Linux ❌ | *command not found* |
>
> Cách mở đúng: bấm **Start** → gõ `powershell` → chuột phải →
> **Run as administrator**.
>
> Khi chép lệnh, gõ dấu ống `|` **trơn**. Nếu thấy `\|` (có gạch chéo ngược) thì
> đó là cách escape của bash — PowerShell sẽ báo lỗi cú pháp.

| # | Lệnh kiểm tra | Cần xác nhận | Kết quả thật |
|---|---|---|---|
| B1 | `Get-CimInstance -Namespace root\Microsoft\Windows\Defender -ClassName MSFT_MpComputerStatus \| Format-List *` | Có các trường mà `DefenderCollector` đang đọc | |
| B2 | `Get-CimInstance -Namespace root\Microsoft\Windows\Defender -ClassName MSFT_MpThreatDetection \| Format-List *` | **Có trường `ThreatName` không?** Nếu không, phải tra chéo qua `MSFT_MpThreat` | |
| B3 | `Get-CimInstance -Namespace root\Microsoft\Windows\TaskScheduler -ClassName MSFT_ScheduledTask \| Select -First 3 \| Format-List *` | Truy vấn được, tên thuộc tính khớp | |
| B4 | `Get-CimInstance Win32_ComputerSystem, Win32_BIOS, Win32_Processor` | Khớp với `HardwareCollector` | |
| B5 | `Get-CimInstance SoftwareLicensingProduct \| Where PartialProductKey \| Format-List *` | Khớp với `WindowsLicenseCollector` | |

> Nếu B2 không có `ThreatName`: đây là sai lệch đã được dự đoán trước. Báo lại
> tên trường thật, tôi sửa adapter.

## C. Kết quả đầu ra

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| C1 | `.\publish\swico.exe --silent` | Tạo đủ 3 cấp thư mục theo quy ước | |
| C2 | Mở file `.html` sinh ra | Hiển thị đúng, tiếng Việt có dấu, không vỡ trang | |
| C3 | **Mở file `.xlsx` bằng Excel thật** | **Excel KHÔNG báo "file hỏng, cần sửa"** | ❌ **ĐÃ LỖI (18/08) — đã sửa, cần thử lại** |
| C4 | Kiểm tra `.xlsx`: dòng tiêu đề | Đóng băng khi cuộn, in đậm, nền xanh nhạt | |
| C5 | Kiểm tra `.xlsx`: cột mã số | `"007"` giữ nguyên số 0 ở đầu, **không** thành `7` | |
| C6 | Mở file `.json` | Đọc được, đúng `SchemaVersion` | |
| C7 | `.\publish\swico.exe --scope license` rồi `--scope hardware` | Mỗi lần chỉ sinh báo cáo tương ứng | |
| C8 | Mở `tsudev-tong-hop.html` | Bảng gom đủ các báo cáo, bấm "Mở →" nhảy đúng file | |
| C9 | Copy thư mục kết quả từ máy khác vào cấp 2, chạy lại | Trang tổng hợp gom cả máy mới | |
| C10 | `echo $LASTEXITCODE` sau khi chạy | Đúng quy ước 0/1/2/3 | |

> **C3 là mục quan trọng nhất trong bảng này.** Bộ ghi XLSX được viết tay theo
> chuẩn OOXML và ở phiên S001 đã có **một lỗi đặt sai vị trí phần tử `<pane>`**
> — file vẫn là XML hợp lệ nhưng Excel sẽ từ chối. Lỗi đó đã sửa, nhưng **chưa
> ai mở bằng Excel thật lần nào**. Môi trường phát triển không có Excel,
> LibreOffice, cũng không cài được `openpyxl`.

## D. Đối chiếu với bản PowerShell cũ

Đây là **bài kiểm tra hồi quy thật sự duy nhất** hiện có.

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| D1 | Chạy bộ PowerShell cũ và `swico.exe` trên **cùng một máy** | Kết luận bản quyền giống nhau | ❌ **ĐÃ LỖI (18/08): swico báo hợp lệ, PowerShell báo không — đã sửa, cần thử lại** |
| D2 | So danh sách phần mềm phát hiện được | Không thiếu mục nào so với bản cũ | |
| D3 | So điểm rủi ro và số phát hiện | Cùng thang, cùng kết luận | |

## E. Ma trận cấu hình

Chạy tối thiểu trên 3 máy khác nhau. Càng khác nhau càng tốt.

| Máy | Windows | Office | Kích hoạt | Quyền | Kết quả |
|---|---|---|---|---|---|
| 1 | Win 11 | Có | Đã kích hoạt | Admin | |
| 2 | Win 10 | Không | Đã kích hoạt | Admin | |
| 3 | Win 10/11 | Có | **Chưa** kích hoạt | Admin | |
| 4 | bất kỳ | — | — | **Không** admin | |

## F. Kiểm tra installer (sau khi có file setup)

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| F1 | Chạy `swico-setup-3.0.0.exe` | Hiện EULA, cài được, hiện tiếng Việt | |
| F2 | Mở Command Prompt mới, gõ `swico --help` | Chạy được (nếu đã chọn thêm vào PATH) | |
| F3 | Bảng điều khiển → Programs | Có mục "tsudev SWICO", gỡ được | |
| F4 | Sau khi gỡ: kiểm tra biến PATH | Đường dẫn đã bị rút ra, không để lại rác | |
| F5 | Cài đè phiên bản cũ | Nâng cấp tại chỗ, **không** tạo mục thứ hai | |
| F6 | `swico-setup-3.0.0.exe /VERYSILENT` | Cài im lặng, không hiện cửa sổ nào | |

## G. Phần mềm diệt virus

Rủi ro đặc thù: công cụ đòi quyền Administrator, đọc registry bản quyền, quét
dấu hiệu crack, đọc trạng thái Defender — mô tả gần trùng khớp phần mềm độc hại.

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| G1 | Tải exe về máy có Defender bật đầy đủ | Không bị chặn/xoá | |
| G2 | Nộp exe lên VirusTotal | Ghi lại số hãng báo động | |
| G3 | Nếu có báo động: gửi mẫu qua cổng báo nhầm của Microsoft và các hãng liên quan | | |

---

## Sau khi chạy xong

Gửi lại bảng này kèm mọi thông báo lỗi nguyên văn. Với mỗi mục lệch, ghi rõ:

1. Lệnh đã chạy
2. Kết quả mong đợi và kết quả thật
3. Thông báo lỗi đầy đủ (nếu có)

Kết quả sẽ được ghi vào `docs/journal/` của phiên tương ứng và chuyển thành
test hồi quy để lần sau không phải kiểm thủ công.
