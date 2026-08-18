# PLAN — Kế hoạch tái cấu trúc toàn diện

**Chú thích trạng thái:** ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⛔ bị chặn

---

## Nguyên tắc dẫn đường

1. **Khôi phục khả năng build trước, tối ưu sau.** Không có ý nghĩa gì khi bàn
   installer hay chữ ký số cho một solution chưa biên dịch được.
2. **Giữ nguyên kiến trúc Ports & Adapters.** Đây là điểm mạnh thật sự của bản
   thiết kế hiện tại: nó cho phép unit-test toàn bộ nghiệp vụ trên Linux.
   Mọi cải tiến phải củng cố, không được làm loãng ranh giới này.
3. **Một nguồn sự thật cho version.** Số hiệu phiên bản chỉ khai báo ở đúng một
   nơi (`Directory.Build.props`) rồi chảy xuống exe, installer, winget manifest.
4. **Tài liệu phải phản ánh sự thật.** README cũ mô tả thứ không tồn tại; đó là
   dạng nợ kỹ thuật nguy hiểm nhất vì nó khiến người đọc tin nhầm.

---

## Phase 0 — Hạ tầng nối tiếp phiên ✅

Chống mất việc khi máy sập / hết token / đứt context.

- ✅ `docs/STATE.md` — trạng thái sống, đọc đầu tiên
- ✅ `docs/CONTINUITY.md` — giao thức nối tiếp phiên
- ✅ `docs/PLAN.md` — file này
- ✅ `docs/journal/` — nhật ký theo phiên

## Phase 1 — Khôi phục khả năng build 🔄

**Đây là đường găng. Mọi Phase sau đều chờ Phase này.**

- 🔄 `git init` + `.gitignore` — có điểm khôi phục trước khi động vào cấu trúc
- ⬜ Tái cấu trúc `src/` + `tests/` đúng như csproj đang trỏ tới
- ⬜ Tách file gộp nhiều class thành thư mục theo tầng (Models/Abstractions/…)
- ⬜ Tạo `Tsudev.Audit.Core.csproj`, `tests/unittests/unittests.csproj`
- ⬜ Tạo `Tsudev.SystemAudit.sln`, `Directory.Build.props`, `nuget.config`
- ⬜ Cứu `tests/unittests/Program.cs` khỏi `mnt/user-data/outputs/…` rồi xoá `mnt/`

**Tiêu chí hoàn thành:** `dotnet build` chạy được và chỉ còn lỗi *thiếu Rendering*.

## Phase 2 — Viết mới lớp Rendering ⬜

Phần bị thiếu hoàn toàn. Ba class, API phải khớp chính xác lời gọi đã có sẵn
trong `Program.cs` và trong bộ test.

- ⬜ `HtmlReportRenderer` — dựng báo cáo HTML; **bắt buộc chống HTML injection**
  (bộ test đã có sẵn ca kiểm thử cho việc này)
- ⬜ `XlsxWriter` — tự ghi định dạng OpenXML bằng `System.IO.Compression`,
  **không thêm phụ thuộc NuGet** (quyết định đã chốt: tránh ràng buộc giấy phép)
- ⬜ `DashboardBuilder` — quét đệ quy **không giới hạn độ sâu**, gom nhiều máy
- ⬜ `CsvWriter` — README hứa có `.csv`, cần đối chiếu xem đã có chưa

**Tiêu chí hoàn thành:** `dotnet build` xanh hoàn toàn.

## Phase 3 — Test xanh + publish được ⬜

- ⬜ Toàn bộ unit test pass (README nói 54 — cần **đếm lại con số thật**)
- ⬜ `dotnet publish -r win-x64 --self-contained` ra được exe đơn file
- ⬜ Ghi nhận kích thước exe thật (self-contained + nén thường ~15–25 MB)

## Phase 4 — Chuẩn hoá chất lượng mã ⬜

Giờ mới đến lượt "tối ưu hoá toàn diện" — khi đã có lưới an toàn là test xanh.

- ⬜ Bật `TreatWarningsAsErrors` + `AnalysisLevel=latest-recommended`
- ⬜ `.editorconfig` thống nhất style
- ⬜ Rà soát nullable warning (hiện `Nullable=enable` nhưng chưa chắc sạch)
- ⬜ Cân nhắc chuyển bộ test tự chế (đếm pass/fail thủ công) sang **xUnit**
  → được báo cáo lỗi tử tế, chạy song song, tích hợp CI chuẩn
- ⬜ Thêm `--version`, `--json-only` cho CLI; chuẩn hoá mã thoát đã khai báo

## Phase 5 — Đóng gói ⬜

Hai kênh song song theo quyết định đã chốt.

- ⬜ `packaging/installer.iss` — Inno Setup 6:
  hiển thị EULA · chọn thư mục · tuỳ chọn thêm vào PATH ·
  `/VERYSILENT` cho triển khai hàng loạt · gỡ cài đặt sạch
- ⬜ Portable zip cho người dùng không muốn cài
- ⬜ `packaging/winget/` — manifest 3 file theo schema 1.6
- ⬜ Script `packaging/build-all.ps1` nối chuỗi: publish → ký → đóng gói → ký lại

## Phase 6 — Ký số Azure Trusted Signing ⬜

- ⬜ Tài liệu hoá bước thiết lập phía Azure (tài nguyên, Identity Validation —
  **khâu này Microsoft duyệt thủ công, cần vài ngày, đừng để tới phút chót**)
- ⬜ `.github/workflows/release.yml` — dùng `azure/trusted-signing-action`
  với OIDC (không lưu secret dài hạn trong repo)
- ⬜ Ký **cả hai**: `tsudev-audit.exe` và file setup của Inno Setup
- ⬜ Bước verify sau khi ký: kiểm tra chữ ký **và** dấu thời gian
- ⬜ Ghi chú thực tế về SmartScreen: chữ ký hợp lệ **không** xoá cảnh báo ngay;
  cần tích luỹ uy tín theo lượt tải. Cần nói rõ để khỏi kỳ vọng sai.

## Phase 7 — CI/CD ⬜

- ⬜ `ci.yml`: build + test trên ubuntu (rẻ, nhanh) cho mỗi push/PR
- ⬜ `release.yml`: kích hoạt theo tag `v*`, chạy trên windows runner để ký
- ⬜ Tự động tạo GitHub Release + đính kèm exe/setup/zip + SHA256

## Phase 8 — Tài liệu ⬜

- ⬜ Viết lại `README.md` cho khớp sự thật
- ⬜ `docs/ARCHITECTURE.md` — giải thích Ports & Adapters, sơ đồ luồng
- ⬜ `LICENSE` + `EULA.txt` cho installer
- ⬜ `CHANGELOG.md` theo Keep a Changelog
- ⬜ `docs/DEPLOYMENT.md` — hướng dẫn triển khai GPO/Intune/RMM

## Phase 9 — Kiểm chứng trên Windows thật ⛔

**Bị chặn: không có máy Windows trong môi trường này.** Cần người dùng làm.

- ⛔ Chạy exe trên Windows 10/11 thật, đối chiếu kết quả với bản PowerShell cũ
- ⛔ Xác minh tên thuộc tính WMI, đặc biệt `MSFT_MpComputerStatus`,
  `MSFT_MpThreatDetection`, `MSFT_ScheduledTask` — **chưa từng chạy thật lần nào**
- ⛔ Xác minh hộp thoại UAC xuất hiện đúng
- ⛔ Thử installer: cài, chạy, gỡ, cài lại đè phiên bản cũ

---

## Rủi ro đã nhận diện

| Rủi ro | Ảnh hưởng | Cách giảm thiểu |
|---|---|---|
| Adapter WMI **chưa từng chạy thật** | Cao — tool có thể chạy nhưng ra dữ liệu rỗng | Phase 9; sai sót gần như chắc chắn gói gọn trong `WindowsAdapters.cs` |
| Identity Validation của Azure duyệt lâu | Trung bình — chặn ngày phát hành | Nộp hồ sơ **sớm**, song song với Phase 2–5 |
| SmartScreen vẫn cảnh báo dù đã ký | Trung bình — người dùng hoang mang | Nói rõ trong tài liệu; uy tín tích luỹ dần |
| Tự ghi OpenXML cho XLSX | Trung bình — định dạng dễ sai vặt | Test round-trip; giữ cấu trúc file tối giản |
| Không chạy thử được exe ở đây | Trung bình | CI trên windows runner ít nhất chạy được `--help` |
