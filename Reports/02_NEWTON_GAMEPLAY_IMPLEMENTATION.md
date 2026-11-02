# BÁO CÁO 02: THỰC THI GAMEPLAY & HIỂN THỊ QUANG HỌC VÂN TRÒN NEWTON (PHASE 2 & PHASE 3)

## 1. TỔNG QUAN YÊU CẦU & BỐI CẢNH

Theo tài liệu chỉ đạo Master Prompt và mã nguồn HTML gốc ([`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`](file:///y:/Contrast/My%20project/CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html)), màn chơi Newton Rings không phải là hoạt cảnh nền thụ động hay trigger chiến thắng tự động khi người chơi bước vào. Đây là một giai đoạn gameplay đo đạc quang học tương tác thực sự:
- **Người chơi trực tiếp điều khiển quá trình đo đạc:** Nhảy lên thanh ray quỹ đạo, canh chuẩn trục quang học $K - O - K'$, và nhấn phím **`X`** (hoặc nút ảo **`X (MARK)`** trên mobile) để khóa các mốc $K$, $I$, $K'$.
- **Phím Space không được kích hoạt đo:** Phím `Space` hoàn toàn dành riêng cho thao tác Nhảy (`Jump`).
- **Cơ chế dung sai & canh trục:** Bước $I$ yêu cầu dung sai căn chỉnh $\le 48\text{ px}$ ($0.75\text{ u}$), bước $K'$ yêu cầu dung sai căn chỉnh $\le 54\text{ px}$ ($0.84375\text{ u}$). Nếu canh sai, hệ thống từ chối khóa mốc và người chơi phải chờ chu kỳ quỹ đạo tiếp theo để căn lại.
- **Dữ liệu cấu hình động từ Level JSON:** Mỗi màn chơi có tâm buồng Newton ($C_X, C_Y$), bậc vân ($i, k$), chu kỳ quay ($T$), kích thước thanh ray và chế độ kích hoạt quỹ đạo (`orbitStart = "auto"` hoặc `"dock"`) riêng biệt được tải trực tiếp từ JSON, không hardcode trong C#.

---

## 2. KIẾN TRÚC & HỆ THỐNG ĐÃ SỬA ĐỔI

### 2.1. Mở Rộng Level Schema Cho Buồng Newton
Trong [`Assets/Script/Contrast/Data/LevelData.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs), cấu trúc `NewtonChamberData`, `NewtonRailsConfigData`, `NewtonRailData` được bổ sung và tích hợp vào `LevelData`:
```csharp
[Serializable]
public class NewtonChamberData
{
    public float cx;
    public float cy;
    public int i = 3;
    public int k = 6;
    public float R = 1500f;
    public float lambda = 550f;
    public string orbitStart = "auto"; // "auto" hoặc "dock"
    public NewtonRailsConfigData rails;
}
```
Lớp [`LevelDataSanitizer.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelDataSanitizer.cs) đã được cập nhật để bảo toàn và chuẩn hóa toàn bộ trường dữ liệu `newton` và `collectibles` mà không làm hỏng dữ liệu level cũ.

### 2.2. Bộ Điều Khiển Đo Đạc Động ([`EasyNewtonFinaleController.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs))
Trước đây, `EasyNewtonFinaleController` chứa các thông số hardcode cho bàn Easy cũ ($cx=5135, cy=659, k=6, i=3$) và lắng nghe phím `Space` trong `Update`.

Chúng tôi đã cấu trúc lại bộ điều khiển:
1. **Khởi tạo động qua `Initialize(PlayerController player, SmoothCamera2D camFollow, NewtonChamberData newtonData)`:**
   - Tính toán bán kính vân chuẩn theo công thức quang học HTML:
     $$r_k = \sqrt{\frac{k}{N}} \cdot R_{\text{outer}}, \quad r_i = \sqrt{\frac{i}{N}} \cdot R_{\text{outer}}$$
     với $N=7$ và $R_{\text{outer}} = 380\text{ px} / 64 = 5.9375\text{ u}$.
   - Thiết lập tâm $C = (cx / 64, -cy / 64)$.
   - Khởi tạo thanh ray $K$ và $I$ với kích thước, chu kỳ ($T$), hướng quay, và góc xuất phát đọc từ `newtonData.rails` hoặc suy diễn chính xác theo HTML.
2. **Quản lý chế độ quỹ đạo `orbitStart`:**
   - Nếu `orbitStart == "auto"`: Quỹ đạo tự động quay ngay khi buồng đo được kích hoạt.
   - Nếu `orbitStart == "dock"`: Thanh ray $K$ và $I$ đứng yên tại vị trí ban đầu cho đến khi người chơi lần đầu tiên tiếp đất thành công lên thanh $K$. Khi người chơi chạm $K$, hệ thống chuyển `orbitRunning = true` và bắt đầu quay.
3. **Lắng nghe phím bấm chuẩn hóa:**
   - Kiểm tra phím `X` thông qua `player.InputIntent.NewtonMarkRequested` (hoặc fallback `Input.GetKeyDown(KeyCode.X)` trên standalone).
   - **Tuyệt đối không sử dụng Space.** Phím Space được bảo lưu cho nhảy.
4. **Máy trạng thái đo đạc (`MeasurementStep`):**
   - `WaitingForK`: Người chơi phải đứng trên thanh $K$. Nhấn `X` sẽ khóa vị trí hiện tại của $K$, thiết lập góc tham chiếu $\theta_K$, vẽ đường kính trục $K - O - K'$, hiển thị mốc $K$, phát âm thanh/hiệu ứng thành công và chuyển sang bước `WaitingForI`.
   - `WaitingForI`: Người chơi di chuyển sang thanh $I$. Khi nhấn `X`:
     - Kiểm tra người chơi có đang đứng trên $I$ không.
     - Tính toán vị trí kỳ vọng của $I$ trên trục tham chiếu: $P_{I,\text{expected}} = C + r_I \cdot (\cos\theta_K, \sin\theta_K)$.
     - Tính khoảng cách sai lệch thực tế: $\Delta d = \| P_I - P_{I,\text{expected}} \|$.
     - So sánh với dung sai $48\text{ px} / 64 = 0.75\text{ u}$.
     - Nếu $\Delta d \le 0.75\text{ u}$: Khóa mốc $I$, ghi nhận độ chính xác, hiển thị mốc $I$, chuyển sang `WaitingForKPrime`.
     - Nếu $\Delta d > 0.75\text{ u}$: Báo lỗi *"Chưa đúng trục K-O! Lệch: X px (Yêu cầu <= 48 px)"*, rung đỏ HUD và không chuyển bước.
   - `WaitingForKPrime`: Người chơi quay lại thanh $K$. Khi nhấn `X`:
     - Kiểm tra người chơi có đang đứng trên $K$ không.
     - Vị trí kỳ vọng đối diện: $P_{K',\text{expected}} = C - r_k \cdot (\cos\theta_K, \sin\theta_K)$.
     - Tính khoảng cách sai lệch: $\Delta d = \| P_K - P_{K',\text{expected}} \|$.
     - So sánh với dung sai $54\text{ px} / 64 = 0.84375\text{ u}$.
     - Nếu $\Delta d \le 0.84375\text{ u}$: Khóa mốc $K'$, hoàn thành đo đạc, chuyển sang `RevealingResult`.
     - Nếu $\Delta d > 0.84375\text{ u}$: Báo lỗi *"Chưa đúng phía đối diện! Lệch: X px (Yêu cầu <= 54 px)"*.
   - `RevealingResult`: Thời gian đếm ngược chính xác **5.6 giây** theo HTML reference (`revealTimer = 5.6f`).
     - Hiển thị công thức tính bước sóng:
       $$\lambda = \frac{D_k^2 - D_i^2}{4(k - i) R} \approx 550.0\text{ nm}$$
     - Sau 5.6 giây, gọi hàm `CompleteLevel()` kích hoạt flow chiến thắng của Unity (`VictoryTrigger` / `LevelCompleted`).

### 2.3. HUD Đo Đạc & Phản Hồi Tiếng Việt
HUD được cấu hình chuẩn mực với các chuỗi chỉ dẫn chính thức:
- **Bước 1 (Chọn K):**
  - Tiêu đề: `CHỌN K TRÊN VÂN k` (hoặc `CHỌN K TRÊN VÂN 6 / VÂN 4 / VÂN 7`)
  - Hướng dẫn: `Đứng trên thanh K và nhấn X để đánh dấu`
- **Bước 2 (Tìm I):**
  - Tiêu đề: `TÌM I TRÊN VÂN i`
  - Hướng dẫn: `Đứng trên thanh I, căn đúng trục K–O và nhấn X`
- **Bước 3 (Tìm K'):**
  - Tiêu đề: `TÌM K′ TRÊN VÂN k`
  - Hướng dẫn: `Quay lại thanh K, căn phía đối diện và nhấn X`
- **Hoàn thành:**
  - Tiêu đề: `ĐÃ HOÀN THÀNH K – I – K′`
  - Hướng dẫn: `Đang tính toán bước sóng ánh sáng λ...`

### 2.4. Tích Hợp Vào LevelLoader ([`LevelLoader.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs))
Trong phương thức `BuildLevel`:
- Kiểm tra `levelData.newton != null && levelData.newton.k > 0`.
- Tạo `GameObject ("NewtonChamberController")` gắn component `EasyNewtonFinaleController`.
- Gọi `newtonCtrl.Initialize(spawnedPlayer, camFollow, levelData.newton);`.
- Nếu level không có cấu hình buồng Newton (hoặc $k \le 0$), hệ thống duy trì cờ đích đến thông thường (`flagPole`).

---

## 3. DANH SÁCH FILE ĐÃ SỬA ĐỔI

| STT | Tệp tin | Thay đổi chính |
|---|---|---|
| 1 | [`Assets/Script/Contrast/Data/LevelData.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs) | Thêm các DTO `NewtonChamberData`, `NewtonRailsConfigData`, `NewtonRailData`, `CollectibleData`. |
| 2 | [`Assets/Script/Contrast/Data/LevelDataSanitizer.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelDataSanitizer.cs) | Bảo toàn cấu trúc `newton`, `collectibles` khi nạp và lưu JSON. |
| 3 | [`Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs) | Khởi tạo động từ JSON, mapping phím X, kiểm tra dung sai 48px/54px, orbitStart auto/dock, 5.6s reveal. |
| 4 | [`Assets/Script/Contrast/Level/LevelLoader.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | Khởi tạo tự động buồng đo Newton theo dữ liệu của level được nạp. |

---

## 4. XÁC MINH CƠ CHẾ AN TOÀN & KHÔNG REGRESSION

1. **Người chơi ngã/chết trong khi đo đạc:** Khi chết và hồi sinh tại Checkpoint, trạng thái đo đạc được reset an toàn về mốc ban đầu mà không làm crash game.
2. **Bấm phím X sai vị trí:** Nhấn `X` ngoài buồng đo hoặc khi đang ở trên không sẽ không kích hoạt bất kỳ mốc đánh dấu nào và không làm chuyển trạng thái.
3. **Bấm phím Space:** Space tạo lực nhảy bình thường lên người chơi mà không gửi tín hiệu đánh dấu Newton.
4. **Tính độc lập với Editor:** Trình soạn thảo level vẫn có thể tải, chỉnh sửa và lưu các block địa hình bình thường nhờ cơ chế serialization tương thích ngược.
