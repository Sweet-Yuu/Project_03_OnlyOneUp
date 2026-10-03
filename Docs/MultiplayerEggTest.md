# Thử nghiệm tổ và trứng qua Relay

Mở `Assets/Scenes/SceneTest_Multi_Egg.unity`. Scene này được sao chép từ bản
`SceneTest_Multi` đã lưu trên đĩa. Các thay đổi chưa lưu trong Editor không được
đưa vào bản sao. Build Settings hiện chỉ bật scene thử trứng.

## Cách kiểm tra

1. Đợi Unity nhập và biên dịch các script, kiểm tra Console không có lỗi đỏ.
2. Mở scene thử trứng và build lại. Bản build cũ không tương thích với trạng thái
   mạng mới. Editor và bản build phải chạy cùng scene và cùng phiên bản code.
3. Create Room ở một bên, Join Room ở bên kia. Trứng chỉ xuất hiện khi đủ hai người.
4. Để cả hai đứng yên: trứng rơi nhẹ xuống đáy tổ và ổn định trên đó.
5. Di chuyển, xoay và dừng: Host mô phỏng trứng; Client hiển thị tổ và trứng từ
   cùng snapshot. Client không mô phỏng va chạm của trứng.
6. Host bấm **Drop Egg (test)**: trứng được chuyển ra ngoài tổ. Khi chạm đất,
   cả hai bên phải hiện trạng thái vỡ và trứng màu đỏ đứng yên.
7. Host bấm **Reset Egg (test)**: cả hai trở về trạng thái trứng nguyên vẹn ở tổ,
   không nội suy từ vị trí rơi cũ về vị trí mới.
8. Client thoát rồi vào lại: Host chuyển sang chờ khi thiếu người; khi đủ người,
   trứng được đặt lại. Đổi bên làm Host để kiểm tra cả hai chiều.

## Phạm vi bản thử

- Model lấy từ `Pink_egg_Unity.fbx`, chuẩn hóa kích thước khi chạy. Collider của
  trứng là capsule; collider của model bị tắt. Đây là hình dạng va chạm gần đúng.
- Chỉ Host chạy Rigidbody động, phát hiện va chạm và quyết định vỡ. Client giữ
  Rigidbody kinematic và tắt collider trứng. Không có lực thứ hai từ Client.
- Trứng chạm vật ngoài tổ sẽ vỡ; va chạm trong tổ chỉ làm vỡ khi tốc độ va chạm
  đạt `Egg Break Speed` (mặc định 6). Có khoảng bỏ qua va chạm 0,35 giây sau reset.
- Trạng thái vỡ hiện bằng màu đỏ và dừng trứng, chưa dùng model vỡ/hiệu ứng.
- Tổ và trứng dùng một NetworkVariable chứa cặp trạng thái, bộ đệm tối đa 32
  snapshot và cùng thời điểm nội suy. `Presentation Delay` mặc định 0,1 giây.
  Đây là bản thử đơn giản dùng truyền tin reliable, chưa tối ưu băng thông.
- Client vẫn có thể thấy tổ trễ so với nhân vật mình. Chưa có prediction/replay
  cho cụm nhân vật–tổ–trứng; thay đổi này không phải bản sửa dứt điểm hiện tượng
  tổ chạy theo nhân vật sau khi thả phím. Nội suy hình ảnh cũng không bảo đảm
  tiếp xúc tuyệt đối giữa các snapshot khi tổ xoay rất nhanh.
- Chưa xử lý tổ bị tường chặn, hồi sinh cả đội về checkpoint hoặc coin chung.

Scene cũ vẫn dùng NetworkTransform để đồng bộ tổ và không bật `Enable Egg Test`.
Scene mới bỏ NetworkTransform trên Nest để tránh hai hệ thống ghi cùng Transform.
Không thêm NetworkObject/NetworkTransform riêng cho quả trứng được tạo lúc chạy.

## Kiểm tra đã thực hiện khi tạo bản thử

Đã biên dịch các script mạng bằng trình biên dịch C# và thư viện Unity/Netcode
của dự án; không có lỗi C#. Đã kiểm tra tham chiếu collider, model và cấu hình
scene/build trên đĩa. Chưa chạy Play Mode hoặc phiên Relay hai máy với bản mới;
kiểm tra vật lý và cảm giác chuyển động cần thực hiện theo các bước trên.
