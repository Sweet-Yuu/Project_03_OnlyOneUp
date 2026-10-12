# Quy tắc cho dự án Unity C#

## Nguyên tắc SOLID (bắt buộc tuân thủ)
- **S - Single Responsibility**: Mỗi class chỉ làm một việc. Ví dụ: PlayerMovement chỉ lo di chuyển, không xử lý máu hay UI.
- **O - Open/Closed**: Mở rộng bằng interface hoặc kế thừa, không sửa code cũ đang chạy tốt.
- **L - Liskov Substitution**: Class con phải thay thế được class cha mà không làm hỏng hành vi.
- **I - Interface Segregation**: Interface nhỏ, tập trung (ví dụ IDamageable, IMovable), không tạo interface quá lớn.
- **D - Dependency Inversion**: Phụ thuộc vào interface/abstraction, không phụ thuộc trực tiếp vào class cụ thể.

## Quy ước code Unity
- Đặt tên class, method theo PascalCase; biến private theo _camelCase.
- Dùng [SerializeField] private thay vì public field.
- Không dùng Find / GetComponent trong Update, hãy cache trong Awake hoặc Start.
- Tách logic ra khỏi MonoBehaviour khi có thể.
- Không dùng singleton bừa bãi, ưu tiên inject dependency.

## Cách làm việc
- Trước khi viết code, giải thích ngắn gọn cách thiết kế.
- Mỗi class một file, tên file trùng tên class.
- Viết chú thích tiếng Việt cho phần logic phức tạp.
- Không tự ý sửa file ngoài phạm vi yêu cầu.