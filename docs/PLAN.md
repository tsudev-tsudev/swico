# PLAN — Kế hoạch tái cấu trúc toàn diện

**Trạng thái:** ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⛔ chờ người dùng · ⏭️ đã dời

> **Bản v2** — viết lại ngày 18/08/2026 sau khi đối chiếu với artifact
> "Kế hoạch tái cấu trúc toàn diện — tsudev SWICO". Xem `docs/DECISIONS.md`
> để biết vì sao phạm vi và thứ tự đổi so với bản v1.

---

## Nguyên tắc dẫn đường

1. **Khôi phục khả năng build trước, tối ưu sau.** Không có ý nghĩa gì khi bàn
   installer hay chữ ký số cho một solution chưa biên dịch được.
2. **Giữ nguyên kiến trúc Ports & Adapters.** Đây là điểm mạnh thật sự của bản
   thiết kế gốc: nó cho phép unit-test toàn bộ nghiệp vụ trên Linux.
3. **Một nguồn sự thật cho version** — `Directory.Build.props`, chảy xuống exe,
   installer và winget manifest.
4. **Kiểm chứng thực tế trước khi đầu tư vào đóng gói.** Đóng gói và ký số một
   chương trình chưa ai biết chạy đúng hay không là đầu tư có thể phải làm lại.
5. **Tài liệu phải phản ánh sự thật.** README cũ mô tả thứ không tồn tại — đó là
   dạng nợ kỹ thuật nguy hiểm nhất vì nó khiến người đọc tin nhầm.

---

## Phase 0 — Hạ tầng nối tiếp phiên ✅

- ✅ `docs/STATE.md`, `docs/CONTINUITY.md`, `docs/PLAN.md`, `docs/journal/`
- ✅ `docs/DECISIONS.md` — đối chiếu artifact, ghi các quyết định đã chốt

## Phase 1 — Khôi phục khả năng build ✅

- ✅ `git init` + `.gitignore` + commit snapshot làm điểm lùi
- ✅ Tái cấu trúc `src/` + `tests/` đúng như csproj đang trỏ tới
- ✅ `Tsudev.Audit.Core.csproj`, `unittests.csproj`, `.sln`, `Directory.Build.props`
- ✅ Cứu `tests/unittests/Program.cs`, xoá cây `mnt/`
- ⏭️ Tách file gộp nhiều class thành file riêng → **Phase 5** (chỉnh trang, không chặn)

## Phase 2 — Viết mới lớp Rendering ✅

- ✅ `HtmlReportRenderer` — escape tập trung tại `E()` chống HTML injection
- ✅ `XlsxWriter` — tự ghi OOXML, không thêm phụ thuộc NuGet
- ✅ `DashboardBuilder` — quét đệ quy không giới hạn độ sâu
- ✅ **`54 PASS, 0 FAIL`** — khớp đúng con số README tuyên bố
- ✅ `dotnet publish -r win-x64` từ Linux → `swico.exe` **34 MB**

## Phase 3 — Danh tính, pháp lý, đóng gói, CI/CD ✅

- ✅ Chốt tên: **`tsuowlit SWICO`**, assembly `swico.exe`, winget `tsuowlit.SWICO`
- ✅ `LICENSE` (Apache-2.0), `NOTICE`, `THIRD-PARTY-NOTICES.md`
- ✅ `EULA.txt` — mục 3 nói rõ báo cáo **không phải kết luận pháp lý**
- ✅ `PRIVACY.md` — công cụ không kết nối mạng, không gửi dữ liệu đi đâu
- ✅ `packaging/innosetup/swico.iss` — song ngữ, `/VERYSILENT`, tuỳ chọn PATH
- ✅ `packaging/winget/` — 3 manifest schema 1.6
- ✅ `.github/workflows/ci.yml` + `release.yml`
- ✅ `docs/SIGNING.md`, `README.md` viết lại, `CHANGELOG.md`

## Phase 4 — Kiểm chứng trên Windows thật ⛔ **CHỜ NGƯỜI DÙNG**

**Hạng mục rủi ro cao nhất của cả dự án.** Người dùng đã xác nhận có máy Windows
và tự chạy thử được. Kịch bản đầy đủ: `docs/WINDOWS-VERIFICATION.md`.

- ⛔ A: build/test/UAC/`--help` trên Windows
- ⛔ B: **xác nhận tên thuộc tính WMI** — `MSFT_MpComputerStatus`,
  `MSFT_MpThreatDetection` (trường `ThreatName`), `MSFT_ScheduledTask`
- ⛔ C: kết quả đầu ra — **C3: mở `.xlsx` bằng Excel thật** là mục quan trọng nhất
- ⛔ D: đối chiếu với bộ PowerShell cũ (bài kiểm tra hồi quy thật sự duy nhất)
- ⛔ E: ma trận ≥3 máy khác cấu hình
- ⛔ F: kiểm tra installer (cài, gỡ, cài đè, `/VERYSILENT`)
- ⛔ G: kiểm tra phần mềm diệt virus + nộp VirusTotal

## Phase 5 — Chuẩn hoá chất lượng mã ⬜

Làm khi đã có kết quả từ Phase 4 — sửa theo dữ liệu thật, không theo phỏng đoán.

- ⬜ Dọn ~25 cảnh báo CA1305/CA1826 còn tồn
- ⬜ `.editorconfig` + cân nhắc `TreatWarningsAsErrors`
- ⬜ Chuyển bộ test tự chế sang **xUnit** — báo lỗi tử tế, chạy song song
- ⬜ Tách file gộp nhiều class thành file riêng
- ⬜ Tách luật phát hiện ra `Core/Rules/` dạng **dữ liệu có phiên bản**
      (bắt buộc, vì mã nguồn sẽ công khai — xem `docs/SIGNING.md`)
- ⬜ Logging có cấu trúc thay `Console.WriteLine` rải rác
- ⬜ Thêm `--version`, cân nhắc `--json-only`
- ⬜ **Đánh giá NativeAOT** thay `PublishSingleFile`: file nhỏ hơn nhiều (hiện
      34 MB), khởi động nhanh hơn, và **ít bị diệt virus báo nhầm hơn hẳn**

## Phase 6 — Ký số & phát hành ⛔ **CHỜ NGƯỜI DÙNG**

- ⛔ **Nộp hồ sơ SignPath Foundation** — duyệt mất vài ngày tới vài tuần,
      **nộp càng sớm càng tốt**, làm việc khác trong lúc chờ
- ⛔ Đưa repo lên GitHub công khai (điều kiện bắt buộc của SignPath)
- ⛔ Bật xác thực đa yếu tố cho GitHub và SignPath (điều kiện bắt buộc)
- ⬜ Cấu hình secret `SIGNPATH_API_TOKEN` + variable `SIGNPATH_ORGANIZATION_ID`
- ⬜ Phát hành thử `v3.0.0-rc1`, kiểm tra toàn bộ pipeline
- ⬜ Nộp manifest lên winget-pkgs

---

## Đã loại khỏi phạm vi (quyết định D2)

Ghi rõ để phiên sau không vô tình làm lại:

- ❌ **Agent chạy nền** (Windows Service) — artifact GĐ 06
- ❌ **Server tập trung + dashboard web** — artifact GĐ 07
- ❌ **Hệ thống license key thương mại** — người dùng đã chốt không làm
- ❌ MSI/WiX, MSIX/Microsoft Store
- ❌ Azure Artifact Signing — không dùng được từ Việt Nam

---

## Rủi ro còn lại

| Rủi ro | Ảnh hưởng | Cách giảm thiểu |
|---|---|---|
| Adapter WMI **chưa từng chạy thật** | Cao — tool chạy nhưng có thể ra dữ liệu rỗng | Phase 4; sai sót gần như chắc chắn gói gọn trong `WindowsAdapters.cs` |
| **XLSX chưa từng được Excel mở** | Cao — đã có 1 lỗi loại này lọt lưới ở Phase 2 | Phase 4 mục C3 |
| Diệt virus báo nhầm | Cao — đặc thù của đúng loại công cụ này | Ký số + cân nhắc NativeAOT + nộp mẫu mỗi lần phát hành |
| SignPath duyệt lâu | Trung bình — chặn ngày phát hành | Nộp sớm; pipeline vẫn chạy được khi chưa có chữ ký |
| Luật phát hiện bị né sau khi mở mã | Trung bình | Tách luật ra dữ liệu có phiên bản (Phase 5) |
| SmartScreen vẫn cảnh báo dù đã ký | Trung bình — người dùng hoang mang | Nói rõ trong tài liệu; uy tín tích luỹ dần theo lượt tải |
