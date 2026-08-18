# Thông báo về thành phần của bên thứ ba

`tsuowlit SWICO` được xây dựng với chủ trương **giữ số phụ thuộc ở mức tối
thiểu**. Mỗi gói thêm vào là thêm một giấy phép phải rà soát, thêm một nguồn lỗ
hổng phải theo dõi, và thêm dung lượng cho file thực thi đơn.

Đó cũng là lý do bộ ghi file `.xlsx` được **tự viết theo chuẩn OOXML** thay vì
dùng ClosedXML hay EPPlus, và trang báo cáo HTML không nạp bất kỳ thư viện
JavaScript nào từ bên ngoài.

## Phụ thuộc trực tiếp

| Gói | Phiên bản | Giấy phép | Dùng ở đâu | Vì sao cần |
|---|---|---|---|---|
| `System.Management` | 8.0.0 | MIT | `Tsudev.Audit.Windows` | Gói chính thức của Microsoft để truy vấn WMI/CIM. Không có cách nào khác để đọc thông tin phần cứng và trạng thái Defender trên Windows. |

## Nền tảng thực thi

Bản phát hành là **self-contained**: thư viện thực thi .NET được đóng kèm nên
máy đích không cần cài .NET Runtime.

| Thành phần | Giấy phép |
|---|---|
| .NET Runtime & Base Class Library (Microsoft) | MIT |

## Không có phụ thuộc nào khác

Bộ kiểm thử, lớp render HTML/XLSX/Dashboard, và toàn bộ logic nghiệp vụ **không
dùng gói bên thứ ba nào**.

## Cách sinh lại danh sách này

```bash
dotnet list Tsudev.SystemAudit.sln package --include-transitive
```

SBOM chuẩn CycloneDX được sinh tự động và đính kèm mỗi bản phát hành (xem
`.github/workflows/release.yml`).
