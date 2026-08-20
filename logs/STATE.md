# STATE.md — Trạng thái project (agent đọc đầu phiên, cập nhật cuối phiên)

> ## ⚠️ QUAN HỆ VỚI `docs/STATE.md` — đọc trước khi ghi vào file này
>
> Repo này có **hai** file tên `STATE.md`. Chúng **không trùng vai**, và việc để
> chúng lẫn vai là đúng loại lỗi "hai cái tên cho một thứ" mà
> `docs/VERSIONING.md` mục 2.1 đã cảnh báo:
>
> | File | Vai trò | Nguồn sự thật của |
> |---|---|---|
> | **`docs/STATE.md`** | Trạng thái **sản phẩm** | Việc gì đã xong/đang chờ, cạm bẫy đã biết, quyết định đã chốt |
> | **`logs/STATE.md`** (file này) | **Điều phối agent** theo `AGENTS.md` mục 2 | Ai đang làm task nào, hàng đợi ngắn hạn |
>
> **Quy tắc:** khi hai file nói khác nhau về trạng thái sản phẩm →
> **`docs/STATE.md` thắng**. File này chỉ trỏ tới đó, không chép lại nội dung
> (`AGENTS.md` mục 1: mỗi tri thức ghi một lần duy nhất).

## Hàng đợi task (làm từ trên xuống)

Nguồn: `docs/STATE.md` mục 3. Ở đây chỉ ghi mã việc + trạng thái khóa.

- [ ] **3.0** Đẩy các commit đang chờ lên `origin/main` — ⛔ cần người dùng đồng ý
      (đẩy lên kho công khai là việc hướng ra ngoài). Đếm bằng
      `git log --oneline origin/main..HEAD`.
- [ ] **3.1** PR winget #419878 vướng `Validation-Executable-Error` — ⛔ cần Windows
- [ ] **3.2** Kiểm chứng chức năng tự cập nhật trên Windows thật — ⛔ cần người dùng
- [ ] **3.3** Kiểm bằng mắt phần hiển thị tiến trình (mục H) — ⛔ cần người dùng
- [ ] **3.4** Cấu hình SignPath sau khi được duyệt — ⛔ chờ bên ngoài
- [ ] **3.5** Việc kỹ thuật (xUnit ▸ tách file gộp ▸ logging ▸ `--json-only`) — làm được ngay
- [ ] **3.6** Quyết định có phát hành `26.8.19` không — ⛔ cần người dùng
- [ ] **QU-2** Nhờ người dùng kiểm **Inno Setup** biên dịch được với
      `VersionInfoVersion=26.9.0901` (ngày một chữ số) — ⛔ cần Windows.
      Rủi ro kỹ thuật còn lại duy nhất của việc đổi quy ước; `docs/STATE.md` mục 3.2.
- [ ] **QU-3** Quyết có phát hành **bản cầu nối** dạng cũ không, để hai bản đã
      cài (`26.8.18`, `26.8.18.2`) không mất đường cập nhật — ⛔ cần người dùng;
      `docs/STATE.md` mục 4.7.

## Đang thực hiện

| Task | Agent | Bắt đầu |
|---|---|---|
| (không có) | | |

## Đã hoàn thành (mới nhất trên cùng)

- 20/08/2026 — Xoá `tsudev-conventions.zip` sau khi đối chiếu đủ 11 file và
  dựng lại được nguyên vẹn từ repo. Cách dựng lại: `docs/CONVENTIONS-README.md`.
- 20/08/2026 — **QU-1 XONG:** đổi quy ước đặt tên phiên bản sang `YY.M.DDNN`
  (D-S004-1). Mã + 30 test mới + CI/CD + đóng gói + tài liệu. 264 PASS, 0 FAIL.
- 20/08/2026 — Áp dụng bộ quy ước `tsudev-conventions` v1.0.0 vào repo
  (`AGENTS.md`, `docs/DESIGN_SYSTEM.md`, `docs/PROJECT_STRUCTURE.md`,
  `docs/ARCHITECTURE.md`, `tokens/`, `logs/`, hợp nhất `.gitignore`).
  Phiếu bàn giao: `logs/handover/20260820-01_ap-dung-bo-quy-uoc.md`.
- 19/08/2026 — Khởi tạo bộ quy ước v1.0.0

## Quyết định quan trọng

- 20/08/2026 — **Không** ghi đè `README.md` sản phẩm bằng README của bộ quy ước;
  bản gốc giữ tại `docs/CONVENTIONS-README.md`.
- 20/08/2026 — `.gitignore` **hợp nhất** (chuẩn tối thiểu + phần riêng của repo),
  không thay thế: giữ `publish/`, `packaging/output/`, `*.zip` và đặc biệt
  `tsudev-bao-cao-ra-quet-*/` — báo cáo chứa dữ liệu máy thật (`PRIVACY.md`).
- 20/08/2026 — **D-S004-1: đổi quy ước đặt tên phiên bản sang `DESIGN_SYSTEM.md`
  mục 6** (phương án B). Chủ project quyết sau khi được nêu rõ rủi ro: chạm vào
  đường cập nhật của hai bản đã phát hành. Bù trừ bắt buộc: parser nhận cả hai dạng tên.
- 19/08/2026 — Dùng Inter làm font chuẩn; token là nguồn chân lý duy nhất;
  region ưu tiên Singapore → Nhật Bản.
