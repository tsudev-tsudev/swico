# STATE — Trạng thái sống của dự án

> **File này là nguồn sự thật DUY NHẤT về "đang làm tới đâu".**
> Phiên Claude Code mới **BẮT BUỘC đọc file này đầu tiên**, trước cả README.
> Quy ước cập nhật: xem `docs/CONTINUITY.md`.

- **Cập nhật lần cuối:** 2026-08-18
- **Phiên hiện tại:** S001
- **Giai đoạn:** Phase 1 — Khôi phục khả năng build

---

## 1. Kết luận ngắn gọn về hiện trạng

Dự án **chưa build được**. Đây là sự thật quan trọng nhất và nó **mâu thuẫn với
README gốc** (README mô tả một solution hoàn chỉnh với 54 test đang xanh — điều
đó không đúng với nội dung thư mục thực tế).

Nguyên nhân gốc: mã nguồn đã bị "làm phẳng" — 9 file `.cs` nằm chung thư mục gốc
thay vì trong layout `src/` mà các `.csproj` đang trỏ tới, đồng thời **thiếu hẳn**
một số thành phần bắt buộc.

## 2. Bảng chốt: cái gì có, cái gì thiếu

| Thành phần | Trạng thái | Ghi chú |
|---|---|---|
| `AuditModels.cs` (Core.Models) | ✅ Có | 222 dòng |
| `SystemAbstractions.cs` (Core.Abstractions) | ✅ Có | 132 dòng, các port IWmiQuery/IRegistryReader/... |
| `LicenseCollectors.cs` (Core.Collectors) | ✅ Có | 225 dòng |
| `InventoryCollectors.cs` (Core.Collectors) | ✅ Có | 405 dòng |
| `ActivationRiskScanner.cs` (Core.Collectors) | ✅ Có | 320 dòng |
| `ReportBuilders.cs` (Core.Reports) | ✅ Có | 289 dòng |
| `FakeAdapters.cs` (Core.Testing) | ✅ Có | 118 dòng |
| `WindowsAdapters.cs` (Windows) | ✅ Có | 193 dòng, chưa từng chạy trên Windows thật |
| `Program.cs` (Cli) | ✅ Có | 311 dòng |
| **`Core.Rendering`** | ❌ **THIẾU HOÀN TOÀN** | `HtmlReportRenderer`, `XlsxWriter`, `DashboardBuilder` — Program.cs:86,87,118 gọi cả ba |
| **`Tsudev.Audit.Core.csproj`** | ❌ **THIẾU** | 2 csproj còn lại đều ProjectReference tới nó |
| **`tests/unittests/unittests.csproj`** | ❌ **THIẾU** | file test `Program.cs` bị lạc ở `mnt/user-data/outputs/...` |
| **`.sln`** | ❌ Thiếu | |
| **git repo** | ❌ Chưa init | |
| `Tsudev.Audit.Cli.csproj` | ✅ Có | |
| `Tsudev.Audit.Windows.csproj` | ✅ Có | |
| `app.manifest` | ✅ Có | requireAdministrator |
| `build.ps1` | ✅ Có | sẽ cần viết lại sau tái cấu trúc |

## 3. Quyết định đã chốt với người dùng (phiên S001)

Bốn quyết định này **đã được người dùng xác nhận**, không hỏi lại ở phiên sau:

1. **Lớp Rendering:** viết mới hoàn toàn từ đầu, khớp API mà `Program.cs` và
   `tests/unittests/Program.cs` đang gọi. XLSX **tự ghi OpenXML**, không thêm
   phụ thuộc NuGet (ClosedXML/EPPlus) — tránh vấn đề giấy phép thư viện.
2. **Đóng gói:** hai kênh song song —
   (a) **Installer EXE bằng Inno Setup 6**, và
   (b) **portable single-exe + publish lên winget**.
   *Không* làm MSI/WiX. *Không* làm MSIX (xung đột với `requireAdministrator`).
3. **Ký số:** **Azure Trusted Signing** (dịch vụ đám mây của Microsoft), tích hợp
   thẳng vào GitHub Actions, không giữ token vật lý.
4. **License:** **KHÔNG** làm hệ thống bản quyền/kích hoạt cho bản thân tool.
   Chữ "license" trong yêu cầu chỉ có nghĩa là **tính năng kiểm tra bản quyền
   Windows/Office** vốn đã có. Vẫn bổ sung file LICENSE mã nguồn + EULA cho installer.

## 4. Việc đang làm dở NGAY LÚC NÀY

Xem `docs/PLAN.md` để biết toàn bộ lộ trình, và danh sách task của phiên.
Trạng thái từng Phase được đánh dấu trong `docs/PLAN.md`.

## 5. Cạm bẫy đã biết — đọc để khỏi vấp lại

- Máy dev là **Linux**, không phải Windows. `dotnet` **không có sẵn trong PATH**;
  SDK được cài riêng vào `~/.dotnet` (xem `docs/CONTINUITY.md` mục "Môi trường").
- `net8.0-windows` **vẫn build được trên Linux** (không dùng WinForms/WPF), và
  `dotnet publish -r win-x64 --self-contained` cross-compile từ Linux cũng chạy.
  Nhưng **không thể chạy thử** file exe kết quả ở đây.
- `System.Management` (NuGet) biên dịch được trên Linux nhưng ném exception khi
  chạy — đây là lý do kiến trúc Ports & Adapters tồn tại, đừng "sửa" nó.
- README gốc **không đáng tin**; đối chiếu với file thật trước khi dựa vào nó.
- Thư mục `mnt/user-data/outputs/...` là rác từ môi trường cũ, chỉ chứa file test
  bị lạc. Lấy file ra rồi xoá cả cây thư mục `mnt/`.
