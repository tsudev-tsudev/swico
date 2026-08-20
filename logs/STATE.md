# STATE.md — Trạng thái project (agent đọc đầu phiên, cập nhật cuối phiên)

> ## ⚠️ QUAN HỆ VỚI `docs/STATE.md` — đọc trước khi ghi vào file này
>
> Repo này có **hai** file tên `STATE.md`. Chúng **không trùng vai**, và việc để
> chúng lẫn vai là đúng loại lỗi "hai cái tên cho một thứ" mà
> `docs/VERSIONING.md` mục 2 đã cảnh báo:
>
> | File | Vai trò | Nguồn sự thật của |
> |---|---|---|
> | **`docs/STATE.md`** | Trạng thái **sản phẩm** | Việc gì đã xong/đang chờ, cạm bẫy đã biết, quyết định đã chốt |
> | **`logs/STATE.md`** (file này) | **Điều phối agent** theo `AGENTS.md` mục 2 | Ai đang làm task nào, hàng đợi ngắn hạn |
>
> **Quy tắc:** khi hai file nói khác nhau về trạng thái sản phẩm →
> **`docs/STATE.md` thắng**. File này chỉ trỏ tới đó, không chép lại nội dung
> (`AGENTS.md` mục 1: mỗi tri thức ghi một lần duy nhất).

- **Phiên gần nhất:** S005 — 20/08/2026
- **Bàn giao gần nhất:** `logs/handover/20260820-02_khep-phien-S004.md`

## Hàng đợi task (làm từ trên xuống)

Nguồn: `docs/STATE.md` mục 3. Ở đây chỉ ghi mã việc + trạng thái.

- [ ] **QU-2** ⛔ Nhờ người dùng kiểm **Inno Setup** biên dịch được với
      `VersionInfoVersion=26.9.0901` (ngày một chữ số) — mục **F7/F8** trong
      `docs/WINDOWS-VERIFICATION.md`. **Rủi ro kỹ thuật còn lại duy nhất** của
      việc đổi quy ước; phía .NET đã đo xong, phía Inno Setup thì chưa.
- [ ] **QU-3** ⛔ Quyết có phát hành **bản cầu nối** dạng cũ không, để hai bản đã
      cài (`26.8.18`, `26.8.18.2`) không mất đường cập nhật — `docs/STATE.md` mục 4.7.
- [ ] **3.1** ⛔ PR winget #419878 vướng `Validation-Executable-Error` — cần Windows
- [ ] **3.2** ⛔ Kiểm chứng chức năng tự cập nhật trên Windows thật — cần người dùng
- [ ] **3.3** ⛔ Kiểm bằng mắt phần hiển thị tiến trình (mục H) — cần người dùng
- [ ] **3.4** ⛔ Cấu hình SignPath sau khi được duyệt — chờ bên ngoài
- [ ] **3.5** ⬜ Việc kỹ thuật (xUnit ▸ tách file gộp ▸ logging ▸ `--json-only`)
      — **làm được ngay, không cần người dùng**. Đáng giá nhất: chuyển sang xUnit
      (`tests/unittests/Program.cs` nay **1313 dòng** trong một file).
- [ ] **3.6** ⬜ Quyết định có phát hành `26.8.1901` không — **đọc mục 4.7 trước**
- [ ] **QU-6** ⬜ Quyết có đưa **4 sắc chữ ký thương hiệu** `tsudev` vào
      `tokens/design-tokens.json` không — hiện là bốn giá trị màu **duy nhất**
      còn viết cứng (`DesignTokens.cs`). Sửa file thuộc bộ quy ước nên ⛔ **cần
      chủ project cho phép trực tiếp**. `docs/STATE.md` mục QU-4.

## Đang thực hiện

| Task | Agent | Bắt đầu |
|---|---|---|
| (không có) | | |

## Đã hoàn thành (mới nhất trên cùng)

- 20/08/2026 — **QU-4 XONG:** báo cáo HTML + `.xlsx` chạy hoàn toàn bằng
  `tokens/` (nhúng vào assembly, sinh biến CSS nội tuyến). Thêm chế độ tối theo
  hệ điều hành + ép bảng màu sáng khi in. +19 test (mục 19). 298 PASS, 0 FAIL.
  Ba thay đổi nhìn thấy được + phần còn nợ: `docs/STATE.md` mục QU-4.
- 20/08/2026 — **QU-5 XONG:** gom định dạng ngày giờ hiển thị về `DateDisplay`
  (`Core/Reports/DateDisplay.cs`) — trước đó báo cáo in **ba** dạng khác nhau và
  không test nào chặn. +12 test (mục 18). 276 PASS, 0 FAIL. Chi tiết + hai chỗ
  cố ý KHÔNG thống nhất: `docs/STATE.md` mục QU-5.
- 20/08/2026 — **3.0 XONG:** push 4 commit S004 lên `origin/main` (`ce24691`).
  CI run **32360493509 xanh cả hai job** — lần đầu luật đặt tên mới chạy trên
  runner GitHub. Bước `Kiem tra quy uoc dat ten` của `release.yml` vẫn chưa từng
  chạy (chỉ kích hoạt khi gắn tag).
- 20/08/2026 — Sửa `docs/STATE.md`: mục 3.0 và khối trạng thái đang nói S003
  **chưa push**, trong khi `git ls-remote` cho thấy đã push và CI xanh.
- 20/08/2026 — Xoá `tsudev-conventions.zip` sau khi đối chiếu đủ 11 file và
  dựng lại được nguyên vẹn từ repo. Cách dựng lại: `docs/CONVENTIONS-README.md`.
- 20/08/2026 — **QU-1 XONG:** đổi quy ước đặt tên phiên bản sang `YY.M.DDNN`
  (D-S004-1). Mã + 30 test mới + CI/CD + đóng gói + tài liệu. 264 PASS, 0 FAIL.
- 20/08/2026 — Áp dụng bộ quy ước `tsudev-conventions` v1.0.0 vào repo
  (`AGENTS.md`, `docs/DESIGN_SYSTEM.md`, `docs/PROJECT_STRUCTURE.md`,
  `docs/ARCHITECTURE.md`, `tokens/`, `logs/`, hợp nhất `.gitignore`).
- 19/08/2026 — Khởi tạo bộ quy ước v1.0.0

## Quyết định quan trọng

- 20/08/2026 — **D-S004-1: đổi quy ước đặt tên phiên bản** sang `DESIGN_SYSTEM.md`
  mục 6. Chủ project quyết sau khi được nêu rõ rủi ro: chạm vào đường cập nhật
  của hai bản đã phát hành. Bù trừ đã làm: parser nhận **cả hai** dạng tên.
- 20/08/2026 — **Không** ghi đè `README.md` sản phẩm bằng README của bộ quy ước;
  bản gốc giữ tại `docs/CONVENTIONS-README.md`.
- 20/08/2026 — `.gitignore` **hợp nhất** (chuẩn tối thiểu + phần riêng của repo),
  không thay thế: giữ `publish/`, `packaging/output/`, `*.zip` và đặc biệt
  `tsudev-bao-cao-ra-quet-*/` — báo cáo chứa dữ liệu máy thật (`PRIVACY.md`).
- 20/08/2026 — **Không** tái cấu trúc `src/` theo cây mẫu của
  `PROJECT_STRUCTURE.md`; lý do ở `docs/ARCHITECTURE.md` mục 3.
- 19/08/2026 — Dùng Inter làm font chuẩn; token là nguồn chân lý duy nhất;
  region ưu tiên Singapore → Nhật Bản.
