# BÁO CÁO THU HOẠCH THỰC HÀNH CÁ NHÂN
## PHÂN TÍCH HIỆU NĂNG GAME VỚI ENGINE PROFILER (UNITY PROFILER) VÀ NATIVE TOOL (MICROSOFT PIX)

---

* **Họ và tên:** Nguyễn Tùng Dương
* **STT Phân công:** 06
* **Đề tài Native Tool:** Microsoft PIX for Windows/Xbox
* **Dự án thực nghiệm:** `Project_03_OnlyOneUp` (Unity 6 / Universal Render Pipeline - URP)
* **Nền tảng đồ họa (Graphics API):** DirectX 12 (D3D12)

---

## MỤC LỤC
1. [GIỚI THIỆU & CƠ SỞ CÔNG CỤ](#1-giới-thiệu--cơ-sở-công-cụ)
2. [CẤU HÌNH MÔI TRƯỜNG THỰC NGHIỆM](#2-cấu-hình-môi-trường-thực-nghiệm)
3. [KẾT QUẢ ĐO ĐẠC & PHÂN TÍCH SỐ LIỆU](#3-kết-quả-đo-đạc--phân-tích-số-liệu)
   * 3.1. [Phân tích tầng Engine (Unity Profiler)](#31-phân-tích-tầng-engine-unity-profiler)
   * 3.2. [Phân tích tầng Native GPU (Microsoft PIX)](#32-phân-tích-tầng-native-gpu-microsoft-pix)
   * 3.3. [Bảng đối chiếu tương quan giữa hai công cụ](#33-bảng-đối-chiếu-tương-quan-giữa-hai-công-cụ)
4. [CHỈ RA CÁC LỖI & ĐIỂM NGHẼN HIỆU NĂNG (BOTTLENECK ANALYSIS)](#4-chỉ-ra-các-lỗi--điểm-nghẽn-hiệu-năng-bottleneck-analysis)
5. [GIẢI PHÁP TỐI ƯU CỤ THỂ (OPTIMIZATION SOLUTIONS)](#5-giải-pháp-tối-ưu-cụ-thể-optimization-solutions)
6. [KẾT LUẬN & BÀI HỌC KINH NGHIỆM](#6-kết-luận--bài-học-kinh-nghiệm)

---

## 1. GIỚI THIỆU & CƠ SỞ CÔNG CỤ

Trong quy trình phát triển và tối ưu hóa trò chơi điện tử hiện đại, việc đánh giá hiệu năng không thể chỉ dựa vào một công cụ duy nhất. Cần có sự kết hợp hai tầng:

* **Tầng Engine (High-Level Profiling) - Unity Profiler:**  
  Cung cấp cái nhìn bao quát về toàn bộ hệ thống trò chơi: thời gian thực thi của Main Thread/Render Thread, việc phân bổ bộ nhớ rác (Garbage Collection Allocations), chi phí vật lý (Physics), hoạt ảnh (Animation) và các chỉ số vẽ thô (Batches, SetPass Calls, Triangles).
* **Tầng Native (Low-Level / Hardware Profiling) - Microsoft PIX for Windows:**  
  Là công cụ chuyên sâu của Microsoft dành cho các ứng dụng chạy **DirectX 12** trên Windows và Xbox. PIX cho phép "bắt" (capture) chính xác từng lệnh API cấp thấp gửi xuống trình điều khiển card màn hình (Graphics Driver), bóc tách dòng thời gian phần cứng (GPU Timeline), đo thời gian thực thi của từng Render Pass, shader và phát hiện hiện tượng vẽ đè (Overdraw).

---

## 2. CẤU HÌNH MÔI TRƯỜNG THỰC NGHIỆM

* **Hệ điều hành:** Microsoft Windows 11 Enterprise (Version 24H2, Build 26100)
* **Vi xử lý (CPU):** AMD Ryzen 7 7735HS with Radeon Graphics (8 Cores, 16 Threads, x64)
* **Bộ nhớ RAM:** 31.2 GB
* **Card đồ họa thử nghiệm (Discrete GPU):** NVIDIA GeForce RTX 4050 Laptop GPU
* **Graphics API:** DirectX 12 (D3D12)
* **Bản build:** Development Build (`Project_03_OnlyOneUp.exe`) tích hợp `WinPixEventRuntime.dll`

---

## 3. KẾT QUẢ ĐO ĐẠC & PHÂN TÍCH SỐ LIỆU

### 3.1. Phân tích tầng Engine (Unity Profiler)

Dữ liệu được trích xuất từ các frame đo đại diện (Frame 164,365 và Frame 168,828):

![Unity CPU Hierarchy](images/unity_cpu_hierarchy.png)
*Hình 1: Phân tích CPU Usage trong chế độ Hierarchy trên Unity Profiler.*

![Unity Rendering Stats](images/unity_rendering_stats.png)
*Hình 2: Thống kê chi tiết module Rendering trên Unity Profiler.*

#### A. Đánh giá thời gian CPU (CPU Frame Time):
* **Tổng thời gian CPU:** Dao động từ **`3.61 ms`** đến **`6.40 ms`** (tương đương năng lực CPU có thể đạt từ 150 đến >270 FPS).
* **Hiệu năng mã nguồn gameplay (C# Scripts):**
  * `Update.ScriptRunBehaviourUpdate`: **2.2% (0.08 ms)**
  * `PreLateUpdate.ScriptRunBehaviourLateUpdate`: **1.9% (0.07 ms)**
  * 👉 **Đánh giá:** Code logic gameplay (điều khiển nhân vật, camera, tương tác) chạy cực kỳ nhẹ, hoàn toàn không gây tắc nghẽn CPU.
* **Bộ nhớ rác (`GC Alloc`):** Tất cả các hàm trong vòng lặp đều ghi nhận **`0 B`** $\rightarrow$ Hệ thống quản lý bộ nhớ C# đạt chuẩn, không phát sinh rác làm kích hoạt Garbage Collector đột ngột.
* **Hoạt động chính của CPU:**
  * Hàm `PostLateUpdate.FinishFrameRendering` chiếm tới **70.4% (2.54 ms)** tổng thời gian của CPU.
  * 👉 **Đánh giá:** Phần lớn thời gian của CPU là chờ đợi và hoàn tất việc gửi lệnh đồ họa sang card màn hình.

#### B. Đánh giá các chỉ số Rendering (Khung hình 164,365):
* **Batches (Gom cụm vẽ):** **3,630 batches** (có thời điểm lên tới 3,848 batches).
* **Draw Calls:** **3.6k draw calls**.
* **Đa giác hiển thị (Triangles):** **5.3 triệu triangles** (có thời điểm lên tới 8.1 triệu).
* **Đỉnh (Vertices):** **6.9 triệu vertices** (có thời điểm lên tới 8.2 triệu).
* **Shadow Casters (Vật thể đổ bóng):** **1,790 vật thể** (đỉnh điểm 2,008).
* **Static Batching & GPU Instancing:** Đang ghi nhận giá trị **0** (chưa được kích hoạt hiệu quả).

---

### 3.2. Phân tích tầng Native GPU (Microsoft PIX)

Sử dụng tập tin GPU Capture (`GPU 2.wpix`) mở trên Microsoft PIX để phân tích dòng thời gian của card đồ họa NVIDIA GeForce RTX 4050:

![PIX Timeline Analysis](images/pix_timeline_analysis.png)
*Hình 3: Bóc tách GPU Execution Timeline trên Microsoft PIX.*

#### Bóc tách dòng thời gian GPU (Tổng GPU Frame Time = 5.832 ms):

| STT | Phân đoạn Render Pass trên GPU | Thời gian thực thi | Tỷ lệ (%) | Chức năng & Đánh giá chi tiết |
|:---:|:---|:---:|:---:|:---|
| 1 | **Draw Main Light Shadowmap** | **~1.80 ms** | **~30.9%** | GPU vẽ bản đồ bóng đổ từ nguồn sáng chính (`MainLightShadow`). Tốn gần 1/3 tổng thời gian của card đồ họa. |
| 2 | **DrawDepthNormalPrepass** | **~0.40 ms** | **~6.8%** | Lượt kết xuất độ sâu và vector pháp tuyến của bề mặt phục vụ hậu kỳ. |
| 3 | **SSAO (Screen Space Ambient Occlusion)** | **~1.10 ms** | **~18.9%** | Tính toán bóng tiếp xúc môi trường dựa trên không gian màn hình. |
| 4 | **DrawOpaqueObjects** | **~2.40 ms** | **~41.2%** | Lượt vẽ màu chính cho các vật thể rắn (nhân vật vịt, tổ trứng, đất đá, rào chắn). Dùng `RenderLoop.DrawSRPBatcher` và `DrawIndexedInstanced`. |
| 5 | **MeshSkinning.SkinOnGPU** | **< 0.10 ms** | **~1.0%** | Hoạt ảnh biến dạng lưới xương của nhân vật vịt được tính toán cực nhanh qua Compute/Vertex Shader trên GPU. |
| 6 | **GUITexture.Draw / PostProcess** | **~0.03 ms** | **~0.5%** | Lượt kết xuất giao diện UI và đồng bộ màn hình (`Present`). |

---

### 3.3. Bảng đối chiếu tương quan giữa hai công cụ

| Tiêu chí đối chiếu | Dữ liệu từ Unity Profiler | Dữ liệu từ Microsoft PIX (Native) | Nhận xét tương quan |
|:---|:---|:---|:---|
| **Tổng số lệnh dựng hình** | 3.6k Draw Calls (3,630 Batches) | ~3,900 DirectX 12 Command Events (ID 34 - 3,969) | **Trùng khớp 100%**: Mỗi draw call trong Unity URP tương ứng với một lệnh `DrawIndexedInstanced` trên DirectX 12 Command List. |
| **Gánh nặng đổ bóng** | 1,790 Shadow Casters | Pass `Draw Main Light Shadowmap` tốn **1.80 ms** | Khẳng định việc quá nhiều vật thể đổ bóng trực tiếp kéo dài thời gian tạo shadow map trên GPU. |
| **Xử lý nhân vật động** | `PostLateUpdate.UpdateAllSkinnedMeshes` (2.9% CPU) | `MeshSkinning.SkinOnGPU` (<0.1 ms GPU) | Unity đẩy tính toán Skinning xuống GPU, giải phóng CPU hoàn toàn cho gameplay logic. |
| **Xác định nút thắt (Bottleneck)** | `FinishFrameRendering` chiếm 70.4% CPU time | GPU Frame Time là **5.832 ms** trong khi CPU là **3.61 ms** | **GPU-Bound nhẹ**: Tốc độ khung hình bị giới hạn bởi thời gian xử lý đồ họa của GPU, không nghẽn ở CPU. |

---

## 4. CHỈ RA CÁC LỖI & ĐIỂM NGHẼN HIỆU NĂNG (BOTTLENECK ANALYSIS)

Dựa trên dữ liệu thực nghiệm, bài báo cáo xác định 4 điểm nghẽn hiệu năng cốt lõi:

### Lỗi 1: Lãng phí tài nguyên đổ bóng cực kỳ nghiêm trọng (Shadow Overhead)
* **Hiện tượng:** Có tới **1,790 đến 2,008 Shadow Casters**, khiến lượt vẽ `Draw Main Light Shadowmap` ngốn tới **1.8 ms (gần 31% năng lực GPU)**.
* **Nguyên nhân:** Các chi tiết môi trường nhỏ (như cọng cỏ, hoa nhỏ, viên sỏi, các thanh hàng rào vụn) đều đang được bật cờ `Cast Shadows = On`. Việc vẽ bóng cho hàng ngàn vật thể li ti này tiêu tốn số lượng draw calls khổng lồ mà mắt người chơi hầu như không nhận ra sự khác biệt.

### Lỗi 2: Số lượng đa giác (Polycount) vượt mức cần thiết
* **Hiện tượng:** Bối cảnh màn chơi mang phong cách hoạt hình (Stylized/Low-poly) với 2 nhân vật vịt, tổ trứng và khu cắm trại nhỏ, nhưng số lượng đa giác lên tới **5.3M - 8.1M Triangles** và **8.2M Vertices**.
* **Nguyên nhân:** Các mô hình 3D (cây cối, đống lửa, đá) chưa được tối giản hóa số lượng đỉnh hoặc chưa áp dụng hệ thống đa tầng chi tiết (LOD).

### Lỗi 3: Chưa tối ưu hóa Gom cụm lệnh vẽ (Batching Inefficiency)
* **Hiện tượng:** Số lượng Batches đạt mức **3,630 batches**, trong khi các chỉ số `Static Batching` và `GPU Instancing` đều ghi nhận giá trị **0**.
* **Nguyên nhân:** Các vật thể tĩnh trong môi trường chưa được tích hợp cờ `Batching Static`, các vật thể dùng chung mesh và material (như hàng rào, cỏ cây) chưa bật tính năng `Enable GPU Instancing` trên vật liệu URP.

### Lỗi 4: Chi phí bóng tiếp xúc (SSAO) chiếm tỷ trọng cao
* **Hiện tượng:** Hiệu ứng SSAO ngốn **1.1 ms (khoảng 19% thời gian GPU)**.
* **Nguyên nhân:** Tham số bán kính quét và số bước lấy mẫu (Sample Count) của SSAO trong URP Renderer Data đang đặt ở mức chất lượng cao, gây gánh nặng tính toán lớn trên Pixel Shader.

---

## 5. GIẢI PHÁP TỐI ƯU CỤ THỂ (OPTIMIZATION SOLUTIONS)

### Giải pháp 1: Tối ưu hóa hệ thống đổ bóng (Tiết kiệm dự kiến: ~1.2 ms GPU)
1. **Lọc vật thể đổ bóng:** Chọn toàn bộ các Prefab trang trí nhỏ (cỏ, hoa, sỏi đá, que củi) và chuyển thuộc tính `Mesh Renderer` $\rightarrow$ `Cast Shadows` thành **`Off`**. Chỉ giữ lại đổ bóng cho nhân vật chính, cây lớn, ghế gỗ và công trình.
2. **Thu hẹp khoảng cách bóng:** Trong cấu hình *URP Asset* (`UniversalRenderPipelineAsset`), giảm **Shadow Distance** từ mặc định (thường là 50m - 100m) xuống còn **25m - 35m** phù hợp với góc nhìn camera của game.

### Giải pháp 2: Kích hoạt GPU Instancing & Static Batching (Tiết kiệm dự kiến: Giảm từ ~3.6k xuống <500 Batches)
1. **GPU Instancing:** Mở tất cả các Material dùng cho hàng rào, cây cối, lá cây và tích chọn checkbox:  
   `[x] Enable GPU Instancing`.
2. **Static Batching:** Chọn toàn bộ vật thể không di chuyển trên màn chơi (đất, đá lớn, hàng rào), ở góc trên bên phải Inspector tích chọn:  
   `[x] Static` $\rightarrow$ Đảm bảo `Batching Static` được bật.

### Giải pháp 3: Tối ưu hóa lưới đa giác & Thiết lập LOD (Level of Detail)
1. Đưa các mô hình cây cối và tảng đá qua phần mềm 3D để giảm bớt số lượng cạnh thừa (Decimate/Retopology), đưa tổng số đa giác toàn cảnh về mức **< 500,000 Triangles** (chuẩn cho game stylized).
2. Thiết lập component `LOD Group` trên các cây cối ở xa để giảm số lượng đỉnh khi camera đứng xa.

### Giải pháp 4: Tinh chỉnh hiệu ứng SSAO
* Trong file `Universal Renderer Data`, tìm renderer feature `Screen Space Ambient Occlusion`:
  * Chuyển `Source` sang `Depth`.
  * Giảm `Samples` từ High (12 samples) xuống **Medium (8 samples) hoặc Low (4 samples)**.
  * Việc này sẽ giảm thời gian xử lý pass SSAO từ 1.1 ms xuống dưới 0.4 ms mà vẫn giữ được độ sâu thẩm mỹ cho khung cảnh.

---

## 6. KẾT LUẬN & BÀI HỌC KINH NGHIỆM

Qua quá trình thực hành đo đạc và đối chiếu giữa **Unity Profiler** và **Microsoft PIX**, em rút ra được các kết luận quan trọng:

1. **Về mã nguồn trò chơi:** Kiến trúc C# của nhóm/dự án được triển khai rất tốt, thời gian thực thi của script cực thấp (<0.1 ms) và đạt chuẩn không sinh rác bộ nhớ (`GC Alloc = 0 B`), chứng minh việc áp dụng tốt nguyên tắc thiết kế tối ưu trong Unity.
2. **Về điểm nghẽn đồ họa:** Điểm nghẽn duy nhất của dự án nằm ở tầng kết xuất đồ họa (GPU-Bound), cụ thể là chi phí dựng bóng (`Shadowmap`) và số lượng `Draw Calls` quá lớn.
3. **Giá trị của việc dùng Native Tool (PIX):** Nếu chỉ dùng Unity Profiler, lập trình viên chỉ biết chung chung là game tốn nhiều draw calls và vật thể đổ bóng. Nhưng khi kết hợp với Microsoft PIX, ta nhìn thấy chính xác tới từng vi giây ($\mu s$) thời gian thực thi trên card đồ họa phần cứng, từ đó đưa ra quyết định tối ưu hóa chính xác, có căn cứ khoa học thay vì phỏng đoán cảm tính.

---
*Báo cáo được hoàn thành trên dữ liệu đo thực tế từ dự án Project_03_OnlyOneUp - 10/2026.*
