# Nhật ký thay đổi

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/);
đánh số phiên bản theo [SemVer](https://semver.org/lang/vi/).

## [Chưa phát hành]

### Thêm

- **Bộ luật phát hiện tách khỏi mã nguồn** (`Core/Rules/`) — dạng dữ liệu JSON
  có phiên bản, nạp theo thứ tự `--rules` → file cạnh exe → bộ luật đóng kèm.
  File hỏng/thiếu/rỗng đều quay về bộ luật đóng kèm kèm cảnh báo. Cho phép cập
  nhật dấu hiệu mà không phải biên dịch, ký và phát hành lại.
- Tham số CLI `--rules <file.json>` và `--version`.
- Trường `DetectionRulesVersion` trong báo cáo — truy ngược được kết luận do bộ
  luật nào sinh ra.
- Lớp `Core.Rendering` — **viết mới hoàn toàn** sau khi bản gốc bị mất:
  `HtmlReportRenderer`, `XlsxWriter` (tự ghi OOXML, không phụ thuộc NuGet),
  `DashboardBuilder` (quét đệ quy không giới hạn độ sâu).
- Hồ sơ pháp lý: `LICENSE` (Apache-2.0), `NOTICE`, `EULA.txt`, `PRIVACY.md`,
  `THIRD-PARTY-NOTICES.md`.
- Bộ đóng gói: `packaging/innosetup/swico.iss` (Inno Setup 6, hai ngôn ngữ
  Việt–Anh, hỗ trợ `/VERYSILENT`, tuỳ chọn thêm vào PATH) và manifest winget.
- CI/CD: `.github/workflows/ci.yml` và `release.yml` — build, test, đóng gói,
  ký số qua SignPath Foundation, sinh SBOM, tính checksum, tạo release.
- Hạ tầng nối tiếp phiên: `docs/STATE.md`, `docs/CONTINUITY.md`, `docs/PLAN.md`,
  `docs/DECISIONS.md`, `docs/journal/`.
- `docs/WINDOWS-VERIFICATION.md` — kịch bản kiểm chứng trên máy Windows thật.

### Thay đổi

- **Đổi tên sản phẩm thành `tsuowlit SWICO`.** Trước đó ba tên dùng lẫn lộn:
  thư mục `tsudev-swico`, assembly `tsudev-audit`, manifest `tsudev.SystemAudit`.
  Tên assembly nay là `swico.exe`; namespace `Tsudev.Audit.*` **giữ nguyên** vì
  đó là chi tiết nội bộ người dùng không thấy.
- Tái cấu trúc từ 9 file `.cs` nằm phẳng ở thư mục gốc sang layout `src/` +
  `tests/` mà các `.csproj` vốn đã trỏ tới.
- Gom version và metadata về `Directory.Build.props` — một nguồn sự thật duy nhất.

### Sửa

- **Dự án không build được.** Thiếu `Tsudev.Audit.Core.csproj` (cả hai csproj
  còn lại đều tham chiếu tới nó) và thiếu toàn bộ namespace `Core.Rendering`
  mà `Program.cs` gọi ở 3 chỗ.
- `<pane>` trong file XLSX đặt sai vị trí theo lược đồ SpreadsheetML — phải nằm
  trong `<sheetViews>` **trước** `<sheetData>`. File vẫn là XML hợp lệ nên lỗi
  này lọt lưới khi chỉ thử trên Linux, nhưng Excel sẽ báo file hỏng.
- Khôi phục bộ test bị lạc trong `mnt/user-data/outputs/`; nay `64 PASS, 0 FAIL`.
- Phần trợ giúp `--help` còn sót tên cũ `tsudev System Audit` / `tsudev-audit.exe`
  sau khi đổi tên sản phẩm.

### Đã biết còn tồn đọng

- `WindowsAdapters.cs` đã chạy được trên Windows thật (sinh ra báo cáo HTML),
  nhưng **chưa đối chiếu tên thuộc tính WMI** và chưa so với bản PowerShell cũ.
  Xem `docs/WINDOWS-VERIFICATION.md`.
- File `.xlsx` chưa từng được Excel thật mở (môi trường phát triển không có).
- Còn khoảng 25 cảnh báo CA1305/CA1826 chưa dọn.

## [3.0.0] — chưa phát hành

Bản viết lại bằng C#/.NET 8 từ bộ 10 file PowerShell, gộp thành một file `.exe`
duy nhất. Phiên bản này chưa từng được phát hành ra ngoài.
