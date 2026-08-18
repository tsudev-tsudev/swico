# tsudev System Audit (bản viết lại bằng C#/.NET 8)

Bộ công cụ kiểm tra bản quyền Windows và thu thập cấu hình phần cứng, viết lại
từ bộ 10 file PowerShell thành **một file `.exe` duy nhất**.

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
  Tsudev.Audit.Cli/         net8.0-windows  ← 1 file exe, thay cả 2 file .bat
tests/
  unittests/                net8.0          ← 54 test, chạy được trên mọi nền tảng
```

### Vì sao tách `Core` khỏi Windows?

Đây là quyết định kiến trúc quan trọng nhất (mô hình **Ports & Adapters**):

1. **Kiểm thử được**: toàn bộ logic nghiệp vụ (quét dấu hiệu crack, tính điểm rủi
   ro, phân loại phần mềm, dựng HTML/XLSX) unit-test được **không cần máy Windows**.
   54 test hiện chạy trên Linux CI.
2. **Dễ mở rộng**: nếu sau này làm collector cho Linux, chỉ cần viết adapter mới —
   **tái dùng nguyên vẹn** toàn bộ lớp render báo cáo.
3. **Dễ audit**: mọi lệnh gọi hệ thống tập trung trong đúng một file
   (`WindowsAdapters.cs`), thuận lợi khi rà soát bảo mật.

## Build

Yêu cầu: **.NET 8 SDK** trên Windows, có internet (để tải gói `System.Management`).

```powershell
# Tạo solution (chỉ cần làm 1 lần)
dotnet new sln -n Tsudev.SystemAudit
dotnet sln add src/Tsudev.Audit.Core/Tsudev.Audit.Core.csproj
dotnet sln add src/Tsudev.Audit.Windows/Tsudev.Audit.Windows.csproj
dotnet sln add src/Tsudev.Audit.Cli/Tsudev.Audit.Cli.csproj
dotnet sln add tests/unittests/unittests.csproj

# Build + test + xuất exe
.\build.ps1
```

> **Lưu ý về `nuget.config`**: file này đang có `<clear />` để build offline.
> Trên máy có internet, **xoá dòng `<clear />`** (hoặc xoá luôn file) để NuGet
> hoạt động bình thường.

Kết quả: `publish/tsudev-audit.exe` — file đơn, không cần cài .NET Runtime.

## Cách dùng

```
tsudev-audit.exe                          Quét đầy đủ, mở báo cáo khi xong
tsudev-audit.exe --scope license          Chỉ kiểm tra bản quyền
tsudev-audit.exe --scope hardware         Chỉ thu thập phần cứng
tsudev-audit.exe --silent --sfc           Quét sâu, không mở trình duyệt (GPO/RMM)
tsudev-audit.exe -o D:\BaoCao --silent    Lưu vào thư mục chỉ định
tsudev-audit.exe --help                   Xem toàn bộ tham số
```

### Mã thoát (cho tích hợp RMM/giám sát)

| Mã | Ý nghĩa |
|----|---------|
| 0 | Thành công hoàn toàn |
| 1 | Thành công nhưng có mục thiếu dữ liệu (báo cáo chính vẫn đầy đủ) |
| 2 | Lỗi nghiêm trọng, không tạo được báo cáo |
| 3 | Tham số dòng lệnh không hợp lệ |

## Cấu trúc thư mục kết quả

Giữ nguyên quy ước 3 cấp đã thống nhất:

```
<thư mục chứa exe>/
  tsudev-bao-cao-ra-quet-<Máy>-<Ngày>/          ← cấp 2
    tsudev-tong-hop.html                         ← trang tổng hợp mọi máy
    tsudev-ket-qua-ra-quet-<Máy>-<Ngày>/         ← cấp 3
      Windows_License_Audit_*.html / .json / .xlsx / .csv
      Windows_Hardware_Inventory_*.html / .json / .xlsx / .csv
```

Copy thư mục **cấp 3** từ máy khác vào cấp 2 → `tsudev-tong-hop.html` tự động
đọc sâu (đệ quy, **không giới hạn độ sâu**) và gom vào bảng tổng hợp.

## Tình trạng kiểm thử

| Thành phần | Trạng thái |
|---|---|
| Models + JSON round-trip | ✅ Đã test |
| Parser ospp.vbs / CIM_DATETIME / InstallDate | ✅ Đã test (kể cả input rác) |
| Quét dấu hiệu crack (6 hạng mục) | ✅ Đã test trên máy giả lập bị nhiễm |
| Tính điểm rủi ro + giới hạn theo nhóm | ✅ Đã test mọi mốc biên |
| Thu thập phần mềm từ registry | ✅ Đã test |
| Diễn giải DISM/SFC | ✅ Đã test |
| Chống HTML injection | ✅ Đã test |
| XLSX writer | ✅ openpyxl + LibreOffice round-trip |
| Dashboard đọc sâu đa máy | ✅ Đã test tới 2 cấp lồng nhau |
| CLI parse tham số | ✅ 16/16 test |
| **Adapter WMI/Registry thực tế** | ⚠️ **CHƯA chạy thử trên Windows** |

### Giới hạn cần biết

Môi trường phát triển không tải được NuGet nên **lớp `Tsudev.Audit.Windows`
chưa được biên dịch/chạy thử**. Cần kiểm tra trên máy Windows thật:

1. `dotnet build` có qua không (gói `System.Management` phải restore được).
2. Tên thuộc tính WMI có khớp không — đặc biệt:
   - `MSFT_MpComputerStatus` / `MSFT_MpThreatDetection` (Defender)
   - `MSFT_ScheduledTask` (namespace `root\Microsoft\Windows\TaskScheduler`)
   - `ThreatName` trong `MSFT_MpThreatDetection` (có thể phải tra thêm qua `MSFT_MpThreat`)
3. Hộp thoại UAC có hiện đúng không (do `app.manifest`).

Nếu có lỗi, hầu hết sẽ nằm gọn trong `WindowsAdapters.cs` — logic nghiệp vụ đã
được kiểm chứng nên không cần đụng tới.

## Bước tiếp theo

1. Build + chạy thử trên Windows, đối chiếu kết quả với bản PowerShell cũ.
2. **Ký số (Authenticode)** — bắt buộc trước khi phân phối rộng.
3. Đóng gói winget (đường ngắn nhất tới người dùng phổ thông).
4. Cân nhắc MSIX/Microsoft Store (lưu ý: xung đột với yêu cầu quyền Administrator).
