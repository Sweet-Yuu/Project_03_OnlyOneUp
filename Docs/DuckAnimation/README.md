# Duck — nhìn quanh rồi gãi đầu

File: Duck_Look_Scratch_v01.blend. Action: Duck_LookAround_HeadScratch (đánh dấu asset và fake user).
255 frame, 30 fps, khoảng 8,5 giây. Nhấn Space trong viewport để xem; Blender phát lặp timeline, clip được tạo kết thúc ở pose đứng trung tính.

- Frame 1–131: đầu nhìn trái, phải, hơi ngẩng và quay về trước. Chest lắc nhẹ theo; chuyển động phân bố qua ba đốt cổ và Head.
- Frame 132–171: nâng cánh phải (Wing.R) theo cung ngoài, nghiêng đầu về cánh.
- Frame 172–207: ba nhịp gãi bên đầu, dùng Wing.R + WingTip.R; Primary giữ thành nhóm gọn.
- Frame 208–255: hạ cánh, thân ổn định và kết thúc ở pose ban đầu.

Các key quaternion dùng Bezier AUTO_CLAMPED. Có marker cho các mốc chính. Không sửa mesh, skin weight, rest matrix, hierarchy hoặc thêm constraint. Model đang mở là bản đã có DEF_Primary_01–05.L/R nhưng không có constraint cổ; dùng đúng scene đó, không thay bằng file cũ.

Kiểm tra: render toàn chuyển động mỗi 3 frame thành GIF 10 fps và xem các góc trước/chéo ở mốc chính. Kiểm tra đủ 255 frame: ma trận Foot.L/R không đổi; ma trận pose đầu/cuối trùng nhau. Mesh/weight hash và toàn bộ rest matrix giữ nguyên. Tiếp xúc gãi đầu là pose FK tạo hình, không dùng collision solver; chưa kiểm tra trong Unity, chưa export hoặc ghi đè FBX.

Nguồn trước chỉnh: Duck_Look_Scratch_v01_Source.blend. Xem nhanh: Duck_Look_Scratch_Preview.gif; Storyboard.jpg. File blend mở tại frame 1.
