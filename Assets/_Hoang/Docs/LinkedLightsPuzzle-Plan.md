# Linked Lights Puzzle Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans; implement in this chat and record progress here.

**Goal:** Thêm puzzle đèn liên kết 3×3 mở cổng bằng prefab độc lập, người dùng tự đặt và thử trong scene.
**Architecture:** Luật C# thuần dùng chín bit biểu diễn đèn; mỗi nút đảo chính nó và hàng xóm ngang/dọc. NetworkBehaviour giữ trạng thái server-authoritative; console sở hữu input và hiển thị. Tái sử dụng IPuzzleInteractable, PuzzleGate, font và vật liệu trong `_Hoang`.
**Tech Stack:** Unity 6000.3.15f1, C#, NGO 2.13.3, Input System, TextMeshPro đã cài.

## Thiết kế đã chọn
- Người dùng chọn đèn liên kết trong hai hướng: đèn 3×3 hoặc xoay đường ống. Đèn có luật ngắn và cần suy luận các tác động chéo.
- Đèn được đánh số 1–9, từ trái sang phải, trên xuống dưới. Nhấn E ở gần để bắt đầu/thoát; 1–9 hoặc numpad chọn ô; R trả về bố cục ban đầu khi chưa giải.
- Tắt hết đèn mở cổng đến hết lần chơi. Sau khi giải, không cho thao tác hoặc reset đóng lại đường đi.
- Sinh bố cục từ các bước bấm trên bảng tắt hoàn toàn, bảo đảm luôn có lời giải. Chuỗi mặc định: 1, 2, 4, 5, 9. Không giới hạn lượt hoặc phạt sai.
- Cả hai người có thể tương tác với bảng chung; server kiểm tra player/khoảng cách và tuần tự xử lý các thao tác, đồng bộ chín đèn và cổng. Không đồng bộ trạng thái focus nhập của từng máy.

## Global Constraints
- Chỉ thêm file trong `Assets/_Hoang`; không sửa puzzle cũ, scene, player, registry, package, settings hoặc tài sản chung.
- Root prefab thường, NetworkObject ở object con Puzzle để tránh NGO tự sửa registry chung.
- Không tự Play Mode, chạy host/client hoặc lưu scene; người dùng tự kiểm tra phần đó.
- Một class một file; SerializeField private; không Find/GetComponent trong Update.

## Review Focus
- Góc/cạnh không được nối vòng hàng hoặc đảo ô chéo.
- Mọi bố cục cho phép cấu hình phải có lời giải và không bắt đầu đã hoàn tất.
- Index ngoài 1–9 không thay đổi bảng; reset không được đóng lại cổng đã mở.
- Chỉ local player gần bàn mới nhận input; server tự xác minh khoảng cách.
- Prefab không tự sửa registry và không thiếu tham chiếu/glyph; đèn dùng MaterialPropertyBlock thay vì tạo material mỗi lần bấm.

## Task 1: Luật và test
Files: `Scripts/Puzzles/LinkedLightsBoard.cs`, `Tests/Editor/LinkedLightsBoardTests.cs`, `Tests/RunLinkedLightsTests.ps1`.
Interfaces: `LinkedLightsBoard(int[] scrambleButtons)`, `Mask`, `IsSolved`, `IsLit(int button)`, `TryPress(int button)`, `TryReset()`.
- [x] Viết và chạy test thất bại trước khi triển khai.
- [x] Triển khai mask 9 bit, láng giềng ngang/dọc, seed hợp lệ, reset và khóa trạng thái giải xong.
- [x] Chạy test lại, xác nhận pass.

## Task 2: Runtime và prefab
Files: `Scripts/Puzzles/LinkedLightsPuzzle.cs`, `Scripts/Puzzles/LinkedLightsConsole.cs`, `Editor/LinkedLightsPuzzleBuilder.cs`, `Tests/Editor/LinkedLightsPrefabValidation.cs`.
- [x] Thêm điều phối offline/NGO, kiểm tra cấu hình, server quyền ghi và khoảng cách; console local input + cập nhật đèn.
- [x] Viết validator prefab, chạy thấy thiếu prefab trước khi tạo.
- [x] Builder tạo board 3×3, hướng dẫn, vùng tương tác và cổng; chỉ asset riêng.
- [x] Unity compile + test luật/prefab; review độc lập và sửa lỗi có căn cứ.

## Task 3: Bàn giao
Files: `Prefabs/Puzzles/LinkedLightsGatePuzzle.prefab`, `Prefabs/Puzzles/LinkedLightsArt/LinkedLampOn.mat`, `LinkedLampOff.mat`, `Docs/LinkedLightsPuzzle-README.md` và meta tương ứng.
- [x] Hướng dẫn cách đặt, đổi seed và kiểm tra offline/online.
- [x] Chạy test mới và test ký hiệu cũ; kiểm tra hash các file đã tồn tại và diff ngoài `_Hoang`.
- [x] Báo chính xác kết quả, phần chưa kiểm chứng.

## Progress
- Thực hiện trên nhánh Hoang hiện tại, chỉ thêm file riêng để không ảnh hưởng phần việc đã có; không commit hoặc tạo worktree.
- Review độc lập: không phát hiện lỗi chặn. Đã siết kiểm tra khoảng cách server để loại cả NaN. Validator kiểm tra seed thật hợp lệ rồi dùng seed mẫu trên bản preview không lưu, tránh báo lỗi sai khi người dùng đã đổi prefab.
- Unity batchmode final exit 0; luật đèn và integration prefab preview đều PASS. Test luật ký hiệu cũ PASS. Hash của 88 file có sẵn trong `_Hoang` và ba file dirty ngoài phạm vi trùng baseline. Không Play Mode, không host/client, không build player.
