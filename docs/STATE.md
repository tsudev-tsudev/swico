# STATE — Trạng thái sống của dự án

> **File này là nguồn sự thật DUY NHẤT về "đang làm tới đâu".**
> Phiên Claude Code mới **BẮT BUỘC đọc file này đầu tiên**.
> Quy ước cập nhật: xem `docs/CONTINUITY.md`.

- **Cập nhật lần cuối:** 2026-08-18
- **Phiên gần nhất:** S001
- **Giai đoạn:** Phase 3 xong ✅ · **Phase 4 đang chờ người dùng** ⛔

---

## 1. Kết luận ngắn gọn về hiện trạng

Dự án **đã build được, test xanh, đóng gói được**. Đây là thay đổi lớn so với
lúc nhận bàn giao (khi đó **không build được**).

```
Build:    ✅ Build succeeded
Test:     ✅ 54 PASS, 0 FAIL
Publish:  ✅ publish/swico.exe — 34 MB, PE32+ x86-64
```

**Việc còn lại KHÔNG nằm ở mã nguồn mà ở kiểm chứng thực tế và thủ tục:**
chạy thử trên Windows thật, và nộp hồ sơ SignPath Foundation.

## 2. Sản phẩm: `tsuowlit SWICO`

| Hạng mục | Giá trị |
|---|---|
| Tên sản phẩm | `tsuowlit SWICO` |
| Assembly | `swico.exe` |
| Winget ID | `tsuowlit.SWICO` |
| Namespace | `Tsudev.Audit.*` — **giữ nguyên**, chi tiết nội bộ |
| Solution | `Tsudev.SystemAudit.sln` — giữ nguyên |
| Tên miền thương hiệu | `https://tsudev.com` — **giữ nguyên** (bộ test khẳng định điều này) |
| Giấy phép | Apache-2.0 |
| Ký số | SignPath Foundation |
| Phiên bản | 3.0.0 (`Directory.Build.props`) |

## 3. Cái gì đã có

| Thành phần | Trạng thái |
|---|---|
| `Core/Models`, `Abstractions`, `Collectors`, `Reports`, `Testing` | ✅ Có từ đầu |
| **`Core/Rendering`** | ✅ **Đã viết mới ~840 dòng** ở phiên S001 |
| `Tsudev.Audit.Windows` | ✅ Biên dịch được — ⚠️ **chưa chạy trên Windows thật** |
| `Tsudev.Audit.Cli` | ✅ Có |
| Project files, `.sln`, `Directory.Build.props` | ✅ Đã tạo |
| Git repo | ✅ Đã init, 7 commit |
| Pháp lý: LICENSE/NOTICE/EULA/PRIVACY/THIRD-PARTY | ✅ Đủ |
| Installer Inno Setup + winget manifest | ✅ Viết xong — ⚠️ **chưa build thử** |
| CI/CD workflows | ✅ Viết xong — ⚠️ **chưa chạy thật** |
| Tài liệu | ✅ README/PLAN/DECISIONS/SIGNING/CONTINUITY/CHANGELOG |

## 4. VIỆC TIẾP THEO — hai việc, cả hai đều cần người dùng

### 4.1 ⛔ Chạy kịch bản kiểm chứng Windows

Mở `docs/WINDOWS-VERIFICATION.md`, chạy từng mục, điền cột "Kết quả thật".

**Ba mục quan trọng nhất:**
- **B2** — `MSFT_MpThreatDetection` có trường `ThreatName` không?
- **C3** — mở file `.xlsx` bằng **Excel thật**, xem có báo "file hỏng" không
- **D1** — đối chiếu kết quả với bộ PowerShell cũ trên cùng một máy

### 4.2 ⛔ Nộp hồ sơ SignPath Foundation

Duyệt mất vài ngày tới vài tuần → **nộp sớm, làm việc khác trong lúc chờ**.
Cần trước: đưa repo lên GitHub công khai + bật xác thực đa yếu tố.
Chi tiết: `docs/SIGNING.md`.

## 5. Cạm bẫy đã biết — đọc để khỏi vấp lại

- Máy dev là **Linux**. `dotnet` **không có trong PATH mặc định**:
  `export PATH="$HOME/.dotnet:$PATH"` (SDK 8.0.424 đã cài ở `~/.dotnet`).
- `net8.0-windows` **build được trên Linux**, `publish -r win-x64` cũng chạy.
  Nhưng **không chạy thử được** file exe ở đây.
- `System.Management` biên dịch được trên Linux nhưng ném exception khi chạy —
  đây là lý do kiến trúc Ports & Adapters tồn tại, **đừng "sửa" nó**.
- **Môi trường không có `pip`, `openpyxl`, LibreOffice, Excel.** File XLSX chỉ
  kiểm được bằng phân tích XML thủ công. Ở Phase 2 đã có **một lỗi loại này lọt
  lưới** (`<pane>` sai vị trí) — XML hợp lệ nhưng Excel từ chối.
- Comment trong file XML/csproj **không được chứa `--`** (`dotnet run --project`
  trong comment làm hỏng cả file).
- Ngữ cảnh `secrets` của GitHub Actions **không dùng được trong `if:` cấp step**.
- **`github.com/tsuowlit/swico` CHƯA TỒN TẠI** — là URL điền tạm có mặt trong
  README, `Directory.Build.props`, manifest winget, `swico.iss`, `EULA.txt`,
  `PRIVACY.md`. Repo chưa có remote nào (`git remote -v` trống), 7 commit chỉ
  nằm trên máy dev này. `git clone` URL đó sẽ báo không tìm thấy.
- Máy dev Linux này **không phải WSL** — máy Windows là máy tách biệt, phải
  chuyển file qua USB/mạng. Đã tạo sẵn `dist/swico-portable.zip` và
  `dist/swico-repo.bundle` cho việc này.
- Bộ test **khẳng định** báo cáo HTML phải chứa `https://tsudev.com`
  (`tests/unittests/Program.cs:174`) — đổi tên miền là phải sửa test.
