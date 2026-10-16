# Form nhập mã phòng

Menu_Scene đã gắn luồng Start → Join → Enter room code. Form nhỏ ở giữa màn hình, ô nhập bên trái, nút Join bên phải; Back trở về Host/Join. Controller riêng JoinCodePanelController trên Canvas, dùng reference serialized và UnityEvent, không phụ thuộc Relay.

Ô nhập nhận chữ/số, tối đa 12 ký tự, tự đổi sang chữ hoa. Join chỉ bật khi có mã. Submit phát sự kiện `_joinRequested` với mã đã chuẩn hóa; sự kiện hiện chưa nối mạng. Escape/gamepad B được xử lý trong LateUpdate để tránh cùng một lần bấm lùi cả hai màn hình.

Kiểm tra trực tiếp Play Mode ngày 2026-10-12: Start mở lựa chọn phòng; Join mở form; nhập ab12cd hiển thị AB12CD; Back trở về Host/Join. Không thấy lỗi biên dịch C# hoặc NullReferenceException trong Editor.log của phiên kiểm tra. Chưa xác nhận Escape/gamepad bằng thiết bị thật. Đã thoát Play Mode, giữ scene ở menu ban đầu; mã thử không lưu vào scene.

Thay đổi logo ẩn khi vào bảng phòng đã có trong MenuStartController của dự án và được giữ nguyên. Không sửa Relay, model hay background. Công cụ JoinCodePanelSetup chỉ dùng để thiết lập lần đầu, không cần chạy lại khi mở scene.
