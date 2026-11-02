# BÁO CÁO 03: TÁI THIẾT CẤU TRÚC LEVEL JSON CHO 3 MÀN CHƠI CHÍNH THỨC (PHASE 4)

## 1. TỔNG QUAN YÊU CẦU & BỐI CẢNH

Theo Master Prompt (Mục 7), dự án cần cập nhật và cấu trúc lại đúng 3 tệp JSON của các màn chơi chiến dịch chính thức:
- **L01 — Cánh cửa không phải vật cản** (16 khối địa hình: 6 WHITE, 4 GRAY, 5 BLACK, 1 FIXED; 6 Checkpoint; 2 Quang Điểm; Buồng Newton $i=3, k=6$, `orbitStart = auto`).
- **L02 — Cầu thang ma** (15 khối địa hình: 3 WHITE, 2 GRAY, 8 BLACK, 2 FIXED; 5 Checkpoint; 2 Quang Điểm; Buồng Newton $i=2, k=4$, `orbitStart = dock`). Khối `L02_C_GHOST_WHITE` phải giữ nguyên vật liệu thực tế là `BLACK`.
- **L03 — Giếng ba tầng** (27 khối địa hình: 5 WHITE, 8 GRAY, 5 BLACK, 9 FIXED; 3 bục di động; 9 Checkpoint; 3 Quang Điểm; Buồng Newton $i=2, k=7$, `orbitStart = auto`).

Trước đây, thư mục `Release/Levels` và runtime chỉ chứa các file nguyên mẫu (prototype) cũ (`default_level.json`, `Easy_Level.json`, `map2.json`) với tọa độ giả định và thiếu hoàn toàn các khối đặc trưng, bục di động và cấu hình buồng đo Newton.

---

## 2. QUY ĐỔI TỌA ĐỘ VÀ ĐỒNG BỘ HỆ THỐNG VẬT LÝ

### 2.1. Tỷ lệ quy đổi & Hệ trục tọa độ
- **Tỷ lệ:** $1\text{ Unity Unit} = 64\text{ HTML Pixels}$.
- **Trục ngang:** Cùng hướng $+X$ sang phải.
  $$\text{posX}_{\text{unity}} = \frac{x_{\text{html}} + w_{\text{html}} / 2}{64}$$
  $$\text{sizeX}_{\text{unity}} = \frac{w_{\text{html}}}{64}$$
- **Trục đứng:** HTML hướng $+Y$ xuống dưới, Unity hướng $+Y$ lên trên.
  $$\text{posY}_{\text{unity}} = -\frac{y_{\text{html}} + h_{\text{html}} / 2}{64}$$
  $$\text{sizeY}_{\text{unity}} = \frac{h_{\text{html}}}{64}$$
- **Mép trên sàn và chân nhân vật:**
  $$Y_{\text{top, unity}} = -\frac{y_{\text{html}}}{64}$$
  $$Y_{\text{feet, unity}} = -\frac{\text{feet}_{\text{html}}}{64}$$
  Nhân vật có chiều cao $1.0\text{ u}$, do đó tâm nhân vật đặt tại $Y_{\text{center}} = Y_{\text{feet}} + 0.5$. Mép dưới nhân vật tiếp xúc với mép trên sàn có sai số tuyệt đối bằng $0.000000$, đảm bảo tiếp đất hoàn hảo ngay khung hình đầu tiên mà không bị rung giật hay xuyên thấu.

### 2.2. Ánh xạ Vật liệu & Màu sắc Logic
| Vật liệu HTML (`material`) | Màu sắc Unity (`rawGrayscaleColor`) | Phân loại `LogicalColor` | Hành vi va chạm |
|:---|:---:|:---:|:---|
| `WHITE` | `0.0` | `LogicalColor.White` | Đi xuyên khi nhân vật mang pha Trắng ($I \ge 0.78$); Rắn cản đường khi Đen/Xám |
| `GRAY` | `127.0` | `LogicalColor.Gray` | Đi xuyên khi nhân vật mang pha Xám ($0.22 < I < 0.78$); Rắn khi Trắng/Đen |
| `BLACK` | `212.0` | `LogicalColor.Black` | Đi xuyên khi nhân vật mang pha Đen ($I \le 0.22$); Rắn cản đường khi Trắng/Xám |
| `FIXED` | `255.0` | `LogicalColor.Universal` | **Rắn tuyệt đối (`alwaysSolid`)**, mọi pha màu đều đứng vững và không bao giờ xuyên qua |

### 2.3. Động học Bục Di Động Màn L03 (Kinematic Moving Platforms)
Ba bục di động trong L03 được tích hợp vào `TrajectoryData` của Unity:
1. **`item_mvnodpwx_a` (Thang máy đứng):**
   - Vật liệu: `BLACK` (`rawGrayscaleColor: 212.0`).
   - Dịch chuyển: $dx = 0, dy = -127\text{ px} \implies \Delta Y_{\text{unity}} = +1.984375\text{ u}$.
   - Chu kỳ: $T = 4.0\text{ s}$, `playbackMode: PingPong`, `rotation: 90.0°`, `scaleX: 1.984375`.
2. **`item_mvnoqbwi_g` (Bục nâng chéo):**
   - Vật liệu: `WHITE` (`rawGrayscaleColor: 0.0`).
   - Dịch chuyển: $dx = 213\text{ px} \implies \Delta X = +3.328125\text{ u}$, $dy = -258\text{ px} \implies \Delta Y = +4.03125\text{ u}$.
   - Độ dài: $L = 5.22802\text{ u}$, Góc: $50.4632^\circ$, Chu kỳ: $T = 5.0\text{ s}$, `playbackMode: PingPong`.
3. **`item_mvnp9r6r_o` (Bục chéo lớn):**
   - Vật liệu: `BLACK` (`rawGrayscaleColor: 212.0`).
   - Dịch chuyển: $dx = 461\text{ px} \implies \Delta X = +7.203125\text{ u}$, $dy = 676\text{ px} \implies \Delta Y = -10.5625\text{ u}$.
   - Độ dài: $L = 12.78589\text{ u}$, Góc: $-55.7073^\circ$, Chu kỳ: $T = 5.0\text{ s}$, `playbackMode: PingPong`.

---

## 3. DANH MỤC TỆP TIN ĐÃ TẠO VÀ CẬP NHẬT

1. [`Assets/Resources/Levels/L01.json`](file:///y:/Contrast/My%20project/Assets/Resources/Levels/L01.json):
   - 16 khối địa hình (6 White, 4 Gray, 5 Black, 1 Fixed).
   - 1 điểm xuất phát (`startPos`) + 5 điểm lưu (`checkPoint`) = 6 vị trí hồi sinh.
   - Buồng Newton: $cx=5135, cy=659, i=3, k=6$, `orbitStart = auto`.
   - 2 vật phẩm Quang Điểm.
2. [`Assets/Resources/Levels/L02.json`](file:///y:/Contrast/My%20project/Assets/Resources/Levels/L02.json):
   - 15 khối địa hình (3 White, 2 Gray, 8 Black, 2 Fixed).
   - Khối `L02_C_GHOST_WHITE` mang màu `BLACK` (`rawGrayscaleColor: 212.0`).
   - 1 điểm xuất phát + 4 điểm lưu = 5 vị trí hồi sinh.
   - Buồng Newton: $cx=5110, cy=528, i=2, k=4$, `orbitStart = dock`.
   - 2 vật phẩm Quang Điểm.
3. [`Assets/Resources/Levels/L03.json`](file:///y:/Contrast/My%20project/Assets/Resources/Levels/L03.json):
   - 27 khối địa hình (5 White, 8 Gray, 5 Black, 9 Fixed), bao gồm 3 bục di động có Trajectory.
   - 1 điểm xuất phát + 8 điểm lưu = 9 vị trí hồi sinh.
   - Buồng Newton: $cx=5201, cy=2100, i=2, k=7$, `orbitStart = auto`.
   - 3 vật phẩm Quang Điểm.
4. [`Assets/Script/Contrast/Data/LevelRepository.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelRepository.cs):
   - Tự động nạp sẵn mẫu `L01`, `L02`, `L03` vào `Application.persistentDataPath/Levels` ngay khi khởi động.
5. [`Assets/Editor/ContrastBuildPipeline.cs`](file:///y:/Contrast/My%20project/Assets/Editor/ContrastBuildPipeline.cs):
   - Hàm `PopulateReleaseLevels()` sao chép đầy đủ `L01.json`, `L02.json`, `L03.json` vào `Release/Levels` và `Release/Windows/Game/Levels`.
6. [`Assets/Script/Contrast/Player/PlayerController.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs):
   - Tích hợp kiểm tra chết rơi vực theo chuẩn HTML (`HasPotentialLandingBelow` và khoảng dung sai 240px rơi tự do).

---

## 4. BẢO TỒN TÍNH TƯƠNG THÍCH VỚI LEVEL EDITOR

- Dữ liệu level mới tuân thủ nghiêm ngặt schema `LevelData`:
  - `startPos`, `checkPoint`, `platforms`, `cameraBoundary`, `newton`, `collectibles`.
- Trình biên tập Level Editor (`LevelEditorScene.unity` / `LevelEditor.exe`) hoàn toàn có thể nạp, chỉnh sửa, di chuyển các khối và lưu ngược trở lại file JSON mà không phát sinh bất kỳ lỗi serialization nào.
- Các trường mở rộng mới (`newton`, `collectibles`, `levelId`, `levelName`) được bảo toàn trọn vẹn qua lớp `LevelDataSanitizer`.
