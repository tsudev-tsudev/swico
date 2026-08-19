# STATE — Trạng thái sống của dự án

> **File này là nguồn sự thật DUY NHẤT về "đang làm tới đâu".**
> Phiên Claude Code mới **BẮT BUỘC đọc file này đầu tiên**, trước cả README.
> Quy ước cập nhật: `docs/CONTINUITY.md`.

- **Cập nhật lần cuối:** 2026-08-19 (phiên S003)
- **Phiên gần nhất:** S003 — `docs/journal/S003-2026-08-19.md`
- **Giai đoạn:** đã phát hành `v26.8.18.2`; PR winget đang chờ gỡ một nhãn lỗi;
  `26.8.19` đã sẵn sàng trong repo nhưng **chưa gắn tag, chưa phát hành**;
  còn 4 việc chờ người dùng + 1 nhóm việc kỹ thuật

> ## ⚡ VIỆC ĐẦU TIÊN CỦA PHIÊN MỚI
>
> **Mục 3.0 trước đã** — phiên S003 để lại **6 commit chưa push**. Chúng chưa
> qua CI, nên mọi câu "CI xanh" trong tài liệu này đang nói về commit `ff1a9ae`
> của phiên trước, KHÔNG phải trạng thái hiện tại.
>
> Sau đó **xem mục 3.1** — PR winget #419878 đang vướng nhãn
> `Validation-Executable-Error` và bot chưa giải thích nguyên nhân.
>
> Phiên S003 đã chốt **quy ước đặt tên phiên bản** (`docs/VERSIONING.md`) và
> nâng `VersionPrefix` lên `26.8.19`. **Chưa gắn tag `v26.8.19`** — việc phát
> hành là quyết định của người dùng, xem mục 3.6.
>
> Mọi việc của phiên S002 đã khép: code đã push, CI run
> [32201733164](https://github.com/tsudev-tsudev/swico/actions/runs/32201733164)
> **xanh cả hai job**, bước kiểm dòng-theo-bước trên Windows thật đã chạy và
> báo **12 bước, tất cả đều có thời gian riêng**.

---

## 1. Tình trạng kỹ thuật

```
Build     : ✅ 0 cảnh báo (TreatWarningsAsErrors bật) — đo trên máy dev Linux
Test      : ✅ 234 PASS, 0 FAIL — đo trên máy dev Linux
CI        : ⚠️ CHƯA chạy cho 6 commit của phiên S003 (chưa push). Lần xanh gần
            nhất là run 32201733164, ứng với commit `ff1a9ae` của phiên S002.
Release   : ✅ v26.8.18.2 đã phát hành chính thức (Latest)
Repo      : ✅ github.com/tsudev-tsudev/swico — PUBLIC, 44 commit, working tree sạch
SDK       : ✅ ghim 8.0.424 qua global.json (dev và CI dùng CÙNG một SDK)
Windows   : ✅ đã chạy thật, cài thật, dữ liệu đúng, đối chiếu với bản PowerShell cũ xong
Terminal  : 🔄 streaming tiến trình quét ĐÃ VIẾT XONG, phần "dáng vẻ" chờ kiểm bằng mắt
Git       : ⚠️ ĐỨNG TRƯỚC origin/main 6 commit — CHƯA PUSH (xem mục 3.0)
```

## 2. Sản phẩm

| Hạng mục | Giá trị |
|---|---|
| Tên | `tsudev SWICO` |
| Assembly | `swico.exe` |
| Winget ID | `tsudev.SWICO` |
| Phiên bản | trong repo: **26.8.19** · đã phát hành: **26.8.18.2** |
| Đặt tên phát hành | `tsudev-swico-vYY.M.D[.N]` — `docs/VERSIONING.md`, có 25 test |
| Namespace | `Tsudev.Audit.*` — **giữ nguyên**, chi tiết nội bộ |
| Tên miền | `https://tsudev.com` (bộ test khẳng định điều này) |
| Giấy phép | Apache-2.0 |
| Ký số | SignPath Foundation — **đang chờ duyệt** |

---

## 3. VIỆC TIẾP THEO — đọc mục này rồi làm

### 3.0 ⬜ Đẩy 6 commit của phiên S003 lên origin — **LÀM TRƯỚC TIÊN**

Phiên S003 commit đầy đủ nhưng **không push** (đẩy lên kho công khai là việc
hướng ra ngoài, cần người dùng đồng ý). Hệ quả: các commit đó **chưa qua CI**,
nên chưa ai xác nhận chúng xanh trên runner Windows.

```bash
git log --oneline origin/main..HEAD    # xem 6 commit đang chờ
git push origin main                    # sau khi người dùng đồng ý
```

Push xong thì **theo dõi CI** — phiên S003 có thêm hai bước CI mới chưa từng
chạy thật lần nào:

| Bước mới | Ở đâu | Rủi ro nếu hỏng |
|---|---|---|
| `Kiem tra VersionPrefix dung quy uoc dat ten` | `ci.yml`, job Linux | dùng `sed` + `dotnet run --no-build`; nếu sai đường dẫn thì job đỏ |
| `Kiem tra quy uoc dat ten` | `release.yml` | chỉ chạy khi gắn tag, nên **CI xanh KHÔNG chứng minh bước này chạy được** |

Cả hai chỉ được kiểm bằng tay trên Linux, chưa chạy trên runner GitHub.

### 3.1 ⛔ PR winget #419878 — vướng `Validation-Executable-Error`

PR: https://github.com/microsoft/winget-pkgs/pull/419878

**Đã xong ở phiên S002:** ký CLA ✅, và lỗi `Error-Hash-Mismatch` **đã sửa** bằng
commit `49ed455` trên nhánh `tsudev.SWICO-26.8.18.2` (đặt `InstallerSha256` =
`63833A50C758C69D1A466707C682754F647F0A09F6ABED6575FC9310B17EC1CA`, đúng hash
của file `.exe` đang nằm trên release).

**Kết quả pipeline:**

| Bước | Kết quả |
|---|---|
| 01–07, 09, 10 | ✅ pass (`07. Installers Scan` — bước từng đỏ — nay 6m30s pass) |
| **08. Installation Validation** | ⏭️ **skipping** (53m27s) |

Nhãn: `Azure-Pipeline-Passed`, `New-Package`, `Validation-Guide`,
**`Validation-Executable-Error`**.

**CHƯA BIẾT nguyên nhân.** Bot Microsoft chưa đăng bình luận giải thích tính tới
cuối phiên S002. Giả thuyết đáng ngờ nhất — **chưa kiểm chứng** — là installer
chưa được ký số (mục 3.4). Bước 08 là bước cài thử thật trong sandbox Windows.

**Việc cần làm:**

1. Đọc bình luận mới của bot:
   `gh pr view 419878 --repo microsoft/winget-pkgs --json comments`
2. Tra ý nghĩa nhãn tại
   <https://learn.microsoft.com/windows/package-manager/package/repository#pull-request-labels>
3. Trên máy Windows, chạy rồi **báo kết quả thật vào PR** (đừng tick khi chưa chạy):
   ```powershell
   winget validate --manifest <thư-mục-manifest>
   winget install  --manifest <thư-mục-manifest>
   ```
   Manifest lấy từ `winget-manifest-26.8.18.2.zip` đính kèm bản phát hành —
   artifact này mang hash ĐÚNG, dùng thẳng được.

> ⛔ **KHÔNG chạy lại `release.yml` cho `v26.8.18.2`.** Sẽ sinh file setup mới,
> ghi đè asset trên release, và làm hash trong PR sai trở lại. Đây đúng là cái
> bẫy đã gây ra `Error-Hash-Mismatch` — xem mục 4.4.

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
| **Giải nén bản portable `.zip` rồi chạy** | **KHÔNG** hiện hộp thoại; chỉ rõ đường tải bản portable mới; mã thoát **30** |
| **Bản đã cài, kiểm `unins000.exe`** | Có tệp đó trong thư mục cài → nhận đúng là bản đã cài, vẫn hiện hộp thoại |
| Đo lại tốc độ cài | Setup nay 24,9 MB thay vì 29,8 MB |

> Hai dòng in đậm là **mới ở phiên S003** và là hai dòng đáng kiểm nhất: chúng
> kiểm chính giả định "Inno Setup luôn đặt `unins000.exe` cạnh ứng dụng". Giả
> định đó đọc từ tài liệu Inno Setup, **chưa ai xác nhận trên máy thật**. Nếu
> sai, bản đã cài sẽ bị coi nhầm là portable — phiền chứ không hỏng, nhưng vẫn
> phải biết.

### 3.3 ⛔ Kiểm bằng mắt phần hiển thị tiến trình — **cần người dùng**

Phiên S002 đã thêm streaming tiến trình quét: mỗi bước một dòng, có con quay,
Ctrl+C dừng ngay và thoát mã `130`. Phần **logic** đã có 24 test (Linux) và CI
Windows kiểm số dòng-theo-bước. Phần **dáng vẻ** thì không test tự động được.

Chạy 9 mục ở `docs/WINDOWS-VERIFICATION.md` mục **H**. Quan trọng nhất:

| Mục | Việc | Vì sao quan trọng nhất |
|---|---|---|
| H4 | Sau Ctrl+C, gõ tiếp một lệnh | Con trỏ bị ẩn mà không hiện lại = terminal hỏng sau khi công cụ đã thoát |
| H5 | Sau Ctrl+C, mở Task Manager | Còn sót `sfc.exe`/`DISM.exe` = máy vẫn gồng dù đã "thoát" |

### 3.4 ⛔ Sau khi SignPath duyệt — **cần người dùng**

1. Thêm secret `SIGNPATH_API_TOKEN` và variable `SIGNPATH_ORGANIZATION_ID`.
2. Phía SignPath: `project-slug=swico`, `signing-policy-slug=release-signing`,
   `artifact-configuration-slug` = `exe` và `installer`.
3. Chạy lại `release.yml` — các bước ký **tự kích hoạt** nhờ điều kiện
   `SIGNPATH_CONFIGURED`, gồm cả bước xác minh chữ ký **và dấu thời gian**.
4. Nộp manifest winget mới cho bản đã ký.

Chi tiết: `docs/SIGNING.md`.

### 3.5 ⬜ Việc kỹ thuật còn lại — làm được không cần người dùng

Xếp theo giá trị giảm dần:

1. **Chuyển bộ test sang xUnit.** Hiện là bộ tự chế đếm pass/fail thủ công
   (`tests/unittests/Program.cs`, ~800 dòng top-level statements). 197 ca trong
   một file là quá nhiều cho một file. xUnit cho báo lỗi tử tế, chạy song song,
   tích hợp CI chuẩn.
2. **Tách file gộp nhiều class thành file riêng** (`InventoryCollectors.cs` 4 class,
   `LicenseCollectors.cs` 5 class). Đã hoãn từ Phase 1 vì lúc đó chưa có test;
   nay có 234 test làm lưới an toàn.
3. **Logging có cấu trúc** thay `Console.WriteLine` rải rác, kèm tuỳ chọn ghi log
   ra file cho tình huống hỗ trợ từ xa.
4. **`--json-only`** cho tích hợp máy-đọc-máy.
4b. **Căn cột cho dòng tiến trình** (nhỏ, tuỳ chọn). Hiện nhãn và thời gian
   không thẳng cột: `✓ CPU   1.0 s` cạnh `✓ Tổng quan thiết bị   1.3 s`. Đệm
   nhãn về một bề rộng cố định trong `ConsoleProgressReporter.Finish()` là xong.
   README đang chép đúng đầu ra thật nên **không sai lệch**; đây thuần tuý là
   thẩm mỹ.
5. **NativeAOT** — cân nhắc, nhưng đọc mục 4 trước: cắt tỉa đã hỏng, NativeAOT
   gần như chắc chắn cũng hỏng vì cùng nguyên nhân (WMI qua COM + phản chiếu).

### 3.6 ⬜ Phát hành `26.8.19` — sẵn sàng, chờ quyết định

`Directory.Build.props` đã ghi `26.8.19`, nhưng **chưa gắn tag và chưa phát
hành**. Repo đang ở trạng thái build được, test xanh, chỉ thiếu một quyết định.

Khi muốn phát hành:

```bash
git tag v26.8.19
git push origin v26.8.19
```

`release.yml` sẽ tự kiểm quy ước đặt tên → chạy test → publish → đóng gói → tạo
bản phát hành **nháp** mang tên `tsudev-swico-v26.8.19`.

> ⛔ Việc này **không đụng gì tới `v26.8.18.2`**, nên PR winget #419878 (mục 3.1)
> không bị ảnh hưởng. Đừng chạy lại `release.yml` cho `26.8.18.2` — xem mục 4.4.

Nếu hôm nay đã phát hành `26.8.19` rồi mà cần phát hành lại **trong cùng ngày**,
số hiệu tiếp theo là `26.8.19.2` (không phải `.1`) — `docs/VERSIONING.md` mục 2.1.

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
- **CHẠY LẠI `release.yml` cho một phiên bản ĐÃ NỘP MANIFEST là làm hỏng manifest
  đó trong im lặng.** Inno Setup đóng gói lại ra file **khác byte** (dấu thời gian
  bên trong installer) → hash mới → asset trên release bị ghi đè → manifest đã nộp
  bỗng trỏ tới một hash không còn tồn tại. **Đã xảy ra thật với PR #419878**: bản
  `v26.8.18.2` chạy `release.yml` ba lần (16:29, 16:33, 16:39); manifest nộp lúc
  16:41 mang hash của bản build 16:33, còn asset bị lần chạy 16:39 ghi đè lúc
  16:43. Quy tắc: **nộp manifest SAU KHI asset trên release đã ở trạng thái cuối
  cùng**, và trước khi nộp luôn tải file từ chính `InstallerUrl` rồi tính lại hash.
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

#### ⚠️ MỘT MÂU THUẪN CHƯA GIẢI QUYẾT (phát hiện ở phiên S002)

`README.md` mục "Giới hạn cần biết" vẫn viết lớp `Tsudev.Audit.Windows`
**"chưa từng chạy trên Windows thật"**, trong khi chính file này (mục 1) ghi
Windows đã chạy thật, cài thật, đối chiếu xong từ phiên S001.

**Một trong hai đang sai.** Phiên S002 cố ý KHÔNG tự sửa: sửa nhầm chiều thì
biến một tài liệu sai thành một tài liệu sai theo kiểu khác, khó phát hiện hơn.
Cần người dùng xác nhận cái nào đúng rồi mới sửa.

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
| `docs/journal/S002-2026-08-19.md` | Phiên S002: sửa hash PR winget, streaming tiến trình quét |
| `docs/journal/S003-2026-08-19.md` | Phiên S003: quy ước đặt tên phiên bản, cổng chặn trong CI/CD |
| `docs/SIGNING.md` | Ký số qua SignPath |
| `docs/WINGET.md` | Nộp winget + cách dùng ngay |
| `docs/UPDATES.md` | Chức năng tự cập nhật |
| `docs/VERSIONING.md` | **Quy ước đặt tên phiên bản** — thực thi bằng mã, không chỉ bằng lời |
| `docs/DETECTION-RULES.md` | Bộ luật phát hiện |
| `docs/WINDOWS-VERIFICATION.md` | Kịch bản kiểm chứng trên Windows |
