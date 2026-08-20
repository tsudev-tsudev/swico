# tsudev-conventions — Bộ quy ước giao diện & vận hành toàn hệ sinh thái (v1.0.0)

Giải nén/copy toàn bộ vào gốc repo. Thứ tự đọc:

1. **AGENTS.md** — quy ước bắt buộc cho lập trình viên & agent AI (đọc đầu MỌI phiên, có sẵn câu lệnh khởi động phiên).
2. **docs/DESIGN_SYSTEM.md** — hệ màu 3 chế độ (Light/Warm/Dark), typography, component, bo góc, versioning.
3. **docs/PROJECT_STRUCTURE.md** — cây thư mục chuẩn + quy tắc đặt tên.
4. **tokens/design-tokens.json** — nguồn giá trị duy nhất; **tokens/tokens.css** — bản CSS cho Web/Electron.
5. **.gitignore** — chuẩn tối thiểu + quy tắc bổ sung liên tục.
6. **logs/** — STATE.md (hàng đợi task), LOCKS.md (khóa file), handover/ (phiếu bàn giao theo mẫu docs/templates/HANDOVER.md).

Nguyên tắc cốt lõi: chỉ dùng token, không hard-code giao diện; ngày hiển thị dạng số DD/MM/YYYY; ưu tiên dịch vụ miễn phí + region Singapore/Nhật Bản; các file quy ước là bất khả xâm phạm.

---

## Ghi chú của repo SWICO — dựng lại bộ quy ước cho repo khác

File `tsudev-conventions.zip` đã được **xoá khỏi repo này** ngày 20/08/2026 sau
khi giải nén đầy đủ (nó nằm trong `.gitignore` nên vốn không được git theo dõi).
Toàn bộ 11 file của bộ quy ước **đã được commit**, nên dựng lại được nguyên vẹn:

```bash
mkdir -p /tmp/tsudev-conventions/{docs/templates,tokens,logs/handover}
cd /tmp/tsudev-conventions
cp <repo>/AGENTS.md .
cp <repo>/docs/CONVENTIONS-README.md README.md
cp <repo>/docs/DESIGN_SYSTEM.md <repo>/docs/PROJECT_STRUCTURE.md docs/
cp <repo>/docs/templates/HANDOVER.md docs/templates/
cp <repo>/tokens/design-tokens.json <repo>/tokens/tokens.css tokens/
cp <repo>/logs/LOCKS.md logs/
touch logs/handover/.gitkeep
head -n 85 <repo>/.gitignore > .gitignore     # 85 dòng đầu = chuẩn tối thiểu
```

Đã kiểm chứng: bản dựng lại **giống bản gốc từng byte**.

Hai điểm khác so với bản gốc trong zip, đều có chủ đích:

| Mục | Ghi chú |
|---|---|
| `logs/STATE.md` | File **làm việc**, đã nạp hàng đợi thật của repo. Bản mẫu rỗng nằm trong lịch sử git, commit `bd8c7e0`. |
| `.gitignore` | 85 dòng đầu là chuẩn tối thiểu **nguyên vẹn**; phần sau là bổ sung riêng của repo, theo `AGENTS.md` mục 3. |

> Bản gốc trong zip còn ba **thư mục rỗng** thừa tên `{docs`,
> `{docs/templates,tokens,logs` và `{docs/templates,tokens,logs/handover}` — dấu
> vết của một lệnh `mkdir -p {…}` chạy bằng shell không nở ngoặc lúc đóng gói.
> Chúng không chứa file nào và **cố ý không được tạo lại**. Nếu bạn đóng gói bộ
> quy ước bằng `sh` thay vì `bash`, lỗi này sẽ lặp lại.
