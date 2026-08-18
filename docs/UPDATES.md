# Chức năng tự cập nhật

## Vì sao bắt buộc cập nhật

Kết luận của công cụ dựa trên **bộ luật phát hiện**. Một bộ luật lỗi thời có thể
bỏ sót dấu hiệu mới và cho ra kết luận "sạch" trên một máy thực sự có vấn đề —
đúng kiểu sai tệ nhất mà công cụ này có thể mắc.

Vì vậy khi phát hiện có bản mới, công cụ **chặn lần quét** cho tới khi cập nhật.

## Ba tình huống, ba cách xử lý

| Tình huống | Xử lý | Mã thoát |
|---|---|---|
| Đã là bản mới nhất | Quét bình thường | theo kết quả quét |
| **Có bản mới**, chạy tương tác | Hộp thoại một nút **"Cập nhật"** → tải, xác minh, cài | `30` |
| **Có bản mới**, chạy `--silent` | **Không** hộp thoại, thoát ngay | `30` |
| **Không kiểm tra được** | ⚠️ **Quét bình thường** kèm ghi chú | theo kết quả quét |

### Vì sao "không kiểm tra được" KHÔNG chặn

Đây là quyết định thiết kế quan trọng nhất của chức năng này.

Công cụ được dùng đúng ở những nơi mạng bị hạn chế nhất — máy trong mạng cách ly,
máy doanh nghiệp chặn GitHub, máy không nối mạng. Nếu "không kiểm tra được" cũng
chặn luôn, công cụ sẽ **vô dụng chính ở nơi nó cần thiết nhất**, và người dùng
không có cách nào vượt qua.

Nên chỉ chặn khi đã **xác định chắc chắn** có bản mới hơn. Mọi trường hợp không
chắc chắn đều cho đi tiếp, kèm ghi chú trong báo cáo.

### Vì sao `--silent` không hiện hộp thoại

`--silent` dùng cho triển khai hàng loạt qua GPO/RMM. Một hộp thoại ở đó sẽ
**treo vô thời hạn** — không ai ngồi bấm nút. Thay vào đó, thoát với mã `30` để
hệ thống triển khai tự xử lý.

## Bảo mật: xác minh trước khi chạy

Tải một file `.exe` từ mạng rồi chạy nó với quyền Administrator mà không kiểm
tra gì là **đúng mô tả của một cuộc tấn công**. Quy trình cập nhật vì thế:

1. Đọc bản phát hành mới nhất từ GitHub API
2. Tải `swico-setup-<phiên-bản>.exe`
3. Tải `SHA256SUMS.txt` từ **cùng bản phát hành đó**
4. **Đối chiếu SHA-256.** Không khớp → xoá file, dừng lại, báo lỗi
5. Chỉ khi khớp mới chạy file cài đặt

### Giới hạn phải nói rõ

`SHA256SUMS.txt` **hiện chưa được ký**. Việc đối chiếu chặn được file hỏng hoặc
tải thiếu, nhưng **không** chặn được kẻ đã chiếm quyền phát hành trên GitHub —
họ có thể thay cả file lẫn checksum.

Khi có chữ ký Authenticode (đang chờ SignPath), phải bổ sung kiểm tra chữ ký của
chính file `.exe`. Đó mới là hàng rào thật sự.

## Quyền riêng tư

Đây là **lần duy nhất** công cụ kết nối Internet. Không có dữ liệu nào của máy
được quét bị gửi đi — chi tiết trong `PRIVACY.md`.

Toàn bộ mã chạm tới mạng nằm gọn trong **một file**:
`src/Tsudev.Audit.Windows/UpdateAdapters.cs`.

## Tắt kiểm tra cập nhật

```powershell
swico.exe --no-update-check
```

Khi tắt, công cụ **không thực hiện bất kỳ kết nối mạng nào**. Phù hợp với:

- máy không nối mạng hoặc mạng cấm ra ngoài
- đội ngũ tự quản lý cập nhật qua hệ thống triển khai riêng
- kiểm thử hồi quy cần cố định một phiên bản

## Đánh số phiên bản

CalVer `yy.M.d`, thêm thành phần thứ tư khi phải phát hành lại trong cùng ngày:

| Phiên bản | Nghĩa |
|---|---|
| `26.8.18` | phát hành ngày 18/08/2026 |
| `26.8.18.1` | bản phát hành **thứ hai** trong ngày 18/08/2026 |
| `26.8.20` | phát hành ngày 20/08/2026 |

Thứ tự so sánh: `26.8.18` < `26.8.18.1` < `26.8.19` < `26.8.20`.

Thành phần thứ tư là bắt buộc về mặt kỹ thuật: không có nó, hai bản dựng khác
nhau trong cùng một ngày sẽ mang **cùng một số hiệu** — điều không bao giờ được
phép xảy ra với phần mềm đã phát hành.

## Kiến trúc

Theo đúng nguyên tắc Ports & Adapters của dự án:

| Thành phần | Nơi | Test được trên Linux |
|---|---|---|
| So sánh phiên bản (`VersionNumber`) | Core | ✅ |
| Quyết định có chặn hay không (`UpdateChecker`) | Core | ✅ |
| Đọc JSON của GitHub (`GitHubReleaseParser`) | Core | ✅ |
| Tra mã băm (`ChecksumFile`) | Core | ✅ |
| Gọi HTTP, tải file (`GitHubUpdateFeed`, `UpdateInstaller`) | Windows | ❌ |
| Hộp thoại (`UpdatePrompt`) | Windows | ❌ |

Toàn bộ **logic quyết định** nằm trong Core và có test — chỉ phần chạm hệ thống
mới nằm ở lớp adapter.
