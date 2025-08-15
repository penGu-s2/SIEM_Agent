# SIEM Agent Dashboard JavaScript

## Toggle Collector System

### Cách hoạt động

Khi người dùng click nút bật/tắt collector, hệ thống sẽ:

1. **Hiển thị dialog xác nhận** với thông tin chi tiết về hành động
2. **Gửi message tới C#** qua WebView2 với action `toggle`
3. **C# xử lý** bằng cách:
   - Đọc file `fluent-bit.conf`
   - Thêm/xóa `[OUTPUT]` block tương ứng với tag của collector
   - Ghi lại file cấu hình
4. **Cập nhật UI** bằng cách gọi `updateCollectorsFromCSharp()`
5. **Hiển thị thông báo** thành công/thất bại

### Cấu trúc file fluent-bit.conf

```ini
[INPUT]
    Name         winlog
    Tag          winlog
    # ... các cấu hình khác

[OUTPUT]
    Name         file
    Match        winlog          # Tương ứng với Tag của INPUT
    Path         .\logs\
    File         winlog.log      # Tên file log
    Format       plain
    Retry_Limit  3
```

### Luồng xử lý

```
JavaScript (dashboard.js) 
    ↓ (postMessage)
WebView2 (WebViewForm.cs)
    ↓ (WebMessageReceived)
UpdateOutputBlockByTag()
    ↓ (đọc/ghi file)
fluent-bit.conf
    ↓ (reload)
updateCollectorsFromCSharp()
    ↓ (render)
UI Dashboard
```

### Các trạng thái

- **Active (🟢)**: Collector đang chạy, có `[OUTPUT]` block, ghi log vào file
- **Inactive (🔴)**: Collector đã dừng, không có `[OUTPUT]` block, không ghi log

### Thông báo

Hệ thống sử dụng notification system để hiển thị:
- ✅ Thành công: Khi bật/tắt collector thành công
- ❌ Lỗi: Khi có lỗi xảy ra
- ℹ️ Thông tin: Khi đang xử lý
- ⚠️ Cảnh báo: Khi có vấn đề

### Bảo mật

- Chỉ cho phép toggle collector đã được cấu hình
- Xác nhận trước khi thực hiện thay đổi
- Log tất cả thay đổi cấu hình
- Validate dữ liệu đầu vào
