# Nhật ký thay đổi

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/);
đánh số phiên bản theo **CalVer** — quy ước đầy đủ ở
[`docs/VERSIONING.md`](docs/VERSIONING.md).

## [Chưa phát hành]

### Thay đổi

- **⚠️ ĐỔI QUY ƯỚC ĐẶT TÊN PHIÊN BẢN** sang chuẩn chung của hệ sinh thái tsudev
  ([`docs/DESIGN_SYSTEM.md`](docs/DESIGN_SYSTEM.md) mục 6). Quyết định D-S004-1,
  20/08/2026.

  | | Trước | Nay |
  |---|---|---|
  | Chuỗi phiên bản | `26.8.19`, `26.8.19.2` | `26.8.1901`, `26.8.1902` |
  | File cài đặt | `swico-setup-26.8.19.exe` | `tsudev-swico_26.8.1901_x64-setup.exe` |
  | Bản portable | `swico-portable-26.8.19.zip` | `tsudev-swico_26.8.1901_x64-portable.zip` |
  | Tên bản phát hành | `tsudev-swico-v26.8.19` | `tsudev-swico_26.8.1901` |

  - Ngày (`DD`) và số thứ tự trong ngày (`NN`) đều **đệm đủ hai chữ số**. Thành
    phần thứ ba được đọc bằng phép chia cho 100, nên bỏ đệm đi thì ngày 9 bản 1
    (`26.9.91`) sẽ đọc ngược lại thành **ngày 0 bản 91**.
  - Thứ tự so sánh vẫn đúng ở mọi nơi: `1901 < 1902 < 2001`.
  - **Đọc được cả dạng cũ.** `26.8.18` ≡ `26.8.1801` và `26.8.18.2` ≡ `26.8.1802`;
    `GitHubReleaseParser` nhận **cả hai** dạng tên tệp đính kèm. Bỏ dạng cũ đi thì
    một bản phát hành cũ sẽ bị coi là "không có file cài đặt" — hỏng **im lặng**.
  - **Giới hạn không sửa được bằng mã:** `swico.exe` của `26.8.18` và `26.8.18.2`
    đã nằm trên máy người dùng với bộ đọc **cũ** biên dịch sẵn bên trong, nên
    chúng không đọc được `26.8.1901`. Hai bản đó **vẫn quét bình thường** nhưng
    mất khả năng cập nhật bắt buộc. Cách gỡ (một bản cầu nối mang số hiệu dạng
    cũ): [`docs/VERSIONING.md`](docs/VERSIONING.md) mục 5.
  - Đã **đo, không phỏng đoán**: với `VersionPrefix` là `26.9.0901`, MSBuild giữ
    `26.9.0901` ở `AssemblyInformationalVersion` nhưng chuẩn hoá `AssemblyVersion`
    thành `26.9.901`. Bộ đọc chấp nhận cả hai và cho ra cùng một giá trị.

### Thêm

- **Bộ quy ước `tsudev-conventions` v1.0.0** áp vào repo: `AGENTS.md`,
  `docs/DESIGN_SYSTEM.md`, `docs/PROJECT_STRUCTURE.md`, `docs/ARCHITECTURE.md`,
  `tokens/`, `logs/` (STATE + LOCKS + handover), `.gitignore` hợp nhất.
- **Quy ước đặt tên phiên bản được thực thi bằng mã**, không chỉ nằm trong tài
  liệu: `ReleaseName.Validate` từ chối mọi số hiệu sai, `release.yml` gọi chính
  hàm đó để **dừng hẳn quy trình phát hành** trước khi build, `ci.yml` kiểm
  `VersionPrefix` ở mỗi PR.
  - Bị cấm: bỏ đệm số 0 ở `DD`/`NN`, số 0 đứng đầu ở `YY`/`M`, và hậu tố `-rc1`.
  - Tag git vẫn dùng dạng ngắn `v26.8.1901` để `release.yml` bắt được mẫu `v*`.

### Sửa

- **Bản portable rơi vào vòng lặp cập nhật không lối thoát.** Khi có bản mới,
  công cụ tải `swico-setup-*.exe` rồi chạy — kể cả khi nó đang chạy ở dạng
  portable. Trình cài đặt đưa một bản **thứ hai** vào `Program Files`, còn file
  `.exe` đang chạy (thường trên USB) **vẫn cũ**; lần sau chạy lại đúng file đó
  thì lại bị chặn tiếp, mãi mãi.
  - Nay bản portable **vẫn bị chặn** — bộ luật cũ thì vẫn là bộ luật cũ — nhưng
    **không hiện hộp thoại** một nút "Cập nhật" (bấm vào cũng không cài được gì)
    mà chỉ rõ đường tải bản `.zip` mới.
  - Nhận biết bằng sự có mặt của `unins000.exe` cạnh ứng dụng. Khi không chắc,
    mặc định coi là portable: đoán nhầm về phía đó chỉ gây phiền, đoán nhầm về
    phía kia thì tạo lại đúng vòng lặp trên.
  - `GitHubReleaseParser` nay nhận ra cả tệp `swico-portable-*.zip` đính kèm.
- **Một số nhánh "phải cập nhật" thoát lặng lẽ, không in mã thoát.** Chỉ nhánh
  `--silent` in dòng `Mã thoát: 30`; ba nhánh còn lại thoát mà không nói gì.
  Nay mọi nhánh đều đi qua một chỗ duy nhất.
- **`VersionNumber.TryParse` không đọc được tên bản phát hành đầy đủ.** Tên đó
  chứa dấu `-`, mà bước cắt hậu tố cắt tại dấu `-` đầu tiên nên chuỗi còn lại chỉ
  là `"tsudev"`. Hỏng **im lặng**: `UpdateChecker` coi "không đọc được" là
  `CheckFailed` và **không chặn**, nên chức năng cập nhật bắt buộc sẽ lặng lẽ
  ngừng hoạt động thay vì báo lỗi.
- **`docs/UPDATES.md` mô tả sai cách đánh số.** Tài liệu ghi `26.8.18.1` là "bản
  phát hành thứ hai", trong khi bản thứ hai đã phát hành thật mang số `26.8.18.2`.
  Tài liệu sai so với bản đã phát hành, không phải ngược lại.

- **Hiển thị tiến trình quét theo thời gian thực.** Trước đây một lần quét in ba
  dòng `[1] Đang...` `[2] Đang...` `[3] Đang...` rồi im lặng hàng phút — người
  dùng không phân biệt được "đang chạy" với "treo". Nay mỗi bước thu thập in một
  dòng riêng **ngay khi nó xong**, kèm thời gian thật của chính bước đó, và một
  con quay cho biết công cụ vẫn đang chạy.
  - Những bước chạy lâu (`DISM`, `sfc /verifyonly`) tự báo **thời gian dự kiến**.
  - Đầu ra bị chuyển hướng (file log, ống dẫn, RMM/CI) thì tự **bỏ con quay và
    mã màu** — không để ký tự điều khiển lọt vào file log.
  - Console không đọc được UTF-8 thì lùi về ký tự ASCII thay vì hiện chữ rác.
- **Ctrl+C dừng ngay lập tức, kể cả khi `sfc` đang chạy dở.** Tiến trình con bị
  kết thúc thật (trước đây `WaitForExit(1800s)` khiến Ctrl+C không có tác dụng
  suốt tối đa 15 phút), con trỏ terminal được trả về, và công cụ thoát với mã
  **130** — quy ước POSIX `128 + SIGINT` mà bash/PowerShell/CI đều hiểu sẵn.
  Bấm Ctrl+C lần thứ hai thì để hệ điều hành kết thúc ngay, phòng khi chính bước
  dọn dẹp bị kẹt.
- `IProgressSink` trong `Core/Progress/` — thứ tự các bước quét là **logic
  nghiệp vụ**, nên nó nằm trong Core và **kiểm thử được trên Linux**, đúng
  nguyên tắc đã rút ra ở `docs/STATE.md` mục 4.3.

### Kiểm thử

- 173 → **264 test**, chia làm bốn đợt:
  - **+24** cho tiến trình quét: thứ tự các bước của cả hai loại báo cáo, huỷ
    trước khi chạy, huỷ *giữa chừng* (giữ nguyên các bước đã xong), `ScanStep`
    không nuốt ngoại lệ, ghi chú thời gian của DISM/sfc, và mã thoát 130.
  - **+25** cho quy ước đặt tên phiên bản.
  - **+10** cho nhánh bản portable của cổng kiểm tra cập nhật.
  - **+30** cho việc đổi quy ước đặt tên sang `YY.M.DDNN`. Ca quan trọng nhất
    quét **cả tháng** — 31 ngày × 4 bản = 124 số hiệu — và đòi mỗi số hiệu phải
    viết-ra-đọc-lại nguyên vẹn, qua được `Validate`, và giữ đúng thứ tự tăng dần
    liên tục. Quét toàn bộ thay vì vài ca lẻ chính là cách bắt được lỗi của
    **ngày 1–9**, nơi số 0 đệm quyết định đúng/sai. Kèm các ca khoá lại việc
    **đọc được dạng cũ** (`26.8.18` ≡ `26.8.1801`) và nhận **cả hai** dạng tên
    tệp đính kèm — đổi cách đánh số mà quên hai điều đó thì các ca này đỏ.
- CI Windows kiểm thêm rằng lần quét thật in **≥ 10 dòng bước, mỗi dòng kèm thời
  gian riêng** — bắt được trường hợp tiến trình lặng lẽ quay về kiểu in dồn.
- ⚠️ **Chưa tự động hoá được:** con quay có quay mượt không, màu có đúng không,
  con trỏ có được trả về sau Ctrl+C không. Kịch bản kiểm bằng mắt: mục H trong
  `docs/WINDOWS-VERIFICATION.md`.

## [26.8.18.2] — 18/08/2026

### Hiệu năng

- **File cài đặt nhỏ hơn 16,4%** (29,8 → 24,9 MB) và khởi động nhanh hơn.
  - **Bỏ nén trong single-file.** Nghe ngược trực giác nhưng nén hai lần là phản
    tác dụng: payload đã nén thì trình cài đặt không nén thêm được nữa, nên file
    setup lại **to hơn** (đo được: 29,1 so với 22,2 MB ở bước mô phỏng). Bỏ nén
    cũng xoá luôn bước giải nén runtime ở lần chạy đầu.
  - **ReadyToRun** — mã máy biên dịch sẵn, không phải đợi JIT lúc khởi động.
  - Bản portable nén bằng 7-Zip `-mx=9` thay vì `Compress-Archive`.
  - ⚠️ Đánh đổi: `swico.exe` tải trực tiếp **to hơn gấp đôi** (34,0 → 75,8 MB).
    Bản portable tăng nhẹ 4,2%. Xem bảng trong README.
- **Đã thử và PHẢI BỎ cắt tỉa (`PublishTrimmed`).** Nó đưa file setup xuống
  9,6 MB — rất hấp dẫn — nhưng thí nghiệm đối chiếu trên Windows thật cho thấy
  nó **làm mất dữ liệu WMI một cách âm thầm**: bản cắt tỉa thu được 11 dòng và
  tóm tắt `- - · 0 CPU · -`, bản đầy đủ thu được 15 dòng và
  `Microsoft Corporation Virtual Machine · 1 CPU · 16.0 GB`. Vẫn hỏng dù đã
  bảo toàn `System.Management`. Báo cáo vẫn sinh ra, nhìn bình thường, nhưng
  thiếu dữ liệu — kiểu sai tệ nhất mà công cụ này có thể mắc.

### Thêm

- **JSON dùng mã sinh lúc biên dịch** (`Core/Serialization/AuditJson.cs`) thay
  vì phản chiếu — nhanh hơn, và tập trung mọi chỗ đọc/ghi JSON vào một nơi.
- **`packaging/tools/winget-local-install.ps1`** — cài bằng winget **ngay**, từ
  manifest cục bộ, không cần chờ Microsoft duyệt. winget vẫn tự đối chiếu
  `InstallerSha256` nên không kém an toàn hơn kho công khai.
- **CI có hàng rào chất lượng dữ liệu.** Job Windows nay **chạy thật một lần
  quét** và kiểm tra số dòng thu thập được, tóm tắt phần cứng không được báo
  `0 CPU` hay hãng/model rỗng, và `.xlsx` là gói OPC hợp lệ. Chính hàng rào này
  đã phát hiện ra sự cố cắt tỉa — kiểm tra "có sinh ra file không" hoàn toàn
  không bắt được lớp lỗi đó.

## [26.8.18.1] — 18/08/2026

### Thêm

- **Chức năng tự động cập nhật.** Khi khởi động, công cụ hỏi GitHub xem đã có
  phiên bản mới chưa. Nếu có, hiện hộp thoại **một nút "Cập nhật"** → tải,
  **đối chiếu SHA-256**, rồi chạy trình cài đặt. Phải cập nhật xong mới quét tiếp.
  - **Không kiểm tra được thì KHÔNG chặn** — công cụ vẫn quét bình thường kèm
    ghi chú. Chặn ở đây sẽ làm công cụ vô dụng đúng ở nơi cần nhất: máy trong
    mạng cách ly, máy bị tường lửa chặn GitHub.
  - **`--silent` không hiện hộp thoại** mà thoát với mã `30`. Hộp thoại trong
    một tiến trình triển khai tự động sẽ treo vô thời hạn.
  - **`--no-update-check`** tắt hẳn — khi đó công cụ không thực hiện kết nối nào.
  - Hộp thoại dùng **TaskDialog** của Windows thay vì WinForms: cho phép đặt tên
    nút tuỳ ý, và không kéo cả bộ thư viện giao diện vào bản self-contained.
- **Bộ favicon đầy đủ** sinh từ logo: `.ico` đa kích thước, PNG 16/32/180/192/512,
  `site.webmanifest`. Favicon 32px được **nhúng thẳng** vào mỗi báo cáo HTML —
  báo cáo thường mở từ đĩa qua `file://` nên không thể trỏ tới file bên ngoài.
- **Thành phần thứ tư trong số hiệu phiên bản** cho trường hợp phát hành lại
  trong cùng một ngày (`26.8.18.1`). Không có nó, hai bản dựng khác nhau trong
  cùng ngày sẽ mang cùng một số hiệu.

### Thay đổi

- ⚠️ **`PRIVACY.md` và `EULA.txt` đã sửa cho trung thực.** Bản 26.8.18 tuyên bố
  *"KHÔNG kết nối Internet vì bất kỳ mục đích nào"* — tuyên bố đó **không còn
  đúng** từ bản này. Tài liệu nay mô tả chính xác: đúng một yêu cầu GET tới
  GitHub, chỉ để lộ địa chỉ IP và số hiệu phiên bản, **không mang theo bất kỳ
  dữ liệu nào** của máy được quét, và tắt được bằng một tham số.
- `GitHubReleaseParser` và `ChecksumFile` đặt trong **Core** chứ không phải lớp
  adapter — đây là logic thuần, và để trong project `net8.0-windows` thì bộ test
  chạy trên Linux không với tới được.

## [26.8.18] — 18/08/2026

### Thay đổi

- **Đổi hệ đánh số phiên bản sang CalVer `yy.M.d`.** Phiên bản `26.8.18` nghĩa là
  bản phát hành ngày 18/08/2026 — nhìn tên file là biết ngay cũ hay mới.
- **Đổi toàn bộ tên `tsuowlit` thành `tsudev`** cho khớp chủ sở hữu tsudev.com.
  Winget ID nay là `tsudev.SWICO`. Bản ghi lịch sử trong `docs/journal/` và
  `docs/DECISIONS.md` **giữ nguyên** tên cũ — sửa chúng là làm sai sự thật về
  những gì đã diễn ra.

### Thêm

- **Cảnh báo khi bộ luật ngoài lệch phiên bản với bộ luật đóng kèm.** File
  `detection-rules.json` cạnh exe luôn được ưu tiên, nên một file cũ sót lại sau
  khi nâng cấp sẽ âm thầm vô hiệu hoá bộ luật mới. Nay chênh lệch được nêu rõ
  cả hai số hiệu phiên bản, trên màn hình lẫn trong báo cáo.
- **Bản portable trong release nay kèm `detection-rules.json` và
  `DETECTION-RULES.md`** — trước đó thiếu, nên người tải từ release không có
  file để cập nhật luật.
- **Logo tsudev trong báo cáo HTML**, kèm chữ ký thương hiệu "tsu" (xanh) +
  "dev" (cam), cả khối là một liên kết tới tsudev.com. Hiện ở cả đầu trang lẫn
  chân trang. Logo được **nhúng thẳng** dưới dạng data URI đặt trong CSS: báo
  cáo phải xem được khi không có mạng và khi copy đi một mình, mà đặt trong CSS
  thì dữ liệu chỉ xuất hiện **một lần** dù thương hiệu hiện ở nhiều chỗ
  (tiết kiệm 13,4 KB mỗi báo cáo).
- **Icon ứng dụng và ảnh trình thuật sĩ cài đặt** sinh từ chính logo gốc.
- `packaging/tools/make-assets.py` — sinh mọi biến thể của logo từ file gốc.
  Tự viết bộ giải mã/thu nhỏ PNG bằng zlib thuần vì môi trường không có thư
  viện ảnh, và thêm một phụ thuộc chỉ để đổi kích thước một file là không đáng.

## [Chưa phát hành]

### Thêm

- **Bộ luật phát hiện tách khỏi mã nguồn** (`Core/Rules/`) — dạng dữ liệu JSON
  có phiên bản, nạp theo thứ tự `--rules` → file cạnh exe → bộ luật đóng kèm.
  File hỏng/thiếu/rỗng đều quay về bộ luật đóng kèm kèm cảnh báo. Cho phép cập
  nhật dấu hiệu mà không phải biên dịch, ký và phát hành lại.
- Tham số CLI `--rules <file.json>` và `--version`.
- Trường `DetectionRulesVersion` trong báo cáo — truy ngược được kết luận do bộ
  luật nào sinh ra.
- Lớp `Core.Rendering` — **viết mới hoàn toàn** sau khi bản gốc bị mất:
  `HtmlReportRenderer`, `XlsxWriter` (tự ghi OOXML, không phụ thuộc NuGet),
  `DashboardBuilder` (quét đệ quy không giới hạn độ sâu).
- Hồ sơ pháp lý: `LICENSE` (Apache-2.0), `NOTICE`, `EULA.txt`, `PRIVACY.md`,
  `THIRD-PARTY-NOTICES.md`.
- Bộ đóng gói: `packaging/innosetup/swico.iss` (Inno Setup 6, hai ngôn ngữ
  Việt–Anh, hỗ trợ `/VERYSILENT`, tuỳ chọn thêm vào PATH) và manifest winget.
- CI/CD: `.github/workflows/ci.yml` và `release.yml` — build, test, đóng gói,
  ký số qua SignPath Foundation, sinh SBOM, tính checksum, tạo release.
- `.editorconfig` — quy ước định dạng, kèm lý do cụ thể cho từng quy tắc được
  tắt hoặc nâng lên mức lỗi.
- Hạ tầng nối tiếp phiên: `docs/STATE.md`, `docs/CONTINUITY.md`, `docs/PLAN.md`,
  `docs/DECISIONS.md`, `docs/journal/`.
- `docs/WINDOWS-VERIFICATION.md` — kịch bản kiểm chứng trên máy Windows thật.

### Thay đổi

- **Đổi tên sản phẩm thành `tsudev SWICO`.** Trước đó ba tên dùng lẫn lộn:
  thư mục `tsudev-swico`, assembly `tsudev-audit`, manifest `tsudev.SystemAudit`.
  Tên assembly nay là `swico.exe`; namespace `Tsudev.Audit.*` **giữ nguyên** vì
  đó là chi tiết nội bộ người dùng không thấy.
- Tái cấu trúc từ 9 file `.cs` nằm phẳng ở thư mục gốc sang layout `src/` +
  `tests/` mà các `.csproj` vốn đã trỏ tới.
- Gom version và metadata về `Directory.Build.props` — một nguồn sự thật duy nhất.

### Sửa

- **Tài liệu hứa `winget install tsudev.SWICO` trong khi gói chưa được nộp** lên
  kho cộng đồng `microsoft/winget-pkgs`, nên lệnh báo *"No package found matching
  input criteria"*. README và nội dung bản phát hành nay nói rõ điều này.
- **Manifest winget mang `InstallerSha256` giả `0000…0000`.** Hash chỉ biết được
  sau khi đóng gói và ký, nên manifest cam kết sẵn trong repo luôn sai. Nay repo
  chỉ giữ **template**; manifest thật sinh trong quy trình phát hành với hash
  đúng của file setup **đã ký**, và đính kèm mỗi bản phát hành.
- **Kịch bản Inno Setup có 4 lỗi khiến `ISCC` dừng hẳn** — nó chưa từng được
  biên dịch lần nào: thiếu `swico.ico`; `Vietnamese.isl` là bản dịch cộng đồng
  không có trong bản cài Inno Setup mặc định; `x64compatible` chỉ hợp lệ từ
  Inno Setup 6.3; `InfoAfterFile` trỏ tới `.md` nên hiện nguyên dấu markdown.
- **Bước sinh SBOM dùng tham số của CycloneDX 5.** Bản 6 đổi `-f`→`-fn` và
  `-j`→`-F Json`. Đã ghim phiên bản công cụ để quy trình phát hành tái lập được.
- **Bước tạo GitHub Release không chỉ rõ `tag_name`**, nên chỉ chạy được khi
  đẩy tag, còn `workflow_dispatch` thì hỏng.
- **Kết luận đánh giá không hề ảnh hưởng tới mã thoát.** Một máy có Office chưa
  kích hoạt vẫn trả về `0`, nên script không bắt được. Nay mã thoát tách hai
  nhóm: sức khoẻ công cụ (`0/1/2/3`) và kết luận đánh giá (`10` cảnh báo,
  `20` nghiêm trọng). Gộp chung vào một mã sẽ mất khả năng phân biệt *"công cụ
  đọc thiếu dữ liệu"* với *"máy này có vấn đề bản quyền"*. Thêm
  `--no-verdict-exit` cho hệ RMM coi mọi mã khác 0 là script lỗi.
- **Công cụ làm mất màu toàn bộ lịch sử cuộn của terminal.** Nguyên nhân là gán
  `Console.OutputEncoding` vô điều kiện — lệnh này gọi `SetConsoleOutputCP` và
  khiến conhost/Windows Terminal dựng lại screen buffer. Nay chỉ gán khi mã
  trang thực sự chưa phải UTF-8, và bỏ qua hoàn toàn khi đầu ra bị chuyển hướng.
  Việc đổi màu chữ cũng chuyển sang `try/finally` để một ngoại lệ khi ghi không
  để console mắc kẹt ở màu vàng.
- **README tuyên bố "CLI parse tham số: 16/16 test" trong khi không có test CLI
  nào.** `CliOptions` nằm trong project `net8.0-windows` nên bộ test chạy trên
  Linux không với tới được. Đã chuyển `CliOptions` và `ExitCodes` vào Core —
  đây là logic thuần, và mã thoát quyết định một hệ giám sát báo động hay im
  lặng nên phải được kiểm thử.
- **File `.xlsx` bị Excel báo hỏng và tự sửa.** `xl/_rels/workbook.xml.rels`
  thiếu quan hệ trỏ tới `styles.xml`. Theo chuẩn OPC, một phần nằm trong gói mà
  không quan hệ nào trỏ tới thì coi như không tồn tại — trong khi mọi ô đều
  tham chiếu `s="0"`/`s="1"` vào đó. Kèm theo: bổ sung `<bookViews>` (vì
  `sheetView` khai báo `workbookViewId="0"`), chống cắt vỡ cặp thế thay thế khi
  rút gọn tên sheet, giới hạn 32.767 ký tự mỗi ô, và loại bỏ thế thay thế lẻ
  cùng `U+FFFE`/`U+FFFF` khỏi dữ liệu WMI.
- **Trạng thái Office không hề tham gia vào kết luận.** Đây là nguyên nhân thật
  của việc báo cáo kết luận "không phát hiện dấu hiệu" trên một máy có Windows
  hợp lệ nhưng Office ở trạng thái `Notification` (chưa kích hoạt) — trong khi
  bộ PowerShell cũ báo không hợp lệ. Nay Office có tiếng nói riêng trong kết
  luận, ở mức **cảnh báo**: đó là vấn đề tuân thủ bản quyền, không phải dấu
  hiệu kích hoạt trái phép, nên gộp chung vào mức Bad sẽ làm mất ý nghĩa của
  mức Bad.
- **Office cài bằng Click-to-Run thường bị báo nhầm là "không có Office".**
  Danh sách đường dẫn `ospp.vbs` thiếu bố cục `...\Microsoft Office\root\Office16`
  mà mọi bản Office 2016 trở đi đều dùng. Bổ sung thêm nguồn thứ hai đọc trực
  tiếp từ `SoftwareLicensingProduct` theo ApplicationID của Office — nguồn này
  không phụ thuộc vào việc tìm thấy `ospp.vbs`.
- **Adapter WMI giả lập bỏ qua hoàn toàn `whereClause`.** Nghĩa là mọi bộ lọc —
  đặc biệt `ApplicationID` phân biệt SKU Windows với SKU Office — **chưa từng
  được kiểm thử**, đúng nơi phát sinh lớp lỗi kết luận sai về bản quyền.
- **Kết luận bản quyền có thể báo NHẦM là hợp lệ.** Trạng thái Genuine được suy ra bằng
  `licensedCount > 0` — chỉ cần **một SKU bất kỳ** ở trạng thái Licensed. Windows
  khai báo nhiều SKU dưới cùng một ApplicationID, nên máy có SKU chính đang
  `Notification`/`Unlicensed` nhưng có SKU phụ `Licensed` vẫn bị chấm "hợp lệ".
  Nay chỉ kết luận hợp lệ khi **không còn SKU nào** ở trạng thái có vấn đề; trạng
  thái còn hạn dùng thử hạ xuống mức **cảnh báo** thay vì hợp lệ.
- **Lỗi định dạng phụ thuộc ngôn ngữ máy trong `FileNaming`.** Tên thư mục kết
  quả dùng `ToString("yyyyMMdd")` không truyền `IFormatProvider`. Trên máy đặt
  ngôn ngữ Thái hoặc Ả Rập, lịch mặc định không phải Gregorian nên năm cho ra
  hoàn toàn khác (2569 thay vì 2026) — sai tên thư mục, vô hiệu quy ước 3 cấp,
  và trang tổng hợp không gom được kết quả giữa các máy khác ngôn ngữ.
- **Máy dev và CI build bằng hai SDK khác nhau.** Runner GitHub có sẵn .NET 10
  SDK, và `dotnet build` luôn chọn SDK mới nhất trên máy nếu không có
  `global.json`. CI vì thế build bằng .NET 10 trong khi máy dev dùng .NET 8 —
  làm lời hứa "build tái lập được" không thành lập. Đã thêm `global.json` ghim
  SDK và đổi workflow sang đọc từ đó.
- Dọn sạch 27 cảnh báo trình phân tích (CA1305/CA1826/CA1822/CA1869/CA1716) và
  bật `TreatWarningsAsErrors` để chúng không tích tụ trở lại.
- **Dự án không build được.** Thiếu `Tsudev.Audit.Core.csproj` (cả hai csproj
  còn lại đều tham chiếu tới nó) và thiếu toàn bộ namespace `Core.Rendering`
  mà `Program.cs` gọi ở 3 chỗ.
- `<pane>` trong file XLSX đặt sai vị trí theo lược đồ SpreadsheetML — phải nằm
  trong `<sheetViews>` **trước** `<sheetData>`. File vẫn là XML hợp lệ nên lỗi
  này lọt lưới khi chỉ thử trên Linux, nhưng Excel sẽ báo file hỏng.
- Khôi phục bộ test bị lạc trong `mnt/user-data/outputs/`; nay `64 PASS, 0 FAIL`.
- Phần trợ giúp `--help` còn sót tên cũ `tsudev System Audit` / `tsudev-audit.exe`
  sau khi đổi tên sản phẩm.

### Đã biết còn tồn đọng

- `WindowsAdapters.cs` đã chạy được trên Windows thật (sinh ra báo cáo HTML),
  nhưng **chưa đối chiếu tên thuộc tính WMI** và chưa so với bản PowerShell cũ.
  Xem `docs/WINDOWS-VERIFICATION.md`.
- File `.xlsx` chưa từng được Excel thật mở (môi trường phát triển không có).
- Còn khoảng 25 cảnh báo CA1305/CA1826 chưa dọn.

## [3.0.0] — chưa phát hành

Bản viết lại bằng C#/.NET 8 từ bộ 10 file PowerShell, gộp thành một file `.exe`
duy nhất. Phiên bản này chưa từng được phát hành ra ngoài.
