# Ký số — SignPath Foundation

## Vì sao không dùng Azure Trusted Signing

Đầu phiên S001 phương án được chọn là Azure Trusted Signing. **Đã kiểm chứng lại
bằng tài liệu Microsoft hiện hành (18/08/2026) và phải loại bỏ phương án này:**

| Điểm | Thực tế |
|---|---|
| Tên gọi | Dịch vụ **đã đổi tên** thành **Azure Artifact Signing** |
| Subscription | **Bắt buộc trả phí.** FAQ nói rõ: không hỗ trợ free, trial hay sponsored |
| Quốc gia | Danh sách được xác minh danh tính **không có Việt Nam**. Với cá nhân, giới hạn còn chặt hơn (Mỹ/Canada) |
| Chứng chỉ EV | **Không cấp**, và không có kế hoạch cấp |

Nếu bạn có pháp nhân đăng ký ở nước thuộc danh sách được hỗ trợ thì phương án
này vẫn dùng được — nhưng đó là điều chỉ bạn xác minh được.

## Phương án đã chốt: SignPath Foundation

Cấp chứng chỉ ký số **OV miễn phí** cho dự án mã nguồn mở. Khoá riêng lưu trong
HSM, tích hợp thẳng vào CI/CD.

### Điều kiện — đây là ràng buộc thật, không thương lượng được

1. **Repo phải công khai.**
2. **Phải có giấy phép mã nguồn mở được công nhận** → đã chọn **Apache-2.0**.
3. Phải đã có bản phát hành ở dạng cần ký.
4. Chức năng phải được mô tả rõ ràng trên trang tải về.
5. **Mọi thành viên phải bật xác thực đa yếu tố** cho cả SignPath lẫn GitHub.

### Cái giá phải trả — nói rõ để không bất ngờ

Mã nguồn mở nghĩa là **toàn bộ luật phát hiện crack trong
`ActivationRiskScanner.cs` sẽ công khai** — 6 hạng mục dấu hiệu và các ngưỡng
tính điểm rủi ro, cho chính người viết crack đọc và né.

Biện pháp bù trừ **bắt buộc phải làm**: tách luật phát hiện ra file dữ liệu có
phiên bản (`Core/Rules/`), cập nhật độc lập với bản exe, để việc né luật cũ
không giữ được giá trị lâu.

## Các bước thiết lập

### 1. Nộp hồ sơ — làm SỚM

Thời gian duyệt từ vài ngày tới vài tuần. **Nộp trước, làm việc khác trong lúc
chờ.** Đừng để tới sát ngày phát hành mới nộp.

Nộp tại: https://signpath.org/apply

Chuẩn bị sẵn: URL repo công khai, giấy phép Apache-2.0 (đã có), mô tả chức năng,
và một bản phát hành thử.

### 2. Cấu hình phía SignPath

| Mục | Giá trị dùng trong `release.yml` |
|---|---|
| Project slug | `swico` |
| Signing policy slug | `release-signing` |
| Artifact configuration | `exe` và `installer` |

### 3. Cấu hình phía GitHub

| Loại | Tên | Nội dung |
|---|---|---|
| Secret | `SIGNPATH_API_TOKEN` | Token API do SignPath cấp |
| Variable | `SIGNPATH_ORGANIZATION_ID` | ID tổ chức trên SignPath |

## Trong lúc chờ duyệt

`release.yml` **vẫn chạy trọn vẹn khi chưa có secret** — các bước ký tự động bị
bỏ qua nhờ điều kiện `SIGNPATH_CONFIGURED`. Bản phát hành ra đời không có chữ ký
nhưng vẫn có:

- **SHA-256 công bố** (`SHA256SUMS.txt`) để người dùng tự đối chiếu
- **SBOM CycloneDX** đính kèm

Nghĩa là không có gì bị chặn: cứ phát hành, ký bổ sung khi hồ sơ được duyệt.

## Điểm kỹ thuật đáng lưu ý trong pipeline

1. **Ngữ cảnh `secrets` không dùng được trong `if:` ở cấp step.** Phải quy đổi
   thành biến môi trường ở cấp job (`SIGNPATH_CONFIGURED`) rồi mới kiểm tra.
2. **SignPath đọc file cần ký từ GitHub artifact**, nên phải `upload-artifact`
   trước và truyền đúng `artifact-id` mà bước đó trả về — không phải `run_id`.
3. **Ký exe TRƯỚC, đóng gói installer SAU**, để file exe nằm trong bộ cài là
   file đã có chữ ký.
4. **Bắt buộc kiểm tra dấu thời gian**, không chỉ kiểm tra trạng thái chữ ký.
   Chữ ký thiếu dấu thời gian sẽ hết hiệu lực khi chứng chỉ hết hạn — lỗi này
   chỉ lộ ra sau nhiều tháng nên phải chặn ngay trong CI.

## Kỳ vọng đúng về SmartScreen

**Chữ ký số hợp lệ KHÔNG xoá ngay cảnh báo SmartScreen.** Uy tín tích luỹ dần
theo lượt tải. Microsoft nói rõ điều này trong tài liệu chính thức. Đừng hứa với
người dùng rằng ký xong là hết cảnh báo.

## Phần mềm diệt virus báo nhầm

Rủi ro đặc thù của **đúng loại công cụ này**: một chương trình đòi quyền
Administrator, đọc registry bản quyền, quét dấu hiệu crack và đọc trạng thái
Defender có mô tả hành vi gần trùng khớp với phần mềm độc hại.

Quy trình bắt buộc trước mỗi bản phát hành:

1. Nộp lên VirusTotal, ghi lại số hãng báo động.
2. Nếu có báo động: gửi mẫu qua cổng báo nhầm của Microsoft Defender
   (https://www.microsoft.com/wdsi/filesubmission) và các hãng liên quan.

Chữ ký số giúp giảm mạnh vấn đề này nhưng **không xoá hẳn**. Cần xử lý liên tục,
không phải làm một lần.

Cân nhắc thêm: chuyển từ `PublishSingleFile` sang **NativeAOT**. Single-file tự
giải nén là mẫu hành vi hay bị báo nhầm. NativeAOT còn cho file nhỏ hơn nhiều —
bản hiện tại **34 MB**.
