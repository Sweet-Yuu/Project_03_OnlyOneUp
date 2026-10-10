# Kiểm thử dự đoán đầu vào mang tổ — MainMap_Online

Chạy host và client với cùng bản code mới. `NestEggFrame` đã thêm số đầu vào được xác nhận,
vận tốc bộ lọc và độ dài bước mô phỏng nên không dùng lẫn bản build cũ/mới.

Trong scene, Nest đã bật `_predictCarryInputs`. Cơ chế mới được ưu tiên thay cho
dự đoán hiển thị cũ; không cần thêm component vào player hoặc gán lại trong Inspector.

1. Client bắt đầu đi, đổi hướng và dừng: tổ phản hồi từ mô phỏng local, không đợi ACK.
2. Host đứng yên, client đi/nhảy; sau đó đổi vai. Thử cả hai cùng di chuyển và xoay tổ.
3. Với RTT khoảng 100–200 ms và jitter, tổ không bị kéo lùi mỗi lần có snapshot mới.
4. Mất một số gói đầu vào: các batch gửi lại phục hồi thứ tự; host không áp dụng nhiều
   đầu vào trong cùng một bước vật lý. Kiểm tra trứng không bị bật ra do nhịp nhận gói.
5. Reset trứng, khởi động lại lượt chơi, ngắt rồi kết nối lại: input thuộc thế hệ cũ
   không được áp dụng vào lượt mới.
6. Mất mạng kéo dài: lịch sử tối đa 128 input. Khi đầy, dự đoán dừng thêm input thay vì
   bỏ dữ liệu chưa xác nhận. Khi phục hồi, kiểm tra quá trình hội tụ và bắt đầu đi lại.
7. Trứng rơi khỏi tổ: kết quả rơi/vỡ vẫn do host quyết định. Đây không phải rollback
   toàn bộ Rigidbody/PhysX của trứng hay CharacterController của người chơi.

Kiểm thử tự động: `Tests/RunNestCarryTests.ps1`. Bộ kiểm thử chạy Unity trong project tạm,
kiểm tra cùng phép mô phỏng ở hai phía, đối chiếu ACK lịch sử, replay, bù lỗi hiển thị,
reset và giả lập RTT/jitter/mất gói. Khâu vận chuyển trong bài kiểm tra được giả lập;
chưa thay thế cho phiên Netcode host/client thực tế hoặc kiểm thử va chạm trên toàn map.
