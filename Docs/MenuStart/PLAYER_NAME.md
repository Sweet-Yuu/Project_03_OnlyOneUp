# Đặt tên trước menu

Menu_Scene có PlayerNameCanvas màu nâu, cùng font và màu với bảng phòng. Ba nút chính ẩn và MenuStartController tắt cho đến khi xác nhận tên. Controller riêng mở lại menu bằng UnityEvent, không thay code MenuStartController.

Tên: tối đa 20 ký tự, giữ nguyên hoa/thường, loại khoảng trắng hai đầu, từ chối tên trống/chỉ có dấu cách. TMP tắt rich text cho tên nhập. Continue hoặc Enter xác nhận. Lưu cục bộ qua IPlayerNameStore / PlayerNamePreferences, khóa Player.DisplayName trong PlayerPrefs. Lần mở sau bỏ qua form nếu đã có tên. Chưa gửi tên qua Relay.

Đã kiểm tra Play Mode: lần đầu thấy bảng tên và không thấy menu; DuckTest -> Continue mở menu chính; chạy lại bỏ qua form. Đã dừng Play Mode, scene lưu ở trạng thái ban đầu. Bản sao trước chỉnh sửa: Menu_BeforeName_*.unity trong thư mục này.
