# STATE — Trạng thái sống của dự án

> **File này là nguồn sự thật DUY NHẤT về "đang làm tới đâu".**
> Phiên Claude Code mới **BẮT BUỘC đọc file này đầu tiên**, trước cả README.
> Quy ước cập nhật: `docs/CONTINUITY.md`.

- **Cập nhật lần cuối:** 2026-08-18 (cuối phiên S001)
- **Phiên gần nhất:** S001 — `docs/journal/S001-2026-08-18.md`
- **Giai đoạn:** đã phát hành `v26.8.18.2`; còn 3 việc chờ người dùng + 1 nhóm việc kỹ thuật

---

## 1. Tình trạng kỹ thuật

```
Build     : ✅ 0 cảnh báo (TreatWarningsAsErrors đang bật)
Test      : ✅ 173 PASS, 0 FAIL
CI        : ✅ xanh — gồm QUÉT THẬT trên Windows runner + kiểm tra chất lượng dữ liệu
Release   : ✅ v26.8.18.2 đã phát hành chính thức (Latest)
Repo      : ✅ github.com/tsudev-tsudev/swico — PUBLIC, 33 commit, working tree sạch
SDK       : ✅ ghim 8.0.424 qua global.json (dev và CI dùng CÙNG một SDK)
Windows   : ✅ đã chạy thật, cài thật, dữ liệu đúng, đối chiếu với bản PowerShell cũ xong
```

## 2. Sản phẩm

| Hạng mục | Giá trị |
|---|---|
| Tên | `tsudev SWICO` |
| Assembly | `swico.exe` |
| Winget ID | `tsudev.SWICO` |
| Phiên bản | **26.8.18.2** — CalVer `yy.M.d[.N]` (`Directory.Build.props`) |
| Namespace | `Tsudev.Audit.*` — **giữ nguyên**, chi tiết nội bộ |
| Tên miền | `https://tsudev.com` (bộ test khẳng định điều này) |
| Giấy phép | Apache-2.0 |
| Ký số | SignPath Foundation — **đang chờ duyệt** |

---

## 3. VIỆC TIẾP THEO — đọc mục này rồi làm

### 3.1 ⛔ Hoàn tất PR winget #419878 — **cần người dùng**

PR đã mở: https://github.com/microsoft/winget-pkgs/pull/419878

**QUAN TRỌNG — tôi đã ghi RÕ TRONG PR là hai việc sau CHƯA làm.** Đừng tick chúng
mà chưa thực sự chạy:

1. **Ký CLA** khi bot của Microsoft yêu cầu trên PR.
2. Trên máy Windows, chạy rồi **báo kết quả vào PR**:
   ```powershell
   winget validate --manifest <thư-mục-manifest>
   winget install  --manifest <thư-mục-manifest>
   ```
   Manifest lấy từ `winget-manifest-26.8.18.2.zip` đính kèm bản phát hành.

Đã kiểm chứng sẵn: hash khớp file đã phát hành, URL tải được HTTP 200 đúng dung
lượng, manifest đúng lược đồ 1.6.

`winget install tsudev.SWICO` chỉ chạy được **sau khi PR được hợp nhất**. Trong
lúc chờ, dùng `packaging/tools/winget-local-install.ps1`.

### 3.2 ⛔ Kiểm chứng chức năng tự cập nhật — **cần người dùng**

`v26.8.18.2` đã phát hành nên bản **26.8.18 đang cài trên máy** sẽ phát hiện được.
Chạy nó và kiểm:

| Việc | Kỳ vọng |
|---|---|
| Chạy bản 26.8.18 đã cài | Hộp thoại **một nút "Cập nhật"** → tải → đối chiếu SHA-256 → chạy installer |
| `swico.exe --silent` | Mã thoát **30**, **KHÔNG** hiện hộp thoại |
| `swico.exe --no-update-check` | Quét bình thường, không kết nối mạng |
| Đo lại tốc độ cài | Setup nay 24,9 MB thay vì 29,8 MB |

### 3.3 ⛔ Sau khi SignPath duyệt — **cần người dùng**

1. Thêm secret `SIGNPATH_API_TOKEN` và variable `SIGNPATH_ORGANIZATION_ID`.
2. Phía SignPath: `project-slug=swico`, `signing-policy-slug=release-signing`,
   `artifact-configuration-slug` = `exe` và `installer`.
3. Chạy lại `release.yml` — các bước ký **tự kích hoạt** nhờ điều kiện
   `SIGNPATH_CONFIGURED`, gồm cả bước xác minh chữ ký **và dấu thời gian**.
4. Nộp manifest winget mới cho bản đã ký.

Chi tiết: `docs/SIGNING.md`.

### 3.4 ⬜ Việc kỹ thuật còn lại — làm được không cần người dùng

Xếp theo giá trị giảm dần:

1. **Chuyển bộ test sang xUnit.** Hiện là bộ tự chế đếm pass/fail thủ công
   (`tests/unittests/Program.cs`, ~800 dòng top-level statements). 173 ca trong
   một file là quá nhiều cho một file. xUnit cho báo lỗi tử tế, chạy song song,
   tích hợp CI chuẩn.
2. **Tách file gộp nhiều class thành file riêng** (`InventoryCollectors.cs` 4 class,
   `LicenseCollectors.cs` 5 class). Đã hoãn từ Phase 1 vì lúc đó chưa có test;
   nay có 173 test làm lưới an toàn.
3. **Logging có cấu trúc** thay `Console.WriteLine` rải rác, kèm tuỳ chọn ghi log
   ra file cho tình huống hỗ trợ từ xa.
4. **`--json-only`** cho tích hợp máy-đọc-máy.
5. **NativeAOT** — cân nhắc, nhưng đọc mục 4 trước: cắt tỉa đã hỏng, NativeAOT
   gần như chắc chắn cũng hỏng vì cùng nguyên nhân (WMI qua COM + phản chiếu).

---

## 4. CẠM BẪY ĐÃ BIẾT — đọc để khỏi vấp lại

### 4.1 Về đóng gói và hiệu năng

- **KHÔNG bật `PublishTrimmed`.** Đã thử: file setup xuống 9,6 MB nhưng **làm mất
  dữ liệu WMI âm thầm** (11 dòng + `0 CPU` thay vì 15 dòng + dữ liệu thật). Vẫn
  hỏng dù đã đặt `TrimmerRootAssembly` cho `System.Management`. Báo cáo vẫn sinh
  ra, nhìn bình thường, nhưng thiếu dữ liệu.
- **NativeAOT gần như chắc chắn hỏng cùng lý do.** Nếu muốn thử, phải dùng cách
  đã dùng cho cắt tỉa: thêm bước publish thứ hai vào `ci.yml` rồi **so sánh số
  dòng và tóm tắt phần cứng của hai bản trên CÙNG một máy**. Đó là cách duy nhất
  phát hiện được.
- **`EnableCompressionInSingleFile` phải là `false`.** Bật lên làm file setup
  **to hơn** (nén hai lần là phản tác dụng).
- Đổi lại: `swico.exe` tải trực tiếp to hơn gấp đôi (75,8 MB). Có chủ đích, đã
  ghi rõ trong README.

### 4.2 Về môi trường dev

- Máy dev là **Linux**. `dotnet` **không có trong PATH mặc định**:
  `export PATH="$HOME/.dotnet:$PATH"`.
- **`global.json` ghim SDK 8.0.424 — đừng xoá.** Runner GitHub có sẵn .NET 10 và
  `dotnet build` luôn chọn bản mới nhất nếu không ghim (đã từng làm CI đỏ).
- **Không có `pip`, `openpyxl`, LibreOffice, Excel, `winget`, Inno Setup.** Mọi
  thứ cần Windows phải kiểm chứng qua CI hoặc nhờ người dùng.
- `--no-incremental` **không phải** tham số của `dotnet publish` (chỉ có ở `build`).

### 4.3 Về kiến trúc

- **Logic thuần phải nằm trong Core**, không phải lớp adapter — nếu không thì bộ
  test chạy trên Linux không với tới được. Đã vấp **hai lần** trong phiên S001:
  `CliOptions` (README từng tuyên bố "16/16 test" khi không có test nào), và
  `GitHubReleaseParser`/`ChecksumFile`.
- **Mọi mã chạm mạng nằm gọn trong `src/Tsudev.Audit.Windows/UpdateAdapters.cs`.**
  Giữ nguyên tính chất "một file duy nhất" đó — `PRIVACY.md` mời người dùng tự
  kiểm chứng bằng cách đọc đúng file đó.
- **Mọi chỗ đọc/ghi JSON đi qua `Core/Serialization/AuditJson.cs`** (mã sinh lúc
  biên dịch). Đừng gọi `JsonSerializer` trực tiếp ở nơi khác.
- Bộ luật phát hiện là **dữ liệu có phiên bản** (`Core/Rules/`), không phải mã.
  File ngoài **luôn thắng** bộ luật đóng kèm → có cảnh báo lệch phiên bản.

### 4.4 Về phát hành

- **KHÔNG BAO GIỜ tải tệp từ `dist/` lên GitHub Release.** Đã từng xảy ra: một
  `swico-portable.zip` cục bộ chứa binary **cũ hơn** và không có trong
  `SHA256SUMS.txt` bị tải lên bản `v26.8.18`.
- **Hai release cùng một tag** thì `gh release delete <tag>` **nguy hiểm** — có
  thể xoá nhầm bản đã phát hành. Phải xoá theo **ID**:
  `gh api repos/OWNER/REPO/releases` lấy id rồi `gh api -X DELETE .../releases/<id>`.
- **Manifest winget KHÔNG cam kết sẵn trong repo** — chỉ có template. Hash chỉ
  biết sau khi đóng gói và ký, nên manifest cam kết sẵn luôn mang hash sai.
- **Logo và biến thể** sinh bằng `packaging/tools/make-assets.py`. Sửa
  `assets/tsudev-logo.png` xong phải chạy lại script.

### 4.5 Về tính trung thực của tài liệu

Trong phiên S001, **bốn lần** tài liệu hứa thứ chưa tồn tại — đây là lớp lỗi hay
lặp lại nhất, cần chủ động chống:

| Lần | Nội dung sai |
|---|---|
| 1 | README mô tả solution 4 project + "54 test xanh" khi dự án **không build được** |
| 2 | Hướng dẫn `git clone` một URL repo **chưa tạo** |
| 3 | "CLI parse tham số: 16/16 test" khi **không có test CLI nào** |
| 4 | `winget install tsudev.SWICO` khi gói **chưa nộp** lên kho cộng đồng |

Và một lần trong PR gửi ra ngoài: **tự tick các ô** "đã ký CLA", "đã chạy
`winget validate`" trong PR gửi `microsoft/winget-pkgs` khi chưa làm. Đã sửa.

**Quy tắc rút ra:** viết tài liệu theo trạng thái **thật**, không theo trạng thái
mong muốn. Mọi ô tick trong mẫu PR là một lời khai — tick một ô chưa làm là nói
dối với người sẽ đọc nó.

### 4.6 Bản ghi lịch sử — cố ý KHÔNG sửa

`docs/journal/` và `docs/DECISIONS.md` vẫn dùng tên cũ **`tsuowlit`** và URL cũ.
Đó là bản ghi những gì đã diễn ra; sửa chúng là làm sai sự thật. **Đừng "sửa cho
đồng bộ".**

---

## 5. Bản đồ tài liệu

| File | Nội dung |
|---|---|
| `docs/STATE.md` | **File này** — đọc đầu tiên |
| `docs/CONTINUITY.md` | Giao thức nối tiếp phiên, môi trường dev |
| `docs/PLAN.md` | Lộ trình theo giai đoạn |
| `docs/DECISIONS.md` | Đối chiếu với artifact kế hoạch, các quyết định đã chốt |
| `docs/journal/S001-2026-08-18.md` | Toàn bộ diễn biến phiên S001, kèm **lý do** từng quyết định |
| `docs/SIGNING.md` | Ký số qua SignPath |
| `docs/WINGET.md` | Nộp winget + cách dùng ngay |
| `docs/UPDATES.md` | Chức năng tự cập nhật |
| `docs/DETECTION-RULES.md` | Bộ luật phát hiện |
| `docs/WINDOWS-VERIFICATION.md` | Kịch bản kiểm chứng trên Windows |
