# Start → Host / Join

Mở `Assets/Scenes/Menu_Scene.unity`, Play, nhấn Start game. Bảng Online game hiện Host và Join; Back quay lại ba nút chính. Host/Join chỉ là giao diện ở bước này, chưa gọi Relay; Options/Quit chưa gắn chức năng theo phạm vi đã thống nhất.

`MenuStartController` gắn trên Canvas, dùng reference serialize và sự kiện OnClick lưu trong scene. Bảng nằm ở root `RoomSelectionCanvas`, tắt mặc định, có CanvasScaler riêng 960×540. Có xử lý Escape/gamepad B khi bảng đang mở; không dùng Find/GetComponent trong Update. Có thể chỉnh màu, chữ và bố cục trực tiếp trong hierarchy.

Build Settings đã thêm Menu_Scene làm scene enabled đầu tiên. Giữ các scene trước đó; code và bảng NetworkTestUI trong scene test/map không bị sửa. Muốn kiểm tra menu hãy Play từ Menu_Scene; Play trực tiếp MainMap_Online vẫn hiển thị giao diện test như cũ.

Đã kiểm tra:
- Unity biên dịch hai script mới thành công.
- Kiểm tra reference/OnClick và mở/đóng 3 lần trong Editor.
- Play Mode: khởi đầu chỉ menu chính; click Start mở bảng; click Back về menu; click Start lần hai vẫn mở được.
- Phím Escape đã có code nhưng lần thử bằng công cụ nhập phím chưa xác nhận được kết quả; cần kiểm tra bằng bàn phím thật trong Game View.
- Đối chiếu snapshot trước sửa: chỉ 3 block có sẵn thay đổi (OnClick của Start, thêm component trên Canvas và danh sách scene roots); không xóa object cũ, không đổi background, camera, model hoặc layout nút cũ.

`Assets/Editor/MenuStartSetup.cs` là công cụ tạo scene một lần, có chặn chạy lặp để tránh nhân đôi panel. Snapshot scene trước thay đổi được lưu cạnh tài liệu này. Bước sau có thể nối HostButton/JoinButton với một controller phòng riêng và tái sử dụng RelaySession/IRelayRoomService.
