# BÁO CÁO PHÂN TÍCH TÁC ĐỘNG VÀ ĐÁNH GIÁ KIẾN TRÚC HIỆN TẠI
*(00_CORRECTION_IMPACT_ANALYSIS.md)*

> **Dự án:** CONTRAST (Unity 2D Custom Physics & Runtime Level Editor)  
> **Giai đoạn:** Phase 0 — Initial Codebase Audit & Impact Analysis  
> **Tài liệu tham chiếu tối cao:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
> **Mục tiêu:** Xác định hiện trạng luồng thực thi, các khiếm khuyết đối chiếu với HTML gốc, danh mục tệp cần hiệu chỉnh tối thiểu và đánh giá rủi ro hồi quy trước khi thực hiện.

---

## 1. KHẢO SÁT HIỆN TRẠNG LUỒNG THỰC THI (CURRENT EXECUTION FLOW)

### 1.1. Luồng nhập liệu và điều khiển (Input & Player Control Flow)
- **Cấu trúc hiện tại:**
  - `IPlatformInputAdapter` định nghĩa giao diện phần cứng cho `StandaloneKeyboardInputAdapter` và `MobileTouchInputAdapter`.
  - `PlayerInput.cs` đọc từ adapter và sinh `PlayerInputIntent`.
  - `PlayerController.ProcessUpdate()` nhận intent, ủy quyền tính toán cho `PlayerMovementResolver` (gồm `PlayerOverlapResolver`, `PlayerCollisionResolver`, `PlayerJumpMotion`), cập nhật vị trí qua `ApplyMove()`.
- **Khiếm khuyết nghiêm trọng đối chiếu với HTML:**
  1. `StandaloneKeyboardInputAdapter.cs` đang gán đồng thời phím **Left/Right Arrow** cho cả di chuyển ngang (`GetHorizontalAxis`) và đổi màu (`GetColorChunkStep`). Theo HTML gốc (và Master Prompt mục 4.1), phím mũi tên Trái/Phải **chỉ dùng để chỉnh pha quang học**, tuyệt đối không được làm nhân vật di chuyển ngang! Di chuyển ngang chỉ dùng **A / D**.
  2. Phím **Space** đang bị `EasyNewtonFinaleController` can thiệp qua cờ tĩnh `PlayerInput.IsNewtonFinaleActive`: khi vào buồng Newton, phím Space bị vô hiệu hóa chức năng nhảy để làm phím đo điểm. Trong khi đó, mã nguồn HTML gốc khẳng định tại dòng 1602–1603:  
     `// SPACE/W are jump only. X is exclusively the Newton mark/shot input.`  
     Phím Space phải luôn dùng để nhảy; phím **X** mới là phím độc quyền dùng để đánh dấu và bắn tia đo Newton!
  3. Thiếu ánh xạ phím **S** cho hành vi rơi nhanh (Fast Fall: `player.vy = Math.min(CFG.maxFall, player.vy + 900 * dt)`).
  4. Thiếu nút **X (MARK)** và nút chỉnh pha trên giao diện cảm ứng di động `VirtualControlsUI.cs`.

### 1.2. Luồng buồng đo Vòng tròn Newton (Newton Rings Gameplay Flow)
- **Cấu trúc hiện tại:**
  - `EasyNewtonFinaleController.cs` là một bộ điều khiển đơn lẻ được viết cứng cho một màn prototype "Easy" cũ với tọa độ cứng `centerO = (61.0, 0.625)`, bậc vòng cố định $k=4, i=2$.
  - `LevelLoader.cs` kiểm tra điều kiện cứng `IsEasyLevel` bằng cách dò tìm tọa độ xấp xỉ `p.position.x ≈ 45.625` hoặc tên file chứa `"map2"` hay `"easy"`. Nếu thỏa mãn mới kích hoạt `EasyNewtonFinaleController`.
  - Khi bắt đầu đo, `EasyNewtonFinaleController` bắt sự kiện `IsSpacePressedThisFrame()`, người chơi bấm Space để chốt điểm thay vì phím X.
- **Khiếm khuyết nghiêm trọng đối chiếu với HTML:**
  1. Hoàn toàn thiếu hỗ trợ buồng đo động theo cấu hình JSON của từng màn chơi (`L01` có $i=3, k=6$, `L02` có $i=2, k=4$ chế độ `dock`, `L03` có $i=2, k=7$).
  2. Bị trói buộc vào phím Space thay vì phím X.
  3. `LevelData.cs` hiện tại chưa có trường dữ liệu chứa cấu hình Newton (`newton`), khiến cấu hình buồng đo bị gán cứng trong mã nguồn C#.

### 1.3. Luồng dữ liệu màn chơi và JSON Levels (Level Data & Persistence Flow)
- **Cấu trúc hiện tại:**
  - `LevelRepository.cs` quản lý đường dẫn lưu trữ tại `Application.persistentDataPath/Levels` và các thư mục Releases kế cận.
  - `ContrastBuildPipeline.cs` trong hàm `PopulateReleaseLevels()` đang sao chép file `default_level.json` thành `Level_01.json`, `Easy_Level.json` thành `Level_02.json`, và `map2.json` thành `Level_03.json`. Đây là dữ liệu giả định/prototype, không chứa địa hình thật của 3 màn chơi chính thức L01, L02, L03 từ HTML!
- **Khiếm khuyết nghiêm trọng đối chiếu với HTML:**
  1. Màn L01 thiếu 16 khối địa hình chuẩn từ HTML (6 White, 4 Gray, 5 Black, 1 Fixed).
  2. Màn L02 thiếu 15 khối địa hình chuẩn từ HTML (3 White, 2 Gray, 8 Black, 2 Fixed), đặc biệt khối `L02_C_GHOST_WHITE` có vật liệu thực tế là `BLACK`.
  3. Màn L03 thiếu 27 khối địa hình chuẩn từ HTML, bao gồm 3 nền tảng di động dao động điều hòa (`item_mvnodpwx_a`, `item_mvnoqbwi_g`, `item_mvnp9r6r_o`).

---

## 2. DANH MỤC THIẾU SÓT VÀ TÍNH NĂNG CẦN BỔ SUNG (GAP ANALYSIS)

| Hệ thống | Hiện trạng Unity | Yêu cầu chuẩn HTML | Giải pháp kiến trúc tối thiểu an toàn |
|:---|:---|:---|:---|
| **Keyboard Input** | Mũi tên Trái/Phải vừa đi ngang vừa đổi màu; Space bị cướp làm nút đo | A/D đi ngang, Mũi tên đổi pha, Space/W nhảy, X đánh dấu Newton, S rơi nhanh | Hiệu chỉnh `StandaloneKeyboardInputAdapter.cs` và `IPlatformInputAdapter.cs` |
| **Mobile Virtual Input** | Chỉ có nút Left, Right, Jump, và các nút màu 1,2,3 | Cần có nút tương đương phím X (MARK), nút chỉnh pha, nút rơi nhanh | Bổ sung nút `BtnNewtonMark` và nút Tune Phase trên `VirtualControlsUI.cs`, chuyển tiếp vào `MobileTouchInputAdapter.cs` |
| **Phase Tuning Model** | `PlayerController` chuyển màu rời rạc (White, Gray, Black) | Pha liên tục $\phi$, $I = (1 + \cos\phi)/2$, ngưỡng White $\ge 0.78$, Black $\le 0.22$, Gray $0.22 < I < 0.78$ | Bổ sung trường `CurrentPhase` và hàm `TunePhase(float delta)` trên `PlayerController`, tính toán $I$ và tự động phân loại `LogicalColor` tương thích hoàn toàn với hệ thống va chạm AABB hiện có |
| **Newton Rings Chamber** | Gán cứng trong `EasyNewtonFinaleController` cho màn prototype | Mỗi màn có thông số riêng ($C_X, C_Y, i, k, \text{orbitStart}$, chu kỳ, góc quét) | Mở rộng `LevelData.cs` thêm lớp `NewtonChamberData`, nâng cấp controller thành `NewtonChamberController` đọc dữ liệu động từ `LevelData` |
| **Newton Marking Input** | Bấm Space | Bấm **phím X** (hoặc nút Mark ảo trên Mobile) | Bắt sự kiện `GetNewtonMarkDown()` từ input adapter, kiểm tra vị trí người chơi trên bục K / I |
| **JSON Levels** | Chỉ có file mẫu prototype giả lập | 3 màn chơi chính thức L01, L02, L03 với đầy đủ tọa độ, khối, checkpoints, collectibles, buồng Newton | Tạo mới và cập nhật chuẩn hóa 3 file JSON `L01.json`, `L02.json`, `L03.json` và đồng bộ vào `Assets/Resources/Levels` và `Release/Levels` |

---

## 3. DANH SÁCH CÁC TỆP DỰ KIẾN HIỆU CHỈNH (TARGET FILES FOR MODIFICATION)

### 3.1. Phân hệ Input & Controls
1. `Assets/Script/Contrast/Input/IPlatformInputAdapter.cs`
   - Bổ sung `bool GetNewtonMarkDown()`.
   - Bổ sung `bool GetFastFall()`.
   - Bổ sung `float GetPhaseTuneAxis()` (đọc mũi tên Trái/Phải).
2. `Assets/Script/Contrast/Input/StandaloneKeyboardInputAdapter.cs`
   - Tách biệt hoàn toàn: A/D chỉ điều khiển `GetHorizontalAxis()`; Mũi tên Trái/Phải chỉ điều khiển `GetPhaseTuneAxis()`.
   - W và Space điều khiển `GetJumpDown()`.
   - S điều khiển `GetFastFall()`.
   - X điều khiển `GetNewtonMarkDown()`.
3. `Assets/Script/Contrast/Input/MobileTouchInputAdapter.cs`
   - Bổ sung các phương thức `TriggerNewtonMark()`, `SetFastFall(bool)`, `TriggerPhaseTune(float)`.
4. `Assets/Script/Contrast/UI/VirtualControlsUI.cs`
   - Bổ sung nút ảo `BtnNewtonMark` (nhãn "MARK [X]") và các nút điều chỉnh pha trên màn hình cảm ứng.
5. `Assets/Script/Contrast/Player/Input/PlayerInput.cs`
   - Loại bỏ logic cướp phím Space của `IsNewtonFinaleActive`.
   - Bổ sung `NewtonMarkRequested`, `FastFallRequested`, `PhaseTuneDelta` vào `PlayerInputIntent`.

### 3.2. Phân hệ Player & Phase Tuning
6. `Assets/Script/Contrast/Player/PlayerController.cs`
   - Thêm trạng thái pha liên tục `currentPhase` và cập nhật cường độ $I = (1 + \cos(\text{phase}))/2$.
   - Tự động lượng tử hóa thành `LogicalColor` theo ngưỡng chuẩn HTML ($I \ge 0.78 \to$ White, $I \le 0.22 \to$ Black, còn lại Gray).
   - Xử lý gia tốc rơi nhanh khi có cờ `FastFallRequested`.
   - Phơi bày thuộc tính `CurrentGroundedPlatform` phục vụ kiểm tra điều kiện đứng trên thanh đo Newton.

### 3.3. Phân hệ Newton Rings Gameplay
7. `Assets/Script/Contrast/Data/LevelData.cs`
   - Mở rộng hợp đồng dữ liệu tương thích ngược: thêm cấu trúc `NewtonChamberData`, `NewtonRailData`, `CollectibleData`.
8. `Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs` (hoặc nâng cấp thành controller tái sử dụng tổng quát)
   - Tiếp nhận cấu hình buồng đo động từ `LevelData.newton` thay vì các hằng số cứng.
   - Hỗ trợ cả 3 cấu hình: L01 ($i=3, k=6$), L02 ($i=2, k=4$, dock), L03 ($i=2, k=7$).
   - Sử dụng intent `NewtonMarkRequested` (phím X) để khóa điểm.
9. `Assets/Script/Contrast/Level/LevelLoader.cs`
   - Xóa bỏ các heuristic kiểm tra tên file cũ. Tự động khởi tạo buồng Newton nếu `LevelData.newton != null`.

### 3.4. Phân hệ Dữ liệu JSON & Build Pipeline
10. `Assets/Resources/Levels/L01.json`, `L02.json`, `L03.json`
    - Xây dựng 3 tệp JSON chuẩn xác 100% tọa độ và khối từ HTML.
11. `Assets/Editor/ContrastBuildPipeline.cs`
    - Cập nhật hàm `PopulateReleaseLevels()` để đóng gói đúng L01, L02, L03 vào thư mục phát hành.

---

## 4. PHÂN TÍCH RỦI RO HỒI QUY VÀ BIỆN PHÁP PHÒNG NGỪA (REGRESSION RISKS)

1. **Rủi ro hồi quy chuyển động và va chạm AABB:**
   - *Nguy cơ:* Việc mở rộng pha liên tục có thể làm sai lệch các thuật toán va chạm AABB hiện có của `PlayerCollisionResolver`.
   - *Biện pháp:* Pha liên tục được ánh xạ thẳng về enum `LogicalColor` hiện tại (`White`, `Gray`, `Black`) ở mỗi khung hình. `PlayerCollisionResolver` và `ColorPlatform` tiếp tục sử dụng `LogicalColor` nguyên bản mà không bị thay đổi bất kỳ dòng mã va chạm cốt lõi nào.
2. **Rủi ro tương thích ngược của Level Editor:**
   - *Nguy cơ:* Việc thêm trường `newton` vào `LevelData` có thể làm hỏng chức năng Import/Export của Level Editor độc lập.
   - *Biện pháp:* Khai báo trường `newton` là tùy chọn (`[System.Serializable]`). Nếu tệp JSON cũ không có `newton`, giá trị sẽ là `null`. Bộ nạp và bộ tuần hoàn `LevelDataSanitizer` được cập nhật bảo toàn trường này khi xuất nhập.
3. **Rủi ro xung đột bộ kiểm thử tự động hiện có:**
   - *Nguy cơ:* Các ca kiểm thử trong `EditorLevelTests.cs` (như `Test_PlatformInputAdapters_And_Equivalence`, `Test_EasyNewtonFinale_Integration`) có thể kỳ vọng phím Space hoặc các giá trị cũ.
   - *Biện pháp:* Cập nhật các mock adapter và assertion trong `EditorLevelTests.cs` để phản ánh đúng chuẩn phím X và bộ tham số mới, bảo đảm toàn bộ suite kiểm thử đạt 100% PASS.

---

> **KẾT LUẬN GIAI ĐOẠN 0:**  
> Đã hoàn thành khảo sát toàn diện. Kế hoạch can thiệp tuân thủ nghiêm ngặt nguyên tắc thay đổi tối thiểu an toàn, bảo tồn cấu trúc tách bạch của Unity, và sẵn sàng chuyển sang **Phase 1: Input System Correction**.
