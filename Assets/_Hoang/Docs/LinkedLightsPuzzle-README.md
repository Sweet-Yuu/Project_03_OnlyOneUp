# Đèn liên kết 3×3

## Cách đặt và chơi
1. Kéo `Assets/_Hoang/Prefabs/Puzzles/LinkedLightsGatePuzzle.prefab` vào **scene của bạn**, đặt root tại mặt đất, scale `(1,1,1)`.
2. Mặt bảng hướng về local `-Z`; lối đi chạy dọc `+Z`, cổng ở phía sau, bảng nằm bên trái để không chắn đường sau khi giải.
3. Scene cần sàn/collider và player có CharacterController, hoặc collider tag `Player`. Prefab không tạo camera, player hoặc sàn.
4. Đến gần bảng, nhấn **E** để bắt đầu/thoát. Bấm **1–9** ở hàng số hoặc numpad để chọn ô, **R** trả về bố cục ban đầu. Rời vùng tương tác hoặc chuyển khỏi cửa sổ game sẽ thoát điều khiển.

Các ô được đánh số theo cách đọc, không theo bố cục vật lý của numpad:

```text
1  2  3
4  5  6
7  8  9
```

Mỗi lần bấm đảo chính ô đó và các ô sát cạnh ngang/dọc. Không đảo ô chéo và không nối vòng qua cạnh bảng. **Tắt cả chín đèn để mở cổng.** Sau khi giải, input/reset không đóng lại cổng. Dừng rồi Play lại để chơi lại từ đầu.

Không cần EventSystem, click chuột, đổi cursor, chỉnh Input Actions, khóa chuyển động hoặc dừng thời gian.
Đặt các vùng tương tác puzzle cách nhau để khi nhấn E người chơi chỉ đứng trong một bàn điều khiển.

## Đổi bố cục
Trên object con **Puzzle**, component **LinkedLightsPuzzle**, chỉnh `Scramble Buttons`:
- Đây là **chuỗi nút dùng tạo bố cục từ bảng tắt**, không phải danh sách đèn đang bật.
- Mỗi phần tử là số 1–9. Chuỗi mặc định: **1, 2, 4, 5, 9**.
- Bố cục luôn có lời giải vì được tạo bằng các thao tác hợp lệ từ đích. Bấm lại các nút của chuỗi theo thứ tự bất kỳ sẽ giải được.
- Chuỗi rỗng, số ngoài 1–9 hoặc chuỗi triệt tiêu thành bảng tắt sẵn sẽ báo lỗi cấu hình và không cho chơi.
- Chỉnh trước khi Play. R chỉ reset về bố cục đó, không sinh câu đố ngẫu nhiên khác.

Đáp án mẫu dành cho người thiết kế: bấm **1 → 2 → 4 → 5 → 9** từ bố cục mặc định. Bảng trong game chỉ ghi luật, không hiện đáp án.

Có thể gán một component khác thực thi `IPuzzleInteractable` vào `Gate Behaviour`. Cổng có sẵn dùng `PuzzleGate`; chỉnh `Open Offset` trên cổng nếu cần đổi hướng mở.

## Online
- Đặt cùng prefab vào scene gameplay trên cả hai máy, dùng luồng load scene NGO hiện có, và **lưu scene của bạn trước khi thử online/build** để Unity cấp ID cho NetworkObject đặt trong scene.
- Root ngoài cùng là wrapper thường; giữ NetworkObject ở object con **Puzzle** để NGO không tự thêm prefab này vào registry chung khi import.
- Mỗi máy chỉ nhận input khi local player ở gần bảng. Server kiểm tra lại người gửi và khoảng cách đến `Console Point` trước khi xử lý bấm/reset.
- Cả hai người thao tác trên cùng một bảng; thao tác của một người cập nhật cả hai máy. R cũng reset bảng chung, nên cần phối hợp.
- Đồng bộ trạng thái chín đèn; trạng thái giải xong dùng cùng dữ liệu để mở cổng và hỗ trợ người tham gia muộn. Không cho phiên mạng chưa spawn puzzle chạy nhầm offline.
- Đây là prefab đặt sẵn trong scene, không dùng Instantiate/Spawn động. Không chỉnh `DefaultNetworkPrefabs.asset` trong tác vụ này.

## Kiểm tra và menu
- **Tools > Hoang > Create Linked Lights Puzzle Prefab**: tạo prefab nếu chưa có; nếu đã có chỉ chọn asset, giữ nguyên chỉnh sửa của bạn.
- **Tools > Hoang > Validate Linked Lights Puzzle Prefab**: kiểm tra luật, wiring, chín đèn, chữ và thao tác offline trên bản prefab preview. Không Play Mode, không lưu scene.
- Test luật bằng PowerShell ở root dự án: `& 'Assets/_Hoang/Tests/RunLinkedLightsTests.ps1'`. Truyền `-UnityEditorPath` nếu Unity cài ở nơi khác.
- Prefab tái sử dụng font/vật liệu trong `Assets/_Hoang/Prefabs/Puzzles/SymbolCipherArt`; giữ các asset dùng chung này khi di chuyển/xóa puzzle ký hiệu cũ.

## Checklist tự kiểm tra trong scene
- Đọc rõ số 1–9 và hướng dẫn từ camera thật; đèn bật/tắt đủ tương phản.
- Đứng xa không thao tác được; đứng gần bấm E và thử hàng số/numpad/R/E/rời vùng.
- Bấm góc 1 chỉ đảo ô 1, 2, 4; giữa 5 đảo 2, 4, 5, 6, 8.
- Bấm sai rồi R khôi phục đúng bố cục; giải đúng mở cổng đủ cao và collider đi theo cổng.
- Địa hình/tường của bạn ngăn người chơi đi vòng qua cổng; prefab không tự xây lại map.
- Host/client: hai người thấy cùng đèn, bấm/reset cập nhật đúng trên cả hai; thử giải xong, rời/đến lại và tải lại scene.

## Kết quả bàn giao
- Unity **6000.3.15f1** đã compile và chạy builder/validator thành công, **exit 0**. Log: `Assets/_Hoang/Tests/.artifacts~/linked-lights-final.log` (kết quả cục bộ được Git/Unity bỏ qua).
- Test luật **PASS**: cạnh/góc/giữa, không nối vòng/đảo chéo, nghiệm, reset, khóa khi giải xong và cấu hình sai. Test luật puzzle ký hiệu cũ cũng **PASS**.
- Prefab preview **PASS**: đủ chín đèn đúng thứ tự, tham chiếu/trigger, kích thước chữ; thao tác lên coordinator thật đổi màu đèn, reset đúng và mở/giữ mở cổng. Preview bị hủy, không lưu scene.
- Review độc lập không còn lỗi chặn. Hash của **88 file đã có trong `_Hoang`** giữ nguyên; ba file dirty ngoài phạm vi cũng giữ nguyên. Chỉ thêm file cho puzzle đèn.
- **Chưa kiểm tra** Play Mode, input bàn phím thực tế, camera/va chạm thực tế, host/client/late join, build player hoặc toàn bộ suite cũ. Bạn tự thử scene theo checklist phía trên.
