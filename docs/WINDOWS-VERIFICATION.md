# Kịch bản kiểm chứng trên Windows thật

> **Dành cho bạn chạy, không phải cho tôi.** Môi trường phát triển là Linux nên
> mọi thứ dưới đây chưa từng được thực thi lần nào.
>
> Đây là hạng mục **rủi ro cao nhất** của cả dự án: lớp `WindowsAdapters.cs`
> chạm trực tiếp vào WMI/Registry và **chưa bao giờ chạy trên Windows**.
> Toàn bộ logic nghiệp vụ đã có 54 test bảo vệ, nhưng lớp chạm hệ điều hành thì chưa.

Chép kết quả từng mục vào cột **Kết quả thật** rồi gửi lại. Chỗ nào lệch, gần
như chắc chắn lỗi nằm gọn trong `src/Tsudev.Audit.Windows/WindowsAdapters.cs`.

---

## Chuẩn bị

Cần: Windows 10 hoặc 11, quyền Administrator, .NET 8 SDK.

```powershell
git clone https://github.com/tsuowlit/swico
cd swico
.\build.ps1
```

Kỳ vọng: test báo `54 PASS, 0 FAIL`, rồi sinh ra `publish\swico.exe`.

---

## A. Những thứ phải đúng trước tiên

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| A1 | `dotnet build Tsudev.SystemAudit.sln -c Release` | Không lỗi. Gói `System.Management` restore được. | |
| A2 | `dotnet run --project tests/unittests -c Release` | `54 PASS, 0 FAIL` | |
| A3 | Nháy đúp `publish\swico.exe` | **Hiện hộp thoại UAC** (do `app.manifest`) | |
| A4 | `.\publish\swico.exe --help` | In danh sách tham số bằng tiếng Việt có dấu, không lỗi font | |
| A5 | Chạy **không** quyền Administrator | Hiện cảnh báo, vẫn chạy, không sập | |

## B. Tên thuộc tính WMI — chỗ dễ sai nhất

Đây là những lớp WMI mà README gốc đã tự cảnh báo là chưa xác nhận. Chạy từng
lệnh dưới đây trong PowerShell **có quyền Administrator** rồi đối chiếu tên
thuộc tính với mã nguồn.

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
| C3 | **Mở file `.xlsx` bằng Excel thật** | **Excel KHÔNG báo "file hỏng, cần sửa"** | |
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
| D1 | Chạy bộ PowerShell cũ và `swico.exe` trên **cùng một máy** | Kết luận bản quyền giống nhau | |
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
| F3 | Bảng điều khiển → Programs | Có mục "tsuowlit SWICO", gỡ được | |
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
