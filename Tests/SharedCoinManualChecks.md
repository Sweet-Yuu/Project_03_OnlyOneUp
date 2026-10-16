# Kiểm thử coin dùng chung

Mở `MainMap_Online` trên hai bản game cùng phiên bản, một Host và một Client.
Ghi lại số coin ban đầu ở từng máy (hai số có thể khác nhau do PlayerPrefs).

1. Host chạm một coin: cả hai máy tăng đúng 1 và coin biến mất trên cả hai.
2. Client chạm coin khác: kết quả tương tự, kiểm tra đặc biệt trường hợp controller của Client bị tắt trên Host.
3. Cả hai cùng chạm một coin: mỗi máy chỉ tăng 1, không tăng 2.
4. Tiếp tục đứng ở vị trí coin vừa nhặt: không được cộng thêm.
5. Client vào sau khi Host đã nhặt coin: coin đã nhặt không xuất hiện lại; Client không nhận thưởng hồi tố.
6. Thoát và mở lại game: số dư từng máy được lưu theo SaveSystem hiện có.
7. Chạy SceneQuang offline: nhân vật có tag Player vẫn nhặt coin như trước.

Đã kiểm tra biên dịch C# bằng Roslyn và tham chiếu Unity/Netcode của dự án.
Các bước Play Mode hai máy ở trên cần được chạy trong Unity hoặc bản build;
biên dịch riêng chưa kiểm chứng RPC sau Netcode IL post-processing hay đồng bộ thực tế.
