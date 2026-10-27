# Puzzle giải mã ký hiệu

## Phạm vi đã chọn
- Người chơi suy luận giá trị của tròn, tam giác, vuông từ ba tổng từng cặp, rồi nhập mã theo thứ tự đó để mở đường.
- Chỉ tạo file trong `Assets/_Hoang`. Không sửa scene, player, prefab cũ, network prefab registry, vật liệu chung, package hoặc project settings.
- Bàn giao prefab độc lập; người dùng tự đặt và kiểm tra trong scene.

## Luật chơi
- Mặc định: tròn + tam giác = 7; tam giác + vuông = 5; tròn + vuông = 8.
- Mỗi ký hiệu là một chữ số 0–9; mã là tròn → tam giác → vuông. Đáp án mặc định dành cho người thiết kế: 523.
- Bảng gợi ý và đáp án cùng xuất phát từ ba tổng trong Inspector, tránh chỉnh gợi ý mà quên đổi đáp án.
- Đứng gần bàn nhập, bấm E, nhập ba chữ số, Enter xác nhận; Backspace xóa; E thoát. Nhập sai có phản hồi và được thử lại. Nhập đúng mở cổng đến hết lần chơi.
- Không khóa chuột, dừng thời gian hoặc can thiệp controller. Bàn nhập dùng bàn phím, bảng chữ trong thế giới là phần hiển thị.

## Cách tiếp cận
- Đã cân nhắc giải mã ký hiệu và suy luận thứ tự. Người dùng chọn giải mã ký hiệu.
- Tái sử dụng `IPuzzleInteractable` / `PuzzleGate`; tách luật C# thuần, điều phối trạng thái và bàn nhập.
- Chạy offline khi chưa có phiên mạng; khi online server kiểm tra đáp án và khoảng cách, đồng bộ mở cổng cho các client, kể cả client tham gia muộn.
- Prefab đặt sẵn trong scene dùng NetworkObject của scene. Không tự đăng ký hoặc spawn động.

## Kiểm chứng và giới hạn
- Kiểm tra tự động: đáp án, tổng vô nghiệm/ngoài miền, chữ số 0 đầu, dữ liệu nhập sai, mở khóa không bị đóng lại bởi lần nhập sau.
- Compile bằng Unity và kiểm tra wiring của prefab nếu Editor khả dụng. Không tự vào Play Mode hoặc sửa/lưu scene.
- Người dùng tự kiểm tra hình thức, va chạm, camera thực tế và host/client trong scene của mình.
