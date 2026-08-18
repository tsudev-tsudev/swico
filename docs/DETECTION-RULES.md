# Bộ luật phát hiện

## Vấn đề cần giải quyết

Mã nguồn dự án này **sẽ công khai** — đó là điều kiện bắt buộc để được cấp
chứng chỉ ký số miễn phí qua SignPath Foundation (xem `docs/SIGNING.md`).

Nghĩa là người viết công cụ kích hoạt trái phép đọc được **chính xác** các dấu
hiệu đang bị rà soát, và né chúng. Đây không phải rủi ro lý thuyết — đó là cách
các bộ luật phát hiện bị vô hiệu hoá trong thực tế.

## Cách xử lý: luật là dữ liệu, không phải mã

Toàn bộ dấu hiệu nằm trong `src/Tsudev.Audit.Core/Rules/detection-rules.json`,
tách hẳn khỏi mã nguồn quét.

**Điều này KHÔNG phải bảo mật bằng che giấu.** Luật vẫn công khai như trước.
Thứ thay đổi là **tốc độ cập nhật**: khi xuất hiện biến thể mới, chỉ cần thay
một file JSON. Không biên dịch lại, không ký lại, không phát hành lại, không
cài lại. Việc né luật cũ vì thế mất giá trị lâu dài.

## Thứ tự ưu tiên khi nạp

1. File chỉ định bằng `--rules <đường-dẫn>`
2. File `detection-rules.json` đặt **cạnh** `swico.exe`
3. **Bộ luật đóng kèm** bên trong exe

Bậc 3 luôn tồn tại, nên công cụ chạy được ngay cả khi chỉ copy mỗi một file exe.

## Nguyên tắc: file luật hỏng KHÔNG được làm hỏng lần quét

Mọi lỗi khi nạp — thiếu file, sai JSON, không đủ quyền đọc, nội dung không hợp
lệ — đều dẫn tới **quay về bộ luật đóng kèm** kèm một cảnh báo hiện trong báo cáo.

Riêng **bộ luật rỗng bị từ chối** dù về mặt cú pháp là hợp lệ. Lý do: bộ luật
rỗng khiến mọi máy đều "sạch". Đó nguy hiểm hơn một bộ luật sai, vì nó tạo cảm
giác an toàn giả mà không có dấu hiệu nào cho thấy có gì bất thường.

## ⚠️ Cạm bẫy: file ngoài luôn thắng bộ luật đóng kèm

File `detection-rules.json` đặt cạnh `swico.exe` **luôn được ưu tiên** hơn bộ
luật bên trong chương trình. Điều đó có mặt trái:

> Sau khi nâng cấp lên bản `swico.exe` mới, một file `detection-rules.json` **cũ**
> còn sót lại sẽ **âm thầm vô hiệu hoá** bộ luật mới trong bản nâng cấp.

Vì vậy công cụ **so phiên bản và cảnh báo** mỗi khi bộ luật ngoài khác bộ luật
đóng kèm — cảnh báo hiện cả trên màn hình lẫn trong báo cáo, nêu rõ cả hai số
hiệu phiên bản. Nếu bạn vừa nâng cấp, hãy xoá file cũ hoặc cập nhật nó.

Cùng phiên bản thì không cảnh báo, để không làm nhiễu mỗi lần quét.

## Cấu trúc file

| Trường | Ý nghĩa |
|---|---|
| `version` | **Bắt buộc.** Ghi vào báo cáo để truy ngược được kết luận do luật nào sinh ra |
| `updatedUtc` | Thời điểm cập nhật |
| `notes` | Ghi chú tự do, không ảnh hưởng hành vi |
| `scanRoots` | Thư mục gốc sẽ duyệt tìm tên nghi vấn |
| `suspiciousNames` | Tên đặc trưng của công cụ kích hoạt trái phép |
| `legitimateTaskNames` | Task hợp lệ của Windows — **loại trừ để không báo động nhầm** |
| `hookDirectories` | Thư mục chứa file hook cần kiểm |
| `hookFiles` | File thay thế trực tiếp thành phần lõi bảo vệ bản quyền |
| `knownKmsHosts` | Máy chủ KMS công cộng đã biết, xuất hiện trong hosts file |
| `hostsInterferenceKeywords` | Từ khoá cho thấy hosts đang chặn máy chủ xác thực |

## Cập nhật luật

```bash
# 1. Sửa file, nhớ TĂNG version
vi src/Tsudev.Audit.Core/Rules/detection-rules.json

# 2. Kiểm chứng - bộ test bắt được bộ luật rỗng và sai định dạng
dotnet run --project tests/unittests -c Release
```

Với người dùng cuối chỉ cần cập nhật luật: tải file `detection-rules.json` mới,
đặt cạnh `swico.exe`. Xong.

## Vì sao `legitimateTaskNames` quan trọng

`SoftwareProtectionPlatform` là task **hợp lệ** của Windows dùng để gia hạn
license KMS trong doanh nghiệp. Kích hoạt qua KMS nội bộ là hoàn toàn hợp pháp
và rất phổ biến ở tổ chức lớn.

Báo động nhầm ở đây không phải phiền toái nhỏ: báo cáo của công cụ này có thể
được dùng làm căn cứ trong tranh chấp lao động hoặc thanh tra. Một kết luận sai
theo hướng buộc tội có hậu quả thật với người thật.

## Giới hạn — phải nói rõ trong báo cáo

Đây là quét theo **dấu hiệu đã biết**. Nó **không** phát hiện được 100% biến thể,
đặc biệt là bản đổi tên, bản tuỳ biến mới, hoặc kỹ thuật HWID không để lại dấu vết.

- **Không có phát hiện ≠ máy sạch tuyệt đối.**
- **Có phát hiện ≠ kết luận vi phạm** — tên file và tên service hoàn toàn có thể
  trùng lặp ngẫu nhiên. Cần xác minh thủ công.
