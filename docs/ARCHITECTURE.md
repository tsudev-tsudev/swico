# ARCHITECTURE — Quyết định kiến trúc của riêng repo này

> Bắt buộc theo `docs/PROJECT_STRUCTURE.md`. File này **không chép lại** nội dung
> đã có ở nơi khác — theo `AGENTS.md` mục 1, mỗi tri thức ghi **một lần duy nhất**,
> các phiên sau chỉ tham chiếu đường dẫn.

## 1. Mô hình: Ports & Adapters

| Tầng | Thư mục | Nền tảng | Test trên Linux |
|---|---|---|---|
| Nghiệp vụ thuần | `src/Tsudev.Audit.Core/` | `net8.0` | ✅ |
| Adapter hệ thống | `src/Tsudev.Audit.Windows/` | `net8.0-windows` | ❌ |
| Điểm vào | `src/Tsudev.Audit.Cli/` | `net8.0-windows` | ❌ |

Vì sao tách, và chi tiết từng thư mục con: [`README.md`](../README.md) mục *Kiến trúc*.

## 2. Bốn luật bất di bất dịch

Vi phạm bất kỳ luật nào dưới đây đều đã từng gây lỗi thật trong dự án này.
Lý do đầy đủ + lịch sử vi phạm: [`docs/STATE.md`](STATE.md) mục 4.3.

| Luật | Hệ quả nếu vi phạm |
|---|---|
| **Logic thuần phải nằm trong Core**, không nằm ở adapter | Bộ test chạy trên Linux không với tới được (đã vấp 2 lần ở phiên S001) |
| **Mọi mã chạm mạng nằm gọn trong `src/Tsudev.Audit.Windows/UpdateAdapters.cs`** | `PRIVACY.md` mời người dùng tự kiểm chứng bằng cách đọc đúng file đó |
| **Mọi chỗ đọc/ghi JSON đi qua `Core/Serialization/AuditJson.cs`** | Mã sinh lúc biên dịch; gọi `JsonSerializer` trực tiếp sẽ hỏng khi cắt tỉa |
| **Luật phát hiện là dữ liệu có phiên bản** (`Core/Rules/`), không phải mã | Xem [`docs/DETECTION-RULES.md`](DETECTION-RULES.md) |

## 3. Vì sao `src/` KHÔNG theo cây mẫu của `PROJECT_STRUCTURE.md`

`docs/PROJECT_STRUCTURE.md` mô tả cây `src/components|features|services|utils` —
cây đó dành cho project Web/Electron của hệ sinh thái. Repo này là **.NET solution**
nên `src/` chia theo **project biên dịch** (`Tsudev.Audit.Core` / `.Windows` / `.Cli`),
đúng chuẩn .NET và đúng ranh giới Ports & Adapters ở mục 1.

Giữ nguyên tinh thần của quy ước: mỗi file một trách nhiệm, hàm dùng ≥ 2 nơi thì
tách ra dùng chung, tên theo chuẩn ngôn ngữ (C# = `PascalCase`).

> Còn tồn đọng đúng theo quy ước "file > 400 dòng phải cân nhắc tách":
> `tests/unittests/Program.cs` (**1049 dòng**), `InventoryCollectors.cs` (4 class),
> `LicenseCollectors.cs` (5 class). Đã nằm trong hàng đợi — `docs/STATE.md` mục 3.5.

## 4. Ràng buộc bên ngoài chi phối kiến trúc

| Ràng buộc | Hệ quả | Chi tiết |
|---|---|---|
| Ký số qua SignPath Foundation | Repo **bắt buộc** công khai + Apache-2.0 | [`docs/SIGNING.md`](SIGNING.md) |
| Cắt tỉa (`PublishTrimmed`) làm **mất dữ liệu WMI âm thầm** | Cấm bật; NativeAOT gần như chắc chắn hỏng cùng lý do | [`docs/STATE.md`](STATE.md) mục 4.1 |
| Máy dev là **Linux**, không có Windows/Excel/Inno Setup/winget | Mọi thứ cần Windows phải qua CI hoặc nhờ người dùng | [`docs/CONTINUITY.md`](CONTINUITY.md) mục 1 |

## 5. Bản đồ tài liệu đầy đủ

[`docs/STATE.md`](STATE.md) mục 5.
