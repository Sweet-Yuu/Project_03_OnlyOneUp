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

## Vật lý trứng phối hợp

`MainMap_Online` bật `Assist Egg Carry` trên Nest. Khi được hỗ trợ, trứng mô phỏng
lăn theo trọng lực dọc sàn, có lực cản nhưng không có lò xo kéo về tâm.
`Egg Travel = 0.75` đơn vị giới hạn khoảng dịch ngang tới viền của prefab hiện tại.
Trứng lăn ngang tại đáy tổ, không nâng độ cao theo khoảng cách tới viền.
Chỉ khi góc giữa trục up của tổ và phương thẳng đứng vượt `Egg Release Tilt = 70` độ,
trứng mới bắt đầu bước thoát qua thành (khoảng 0.25 giây) rồi rơi bằng Rigidbody.
Rigidbody ở chế độ kinematic trong giai đoạn này để sàn tổ không truyền xung lực
hất trứng ra khi một người nhảy. Trứng vẫn kiểm tra va chạm môi trường để quyết định vỡ.

Hai lần bật nhảy thật của hai người trong `Combined Jump Window = 0.2` giây sẽ
thả trứng sang Rigidbody động. Vận tốc hất thêm là `Egg Launch Up = 15` và
`Egg Launch Side = 5`, cộng vận tốc tại điểm đặt trứng trên tổ. Không tự hút trứng
vào tổ sau khi thả; reset trứng sẽ bật lại hỗ trợ. Các tham số được đọc khi Nest spawn.

1. Host nhảy một mình, rồi đổi sang client: trứng lăn tới viền thấp, không bị kéo về tâm.
   Sau khi đáp và tổ nằm ngang, trứng vẫn sát đáy, không treo lơ lửng trên viền.
2. Hai người nhảy cách nhau hơn 0.2 giây: trứng ở trong tổ nếu độ nghiêng chưa vượt 70°.
3. Hai người cùng bật nhảy hoặc lệch khoảng 0.1 giây: trứng vượt thành và rơi tự do.
4. Bấm nhảy khi đang trên không hoặc bị chặn hoàn toàn: không tính thêm lần bật nhảy.
5. Lặp lại với RTT 100–200 ms: ghép theo thời điểm bật nhảy đã bù đồng hồ NGO,
   không theo thời điểm RPC đến. Quyết định thả trứng vẫn cần chờ host nhận đủ hai tín hiệu.
6. Reset giữa hai lần nhảy, ngắt/kết nối lại: tín hiệu thuộc thế hệ cũ không hất trứng mới.
7. Kiểm tra nút thả trứng thử: trứng được reset rồi thả sang vật lý tự do.
8. Nghiêng tổ sang hai phía: dưới 70° giữ trứng tại viền; trên 70° trứng thoát ra.
   Xoay yaw trên mặt phẳng không được làm trứng rơi; respawn khôi phục trứng ở tâm.

Kiểm thử tự động có mô phỏng PhysX với các collider lấy từ prefab Nest thật;
phần art và network component được bỏ khỏi bản sao trong project kiểm thử tạm.

## Ba mức hư hại của trứng

`MainMap_Online` bật `Progressive Egg Damage` và gán model `Broken_egg2` có sẵn.
Host lưu số lần va chạm trong `NestEggFrame.EggDamage`; phải chạy host/client
cùng bản build mới vì snapshot có thêm trường này.

1. Va collider môi trường lần đầu: có vết rạn nhẹ, trứng vẫn chuyển động.
2. Rời collider rồi va lại: nhiều vết rạn hơn, vẫn chưa vỡ.
3. Va lần thứ ba: ẩn vỏ, hiện model lòng trắng/lòng đỏ, phát sự kiện vỡ một lần.
4. Giữ trứng tì vào cùng collider nhiều giây: không tự tăng số lần va chạm.
5. Chạm tổ, người chơi hoặc trigger: không tăng hư hại. Kiểm tra cả lúc được tổ
   hỗ trợ (overlap capsule) và lúc đã bị hất ra (collision callbacks).
6. Reset: khôi phục vỏ nguyên và bộ đếm về 0 trên cả hai máy; client nhận snapshot
   có mức rạn 1/2/3 phải hiện đúng ngay, không cần nhận đủ các va chạm trước đó.

Vết rạn được tạo trên bản sao material của từng trứng, không sửa material/model gốc.
Hình model vỡ được đặt tại điểm va chạm ở lần thứ ba.

## Hiệu ứng khi trứng nứt

Ở Nest → Local egg damage feedback: `Egg Damage Shake Duration` mặc định 0.35 giây,
`Egg Damage Shake Strength` 0.07 đơn vị và `Egg Damage Flash Duration` 0.9 giây.
Lần nứt thứ hai nhân cường độ và thời gian với 1.35; tham số được đọc khi Nest spawn.

1. Trứng nứt lần 1/2: mỗi máy rung camera local ngắn và hiện đỏ gradient ở bốn góc,
   mờ dần, vùng giữa màn hình vẫn rõ. Client phát cùng lúc mức rạn được hiển thị.
2. Camera vẫn theo người chơi, xoay/zoom được; kết thúc không còn lệch vị trí/góc.
3. UI vẫn nhận chuột/chạm trong lúc lớp đỏ hiện; thử màn hình ngang và dọc.
4. Snapshot lặp không phát lại hiệu ứng; vào muộn khi trứng đã nứt không bị rung.
5. Reset, vỡ lần ba hoặc thoát phòng giữa hiệu ứng: lớp đỏ biến mất và camera được trả lại.

## Đường dự đoán trứng rơi

`MainMap_Online` bật `Show Egg Trajectory` trên NetworkNest. Sau khi trứng rời tổ,
đường đỏ dự đoán đường bay và vòng tròn đánh dấu điểm va chạm collider đầu tiên.
Đây là dự đoán theo vận tốc và trọng lực hiện tại, không dự đoán nảy sau va chạm.
Kiểm tra host/client, trứng văng ngang, rơi vào nền dốc, reset và trứng vỡ;
đường phải ẩn khi ở trong tổ hoặc không còn nguyên vẹn. Không có bề mặt trong
khoảng dự đoán thì không hiện vòng tròn. Cả hai máy cần cùng build vì snapshot có thêm vận tốc trứng.

## Checkpoint chung và hồi sinh

`MainMap_Online` có component `NetworkRoundManager` trên cùng NetworkObject với Nest.
Manager giữ checkpoint, RPC va chạm, thời gian chờ và quyết định hồi sinh;
`NetworkNest` chỉ thông báo trứng vỡ và thực hiện chuẩn bị/reset qua `IRoundResetTarget`.
Player gửi va chạm qua `IRoundSession`. Component offline trên cờ
`CheckPoint/Checkpoint_Flag3` được thay bằng `OnlineCheckpoint`, ID 1 và collider
đã bật `Is Trigger`. Không dùng singleton `SpawnPlayer` của chế độ offline.

- Chưa đạt checkpoint: hồi sinh quanh `Start Center` trên **NetworkRoundManager**
  (45, 2.1, 30 trong scene hiện tại). Trường cũ trên NetworkNest chỉ dùng ở scene không có manager.
- Một trong hai owner chạm collider checkpoint: host lưu tâm spawn cho cả đội.
  `Spawn Center` trên `OnlineCheckpoint` có thể gán một Transform ở nền phẳng trống;
  để trống thì lấy vị trí cờ. Host kiểm tra đủ chỗ cho hai player trước khi chấp nhận.
- Trứng vỡ: sau `Egg Break Respawn Delay` mặc định 1 giây, chuẩn bị lại hai người,
  tổ và trứng nguyên vẹn tại tâm đã lưu. Chạm collider/trigger có tag `Dead`
  (kể cả tag ở object cha) sẽ bắt đầu hồi sinh ở bước vật lý kế tiếp.
- Điểm lưu còn hiệu lực qua các lần hồi sinh/ngắt kết nối trong phòng hiện tại;
  khởi động phiên mới trở lại Start Center. Chưa có lưu checkpoint ra ổ đĩa.
- Checkpoint mới: gắn `OnlineCheckpoint`, dùng ID dương khác nhau, có collider;
  ưu tiên trigger. Các vùng được cache khi manager spawn, nên đặt sẵn trong scene.

Kiểm tra trên hai máy: chưa đạt cờ rồi chết; host chạm cờ rồi client chạm Dead;
client chạm cờ rồi làm trứng vỡ; chết lặp lại; cả hai chạm Dead cùng lúc; reconnect;
RPC va chạm cũ đến sau teleport không được đổi điểm hay gây thêm lần hồi sinh.
Kiểm tra camera chuyển thẳng tới điểm hồi sinh, trứng hết rạn và hiệu ứng đỏ đã tắt.
