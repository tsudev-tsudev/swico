# CONTINUITY — Giao thức nối tiếp giữa các phiên

> Mục đích: khi máy tắt đột ngột, hết token, hoặc context window bị cắt, **phiên
> sau đọc đúng 3 file là làm việc tiếp được ngay**, không cần hỏi lại người dùng.

---

## 0. Phiên mới BẮT ĐẦU TỪ ĐÂY — đọc theo đúng thứ tự này

> **Từ 20/08/2026 repo áp dụng bộ quy ước `tsudev-conventions` v1.0.0.**
> `AGENTS.md` mục 0 cũng có một danh sách đọc đầu phiên. Hai danh sách **không
> mâu thuẫn, chúng bổ sung nhau** — đọc theo thứ tự hợp nhất dưới đây.
>
> | Hỏi gì | Đọc file nào |
> |---|---|
> | Được phép làm gì, cấm gì | `AGENTS.md` |
> | Sản phẩm đang tới đâu | `docs/STATE.md` |
> | Ai đang giữ file nào | `logs/LOCKS.md` |
> | Bàn giao gần nhất | `logs/handover/` (mới nhất theo tên) |

1. `AGENTS.md` — quy ước bắt buộc, **bất khả xâm phạm**. Đọc trước tiên.
2. `docs/STATE.md` — đang ở đâu, cái gì có/thiếu, quyết định nào đã chốt.
   **Đây là nguồn sự thật về trạng thái sản phẩm**, thắng `logs/STATE.md` khi
   hai file nói khác nhau (lý do: `logs/STATE.md` phần đầu).
3. `docs/PLAN.md` — lộ trình đầy đủ, Phase nào ✅/🔄/⬜.
4. `docs/journal/` — đọc file mới nhất (sắp theo tên) để biết **10 phút cuối
   cùng của phiên trước đã làm gì và đang định làm gì tiếp**.
5. `logs/LOCKS.md` + `logs/handover/` — khóa file và phiếu bàn giao đang mở.

**Hai hệ ghi chép, hai vai khác nhau — đừng gộp:**

| | `docs/journal/SNNN-*.md` | `logs/handover/YYYYMMDD-NN_*.md` |
|---|---|---|
| Ghi | **theo dòng thời gian**, ngay sau mỗi mốc | **một lần**, lúc bàn giao |
| Trả lời | "chuyện gì đã xảy ra, **vì sao** quyết vậy" | "phiên sau làm tiếp từ đâu" |
| Sửa lại sau không | **Không** — là bản ghi lịch sử (`docs/STATE.md` mục 4.6) | Có — mục 6 điền sau khi xử lý |

Sau đó chạy `git log --oneline -15` để đối chiếu lời kể với sự thật trong repo.

**Nguyên tắc vàng:** khi journal và mã nguồn mâu thuẫn → **tin mã nguồn**, rồi
sửa lại journal cho đúng.

## 1. Môi trường dev (Linux)

```bash
export PATH="$HOME/.dotnet:$PATH"     # BẮT BUỘC — dotnet KHÔNG có trong PATH mặc định
dotnet --version                       # kỳ vọng: 8.0.x
```

Nếu `dotnet` không tồn tại, cài lại (không cần quyền root):

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
```

Lệnh hay dùng:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build  Tsudev.SystemAudit.sln -c Release          # build tất cả
dotnet run   --project tests/unittests -c Release        # chạy test
dotnet publish src/Tsudev.Audit.Cli -c Release -r win-x64 -o publish   # ra exe
```

## 2. Quy ước ghi nhật ký — làm ĐỀU, không dồn cuối phiên

Dồn cuối phiên là cách chắc chắn nhất để mất dữ liệu khi bị cắt đột ngột.

**Mỗi phiên tạo đúng một file:** `docs/journal/SNNN-YYYY-MM-DD.md`
(`NNN` = số phiên tăng dần: S001, S002, …).

**Ghi thêm vào file đó NGAY SAU MỖI mốc sau — không đợi:**

- hoàn thành một Phase hoặc một task,
- ra một quyết định kỹ thuật (kèm **lý do**, vì lý do mới là thứ khó tái tạo),
- gặp lỗi mất >10 phút để gỡ (kèm **cách gỡ**),
- ngay trước khi bắt đầu một việc dài/rủi ro (để nếu đứt thì biết đang dở ở đâu).

**Đồng thời cập nhật:**
- `docs/STATE.md` khi hiện trạng đổi (mục 4 "đang làm dở ngay lúc này").
- `docs/PLAN.md` khi một Phase đổi trạng thái ⬜ → 🔄 → ✅.

## 3. Mẫu một mục nhật ký

```markdown
### HH:MM — <việc gì>

**Bối cảnh:** đang làm gì, vì sao động tới chỗ này.
**Đã làm:** thay đổi cụ thể, kèm đường dẫn file:dòng.
**Kết quả:** build/test xanh hay đỏ; số liệu thật, không phỏng đoán.
**Quyết định & lý do:** (nếu có) chọn A thay vì B vì …
**Tiếp theo:** việc kế tiếp đã định làm.
```

## 4. Kỷ luật commit — mỗi commit là một điểm khôi phục

- Commit **nhỏ và thường xuyên**; mỗi commit phải để repo ở trạng thái mô tả được.
- Không bao giờ để công việc chỉ tồn tại trong context của model.
- Mẫu message:

```
<phase>: <việc đã làm>

<vì sao>

Refs: docs/journal/SNNN-YYYY-MM-DD.md
```

## 5. Kết thúc phiên (khi còn kịp)

Trước khi dừng, đảm bảo cả 4 điều sau — nếu thiếu, phiên sau sẽ mò:

1. `docs/STATE.md` mục 4 mô tả **đúng** việc đang dở.
2. Journal hôm nay có mục cuối ghi rõ **"Tiếp theo:"**.
3. `docs/PLAN.md` đúng trạng thái từng Phase.
4. Đã commit hết (`git status` sạch), hoặc journal nói rõ vì sao còn dở dang.
