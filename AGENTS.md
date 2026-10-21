# Quy tắc cho dự án Unity C#

## 1. Tư duy phát triển: Tối giản & Hiệu quả (Senior Dev Mindset)
- **Code tốt nhất là code không cần viết**: Giải quyết triệt để vấn đề với ít code mới nhất. Không over-engineering, không tạo abstraction/wrapper/boilerplate khi chưa có yêu cầu.
- **Xác định đúng phạm vi (Scope)**: Trước khi viết hoặc sửa, kiểm tra toàn bộ nơi bị ảnh hưởng (callers, dữ liệu, config). Không tự ý thêm tính năng, option hoặc tính linh hoạt mà không ai yêu cầu.
- **Tái sử dụng tối đa**:
  1. Ưu tiên dùng những gì codebase đã có (helper, component, pattern có sẵn).
  2. Dùng thư viện chuẩn của C# hoặc tính năng có sẵn của Unity.
  3. Không thêm thư viện phụ thuộc mới chỉ vì vài dòng code.
- **Sửa lỗi tận gốc**: Trước khi sửa bug, grep/tìm kiếm mọi nơi gọi tới hàm bị ảnh hưởng, sửa triệt để tại phần code dùng chung.
- **Không bao giờ cắt giảm**: Khâu kiểm tra tính hợp lệ dữ liệu (validation), xử lý lỗi tránh mất mát dữ liệu, và các yêu cầu người dùng chỉ định rõ.
- **Ghi chú shortcut**: Nếu phải dùng giải pháp tạm thời/rút gọn (shortcut), đánh dấu comment: `// shortcut: <giới hạn>, <khi nào cần nâng cấp>`.

## 2. Nguyên tắc SOLID (bắt buộc tuân thủ)
- **S - Single Responsibility**: Mỗi class chỉ làm một việc. Ví dụ: PlayerMovement chỉ lo di chuyển, không xử lý máu hay UI.
- **O - Open/Closed**: Mở rộng bằng interface hoặc kế thừa, không sửa code cũ đang chạy tốt.
- **L - Liskov Substitution**: Class con phải thay thế được class cha mà không làm hỏng hành vi.
- **I - Interface Segregation**: Interface nhỏ, tập trung (ví dụ IDamageable, IMovable), không tạo interface quá lớn.
- **D - Dependency Inversion**: Phụ thuộc vào interface/abstraction, không phụ thuộc trực tiếp vào class cụ thể.

## 3. Quy ước code Unity
- Đặt tên class, method theo PascalCase; biến private theo _camelCase.
- Dùng [SerializeField] private thay vì public field.
- Không dùng Find / GetComponent trong Update, hãy cache trong Awake hoặc Start.
- Tách logic ra khỏi MonoBehaviour khi có thể.
- Không dùng singleton bừa bãi, ưu tiên inject dependency.

## 4. Cách làm việc & Báo cáo
- Trước khi viết code, giải thích ngắn gọn cách thiết kế và phạm vi ảnh hưởng.
- Mỗi class một file, tên file trùng tên class.
- Viết chú thích tiếng Việt cho phần logic phức tạp; chỉ giải thích lý do ("why") mà code không thể hiện được.
- Không tự ý sửa file ngoài phạm vi yêu cầu.
- **Báo cáo cuối phản hồi**: Kết thúc mỗi phản hồi bằng 1-2 dòng nêu rõ những gì đã bỏ qua/chưa kiểm tra và rủi ro tiềm ẩn (nếu có) mà người dùng cần lưu ý.