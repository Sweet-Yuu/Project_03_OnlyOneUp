# Symbol Cipher Puzzle Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan in the current chat. Track progress here.

**Goal:** Tạo prefab giải mã ba ký hiệu để mở cổng, không chỉnh scene hoặc tài sản của người khác.
**Architecture:** Luật C# thuần xác định nghiệm duy nhất và kiểm tra mã. NetworkBehaviour sở hữu trạng thái mở khóa, nhận yêu cầu từ console cục bộ và kích hoạt IPuzzleInteractable. Editor builder tạo prefab và tài sản riêng.
**Tech Stack:** Unity 6000.3.15f1, C#, NGO 2.13.3, Input System, TextMeshPro đã có.
**Spec:** `Assets/_Hoang/Docs/SymbolCipherPuzzle-Design.md`

## Global Constraints
- Chỉ tạo/sửa trong `Assets/_Hoang`; không sửa các file đang dirty của người dùng.
- Không thêm package; không đổi player, registry, scene hay settings.
- Một class một file; private field `_camelCase`; serialized field private.
- Người dùng tự kiểm tra scene; không tự vào Play Mode.

## Review Focus
- Tổng lẻ/âm/quá lớn hoặc nghiệm ngoài 0–9 phải bị từ chối.
- Mã có số 0 đầu, mã thiếu/thừa chữ số hoặc ký tự khác phải xử lý chính xác.
- Sai mã không được mở cổng; thử lại sau khi đã giải không đóng cổng.
- Remote player không được điều khiển console của máy khác; server từ chối yêu cầu ở xa.
- Prefab cần đủ tham chiếu và chữ có glyph; builder không ghi đè asset cũ hoặc lưu scene.

## Task 1: Luật giải mã
Files: `Scripts/Puzzles/SymbolCipherLock.cs`, `Tests/Editor/SymbolCipherLockTests.cs`, `Tests/RunSymbolCipherTests.ps1`.
Interfaces: `new SymbolCipherLock(int circleTriangle, int triangleSquare, int circleSquare)`; `Code`, `IsUnlocked`, `TryUnlock(string entry)`.
- [x] Viết test độc lập, chạy thấy thất bại trước khi triển khai.
- [x] Tính nghiệm từng ký hiệu, kiểm tra miền và parity; đối chiếu mã đúng ba chữ số.
- [x] Chạy lại toàn bộ test luật, xác nhận pass.

## Task 2: Điều phối và nhập mã
Files: `Scripts/Puzzles/SymbolCipherPuzzle.cs`, `Scripts/Puzzles/SymbolCipherConsole.cs`.
- [x] Tạo coordinator dùng IPuzzleInteractable, báo config lỗi, không cho chế độ online chưa spawn chạy nhầm offline.
- [x] Tạo console trigger cho local player; E/0–9/numpad/Backspace/Enter; thoát khi rời vùng hoặc mất focus.
- [x] Server kiểm tra client/player/khoảng cách và mã; đồng bộ trạng thái; gửi kết quả về người nhập.

## Task 3: Prefab và bàn giao
Files: `Editor/SymbolCipherPuzzleBuilder.cs`, `Tests/Editor/SymbolCipherPrefabValidation.cs`, `Prefabs/Puzzles/SymbolCipherGatePuzzle.prefab`, font/material riêng trong `Assets/_Hoang`, `Docs/SymbolCipherPuzzle-README.md`.
- [x] Builder tạo bảng gợi ý, terminal và cổng trên hierarchy riêng, không mở/lưu scene.
- [x] Compile và kiểm tra prefab references/glyph/default clue qua Unity batchmode; không Play Mode.
- [x] Viết hướng dẫn đặt prefab, đổi câu đố và kiểm tra offline/online.
- [x] Kiểm tra cuối diff và hash các file dirty ngoài `_Hoang`.

## Progress / rulings
- Người dùng chọn ký hiệu, yêu cầu tự kiểm tra trong scene và tránh conflict. Thực hiện trực tiếp trên nhánh Hoang, chỉ thêm file riêng; không tạo worktree/commit hoặc thay đổi phần việc đã có.
- Ruling: dùng wrapper root thường, NetworkObject ở con Puzzle — package NGO tự thêm network prefab root vào registry chung khi import. Đã hoàn nguyên riêng entry tự thêm của puzzle mới và đối chiếu SHA256 với baseline; giữ nguyên các thay đổi có sẵn của người dùng.
- Review: chỉ server được gọi RPC trả kết quả; đã xác minh mặc định NGO cho mọi client gọi, thêm regression check thất bại trước khi giới hạn InvokePermission.Server.
- Validation: Unity 6000.3.15f1 batchmode exit 0; test luật và prefab đều PASS. Kiểm tra font, kích thước chữ, trigger, missing scripts, policy RPC và wrapper. SHA256 ba file dirty ngoài `_Hoang` trùng baseline. Không Play Mode, không build player, không chạy host/client; người dùng tự thử scene theo yêu cầu.
