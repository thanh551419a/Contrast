# BÁO CÁO 01 — HIỆU CHỈNH HỆ THỐNG ĐIỀU KHIỂN & ĐỒNG BỘ ĐA NỀN TẢNG (INPUT CORRECTION REPORT)

**Dự án:** CONTRAST (Unity 6 / Windows Desktop & Android)  
**Tác giả:** Senior Unity C# Gameplay Engineer & Game Systems Architect  
**Tham chiếu quy chuẩn:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
**Ngày thực hiện:** 02/11/2026  

---

## 1. MỤC TIÊU & BỐI CẢNH HIỆU CHỈNH

Trong quá trình đối soát giữa bản game tham chiếu HTML gốc và kiến trúc Unity hiện tại, nhóm phát triển đã phát hiện các sai lệch nghiêm trọng về hệ thống phím bấm và mô hình pha quang học:
1. **Phím mũi tên (Arrow Keys) bị xung đột kép:** Trước đây, phím `LeftArrow` / `RightArrow` vừa làm người chơi di chuyển ngang vừa đồng thời kích hoạt chuyển màu theo nấc thô (`GetColorChunkStep`). Quy chuẩn HTML quy định rõ: **A / D là phím duy nhất để di chuyển ngang**, còn **← / → chỉ dành riêng để điều chỉnh góc pha liên tục (Phase Tuning)**.
2. **Phím Space bị chiếm quyền trong buồng Newton:** Bản prototype cũ dùng cờ tĩnh `IsNewtonFinaleActive` để chặn lệnh nhảy của phím `Space` và ép phím `Space` làm phím đánh dấu đo quang học. Quy chuẩn HTML quy định dứt khoát: **Space và W luôn luôn là phím nhảy**. Phím đánh dấu đo đạc vân tròn Newton **duy nhất và độc quyền là phím X**.
3. **Mô hình pha rời rạc vs. Mô hình pha liên tục:** Unity trước đây lưu trữ trạng thái màu rời rạc (`LogicalColor.White/Gray/Black`), trong khi HTML gốc sử dụng mô hình sóng giao thoa với góc pha tuần hoàn liên tục $\phi \in [0, 2\pi)$, hàm cường độ sáng:
   $$I = \frac{1 + \cos(\phi)}{2}$$
   Cùng các ngưỡng phân loại vật lý:
   - $I \ge 0.78$: **SÁNG (WHITE / BRIGHT)**
   - $I \le 0.22$: **TỐI (BLACK / DARK)**
   - $0.22 < I < 0.78$: **ĐIỀU PHA (GRAY / TUNING)**
4. **Thiếu hỗ trợ rơi nhanh (Fast Fall - phím S):** Người chơi giữ phím `S` để gia tốc rơi nhanh hơn khi vượt các vực sâu hoặc giếng thẳng đứng.
5. **Giao diện cảm ứng Android thiếu nút chức năng tương đương:** Cần bổ sung các nút bấm ảo cho điều pha, đánh dấu Newton (X) và rơi nhanh (S).

---

## 2. BẢNG ÁNH XẠ ĐIỀU KHIỂN CHÍNH THỨC (OFFICIAL MAPPING)

| Hành động (Gameplay Action) | Bàn phím Windows (Desktop) | Nút ảo Android (Mobile Touch) | Ý định game (Gameplay Intent) |
|---|---|---|---|
| **Di chuyển sang trái** | `A` | Nút giữ `◄` (BtnLeft) | `Move.x = -moveSpeed * dt` |
| **Di chuyển sang phải** | `D` | Nút giữ `►` (BtnRight) | `Move.x = +moveSpeed * dt` |
| **Nhảy (Jump)** | `W` hoặc `Space` | Nút chạm `JUMP` (BtnJump) | `JumpRequested = true` (khi tiếp đất) |
| **Rơi nhanh (Fast Fall)** | `S` | Nút giữ `▼ FALL` (BtnFastFall) | `FastFall = true` (tăng gia tốc rơi $+14.06\text{ m/s}^2$) |
| **Điều pha sang trái (Tune Left)** | `LeftArrow` | Nút giữ `◄ PHASE` (BtnPhaseLeft) | `PhaseTuneDelta = -2.75 * dt\text{ rad}` |
| **Điều pha sang phải (Tune Right)**| `RightArrow` | Nút giữ `PHASE ►` (BtnPhaseRight) | `PhaseTuneDelta = +2.75 * dt\text{ rad}` |
| **Đánh dấu buồng Newton (Newton Mark)** | `X` | Nút chạm `X (MARK)` (BtnNewtonMark) | `NewtonMarkRequested = true` |
| **Chơi lại màn (Restart)** | `R` | Menu / UI Pause | Tái khởi động tại trạm gần nhất |

> **Quy tắc tuyệt đối:** Phím `Space` chỉ dùng để nhảy. Bấm `Space` không bao giờ sinh ra tín hiệu `NewtonMarkRequested`. Phím `X` chỉ dùng để đánh dấu buồng Newton và không làm nhân vật nhảy.

---

## 3. CÁC TẬP TIN ĐÃ THAY ĐỔI & KIẾN TRÚC THỰC HIỆN

### 3.1. `Assets/Script/Contrast/Input/IPlatformInputAdapter.cs`
- Bổ sung các phương thức giao kết đa nền tảng:
  - `bool GetFastFall()`
  - `float GetPhaseTuneAxis()`
  - `bool GetNewtonMarkDown()`
- Giữ nguyên `GetHorizontalAxis()`, `GetJumpDown()`, `GetColorChunkStep()`, `TryGetDirectColor()`, `ResetFrameState()`.

### 3.2. `Assets/Script/Contrast/Input/StandaloneKeyboardInputAdapter.cs`
- `GetHorizontalAxis()`: Chỉ đọc `A` và `D`. Tách bỏ hoàn toàn phím mũi tên khỏi chuyển động ngang.
- `GetJumpDown()`: Đọc `W` hoặc `Space`. Loại bỏ sự phụ thuộc vào cờ finale cũ.
- `GetFastFall()`: Trả về trạng thái phím `S`.
- `GetPhaseTuneAxis()`: `RightArrow` (+1f), `LeftArrow` (-1f).
- `GetNewtonMarkDown()`: Trả về `X` được nhấn trong frame (`wasPressedThisFrame`).

### 3.3. `Assets/Script/Contrast/Input/MobileTouchInputAdapter.cs`
- Thêm các biến trạng thái: `fastFallHeld`, `phaseTuneLeftHeld`, `phaseTuneRightHeld`, `newtonMarkTriggered`.
- Cung cấp các trình kích hoạt sự kiện: `SetFastFall(bool)`, `SetPhaseTuneLeft(bool)`, `SetPhaseTuneRight(bool)`, `TriggerNewtonMark()`.
- Cài đặt đầy đủ các phương thức tương ứng của `IPlatformInputAdapter`.

### 3.4. `Assets/Script/Contrast/UI/VirtualControlsUI.cs`
- Xây dựng thêm nút giữ `▼ FALL` tại góc dưới trái.
- Xây dựng 2 nút giữ `◄ PHASE` và `PHASE ►` tại cạnh dưới màn hình phục vụ điều pha liên tục.
- Xây dựng nút nổi bật `X (MARK)` với viền vàng kim tại góc phải trên nút Jump, kích hoạt độc quyền phép đo Newton.
- Hỗ trợ tham số cỡ chữ tùy biến trong `CreateTouchHoldButton`.

### 3.5. `Assets/Script/Contrast/Player/Input/PlayerInput.cs`
- Loại bỏ logic chiếm quyền nhảy của phím Space (`IsNewtonFinaleActive`).
- Mở rộng struct `PlayerInputIntent` với:
  - `public readonly bool FastFall`
  - `public readonly float PhaseTuneDelta`
  - `public readonly bool NewtonMarkRequested`
- Tính toán độ lệch pha theo chuẩn HTML: `PhaseTuneDelta = phaseTuneAxis * 2.75f * deltaTime`.
- Cung cấp hàm khởi tạo tương thích ngược cho các unit test cũ.

### 3.6. `Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs`
- Bổ sung overload `GetMoveIntent(float deltaTime, bool isGrounded, bool fastFall)`.
- Khi `fastFall == true`, áp dụng gia tốc rơi bổ sung $14.0625\text{ units/s}^2$ ($900\text{ px/s}^2 / 64$), tương đương hệt hành vi trong HTML: `player.vy = Math.min(CFG.maxFall, player.vy + 900*dt)`.

### 3.7. `Assets/Script/Contrast/Player/PlayerController.cs`
- Thêm trường dữ liệu góc pha liên tục `currentPhase` ($\phi$), khởi tạo mặc định bằng 0 hoặc $\pi/2$.
- Cung cấp các phương thức toán học tĩnh:
  - `WrapPhase(float phi)`: chuẩn hóa góc pha về chu kỳ $[0, 2\pi)$.
  - `CalculateIntensity(float phase)`: tính $I = (1 + \cos\phi)/2$.
  - `ClassifyIntensity(float intensity)`: phân loại màu theo đúng ngưỡng $0.78$ và $0.22$.
- Cung cấp hàm `TunePhase(float delta)` và `SetPhase(float phase)` để cập nhật màu và giá trị xám tương ứng.
- Trong `ProcessUpdate`:
  - Reset `NewtonMarkRequested = false`.
  - Tiếp nhận sự kiện `input.NewtonMarkRequested` và bắn `OnNewtonMarkRequested`.
  - Tiếp nhận `input.PhaseTuneDelta` để gọi `TunePhase()`.
  - Truyền cờ `input.FastFall` vào `jumpMotion.GetMoveIntent()`.

### 3.8. `Assets/Script/Contrast/Presentation/PhaseTunerUI.cs`
- Kết nối trực tiếp với `player.CurrentPhase` thực tế thay vì nội suy góc pha giả lập.
- Cập nhật đồ thị giao thoa 2 sóng, dải màu ombre và nhãn trạng thái chính xác theo góc pha thực của người chơi.
- Cập nhật bảng hướng dẫn điều khiển góc trên bên trái hiển thị đầy đủ 7 phím điều khiển chính thức.

### 3.9. `Assets/Editor/EditorLevelTests.cs`
- Cập nhật mock `MockControlledInputAdapter` hỗ trợ `FastFall`, `PhaseTuneAxis`, `NewtonMark`.
- Bổ sung 3 ca kiểm thử tương đương đa nền tảng mới:
  - `Scenario E`: Kiểm thử tương đương cờ `FastFall`.
  - `Scenario F`: Kiểm thử tương đương `PhaseTuneDelta`.
  - `Scenario G`: Kiểm thử tương đương `NewtonMarkRequested` (nhấn X / nút Mark tạo intent, và nhấn Space tuyệt đối KHÔNG tạo intent đánh dấu Newton).

---

## 4. KẾT QUẢ KIỂM THỬ GIAI ĐOẠN 1

- Hệ thống phân tách rõ ràng giữa bàn phím Desktop và nút bấm cảm ứng Mobile. Cả hai luồng adapter đều sinh ra cùng một cấu trúc `PlayerInputIntent` chuẩn mực.
- Đã kiểm tra không có xung đột phím giữa di chuyển (A/D) và điều pha (←/→).
- Đã xác thực phím Space chỉ dùng để nhảy trong mọi hoàn cảnh, giải quyết dứt điểm lỗi kế thừa từ prototype Easy.
