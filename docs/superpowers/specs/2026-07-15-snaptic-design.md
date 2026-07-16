# Snaptic — Thiết kế

Ngày: 2026-07-15
Trạng thái: Đã duyệt thiết kế, chờ lập kế hoạch thi công

## 1. Tổng quan

Snaptic là ứng dụng desktop nằm ở khay hệ thống (tray), làm hai chiều:

- **Ảnh → dữ liệu**: khoanh vùng màn hình để chụp, app nhận diện nội dung trong ảnh (QR, barcode, text) và đưa ra hành động phù hợp theo ngữ cảnh.
- **Dữ liệu → ảnh**: nhập text, app tạo ra mã QR hoặc barcode.

Điểm khác biệt so với ShareX / Lightshot / Snipping Tool không nằm ở việc chụp, mà ở chỗ **app hiểu được nội dung nó vừa chụp**.

### Người dùng và mục tiêu

Công cụ cá nhân, dùng trên máy riêng. Không phát hành công khai trong phạm vi bản này.

Thành công nghĩa là: bấm `Ctrl+Alt+Q`, khoanh vùng, và trong dưới một giây có được thứ mình cần trong clipboard.

## 2. Phạm vi

### Trong phạm vi

| Nhóm | Nội dung |
|---|---|
| Nền tảng | **Chỉ Windows.** Chọn stack cross-platform để sau này thêm macOS không phải viết lại |
| Kích hoạt | Phím tắt toàn cục (mặc định `Ctrl+Alt+Q`) + menu chuột phải ở tray |
| Chụp | Khoanh vùng tự do, hỗ trợ đa màn hình, hỗ trợ DPI scaling khác nhau |
| Nhận diện | QR, barcode 1D, OCR text — **OCR mặc định chỉ tiếng Anh**; cài thêm được 34 ngôn ngữ khác qua Windows Settings; **không có tiếng Việt** (§8 R1) |
| Hành động | Copy ảnh · Save file · Copy Data URI · Copy QR/barcode text · Copy OCR text · Open link |
| Tạo mã | QR, Code128, EAN-13, UPC-A, Code39 |
| Cấu hình | Đổi phím tắt, chọn ngôn ngữ OCR, thư mục lưu mặc định |

### Ngoài phạm vi

Những thứ sau **cố ý** không làm, kèm lý do:

- **Code signing, installer, auto-update, notarization** — chỉ dùng cá nhân. Nếu sau này phát hành thật thì đây là khối lượng việc đáng kể phải tính lại.
- **macOS** — không có máy Mac để test. Kiến trúc giữ đường sang, nhưng không viết code không test được.
- **Đọc nhiều mã trong một ảnh** — thao tác khoanh vùng đã tự chọn ra thứ cần rồi; hỗ trợ nhiều mã đòi thêm UI danh sách. Thêm khi gặp nhu cầu thật.
- **Annotate / crop / vẽ lên ảnh** — không thuộc mục đích app.
- **Lịch sử ảnh đã chụp** — chưa có nhu cầu.
- **OCR nhiều ngôn ngữ cùng lúc** — Windows OCR mỗi engine một ngôn ngữ.
- **OCR tiếng Việt** — Windows OCR không có gói tiếng Việt (đã kiểm chứng, §8 R1). Đã cân nhắc Tesseract và **chủ động từ chối**: không đáng đổi lấy 15–30MB model + phụ thuộc native. Kiến trúc để ngỏ đường thêm sau.
- **Test UI tự động (Avalonia.Headless)** — không đáng cho tool cá nhân; giữ ViewModel mỏng và test ViewModel.

## 3. Nền tảng kỹ thuật

| Hạng mục | Lựa chọn |
|---|---|
| Ngôn ngữ | C# |
| Runtime | **.NET 10** (LTS) — đã xác nhận SDK 10.0.300 có trên máy. `Core` → `net10.0`; `Windows` và `App` → `net10.0-windows10.0.19041.0` |
| UI | **Avalonia UI 12.1.0** |
| Kiểu ảnh dùng chung | **`SKBitmap` (SkiaSharp 3.119.4)** — xem ghi chú dưới |
| QR / Barcode (cả đọc lẫn tạo) | **ZXing.Net 0.16.11** qua **ZXing.Net.Bindings.SkiaSharp 0.16.22** |
| OCR | `Windows.Media.Ocr` qua CsWinRT |
| DI | `Microsoft.Extensions.DependencyInjection` |
| Test | xUnit |

**Đã xác minh bằng build thật (2026-07-15), không phải phỏng đoán:** Avalonia 12.1.0 + ZXing.Net.Bindings.SkiaSharp 0.16.22 cùng hội tụ về SkiaSharp 3.119.4, restore và build trên `net10.0` cho 0 lỗi 0 cảnh báo.

**Vì sao `SKBitmap` làm kiểu ảnh của `Core`:** `System.Drawing` chỉ chạy Windows nên vi phạm luật "Core không dính OS". Core cần một kiểu ảnh để: nhận từ `IScreenCapture`, đưa cho ZXing decode, encode PNG cho Data URI và Save.

SkiaSharp giải cả bốn: chạy mọi OS (**không phải coupling với OS**, chỉ là một thư viện), ZXing có binding sẵn nên không phải tự viết cầu nối, encode PNG sẵn có, và **Avalonia vốn đã dùng Skia** nên không thêm phụ thuộc native nào — cùng một `libSkiaSharp` mà app đã nạp.

### Vì sao chọn stack này

Ghi lại để sau đọc còn hiểu, vì phương án bị loại không hề vô lý:

- **C# + Avalonia thay vì C++ + Win32 thuần**: Win32 thuần cho app dưới 10MB nhưng mất macOS vĩnh viễn và mỗi tính năng mới đều chậm hơn. App này 99% thời gian nằm chờ; việc nặng nhất là OCR — do DLL của Windows làm, tốn bằng nhau dù gọi từ ngôn ngữ nào. **Ngôn ngữ không nằm trên đường đi nóng**, nên đổi tốc độ phát triển lấy RAM ở đây là lỗ.
- **C# + Avalonia thay vì C++ + Qt**: Qt không nhẹ. Chịu toàn bộ cái giá của C++ mà RAM gần như ngang C#.
- **Thay vì Electron**: OCR chỉ có tesseract.js (chậm, kém chính xác hơn Windows OCR) và 150–200MB RAM thường trú là quá đắt.
- **Thay vì Tauri**: barcode 1D trong Rust rất yếu, phải bind sang zbar (khó build trên Windows). Hai thứ cần nhất lại là hai thứ tốn công nhất.

## 4. Kiến trúc

Nguyên tắc chi phối: **cô lập mọi thứ dính Windows vào một tầng duy nhất.** Đây là toàn bộ lý do chọn C# + Avalonia thay vì Win32 thuần — nếu không giữ ranh giới này thì lựa chọn đó vô nghĩa.

```
        Snaptic.App  (Avalonia — tray, overlay, preview, generator, settings)
              │                          │
              │ dùng interface           │ tham chiếu CHỈ để nạp
              ▼                          │ implementation vào DI
        Snaptic.Core                     │
   (logic thuần — không biết             │
    Windows, không biết UI)              │
              ▲                          │
              │ hiện thực interface      │
              │                          ▼
        Snaptic.Windows  (Win32 / WinRT)
```

Chiều phụ thuộc là thứ quan trọng nhất ở đây:

- `Core` **không tham chiếu ai cả** — đó là điều làm nó test được và mang sang macOS được.
- `Windows` tham chiếu `Core` (để hiện thực interface), **không** ngược lại.
- `App` tham chiếu **cả hai**, nhưng bất đối xứng: nó *dùng* `Core` khắp nơi, còn `Windows` thì chỉ đụng đúng **một chỗ duy nhất** — chỗ đăng ký DI lúc khởi động. Ngoài file đó ra, không dòng nào trong `App` được nhắc tên lớp nào của `Windows`.

Ràng buộc cuối là thứ dễ vi phạm nhất trong lúc code (gọi thẳng `WindowsClipboard` cho nhanh), và vi phạm nó là làm hỏng toàn bộ mục đích của kiến trúc này. Thêm macOS sau = viết `Snaptic.Mac` hiện thực đúng 4 interface + đổi một dòng đăng ký DI.

### `Snaptic.Core`

Không tham chiếu gì dính OS. Chứa toàn bộ logic có nhánh.

**Interface (tầng platform phải hiện thực):**

| Interface | Trách nhiệm |
|---|---|
| `IScreenCapture` | Chụp toàn bộ màn hình (mọi monitor) → ảnh + thông tin DPI từng monitor |
| `ITextRecognizer` | `GetAvailableLanguages()` → danh sách; `Recognize(ảnh, ngôn ngữ)` → text |
| `IClipboardService` | Đặt ảnh / text vào clipboard |
| `IHotkeyService` | `Register(tổ hợp)`, `Unregister()`; sự kiện khi bấm |

**Logic (test được, không cần màn hình):**

| Thành phần | Trách nhiệm |
|---|---|
| `BarcodeDecoder` | ZXing đọc mã. Chạy được mọi OS nên nằm ở Core, không cần interface |
| `BarcodeGenerator` | ZXing tạo mã |
| `BarcodeFormatSpec` | Luật hợp lệ từng định dạng — hàm thuần `(text, format) → hợp lệ/không + lời nhắc` |
| `RecognitionService` | Điều phối decode + OCR, trả `CaptureAnalysis` |
| `LinkValidator` | Quyết định URL nào được phép mở |
| `DataUriEncoder` | Ảnh → `data:image/png;base64,…` |
| `DpiMapper` | Quy đổi **từng điểm**: `(monitor, điểm logical) → điểm physical toàn cục` |
| `SettingsService` | Model config + đọc/ghi JSON |

**Mô hình dữ liệu:**

```
CaptureResult      sealed class { Image: SKBitmap, CapturedAt } — KHÔNG phải record (bẫy `with`)
BarcodeResult      { Format, Text, IsHttpUrl }
CaptureAnalysis    { Barcode: BarcodeResult?, BarcodeStatus,
                     OcrText: string?, OcrStatus }
RecognitionStatus  = Pending | Done | Unavailable | Failed
HotkeyCombo        { Modifiers, Key }
AppSettings        { Hotkey, OcrLanguage, DefaultSaveFolder }
```

`RecognitionStatus` không phải thừa: nó là thứ điều khiển việc nút mọc dần trong cửa sổ preview (xem §5.3).

### `Snaptic.Windows`

Tầng duy nhất phải viết lại cho macOS. **Giữ mỏng đến mức không cần test.** Mỗi lớp một việc, không nhánh logic. Lớp nào bắt đầu mọc `if` phức tạp là dấu hiệu logic đặt sai chỗ — đẩy về `Core`.

| Lớp | Cách làm |
|---|---|
| `WindowsScreenCapture` | Win32 `BitBlt` |
| `WindowsOcr` | `Windows.Media.Ocr` qua CsWinRT |
| `WindowsClipboard` | Win32 clipboard, `CF_DIB` cho ảnh |
| `WindowsHotkey` | Win32 `RegisterHotKey` |

Cần khai báo **PerMonitor-V2 DPI awareness** trong app manifest.

### `Snaptic.App`

Avalonia. TrayIcon + 4 cửa sổ (§5). Logic hiện nút nằm ở ViewModel, không nằm trong XAML — để test được.

## 5. Luồng dữ liệu và các bề mặt UI

### 5.1 Tray

Menu chuột phải:

```
  Chụp màn hình        Ctrl+Alt+Q
  Tạo QR...
  Tạo Barcode...
  ─────────────────
  Cài đặt...
  Thoát
```

### 5.2 Chụp → nhận diện → hành động

```
Ctrl+Alt+Q  (hoặc menu tray)
   │
   ├─► Chụp NGAY toàn bộ màn hình (mọi monitor) ──► ảnh đóng băng
   │
   ├─► OverlayWindow: MỘT cửa sổ trên MỖI monitor, vẽ phần ảnh đóng băng
   │    tương ứng + lớp mờ, con trỏ chữ thập
   │      ├─ Esc ──► hủy, không mở gì
   │      └─ kéo & thả ──► hai điểm góc (tọa độ logical, có thể ở 2 monitor khác nhau)
   │
   ├─► DpiMapper: quy đổi TỪNG ĐIỂM → tọa độ physical toàn cục
   ├─► Cắt từ ảnh đóng băng
   │
   ├─► PreviewWindow mở NGAY với ảnh ────────────────────┐
   │                                                      │
   └─► Chạy nền song song:                                │
          ├─ BarcodeDecoder   ~5–20ms  ──────────────► nút mọc thêm
          └─ Windows OCR      ~10–30ms  ────────────► khi kết quả về
```

**Đóng băng màn hình trước, cắt sau.** Chụp toàn màn hình ngay khi bấm phím, rồi cho khoanh lên ảnh tĩnh đó. Vì: menu đang mở không biến mất, không nháy hình, vùng chọn khớp đúng thứ nhìn thấy.

**Hệ tọa độ — chỗ dễ sai nhất trong toàn bộ app.** Ảnh đóng băng nằm trong không gian **physical pixel của toàn bộ desktop ảo**. Overlay làm việc bằng **logical pixel**, mà không gian logical **bị chia khúc**: mỗi monitor một hệ số scaling riêng, nên không tồn tại một hệ số chung nào để quy đổi cả vùng chọn.

Cách giải: mỗi monitor một `OverlayWindow` riêng, mỗi cửa sổ biết DPI của monitor mình. `DpiMapper` quy đổi **từng điểm một** — `(monitor, điểm logical) → điểm physical toàn cục` — dùng DPI của monitor chứa chính điểm đó.

Nhờ vậy vùng chọn **vắt ngang hai monitor khác scaling vẫn đúng**: hai điểm góc được quy đổi độc lập, và hình chữ nhật giữa chúng trong không gian physical là xác định, không mơ hồ. Nếu quy đổi cả rect bằng một hệ số duy nhất thì ca này sai — và nó chỉ lộ ra trên máy có nhiều màn hình khác scaling.

**Cửa sổ preview không chờ nhận diện.** Chờ OCR xong mới mở thì mỗi cú chụp khựng 1/3 giây — cảm giác ì dù không thực sự chậm.

### 5.3 PreviewWindow

Nhận vào *(ảnh, `CaptureAnalysis`)*. Không có cờ chế độ: đường chụp truyền analysis tính bất đồng bộ, đường tạo mã truyền analysis rỗng.

| Nút | Điều kiện hiện |
|---|---|
| Copy ảnh · Save · Copy Data URI | luôn có ngay |
| Copy mã | `BarcodeStatus = Done` và có kết quả |
| Open link | thêm điều kiện `IsHttpUrl` |
| Copy text OCR | `OcrStatus = Done` và text không rỗng |

**"Copy mã" là một nút, không phải hai.** Nhãn đổi theo định dạng decode được: `"Copy QR text"` khi là QR, `"Copy barcode"` khi là mã 1D. Không bao giờ hiện cả hai cùng lúc vì mỗi ảnh chỉ đọc một mã (§2).

**Save mở hộp thoại, không tự lưu.** Tên điền sẵn `Snaptic_2026-07-15_154230.png`, thư mục mặc định lấy từ settings, định dạng PNG. Tự lưu nhanh hơn một click nhưng không biết file nằm đâu và thư mục đầy rác sau vài tuần.

**Open link hiện luôn URL** trên nút để thấy mình sắp đi đâu **trước khi** bấm.

### 5.4 Cửa sổ tạo mã

Ô nhập text + dropdown định dạng. Ảnh mã cập nhật ngay khi gõ. Nút Tạo khóa khi nội dung không hợp lệ, kèm lời nhắc cụ thể.

| Định dạng | Luật | Lời nhắc khi sai |
|---|---|---|
| QR | mọi text, không rỗng | — |
| Code128 | ASCII 0–127, không rỗng | "Code128 không nhận ký tự có dấu" |
| EAN-13 | **đúng 12 chữ số** — app tự tính số kiểm tra thứ 13 | "Cần đúng 12 chữ số, đang có 9" |
| UPC-A | **đúng 11 chữ số** — app tự tính số kiểm tra thứ 12 | "Cần đúng 11 chữ số, đang có 9" |
| Code39 | A–Z HOA, 0–9, `- . $ / + %` (khoảng trắng hợp lệ) | "Code39 không nhận chữ thường" |

**EAN-13 nhận đúng 12 số, không nhận 13.** Nhập 13 số → báo "cần đúng 12". Cho phép cả 12 lẫn 13 thì sinh mơ hồ: số thứ 13 là số kiểm tra người dùng tự tính (phải xác thực) hay là số dữ liệu? Chốt một luật cho khỏi phải quyết định lúc viết code. UPC-A tương tự với 11.

Tạo xong → ảnh chảy vào `PreviewWindow` với analysis rỗng.

### 5.5 Settings

```
Phím tắt chụp:    [ Ctrl+Alt+Q ]   ← bấm rồi gõ tổ hợp mới
Ngôn ngữ OCR:     [ dropdown ]     ← liệt kê từ OS
      └─ nếu rỗng: "Máy chưa cài gói OCR nào" + hướng dẫn cài trong Windows Settings
Thư mục lưu mặc định: [ … ]
```

Config: `%AppData%\Snaptic\config.json`

**Mặc định lần chạy đầu:**

| Thiết lập | Mặc định |
|---|---|
| Phím tắt | `Ctrl+Alt+Q` |
| Ngôn ngữ OCR | Ngôn ngữ hệ thống nếu có trong danh sách OS trả về; không thì tiếng Anh; không có nữa thì không chọn gì và ẩn nút OCR (§6) |
| Thư mục lưu | `Pictures\Snaptic` |

**Luật đổi phím tắt:**

- **Bắt buộc ít nhất một phím bổ trợ** (Ctrl/Alt/Shift/Win). Gán phím trần `A` thì mọi lần gõ chữ A ở bất cứ đâu đều bật overlay.
- **Thử đăng ký ngay khi gán.** Hỏng → báo *"tổ hợp này đang bị ứng dụng khác dùng"* và giữ nguyên phím cũ. Không im lặng nhận rồi để phát hiện sau.

**Vì sao mặc định `Ctrl+Alt+Q`:** phím tắt toàn cục chặn phím **trước** khi app nhận được. `Ctrl+Shift+S` sẽ cướp "Save As" của mọi ứng dụng — hỏng thao tác lưu file khắp nơi với nguyên nhân rất khó đoán. `Win+Shift+S` là của Snipping Tool sẵn trong Windows. `Ctrl+Alt+Q` hiếm app giành và không đụng phím tắt hệ thống nào.

## 6. Xử lý lỗi

Nguyên tắc: **không decode được thì không phải lỗi, chỉ là không hiện nút.**

**Cách báo lỗi cho người dùng:** dùng **thông báo bong bóng ở tray** (`TrayIcon` của Avalonia), **không** dùng Windows Toast API. Toast API cần app có AppUserModelID đăng ký — mà đăng ký thì gắn với installer, thứ đã nằm ngoài phạm vi (§2). Bong bóng tray đủ dùng và không kéo theo gì. Từ "toast" ở bảng dưới nghĩa là bong bóng tray.

| Tình huống | Xử lý |
|---|---|
| Không có OCR engine nào | Phát hiện lúc khởi động → ẩn hẳn nút OCR, ghi log, không crash |
| Clipboard đang bị app khác giữ | Thử lại vài lần cách ~50ms → vẫn hỏng thì toast báo. Lỗi Win32 có thật, không phải phòng xa |
| Phím tắt bị chiếm lúc khởi động | Báo qua tray; app vẫn chạy, vẫn chụp được từ menu tray |
| Chụp trúng nội dung DRM | Ra ảnh đen — hành vi của OS, không sửa được, **không xử lý gì**. Cố dò "ảnh toàn đen để cảnh báo" là ý tồi: vùng tối hợp lệ (nền app dark theme) cũng toàn đen → báo nhầm nhiều hơn báo đúng |
| Khoanh vùng quá nhỏ (lỡ tay click) | Dưới 8×8 px → coi như hủy, không mở cửa sổ |
| Đa màn hình khác DPI | Quy đổi **từng điểm** theo DPI của monitor chứa điểm đó, không dùng một hệ số chung (§5.2) |
| Config hỏng hoặc thiếu | Về mặc định, không crash |
| ZXing ném exception lúc tạo mã | Validate trước nên hiếm; vẫn bắt và hiện message |

### An toàn: Open link

Một QR code là **dữ liệu từ bên ngoài**. Nó có thể chứa `file:///`, `javascript:`, scheme của app khác (`steam://`, `ms-…`), hay đường dẫn UNC `\\máy\share`. Mở thẳng những thứ đó là giao cho người lạ quyền kích hoạt thứ họ muốn trên máy.

**Luật: chỉ `http://` và `https://` được mở.** Scheme khác → không hiện nút Open link, nhưng text vẫn copy được bình thường.

`LinkValidator` là hàng rào duy nhất ở đây → test kỹ (§7).

## 7. Chiến lược test

Nguyên tắc: **đẩy mọi thứ có nhánh về `Core` để test không cần màn hình; giữ tầng `Windows` mỏng tới mức không cần test.**

### Tự động — logic thuần

| Test | Vì sao đáng test |
|---|---|
| `LinkValidator` | Quan trọng nhất về an toàn. http/https qua; `file://`, `javascript:`, `steam://`, UNC phải bị chặn |
| `DpiMapper` | Bug kinh điển của app chụp màn. Bảng ca: điểm logical × hệ số 1.0 / 1.25 / 1.5 / 2.0. **Bắt buộc có ca vùng chọn vắt ngang 2 monitor khác scaling** — ca này không thể test tay ở máy 1 màn hình |
| `BarcodeFormatSpec` × 4 định dạng | Bảng ca: EAN-13 với 11/12/13 số, có chữ, rỗng, có dấu… |
| `RecognitionService` | Decoder giả + OCR giả → đúng `CaptureAnalysis` |
| `SettingsService` | Ghi/đọc round-trip; file hỏng hoặc thiếu → về mặc định, không crash |
| `HotkeyCombo` | Từ chối tổ hợp không có phím bổ trợ |
| `PreviewViewModel` | Nút nào hiện với analysis nào |
| `DataUriEncoder` | Đúng tiền tố `data:image/png;base64,` + base64 hợp lệ |

### Tự động — bằng ảnh mẫu

- Vài file PNG chứa QR/barcode biết trước nội dung → decode ra đúng text. Ảnh không có mã → null.
- **Vòng khép kín**: `Generate(QR, "hello")` → `Decode` → `"hello"`. Rẻ và kiểm được cả hai chiều vì dùng chung ZXing.

### Làm tay theo checklist

Phần platform cần màn hình/OS thật:

- Chụp ở 100% / 150% / đa màn khác DPI
- Chụp menu đang mở
- Copy ảnh → paste vào Word / Chrome / Discord
- Hotkey khi đang ở app fullscreen
- Đổi phím tắt sang tổ hợp đang bị chiếm → phải báo và giữ phím cũ

## 8. Rủi ro

### R1 — Windows OCR không hỗ trợ tiếng Việt *(ĐÃ KIỂM CHỨNG 2026-07-15 — xác nhận có thật, đã chấp nhận)*

**Kết quả kiểm chứng.** Chạy trên máy người dùng (Windows 11 build 26200):

```powershell
# Đã cài:
[Windows.Media.Ocr.OcrEngine]::AvailableRecognizerLanguages   # → chỉ en-US

# Windows CÓ những gói OCR nào (PowerShell Admin):
Get-WindowsCapability -Online | Where-Object { $_.Name -Like 'Language.OCR*' }
# → 35 gói: ar-SA, bg-BG, bs-LATN-BA, cs-CZ, da-DK, de-DE, el-GR, en-GB,
#   en-US, es-ES, es-MX, fi-FI, fr-CA, fr-FR, hr-HR, hu-HU, it-IT, ja-JP,
#   ko-KR, nb-NO, nl-NL, pl-PL, pt-BR, pt-PT, ro-RO, ru-RU, sk-SK, sl-SI,
#   sr-CYRL-RS, sr-LATN-RS, sv-SE, tr-TR, zh-CN, zh-HK, zh-TW
# → KHÔNG có vi-VN
```

**Kết luận: Windows OCR không đọc được tiếng Việt.** Không phải "chưa cài" — Microsoft không phát hành gói OCR tiếng Việt. Cài gì cũng không có.

**Quyết định: chấp nhận, giữ nguyên Windows OCR.** Không chuyển sang Tesseract. Đánh đổi đã cân nhắc:

- **Được:** không phải bó model 15–30MB vào app; độ chính xác Windows OCR cao hơn Tesseract trên các ngôn ngữ nó hỗ trợ; không thêm phụ thuộc native nào.
- **Mất:** OCR tiếng Việt — vĩnh viễn, không có đường vòng trong phạm vi này.

**Hệ quả với thiết kế:** không thay đổi gì. `ITextRecognizer` vẫn ở tầng `Windows`, tầng platform vẫn 4 interface.

- Mặc định OCR ra **en-US** (luật §5.5 đã đúng sẵn: ngôn ngữ hệ thống `vi-VN` không có trong danh sách → tự lùi về tiếng Anh).
- Người dùng **cài thêm được 34 gói kia** qua Windows Settings; dropdown §5.5 tự nhặt vào, không cần sửa code.
- **Tiếng Việt KHÔNG DẤU thì engine en-US đọc tốt** (chỉ là chữ Latin). Đo trên máy: "Xin chao Viet Nam" ra chính xác.
- **CẢNH BÁO — chữ CÓ DẤU không hỏng hẳn mà ra SAI trông hợp lý.** Đo trên máy: "Tiếng Việt có dấu" → `Tiéng Viét cé dä'u`; "Đường Trần Hưng Đạo" → `Dddng Trän Hdng Dao`. Engine thay dấu này bằng dấu khác rồi trả về, không báo lỗi. Nguy hiểm hơn hỏng hẳn: người dùng copy ra tưởng đúng. UI nên nói rõ ngôn ngữ OCR đang dùng để họ tự đánh giá.
- Tính năng "liệt kê ngôn ngữ OCR" (§5.5) chính là cách app nói thật chuyện này — nó không giấu việc tiếng Việt vắng mặt.

**Nếu sau này cần OCR tiếng Việt thật:** thêm `TesseractTextRecognizer` hiện thực cùng `ITextRecognizer` và cho chọn engine trong settings. Kiến trúc đã sẵn sàng; đây là việc thêm, không phải việc sửa.

### R2 — Clipboard ảnh trên Avalonia

Avalonia không đặt ảnh vào clipboard tiện như text. Đã tính: `WindowsClipboard` gọi thẳng Win32 `CF_DIB`. Không phải chi phí phát sinh — phần này dù chọn stack nào cũng phải viết riêng cho từng OS.

### R3 — Đa màn hình khác DPI

Ca khó nhất và chỉ lộ ra trên máy có scaling ≠ 100%. Giảm rủi ro bằng: PerMonitor-V2 manifest, `DpiMapper` tách riêng + test bảng, và checklist tay bắt buộc.

## 9. Nhật ký quyết định

| Quyết định | Lý do |
|---|---|
| Chỉ Windows cho bản này | Không có máy Mac để test. Không viết code không test được |
| Không signing/installer/update | Chỉ dùng cá nhân |
| C# + Avalonia | Xem §3 |
| Tách `Core` / `Windows` | Là toàn bộ lý do không chọn Win32 thuần. Cũng làm logic test được không cần màn hình |
| Đóng băng màn hình trước, cắt sau | Chụp được menu đang mở, không nháy, khớp thứ nhìn thấy |
| Preview không chờ OCR | Tránh khựng 1/3 giây mỗi cú chụp |
| Chỉ đọc một mã mỗi ảnh | Khoanh vùng đã tự chọn ra thứ cần. Nhiều mã đòi thêm UI danh sách |
| Open link chỉ http/https | QR là dữ liệu từ bên ngoài; scheme khác là đường tấn công |
| Save mở hộp thoại | Tự lưu thì không biết file ở đâu, thư mục đầy rác |
| Mặc định `Ctrl+Alt+Q` | `Ctrl+Shift+S` cướp "Save As" toàn hệ thống; `Win+Shift+S` là của Snipping Tool |
| Phím tắt bắt buộc có phím bổ trợ | Phím trần biến mọi lần gõ chữ đó thành lệnh chụp |
| Tạo mã dùng lại `PreviewWindow` | Mã tạo ra cũng chỉ là ảnh. Truyền analysis rỗng, không cần cờ chế độ |
| Bỏ test UI tự động | Không đáng cho tool cá nhân; ViewModel mỏng và test ViewModel là đủ |
| **Chấp nhận không có OCR tiếng Việt** | Đã kiểm chứng: Windows OCR không phát hành gói `vi-VN` (§8 R1). Từ chối Tesseract vì không đáng đổi lấy 15–30MB model + phụ thuộc native. Thêm sau được, kiến trúc đã sẵn |
| **.NET 10, `App` mang TFM Windows** | `App` phải tham chiếu `Snaptic.Windows` để nạp DI → buộc mang TFM `net10.0-windows…`. Hệ quả: thêm macOS sau này cần multi-target `App` + đăng ký DI theo điều kiện, **không chỉ "đổi một dòng"** như §4 nói. `Core` vẫn sạch, nên đây là việc vặt ở tầng App, không phải viết lại |
