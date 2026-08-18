# Tuyên bố quyền riêng tư

**Áp dụng cho:** `tsudev SWICO` phiên bản 3.x (bản CLI)
**Cập nhật:** 18/08/2026

## Tóm tắt trong một câu

Phần mềm này **không gửi dữ liệu đi đâu cả**. Không có kết nối mạng, không có
đo lường từ xa, không có máy chủ.

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

## Dữ liệu đi đâu

**Chỉ ghi ra đĩa của chính máy đó**, vào thư mục bạn chỉ định (mặc định là thư
mục chứa file thực thi), dưới dạng HTML, JSON, XLSX và CSV.

Phần mềm **không** thực hiện bất kỳ kết nối mạng nào. Bạn có thể tự kiểm chứng:
chặn nó bằng tường lửa và quan sát — mọi chức năng vẫn hoạt động đầy đủ.

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
