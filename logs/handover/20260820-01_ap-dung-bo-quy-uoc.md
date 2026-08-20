# PHIẾU BÀN GIAO — Áp dụng bộ quy ước tsudev-conventions v1.0.0

- **Mã phiếu**: 20260820-01
- **Từ**: phiên S004 (Claude Code) — **Đến**: phiên sau / chủ project
- **Thời điểm**: 16:20 20/08/2026
- **Trạng thái**: HOÀN THÀNH (xem mục 6)

## 1. Việc đã làm xong

- Đọc toàn bộ 18 file markdown sẵn có trước khi động vào repo.
- Giải nén `tsudev-conventions.zip` (zip lồng zip) và cài vào gốc repo:

| File/Thư mục | Ghi chú |
|---|---|
| `AGENTS.md` | nguyên bản — bất khả xâm phạm |
| `docs/DESIGN_SYSTEM.md` | nguyên bản |
| `docs/PROJECT_STRUCTURE.md` | nguyên bản |
| `docs/templates/HANDOVER.md` | nguyên bản |
| `tokens/design-tokens.json`, `tokens/tokens.css` | nguyên bản |
| `logs/LOCKS.md`, `logs/handover/` | nguyên bản |
| `logs/STATE.md` | đã **nạp hàng đợi thật** từ `docs/STATE.md` mục 3 |
| `docs/ARCHITECTURE.md` | **mới** — bắt buộc theo `PROJECT_STRUCTURE.md`, viết dạng trỏ đường dẫn, không chép lại |
| `.gitignore` | **hợp nhất**, không thay thế (xem mục 5.2) |
| `docs/CONVENTIONS-README.md` | README của bộ quy ước, dời sang đây (xem mục 5.1) |

- Kiểm chứng: `dotnet build -c Release` → **0 cảnh báo, 0 lỗi**;
  `dotnet run --project tests/unittests -c Release` → **234 PASS, 0 FAIL**
  (đo trên máy dev Linux — không có thay đổi mã nguồn nào trong phiếu này).
- Kiểm chứng `.gitignore` mới **không làm mất file nào đang được git theo dõi**
  (`git ls-files | git check-ignore --stdin` → trống).

## 2. Việc dang dở + bước tiếp theo CỤ THỂ

- [x] Chủ project đã quyết mục **5.3** → **phương án B**. Xem mục 6.
- [ ] Commit các file trên (chưa commit — theo `docs/STATE.md` mục 3.0, repo còn
      một loạt commit của phiên S003 **chưa push**; đừng trộn hai việc vào nhau).
- [ ] Áp `tokens/` vào lớp render báo cáo HTML (`Core/Rendering/HtmlReportRenderer.cs`,
      `DashboardBuilder.cs`) — hiện màu/cỡ chữ đang **hard-code**, trái
      `AGENTS.md` mục 6. Đây là việc thật, chưa làm, và cần cẩn thận:
      báo cáo HTML **không được nạp tài nguyên ngoài** (`THIRD-PARTY-NOTICES.md`),
      nên token phải **nội tuyến** vào `<style>` chứ không `@import tokens.css`.
- [ ] Rà `DD/MM/YYYY` trong báo cáo và `FileNaming` — quy ước bắt buộc
      `HH:mm DD/MM/YYYY`, chưa đối chiếu.

## 3. File liên quan / đang khóa

Không khóa file nào (`logs/LOCKS.md` trống). Phiên này chỉ thêm file mới,
sửa duy nhất `.gitignore`.

## 4. Yêu cầu gửi agent đang giữ khóa

Không có.

## 5. Cảnh báo / quyết định quan trọng

### 5.1 README — cố ý KHÔNG ghi đè

`tsudev-conventions/README.md` là README **của bộ quy ước**, còn `README.md` ở gốc
repo là README **của sản phẩm** (344 dòng, có badge CI, bảng mã thoát, kiến trúc,
bảng "tình trạng kiểm thử — trung thực"). Copy đè sẽ **xoá trắng** tài liệu đó.
Đã dời bản gốc sang `docs/CONVENTIONS-README.md`; nội dung y hệt
`HUONG-DAN-SU-DUNG.md` trong file zip.

### 5.2 `.gitignore` — hợp nhất, không thay thế

`.gitignore` chuẩn của bộ quy ước **thiếu** những mục sống còn của repo này:

| Mục bị thiếu | Hậu quả nếu mất |
|---|---|
| `tsudev-bao-cao-ra-quet-*/`, `tsudev-ket-qua-ra-quet-*/` | **Commit nhầm dữ liệu audit thật** — số sê-ri phần cứng, danh sách phần mềm, trạng thái bản quyền của máy thật (`PRIVACY.md`) |
| `publish/`, `packaging/output/`, `artifacts/` | Rác build lọt vào repo |
| `*.zip` | Bản portable dựng cục bộ lọt vào repo — đúng cạm bẫy `docs/STATE.md` mục 4.4 |
| `.vs/`, `*.suo` | Rác IDE |

Đã giữ chuẩn tối thiểu **nguyên vẹn** rồi bổ sung phần riêng ở cuối file, đúng
tinh thần `AGENTS.md` mục 3 ("bổ sung, không xóa bớt").

### 5.3 ⛔ XUNG ĐỘT THẬT — quy ước đặt tên phiên bản (CẦN CHỦ PROJECT QUYẾT)

Hai quy ước **loại trừ nhau**, và cả hai đều đang nằm trong repo:

| | `docs/DESIGN_SYSTEM.md` mục 6 (mới) | `docs/VERSIONING.md` (đang chạy) |
|---|---|---|
| Dạng | `tsudev-swico_26.8.1901_x64-setup.exe` | `swico-setup-26.8.19.exe` |
| Chuỗi version | `26.8.1901` | `26.8.19` |
| Bản 1 trong ngày | có số đếm `01` | **không** có số đếm |
| Bản 2 trong ngày | `26.8.1902` | `26.8.19.2` |

**Chưa áp dụng quy ước mới**, vì bốn lý do đo được — không phải phỏng đoán:

1. **Bị chặn bởi chính mã sản xuất.** `ReleaseName.Validate()`
   (`src/Tsudev.Audit.Core/Updates/ReleaseName.cs`) đọc thành phần thứ ba là
   **ngày**; `1901` không phải ngày hợp lệ → **`release.yml` dừng hẳn** ở bước
   *Kiem tra quy uoc dat ten*, và `ci.yml` đỏ ở mỗi PR. Quy ước cũ được **25 test**
   thi hành.
2. **Làm hỏng chức năng tự cập nhật của hai bản ĐÃ phát hành ra ngoài.**
   `26.8.18` và `26.8.18.2` đang chạy trên máy người dùng, và chúng tìm file cài
   đặt **theo tên** `swico-setup-<phiên-bản>.exe` (`GitHubReleaseParser`).
   Đổi sang `tsudev-swico_..._x64-setup.exe` thì các máy đó **không tìm thấy
   asset** → cập nhật hỏng **trong im lặng**. Đúng loại lỗi mà `docs/UPDATES.md`
   tồn tại để ngăn.
3. **Ngày 1–9 sinh số 0 đứng đầu.** `DD` hai chữ số → bản 1 ngày 09/9 là
   `26.9.0901`. .NET `Version` đọc `0901` thành `901` rồi in ra `26.9.901`,
   **lệch** với tên file `26.9.0901`. Đây đúng là lỗi mà `docs/VERSIONING.md`
   mục 2.2 mô tả: cùng số hiệu, khác chuỗi → hai tên file cài đặt.
4. **Không tự động chuyển đổi được đường lùi.** Đây là quyết định hướng ra ngoài
   (ảnh hưởng máy người dùng đã cài), nên thuộc thẩm quyền chủ project.

> Ghi nhận công bằng cho quy ước mới: nó **không** mắc lỗi so sánh số nguyên mà
> `docs/VERSIONING.md` mục 3 cảnh báo. Vì `DD` được đệm 2 chữ số nên
> `26.8.1901 < 26.8.1902 < 26.8.2001` vẫn đúng thứ tự. Vấn đề nằm ở 4 điểm trên,
> không nằm ở thứ tự so sánh.

**Ba phương án:**

| | Phương án | Việc phải làm | Rủi ro |
|---|---|---|---|
| **A** | **Giữ `docs/VERSIONING.md`** (khuyến nghị) | Chủ project ghi một dòng ngoại lệ cho repo này | Thấp — không đụng gì đang chạy |
| **B** | Đổi sang `DESIGN_SYSTEM.md` mục 6 | Sửa `ReleaseName.cs` + 25 test + `release.yml` + `ci.yml` + `swico.iss` + `Directory.Build.props` + `GitHubReleaseParser` (nhận **cả hai** dạng tên để bản cũ còn cập nhật được) + nộp lại manifest winget | **Cao** — chạm đúng đường cập nhật của bản đã phát hành |
| **C** | Sửa `DESIGN_SYSTEM.md` cho khớp thực tế | Vi phạm "bất khả xâm phạm" — **chỉ chủ project được quyết** | Ảnh hưởng mọi repo khác trong hệ sinh thái |

Khuyến nghị **A**: quy ước hiện tại đã được thực thi bằng mã, có test, có cổng
chặn CI/CD, và đã sống sót qua hai bản phát hành thật.

### 5.4 `src/` không theo cây mẫu — có chủ đích

`PROJECT_STRUCTURE.md` mô tả cây `components|features|services|utils` (Web/Electron).
Repo này là .NET solution, `src/` chia theo project biên dịch. Lý do đầy đủ:
`docs/ARCHITECTURE.md` mục 3. **Không** tái cấu trúc — sẽ phá kiến trúc
Ports & Adapters vốn là thứ cho phép 234 test chạy được trên Linux.

## 6. Kết quả xử lý (agent nhận điền sau khi thực hiện)

**20/08/2026 — chủ project chọn phương án B: đổi sang `DESIGN_SYSTEM.md` mục 6.**
Rủi ro ở mục 5.3 đã được nêu đầy đủ trước khi quyết và vẫn được chấp nhận.

**ĐÃ THỰC HIỆN XONG** — diễn biến đầy đủ: `docs/journal/S004-2026-08-20.md`
mục 16:35 và 16:52.

| Việc | Kết quả |
|---|---|
| `VersionNumber` đọc **cả hai** dạng | ✅ `26.8.18` ≡ `26.8.1801` |
| `GitHubReleaseParser` nhận **cả hai** dạng tên tệp | ✅ ràng buộc sống còn ở mục 5.3, đã làm |
| `ReleaseName.Validate` + cổng CI/CD | ✅ thử tay 10 chuỗi, đúng cả 10 |
| Đóng gói (Inno Setup, winget, release.yml) | ✅ |
| Test | ✅ 234 → **264 PASS, 0 FAIL** |
| Tài liệu | ✅ VERSIONING (viết lại), README, UPDATES, CHANGELOG, STATE, PLAN |

**Hai việc mở ra từ phiếu này** (đã vào hàng đợi `logs/STATE.md`):

- **QU-2** — Inno Setup với `26.9.0901`: **chưa kiểm được**, máy dev không có `ISCC`.
  Phía .NET đã đo xong; phía Inno Setup là rủi ro còn lại.
- **QU-3** — hai bản đã phát hành mất đường cập nhật. **Không sửa được bằng mã**
  (bộ đọc cũ đã biên dịch sẵn trong exe trên máy người dùng). Hệ quả thật đã truy
  theo mã: `CheckFailed` → vẫn quét bình thường kèm ghi chú, không sập. Cách gỡ:
  một bản cầu nối mang số hiệu dạng cũ — quyết định của chủ project.
