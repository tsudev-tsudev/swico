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
- `.editorconfig` — quy ước định dạng, kèm lý do cụ thể cho từng quy tắc được
  tắt hoặc nâng lên mức lỗi.
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

- **File `.xlsx` bị Excel báo hỏng và tự sửa.** `xl/_rels/workbook.xml.rels`
  thiếu quan hệ trỏ tới `styles.xml`. Theo chuẩn OPC, một phần nằm trong gói mà
  không quan hệ nào trỏ tới thì coi như không tồn tại — trong khi mọi ô đều
  tham chiếu `s="0"`/`s="1"` vào đó. Kèm theo: bổ sung `<bookViews>` (vì
  `sheetView` khai báo `workbookViewId="0"`), chống cắt vỡ cặp thế thay thế khi
  rút gọn tên sheet, giới hạn 32.767 ký tự mỗi ô, và loại bỏ thế thay thế lẻ
  cùng `U+FFFE`/`U+FFFF` khỏi dữ liệu WMI.
- **Trạng thái Office không hề tham gia vào kết luận.** Đây là nguyên nhân thật
  của việc báo cáo kết luận "không phát hiện dấu hiệu" trên một máy có Windows
  hợp lệ nhưng Office ở trạng thái `Notification` (chưa kích hoạt) — trong khi
  bộ PowerShell cũ báo không hợp lệ. Nay Office có tiếng nói riêng trong kết
  luận, ở mức **cảnh báo**: đó là vấn đề tuân thủ bản quyền, không phải dấu
  hiệu kích hoạt trái phép, nên gộp chung vào mức Bad sẽ làm mất ý nghĩa của
  mức Bad.
- **Office cài bằng Click-to-Run thường bị báo nhầm là "không có Office".**
  Danh sách đường dẫn `ospp.vbs` thiếu bố cục `...\Microsoft Office\root\Office16`
  mà mọi bản Office 2016 trở đi đều dùng. Bổ sung thêm nguồn thứ hai đọc trực
  tiếp từ `SoftwareLicensingProduct` theo ApplicationID của Office — nguồn này
  không phụ thuộc vào việc tìm thấy `ospp.vbs`.
- **Adapter WMI giả lập bỏ qua hoàn toàn `whereClause`.** Nghĩa là mọi bộ lọc —
  đặc biệt `ApplicationID` phân biệt SKU Windows với SKU Office — **chưa từng
  được kiểm thử**, đúng nơi phát sinh lớp lỗi kết luận sai về bản quyền.
- **Kết luận bản quyền có thể báo NHẦM là hợp lệ.** Trạng thái Genuine được suy ra bằng
  `licensedCount > 0` — chỉ cần **một SKU bất kỳ** ở trạng thái Licensed. Windows
  khai báo nhiều SKU dưới cùng một ApplicationID, nên máy có SKU chính đang
  `Notification`/`Unlicensed` nhưng có SKU phụ `Licensed` vẫn bị chấm "hợp lệ".
  Nay chỉ kết luận hợp lệ khi **không còn SKU nào** ở trạng thái có vấn đề; trạng
  thái còn hạn dùng thử hạ xuống mức **cảnh báo** thay vì hợp lệ.
- **Lỗi định dạng phụ thuộc ngôn ngữ máy trong `FileNaming`.** Tên thư mục kết
  quả dùng `ToString("yyyyMMdd")` không truyền `IFormatProvider`. Trên máy đặt
  ngôn ngữ Thái hoặc Ả Rập, lịch mặc định không phải Gregorian nên năm cho ra
  hoàn toàn khác (2569 thay vì 2026) — sai tên thư mục, vô hiệu quy ước 3 cấp,
  và trang tổng hợp không gom được kết quả giữa các máy khác ngôn ngữ.
- **Máy dev và CI build bằng hai SDK khác nhau.** Runner GitHub có sẵn .NET 10
  SDK, và `dotnet build` luôn chọn SDK mới nhất trên máy nếu không có
  `global.json`. CI vì thế build bằng .NET 10 trong khi máy dev dùng .NET 8 —
  làm lời hứa "build tái lập được" không thành lập. Đã thêm `global.json` ghim
  SDK và đổi workflow sang đọc từ đó.
- Dọn sạch 27 cảnh báo trình phân tích (CA1305/CA1826/CA1822/CA1869/CA1716) và
  bật `TreatWarningsAsErrors` để chúng không tích tụ trở lại.
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
