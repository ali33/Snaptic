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
