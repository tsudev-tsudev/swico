# STATE — Trạng thái sống của dự án

> **File này là nguồn sự thật DUY NHẤT về "đang làm tới đâu".**
> Phiên mới đọc `AGENTS.md` (quy ước bắt buộc) rồi tới **file này** — trước cả README.
> Thứ tự đọc đầy đủ: `docs/CONTINUITY.md` mục 0.

- **Cập nhật lần cuối:** 2026-08-20 (phiên S004)
- **Phiên gần nhất:** S004 — `docs/journal/S004-2026-08-20.md`
- **Quy ước bắt buộc:** `AGENTS.md` — **đọc trước cả file này**
- **Giai đoạn:** đã phát hành `v26.8.18.2`; PR winget đang chờ gỡ một nhãn lỗi;
  `26.8.1901` đã sẵn sàng trong repo nhưng **chưa gắn tag, chưa phát hành**;
  còn 4 việc chờ người dùng + 1 nhóm việc kỹ thuật

> ## ⚡ VIỆC ĐẦU TIÊN CỦA PHIÊN MỚI
>
> **Đọc `AGENTS.md` trước, rồi tới đây.** Bàn giao gần nhất:
> `logs/handover/20260820-02_khep-phien-S004.md`.
>
> **Mục 3.0 trước đã** — phiên S004 để lại **4 commit chưa push**. Chúng chưa qua
> CI, nên mọi câu "CI xanh" dưới đây đang nói về commit `200c0fd` của phiên S003,
> KHÔNG phải trạng thái hiện tại.
>
> Phiên S004 đã **đổi quy ước đặt tên phiên bản** sang `docs/DESIGN_SYSTEM.md`
> mục 6 (quyết định **D-S004-1**): `VersionPrefix` nay là **`26.8.1901`**, file
> cài đặt là `tsudev-swico_26.8.1901_x64-setup.exe`. **Chưa gắn tag** — việc phát
> hành là quyết định của người dùng, xem mục 3.6, và **đọc mục 4.7 trước**.
>
> Sau đó **xem mục 3.1** — PR winget #419878 đang vướng nhãn
> `Validation-Executable-Error` và bot chưa giải thích nguyên nhân.
>
> Mọi việc của phiên S003 đã khép: code **đã push**, CI run
> [32224326731](https://github.com/tsudev-tsudev/swico/actions/runs/32224326731)
> **xanh** cho commit `200c0fd`.

---

## 1. Tình trạng kỹ thuật

```
Build     : ✅ 0 cảnh báo (TreatWarningsAsErrors bật) — đo trên máy dev Linux
Test      : ✅ 276 PASS, 0 FAIL — đo trên máy dev Linux
CI        : ✅ run 32360493509 XANH cả hai job (Linux + smoke test Windows) cho
            commit `ce24691`. Đây là lần đầu luật đặt tên MỚI + `VersionPrefix`
            `26.8.1901` chạy trên runner GitHub. Bước `Kiem tra quy uoc dat ten`
            của `release.yml` vẫn CHƯA từng chạy (chỉ kích hoạt khi gắn tag).
Release   : ✅ v26.8.18.2 đã phát hành chính thức (Latest)
Repo      : ✅ github.com/tsudev-tsudev/swico — PUBLIC, working tree sạch
SDK       : ✅ ghim 8.0.424 qua global.json (dev và CI dùng CÙNG một SDK)
Windows   : ✅ đã chạy thật, cài thật, dữ liệu đúng, đối chiếu với bản PowerShell cũ xong
Terminal  : 🔄 streaming tiến trình quét ĐÃ VIẾT XONG, phần "dáng vẻ" chờ kiểm bằng mắt
Git       : ✅ ngang bằng origin/main (đếm bằng `git log --oneline origin/main..HEAD`)
```

## 2. Sản phẩm

| Hạng mục | Giá trị |
|---|---|
| Tên | `tsudev SWICO` |
| Assembly | `swico.exe` |
| Winget ID | `tsudev.SWICO` |
| Phiên bản | trong repo: **26.8.1901** · đã phát hành: **26.8.18.2** (dạng cũ) |
| Đặt tên phát hành | `tsudev-swico_YY.M.DDNN_x64-setup.exe` — `docs/VERSIONING.md`, có 33 test |
| Namespace | `Tsudev.Audit.*` — **giữ nguyên**, chi tiết nội bộ |
| Tên miền | `https://tsudev.com` (bộ test khẳng định điều này) |
| Giấy phép | Apache-2.0 |
| Ký số | SignPath Foundation — **đang chờ duyệt** |

---

## 3. VIỆC TIẾP THEO — đọc mục này rồi làm

### 3.0 ✅ ĐÃ XONG — commit S004 đã push, CI xanh

Ngày 20/08/2026, phiên S005 đẩy 4 commit của S004 lên `origin/main` sau khi
được chủ project đồng ý. `origin/main` = `ce24691`.

CI run **32360493509** — **xanh cả hai job**:

| Job | Kết quả |
|---|---|
| Build & test (Linux) | ✅ 25s |
| Smoke test (Windows) | ✅ 1m57s |

**Điều này chứng minh được gì:** bước `Kiem tra VersionPrefix dung quy uoc dat ten`
trong `ci.yml` chạy được với luật đặt tên **mới** và `VersionPrefix` **mới**
(`26.8.1901`) trên runner GitHub thật — trước đó nó mới chỉ xanh với luật cũ.

**Điều này KHÔNG chứng minh được:** bước `Kiem tra quy uoc dat ten` trong
`release.yml` vẫn **chưa từng chạy thật**, vì nó chỉ kích hoạt khi gắn tag.
Lần phát hành đầu tiên theo quy ước mới vẫn là lần chạy đầu tiên của bước đó.

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

**Thêm ở phiên S004 — hai điều CHỈ kiểm được trên Windows thật:**

| Việc | Kỳ vọng | Vì sao chưa ai biết |
|---|---|---|
| Inno Setup biên dịch được với `VersionInfoVersion=26.8.1901` | `ISCC` không báo lỗi, file ra đúng tên `tsudev-swico_26.8.1901_x64-setup.exe` | Máy dev Linux **không có Inno Setup**; chuỗi 4 chữ số ở thành phần thứ ba là mới |
| Thử `VersionInfoVersion=26.9.0901` (ngày một chữ số) | Không báo lỗi số 0 đứng đầu | Đây là dạng **chưa từng đưa qua `ISCC`** lần nào |

> Điều thứ hai là chỗ rủi ro còn lại của cả việc đổi quy ước. Phía .NET đã **đo
> xong** (xem `docs/VERSIONING.md` mục 4), phía Inno Setup thì **chưa**.

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
   (`tests/unittests/Program.cs`, **1203 dòng** top-level statements). 276 ca
   trong một file là quá nhiều cho một file. xUnit cho báo lỗi tử tế, chạy song song,
   tích hợp CI chuẩn.
2. **Tách file gộp nhiều class thành file riêng** (`InventoryCollectors.cs` 4 class,
   `LicenseCollectors.cs` 5 class). Đã hoãn từ Phase 1 vì lúc đó chưa có test;
   nay có 276 test làm lưới an toàn.
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

### 3.6 ⬜ Phát hành `26.8.1901` — sẵn sàng, chờ quyết định

`Directory.Build.props` đã ghi `26.8.1901`, nhưng **chưa gắn tag và chưa phát
hành**. Repo đang ở trạng thái build được, test xanh, chỉ thiếu một quyết định.

Khi muốn phát hành:

```bash
git tag v26.8.1901
git push origin v26.8.1901
```

`release.yml` sẽ tự kiểm quy ước đặt tên → chạy test → publish → đóng gói → tạo
bản phát hành **nháp** mang tên `tsudev-swico_26.8.1901`.

> ⚠️ **Cân nhắc bản cầu nối trước.** Phát hành thẳng `26.8.1901` thì hai bản đã
> cài trên máy người dùng (`26.8.18`, `26.8.18.2`) **không đọc được** số hiệu đó
> và mất đường cập nhật — xem mục 4.7. Nếu điều đó quan trọng, phát hành
> `26.8.20` (dạng cũ) làm cầu nối **trước**, rồi mới sang dạng mới.

> ⛔ Việc này **không đụng gì tới `v26.8.18.2`**, nên PR winget #419878 (mục 3.1)
> không bị ảnh hưởng. Đừng chạy lại `release.yml` cho `26.8.18.2` — xem mục 4.4.

Nếu hôm nay đã phát hành rồi mà cần phát hành lại **trong cùng ngày**, số hiệu
tiếp theo là `26.8.1902` — `docs/VERSIONING.md` mục 6.

---

### QU-5 ✅ ĐÃ XONG — định dạng ngày giờ hiển thị về đúng một luật

Trước phiên S005, báo cáo in ra **ba** định dạng khác nhau cho cùng một khái
niệm — `dd/MM/yyyy HH:mm`, `dd/MM/yyyy HH:mm:ss`, `yyyy-MM-dd HH:mm:ss` — và
**không có test nào chặn**. Quy ước (`docs/DESIGN_SYSTEM.md`) chỉ có một:

```
Ngày     : DD/MM/YYYY          ví dụ 01/02/2027
Ngày giờ : HH:mm DD/MM/YYYY    ví dụ 14:30 19/08/2026
```

Luật nay nằm ở **một chỗ duy nhất**: `Core/Reports/DateDisplay.cs`. Bảy chỗ in
ngày (2 renderer, 2 collector, CLI) đều gọi vào đó. 12 test ở mục 18 khoá lại.

**Hai điều cố ý KHÔNG thống nhất — đừng "dọn cho gọn":**

| Chỗ | Định dạng | Vì sao |
|---|---|---|
| Tên file/thư mục (`FileNaming`) | `yyyyMMdd_HHmmss` | Phải **sắp xếp được theo thứ tự chữ cái**. `DD/MM/YYYY` thì `01/12` đứng trước `02/01`, và `/` trong tên file thành đường dẫn thư mục. |
| File `.json` đi kèm báo cáo | ISO 8601 | Máy đọc, `DashboardBuilder` dựng lại trang tổng hợp từ đó. Đổi sang chữ người đọc là hỏng trang tổng hợp. |

**Culture bị khoá `InvariantCulture`** ở cả hai lớp, cùng một lý do đã ghi sẵn ở
`FileNaming`: máy đặt ngôn ngữ Thái dùng Phật lịch → `yyyy` ra **2569** thay vì
2026; một số ngôn ngữ đổi luôn dấu `/` thành `.`. Có test cho cả hai ca (`th-TH`,
`de-DE`) vì báo cáo gửi cho kế toán phải giống nhau trên mọi máy.

**Bỏ giây khi hiển thị.** Quy ước dừng ở phút. Giây không mất hẳn: tên file vẫn
giữ `HHmmss`, nên hai lần quét cách nhau vài giây vẫn phân biệt được bằng tên.

**Ngày cài đặt phần mềm** đọc từ registry (`FormatInstallDate`) cũng đổi theo:
`2024-01-05` → `05/01/2024`. Hàm này **không** dùng `DateTime.ParseExact` — máy
thật có registry ghi ngày không tồn tại (`20240230`); cắt chuỗi thì người đọc
vẫn thấy được registry ghi gì, `ParseExact` thì ném.

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

### 4.7 ⛔ Hai bản đã phát hành KHÔNG đọc được số hiệu dạng mới

Ngày 20/08/2026, quyết định **D-S004-1** đổi quy ước đặt tên sang
`docs/DESIGN_SYSTEM.md` mục 6: `26.8.19` → **`26.8.1901`**,
`swico-setup-26.8.19.exe` → **`tsudev-swico_26.8.1901_x64-setup.exe`**.

Mã mới đọc được **cả hai** dạng (`26.8.18` ≡ `26.8.1801`) và
`GitHubReleaseParser` nhận **cả hai** dạng tên tệp đính kèm. **Chiều ngược lại
thì không sửa được bằng mã:**

> `swico.exe` của `26.8.18` và `26.8.18.2` **đã nằm trên máy người dùng** với bộ
> đọc phiên bản **cũ** biên dịch sẵn bên trong. Bộ đọc đó gặp tag `v26.8.1901`
> sẽ thấy ngày `1901 > 31` và **không đọc được**.

Hệ quả cụ thể, đã truy theo mã (`UpdateChecker`): hai bản đó rơi vào nhánh
`CheckFailed` → **vẫn quét bình thường kèm ghi chú**, không sập, không chặn —
nhưng **mất khả năng cập nhật bắt buộc**.

**Cách gỡ, nếu muốn:** phát hành **một bản cầu nối** mang số hiệu dạng **cũ**
(ví dụ tag `v26.8.20`, `VersionPrefix` `26.8.20`) chứa exe đã có bộ đọc hai dạng.
Máy đang chạy bản cũ đọc được tag đó → tự cập nhật → từ đó hiểu được cả dạng mới.
Sau bản cầu nối, mọi bản phát hành dùng dạng mới.

> ⚠️ Bản cầu nối đòi **tạm nới cổng chặn** `ReleaseName.Validate` (nó từ chối
> dạng cũ). Đừng nới bằng cách sửa `Validate` — thêm một biến môi trường
> `ALLOW_LEGACY_VERSION` cho đúng một lần chạy, rồi bỏ đi.

Chi tiết đầy đủ: `docs/VERSIONING.md` mục 5.

---

## 5. Bản đồ tài liệu

### Bộ quy ước `tsudev-conventions` v1.0.0 (áp dụng 20/08/2026)

| File | Nội dung |
|---|---|
| `AGENTS.md` | **Quy ước bắt buộc — đọc TRƯỚC file này.** Bất khả xâm phạm |
| `docs/DESIGN_SYSTEM.md` | Hệ màu 3 chế độ, typography, component. Bất khả xâm phạm |
| `docs/PROJECT_STRUCTURE.md` | Cây thư mục chuẩn hệ sinh thái. Bất khả xâm phạm |
| `docs/ARCHITECTURE.md` | Kiến trúc **của riêng repo này** + vì sao `src/` khác cây mẫu |
| `docs/templates/HANDOVER.md` | Mẫu phiếu bàn giao |
| `docs/CONVENTIONS-README.md` | README gốc của bộ quy ước |
| `tokens/design-tokens.json` · `tokens/tokens.css` | Nguồn giá trị giao diện duy nhất |
| `logs/STATE.md` | Điều phối agent — **không** phải trạng thái sản phẩm |
| `logs/LOCKS.md` | Khóa file, kiểm tra TRƯỚC khi sửa bất kỳ file nào |
| `logs/handover/` | Phiếu bàn giao |

### Tài liệu của dự án

| File | Nội dung |
|---|---|
| `docs/STATE.md` | **File này** — nguồn sự thật về trạng thái **sản phẩm** |
| `docs/CONTINUITY.md` | Giao thức nối tiếp phiên, môi trường dev |
| `docs/PLAN.md` | Lộ trình theo giai đoạn |
| `docs/DECISIONS.md` | Đối chiếu với artifact kế hoạch, các quyết định đã chốt |
| `docs/journal/S001-2026-08-18.md` | Toàn bộ diễn biến phiên S001, kèm **lý do** từng quyết định |
| `docs/journal/S002-2026-08-19.md` | Phiên S002: sửa hash PR winget, streaming tiến trình quét |
| `docs/journal/S003-2026-08-19.md` | Phiên S003: quy ước đặt tên phiên bản, cổng chặn trong CI/CD |
| `docs/journal/S004-2026-08-20.md` | Phiên S004: áp bộ quy ước `tsudev-conventions` v1.0.0 |
| `docs/SIGNING.md` | Ký số qua SignPath |
| `docs/WINGET.md` | Nộp winget + cách dùng ngay |
| `docs/UPDATES.md` | Chức năng tự cập nhật |
| `docs/VERSIONING.md` | **Quy ước đặt tên phiên bản** — thực thi bằng mã, không chỉ bằng lời |
| `docs/DETECTION-RULES.md` | Bộ luật phát hiện |
| `docs/WINDOWS-VERIFICATION.md` | Kịch bản kiểm chứng trên Windows |
