# tsuowlit SWICO

Kiểm tra tình trạng bản quyền Windows/Office và thu thập cấu hình phần cứng —
**một file `.exe` duy nhất**, không cần cài .NET Runtime.

[![CI](https://github.com/tsudev-tsudev/swico/actions/workflows/ci.yml/badge.svg)](https://github.com/tsudev-tsudev/swico/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

> **Báo cáo do công cụ tạo ra là dữ liệu kỹ thuật để tham khảo, không phải kết
> luận pháp lý.** Xem [`EULA.txt`](EULA.txt) mục 3 trước khi dùng kết quả vào
> bất kỳ quyết định nào có hệ quả với con người.

## Cài đặt

> ⚠️ **Chưa phát hành.** Repo GitHub và các lệnh dưới đây **chưa hoạt động** —
> mọi URL `github.com/tsudev-tsudev/swico` trong tài liệu này là chỗ điền tạm. Mã
> nguồn hiện chỉ tồn tại cục bộ. Xem `docs/STATE.md` để biết trạng thái thật.

```powershell
winget install tsuowlit.SWICO
```

Hoặc tải từ [Releases](https://github.com/tsudev-tsudev/swico/releases): file setup,
hoặc bản portable `.zip` giải nén chạy thẳng.

## Cách dùng

```
swico.exe                          Quét đầy đủ, mở báo cáo khi xong
swico.exe --scope license          Chỉ kiểm tra bản quyền
swico.exe --scope hardware         Chỉ thu thập phần cứng
swico.exe --silent --sfc           Quét sâu, không mở trình duyệt (GPO/RMM)
swico.exe -o D:\BaoCao --silent    Lưu vào thư mục chỉ định
swico.exe --help                   Xem toàn bộ tham số
```

Công cụ yêu cầu quyền Administrator để đọc trạng thái bản quyền, Defender và
chạy DISM/SFC. Windows sẽ tự hiện hộp thoại UAC.

### Mã thoát (cho tích hợp RMM/giám sát)

| Mã | Ý nghĩa |
|----|---------|
| 0 | Thành công hoàn toàn |
| 1 | Thành công nhưng có mục thiếu dữ liệu (báo cáo chính vẫn đầy đủ) |
| 2 | Lỗi nghiêm trọng, không tạo được báo cáo |
| 3 | Tham số dòng lệnh không hợp lệ |

## Kết quả đầu ra

Bốn định dạng cùng lúc: **HTML** để đọc, **JSON** để tích hợp, **XLSX** và
**CSV** để xử lý tiếp.

```
<thư mục chứa exe>/
  tsudev-bao-cao-ra-quet-<Máy>-<Ngày>/          ← cấp 2
    tsudev-tong-hop.html                         ← trang tổng hợp mọi máy
    tsudev-ket-qua-ra-quet-<Máy>-<Ngày>/         ← cấp 3
      Windows_License_Audit_*.html / .json / .xlsx / .csv
      Windows_Hardware_Inventory_*.html / .json / .xlsx / .csv
```

Quét nhiều máy: copy các thư mục **cấp 3** từ máy khác vào cấp 2 →
`tsudev-tong-hop.html` tự đọc đệ quy (**không giới hạn độ sâu**) và gom lại.

## Quyền riêng tư

Công cụ **không kết nối Internet** và **không gửi dữ liệu đi đâu**. Mọi thứ chỉ
ghi ra đĩa của chính máy đó. Chi tiết: [`PRIVACY.md`](PRIVACY.md).

## Kiến trúc

```
src/
  Tsudev.Audit.Core/        net8.0          ← KHÔNG phụ thuộc Windows
    Models/                                    JSON schema v3.0 (hợp đồng dùng chung)
    Abstractions/                              Các "cổng" (port): IWmiQuery, IRegistryReader...
    Collectors/                                TOÀN BỘ logic nghiệp vụ
    Reports/                                   Lắp ráp collector thành báo cáo
    Rendering/                                 HTML + XLSX + Dashboard
    Testing/                                   Adapter giả lập cho unit test
  Tsudev.Audit.Windows/     net8.0-windows  ← Adapter mỏng: WMI, Registry, Process
  Tsudev.Audit.Cli/         net8.0-windows  ← 1 file exe
tests/
  unittests/                net8.0          ← 54 test, chạy được trên mọi nền tảng
```

### Vì sao tách `Core` khỏi Windows?

Đây là quyết định kiến trúc quan trọng nhất (mô hình **Ports & Adapters**):

1. **Kiểm thử được** — toàn bộ logic nghiệp vụ (quét dấu hiệu crack, tính điểm
   rủi ro, phân loại phần mềm, dựng HTML/XLSX) unit-test được **không cần máy
   Windows**. 54 test chạy trên Linux.
2. **Dễ audit** — mọi lệnh gọi hệ thống tập trung trong đúng một file
   (`WindowsAdapters.cs`). Với một công cụ đọc dữ liệu nhạy cảm và đòi quyền
   Administrator, việc rà soát bảo mật làm được nhanh là điều thiết yếu.
3. **Dễ mở rộng** — thêm nền tảng khác chỉ cần viết adapter mới, tái dùng
   nguyên vẹn lớp render báo cáo.

## Build từ mã nguồn

Yêu cầu: **.NET 8 SDK**. Build được trên cả Windows lẫn Linux/macOS — kể cả
bước publish `win-x64` (cross-compile được), chỉ không chạy thử được file exe.

```powershell
.\build.ps1              # test + build + publish
.\build.ps1 -Package     # thêm bước đóng gói installer (cần Inno Setup 6)
```

Kết quả: `publish/swico.exe`.

Không có phụ thuộc NuGet nào ngoài `System.Management` (gói chính thức của
Microsoft để truy vấn WMI). XLSX được **tự ghi theo chuẩn OOXML** và trang báo
cáo HTML **không nạp thư viện JavaScript nào từ bên ngoài** — báo cáo mở được
trên máy không có mạng. Chi tiết: [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

## Tình trạng kiểm thử — trung thực

| Thành phần | Trạng thái |
|---|---|
| Models + JSON round-trip | ✅ Đã test |
| Parser ospp.vbs / CIM_DATETIME / InstallDate | ✅ Đã test (kể cả input rác) |
| Quét dấu hiệu crack (6 hạng mục) | ✅ Đã test trên máy giả lập bị nhiễm |
| Tính điểm rủi ro + giới hạn theo nhóm | ✅ Đã test mọi mốc biên |
| Thu thập phần mềm từ registry | ✅ Đã test |
| Diễn giải DISM/SFC | ✅ Đã test |
| Chống HTML injection | ✅ Đã test |
| XLSX: quy đổi tên cột, nhận diện số, tên sheet | ✅ Đã test |
| Dashboard đọc sâu đa máy | ✅ Đã test |
| CLI parse tham số | ✅ 16/16 test |
| **XLSX mở bằng Excel thật** | ⚠️ **CHƯA kiểm chứng** — môi trường dev không có Excel |
| **Adapter WMI/Registry thực tế** | ⚠️ **CHƯA chạy thử trên Windows** |

### Giới hạn cần biết

Lớp `Tsudev.Audit.Windows` biên dịch được nhưng **chưa từng chạy trên Windows
thật**. Cần xác nhận tên thuộc tính WMI, đặc biệt `MSFT_MpComputerStatus`,
`MSFT_MpThreatDetection` (trường `ThreatName` có thể phải tra chéo qua
`MSFT_MpThreat`) và `MSFT_ScheduledTask`.

Nếu có lỗi, hầu hết sẽ nằm gọn trong `WindowsAdapters.cs` — logic nghiệp vụ đã
được 54 test kiểm chứng nên không cần đụng tới.

Kịch bản kiểm chứng đầy đủ: [`docs/WINDOWS-VERIFICATION.md`](docs/WINDOWS-VERIFICATION.md).

## Tài liệu

| File | Nội dung |
|---|---|
| [`docs/STATE.md`](docs/STATE.md) | Trạng thái sống — **đọc đầu tiên** |
| [`docs/PLAN.md`](docs/PLAN.md) | Lộ trình theo giai đoạn |
| [`docs/DECISIONS.md`](docs/DECISIONS.md) | Các quyết định đã chốt và lý do |
| [`docs/CONTINUITY.md`](docs/CONTINUITY.md) | Giao thức nối tiếp giữa các phiên làm việc |
| [`docs/SIGNING.md`](docs/SIGNING.md) | Ký số qua SignPath Foundation |
| [`docs/WINDOWS-VERIFICATION.md`](docs/WINDOWS-VERIFICATION.md) | Kịch bản kiểm chứng trên Windows |
| [`CHANGELOG.md`](CHANGELOG.md) | Nhật ký thay đổi |

## Giấy phép

[Apache-2.0](LICENSE). Bạn được fork và phân phối bản sửa đổi, nhưng **không
được dùng tên "tsuowlit" hay "SWICO"** để phát hành bản của mình (mục 6 của
giấy phép). Xem [`NOTICE`](NOTICE).
