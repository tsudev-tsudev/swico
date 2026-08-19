# VERSIONING — Quy ước đặt tên phiên bản phát hành

> **Đây là quy ước bắt buộc của dự án.** Nó không chỉ là thoả thuận giữa người
> với người: quy ước này được **thực thi bằng mã** ở hai chỗ, nên viết sai sẽ bị
> chặn chứ không lọt ra ngoài.
>
> | Chỗ thực thi | Việc nó làm |
> |---|---|
> | `src/Tsudev.Audit.Core/Updates/ReleaseName.cs` | `Validate()` — từ chối mọi số hiệu sai quy ước, có test |
> | `.github/workflows/release.yml`, bước *Kiểm tra quy ước đặt tên* | Dừng hẳn quy trình phát hành trước khi build |

---

## 1. Dạng chuẩn

```
tsudev-swico-vYY.M.D[.N]
```

| Thành phần | Nghĩa | Ví dụ |
|---|---|---|
| `YY` | Năm, **hai chữ số** | `26` = 2026 |
| `M` | Tháng, **không đệm số 0** | `8` = tháng 8 |
| `D` | Ngày, **không đệm số 0** | `19` = ngày 19 |
| `N` | **Thứ tự bản phát hành trong ngày**, chỉ có từ bản thứ hai | `.2` = bản thứ hai |

Ví dụ đầy đủ cho ngày 19/08/2026:

| Bản phát hành trong ngày | Tên |
|---|---|
| Thứ nhất | `tsudev-swico-v26.8.19` |
| Thứ hai | `tsudev-swico-v26.8.19.2` |
| Thứ ba | `tsudev-swico-v26.8.19.3` |
| … | … |

Sang ngày hôm sau, số đếm **bắt đầu lại từ đầu**:

| Ngày | Bản thứ nhất |
|---|---|
| 20/08/2026 | `tsudev-swico-v26.8.20` |
| 01/09/2026 | `tsudev-swico-v26.9.1` |

## 2. Ba điều bị cấm, và vì sao

### 2.1 Cấm `.1`

`tsudev-swico-v26.8.19.1` **không hợp lệ**.

Bản thứ nhất trong ngày đã có tên là `tsudev-swico-v26.8.19`. Cho phép `.1` nữa
nghĩa là **một bản phát hành có hai cái tên** — và hai cái tên cho một thứ là
nguồn gốc của mọi nhầm lẫn về sau: người dùng báo lỗi ở bản này, ta đi tìm ở bản
kia. Số đếm vì thế bắt đầu từ `.2`, đúng bằng thứ tự thật của bản đó trong ngày.

### 2.2 Cấm số 0 đứng đầu

`tsudev-swico-v26.08.09` **không hợp lệ**; viết `tsudev-swico-v26.8.9`.

`26.08.09` và `26.8.9` là **cùng một số hiệu** nhưng **khác chuỗi ký tự**. Chúng
sinh ra hai tên tag khác nhau, hai tên file cài đặt khác nhau
(`swico-setup-26.08.09.exe` với `swico-setup-26.8.9.exe`), và hai dòng khác nhau
trong `SHA256SUMS.txt` — trong khi chức năng tự cập nhật tìm file cài đặt **theo
tên**. Chuẩn hoá một cách viết duy nhất là cách rẻ nhất để chuyện đó không xảy ra.

### 2.3 Cấm hậu tố ở bản phát hành chính thức

`tsudev-swico-v26.8.19-rc1` không dùng cho bản phát hành chính thức.

Lưu ý phân biệt: `VersionNumber.TryParse` **có** cắt hậu tố `-rc1` và `+bam-commit`,
vì nó đọc dữ liệu từ mạng và **một chuỗi lạ không được phép làm hỏng cả lần quét**.
Còn `ReleaseName.Validate` thì **khắt khe**, vì nó gác cổng khâu phát hành: cho
lọt một số hiệu sai ở đó là phát tán cái sai đó ra máy người dùng.

## 3. VÌ SAO số đếm phải nằm sau một dấu chấm

Đây là phần quan trọng nhất của tài liệu này. Một phương án trông tự nhiên hơn —
dính số đếm liền vào ngày — **làm hỏng chính chức năng tự cập nhật**.

Thành phần thứ ba được so sánh như **một số nguyên**, không phải như văn bản:

```
Phương án dính liền:   bản 2 ngày 19/8  ->  26.8.192
                       bản 1 ngày 20/8  ->  26.8.20

           so sánh:    192  >  20       ->  BẢN CŨ ĐƯỢC COI LÀ MỚI HƠN
```

Hậu quả cụ thể:

- Máy đang chạy `26.8.192` **không bao giờ** nhận được bản ngày 20/8 — chức năng
  cập nhật bắt buộc im lặng ngừng hoạt động, đúng ở nơi nó cần chạy nhất.
- Máy đang chạy bản ngày 20/8 lại bị mời "cập nhật" **ngược** về bản 19/8.
- Inno Setup nhận nhầm chiều cài đè (`AppVersion`), winget cũng vậy.

Và nó còn không mã hoá được ba ngày đầu tháng:

| Muốn nói | Viết thành | Trùng với |
|---|---|---|
| Ngày 1, bản 2 | `26.9.12` | Ngày 12, bản 1 |
| Ngày 2, bản 2 | `26.9.22` | Ngày 22, bản 1 |
| Ngày 3, bản 1 | `26.9.31` | Ngày 31, bản 1 |

Với dấu chấm thì không có vấn đề nào ở trên:

```
26.8.19  <  26.8.19.2  <  26.8.19.3  <  26.8.20
```

Thứ tự này đúng ở **mọi** nơi so sánh — `VersionNumber` của dự án, `System.Version`
của .NET, Inno Setup, winget, GitHub. Không cần bộ giải mã riêng ở bất cứ đâu,
nên **không có chỗ nào để lệch**.

## 4. Số hiệu này đi tới đâu

`Directory.Build.props` là **nguồn sự thật duy nhất**. Sửa `VersionPrefix` ở đó,
mọi nơi khác lấy theo:

| Nơi | Dạng | Ví dụ |
|---|---|---|
| `Directory.Build.props` → `VersionPrefix` | số hiệu trần | `26.8.19.2` |
| Tag git *(kích hoạt `release.yml`)* | `v` + số hiệu | `v26.8.19.2` |
| **Tên GitHub Release** | tên đầy đủ | `tsudev-swico-v26.8.19.2` |
| File cài đặt | | `swico-setup-26.8.19.2.exe` |
| Bản portable | | `swico-portable-26.8.19.2.zip` |
| `swico.exe --version` | số hiệu trần | `26.8.19.2` |
| Inno Setup `AppVersion` | số hiệu trần | `26.8.19.2` |
| Thư mục manifest winget | số hiệu trần | `manifests/t/tsudev/SWICO/26.8.19.2/` |

Tag dùng dạng ngắn `v26.8.19.2` chứ không phải tên đầy đủ, vì `release.yml` kích
hoạt theo mẫu `tags: ['v*']`. Tên đầy đủ nằm ở **tên bản phát hành** — chỗ mà
người dùng thật sự nhìn thấy.

Dù vậy, `VersionNumber.TryParse` vẫn **đọc được cả ba dạng** (`26.8.19.2`,
`v26.8.19.2`, `tsudev-swico-v26.8.19.2`), để không có dạng viết nào làm hỏng việc
kiểm tra cập nhật.

## 5. Quy trình phát hành một phiên bản

```bash
# 1. Đặt số hiệu — CHỈ sửa ở một chỗ này
#    (bản đầu tiên trong ngày 19/08/2026)
sed -i 's|<VersionPrefix>.*</VersionPrefix>|<VersionPrefix>26.8.19</VersionPrefix>|' Directory.Build.props

# 2. Ghi mục mới vào CHANGELOG.md

# 3. Commit, gắn tag, đẩy lên
git commit -am "release: tsudev-swico-v26.8.19"
git tag v26.8.19
git push origin main --tags
```

`release.yml` sẽ tự: kiểm tra quy ước → chạy test → publish → ký số (nếu SignPath
đã cấu hình) → đóng gói installer → sinh SBOM, manifest winget, `SHA256SUMS.txt`
→ tạo bản phát hành **nháp** mang tên `tsudev-swico-v26.8.19`.

### Nếu phải phát hành lại trong ngày

Đổi `VersionPrefix` thành `26.8.19.2` rồi gắn tag `v26.8.19.2`. **Không bao giờ**
chạy lại `release.yml` cho một số hiệu đã phát hành.

> ⛔ **CẠM BẪY ĐÃ TỪNG XẢY RA THẬT.** Chạy lại `release.yml` cho một phiên bản đã
> nộp manifest winget sẽ **làm hỏng manifest đó trong im lặng**: Inno Setup đóng
> gói lại ra file khác byte (do dấu thời gian bên trong installer) → hash mới →
> asset trên release bị ghi đè → manifest đã nộp trỏ tới một hash không còn tồn
> tại. Đã xảy ra với PR `microsoft/winget-pkgs#419878`. Chi tiết: `docs/STATE.md`
> mục 4.4.
>
> Phát hành lại = **số hiệu mới**, không phải chạy lại số hiệu cũ.

## 6. Quan hệ với chức năng tự cập nhật

Quy ước này là **nền móng** của chức năng cập nhật bắt buộc (`docs/UPDATES.md`).
Công cụ quyết định "có bản mới hay không" bằng đúng một phép so sánh:

```csharp
if (latest.Version <= current) -> đang dùng bản mới nhất
else                           -> chặn lại, bắt cập nhật
```

Phép so sánh đó chỉ đúng khi thứ tự số hiệu phản ánh đúng thứ tự thời gian phát
hành. Đó chính là điều mục 3 bảo vệ — và là lý do quy ước này được kiểm chứng
bằng test chứ không chỉ được mô tả bằng lời.

## 7. Lịch sử — cố ý không sửa lại

Các bản phát hành trước quy ước này:

| Phiên bản | Thực tế | Có hợp quy ước không |
|---|---|---|
| `26.8.18` | Bản thứ nhất ngày 18/08/2026 | ✅ |
| `26.8.18.2` | Bản thứ **hai** ngày 18/08/2026 | ✅ |

Hai bản đã phát hành **đều đúng quy ước này**. Cái sai nằm ở tài liệu: bản cũ của
`docs/UPDATES.md` từng mô tả `26.8.18.1` là "bản phát hành thứ hai" — mô tả đó
không khớp với bất kỳ bản phát hành nào từng tồn tại, và đã được sửa.
