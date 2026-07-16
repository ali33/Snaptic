# Checklist kiểm thử tay — Snaptic

Phần platform (chụp, clipboard, phím tắt, OCR) không test tự động được vì cần màn hình
và OS thật. Đây là hàng rào thay thế.

**Đã tự động hoá được nhiều hơn kế hoạch dự tính:** phần lớn mục dưới đây kiểm được bằng
cách tổng hợp phím/chuột (`keybd_event`, `mouse_event`) và dùng chính `WindowsScreenCapture`
của app để chụp lại màn hình mà soi. Mục nào **thật sự cần người** thì đánh dấu 👤.

## Chụp màn hình

- [ ] Ctrl+Alt+Q → overlay phủ mọi màn hình, con trỏ chữ thập
- [ ] Kéo thả → cửa sổ preview mở với đúng vùng đã khoanh
- [ ] Esc → huỷ, không mở gì
- [ ] Click một phát không kéo → không mở gì (dưới ngưỡng 8×8)
- [ ] **Mở menu chuột phải rồi bấm Ctrl+Alt+Q → menu VẪN CÒN trong ảnh**
- [ ] 👤 Bấm phím tắt khi đang ở app toàn màn hình → vẫn bật được overlay
- [ ] 👤 Giữ Ctrl+Alt+Q → overlay chỉ bật MỘT lần, không spam (`MOD_NOREPEAT`)

## DPI và đa màn hình

> ⚠ Máy phát triển chỉ có **1 màn hình @100%**, nên ba mục này **chưa từng được kiểm
> thử tay**. Test bảng của `DpiMapper` là hàng rào duy nhất cho chúng.

- [ ] 👤 Chụp trên màn 150% → vùng khớp chính xác, ảnh **không mờ**
- [ ] 👤 Đa màn cùng scaling → kéo được trên cả hai
- [ ] 👤 **Đa màn KHÁC scaling → vùng khớp trên cả hai**
- [ ] 👤 **Vùng chọn vắt ngang hai màn khác scaling → ảnh ra đúng, không lệch**

## Nhận diện

- [ ] Chụp QR chứa link → nút "Copy QR text" + "Mở https://..." mọc thêm
- [ ] Chụp QR chứa text thường → có "Copy QR text", **KHÔNG** có "Mở link"
- [ ] Chụp barcode 1D → nhãn là "Copy barcode", không phải "Copy QR text"
- [ ] Chụp vùng không có mã → không có nút mã nào
- [ ] Chụp đoạn chữ tiếng Anh → nút "Copy text (N ký tự)" mọc thêm
- [ ] **Cửa sổ hiện ảnh NGAY, không khựng chờ OCR**
- [ ] **Khoanh trọn cả QR** — khoanh hụt một góc thì không decode được, đó là đúng
- [ ] Chụp chữ Việt **không dấu** → OCR đọc chính xác
- [ ] Chụp chữ Việt **có dấu** → OCR ra sai (`Tiéng Viét cé dä'u`), đây là giới hạn đã biết

## An toàn

- [ ] Tạo QR chứa `file:///C:/Windows/System32/calc.exe`, chụp lại →
      **KHÔNG** có nút Mở link, nhưng Copy QR text vẫn có

## Clipboard

- [ ] 👤 Copy ảnh → paste vào **Paint**: đúng, không lộn ngược, không lệch màu
- [ ] 👤 Copy ảnh → paste vào **Word**: đúng
- [ ] 👤 Copy ảnh → paste vào **Chrome**: đúng
- [ ] Copy Data URI → paste vào Notepad: `data:image/png;base64,...`
- [ ] Copy QR text → paste vào Notepad: đúng nội dung

## Lưu file

- [ ] 👤 Save... → hộp thoại mở ở `Pictures\Snaptic`
- [ ] 👤 Tên điền sẵn dạng `Snaptic_2026-07-16_154230.png`
- [ ] 👤 Lưu xong → file mở được, đúng ảnh
- [ ] 👤 Bấm Cancel → không tạo file, không lỗi

## Chụp lại

- [ ] Bấm "Chụp lại" → cửa sổ preview đóng, overlay mở lại
- [ ] **Ảnh đóng băng lần 2 KHÔNG chứa cửa sổ preview cũ** (đã từng là bug thật)
- [ ] Khoanh lần 2 → preview mới với ảnh mới

## Tạo mã

- [ ] Tạo QR "hello" → ảnh hiện **ngay khi gõ**
- [ ] 👤 Đổi dropdown sang EAN-13 với text "hello" → nút Tạo **tắt** + lời nhắc "chỉ nhận chữ số"
- [ ] 👤 EAN-13 với 11 số → "cần đúng 12 chữ số, đang có 11"
- [ ] 👤 EAN-13 với 12 số → tạo được
- [ ] 👤 Code39 với chữ thường → "không nhận chữ thường"
- [ ] Bấm Tạo → preview mở, **chỉ có nút cơ bản** (không có Copy QR text)
- [ ] Copy ảnh mã → paste vào Paint ra đúng mã
- [ ] **Chụp lại chính mã vừa tạo trên màn hình → decode ra đúng text đã gõ**

## Cài đặt

- [ ] Dropdown ngôn ngữ liệt kê en-US (không có tiếng Việt — đúng thiết kế)
- [ ] Ngôn ngữ hiện sẵn là ngôn ngữ app ĐANG dùng, không để trống
- [ ] 👤 Đổi phím sang Ctrl+Alt+W → Lưu → phím mới hoạt động
- [ ] 👤 Gõ phím trần "A" → báo phải có phím bổ trợ, giữ phím cũ
- [ ] 👤 Gán tổ hợp đã bị app khác chiếm → báo lỗi, giữ phím cũ
- [ ] 👤 Đổi phím rồi bấm Huỷ → phím cũ vẫn hoạt động
- [ ] Tắt app mở lại → phím đã lưu vẫn đúng

## Khởi động cùng Windows

- [ ] Xoá `%AppData%\Snaptic\config.json` → chạy → registry `HKCU\...\Run` **CÓ** Snaptic
- [ ] Xoá Snaptic khỏi registry → chạy lại → **KHÔNG** tự bật lại (app không cãi lời)
- [ ] Cài đặt → ô tick phản ánh đúng registry
- [ ] Xoá khỏi registry → mở Cài đặt → ô tick **trống** (đọc OS, không đọc config)
- [ ] Tick rồi bấm Huỷ → registry **không đổi** (chỉ áp lúc Lưu)
- [ ] 👤 Đăng xuất/đăng nhập lại Windows → Snaptic tự chạy

## Vòng đời app

- [ ] Đóng mọi cửa sổ → app **vẫn sống** ở tray
- [ ] Tray → Thoát → icon biến mất, tiến trình kết thúc hẳn
- [ ] Chạy hai instance → instance thứ hai hiện toast báo phím tắt bị chiếm, **không crash**
- [ ] Xoá `%AppData%\Snaptic\config.json` → app chạy bình thường với mặc định
- [ ] Ghi rác vào config.json → app chạy bình thường với mặc định, **không crash**
- [ ] Chụp 20 lần liên tiếp → RAM trong Task Manager không tăng dần (không rò ảnh)
- [ ] **Ca đua: chụp vùng chữ lớn rồi ĐÓNG cửa sổ NGAY** (lúc OCR đang chạy) → không crash. Làm 5 lần
