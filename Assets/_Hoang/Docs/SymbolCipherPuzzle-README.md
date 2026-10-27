# Mini puzzle giải mã ký hiệu

## Đặt vào scene của bạn
1. Kéo `Assets/_Hoang/Prefabs/Puzzles/SymbolCipherGatePuzzle.prefab` vào scene bạn tự quản lý. Không cần sửa player hoặc các prefab cũ.
2. Đặt root tại mặt đất, giữ scale `(1,1,1)`. Mặt bảng hướng về local `-Z`; lối đi chạy dọc `+Z`, cổng nằm phía sau bàn nhập.
3. Đảm bảo scene của bạn có sàn với collider và player có CharacterController, hoặc collider gắn tag `Player`. Prefab không mang sàn, camera hay player riêng.
4. Play scene của bạn, đến gần bàn nhập ở bên phải bảng gợi ý, nhấn **E**, nhập **0–9** (hàng số hoặc numpad), **Enter** xác nhận. **Backspace** xóa một số; **E** thoát. Rời vùng tương tác hoặc chuyển khỏi cửa sổ game cũng kết thúc nhập.
5. Mã sai giữ cổng đóng và cho thử lại. Mã đúng mở cổng đến hết lần chơi; dừng rồi Play lại để khóa lại.

Các bảng là chữ 3D trong thế giới. Không cần EventSystem, click chuột, đổi khóa cursor, chỉnh Input Actions hoặc khóa chuyển động của player.

## Đổi câu đố
Trên component **SymbolCipherPuzzle** của object con **Puzzle**:
- `Circle Triangle Sum`: tròn + tam giác.
- `Triangle Square Sum`: tam giác + vuông.
- `Circle Square Sum`: tròn + vuông.

Chọn trước ba chữ số 0–9, rồi đặt ba tổng tương ứng. Bảng chữ được cập nhật từ cùng dữ liệu khi bắt đầu chơi. Nếu tổng không có nghiệm nguyên hoặc nghiệm vượt 0–9, component báo lỗi rõ ràng và không cho mở cổng.

Mặc định: `7, 5, 8` → tròn `5`, tam giác `2`, vuông `3` → mã **523**. Đây là đáp án dành cho người thiết kế; bảng trong game không hiện đáp án. Mã có số 0 đầu vẫn cần nhập đủ ba chữ số.

Muốn dùng cổng của bạn: gán component thực thi `IPuzzleInteractable` vào `Gate Behaviour`. Cổng đi kèm dùng `PuzzleGate`; `Open Offset` quyết định hướng/khoảng mở. Chỉ chỉnh trên scene/prefab của bạn.

## Online
- Đặt **cùng prefab** vào scene gameplay của cả hai máy và dùng luồng load scene Netcode hiện có. Giữ NetworkObject trên object con **Puzzle**.
- Lưu scene của bạn trước khi thử online/build để Unity cấp ID riêng cho NetworkObject đặt trong scene.
- Console chỉ nhận bàn phím khi **local player** ở trong vùng trigger. Server kiểm tra player gửi yêu cầu có đang ở gần `Console Point` và tự kiểm tra mã.
- Trạng thái giải xong được đồng bộ cho cả hai máy và người tham gia muộn. Tiến độ gõ thuộc mỗi máy; không đồng bộ từng phím.
- Đây là prefab **đặt sẵn trong scene**. Không tự thêm vào `DefaultNetworkPrefabs.asset`. Nếu sau này cần Instantiate/Spawn động, phải đăng ký bằng quy trình network của dự án; phần đó không nằm trong thay đổi này.
- Root ngoài cùng là wrapper thường; NGO của dự án tự thêm prefab có NetworkObject ở root vào registry chung khi import. Đặt NetworkObject ở con giúp prefab này không tự gây thay đổi registry của người khác.
- Nếu đã có phiên mạng nhưng puzzle chưa được spawn, bàn nhập không chạy nhầm thành puzzle offline.

## Tạo lại và kiểm chứng
- Menu **Tools > Hoang > Create Symbol Cipher Puzzle Prefab** tạo prefab nếu chưa có; nếu đã có chỉ chọn asset, không ghi đè các chỉnh sửa của bạn.
- Test luật độc lập: `& 'Assets/_Hoang/Tests/RunSymbolCipherTests.ps1'` trong PowerShell ở root dự án. Có thể truyền `-UnityEditorPath` nếu Unity cài ở nơi khác.
- Trong Unity, menu **Tools > Hoang > Validate Symbol Cipher Puzzle Prefab** kiểm tra luật, tham chiếu prefab, trigger, glyph và missing script. Không mở/lưu scene, không vào Play Mode.
- Kết quả compile/test và những phần chưa kiểm tra được ghi trong phần cuối file này khi bàn giao.

## Checklist để bạn tự thử
- Đọc rõ cả ba phương trình từ camera thực tế; các ký hiệu hiển thị đầy đủ.
- Ngoài vùng trigger không nhập được; đứng gần nhấn E nhập được bằng cả hàng số/numpad.
- Thử sai, thiếu chữ số, Backspace, thoát bằng E, rời vùng rồi quay lại.
- Nhập 523: cổng nâng đủ cao, collider di chuyển theo cổng, hai người đi qua được.
- Kiểm tra hướng bố trí và địa hình không cho đi vòng qua cổng. Prefab chỉ là bộ phận giải đố, không tự xây tường bao quanh map.
- Host/client: mỗi máy điều khiển bàn nhập của chính mình, sai không mở, đúng mở trên cả hai, thử tải lại scene.

## Kết quả bàn giao
- Unity **6000.3.15f1** đã compile các script mới và chạy builder/validator thành công (batchmode exit **0**).
- Còn cảnh báo có sẵn **CS0114** tại `DualPressurePuzzleCoordinator.OnDestroy` (cũng xuất hiện trong lượt compile đầu trước khi tạo prefab mới); không sửa code puzzle cũ trong tác vụ này.
- Test luật: **PASS** cho nghiệm, mã sai/đúng, thử lại, giữ trạng thái mở, số 0 đầu, miền chữ số và gợi ý vô nghiệm.
- Test prefab: **PASS** cho wrapper không kích hoạt auto-registry, tham chiếu cổng/console/bảng gợi ý, trigger/Rigidbody, không missing script, glyph tiếng Việt/ký hiệu và chiều cao chữ vừa bảng. RPC kết quả chỉ server được gọi.
- Log nằm trong `Assets/_Hoang/Tests/.artifacts~/final-validation.log` (thư mục kết quả cục bộ được Git/Unity bỏ qua).
- Các scene không đổi. SHA256 của ba file đã dirty sẵn ngoài `_Hoang` trùng trước khi bắt đầu; thay đổi mới chỉ trong `_Hoang`.
- **Chưa thử** Play Mode, camera/va chạm thực tế, host/client/late join, build player hoặc toàn bộ suite cũ của dự án. Bạn tự kiểm tra trong scene theo checklist phía trên.
