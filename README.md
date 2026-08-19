# tsudev SWICO

<img src="assets/tsudev-logo-144.png" alt="tsudev" align="right" height="120">


Kiểm tra tình trạng bản quyền Windows/Office và thu thập cấu hình phần cứng —
**một file `.exe` duy nhất**, không cần cài .NET Runtime.

[![CI](https://github.com/tsudev-tsudev/swico/actions/workflows/ci.yml/badge.svg)](https://github.com/tsudev-tsudev/swico/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

> **Báo cáo do công cụ tạo ra là dữ liệu kỹ thuật để tham khảo, không phải kết
> luận pháp lý.** Xem [`EULA.txt`](EULA.txt) mục 3 trước khi dùng kết quả vào
> bất kỳ quyết định nào có hệ quả với con người.

## Đánh số phiên bản

Mỗi bản phát hành mang tên `tsudev-swico-vYY.M.D[.N]` — **CalVer**, phiên bản
chính là ngày phát hành:

| Tên bản phát hành | Nghĩa |
|---|---|
| `tsudev-swico-v26.8.19` | bản thứ nhất ngày 19/08/2026 |
| `tsudev-swico-v26.8.19.2` | bản **thứ hai** cùng ngày 19/08/2026 |
| `tsudev-swico-v26.9.3` | bản thứ nhất ngày 03/09/2026 |

Số cuối chỉ xuất hiện từ bản thứ hai trong ngày, và nó **chính là thứ tự** của
bản đó trong ngày. Không có số 0 thừa ở đầu (`26.8.19`, không phải `26.08.19`)
để khớp cách .NET và Inno Setup diễn giải số hiệu. Ưu điểm: nhìn tên file
`swico-setup-26.8.19.exe` là biết ngay bản đó cũ hay mới, không cần tra bảng.

Số đếm nằm **sau một dấu chấm** chứ không dính liền vào ngày, vì `26.8.192` sẽ
được so sánh như số nguyên `192 > 20` và làm hỏng chính chức năng tự cập nhật.
Quy ước đầy đủ, kèm những gì bị cấm và vì sao: [`docs/VERSIONING.md`](docs/VERSIONING.md).

## Cài đặt

Tải từ [Releases](https://github.com/tsudev-tsudev/swico/releases):

| Cách | Tệp | Dùng khi |
|---|---|---|
| **Cài đặt** | `swico-setup-<phiên-bản>.exe` | Cài cố định, có mục gỡ cài đặt, hỗ trợ `/VERYSILENT` để triển khai hàng loạt |
| **Portable** | `swico-portable-<phiên-bản>.zip` | Cắm USB đi từng máy — giải nén chạy thẳng, không đụng registry |
| **Chỉ file exe** | `swico.exe` | Nhúng vào script hoặc RMM |

Cả ba chứa **cùng một chương trình**; chỉ khác cách giao đến máy đích. Đối chiếu
tệp tải về với `SHA256SUMS.txt` đính kèm mỗi bản phát hành.

**Về dung lượng:** từ bản 26.8.18.2, file `swico.exe` không còn được nén sẵn bên
trong. Nghe ngược trực giác, nhưng nén hai lần là phản tác dụng — payload đã nén
thì trình cài đặt không nén thêm được nữa, nên **file setup lại to hơn**. Bỏ nén
cũng xoá luôn bước giải nén runtime mỗi lần khởi động.

| Tệp | 26.8.18 | 26.8.18.2 | |
|---|---|---|---|
| `swico-setup-*.exe` | 29,8 MB | **24,9 MB** | giảm 16,4% |
| `swico-portable-*.zip` | 29,1 MB | 30,3 MB | tăng 4,2% |
| `swico.exe` tải trực tiếp | 34,0 MB | 75,8 MB | **tăng 123%** |

Trình cài đặt nén bằng LZMA2 nên hưởng lợi trọn vẹn. `.zip` chỉ có Deflate nên
gần như hoà. Còn `swico.exe` tải trực tiếp thì **to hơn gấp đôi** — vì trước đây
nó tự nén bên trong, giờ thì không.

**Nên chọn gì:**

- **File setup** — nhỏ nhất, và là cách hầu hết người dùng nên dùng
- **Bản portable** — khi không muốn cài đặt
- **`swico.exe` trực tiếp** — chỉ khi thật cần một file đơn cho script; hãy biết
  rằng bạn đang tải nhiều hơn gấp đôi

Đổi lại ở cả ba dạng: không còn bước giải nén runtime ở lần chạy đầu, và mã máy
đã biên dịch sẵn (ReadyToRun) nên không phải đợi JIT lúc khởi động.

> ⚠️ **`winget install tsudev.SWICO` chưa dùng được.** Gói chưa được nộp lên kho
> cộng đồng `microsoft/winget-pkgs`, nên lệnh này sẽ báo *"No package found
> matching input criteria"*. Manifest đã sẵn sàng và đính kèm mỗi bản phát hành;
> quy trình nộp ở [`docs/WINGET.md`](docs/WINGET.md).

## Cách dùng

```
swico.exe                          Quét đầy đủ, mở báo cáo khi xong
swico.exe --scope license          Chỉ kiểm tra bản quyền
swico.exe --scope hardware         Chỉ thu thập phần cứng
swico.exe --silent --sfc           Quét sâu, không mở trình duyệt (GPO/RMM)
swico.exe -o D:\BaoCao --silent    Lưu vào thư mục chỉ định
swico.exe --rules .\luat-moi.json  Quét bằng bộ luật cập nhật
swico.exe --no-verdict-exit        Kết luận không ảnh hưởng mã thoát
swico.exe --version                Xem phiên bản
swico.exe --help                   Xem toàn bộ tham số
```

Công cụ yêu cầu quyền Administrator để đọc trạng thái bản quyền, Defender và
chạy DISM/SFC. Windows sẽ tự hiện hộp thoại UAC.

### Theo dõi tiến trình khi đang quét

Một lần quét đầy đủ mất từ vài chục giây tới hơn 15 phút (nếu bật `--sfc`).
Công cụ in **từng bước một, ngay khi bước đó chạy xong**, kèm thời gian thật của
chính nó — không dồn lại tới cuối:

Dưới đây là đầu ra **thật**, chép từ log CI chạy trên runner Windows
([run 32201733164](https://github.com/tsudev-tsudev/swico/actions/runs/32201733164)):

```
[2] Đang thu thập cấu hình phần cứng...
    ✓ Tổng quan thiết bị   1.3 s
    ✓ CPU   1.0 s
    ✓ RAM   0.0 s
    ✓ Ổ đĩa   0.0 s
    ✓ Phân vùng   0.0 s
    ✓ Card đồ họa   0.0 s
    ✓ Card mạng   0.0 s
    ✓ Driver lỗi   0.1 s
      DISM CheckHealth đang chạy (thường dưới 5 giây, tối đa 3 phút)...
    ✓ Toàn vẹn file hệ thống   0.9 s
    ✓ Windows Defender   0.2 s
    ✓ Ghi báo cáo ra đĩa   0.0 s
```

Trên terminal thật, dòng đang chạy còn có **con quay** (`⠹`) quay tại chỗ trước
khi được thay bằng `✓` — log CI không thể hiện được điều đó vì đầu ra bị chuyển
hướng nên con quay tự tắt (xem đoạn dưới).

Con quay cho biết công cụ **đang chạy chứ không treo** — đây là khác biệt mà một
cột thời gian đứng yên không nói được. Những bước chạy lâu (DISM, `sfc`) còn tự
báo thời gian dự kiến.

Khi đầu ra bị **chuyển hướng** (ghi ra file log, đưa qua ống dẫn, chạy trong
RMM/CI), công cụ tự bỏ con quay và mã màu — chỉ còn mỗi bước một dòng sạch, để
không có ký tự điều khiển nào lọt vào file log.

**Ctrl+C dừng ngay lập tức**, kể cả khi `sfc` đang chạy dở: tiến trình con bị
kết thúc, con trỏ terminal được trả về bình thường, những mục đã quét xong vẫn
nằm nguyên trên màn hình, và công cụ thoát với mã `130`.

### Mã thoát (cho tích hợp RMM/giám sát)

Chia hai nhóm **có chủ đích**, vì hệ thống giám sát cần phân biệt *"công cụ đọc
thiếu dữ liệu"* với *"máy này có vấn đề bản quyền"*.

**Sức khoẻ công cụ** — công cụ chạy có trọn vẹn không:

| Mã | Ý nghĩa |
|----|---------|
| 0 | Hoàn tất, không phát hiện vấn đề |
| 1 | Hoàn tất nhưng thiếu dữ liệu ở một số mục (báo cáo chính vẫn đầy đủ) |
| 2 | Lỗi nghiêm trọng, không tạo được báo cáo |
| 3 | Tham số dòng lệnh không hợp lệ |

**Kết luận đánh giá** — máy được quét có vấn đề không:

| Mã | Ý nghĩa |
|----|---------|
| 10 | Kết luận mức **cảnh báo** (ví dụ: Office chưa kích hoạt) |
| 20 | Kết luận mức **nghiêm trọng** (dấu hiệu kích hoạt trái phép) |
| 30 | **Cần cập nhật** trước khi quét (chỉ ở chế độ `--silent`) |
| 130 | Người dùng **huỷ** bằng Ctrl+C (quy ước POSIX `128 + SIGINT`) |

Thứ tự ưu tiên khi nhiều điều kiện cùng xảy ra: `2 > 3 > 130 > 20 > 10 > 1 > 0`.
Kết luận đánh giá thắng "thiếu dữ liệu" vì nó cần hành động hơn.

Nếu hệ RMM của bạn coi **mọi** mã khác 0 là script lỗi, thêm `--no-verdict-exit`
để chỉ dùng nhóm mã sức khoẻ công cụ.

## Kết quả đầu ra

Bốn định dạng cùng lúc: **HTML** để đọc, **JSON** để tích hợp, **XLSX** và
**CSV** để xử lý tiếp.

```
<thư mục chứa exe>/
  tsudev-bao-cao-ra-quet-<Máy>-<Ngày>/          ← cấp 2
    tsudev-tong-hop.html                         ← trang tổng hợp mọi máy
    tsudev-ket-qua-ra-quet-<Máy>-<Ngày>/         ← cấp 3
      Windows_License_Audit_*.html / .json / .xlsx / .csv
      Windows_Hardware_Inventory_*.html / .json / .xlsx / .csv
```

Quét nhiều máy: copy các thư mục **cấp 3** từ máy khác vào cấp 2 →
`tsudev-tong-hop.html` tự đọc đệ quy (**không giới hạn độ sâu**) và gom lại.

## Bộ luật phát hiện cập nhật độc lập

Các dấu hiệu kích hoạt trái phép nằm trong một **file dữ liệu riêng có phiên
bản**, không nằm cứng trong mã. Khi có biến thể mới, chỉ cần thay file
`detection-rules.json` đặt cạnh `swico.exe` — không cần cài lại, không cần chờ
bản phát hành mới.

```powershell
swico.exe --rules .\luat-moi.json
```

Thứ tự ưu tiên: `--rules` → file cạnh exe → bộ luật đóng kèm bên trong exe.
File hỏng hoặc thiếu sẽ tự quay về bộ luật đóng kèm kèm cảnh báo, **không làm
hỏng lần quét**. Chi tiết: [`docs/DETECTION-RULES.md`](docs/DETECTION-RULES.md).

> Đây **không phải** bảo mật bằng che giấu — mã nguồn công khai nên luật cũng
> công khai. Thứ thay đổi là **tốc độ cập nhật**.

## Tự động cập nhật

Khi khởi động, công cụ hỏi GitHub xem đã có phiên bản mới chưa. Nếu có, hiện hộp
thoại một nút **"Cập nhật"** — tải, **đối chiếu SHA-256**, rồi chạy trình cài đặt.
Phải cập nhật xong mới quét tiếp, vì kết luận dựa trên bộ luật phát hiện và một
bộ luật lỗi thời có thể bỏ sót dấu hiệu mới.

**Nếu không kiểm tra được** (mất mạng, tường lửa chặn) thì công cụ **vẫn quét
bình thường** kèm ghi chú. Chặn ở đây sẽ làm công cụ vô dụng đúng ở nơi cần nhất.

Ở chế độ `--silent` không hiện hộp thoại — thoát với mã `30` để hệ thống triển
khai tự xử lý. Tắt hẳn bằng `--no-update-check`.

Chi tiết: [`docs/UPDATES.md`](docs/UPDATES.md).

## Quyền riêng tư

**Không dữ liệu nào của máy được quét rời khỏi máy.** Công cụ thực hiện đúng
**một** kết nối mạng — kiểm tra phiên bản mới — và kết nối đó chỉ để lộ địa chỉ
IP cùng số hiệu phiên bản, như mọi yêu cầu HTTP. Tắt bằng `--no-update-check`.

Toàn bộ mã chạm tới mạng nằm gọn trong một file để bạn tự kiểm chứng:
`src/Tsudev.Audit.Windows/UpdateAdapters.cs`. Chi tiết: [`PRIVACY.md`](PRIVACY.md).

## Kiến trúc

```
src/
  Tsudev.Audit.Core/        net8.0          ← KHÔNG phụ thuộc Windows
    Models/                                    JSON schema v3.0 (hợp đồng dùng chung)
    Abstractions/                              Các "cổng" (port): IWmiQuery, IRegistryReader...
    Collectors/                                TOÀN BỘ logic nghiệp vụ
    Cli/                                       Phân tích tham số + mã thoát (logic thuần, test được)
    Updates/                                   So sánh phiên bản + quyết định cập nhật
    Rules/                                     Luật phát hiện dạng dữ liệu có phiên bản
    Reports/                                   Lắp ráp collector thành báo cáo
    Rendering/                                 HTML + XLSX + Dashboard
    Testing/                                   Adapter giả lập cho unit test
  Tsudev.Audit.Windows/     net8.0-windows  ← Adapter mỏng: WMI, Registry, Process
  Tsudev.Audit.Cli/         net8.0-windows  ← 1 file exe
assets/                     Logo gốc + biến thể sinh tự động (icon, favicon, ảnh trình thuật sĩ)
  favicon/                  Bộ favicon đầy đủ: .ico, PNG 16/32/180/192/512, webmanifest
packaging/
  tools/make-assets.py      Sinh mọi biến thể của logo từ file gốc
tests/
  unittests/                net8.0          ← 197 test, chạy được trên mọi nền tảng
```

### Vì sao tách `Core` khỏi Windows?

Đây là quyết định kiến trúc quan trọng nhất (mô hình **Ports & Adapters**):

1. **Kiểm thử được** — toàn bộ logic nghiệp vụ (quét dấu hiệu crack, tính điểm
   rủi ro, phân loại phần mềm, dựng HTML/XLSX) unit-test được **không cần máy
   Windows**. 197 test chạy trên Linux.
2. **Dễ audit** — mọi lệnh gọi hệ thống tập trung trong đúng một file
   (`WindowsAdapters.cs`). Với một công cụ đọc dữ liệu nhạy cảm và đòi quyền
   Administrator, việc rà soát bảo mật làm được nhanh là điều thiết yếu.
3. **Dễ mở rộng** — thêm nền tảng khác chỉ cần viết adapter mới, tái dùng
   nguyên vẹn lớp render báo cáo.

## Build từ mã nguồn

Yêu cầu: **.NET 8 SDK**. Build được trên cả Windows lẫn Linux/macOS — kể cả
bước publish `win-x64` (cross-compile được), chỉ không chạy thử được file exe.

```powershell
.\build.ps1              # test + build + publish
.\build.ps1 -Package     # thêm bước đóng gói installer (cần Inno Setup 6)
```

Kết quả: `publish/swico.exe`.

Không có phụ thuộc NuGet nào ngoài `System.Management` (gói chính thức của
Microsoft để truy vấn WMI). XLSX được **tự ghi theo chuẩn OOXML** và trang báo
cáo HTML **không nạp thư viện JavaScript nào từ bên ngoài** — báo cáo mở được
trên máy không có mạng. Chi tiết: [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

## Tình trạng kiểm thử — trung thực

| Thành phần | Trạng thái |
|---|---|
| Models + JSON round-trip | ✅ Đã test |
| Parser ospp.vbs / CIM_DATETIME / InstallDate | ✅ Đã test (kể cả input rác) |
| Quét dấu hiệu crack (6 hạng mục) | ✅ Đã test trên máy giả lập bị nhiễm |
| Tính điểm rủi ro + giới hạn theo nhóm | ✅ Đã test mọi mốc biên |
| Thu thập phần mềm từ registry | ✅ Đã test |
| Diễn giải DISM/SFC | ✅ Đã test |
| Chống HTML injection | ✅ Đã test |
| XLSX: quy đổi tên cột, nhận diện số, tên sheet | ✅ Đã test |
| Dashboard đọc sâu đa máy | ✅ Đã test |
| Logo, favicon & chữ ký thương hiệu | ✅ 11 test |
| So sánh phiên bản CalVer | ✅ 15 test |
| Quy ước đặt tên phiên bản (`ReleaseName`) | ✅ 25 test |
| Cổng kiểm tra cập nhật | ✅ 10 test |
| Đọc bản phát hành GitHub + đối chiếu mã băm | ✅ 16 test |
| Bộ luật tách rời (nạp, kiểm tra, quay về mặc định) | ✅ Đã test |
| CLI parse tham số | ✅ 21 test |
| Mã thoát (gồm ca hồi quy Office chưa kích hoạt) | ✅ 11 test |
| Tiến trình quét: thứ tự bước, huỷ giữa chừng, mã 130 | ✅ 24 test |
| **Con quay & màu trên terminal thật** | ⚠️ **CHƯA tự động hoá** — cần mắt người |
| **XLSX mở bằng Excel thật** | ⚠️ **CHƯA kiểm chứng** — môi trường dev không có Excel |
| **Adapter WMI/Registry thực tế** | ⚠️ **CHƯA chạy thử trên Windows** |

### Giới hạn cần biết

Lớp `Tsudev.Audit.Windows` biên dịch được nhưng **chưa từng chạy trên Windows
thật**. Cần xác nhận tên thuộc tính WMI, đặc biệt `MSFT_MpComputerStatus`,
`MSFT_MpThreatDetection` (trường `ThreatName` có thể phải tra chéo qua
`MSFT_MpThreat`) và `MSFT_ScheduledTask`.

Nếu có lỗi, hầu hết sẽ nằm gọn trong `WindowsAdapters.cs` — logic nghiệp vụ đã
được 197 test kiểm chứng nên không cần đụng tới.

**Phần hiển thị tiến trình cũng có một khoảng chưa tự động hoá được.** Thứ tự
các bước, việc huỷ giữa chừng và mã thoát 130 đều có test chạy trên Linux; CI
trên Windows còn kiểm rằng mỗi bước in ra một dòng riêng kèm thời gian của chính
nó. Nhưng *con quay có quay mượt không*, *màu có đúng không*, *con trỏ có được
trả về sau Ctrl+C không* thì *chưa* có cách kiểm tự động — những thứ đó cần một
người ngồi trước terminal thật. Xem `docs/WINDOWS-VERIFICATION.md`.

Kịch bản kiểm chứng đầy đủ: [`docs/WINDOWS-VERIFICATION.md`](docs/WINDOWS-VERIFICATION.md).

## Tài liệu

| File | Nội dung |
|---|---|
| [`docs/STATE.md`](docs/STATE.md) | Trạng thái sống — **đọc đầu tiên** |
| [`docs/PLAN.md`](docs/PLAN.md) | Lộ trình theo giai đoạn |
| [`docs/DECISIONS.md`](docs/DECISIONS.md) | Các quyết định đã chốt và lý do |
| [`docs/CONTINUITY.md`](docs/CONTINUITY.md) | Giao thức nối tiếp giữa các phiên làm việc |
| [`docs/SIGNING.md`](docs/SIGNING.md) | Ký số qua SignPath Foundation |
| [`docs/WINGET.md`](docs/WINGET.md) | Đưa gói lên winget và vì sao chưa dùng được |
| [`docs/UPDATES.md`](docs/UPDATES.md) | Chức năng tự cập nhật và các quyết định thiết kế |
| [`docs/VERSIONING.md`](docs/VERSIONING.md) | Quy ước đặt tên phiên bản phát hành |
| [`docs/DETECTION-RULES.md`](docs/DETECTION-RULES.md) | Bộ luật phát hiện và cách cập nhật |
| [`docs/WINDOWS-VERIFICATION.md`](docs/WINDOWS-VERIFICATION.md) | Kịch bản kiểm chứng trên Windows |
| [`CHANGELOG.md`](CHANGELOG.md) | Nhật ký thay đổi |

## Giấy phép

[Apache-2.0](LICENSE). Bạn được fork và phân phối bản sửa đổi, nhưng **không
được dùng tên "tsudev" hay "SWICO"** để phát hành bản của mình (mục 6 của
giấy phép). Xem [`NOTICE`](NOTICE).
