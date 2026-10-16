# Kiểm tra cánh vịt — chỉ thử pose

Kết luận: rig điều khiển được hai đoạn cánh, nhưng hình dáng cánh hiện tại là cánh ngắn, dày, lông xếp thành cụm. Có thể mở ngang đẹp hơn để diễn tả giữ thăng bằng; chỉ xoay WingTip không tạo được quạt lông rộng hoặc tăng chiều dài đáng kể. Muốn silhouette dạng quạt rõ rệt cần chỉnh hình dáng/bố trí lông và bổ sung điều khiển lông, không chỉ tăng góc mở trong Unity.

## Trạng thái và phạm vi

Scene đang mở có tiêu đề chưa lưu, Armature tên Duck_Rig. Đã lưu Wing_Initial_Snapshot.blend và initial_state.json trước thử. Scene hiện tại không có constraint trên các bone Neck_01–03; không có CTRL_Beak. Đây là trạng thái thực tế của scene được kiểm tra, không phải thay đổi của lần thử này. Không nạp hay sửa file Duck_NeckIK.blend trước đó.

Thử xoay bốn bone trong Pose Mode theo cả hai chiều từng trục X/Y/Z, đo mesh sau Armature evaluation và render trước/bên/sau. Kết thúc đã phục hồi rotation mode, rotation, location, scale của các bone thử, toàn bộ pose, active object, selection và Object Mode. live_verification.json xác nhận khôi phục; hash tọa độ vertex và weight của toàn bộ mesh không đổi. Bản thử riêng Duck_Wings_Wide_Trial.blend chỉ thay đổi rotation bốn bone cánh và cách hiển thị viewport; trial_validation.json xác nhận rest matrix, hierarchy, constraint, mesh, weight và pose ngoài cánh không đổi. Không apply modifier, không export FBX, không sửa code Unity.

## Mesh và skeleton

Hai chuỗi: Chest → Wing.L → WingTip.L → WingTip.L_end và Chest → Wing.R → WingTip.R → WingTip.R_end. Tất cả không có constraint. Không có bone điều khiển từng lông.

Mỗi bên có 9 mesh:

- Wing_L hoặc Wing_R: 5.954 vertex.
- Wing_L/R_Covert_00–02: ba mesh lông phủ, 1.442 vertex mỗi mesh.
- Wing_L/R_Primary_00–04: năm mesh lông dài, 1.442 vertex mỗi mesh.

Cả 18 mesh có Armature modifier trỏ Duck_Rig và vertex group Wing + WingTip đúng bên. Các bone *_end không có weight trên 18 mesh này.

WingTip ảnh hưởng 3.273/5.954 vertex của mesh cánh chính; tổng weight trung bình chiếm khoảng 33,3%. Trên từng mesh Primary, weight trung bình của WingTip chiếm khoảng 57,6–73,8%. Khi chỉ xoay WingTip.L 25° quanh trục mở, đầu lông di chuyển tới 0,229 đơn vị; cánh phải hoàn toàn đứng yên. WingTip.R cho kết quả đối xứng. WingTip hoạt động thực sự, không phải chỉ điều khiển một mảnh trang trí nhỏ.

## So sánh thực tế

| Pose | Góc quanh trục mở Wing | Góc quanh trục mở WingTip | Sải ngang mesh hai cánh |
|---|---:|---:|---:|
| Khép ban đầu | 0° | 0° | 2,000880 |
| Mở vừa | 35° | 12° | 2,757778 |
| Mở rộng thử | 58° | 12° | 3,083576 |
| Gần mức tối đa của code hiện có, chỉ vai | 64,1° | 0° | 3,049845 |
| Thử WingTip quá lớn | 58° | 40° | 3,128404 |

Các số đo là đơn vị scene, từ min X đến max X của mesh cánh đã deform, không phải chiều dài xương và không khẳng định là mét trong Unity. Trục mở là axis-angle mô tả bên dưới, không phải giá trị Euler Z.

Mở rộng tăng sải khoảng 54,1% so với khép, nhưng chỉ lớn hơn trường hợp xoay vai gần ngang khoảng 1,1%. Hai đoạn Wing/WingTip trong rest pose vốn gần thẳng: lệch khoảng 3,56°. Không có một đoạn cánh dài đang gập giấu bên trong để WingTip duỗi ra.

Ở pose 58°/12°, vai vẫn tiếp giáp thân, không thấy khe hở rõ hoặc mesh bung trong ba góc render. Đây là quan sát hình ảnh, không phải chứng nhận không giao nhau ở mọi vertex. Vai là hai mesh rời lồng vào thân, vẫn có đường nối. Lông chồng lớp từ đầu; khi tăng tip lên 40° lông cong như móc và có vùng lớp lông chồng/đâm vào nhau rõ hơn, thay vì tách thành quạt. Thử roll vai 25° làm thay đổi diện tích nhìn thấy của lông nhưng hầu như không tăng sải (3,075591).

## Trục local và pose chính xác

Local Y chạy dọc bone: xoay Y là roll/twist, không duỗi chiều dài. Local +X nâng cánh đồng thời quét ra sau ở cả hai bên. Local Z cần dấu đối nhau: +Z cho trái, −Z cho phải để mở ra ngoài. Để mở sang bên mà hạn chế quét trước/sau, dùng trục kết hợp X/Z đo từ rest geometry:

| Bone | Trục quay trong local bone (X,Y,Z) | Góc mở rộng |
|---|---|---:|
| Wing.L | (0,487250; 0; 0,873262) | +58° |
| Wing.R | (0,487250; 0; −0,873262) | +58° |
| WingTip.L | (0,431527; 0; 0,902100) | +12° |
| WingTip.R | (0,431527; 0; −0,902100) | +12° |

Rotation mode ban đầu và bản thử: QUATERNION. Pose gốc của cả bốn bone gần identity (W,X,Y,Z)=(1,0,0,0), chỉ có sai số import cỡ 1e−7; giá trị đầy đủ nằm trong initial_state.json. Rest orientation của bone không phải identity; pose quaternion là offset trong hệ bone rest, không phải trực tiếp rotation so với parent trong Unity.

Pose mở rộng, quaternion Blender (W,X,Y,Z), đã bao gồm offset pose gốc rất nhỏ:

| Bone | W | X | Y | Z |
|---|---:|---:|---:|---:|
| Wing.L | 0,874619663 | 0,236223623 | −0,000000036 | 0,423366189 |
| Wing.R | 0,874619663 | 0,236223623 | 0,000000036 | −0,423366189 |
| WingTip.L | 0,994521916 | 0,045106847 | 0,000000008 | 0,094295137 |
| WingTip.R | 0,994521916 | 0,045106847 | −0,000000008 | −0,094295137 |

Euler XYZ tương đương trong Blender, đơn vị độ, để nhập bằng tay khi đổi rotation mode sang XYZ trong bản thử:

| Bone | Khép | Mở vừa | Mở rộng |
|---|---|---|---|
| Wing.L | (0,0,0) | (16.278496, −4.413298, 30.157088) | (24.944107, −11.538026, 49.099054) |
| Wing.R | (0,0,0) | (16.278496, 4.413298, −30.157088) | (24.944107, 11.538026, −49.099054) |
| WingTip.L | (0,0,0) | (5.147657, −0.487404, 10.810654) | (5.147657, −0.487404, 10.810654) |
| WingTip.R | (0,0,0) | (5.147657, 0.487404, −10.810654) | (5.147657, 0.487404, −10.810654) |

## Áp dụng sang Unity

Assets/Scripts/DuckNeckIK.cs hiện lấy WingTip để tính hướng dọc cánh nhưng chỉ gán rotation cho Wing. Không sửa file đó trong lần kiểm tra này.

Không chép thẳng bảng Euler Blender vào Transform.localEulerAngles: bone rest orientation, trục FBX và thứ tự Euler có thể khác. Không thể xác nhận số localRotation tuyệt đối của GameObject trong Unity chỉ từ Blender. Cách triển khai phù hợp với code hiện tại là chụp restRotation trong Unity ở pose khép và tính trục từ chính skeleton đã import, riêng từng bone:

1. Khởi tạo cả bốn bone trước khi thay đổi bất kỳ pose nào.
2. Wing dùng điểm cuối là vị trí WingTip; WingTip dùng WingTip.*_end.
3. along = normalize(tip.position − bone.position); outward = root.right nhân dấu phía bone so với root.
4. hingeWorld = normalize(Cross(along, outward)); axisLocal = bone.InverseTransformDirection(hingeWorld).
5. localRotation = restRotation * Quaternion.AngleAxis(angle, axisLocal).

Góc gợi ý từ phép thử: Wing 58°, WingTip 12°. Nội suy từ 0 đến các góc này khi tăng mức giữ thăng bằng. Công thức dùng trục bone-local nên delta nhân bên phải restRotation; code hiện tại dùng trục parent-local nên nhân bên trái. Không trộn hai quy ước. Việc tính trục từ geometry giúp tránh giả định dấu góc hai bên hay copy trục Blender sang engine. Chưa chạy kiểm chứng runtime trong Unity ở lần kiểm tra này.

## Cần chỉnh gì nếu muốn cánh lớn dạng quạt

- Hình dáng: phần cánh chính đang là khối bầu ngắn, dày; lông gần song song. Cần kéo dài phần ngoài cánh và/hoặc mở rộng bề mặt cánh theo silhouette mong muốn. Chưa có cơ sở chọn một tỷ lệ scale cố định khi chưa có mẫu hình đích.
- Bố trí lông: trải các Primary quanh cung quạt, điều chỉnh góc và chiều dài từng lông; giữ độ chồng lớp hợp lý từ trước và sau.
- Rig: hai đoạn hiện đủ nâng/hạ, gập cánh đơn giản, nhưng thiếu bậc tự do điều khiển quạt lông. Có thể thêm vài bone nhóm lông hoặc bone từng Primary nếu cần xòe có animation; giữ Wing/WingTip cũ làm khung.
- Weight: không cần sửa để chữa lỗi WingTip không hoạt động, vì nó đang hoạt động. Nếu bổ sung điều khiển lông, nên để phần thân lông cứng hơn, blend chủ yếu ở chân lông; weight hiện tại trải giữa Wing và WingTip khiến lông dài bị uốn theo khớp khi xoay mạnh.

Chưa thực hiện bất kỳ chỉnh sửa hình dáng, weight, bone hay export nào trong các đề xuất trên.

Ảnh: comparison_3x3.jpg (khép/mở vừa/mở rộng từ ba hướng); before_after.jpg (khép/mở rộng/tip quá lớn). Các PNG gốc nằm cạnh báo cáo.
