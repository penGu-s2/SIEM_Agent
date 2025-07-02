# SIEM Agent - Fluent Bit WinForms

Ứng dụng quản lý, đồng bộ và cấu hình Fluent Bit cho hệ thống thu thập log tập trung.

## 1. Đồng bộ cấu hình Fluent Bit từ xa

Để tự động lấy và đồng bộ file cấu hình `fluent-bit.conf` từ server trung tâm, bạn cần bật tính năng đồng bộ trong file `agent_config.json`:

```json
"config_sync": {
  "enabled": true,
  "url": "https://cbgs.hanhchinhcong.net/getFluentbitConfig?key=mydreamteam123456778899",
  "interval_minutes": 5
}
```
- **enabled**: Đặt thành `true` để bật đồng bộ tự động.
- **url**: Địa chỉ API trả về nội dung file cấu hình Fluent Bit.
- **interval_minutes**: Khoảng thời gian (phút) giữa các lần đồng bộ.

Khi bật tính năng này, Agent sẽ tự động tải file cấu hình mới từ server định kỳ và áp dụng ngay (restart Fluent Bit nếu cần).

## 2. Các control chính trong ứng dụng

### DashboardControl
- Giao diện tổng quan, chứa các tab cấu hình output (HTTP, Opensearch, File) và các thông tin hệ thống.
- Là nơi điều hướng đến các control cấu hình chi tiết.

### HttpOutputControl
- Tab cấu hình gửi log qua HTTP.
- Bên trái: danh sách nguồn log (CheckedListBox), tích chọn để bật cấu hình HTTP cho từng nguồn.
- Khi tích chọn, popup nhập thông tin HTTP sẽ hiện ra (Host, Port, URI, ...).
- Bên phải: bảng hiển thị các cấu hình HTTP đã lưu, có thể xóa từng cấu hình.
- Mỗi thay đổi sẽ tự động cập nhật file `fluent-bit.conf` và restart Fluent Bit.

### OpensearchOutputControl
- Tab cấu hình gửi log qua Opensearch.
- Bên trái: danh sách nguồn log (CheckedListBox), tích chọn để bật cấu hình Opensearch cho từng nguồn.
- Khi tích chọn, popup nhập thông tin Opensearch sẽ hiện ra (Host, Port, Index, ...).
- Bên phải: bảng hiển thị các cấu hình Opensearch đã lưu, có thể xóa từng cấu hình.
- Mỗi thay đổi sẽ tự động cập nhật file `fluent-bit.conf` và restart Fluent Bit.

### LogsControl
- Giao diện xem log thu thập được từ các nguồn.
- Cho phép lọc, tìm kiếm, xem chi tiết log theo từng loại nguồn log.
- Hỗ trợ thao tác start/stop collector, làm mới dữ liệu.

### ParserControl
- Quản lý các parser (định nghĩa cách phân tích log) cho Fluent Bit.
- Cho phép thêm, sửa, xóa các parser trực tiếp trên giao diện.
- Parser sẽ được lưu vào file `parsers.conf` và áp dụng cho Fluent Bit.

## 3. Thêm/xóa cấu hình input/output qua giao diện

- **Thêm output**: Vào tab Output HTTP/Opensearch/File, tích chọn nguồn log, nhập thông tin cấu hình trong popup, nhấn Lưu. Cấu hình sẽ được ghi vào `fluent-bit.conf`.
- **Xóa output**: Chọn dòng cấu hình trong bảng, nhấn "Xóa output đã chọn".
- **Thêm/xóa input**: Thường thực hiện bằng cách chỉnh sửa file cấu hình hoặc qua giao diện nếu có hỗ trợ. Sau khi thêm/xóa input, kiểm tra lại các tab output để đồng bộ cấu hình.

## 4. Lưu ý
- Mỗi thay đổi cấu hình sẽ tự động cập nhật file `fluent-bit.conf` và restart Fluent Bit để áp dụng ngay.
- Nếu bật đồng bộ từ xa, mọi thay đổi thủ công sẽ bị ghi đè khi server gửi cấu hình mới.
- Đảm bảo các trường cấu hình nhập đúng định dạng, ứng dụng sẽ cảnh báo nếu có lỗi.

---
Nếu cần hỗ trợ thêm, vui lòng liên hệ DreamTeam!
