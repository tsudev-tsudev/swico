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
- ✅ **`64 PASS, 0 FAIL`** — khớp đúng con số README tuyên bố
- ✅ `dotnet publish -r win-x64` từ Linux → `swico.exe` **34 MB**

## Phase 3 — Danh tính, pháp lý, đóng gói, CI/CD ✅

- ✅ Chốt tên: **`tsudev SWICO`**, assembly `swico.exe`, winget `tsudev.SWICO`
- ✅ `LICENSE` (Apache-2.0), `NOTICE`, `THIRD-PARTY-NOTICES.md`
- ✅ `EULA.txt` — mục 3 nói rõ báo cáo **không phải kết luận pháp lý**
- ✅ `PRIVACY.md` — công cụ không kết nối mạng, không gửi dữ liệu đi đâu
- ✅ `packaging/innosetup/swico.iss` — song ngữ, `/VERYSILENT`, tuỳ chọn PATH
- ✅ `packaging/winget/` — 3 manifest schema 1.6
- ✅ `.github/workflows/ci.yml` + `release.yml`
- ✅ `docs/SIGNING.md`, `README.md` viết lại, `CHANGELOG.md`

## Phase 3b — Kho mã trên GitHub ✅

- ✅ Repo `github.com/tsudev-tsudev/swico` — **private**, 9 commit
- ✅ Trỏ toàn bộ URL điền tạm về kho thật (19 lần, 8 file)
- ✅ **CI xanh cả hai job**, gồm smoke test trên Windows runner
- ⬜ Chuyển sang **public** — chờ sẵn sàng nộp SignPath

## Phase 5a — Tách luật phát hiện ✅

**Đây là điều kiện để chuyển repo sang công khai**, nên làm sớm hơn phần còn
lại của Phase 5.

- ✅ `Core/Rules/detection-rules.json` — luật dạng dữ liệu có phiên bản
- ✅ `Core/Rules/DetectionRuleSet.cs` — nạp, kiểm tra, quay về mặc định an toàn
- ✅ Thứ tự ưu tiên `--rules` → file cạnh exe → bộ luật đóng kèm
- ✅ Trường `DetectionRulesVersion` trong báo cáo để truy ngược
- ✅ 10 test mới → **64 PASS, 0 FAIL**
- ✅ `docs/DETECTION-RULES.md`

## Phase 4 — Kiểm chứng trên Windows thật ✅

**Đã xong một phần:** người dùng chạy `swico.exe` trên Windows thật và **nhận
được báo cáo HTML**. Đây là lần đầu `WindowsAdapters.cs` thực thi — và nó qua
được. CI cũng chạy `--help` thành công trên Windows runner.

**Còn lại — chính là những mục quan trọng nhất:**

**Hạng mục rủi ro cao nhất của cả dự án.** Người dùng đã xác nhận có máy Windows
và tự chạy thử được. Kịch bản đầy đủ: `docs/WINDOWS-VERIFICATION.md`.

- ✅ A4: `--help` chạy được (xác nhận qua CI Windows runner)
- ✅ C1/C2: sinh được báo cáo HTML, mở xem được
- ⛔ A3: hộp thoại UAC có hiện đúng không
- ⛔ B: **xác nhận tên thuộc tính WMI** — `MSFT_MpComputerStatus`,
  `MSFT_MpThreatDetection` (trường `ThreatName`), `MSFT_ScheduledTask`
- ⛔ C: kết quả đầu ra — **C3: mở `.xlsx` bằng Excel thật** là mục quan trọng nhất
- ⛔ D: đối chiếu với bộ PowerShell cũ (bài kiểm tra hồi quy thật sự duy nhất)
- ⛔ E: ma trận ≥3 máy khác cấu hình
- ⛔ F: kiểm tra installer (cài, gỡ, cài đè, `/VERYSILENT`)
- ⛔ G: kiểm tra phần mềm diệt virus + nộp VirusTotal

## Phase 5b — Chuẩn hoá chất lượng mã còn lại 🔄

Làm khi đã có kết quả từ Phase 4 — sửa theo dữ liệu thật, không theo phỏng đoán.

- ✅ Dọn sạch 27 cảnh báo CA — trong đó CA1305 ở `FileNaming` là **lỗi thật**
- ✅ `.editorconfig` + bật `TreatWarningsAsErrors`
- ⬜ Chuyển bộ test tự chế sang **xUnit** — báo lỗi tử tế, chạy song song
      (nay đã 197 ca trong MỘT file top-level statements — quá nhiều)
- ⬜ Tách file gộp nhiều class thành file riêng
- ⬜ Logging có cấu trúc thay `Console.WriteLine` rải rác
- ✅ Thêm `--version` và `--rules`; sửa `--help` còn sót tên cũ
- ⬜ Cân nhắc `--json-only`
- ⚠️ **NativeAOT — cân nhắc lại, rủi ro cao.** Cắt tỉa đã được thử và **PHẢI BỎ**
      vì làm mất dữ liệu WMI âm thầm; NativeAOT gần như chắc chắn hỏng cùng lý do
      (WMI qua COM + phản chiếu). Nếu thử, **bắt buộc** dùng cách đối chiếu hai
      bản trên cùng một máy trong `ci.yml` — xem `docs/STATE.md` mục 4.1
- ✅ Tối ưu hiệu năng đã làm: bỏ nén trong single-file + ReadyToRun →
      file setup 29,8 → **24,9 MB** (giảm 16,4%), bỏ giải nén runtime lúc khởi động

## Phase 6a — Kiểm chứng quy trình phát hành ✅

Chạy thật `release.yml` trên runner Windows, ba lần, sửa từng lỗi lộ ra:

- ✅ **Inno Setup biên dịch được** — trước đó kịch bản có 4 lỗi *dừng hẳn*:
  thiếu `swico.ico`; `Vietnamese.isl` không có trong bản cài mặc định;
  `x64compatible` chỉ có từ 6.3; `InfoAfterFile` trỏ tới `.md`
- ✅ SBOM CycloneDX — ban 6 đổi tham số (`-f`→`-fn`, `-j`→`-F Json`), đã ghim `6.2.0`
- ✅ `tag_name` phải chỉ rõ, nếu không `workflow_dispatch` sẽ hỏng
- ✅ Bản nháp `v3.0.0` với đủ 5 tệp: exe, setup, portable zip, SBOM, checksum

## Phase 6b — Phát hành ✅ (chưa ký số)

- ✅ `v26.8.18` phát hành chính thức (18/08/2026)
- ✅ `v26.8.18.2` phát hành chính thức — thêm tự cập nhật, favicon, tối ưu hiệu năng
- ✅ Tự động cập nhật bắt buộc (`docs/UPDATES.md`)
- ✅ Bộ favicon đầy đủ sinh từ logo
- ✅ PR winget đã nộp: **microsoft/winget-pkgs#419878**
- ✅ Đường cài winget dùng được ngay: `packaging/tools/winget-local-install.ps1`

## Phase 6c — Quy ước đặt tên phiên bản ✅

Làm ở phiên S003. Điểm mấu chốt: quy ước **được thực thi bằng mã**, không phải
được nhắc trong tài liệu — vì một quy ước chỉ nằm trong văn bản là một quy ước
sẽ bị quên.

- ✅ `docs/VERSIONING.md` — dạng chuẩn `tsudev-swico-vYY.M.D[.N]`, ba điều bị
      cấm kèm lý do, số hiệu đi tới đâu, quy trình phát hành
- ✅ `Core/Updates/ReleaseName.cs` — `Validate/For/TagFor/OrdinalOfDay/NextSameDay`
- ✅ Sửa lỗi thật trong `VersionNumber.TryParse`: không đọc được tên phát hành
      đầy đủ (dấu `-` bị bước cắt hậu tố cắt nhầm), và hỏng **im lặng**
- ✅ `release.yml` — bước chặn gọi thẳng `ReleaseName.Validate`; kiểm tag khớp
      `Directory.Build.props`; tên bản phát hành dùng tên đầy đủ
- ✅ `ci.yml` — kiểm `VersionPrefix` ở mỗi PR
- ✅ 25 test mới → 197 → **224 PASS, 0 FAIL**
- ✅ Sửa `docs/UPDATES.md` (mô tả sai `.1` là bản thứ hai) và tuyên bố SemVer
      sai trong `CHANGELOG.md`

## Phase 7 — Ký số ⛔ **CHỜ NGƯỜI DÙNG**

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
