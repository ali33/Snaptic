# Snaptic

App tray Windows: chụp vùng màn hình → nhận diện nội dung → hành động theo ngữ cảnh.
Kèm chiều ngược lại: nhập text → tạo mã QR/barcode.

## Giới thiệu

**Snaptic** là phần mềm được phát triển và trân trọng dành tặng đồng chí
**Vũ Diệu Huyền – UBND xã Đan Phượng**.

Phần mềm hỗ trợ chụp nhanh vùng màn hình, nhận diện nội dung và tạo mã QR, góp phần
nâng cao sự thuận tiện và hiệu quả trong công việc hằng ngày.

Điểm khác biệt so với ShareX / Lightshot / Snipping Tool không nằm ở việc chụp, mà ở
chỗ **app hiểu được nội dung nó vừa chụp**.

## Chức năng

- **Chụp vùng màn hình** — `Ctrl+Alt+Q` (đổi được). Màn hình được **đóng băng trước**
  rồi mới cho khoanh, nên chụp được cả menu đang mở.
- **Nhận diện** — QR, barcode 1D, OCR text. Nút chỉ hiện khi nhận ra được.
- **Hành động** — Chụp lại · Copy ảnh · Save PNG · Copy Data URI · Copy nội dung mã ·
  Copy text OCR · Mở link
- **Tạo mã** — QR, Code128, EAN-13, UPC-A, Code39. Ảnh cập nhật ngay khi gõ.
- **Khởi động cùng Windows** — mặc định bật ở lần chạy đầu, tắt được trong Cài đặt.

## Giới hạn đã biết

- **Chỉ Windows.** Kiến trúc giữ đường sang macOS nhưng chưa hiện thực.
- **OCR không có tiếng Việt.** Windows không phát hành gói OCR tiếng Việt — đã kiểm
  chứng, xem [spec §8 R1](docs/superpowers/specs/2026-07-15-snaptic-design.md). Mặc
  định tiếng Anh; cài thêm được 34 ngôn ngữ khác qua Windows Settings.

  Đáng lưu ý hơn: chữ Việt **không dấu** thì engine tiếng Anh đọc **chính xác** (nó chỉ
  là chữ Latin). Chữ **có dấu** thì không hỏng hẳn mà ra **sai trông hợp lý** —
  "Tiếng Việt có dấu" → `Tiéng Viét cé dä'u`. Nguy hiểm hơn hỏng hẳn vì dễ tưởng đúng.
- **Mỗi ảnh đọc một mã.** Thao tác khoanh vùng đã tự chọn ra thứ cần rồi.
- **Chỉ mở link http/https.** Mã QR là dữ liệu từ bên ngoài; `file://`, `javascript:`,
  `steam://`, UNC đều bị chặn có chủ đích. Text vẫn copy được bình thường.

## Yêu cầu

- Windows 10 build 19041 trở lên
- .NET 10 SDK

## Chạy

```bash
dotnet run --project src/Snaptic.App
```

Icon hiện ở khay hệ thống. Windows mặc định **giấu icon tray mới** vào vùng overflow —
bấm dấu `^` cạnh đồng hồ nếu không thấy.

## Test

```bash
dotnet test
```

173 test cho phần logic. Tầng `Snaptic.Windows` (chụp, clipboard, phím tắt, OCR) không
test tự động được vì cần màn hình và OS thật — xem [checklist kiểm thử tay](docs/manual-test-checklist.md).

## Kiến trúc

```
Snaptic.App     Avalonia — tray, overlay, preview, generator, settings
    │  dùng interface                 │  tham chiếu CHỈ để nạp DI
    ▼                                 │
Snaptic.Core    logic thuần — không biết Windows, không biết UI
    ▲  hiện thực interface            │
    │                                 ▼
Snaptic.Windows Win32 / WinRT
```

`Core` không tham chiếu ai. `ServiceRegistration.cs` là file **duy nhất** trong `App`
biết `Snaptic.Windows` — giữ được ranh giới này thì thêm macOS chỉ là viết `Snaptic.Mac`
hiện thực 5 interface.

Đây không phải kiến trúc cho đẹp: nó là **toàn bộ lý do** chọn C#+Avalonia thay vì Win32
thuần. Phá ranh giới là lựa chọn đó vô nghĩa.

## Tài liệu

- [Thiết kế đầy đủ](docs/superpowers/specs/2026-07-15-snaptic-design.md) — quyết định và lý do
- [Kế hoạch thi công](docs/superpowers/plans/2026-07-15-snaptic.md) — 17 task
- [Checklist kiểm thử tay](docs/manual-test-checklist.md)

## Ủng hộ tác giả

Nếu Snaptic giúp ích cho công việc của bạn, mời tác giả một ly cà phê:

<img src="docs/tk-techcombank.png" alt="QR chuyển khoản Techcombank" width="300">

| | |
|---|---|
| Ngân hàng | Techcombank |
| Chủ tài khoản | NGUYEN DUC SON |
| Số tài khoản | `7030456789` |

Quét mã bằng app ngân hàng bất kỳ có hỗ trợ VietQR / napas 247. Mã đã điền sẵn số tiền
và nội dung — sửa lại được trước khi xác nhận.

Trong app: **chuột phải icon khay → Giới thiệu...**

## Bản quyền và giấy phép

Copyright © 2026 **Nguyen Duc Son**

Snaptic là phần mềm tự do: bạn được quyền phân phối lại và/hoặc sửa đổi theo các điều
khoản của **Giấy phép Công cộng GNU (GNU GPL)** do Free Software Foundation công bố,
phiên bản 3 hoặc (tuỳ bạn chọn) bất kỳ phiên bản nào mới hơn.

Toàn văn giấy phép: [LICENSE](LICENSE) · <https://www.gnu.org/licenses/gpl-3.0.html>

### Vì sao GPL-3.0 chứ không phải GPL-2.0

Không phải lựa chọn tuỳ hứng — thư viện quyết định:

| Thư viện | Giấy phép | Tương thích |
|---|---|---|
| Avalonia | MIT | GPLv2 và GPLv3 |
| SkiaSharp | MIT | GPLv2 và GPLv3 |
| ZXing.Net | Apache-2.0 | **chỉ GPLv3** |

Apache-2.0 **không tương thích ngược với GPLv2**, nên phát hành Snaptic dưới GPLv2 sẽ
là vi phạm giấy phép của ZXing.Net.

## Miễn trừ trách nhiệm

Phần mềm này được cung cấp **"NGUYÊN TRẠNG" (AS IS)**, **KHÔNG KÈM BẤT KỲ BẢO ĐẢM NÀO**,
dù rõ ràng hay ngụ ý, bao gồm nhưng không giới hạn ở các bảo đảm về khả năng bán được,
tính phù hợp cho một mục đích cụ thể, và không vi phạm quyền của bên thứ ba.

Trong mọi trường hợp, tác giả **không chịu trách nhiệm** với bất kỳ khiếu nại, thiệt hại
hay nghĩa vụ nào phát sinh từ việc sử dụng phần mềm — kể cả mất mát dữ liệu, gián đoạn
công việc, hay thiệt hại gián tiếp.

**Bạn chịu toàn bộ rủi ro về chất lượng và hiệu năng khi sử dụng.**

Cụ thể với Snaptic, một số giới hạn đã biết mà bạn cần lưu ý:

- **OCR tiếng Việt có dấu cho kết quả SAI mà trông hợp lý** (xem mục Giới hạn đã biết).
  Không được dùng kết quả OCR cho mục đích cần độ chính xác mà không kiểm lại bằng mắt.
- **Ảnh chụp có thể chứa thông tin nhạy cảm.** Snaptic không kiểm duyệt nội dung bạn
  chụp, copy hay lưu.
- **Mã QR là dữ liệu từ nguồn không tin cậy.** Snaptic chỉ mở link `http`/`https` và
  chặn các scheme khác, nhưng điều đó **không đảm bảo** trang web đích an toàn.

Đây là bản tóm tắt tiếng Việt cho dễ đọc. Văn bản có hiệu lực pháp lý là **mục 15, 16 và
17** trong [LICENSE](LICENSE) (tiếng Anh).
