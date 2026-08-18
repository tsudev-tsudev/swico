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

## Điều kiện trước khi nộp

| Điều kiện | Trạng thái |
|---|---|
| Có bản phát hành công khai, URL tải ổn định | ✅ |
| `InstallerSha256` khớp đúng file tại URL đó | ✅ (sinh tự động) |
| Giấy phép rõ ràng | ✅ Apache-2.0 |
| **Installer đã được ký số** | ⚠️ **chưa** — chờ SignPath |

Chưa ký **không phải** điều kiện bắt buộc của winget, nhưng gói chưa ký sẽ khiến
người dùng gặp cảnh báo SmartScreen ngay sau khi winget tải về. Nên **nộp sau khi
có chữ ký** để lần tiếp xúc đầu tiên của người dùng không phải là một cảnh báo bảo mật.

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
