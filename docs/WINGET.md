# Đưa SWICO lên winget

## Vì sao `winget install tsudev.SWICO` chưa chạy được

`winget` tìm gói trong **kho cộng đồng của Microsoft**
(`microsoft/winget-pkgs`), **không** tìm trong repo của bạn.

Manifest nằm trong repo này chỉ là *nguyên liệu*. Cho tới khi nó được **hợp nhất
vào `microsoft/winget-pkgs`**, winget hoàn toàn không biết gói tồn tại và sẽ báo:

```
No package found matching input criteria.
```

Kiểm tra nhanh xem gói đã lên kho chưa:

```bash
curl -s -o /dev/null -w "%{http_code}\n" \
  https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests/t/tsudev/SWICO
# 404 = chưa có · 200 = đã có
```

## Manifest được sinh tự động, không cam kết sẵn

Manifest **bắt buộc** phải chứa `InstallerSha256` của chính file setup — mà giá
trị đó chỉ biết **sau khi** đóng gói và ký. Vì vậy repo chỉ giữ **template**
(`packaging/winget/template/`), còn manifest thật được sinh trong quy trình phát
hành và **đính kèm mỗi bản phát hành** dưới dạng `winget-manifest-<phiên-bản>.zip`.

> Một manifest cam kết sẵn trong repo sẽ luôn mang hash cũ hoặc hash giả. Đó là
> một cái bẫy: nhìn như đã sẵn sàng nộp, nhưng nộp lên sẽ bị từ chối ngay ở khâu
> kiểm tra. Dự án này **đã từng** có đúng lỗi đó — manifest mang hash
> `0000…0000` suốt nhiều bản dựng.

Sinh lại thủ công khi cần:

```bash
python3 packaging/tools/make-winget-manifest.py \
    --version 26.8.18 \
    --installer packaging/output/swico-setup-26.8.18.exe \
    --out packaging/winget-out
```

## Dùng winget NGAY BÂY GIỜ, không cần chờ duyệt

winget có sẵn khả năng cài từ **manifest cục bộ**. Mỗi bản phát hành đã đính kèm
manifest sinh tự động, nên đường này chạy được ngay:

```powershell
# PowerShell với quyền Administrator
irm https://raw.githubusercontent.com/tsudev-tsudev/swico/main/packaging/tools/winget-local-install.ps1 -OutFile wg.ps1
.\wg.ps1
```

Script tự: bật chế độ cho phép manifest cục bộ → tải manifest từ bản phát hành
mới nhất → `winget validate` → `winget install --manifest`.

**winget vẫn tự đối chiếu `InstallerSha256`** trong manifest với file tải về, nên
đường này không kém an toàn hơn kho công khai.

Hoặc làm tay:

```powershell
winget settings --enable LocalManifestFiles
# giải nén winget-manifest-<phiên-bản>.zip từ Releases
winget validate --manifest .\manifests\t\tsudev\SWICO\<phiên-bản>
winget install  --manifest .\manifests\t\tsudev\SWICO\<phiên-bản>
```

> Khác biệt duy nhất so với kho công khai: người dùng phải bật một thiết lập và
> chỉ ra thư mục manifest. Sau khi PR được hợp nhất, `winget install tsudev.SWICO`
> sẽ chạy mà không cần gì thêm.

## Điều kiện trước khi nộp

| Điều kiện | Trạng thái |
|---|---|
| Có bản phát hành công khai, URL tải ổn định | ✅ |
| `InstallerSha256` khớp đúng file tại URL đó | ✅ (sinh tự động) |
| Giấy phép rõ ràng | ✅ Apache-2.0 |
| **Installer đã được ký số** | ⚠️ **chưa** — chờ SignPath |

Chưa ký **không phải** điều kiện bắt buộc của winget. Chủ dự án đã quyết định nộp
ngay ở giai đoạn dùng nội bộ, chấp nhận cảnh báo SmartScreen; khi SignPath duyệt
và có bản đã ký sẽ nộp bản cập nhật để phổ biến rộng.

## Cách nộp

### Cách 1 — `wingetcreate` (khuyến nghị)

```powershell
winget install Microsoft.WingetCreate

wingetcreate submit `
  --token <GitHub personal access token> `
  packaging\winget-out\manifests\t\tsudev\SWICO\26.8.18
```

Công cụ tự fork `microsoft/winget-pkgs`, tạo nhánh và mở pull request.

### Cách 2 — thủ công

1. Fork `microsoft/winget-pkgs`.
2. Chép thư mục manifest vào đúng đường dẫn
   `manifests/t/tsudev/SWICO/<phiên-bản>/`.
   Đường dẫn này **suy ra từ `PackageIdentifier`**, không được đặt tuỳ ý.
3. Mở pull request.

### Kiểm tra trước khi nộp

```powershell
winget validate --manifest packaging\winget-out\manifests\t\tsudev\SWICO\26.8.18
winget install --manifest packaging\winget-out\manifests\t\tsudev\SWICO\26.8.18
```

Lệnh thứ hai cài **từ manifest cục bộ** — cách duy nhất kiểm chứng manifest đúng
trước khi nộp lên kho công khai.

## Sau khi pull request được hợp nhất

Bot của winget chạy kiểm thử tự động, thường mất vài giờ tới vài ngày. Khi xong:

```powershell
winget install tsudev.SWICO
```

Lúc đó **mới** cập nhật README và nội dung bản phát hành để giới thiệu lệnh này.

## Mỗi lần phát hành phiên bản mới

Phải nộp manifest mới cho **từng phiên bản** — winget lưu lịch sử theo phiên bản,
không tự cập nhật. `wingetcreate update tsudev.SWICO --version <mới> --urls <url>`
làm việc này gọn hơn là nộp lại từ đầu.
