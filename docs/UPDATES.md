# Chức năng tự cập nhật

## Vì sao bắt buộc cập nhật

Kết luận của công cụ dựa trên **bộ luật phát hiện**. Một bộ luật lỗi thời có thể
bỏ sót dấu hiệu mới và cho ra kết luận "sạch" trên một máy thực sự có vấn đề —
đúng kiểu sai tệ nhất mà công cụ này có thể mắc.

Vì vậy khi phát hiện có bản mới, công cụ **chặn lần quét** cho tới khi cập nhật.

## Năm tình huống, năm cách xử lý

| Tình huống | Xử lý | Mã thoát |
|---|---|---|
| Đã là bản mới nhất | Quét bình thường | theo kết quả quét |
| **Có bản mới**, bản **đã cài**, chạy tương tác | Hộp thoại một nút **"Cập nhật"** → tải, xác minh, cài | `30` |
| **Có bản mới**, chạy `--silent` | **Không** hộp thoại, thoát ngay | `30` |
| **Có bản mới**, bản **portable** | **Không** hộp thoại, chỉ rõ chỗ tải bản portable mới | `30` |
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

### Vì sao bản portable được xử lý khác

Bản portable **vẫn bị chặn** — bộ luật cũ thì vẫn là bộ luật cũ, bất kể công cụ
được cài kiểu gì. Chỉ khác ở chỗ nó **không tự cài**.

Chạy file setup cho một bản portable là một cái bẫy: nó cài **một bản thứ hai**
vào `Program Files`, còn file `.exe` đang chạy — thường nằm trên USB — thì
**vẫn cũ**. Lần sau kỹ thuật viên cắm USB vào máy khác và chạy đúng file đó, họ
lại bị chặn tiếp. Mãi mãi, mà không hiểu vì sao.

Nên với bản portable, công cụ nói thẳng: tải bản `.zip` mới, giải nén, ghi đè.

**Cách nhận biết:** Inno Setup luôn đặt `unins000.exe` cạnh ứng dụng khi cài;
bản portable giải nén từ `.zip` thì không có tệp đó. Khi không chắc chắn, mặc
định coi là **portable** — đoán nhầm về phía đó chỉ gây phiền (phải tự tải), còn
đoán nhầm về phía kia thì tạo ra đúng vòng lặp vừa mô tả.

Mã: `InstallKindDetector` trong `src/Tsudev.Audit.Windows/UpdateAdapters.cs`;
quyết định nằm ở `UpdateChecker` trong Core nên **test được trên Linux**.

## Bảo mật: xác minh trước khi chạy

Tải một file `.exe` từ mạng rồi chạy nó với quyền Administrator mà không kiểm
tra gì là **đúng mô tả của một cuộc tấn công**. Quy trình cập nhật vì thế:

1. Đọc bản phát hành mới nhất từ GitHub API
2. Tải file cài đặt (`tsudev-swico_<phiên-bản>_x64-setup.exe`)
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

> Quy ước đầy đủ, kèm lý do và các trường hợp bị cấm: **`docs/VERSIONING.md`**.
> Phần dưới đây chỉ tóm tắt phần có liên quan tới chức năng cập nhật.

Số hiệu: `YY.M.DDNN`, trong đó `NN` là **thứ tự của bản phát hành trong ngày**,
đệm đủ hai chữ số và bắt đầu từ `01`:

| Phiên bản | Nghĩa |
|---|---|
| `26.8.1801` | bản thứ nhất ngày 18/08/2026 *(phát hành dưới tên cũ `26.8.18`)* |
| `26.8.1802` | bản **thứ hai** ngày 18/08/2026 *(tên cũ `26.8.18.2`)* |
| `26.8.1901` | bản thứ nhất ngày 19/08/2026 |
| `26.8.2001` | bản thứ nhất ngày 20/08/2026 |

Thứ tự so sánh: `26.8.1801` < `26.8.1802` < `26.8.1901` < `26.8.2001`.

Hai chữ số `NN` là bắt buộc về mặt kỹ thuật: không có chúng, hai bản dựng khác
nhau trong cùng một ngày sẽ mang **cùng một số hiệu** — điều không bao giờ được
phép xảy ra với phần mềm đã phát hành.

### Vì sao ngày và số thứ tự phải đệm đủ hai chữ số

Chức năng trên trang này đứng hay đổ hoàn toàn dựa vào **một phép so sánh**:

```csharp
if (latest.Version <= current)  → đang dùng bản mới nhất
else                            → chặn lại, bắt cập nhật
```

Phép so sánh đó chỉ đúng khi thứ tự số hiệu trùng với thứ tự thời gian phát hành.
Thành phần thứ ba được đọc bằng phép chia cho 100. Đệm đủ thì giá trị của nó luôn
bằng `DD × 100 + NN`, nên `1901 < 1902 < 2001` — đúng thứ tự. Bỏ đệm đi thì ngày 9
bản 1 thành `91`, đọc ngược lại ra **ngày 0 bản 91**, và tên file cài đặt sinh ra
từ đó là tên không ai tìm thấy.

Điều này được khoá lại bằng test quét **cả tháng** (124 số hiệu) — xem mục `13b`
trong `tests/unittests/Program.cs`.

### Đọc được cả dạng cũ

Hai bản `26.8.18` và `26.8.18.2` đã phát hành theo dạng cũ. Công cụ đọc được cả
hai dạng và quy về cùng một giá trị (`26.8.18` ≡ `26.8.1801`), và
`GitHubReleaseParser` nhận **cả hai** dạng tên tệp đính kèm.

> ⛔ **Chiều ngược lại thì không.** `swico.exe` của `26.8.18` và `26.8.18.2` đã
> nằm trên máy người dùng với bộ đọc **cũ** biên dịch sẵn bên trong; nó không đọc
> được `26.8.1901`. Hai bản đó rơi vào nhánh *không kiểm tra được* — **vẫn quét
> bình thường kèm ghi chú**, nhưng mất khả năng cập nhật bắt buộc. Cách gỡ:
> `docs/VERSIONING.md` mục 5.

## Kiến trúc

Theo đúng nguyên tắc Ports & Adapters của dự án:

| Thành phần | Nơi | Test được trên Linux |
|---|---|---|
| So sánh phiên bản (`VersionNumber`) | Core | ✅ |
| Quy ước đặt tên (`ReleaseName`) | Core | ✅ |
| Quyết định có chặn hay không (`UpdateChecker`) | Core | ✅ |
| Đọc JSON của GitHub (`GitHubReleaseParser`) | Core | ✅ |
| Tra mã băm (`ChecksumFile`) | Core | ✅ |
| Gọi HTTP, tải file (`GitHubUpdateFeed`, `UpdateInstaller`) | Windows | ❌ |
| Hộp thoại (`UpdatePrompt`) | Windows | ❌ |

Toàn bộ **logic quyết định** nằm trong Core và có test — chỉ phần chạm hệ thống
mới nằm ở lớp adapter.
