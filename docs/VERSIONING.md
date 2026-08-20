# VERSIONING — Quy ước đặt tên phiên bản phát hành

> **Đây là quy ước bắt buộc của dự án.** Nó không chỉ là thoả thuận giữa người
> với người: quy ước này được **thực thi bằng mã** ở hai chỗ, nên viết sai sẽ bị
> chặn chứ không lọt ra ngoài.
>
> | Chỗ thực thi | Việc nó làm |
> |---|---|
> | `src/Tsudev.Audit.Core/Updates/ReleaseName.cs` | `Validate()` — từ chối mọi số hiệu sai quy ước, có test |
> | `.github/workflows/release.yml`, bước *Kiểm tra quy ước đặt tên* | Dừng hẳn quy trình phát hành trước khi build |
> | `.github/workflows/ci.yml` | Kiểm `VersionPrefix` ở **mỗi PR** |
>
> **Nguồn quy ước:** [`docs/DESIGN_SYSTEM.md`](DESIGN_SYSTEM.md) mục 6 — áp dụng
> cho toàn hệ sinh thái tsudev. Tài liệu này là bản diễn giải cho repo SWICO,
> kèm những gì bị cấm và **vì sao**.

---

## 1. Dạng chuẩn

```
{ten-app}_{YY}.{M}.{DD}{NN}_{arch}-setup.{ext}

tsudev-swico_26.8.1901_x64-setup.exe
```

| Thành phần | Nghĩa | Ví dụ |
|---|---|---|
| `YY` | Năm, **hai chữ số**, không đệm 0 | `26` = 2026 |
| `M` | Tháng, **không** đệm số 0 | `8` = tháng 8 |
| `DD` | Ngày, **đệm đủ hai chữ số** | `19`, `09` |
| `NN` | **Thứ tự bản phát hành trong ngày**, đệm đủ hai chữ số, bắt đầu `01` | `01`, `02` |
| `arch` | `x64` \| `x86` \| `arm64` | dự án hiện chỉ phát hành `x64` |

Chuỗi phiên bản trong mã nguồn và manifest là `26.8.1901` — **đồng bộ với tên file**.

Ví dụ đầy đủ cho ngày 19/08/2026:

| Bản phát hành trong ngày | Chuỗi phiên bản | Tên file cài đặt |
|---|---|---|
| Thứ nhất | `26.8.1901` | `tsudev-swico_26.8.1901_x64-setup.exe` |
| Thứ hai | `26.8.1902` | `tsudev-swico_26.8.1902_x64-setup.exe` |
| Thứ ba | `26.8.1903` | `tsudev-swico_26.8.1903_x64-setup.exe` |

Sang ngày hôm sau, số đếm **bắt đầu lại từ `01`**:

| Ngày | Bản thứ nhất |
|---|---|
| 20/08/2026 | `26.8.2001` |
| 09/09/2026 | `26.9.0901` |
| 01/01/2027 | `27.1.0101` |

## 2. Ba điều bị cấm, và vì sao

### 2.1 Cấm bỏ số 0 đệm ở `DD` hoặc `NN`

`26.9.91` **không hợp lệ**; viết `26.9.0901`.

Đây là điều **quan trọng nhất** của cả tài liệu này. Thành phần thứ ba được đọc
bằng phép chia: `DD = giá trị / 100`, `NN = giá trị % 100`. Đệm đủ hai chữ số thì
giá trị đó luôn bằng `DD × 100 + NN`, và mọi thứ khớp. Bỏ đệm đi thì:

```
muốn nói : ngày 9, bản 1
viết thành: 26.9.91
đọc lại   : 91 / 100 = 0  ->  ngày 0, bản 91
```

Một ngày 0 không tồn tại, và tên file cài đặt sinh ra từ đó là tên **không ai
tìm thấy** — trong khi chức năng tự cập nhật tìm file cài đặt **theo tên**.

### 2.2 Cấm số 0 đứng đầu ở `YY` và `M`

`26.08.1901` **không hợp lệ**; viết `26.8.1901`.

`26.08.1901` và `26.8.1901` là **cùng một số hiệu** nhưng **khác chuỗi ký tự**.
Chúng sinh ra hai tên tag khác nhau, hai tên file cài đặt khác nhau, và hai dòng
khác nhau trong `SHA256SUMS.txt`. Chuẩn hoá một cách viết duy nhất là cách rẻ
nhất để chuyện đó không xảy ra.

> Lưu ý sự bất đối xứng: ở `YY`/`M` thì số 0 đứng đầu **bị cấm**, còn ở `DD`/`NN`
> thì nó **bắt buộc**. Không phải mâu thuẫn — `DD` và `NN` có **độ rộng cố định**
> nên số 0 là một phần của giá trị; `YY`/`M` thì không.

### 2.3 Cấm hậu tố ở bản phát hành chính thức

`26.8.1901-rc1` không dùng cho bản phát hành chính thức.

Lưu ý phân biệt: `VersionNumber.TryParse` **có** cắt hậu tố `-rc1` và `+bam-commit`,
vì nó đọc dữ liệu từ mạng và **một chuỗi lạ không được phép làm hỏng cả lần quét**.
Còn `ReleaseName.Validate` thì **khắt khe**, vì nó gác cổng khâu phát hành: cho
lọt một số hiệu sai ở đó là phát tán cái sai đó ra máy người dùng.

## 3. VÌ SAO thứ tự so sánh vẫn đúng

Thành phần thứ ba được so sánh như **một số nguyên**. Nhờ `DD` và `NN` đều đệm
đủ hai chữ số, giá trị đó tăng đơn điệu theo đúng thứ tự thời gian:

```
26.8.1901  (19/8 bản 1)  ->  1901
26.8.1902  (19/8 bản 2)  ->  1902
26.8.2001  (20/8 bản 1)  ->  2001

1901 < 1902 < 2001        ĐÚNG thứ tự
```

Kể cả ở ranh giới dễ sai nhất — ngày 9 sang ngày 10:

```
26.9.0901  ->   901
26.9.0904  ->   904
26.9.1001  ->  1001        901 < 904 < 1001        ĐÚNG
```

Thứ tự này đúng ở **mọi** nơi so sánh — `VersionNumber` của dự án,
`System.Version` của .NET, Inno Setup, winget, GitHub.

> Điều này được khoá lại bằng một ca test **quét cả tháng**: 31 ngày × 4 bản =
> 124 số hiệu, mỗi số phải viết-ra-đọc-lại nguyên vẹn, qua được `Validate`, và
> giữ đúng thứ tự tăng dần. Xem mục `13b` trong `tests/unittests/Program.cs`.

## 4. Số hiệu này đi tới đâu

`Directory.Build.props` là **nguồn sự thật duy nhất**. Sửa `VersionPrefix` ở đó,
mọi nơi khác lấy theo:

| Nơi | Dạng | Ví dụ |
|---|---|---|
| `Directory.Build.props` → `VersionPrefix` | số hiệu trần | `26.8.1901` |
| Tag git *(kích hoạt `release.yml`)* | `v` + số hiệu | `v26.8.1901` |
| **Tên GitHub Release** | tên đầy đủ | `tsudev-swico_26.8.1901` |
| File cài đặt | | `tsudev-swico_26.8.1901_x64-setup.exe` |
| Bản portable | | `tsudev-swico_26.8.1901_x64-portable.zip` |
| `swico.exe --version` | số hiệu trần | `26.8.1901` |
| Inno Setup `AppVersion` | số hiệu trần | `26.8.1901` |
| Thư mục manifest winget | số hiệu trần | `manifests/t/tsudev/SWICO/26.8.1901/` |

Tag dùng dạng ngắn `v26.8.1901` chứ không phải tên đầy đủ, vì `release.yml` kích
hoạt theo mẫu `tags: ['v*']`.

### ⚠️ Một điều đã đo, không phải phỏng đoán: ngày 1–9 trong assembly

Với `<VersionPrefix>26.9.0901</VersionPrefix>`, MSBuild sinh ra **hai** chuỗi:

| Thuộc tính | Giá trị | Ai đọc |
|---|---|---|
| `AssemblyInformationalVersion` | `26.9.0901` — **giữ số 0** | `swico.exe --version` |
| `AssemblyVersion` | `26.9.901` — **mất số 0** | đường dự phòng khi thiếu cái trên |

Đã kiểm chứng bằng cách build thật rồi đọc chuỗi trong `.dll`. Vì vậy
`VersionNumber.TryParse` **bắt buộc** đọc được cả `0901` lẫn `901` và cho ra
cùng một giá trị — nếu không, exe sẽ tự báo nó là một phiên bản khác với tên file
của chính nó. Có test riêng cho điều này.

## 5. Đọc được dạng CŨ — bắt buộc, không phải tiện nghi

Trước ngày 20/08/2026 dự án dùng dạng `tsudev-swico-vYY.M.D[.N]`
(`swico-setup-26.8.19.exe`). **Hai bản đã phát hành ra ngoài theo dạng đó:**

| Phiên bản cũ | Tương đương dạng mới |
|---|---|
| `26.8.18` | `26.8.1801` |
| `26.8.18.2` | `26.8.1802` |

`VersionNumber.TryParse` đọc được **cả hai dạng** và quy về **cùng một giá trị**,
nhờ quy ước "bản thứ nhất mang số thứ tự 01". Phân biệt không nhập nhằng: ngày
chỉ có tối đa 2 chữ số, còn `DDNN` luôn từ 3 chữ số trở lên.

`GitHubReleaseParser` cũng nhận **cả hai dạng tên tệp đính kèm**:

| Dạng | File cài đặt | Bản portable |
|---|---|---|
| Cũ | `swico-setup-*.exe` | `swico-portable-*.zip` |
| Mới | `tsudev-swico_*_x64-setup.exe` | `tsudev-swico_*_x64-portable.zip` |

Bỏ dạng cũ đi thì một bản phát hành cũ sẽ bị coi là "không có file cài đặt", và
người dùng nhận được thông báo *phải tự cập nhật* thay vì **được** cập nhật. Đó
là kiểu hỏng **im lặng** — không ai báo lỗi.

### ⛔ Giới hạn KHÔNG sửa được bằng mã: hai bản đã phát hành

Những điều trên làm cho **bản mới** đọc được **bản cũ**. Chiều ngược lại thì
không: `swico.exe` của bản `26.8.18` và `26.8.18.2` **đã nằm trên máy người dùng**
với bộ đọc phiên bản **cũ** biên dịch sẵn bên trong. Bộ đọc đó gặp tag
`v26.8.1901` sẽ thấy ngày `1901 > 31` và **không đọc được**.

Hệ quả cụ thể, đã truy theo mã (`UpdateChecker`): hai bản đó rơi vào nhánh
`CheckFailed` → **vẫn quét bình thường kèm ghi chú**, không sập, không chặn —
nhưng **mất khả năng cập nhật bắt buộc**.

**Cách gỡ, nếu muốn:** phát hành **một bản cầu nối** mang số hiệu dạng **cũ**
(ví dụ tag `v26.8.20`) chứa exe đã có bộ đọc hai dạng này. Máy đang chạy bản cũ
đọc được tag đó → tự cập nhật → từ đó về sau hiểu được cả dạng mới. Sau bản cầu
nối, mọi bản phát hành dùng dạng mới.

> Đây là quyết định phát hành, thuộc thẩm quyền chủ project — xem
> `docs/STATE.md` mục 3.6.

## 6. Quy trình phát hành một phiên bản

```bash
# 1. Đặt số hiệu — CHỈ sửa ở một chỗ này
#    (bản đầu tiên trong ngày 20/08/2026)
sed -i 's|<VersionPrefix>.*</VersionPrefix>|<VersionPrefix>26.8.2001</VersionPrefix>|' Directory.Build.props

# 2. Ghi mục mới vào CHANGELOG.md theo dạng
#    26.8.2001 — 20/08/2026 — nội dung thay đổi

# 3. Commit, gắn tag, đẩy lên
git commit -am "release: tsudev-swico_26.8.2001"
git tag v26.8.2001
git push origin main --tags
```

`release.yml` sẽ tự: kiểm tra quy ước → chạy test → publish → ký số (nếu SignPath
đã cấu hình) → đóng gói installer → sinh SBOM, manifest winget, `SHA256SUMS.txt`
→ tạo bản phát hành **nháp** mang tên `tsudev-swico_26.8.2001`.

### Nếu phải phát hành lại trong ngày

Đổi `VersionPrefix` thành `26.8.2002` rồi gắn tag `v26.8.2002`. **Không bao giờ**
chạy lại `release.yml` cho một số hiệu đã phát hành.

> ⛔ **CẠM BẪY ĐÃ TỪNG XẢY RA THẬT.** Chạy lại `release.yml` cho một phiên bản đã
> nộp manifest winget sẽ **làm hỏng manifest đó trong im lặng**: Inno Setup đóng
> gói lại ra file khác byte (do dấu thời gian bên trong installer) → hash mới →
> asset trên release bị ghi đè → manifest đã nộp trỏ tới một hash không còn tồn
> tại. Đã xảy ra với PR `microsoft/winget-pkgs#419878`. Chi tiết: `docs/STATE.md`
> mục 4.4.
>
> Phát hành lại = **số hiệu mới**, không phải chạy lại số hiệu cũ.

## 7. Quan hệ với chức năng tự cập nhật

Quy ước này là **nền móng** của chức năng cập nhật bắt buộc (`docs/UPDATES.md`).
Công cụ quyết định "có bản mới hay không" bằng đúng một phép so sánh:

```csharp
if (latest.Version <= current) -> đang dùng bản mới nhất
else                           -> chặn lại, bắt cập nhật
```

Phép so sánh đó chỉ đúng khi thứ tự số hiệu phản ánh đúng thứ tự thời gian phát
hành. Đó chính là điều mục 3 bảo vệ — và là lý do quy ước này được kiểm chứng
bằng test chứ không chỉ được mô tả bằng lời.

## 8. Lịch sử — cố ý không sửa lại

| Phiên bản | Thực tế | Dạng |
|---|---|---|
| `26.8.18` | Bản thứ nhất ngày 18/08/2026 | cũ |
| `26.8.18.2` | Bản thứ **hai** ngày 18/08/2026 | cũ |

Hai bản này **giữ nguyên tên** trên trang phát hành. Đổi tên một bản đã phát hành
là làm hỏng mọi liên kết và mọi mã băm đã công bố về nó.

Quyết định chuyển sang quy ước hiện hành: **D-S004-1**, ngày 20/08/2026 —
`docs/journal/S004-2026-08-20.md`.
