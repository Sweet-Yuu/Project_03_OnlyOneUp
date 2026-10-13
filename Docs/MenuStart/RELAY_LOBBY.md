# Relay room lobby

Trong Menu_Scene: Start -> Host tạo phòng Relay; Start -> Join -> nhập mã -> Join kết nối cùng phòng. Bảng RoomLobbyCanvas hiển thị mã, Copy, Players N/2 và danh sách tên. Back ngắt kết nối rồi trở về Host/Join. Không có Match Settings và chưa tải map/spawn nhân vật.

## Avatar

Chọn RoomLobbyCanvas (dù đang inactive) -> RoomLobbyView -> kéo sprite vào Host Avatar và Client Avatar. Chọn ảnh theo vai trò server/client, không theo máy đang xem. Khi chưa có ảnh dùng ô vàng cho host, ô xanh cho client.

## Kiến trúc

- MenuRelayController nối UI với RelaySession/IRelayRoomService đang có.
- RelayRoomRoster triển khai IRoomRoster: tên từ PlayerNamePreferences gửi trong connection approval payload, host kiểm tra độ dài và sức chứa rồi phát snapshot bằng NGO named messages qua Relay. Client chỉ nhận snapshot từ server. Danh sách chứa ClientId, tên, vai trò; có thể dùng tiếp cho nameplate sau này.
- RoomLobbyView chỉ hiển thị dữ liệu, copy mã và chọn avatar.
- MenuNetworkManager có connection approval, không có PlayerPrefab và tắt NGO scene management trong bước phòng chờ. Khi làm chuyển màn chơi phải cấu hình luồng scene/spawn tương ứng; không ghép thẳng với NetworkManager đang có trong map.
- Giữ nguyên RelayRoomService, RelaySession và scene test hiện có. Relay dùng allocation một client (host + một khách), DTLS.

Tên hiển thị lưu trên máy, được đồng bộ tới người cùng phòng, chưa lưu thành Unity Authentication player profile. Host rời thì phòng đóng; không có host migration.

Tham khảo API: https://docs-multiplayer.unity3d.com/netcode/2.3.2/basics/connection-approval/ và https://docs-multiplayer.unity3d.com/netcode/2.3.2/advanced-topics/message-system/custom-messages/. Chữ ký được đối chiếu package NGO 2.13.3 cài trong dự án.

Kiểm tra: xem relay-validation.txt và relay-smoke-result.txt. MenuRelaySmokeCheck là công cụ Editor kiểm tra endpoint guest tạm thời, không vào build và không lưu đối tượng thử vào scene.

Kết quả: Host qua UI tạo mã thật; Copy khớp clipboard; Back trở về chọn phòng. Hai endpoint NGO trong cùng Unity process kết nối qua Relay thật, xác nhận tên/vai trò/mã và 2/2 ở cả hai đầu; guest rời cập nhật về 1/2. Chưa chạy hai máy vật lý hoặc hai bản build độc lập. Các lỗi cấu hình ở lần đầu của công cụ smoke đã được sửa, lần chạy cuối PASS.
