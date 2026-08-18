# DECISIONS — Đối chiếu artifact kế hoạch với phiên làm việc S001

> Nguồn đối chiếu: artifact **"Kế hoạch tái cấu trúc toàn diện — tsudev SWICO"**
> (bản thảo v1, 18/08/2026) — https://claude.ai/code/artifact/54a30a62-f884-4e8b-815c-04a8fa656d5b
>
> File này ghi lại chỗ nào artifact và phiên S001 **trùng khớp**, chỗ nào
> **mâu thuẫn**, và những gì artifact có mà phiên S001 **bỏ sót**.

---

## 1. Trùng khớp — không cần bàn lại

Artifact và khảo sát độc lập ở phiên S001 đi tới **cùng một chẩn đoán**, dù
được lập riêng rẽ. Điều này làm tăng độ tin cậy của cả hai:

| Điểm | Artifact | Phiên S001 |
|---|---|---|
| Thiếu `Core.csproj` | Chặn | ✅ Đã xác nhận & đã sửa |
| Mất toàn bộ `Core.Rendering` | Chặn, ước 800–1.200 dòng | ✅ Đã xác nhận; **thực tế viết lại ~840 dòng** |
| `tests/unittests` không có csproj, file lạc | Chặn | ✅ Đã xác nhận & đã sửa |
| 9–10 file `.cs` nằm phẳng, không `src/`, không `.sln` | Cao | ✅ Đã xác nhận & đã sửa |
| Chưa phải kho git — **nguyên nhân gốc gây mất mã** | Chặn | ✅ Đồng ý; đã `git init` là việc đầu tiên |
| `WindowsAdapters.cs` chưa từng chạy trên Windows | Cao | ✅ Đồng ý, chưa xử lý được |
| Chưa cài .NET SDK | Dễ xử lý | ✅ Đã cài `~/.dotnet` 8.0.424 |
| XLSX tự ghi OOXML, không thư viện ngoài | Đề xuất | ✅ Đã làm đúng vậy |
| Inno Setup + winget + bản portable | Đề xuất | ✅ Trùng lựa chọn của người dùng |
| Chuyển sang xUnit | GĐ 01 | ✅ Có trong Phase 4 của PLAN.md |

**Kết quả thực tế đã vượt mốc "Xong khi" của giai đoạn 00 trong artifact:**
artifact đặt điều kiện *"`dotnet build` xanh toàn solution và test báo 64/64 pass"*.
Đã đạt: `Build succeeded` + `64 PASS, 0 FAIL`.

---

## 2. ⚠️ MÂU THUẪN NGHIÊM TRỌNG — chữ ký số

**Đây là điểm quan trọng nhất trong toàn bộ tài liệu này.**

- **Phiên S001** (đầu phiên): người dùng chọn **Azure Trusted Signing**.
- **Artifact**: cảnh báo Azure Trusted Signing *"chỉ xác minh danh tính được cho
  tổ chức/cá nhân Mỹ – Canada, nên Việt Nam nhiều khả năng không đăng ký được"*,
  và đề xuất **SignPath Foundation** thay thế.

**Đã kiểm chứng độc lập bằng tài liệu Microsoft hiện hành (18/08/2026).
Artifact đúng ở điểm cốt lõi, và còn có thêm thông tin mới:**

1. **Dịch vụ đã đổi tên** thành **Azure Artifact Signing**
   (`learn.microsoft.com/azure/artifact-signing/`). Mọi tài liệu/action gọi tên
   "Trusted Signing" đều đang nói về dịch vụ này.
2. **Bắt buộc Azure subscription trả phí.** FAQ nói rõ: *"Artifact Signing doesn't
   support free, trial, or sponsored Azure subscriptions."*
3. **Có giới hạn quốc gia thật.** Danh sách các nước được xác minh danh tính
   **không có Việt Nam**; với *cá nhân* thì giới hạn còn chặt hơn (Mỹ/Canada).
4. **Không cấp chứng chỉ EV** — Microsoft nói rõ không có kế hoạch cấp.

**Hệ quả:** lựa chọn "Azure Trusted Signing" của phiên S001 **nhiều khả năng
không thực hiện được** với một cá nhân/tổ chức ở Việt Nam. Cần chọn lại.
Task #8 đã được đánh dấu là **bị chặn, chờ người dùng quyết**.

> Lưu ý: đây là kết luận từ tài liệu công khai. Trước khi bỏ hẳn phương án Azure,
> nếu bạn có pháp nhân đăng ký ở nước thuộc danh sách được hỗ trợ thì vẫn dùng
> được — chỉ bạn mới biết điều đó.

---

## 3. Mâu thuẫn về phạm vi và thứ tự

### 3.1 Phạm vi: artifact lớn hơn hẳn kế hoạch S001

Artifact có **8 giai đoạn**, gồm hai giai đoạn mở rộng mà `PLAN.md` của phiên
S001 **hoàn toàn không có**:

- **GĐ 06 — Agent chạy nền** (Windows Service), 1–2 tuần
- **GĐ 07 — Server tập trung + dashboard web** (ASP.NET Core + EF Core +
  SQLite/PostgreSQL, htmx, docker compose), 3–6 tuần

Đây không phải chi tiết nhỏ: hai giai đoạn này chiếm phần lớn khối lượng của
ước lượng *"3–4 tháng"* trong artifact. `PLAN.md` hiện dừng ở CLI + đóng gói +
ký số + tài liệu. **Cần người dùng xác nhận có giữ phạm vi lớn hay không.**

### 3.2 Thứ tự: artifact xếp "kiểm chứng trên Windows" sớm hơn nhiều

- **Artifact:** GĐ 02 — ngay sau khi chuẩn hoá nền, **trước** pháp lý/đóng gói/ký số.
- **PLAN.md S001:** Phase 9 — cuối cùng.

**Artifact có lý hơn.** Đóng gói và ký số một chương trình chưa ai biết nó chạy
đúng hay không là đầu tư vào thứ có thể phải làm lại. Nên **sửa `PLAN.md` theo
thứ tự của artifact**, với điều kiện người dùng có máy Windows.

---

## 4. Artifact có mà phiên S001 bỏ sót

Những mục dưới đây **cần bổ sung vào `PLAN.md`**:

| # | Hạng mục | Vì sao quan trọng |
|---|---|---|
| 1 | **Chốt MỘT tên sản phẩm** | Đang có ba tên lệch nhau: thư mục `tsudev-swico`, assembly `tsudev-audit`, manifest `tsudev.SystemAudit`. Tên này đi vào installer, winget, chữ ký số, hồ sơ pháp lý — **đổi về sau rất tốn**. Chưa chốt thì chưa làm installer được. |
| 2 | **Báo nhầm của phần mềm diệt virus** | Rủi ro đặc thù và nghiêm trọng: công cụ đòi quyền Administrator, đọc registry bản quyền, quét dấu hiệu crack, đọc trạng thái Defender — mô tả gần trùng khớp phần mềm độc hại. Cần nộp VirusTotal + cổng báo nhầm trước mỗi bản phát hành. |
| 3 | **Cân nhắc NativeAOT** thay `PublishSingleFile` | Artifact nêu đúng: single-file tự giải nén hay bị diệt virus báo nhầm hơn hẳn. Thực tế đo được ở phiên này: **exe hiện 34 MB** — NativeAOT sẽ nhỏ hơn nhiều và khởi động nhanh hơn. |
| 4 | **Tách luật phát hiện ra file dữ liệu có phiên bản** (`Core/Rules/`) | Nếu mở mã nguồn, người viết crack đọc được đúng 6 hạng mục dấu hiệu và các ngưỡng điểm. Tách ra dữ liệu cập nhật độc lập với exe làm việc né luật cũ mất giá trị. |
| 5 | **SBOM (CycloneDX)** + **checksum SHA-256** + **build tái lập được** | Bù đắp khi chưa có chữ ký số, và là thông lệ phát hành nghiêm túc. |
| 6 | **EULA + Tuyên bố quyền riêng tư** | Công cụ đưa ra *phán quyết* về tính hợp lệ bản quyền — kết quả có thể bị dùng làm căn cứ trong tranh chấp lao động/thanh tra. EULA phải nói rõ đây là tham khảo kỹ thuật, không phải kết luận pháp lý. Công cụ cũng đọc sê-ri phần cứng, danh sách phần mềm → dữ liệu nhạy cảm, phải nói rõ nó đi đâu. |
| 7 | **Logging có cấu trúc** (`Microsoft.Extensions.Logging`) | Thay `Console.WriteLine` rải rác; cần cho tình huống hỗ trợ từ xa. |
| 8 | **Cơ chế tự cập nhật** | Kiểm tra GitHub Releases, xác minh chữ ký/checksum trước khi thay thế. |
| 9 | **`THIRD-PARTY-NOTICES.md`**, header SPDX | Kèm theo lựa chọn giấy phép. |

---

## 5. Đã hoà giải xong — không còn là câu hỏi mở

| Vấn đề | Trạng thái |
|---|---|
| **Lớp Rendering có bản sao không?** (artifact Q1) | ✅ **Hết hiệu lực.** Người dùng xác nhận không còn bản sao; đã viết lại xong, 54/64 test pass. |
| **License key thương mại** (artifact Điều chỉnh 01) | ✅ **Đã giải quyết.** Người dùng chốt ở phiên S001: *không* làm hệ thống license key, "license" chỉ là tính năng kiểm tra bản quyền Windows. Trùng với đề xuất của artifact. |
| **`ILicenseGate` giữ điểm nối?** | ⬜ Còn nhỏ. Đề xuất **bỏ** — khi đã chốt không làm license key thì thêm interface cho một tương lai chưa chắc xảy ra là phức tạp thừa. Dễ thêm sau nếu cần. |

---

## 6. ⬜ Câu hỏi còn mở — chặn các giai đoạn sau

1. **Ký số:** SignPath Foundation (miễn phí, **buộc mã nguồn mở công khai**) hay
   mua chứng chỉ OV/EV thương mại (giữ mã nguồn đóng, tốn tiền)?
2. **Mã nguồn mở hay đóng?** Gắn chặt với câu 1. Đánh đổi: mở → ký số miễn phí +
   uy tín, nhưng công khai luật phát hiện crack.
3. **Giấy phép nào?** Apache-2.0 (khuyến nghị của artifact, có bảo vệ nhãn hiệu)
   / MIT / GPL-3.0 — chỉ áp dụng nếu chọn mở mã nguồn.
4. **Phạm vi:** có giữ Agent (GĐ 06) và Server + dashboard (GĐ 07) không?
5. **Tên sản phẩm chính thức là gì?** "SWICO" viết tắt của gì?
6. **Có máy Windows thật để kiểm chứng không?** Quyết định thứ tự các Phase.

---

## 7. ✅ Chốt tại phiên S001 (sau khi đối chiếu artifact)

Bốn quyết định dưới đây **đã được người dùng xác nhận**, thay thế mọi lựa chọn
trước đó. Phiên sau **không hỏi lại**.

| # | Quyết định | Thay thế cho | Hệ quả kéo theo |
|---|---|---|---|
| D1 | **Ký số qua SignPath Foundation** | ~~Azure Trusted Signing~~ (không dùng được từ VN) | **Repo BẮT BUỘC công khai + giấy phép OSS được công nhận.** Nộp hồ sơ sớm, duyệt mất vài ngày–vài tuần. |
| D2 | **Phạm vi: chỉ CLI** — hoàn thiện + đóng gói | Artifact GĐ 06 (Agent) và GĐ 07 (Server + dashboard) | **Bỏ** Agent và Server khỏi kế hoạch. Ước lượng còn ~2–4 tuần thay vì 3–4 tháng. Có thể mở rộng sau. |
| D3 | **Có máy Windows thật, người dùng tự chạy thử** | — | **Đẩy kiểm chứng Windows lên sớm** theo thứ tự của artifact. Tôi soạn kịch bản kiểm thử từng bước để người dùng chạy và báo kết quả về. |
| D4 | **Tên sản phẩm: `tsuowlit SWICO`** | ~~tsudev-swico~~ / ~~tsudev-audit~~ / ~~tsudev.SystemAudit~~ | Tên mới hoàn toàn, không nằm trong ba phương án đã nêu. Cần làm rõ mức độ đổi tên (xem mục 8). |

### Hệ quả của D1 — mã nguồn mở là điều kiện, không phải tuỳ chọn

Chọn SignPath Foundation nghĩa là **chấp nhận công khai mã nguồn**, gồm cả
`ActivationRiskScanner.cs` với 6 hạng mục dấu hiệu và các ngưỡng tính điểm rủi ro.
Đây chính là đánh đổi mà artifact đã nêu ở "Điều chỉnh 02".

Biện pháp bù trừ (theo artifact, nay thành việc bắt buộc):
**tách luật phát hiện ra file dữ liệu có phiên bản** (`Core/Rules/`), cập nhật
độc lập với bản exe, để việc né luật cũ không có giá trị lâu dài.

### Hệ quả của D2 — những gì bị loại khỏi phạm vi

Ghi rõ để phiên sau không vô tình làm lại: **không** xây `Tsudev.Audit.Agent`,
**không** xây `Tsudev.Audit.Server`, **không** dựng dashboard web/docker compose.
Trang tổng hợp vẫn là `tsudev-tong-hop.html` tĩnh như hiện tại.

## 8. ⬜ Còn phải làm rõ: đổi tên sâu tới đâu

Tên `tsuowlit SWICO` là tên **mới hoàn toàn**. Cần chốt phạm vi ảnh hưởng trước
khi làm installer, vì mọi thứ phía sau neo vào đây:

- Namespace `Tsudev.Audit.*` — đổi hay giữ? (đổi = sửa toàn bộ file + test)
- Tên assembly `tsudev-audit.exe` — đổi hay giữ?
- Tên miền: bộ test hiện **khẳng định** báo cáo HTML phải chứa `https://tsudev.com`
  (`tests/unittests/Program.cs:174`). Nếu đổi sang tên miền khác thì phải sửa test.
- Giấy phép mã nguồn: chưa chốt. Đề xuất **Apache-2.0** theo artifact — có điều
  khoản bảo vệ nhãn hiệu, nghĩa là người khác fork được mã nhưng **không được
  phát hành dưới tên của bạn**. Với một tên thương hiệu mới toanh, điều đó đáng giá.
