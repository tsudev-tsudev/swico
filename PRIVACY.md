# Tuyên bố quyền riêng tư

**Áp dụng cho:** `tsudev SWICO` phiên bản 3.x (bản CLI)
**Cập nhật:** 18/08/2026

## Tóm tắt trong một câu

Phần mềm này **không gửi dữ liệu nào của máy bạn đi đâu cả**. Nó thực hiện đúng
**một** kết nối mạng — hỏi GitHub xem đã có phiên bản mới chưa — và kết nối đó
tắt được bằng `--no-update-check`.

> **Thay đổi so với bản trước:** phiên bản 26.8.18 tuyên bố *"không kết nối
> Internet vì bất kỳ mục đích nào"*. Từ bản kế tiếp, tuyên bố đó **không còn
> đúng** vì đã bổ sung chức năng tự cập nhật. Mục "Kết nối mạng" dưới đây mô tả
> chính xác điều gì được gửi đi.

## Vì sao cần tuyên bố này

Công cụ đọc số sê-ri phần cứng, danh sách phần mềm đã cài, trạng thái Defender
và thông tin bản quyền. Đó là dữ liệu nhạy cảm về một máy tính cụ thể. Khi một
chương trình đọc những thứ đó **và** đòi quyền Administrator, người dùng có
quyền yêu cầu biết chính xác dữ liệu đi đâu.

## Dữ liệu được đọc

| Nhóm | Cụ thể | Nguồn |
|---|---|---|
| Bản quyền | Trạng thái kích hoạt Windows/Office, một phần mã sản phẩm, kênh cấp phép | WMI, `slmgr.vbs`, `ospp.vbs`, registry |
| Phần cứng | Máy, bo mạch chủ, CPU, RAM, ổ đĩa, số sê-ri | WMI |
| Phần mềm | Danh sách chương trình đã cài, phiên bản, ngày cài | Registry |
| Bảo mật | Trạng thái Defender, lịch sử phát hiện, tác vụ theo lịch đáng ngờ | WMI |
| Toàn vẹn | Kết quả DISM/SFC (chỉ khi được yêu cầu) | Tiến trình hệ thống |
| Định danh | Tên máy, tên miền, tên người dùng đang đăng nhập | Biến môi trường |

## Kết nối mạng — đúng một, và chỉ một

Khi khởi động, công cụ gọi **một** yêu cầu GET tới:

```
https://api.github.com/repos/tsudev-tsudev/swico/releases/latest
```

**Được gửi đi** (không thể tránh với bất kỳ yêu cầu HTTP nào):

- Địa chỉ IP công cộng của máy
- Chuỗi nhận dạng `tsudev-SWICO/<phiên-bản>` — cho GitHub biết phiên bản đang dùng

**KHÔNG được gửi đi:** tên máy, tên người dùng, số sê-ri phần cứng, danh sách
phần mềm, trạng thái bản quyền, kết quả quét — **không một dữ liệu nào** trong
số công cụ thu thập.

Nếu bạn bấm "Cập nhật", công cụ tải thêm file cài đặt và file `SHA256SUMS.txt`
từ cùng bản phát hành đó, rồi **đối chiếu mã băm trước khi chạy**.

### Tắt hoàn toàn

```
swico.exe --no-update-check
```

Khi tắt, công cụ **không thực hiện bất kỳ kết nối mạng nào**.

### Tự kiểm chứng

Toàn bộ mã chạm tới mạng nằm gọn trong **một file**:
`src/Tsudev.Audit.Windows/UpdateAdapters.cs`. Bạn có thể đọc hết trong vài phút.
Hoặc chặn công cụ bằng tường lửa và quan sát — nó vẫn quét bình thường, chỉ ghi
một ghi chú rằng chưa đối chiếu được phiên bản.

## Dữ liệu quét đi đâu

**Chỉ ghi ra đĩa của chính máy đó**, vào thư mục bạn chỉ định (mặc định là thư
mục chứa file thực thi), dưới dạng HTML, JSON, XLSX và CSV.

Kết quả quét **không bao giờ** rời khỏi máy. Kết nối duy nhất của công cụ là
kiểm tra phiên bản, mô tả ở mục trên.

## Sau khi ghi ra đĩa

Từ thời điểm đó, dữ liệu nằm dưới quyền kiểm soát của bạn. Nếu bạn copy thư mục
kết quả sang máy khác hoặc gửi cho người khác, **bạn** là người chịu trách nhiệm
với dữ liệu đó. Hãy nhớ rằng báo cáo chứa số sê-ri phần cứng và toàn bộ danh
sách phần mềm của máy.

## Nếu phạm vi thay đổi

Nếu về sau có phiên bản agent hoặc máy chủ tập trung khiến dữ liệu rời khỏi máy
người dùng, tài liệu này **bắt buộc phải được cập nhật trước** khi phiên bản đó
phát hành. Ở phạm vi hiện tại (chỉ CLI), điều đó chưa xảy ra.

## Tự kiểm chứng

Mã nguồn công khai. Mọi lệnh gọi hệ thống tập trung tại đúng một file —
`src/Tsudev.Audit.Windows/WindowsAdapters.cs` — chính là để việc rà soát bảo mật
kiểu này làm được nhanh chóng.
