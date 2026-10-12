# Cánh vịt mở rộng — bản chỉnh sửa để duyệt hình dáng

Đã lưu **Duck_Wings_Expanded.blend**, kết thúc ở frame 1, cánh khép trung tính. Bản trước chỉnh được giữ ở Duck_Wings_Expanded_Source.blend. Không ghi đè file khảo sát, file gốc hay FBX trong Assets.

## Hình dáng và rig

- Sửa 18 mesh: Wing_L/R, Wing_L/R_Covert_00–02, Wing_L/R_Primary_00–04. Giữ topology và material; kéo dài phần ngoài cánh, nới bề rộng dần về đầu cánh, giữ vị trí vùng vai. Giữ nguyên weight hai mesh cánh chính.
- Bố trí lại 5 Primary mỗi bên, dài ngắn theo thứ tự, thân lông mỏng hơn và mở theo cung quạt. Ba Covert mỗi bên dài/rộng lại để che gốc lông.
- Thêm **10 deform bone** DEF_Primary_01.L…05.L và .R; parent trực tiếp vào WingTip đúng bên, collection Primary Feathers. Mesh Primary_00 tương ứng bone 01, tiếp tục đến Primary_04 → bone 05.
- Mỗi Primary dùng bone riêng: 100% weight ở thân lông; 13% chiều dài gần gốc chuyển mượt từ 65% bone lông + 35% WingTip tới 100% bone lông. Nhờ vậy lông quay tương đối cứng, chỉ gốc mềm. Covert cập nhật blend Wing/WingTip theo chiều dài. Không thêm driver/constraint hay dependency cycle.

## Kiểm tra và số đo

| Pose | Sải ngang X của mesh cánh đã deform | Điểm thấp nhất Z |
|---|---:|---:|
| Khép mới | 1.989814 | 0.255247 |
| Mở vừa mới | 3.782578 | 0.842756 |
| Mở rộng mới | 4.064280 | 1.226178 |
| Mở rộng + nghiêng thân/vươn cổ | 4.064280 | 1.247124 |

Mở rộng cũ 3.083576 → mới 4.064280: **+31.8%**, trong mục tiêu 25–35%. Sải khép cũ 2.000880 → mới 1.989814, gần như giữ nguyên. Đây là đơn vị tọa độ Armature/scene, không mặc định là mét trong Unity. Z=0 được dùng làm mốc mặt đất. Các ảnh trước/sau dùng cùng camera trực giao 4.65 và cùng độ phân giải, không phóng lớn ảnh sau.

Đã render/quan sát khép, vừa, rộng từ trước, bên và sau; thêm 3 góc pose mang tổ. Vai còn tiếp giáp thân, không thấy hở vai hoặc xuyên thân rõ ở các góc kiểm tra. Khi khép lông dài nằm dọc bên thân, đầu lông cách đất 0.255. Khi mở có năm đầu lông tách rõ, chồng lớp ở gốc; nhìn bên hông cánh mỏng hơn do nhìn gần cạnh. Đây là đánh giá hình ảnh của các pose đã lưu, không phải kiểm chứng va chạm mọi tam giác hay mọi chuyển động.

Validation: **56 xương hiện có giữ nguyên matrix_local chính xác (sai số 0), parent và deform flag**; hướng trục/rest orientation không đổi. Tọa độ vertex và weight mọi mesh ngoài cánh có hash y hệt trước chỉnh. Tất cả 18 Armature modifier còn nguyên và trỏ Duck_Rig; tổng weight sai số tối đa 1.28e-7. Chi tiết ở expanded_validation.json.

Scene nguồn lần này không có CTRL_Beak/constraint cổ (đã ghi trong khảo sát). Không tự ghép hay sửa rig IK cổ ở file Duck_NeckIK.blend. Pose mang tổ chỉ thử bằng xoay Chest/cổ; chưa thử tương tác với một cái tổ thật hoặc runtime IK Unity.

## Dùng lại pose trong Blender

Action đang chạy **WINGS_PosePreview** chỉ key 14 bone cánh: frame **1 khép**, **30 mở vừa**, **60 mở rộng**. Các Action asset **WINGS_Closed**, **WINGS_Medium**, **WINGS_Wide** cũng lưu riêng ở frame 1. **WINGS_CarryTest** lưu mở rộng + Chest local X 18°, Neck_01 −8°, Neck_02 −5°, Neck_03 −3°, Head −2°, không kéo dài xương. Action này là bài thử riêng, không nằm trong animation cánh đang chạy.

Có thể kéo timeline 1–60 để xem. Các góc mở khớp vai/tip là axis-angle trên local axis thực tế, không phải Euler Z. Mở vừa dùng 60% toàn bộ góc mở rộng; cảm giác/sải mở không tuyến tính theo góc.

## Transform và Unity

14 bone cánh dùng **rotation_mode QUATERNION**. Bảng dưới là **Blender pose-bone rotation_quaternion (w,x,y,z), delta trong hệ trục local rest của từng bone**, không phải quaternion tuyệt đối của transform có parent. Khép bằng identity; không đổi location/scale để mở cánh. Rest matrix và parent từng bone nằm trong expanded_pose_transforms.json; CSV đi kèm có toàn bộ số liệu.

| Bone | Khép (w,x,y,z) | Mở vừa | Mở rộng |
|---|---|---|---|
| Wing.L | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| Wing.R | (1.0000000, 0.0000000, -0.0000000, -0.0000000) | (1.0000000, 0.0000000, -0.0000000, -0.0000000) | (1.0000000, 0.0000000, -0.0000000, -0.0000000) |
| WingTip.L | (1.0000000, 0.0000000, -0.0000000, 0.0000000) | (1.0000000, 0.0000000, -0.0000000, 0.0000000) | (1.0000000, 0.0000000, -0.0000000, 0.0000000) |
| WingTip.R | (1.0000000, 0.0000000, 0.0000000, -0.0000000) | (1.0000000, 0.0000000, 0.0000000, -0.0000000) | (1.0000000, 0.0000000, 0.0000000, -0.0000000) |
| DEF_Primary_01.L | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) |
| DEF_Primary_02.L | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) |
| DEF_Primary_03.L | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| DEF_Primary_04.L | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| DEF_Primary_05.L | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| DEF_Primary_01.R | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| DEF_Primary_02.R | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| DEF_Primary_03.R | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, 0.0000000, 0.0000000, 0.0000000) |
| DEF_Primary_04.R | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) |
| DEF_Primary_05.R | (1.0000000, 0.0000000, 0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) | (1.0000000, -0.0000000, -0.0000000, 0.0000000) |


Wing mở rộng 58°, WingTip 12°. Fan Primary 01…05 quay quanh local Z: bên L −24°, −12°, 0°, +12°, +24°; bên R dấu ngược lại. Mở vừa nhân các góc với 0.6. Bone mới có head tại gốc lông trong hình dáng khép; khép mới chính là rest geometry cho vùng lông mới, không thay rest transform bất kỳ bone cũ nào.

Khi được duyệt xuất sang Unity, cần xuất thêm 10 deform bone và weight tương ứng; code chỉ xoay Wing.L/R hiện tại chưa mở được quạt. Điều khiển đồng thời Wing, WingTip và 5 Primary mỗi bên. Cách ít sai trục nhất là bake/export các Action thành clip, rồi blend Closed/Medium/Wide trên các bone cánh. Lần này chưa xuất FBX hoặc thay Assets.

Nếu điều khiển procedural: chụp localRotation khép **sau import Unity** cho mỗi bone; tạo delta trong hệ trục local bone đã import và áp dụng `qClosedUnity * qDeltaUnity`. Có thể lấy delta từ các clip pose đã import: `Inverse(qClosedUnity) * qPoseUnity`, rồi Slerp giữa các mẫu. Không chép trực tiếp Euler hoặc chỉ đổi thứ tự thành (x,y,z,w) từ bảng Blender: khác handedness, trục FBX/bone orientation có thể làm sai hướng. Chuyển theo ma trận cơ sở local đúng cho từng bone: `RdeltaUnity = C * RdeltaBlender * inverse(C)`, với C được xác định từ pipeline export/import; chưa xác nhận C vì chưa export/import bản này. Việc đổi wxyz sang xyzw chỉ là đổi thứ tự thành phần, không phải đổi hệ tọa độ.

Giới hạn còn lại: lông là các mesh rời chồng lớp có chủ ý; các góc fan/roll vượt phạm vi mẫu chưa được kiểm tra. Bộ pose này phục vụ silhouette giữ thăng bằng, chưa phải chu kỳ vỗ cánh hoặc mô phỏng khí động. Không có constraint/driver Blender nào cần chạy trong Unity; biến dạng cuối cùng hoàn toàn do deform bone/weight.

Ảnh: expanded_before_after.jpg, expanded_poses_3x3.jpg, expanded_carry_3views.jpg; PNG riêng từng pose/góc cùng thư mục.
