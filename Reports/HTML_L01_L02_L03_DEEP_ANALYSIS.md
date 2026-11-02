# BÁO CÁO PHÂN TÍCH CHUYÊN SÂU MÀN CHƠI L01, L02, L03 & TIẾN TRÌNH GAMEPLAY TOÀN PHẦN
*(DEEP BLOCK INVENTORY & FULL LEVEL GAMEPLAY FLOW ANALYSIS)*

> **Tài liệu phân tích kỹ thuật đảo ngược độc quyền cho 3 màn chơi chính thức L01, L02, L03.**  
> **Tệp nguồn tham chiếu:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
> **Nguyên tắc:** CHỈ PHÂN TÍCH (ANALYSIS-ONLY). Không tạo file JSON, không sửa mã nguồn Unity, không thay đổi kiến trúc.  
> **Phiên bản tài liệu:** 2.0 (Cập nhật theo Addendum kiểm kê toàn bộ khối và luồng chơi 2 giai đoạn).

---

## MỤC LỤC TỔNG QUAN

1. [Quy ước kỹ thuật & Ma trận màu sắc (Color Collision Matrix)](#1-quy-ước-kỹ-thuật--ma-trận-màu-sắc-color-collision-matrix)
2. [Phân tích chi tiết Màn L01: "Cánh cửa không phải vật cản"](#2-phân-tích-chi-tiết-màn-l01-cánh-cửa-không-phải-vật-cản)
3. [Phân tích chi tiết Màn L02: "Cầu thang ma"](#3-phân-tích-chi-tiết-màn-l02-cầu-thang-ma)
4. [Phân tích chi tiết Màn L03: "Giếng ba tầng"](#4-phân-tích-chi-tiết-màn-l03-giếng-ba-tầng)
5. [So sánh khối địa hình liên màn chơi (Cross-Level Block Comparison)](#5-so-sánh-khối-địa-hình-liên-màn-chơi-cross-level-block-comparison)
6. [So sánh luồng chơi liên màn chơi (Cross-Level Gameplay Comparison)](#6-so-sánh-luồng-chơi-liên-màn-chơi-cross-level-gameplay-comparison)
7. [Các câu hỏi kỹ thuật cần xác nhận (Questions for Joint Review)](#7-các-câu-hỏi-kỹ-thuật-cần-xác-nhận-questions-for-joint-review)

---

## 1. QUY ƯỚC KỸ THUẬT & MA TRẬN MÀU SẮC (COLOR COLLISION MATRIX)

Dựa trên việc truy vết trực tiếp mã nguồn HTML tại các hàm `matches` (dòng 546–551), `isPassable` (dòng 752–761), `moveX` / `moveY` (dòng 769–798), và `editorResolveMovingSolids` (dòng 916–960):

### 1.1. Bản chất các trạng thái màu và cơ chế vật lý
Nhân vật sở hữu giá trị cường độ giao thoa $I \in [0, 1]$ tương ứng với pha sóng $\phi$:
- **Trạng thái SÁNG (BRIGHT / WHITE):** $I \ge 0.78$ (Pha $\phi = +\pi/2$).
- **Trạng thái TỐI (DARK / BLACK):** $I \le 0.22$ (Pha $\phi = -\pi/2$).
- **Trạng thái ĐIỀU PHA (TUNING / GRAY):** $0.22 < I < 0.78$ (Pha $\phi = 0$).

### 1.2. Ma trận tương tác va chạm thực tế (Actual HTML Collision Matrix)

| Vật liệu khối (`material`) | Tính chất vật lý cơ sở | Khi người chơi là TRẮNG ($I \ge 0.78$) | Khi người chơi là XÁM ($0.22 < I < 0.78$) | Khi người chơi là ĐEN ($I \le 0.22$) | Cơ chế Ghosting / Tái hóa rắn an toàn |
|:---|:---|:---:|:---:|:---:|:---|
| **WHITE** (Trắng) | Khối pha dương | **XUYÊN QUA (Passable)**<br>*Không va chạm, đi xuyên* | **RẮN (Solid)**<br>*Đứng lên hoặc cản đường* | **RẮN (Solid)**<br>*Đứng lên hoặc cản đường* | Nếu đổi sang Đen khi đang lọt trong lòng khối, người chơi duy trì trạng thái Ghost cho đến khi thoát hẳn ra ngoài. |
| **BLACK** (Đen) | Khối pha âm | **RẮN (Solid)**<br>*Đứng lên hoặc cản đường* | **RẮN (Solid)**<br>*Đứng lên hoặc cản đường* | **XUYÊN QUA (Passable)**<br>*Không va chạm, đi xuyên* | Nếu đổi sang Trắng khi đang lọt trong lòng khối, người chơi duy trì Ghost cho đến khi thoát hẳn ra ngoài. |
| **GRAY** (Xám) | Khối vùng tinh chỉnh | **RẮN (Solid)**<br>*Đứng lên hoặc cản đường* | **XUYÊN QUA (Passable)**<br>*Không va chạm, đi xuyên* | **RẮN (Solid)**<br>*Đứng lên hoặc cản đường* | Áp dụng tương tự: chỉ xuyên qua khi người chơi đang ở pha Xám ($0.22 < I < 0.78$). |
| **FIXED** (Cố định) | Khối kết cấu vĩnh cửu | **RẮN TUYỆT ĐỐI (Always Solid)** | **RẮN TUYỆT ĐỐI (Always Solid)** | **RẮN TUYỆT ĐỐI (Always Solid)** | `alwaysSolid: true`. Không bao giờ xuyên qua bất kể người chơi mang pha nào. Dùng làm sàn neo buồng đo hoặc thành giếng. |

### 1.3. Sự khác biệt giữa khối tĩnh và bục di động
- **Khối tĩnh (Static blocks):** Tham gia hệ thống va chạm AABB quét (swept AABB). Cản trở hoặc cho phép xuyên qua tức thì theo hàm `isPassable(c)`.
- **Bục di động (Moving platforms):** Có thuộc tính `motion: {enabled: true, dx, dy, period, phase, style}`. Khi người chơi đứng trên bục (`player.groundedOn === platform.id`), vector dịch chuyển $(\Delta x, \Delta y)$ của bục được cộng trực tiếp vào vị trí người chơi (`editorMovingCarries`). Nếu bục di chuyển ép người chơi va vào một khối rắn khác không cho xuyên qua, hàm `editorPathIsFree` phát hiện khoảng cách hẹp và kích hoạt sự kiện chết kẹp (`crushed = true`).

---

## 2. PHÂN TÍCH CHI TIẾT MÀN L01: "CÁNH CỬA KHÔNG PHẢI VẬT CẢN"

### A. Mô tả chung (General Description)
- **Level ID:** `L01`
- **Tên hiển thị:** `"Cánh cửa không phải vật cản"` (*The Door Is Not an Obstacle*)
- **Mục tiêu gameplay:** Dạy người chơi quy tắc cốt lõi: một bức tường cùng màu/cùng pha sẽ cho phép người chơi đi xuyên qua, trong khi một mặt sàn khác màu sẽ giữ người chơi đứng vững.
- **Độ khó:** Dễ / Nhập môn (Tutorial).
- **Chỉ dẫn gốc trong mã nguồn HTML:**  
  > *"Học cơ chế đổi pha trắng/đen cơ bản; các bức tường tương phản chỉ là ảo ảnh nếu chọn đúng pha."*

### B. Thống kê khối địa hình (Block Statistics)
- **Tổng số khối địa hình (`terrain`):** **16 khối** (Được định nghĩa tại `window.__CE_LEVELS__[0].terrain`).
  - Khối màu **TRẮNG (WHITE):** **6 khối** (4 sàn/thềm, 2 cánh cửa đứng).
  - Khối màu **XÁM (GRAY):** **4 khối** (3 sàn/thềm chuyển tiếp, 1 vách cản).
  - Khối màu **ĐEN (BLACK):** **5 khối** (3 sàn di chuyển, 2 cánh cửa đứng).
  - Khối đặc biệt **CỐ ĐỊNH (FIXED):** **1 khối** (Hành lang đệm dẫn vào buồng đo `vestibule`).
- **Phân loại động học:**
  - Khối tĩnh (Static): **16 khối** (100% địa hình trong màn L01 là tĩnh).
  - Khối di động (Moving): **0 khối** (Không có nền tảng di động trong màn L01).
  - Bục bóng mờ (Ghost Platforms): **0 khối riêng biệt** (Các cánh cửa đóng vai trò ghost wall khi cùng pha).
  - Khối bẫy sát thương (Hazards): **0 khối** (Chỉ có nguy cơ chết rơi vực `void`).
*(Lưu ý: 2 ray quét Newton `E_ORBIT_K` và `E_ORBIT_I` được sinh động trong Giai đoạn B và được phân tích riêng ở mục F).*

### C. Danh mục khối hoàn chỉnh (Complete Block Inventory)

| STT | Object ID | Loại hình | Màu sắc | X (px) | Y (px) | Rộng (W) | Cao (H) | Động học | Quy tắc va chạm thực tế trong gameplay |
|:---:|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 1 | `item_mvhs859o_1` | Sàn xuất phát | BLACK | -662 | 972 | 469 | 69 | Static | Rắn khi Trắng/Xám; Đỡ chân người chơi lúc xuất phát (pha Trắng $\pi/2$). |
| 2 | `item_mvhsvutn_1` | Cánh cửa rào 1 | WHITE | -265 | 641 | 72 | 331 | Static | Tường cản Trắng. Người chơi giữ pha Trắng để đi xuyên qua. |
| 3 | `item_mvhswhcv_2` | Sàn chuyển tiếp | GRAY | -193 | 972 | 484 | 69 | Static | Rắn khi Trắng/Đen. Giữ người chơi an toàn khi đi xuyên qua Cửa Trắng 1. |
| 4 | `item_mvhsx0za_3` | Sàn nhịp 2 | BLACK | 291 | 972 | 717 | 69 | Static | Rắn khi Trắng; Đón người chơi sau Cửa Trắng 1 và đưa tới Cửa Đen 1. |
| 5 | `item_mvhsx75h_4` | Cánh cửa rào 2 | BLACK | 944 | 641 | 64 | 331 | Static | Tường cản Đen. Người chơi phải chuyển sang pha Đen để đi xuyên qua. |
| 6 | `item_mvhsyr8t_7` | Sàn bậc trũng | GRAY | 1136 | 1063 | 298 | 73 | Static | Rắn khi Trắng/Đen. Nằm thấp hơn sàn chính, tạo bước nhảy tiếp cận. |
| 7 | `item_mvhszcke_8` | Sàn thềm 1 | WHITE | 1597 | 931 | 282 | 69 | Static | Rắn khi Đen; Đón người chơi nhảy qua vực. Phía trên có Quang Điểm 1. |
| 8 | `item_mvhszkrh_9` | Sàn thềm 2 | WHITE | 2043 | 825 | 412 | 84 | Static | Rắn khi Đen; Nâng độ cao tiếp cận Cánh cửa Đen 3. |
| 9 | `item_mvhszos4_a` | Cánh cửa rào 3 | BLACK | 2375 | 485 | 80 | 340 | Static | Tường cản Đen trên cao. Phải đổi sang pha Đen trên không để lọt qua. |
| 10 | `item_mvht1ljj_c` | Sàn đón sau cửa | WHITE | 2588 | 769 | 303 | 78 | Static | Rắn khi Đen; Đón chân người chơi sau khi lọt qua Cửa Đen 3. |
| 11 | `item_mvhtci4u_g` | Sàn cầu nối | GRAY | 2891 | 769 | 421 | 78 | Static | Rắn khi Trắng/Đen; Đoạn nối trung tính an toàn. |
| 12 | `item_mvht241o_d` | Sàn bước nhảy | BLACK | 3312 | 769 | 318 | 78 | Static | Rắn khi Trắng; Yêu cầu đổi pha Trắng để đứng và lấy đà nhảy cao. |
| 13 | `item_mvht2hrm_e` | Cánh cửa rào 4 | WHITE | 3630 | 485 | 92 | 362 | Static | Tường cản Trắng lớn. Phía trên có Quang Điểm 2. Cần pha Trắng để xuyên. |
| 14 | `item_mvht2mhk_f` | Sàn thềm lớn | WHITE | 3722 | 769 | 489 | 78 | Static | Rắn khi Đen; Đón rơi sau Cửa Trắng 4. |
| 15 | `item_mvhtg97m_h` | Sảnh đệm buồng | FIXED | 4286 | 769 | 185 | 78 | Static | **RẮN TUYỆT ĐỐI**. Sàn tiếp dẫn vào buồng đo Newton (`vestibuleId`). |
| 16 | `item_mvhtkgzc_i` | Vách cản Xám | GRAY | 460 | 641 | 68 | 331 | Static | Tường cản Xám tại $X=460$. Đòi hỏi chuyển pha Xám để đi qua. |

### D. Cấu trúc bản đồ (Map Structure)
Bản đồ L01 có kích thước $W = 6169\text{ px}, H = 1693\text{ px}$. Tuyến đường được bố trí theo dạng đường ống nằm ngang liên tục từ trái sang phải, chia làm 5 khu vực:
1. **Phân đoạn 1 — Khởi đầu & Cửa Trắng 1 ($X = -662 \to 291$):** Xuất phát tại CP0 ($X=-571$) trên sàn Đen, người chơi mang sẵn pha Trắng bước thẳng qua Cửa Trắng 1.
2. **Phân đoạn 2 — Rào cản Xám & Cửa Đen 1 ($X = 291 \to 1597$):** Gặp vách Xám tại $X=460$ (đổi pha Xám), sau đó gặp Cửa Đen 1 tại $X=944$ (đổi pha Đen). Sàn trũng Xám đón rơi tại CP2.
3. **Phân đoạn 3 — Bậc nhảy đón Quang Điểm 1 ($X = 1597 \to 2588$):** Nhảy lên sàn Trắng tại $X=1597$, thu thập `item_mvnivkti_1` tại $X=1736, Y=844$. Tiến lên thềm cao $X=2043$ và vượt Cửa Đen 3 ($X=2375$).
4. **Phân đoạn 4 — Thử thách Quang Điểm 2 & Cửa Trắng 4 ($X = 2588 \to 4286$):** Nhịp chạy ba màu Trắng-Xám-Đen. Nhảy bật cao vượt Cửa Trắng 4 tại $X=3630$ để nhặt `item_mvnivvcv_2` trên không tại $X=3068, Y=661$.
5. **Phân đoạn 5 — Tiếp cận Trạm đo ($X = 4286 \to 5600+$):** Bước lên sàn FIXED `item_mvhtg97m_h` tại $X=4286$, kích hoạt trạng thái tiếp cận Buồng đo Vòng tròn Newton.

### E. Giai đoạn 1 — Gameplay Vượt Chướng Ngại Vật (Stage 1 — Platforming Gameplay)
1. **Xuất phát:** Người chơi sinh ra tại CP0 ($X=-571, Y_{\text{feet}}=972$) với pha Sáng Trắng ($\pi/2$).
2. **Hành động:** 
   - Đi sang phải, bước xuyên qua rào cản Trắng đầu tiên mà không bị cản trở nhờ cùng pha.
   - Nhấn `Q`/`E` chuyển sang pha Xám ($0$) để lọt qua rào cản Xám tại $X=460$.
   - Nhấn tiếp sang pha Đen ($-\pi/2$) để đi qua Cửa Đen tại $X=944$. Lưu ý: khi ở pha Đen, sàn Đen sẽ trở nên xuyên thấu, do đó người chơi phải nhảy hoặc chuyển pha linh hoạt để không rơi xuống hố tử thần.
   - Kích hoạt lần lượt các Checkpoint CP1 ($X=153$), CP2 ($X=1379$), CP3 ($X=2098$), CP4 ($X=2800$).
   - Thu thập 2 Quang Điểm lơ lửng trên đường đi.
3. **Chuyển tiếp:** Khi vượt qua mốc $X > 4286$ và bước lên khối FIXED `item_mvhtg97m_h`, nhân vật tiến vào vùng ảnh hưởng của Buồng đo Newton ($X > c_x - \text{ringScale} \times 4.25 = 5135 - 935 = 4200$), tự động chuyển sang Giai đoạn 2.

### F. Giai đoạn 2 — Gameplay Đánh Dấu Bằng Phím X (Stage 2 — Marking Gameplay Using X)
- **Cách thức kích hoạt:** Khi người chơi tiến sát tọa độ tâm buồng đo $C_X = 5135, C_Y = 659$, cờ `newtonMeasurementStarted = true` được kích hoạt. Camera chuyển sang chế độ đóng khung rộng (framing zoom), hiển thị toàn bộ hệ thống vân tròn Newton đồng tâm.
- **Những gì hiển thị trên màn hình:**
  - Hệ thống các vòng tròn tối đồng tâm với tâm $O$ tại $(5135, 659)$.
  - Hai thanh bục di động xoay tròn (Orbit Rails): Thanh $K$ (đo vòng $k=6$, chiều rộng $136\text{ px}$, chu kỳ $10.8\text{ s}$) và Thanh $I$ (đo vòng $i=3$, chiều rộng $126\text{ px}$, chu kỳ $8.6\text{ s}$).
  - Bảng chỉ dẫn HUD: `"A / D DI CHUYỂN · SPACE / W NHẢY · Q / E ĐỔI PHA · X ĐÁNH DẤU"`.
- **Thao tác đánh dấu bằng phím X (The X-Key Marking Mechanic):**
  - **Dấu 1 (Khóa điểm K):** Người chơi nhảy lên thanh bục xoay $K$. Người chơi có toàn quyền chọn thời điểm: bất kỳ lúc nào nhấn **phím X**, vị trí tức thời của thanh $K$ sẽ được chốt làm điểm $K$. Điểm này thiết lập vĩnh viễn trục đường kính chuẩn $K - O - K'$ đi qua tâm buồng đo.
    - *Phản hồi:* Âm thanh khóa vang lên (`measurementLockSound(0)`), màn hình chớp sáng, HUD hiển thị: `"K ĐÃ KHÓA — TRỤC K-O ĐƯỢC XÁC LẬP"`.
  - **Dấu 2 (Khóa điểm I):** Người chơi phải nhảy sang thanh bục xoay $I$ (vòng trong $i=3$). Thanh $I$ quay liên tục. Người chơi quan sát và chờ cho thanh $I$ xoay đến vị trí thẳng hàng với trục đường kính $K-O$. Khi thẳng hàng, nhấn **phím X**.
    - *Kiểm tra dung sai:* Hệ thống tính khoảng cách lỗi giữa tâm thanh $I$ và điểm trục mục tiêu. Dung sai cho phép là $48\text{ px}$.
    - *Phản hồi thành công:* Nếu trong dung sai, điểm $I$ được khóa cố định. HUD hiển thị độ chính xác (ví dụ: `"I ĐÃ KHÓA — 94%"`).
    - *Phản hồi trượt:* Nếu nhấn sai thời điểm, HUD báo `"CHỜ I THẲNG HÀNG VỚI TRỤC K-O"`, âm thanh báo lỗi vang lên, tăng biến đếm trượt.
  - **Dấu 3 (Khóa điểm K'):** Người chơi quay trở lại thanh $K$. Khi thanh $K$ xoay sang nửa bên kia của đường tròn và thẳng hàng với trục $K-O$ đối diện điểm $K$, người chơi nhấn **phím X**. Dung sai cho phép là $54\text{ px}$.
    - *Phản hồi:* HUD hiển thị `"K' ĐÃ KHÓA"`. Hoàn tất trọn vẹn 3 điểm đánh dấu ($K, I, K'$).
- **Lưu ý xác minh về phím bấm:** Trong mã nguồn dòng 1602–1603 ghi rõ: `SPACE/W are jump only. X is exclusively the Newton mark/shot input.` — Phím Space và W tuyệt đối chỉ dùng để nhảy; phím **X** là phím duy nhất dùng để đánh dấu.
- **Kết thúc thắng lợi:** Khi đủ 3 điểm, trò chơi chuyển sang trạng thái `'NEWTON_REVEAL'`, vẽ đường đo nối các điểm trong $3.2\text{ giây}$, sau đó kích hoạt màn hình chiến thắng `quangShowComplete(0)` và mở khóa màn L02.

### G. Điều kiện Thắng và Thất bại (Victory and Failure)
- **Thắng:** Khóa thành công 3 điểm $K, I, K'$ bằng phím X tại Buồng đo Newton.
- **Thất bại:** Rơi ra ngoài biên bản đồ thế giới ($Y > 1693$ hoặc hố $Y > 1080$). Hồi sinh tại checkpoint kích hoạt gần nhất; không bị mất Quang Điểm đã thu thập.

### H. Bằng chứng mã nguồn (Source Evidence)
- Khai báo màn chơi và danh sách khối: Dòng 25 (`window.__CE_LEVELS__[0]`).
- Thiết lập địa hình và ray đo: `setupEditorEasyLevel()` dòng 324–412.
- Xử lý phím X và nhịp đo: Dòng 1599, dòng 1192–1244 (`fireEasyNewtonMeasurementPulse`).
- Hiển thị kết quả và mở khóa màn: Dòng 1167–1175, dòng 2320–2346, dòng 2864–3006.

---

## 3. PHÂN TÍCH CHI TIẾT MÀN L02: "CẦU THANG MA"

### A. Mô tả chung (General Description)
- **Level ID:** `L02`
- **Tên hiển thị:** `"Cầu thang ma"` (*Ghost Staircase*)
- **Mục tiêu gameplay:** Thử thách kỹ năng nhảy bậc cao dốc đứng và chuyển đổi pha liên tục trên không trung giữa các bục bóng mờ.
- **Độ khó:** Trung bình (Skill Jump / Rhythm Phase Swapping).
- **Chỉ dẫn gốc trong mã nguồn HTML:**  
  > *"Sử dụng các bục bóng mờ (ghost platforms) và bậc thang nhảy; đổi pha liên tục để bước lên cầu thang không trọng lực."*

### B. Thống kê khối địa hình (Block Statistics)
- **Tổng số khối địa hình (`terrain`):** **15 khối** (Được định nghĩa tại `window.__CE_LEVELS__[1].terrain`).
  - Khối màu **ĐEN (BLACK):** **8 khối** (Các bục chạy, bục thoát, và vách ngăn).
  - Khối màu **TRẮNG (WHITE):** **3 khối** (Bục ma dưới gầm, bậc thang đỉnh dốc, và thềm đón bí mật).
  - Khối màu **XÁM (GRAY):** **2 khối** (Bậc thang dốc trung gian và thềm nóc ngách cao).
  - Khối đặc biệt **CỐ ĐỊNH (FIXED):** **2 khối** (Cột trụ treo và thềm tiếp cận trạm đo).
- **Phân loại động học:**
  - Khối tĩnh (Static): **15 khối** (100% địa hình trong màn L02 là tĩnh).
  - Khối di động (Moving): **0 khối**.
  - Bục bóng mờ (Ghost Platforms): **2 bục đặt tên chỉ định** (`L02_B_GHOST_WHITE`, `L02_C_GHOST_WHITE`) kết hợp cấu trúc bậc đan xen.
  - Khối bẫy sát thương (Hazards): **0 khối**.

### C. Danh mục khối hoàn chỉnh (Complete Block Inventory)

| STT | Object ID | Loại hình | Màu sắc | X (px) | Y (px) | Rộng (W) | Cao (H) | Động học | Quy tắc va chạm thực tế trong gameplay |
|:---:|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 1 | `L02_A_START_BLACK` | Sàn xuất phát | BLACK | 0 | 940 | 720 | 100 | Static | Rắn khi Trắng/Xám. Đỡ chân người chơi tại điểm xuất phát CP0. |
| 2 | `L02_A_EXIT_BLACK` | Sàn tầng 1 trên | BLACK | 810 | 685 | 456 | 54 | Static | Rắn khi Trắng. Tạo bậc nhảy nâng độ cao đầu tiên. |
| 3 | `L02_B_GHOST_WHITE` | Bục ma dưới sàn | WHITE | 810 | 828 | 456 | 76 | Static | Rắn khi Đen. Nằm ngay dưới `L02_A_EXIT_BLACK`. Nếu người chơi ở pha Trắng, bục này mờ đi (ghost); đổi sang Đen sẽ đứng vững trên nó. |
| 4 | `L02_B_EXIT_BLACK` | Bậc sàn tầng 2 | BLACK | 1433 | 565 | 455 | 46 | Static | Rắn khi Trắng. Vị trí đặt Checkpoint CP2. |
| 5 | `L02_STAIR_GRAY` | Bậc dốc Xám | GRAY | 1930 | 447 | 230 | 48 | Static | Rắn khi Trắng/Đen. Bậc chuyển tiếp màu trung tính leo lên đỉnh. |
| 6 | `L02_STAIR_BLACK` | Bậc dốc Đen | BLACK | 2160 | 355 | 175 | 46 | Static | Rắn khi Trắng. Đòi hỏi giữ pha Trắng để lấy đà bật nhảy. |
| 7 | `item_mvjq7ws3_1` | Bậc Trắng đỉnh dốc | WHITE | 2255 | 234 | 337 | 46 | Static | Rắn khi Đen. Người chơi phải đổi pha sang Đen giữa không trung để đáp lên bậc này. |
| 8 | `item_mvnkig9d_2` | Thềm Xám trần cao | GRAY | 2294 | 78 | 298 | 48 | Static | Rắn khi Trắng/Đen. Thềm ẩn trên cao dành cho người chơi nhảy bật tối đa. |
| 9 | `item_mvjqox0s_2` | Cột trụ treo | FIXED | 2642 | -24 | 75 | 222 | Static | **RẮN TUYỆT ĐỐI**. Trụ cản từ trần rủ xuống, phân cách khu vực đỉnh. |
| 10 | `L02_C_GHOST_WHITE` | Bục ma tầng cao | BLACK | 2780 | 237 | 287 | 43 | Static | Rắn khi Trắng. Trong JSON mang vật liệu BLACK; kết hợp với bục Trắng tạo cảm giác bậc thang ma. |
| 11 | `L02_TOP_BLACK_RUN` | Sàn chạy đỉnh | BLACK | 3130 | 237 | 620 | 43 | Static | Rắn khi Trắng. Đường chạy trên đỉnh dẫn qua khu vực vực thẳm. |
| 12 | `L02_SECRET_WHITE_CATCH`| Thềm đón bí mật | WHITE | 3319 | 401 | 300 | 46 | Static | Rắn khi Đen. Thềm giấu dưới gầm, đón người chơi khi rơi xuống nhặt `L02_QD02_SECRET_UNDER_BLACK`. |
| 13 | `L02_SAME_COLOR_WALL` | Vách cản cùng màu | BLACK | 3694 | -24 | 56 | 261 | Static | Tường cản Đen chắn ngang đường chạy đỉnh. Phải đổi sang pha Đen để đi xuyên qua. |
| 14 | `L02_ESCAPE_WHITE` | Thềm thoát | BLACK | 3807 | 237 | 148 | 43 | Static | Rắn khi Trắng. Bệ đón chân ngay sau khi xuyên qua vách `L02_SAME_COLOR_WALL`. |
| 15 | `item_mvnkc26j_1` | Thềm đón trạm đo | FIXED | 4265 | 591 | 125 | 52 | Static | **RẮN TUYỆT ĐỐI**. Bệ đáp hạ độ cao cuối cùng trước khi vào buồng Newton. |

### D. Cấu trúc bản đồ (Map Structure)
Bản đồ L02 có kích thước $W = 6144\text{ px}, H = 1562\text{ px}$. Bản đồ tập trung vào độ dốc đi lên cao và nhảy qua khe hẹp:
1. **Phân đoạn 1 — Cầu thang ma tầng thấp ($X = 0 \to 1433$):** Xuất phát tại CP0 ($X=110$). Bục Đen `L02_A_EXIT_BLACK` và bục Trắng `L02_B_GHOST_WHITE` nằm chồng lớp lên nhau, tạo thử thách chọn pha để leo lên sàn tầng 2.
2. **Phân đoạn 2 — Dãy bậc thang dốc đan xen ($X = 1433 \to 2600$):** Bậc dốc dâng cao từ $Y=565$ lên $Y=234$. Người chơi phải nhảy liên tiếp: Đen $\to$ Xám $\to$ Đen $\to$ Trắng, đổi pha liên tục trên không.
3. **Phân đoạn 3 — Hốc bí mật & Đường chạy đỉnh ($X = 2600 \to 3700$):** Tại $X=3450$, người chơi có thể thả rơi xuống thềm Trắng `L02_SECRET_WHITE_CATCH` tại $Y=401$ để nhặt Quang Điểm 2, sau đó nhảy bật ngược lại lên đường chạy đỉnh.
4. **Phân đoạn 4 — Vách cản cùng màu & Lối thoát ($X = 3700 \to 4265$):** Đang chạy trên sàn Đen, người chơi gặp bức tường Đen `L02_SAME_COLOR_WALL` chắn ngang. Phải đổi sang pha Đen để lọt qua vách, rồi lập tức đổi pha Trắng để đáp lên thềm `L02_ESCAPE_WHITE`.
5. **Phân đoạn 5 — Thềm FIXED & Buồng đo Newton ($X = 4265 \to 5400+$):** Thả rơi từ độ cao $Y=237$ xuống thềm FIXED $Y=591$ và tiến vào buồng đo $C_X=5110, C_Y=528$.

### E. Giai đoạn 1 — Gameplay Vượt Chướng Ngại Vật (Stage 1 — Platforming Gameplay)
1. **Xuất phát:** Người chơi sinh ra tại CP0 ($X=110, Y_{\text{feet}}=940$) với pha Xám ban đầu ($\phi = 0$).
2. **Hành động:**
   - Chuyển sang pha Trắng để đứng vững trên bục Đen xuất phát, nhảy lên bậc `L02_A_EXIT_BLACK`.
   - Vượt qua chuỗi bậc thang ma: Nhảy khỏi bục Đen, đổi sang pha Đen trên không để bục Trắng `item_mvjq7ws3_1` đón chân.
   - Nhặt Quang Điểm 1 (`item_mvni3tly_1`) lơ lửng trên bục tại $X=1042, Y=554$.
   - Kích hoạt CP1 ($X=1026$), CP2 ($X=1545$), CP3 ($X=2440$), CP4 ($X=3542$).
   - Khám phá ngách bí mật dưới sàn đỉnh: rơi xuống $Y=345$ để nhặt `L02_QD02_SECRET_UNDER_BLACK`.
   - Đối mặt với bẫy tâm lý tại $X=3694$: Bức tường Đen cùng màu với sàn. Người chơi phải tin tưởng vào cơ chế giao thoa — chuyển sang pha Đen để xuyên qua tường mà không sợ rơi xuống vực nếu căn nhịp chuẩn.
3. **Chuyển tiếp:** Tiếp cận thềm FIXED `item_mvnkc26j_1` tại $X=4265$ và bước vào quỹ đạo Buồng Newton tại $X \approx 4500$.

### F. Giai đoạn 2 — Gameplay Đánh Dấu Bằng Phím X (Stage 2 — Marking Gameplay Using X)
- **Điểm khác biệt quan trọng của L02 (`orbitStart: "dock"`):** Trong màn L02, buồng đo cấu hình chế độ `orbitStart: "dock"` (không tự động hút vào quỹ đạo như L01). Người chơi phải chủ động nhảy neo vào thanh bục đo mới bắt đầu quá trình đo.
- **Thông số thanh quét:**
  - Bậc vòng đo: Vòng trong $i=2$, Vòng ngoài $k=4$.
  - Thanh $K$ ($k=4$): Bề rộng $150\text{ px}$, chu kỳ xoay $T=11.2\text{ s}$, góc xuất phát $192^\circ$.
  - Thanh $I$ ($i=2$): Bề rộng $140\text{ px}$, chu kỳ xoay $T=10.5\text{ s}$, góc xuất phát $155^\circ$.
- **Thao tác đánh dấu bằng phím X:**
  - **Dấu 1 (Khóa K):** Nhảy lên thanh $K$, căn góc xoay mong muốn và nhấn **phím X**. Trục đường kính $K-O-K'$ được xác lập. Âm thanh chốt phát ra, HUD báo: `"K ĐÃ KHÓA"`.
  - **Dấu 2 (Khóa I):** Nhảy sang thanh $I$. Quan sát thanh $I$ xoay quanh vòng $i=2$. Khi thanh $I$ đi vào vị trí thẳng hàng với trục đường kính đã chọn, nhấn **phím X**. Dung sai cho phép là $48\text{ px}$. HUD báo: `"I ĐÃ KHÓA — [Độ chính xác]%"`.
  - **Dấu 3 (Khóa K'):** Nhảy trở lại thanh $K$. Khi thanh $K$ xoay sang nửa đối diện thẳng hàng với trục $K-O$, nhấn **phím X** (dung sai $54\text{ px}$). Hoàn thành trọn vẹn 3 điểm đo.
- **Kết thúc thắng lợi:** Màn hình chuyển sang trạng thái `'NEWTON_REVEAL'`, kích hoạt `finishComplete()`, mở modal hoàn thành màn L02 và cho phép tiến vào L03.

### G. Điều kiện Thắng và Thất bại (Victory and Failure)
- **Thắng:** Khóa đủ 3 điểm $K, I, K'$ bằng phím X trong buồng đo Newton.
- **Thất bại:** Rơi ra ngoài biên bản đồ ($Y > 1562$). Hồi sinh tại Checkpoint gần nhất; giữ nguyên trạng thái Quang Điểm đã nhặt.

### H. Bằng chứng mã nguồn (Source Evidence)
- Khai báo màn chơi và khối: Dòng 25 (`window.__CE_LEVELS__[1]`).
- Cấu hình buồng đo Newton L02: `cx: 5110, cy: 528, orbitStart: "dock", i: 2, k: 4`.

---

## 4. PHÂN TÍCH CHI TIẾT MÀN L03: "GIẾNG BA TẦNG"

### A. Mô tả chung (General Description)
- **Level ID:** `L03`
- **Tên hiển thị:** `"Giếng ba tầng"` (*Three-Tier Well*)
- **Mục tiêu gameplay:** Thử thách tối thượng kết hợp độ sâu thẳng đứng gấp 2 lần, rơi tự do có điều khiển, căn nhịp 3 nền tảng di động dao động điều hòa 2 chiều và né tránh cơ chế ép chết.
- **Độ khó:** Nâng cao / Khó (Advanced Gauntlet).
- **Chỉ dẫn gốc trong mã nguồn HTML:**  
  > *"Khám phá độ sâu thẳng đứng; nhảy rơi tự do có tính toán và căn nhịp các bục nâng di động để tới buồng đo cuối."*

### B. Thống kê khối địa hình (Block Statistics)
- **Tổng số khối địa hình (`terrain`):** **27 khối** (Được định nghĩa tại `window.__CE_LEVELS__[2].terrain`).
  - Khối màu **TRẮNG (WHITE):** **5 khối** (4 khối tĩnh, **1 bục di động 2D**).
  - Khối màu **XÁM (GRAY):** **8 khối** (Các sàn chiếu nghỉ, vách ngăn và thềm đón vực sâu).
  - Khối màu **ĐEN (BLACK):** **5 khối** (2 sàn tĩnh, 1 vách ngăn, **2 bục di động**).
  - Khối đặc biệt **CỐ ĐỊNH (FIXED):** **9 khối** (Thành giếng trái/phải, trần/sàn/tường hốc bí mật, trụ cản trung tâm và thềm đón buồng đo).
- **Phân loại động học:**
  - Khối tĩnh (Static): **24 khối**.
  - **Khối di động (Moving Platforms):** **3 khối** (100% bục di động của trò chơi nằm ở màn L03).
  - Bục bóng mờ (Ghost Platforms): Áp dụng trên toàn bộ các vách ngăn đa màu ở tầng đáy.
  - Nguy cơ ép chết (Crush Hazard): Tích hợp trực tiếp trên 3 bục di động thông qua hàm `editorResolveMovingSolids`.

### C. Danh mục khối hoàn chỉnh (Complete Block Inventory)

| STT | Object ID | Loại hình | Màu sắc | X (px) | Y (px) | Rộng (W) | Cao (H) | Động học & Thông số chuyển động | Quy tắc va chạm thực tế trong gameplay |
|:---:|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 1 | `L03_A_START_WHITE` | Sàn xuất phát | WHITE | 0 | 310 | 520 | 52 | Static | Rắn khi Đen. Sàn đón tại đỉnh giếng. |
| 2 | `item_mvnnoz92_4` | Vách giếng trái | FIXED | 0 | 34 | 53 | 276 | Static | **RẮN TUYỆT ĐỐI**. Thành giếng bảo vệ mép trái. |
| 3 | `item_mvnnpyy9_5` | Vách giếng phải | FIXED | 467 | 34 | 53 | 276 | Static | **RẮN TUYỆT ĐỐI**. Thành giếng dẫn hướng rơi mép phải. |
| 4 | `item_mvnnogav_3` | Bậc ngăn giếng | BLACK | 0 | 787 | 520 | 62 | Static | Rắn khi Trắng. Bậc cản giữa giếng tầng 1 (đặt Checkpoint CP1). |
| 5 | `L03_A_LAND_GRAY` | Chiếu nghỉ giếng | GRAY | 0 | 1265 | 520 | 34 | Static | Rắn khi Trắng/Đen. Đáy hố rơi tự do tầng 1. |
| 6 | `L03_B_GRAY_ALCOVE` | Sàn hốc ngách | GRAY | 0 | 1430 | 520 | 52 | Static | Rắn khi Trắng/Đen. Lối dẫn vào ngách bí mật chứa `QD01`. |
| 7 | `L03_B_ALCOVE_ROOF` | Trần hốc ngách | FIXED | 520 | 1265 | 300 | 34 | Static | **RẮN TUYỆT ĐỐI**. Trần hốc đá che chắn. |
| 8 | `L03_B_ALCOVE_FLOOR`| Sàn hốc ngách | FIXED | 520 | 1430 | 300 | 52 | Static | **RẮN TUYỆT ĐỐI**. Sàn hốc đá (đặt Checkpoint CP2). |
| 9 | `L03_B_ALCOVE_END` | Vách chặn hốc | FIXED | 820 | 1265 | 44 | 217 | Static | **RẮN TUYỆT ĐỐI**. Bức tường cụt chặn cuối ngách. |
| 10 | `item_mvnnvh3q_6` | Bậc Trắng tầng 2 | WHITE | 985 | 1265 | 331 | 49 | Static | Rắn khi Đen. Bậc thềm trên cao tầng 2. |
| 11 | `item_mvnnwc4e_7` | Bậc Xám tầng 2 | GRAY | 985 | 1434 | 331 | 48 | Static | Rắn khi Trắng/Đen. Bậc chuyển tiếp tầng 2. |
| 12 | `item_mvnocock_8` | Bậc Trắng đón rơi| WHITE | 985 | 1842 | 331 | 48 | Static | Rắn khi Đen. Sàn đón chân dưới bục di động 1 (đặt CP3). |
| 13 | **`item_mvnodpwx_a`**| **Bục di động 1 (Thang máy đứng)** | **BLACK** | **985** | **1714** | **331** | **43** | **MOVING**<br>$dx=0, dy=-127$<br>$T=4.0\text{ s}, \phi=0$<br>`style: "smooth"` | Rắn khi Trắng. Dao động thẳng đứng nâng hạ giữa $Y=1714$ và $Y=1587$. Cõng người chơi lên cao; nếu ép người chơi vào trần sẽ gây chết kẹp (`crush`). |
| 14 | `item_mvnongn3_d` | Trụ cản trái | FIXED | 1573 | 1445 | 69 | 269 | Static | **RẮN TUYỆT ĐỐI**. Vách đá cố định phân tầng. |
| 15 | `item_mvnon53h_c` | Trụ cản phải | FIXED | 1835 | 1445 | 66 | 269 | Static | **RẮN TUYỆT ĐỐI**. Vách đá cố định kẹp lối đi (đặt CP4). |
| 16 | `item_mvnoma2x_b` | Thềm Xám tầng 2 | GRAY | 1573 | 1714 | 328 | 56 | Static | Rắn khi Trắng/Đen. Thềm bước sau bục di động 1. |
| 17 | `item_mvnoplkv_f` | Thềm treo lửng | GRAY | 1773 | 1281 | 511 | 48 | Static | Rắn khi Trắng/Đen. Thềm trên cao tầng 2. |
| 18 | `item_mvnoyg6s_i` | Thềm Trắng đáy | WHITE | 2063 | 1890 | 286 | 56 | Static | Rắn khi Đen. Bắt đầu chuỗi sàn ba màu tầng đáy. |
| 19 | `item_mvnozyzu_j` | Thềm Xám đáy | GRAY | 2349 | 1890 | 276 | 56 | Static | Rắn khi Trắng/Đen. Thềm giữa tầng đáy. |
| 20 | `item_mvnp04d2_k` | Thềm Đen đáy | BLACK | 2625 | 1890 | 296 | 56 | Static | Rắn khi Trắng. Thềm tiếp cận bục di động 3. |
| 21 | `item_mvnp12mm_l` | Vách ngăn Xám | GRAY | 2291 | 1663 | 58 | 227 | Static | Tường cản Xám thẳng đứng cắm xuống sàn đáy. |
| 22 | `item_mvnp1axm_m` | Vách ngăn Đen | BLACK | 2570 | 1663 | 55 | 227 | Static | Tường cản Đen thẳng đứng. Phải đổi pha Đen để lọt qua. |
| 23 | `item_mvnp9eab_n` | Vách ngăn FIXED | FIXED | 2859 | 1663 | 62 | 227 | Static | **RẮN TUYỆT ĐỐI**. Trụ cản không thể xuyên thấu. |
| 24 | **`item_mvnp9r6r_o`**| **Bục di động 3 (Bục chéo lớn)** | **BLACK** | **2702** | **2113** | **219** | **49** | **MOVING**<br>$dx=461, dy=676$<br>$T=5.0\text{ s}, \phi=0$<br>`style: "smooth"` | Rắn khi Trắng. Di chuyển chéo biên độ khổng lồ xuyên tầng từ $(2702, 2113)$ tới $(3163, 2789)$. Đưa người chơi qua vực thẳm lớn. |
| 25 | **`item_mvnoqbwi_g`**| **Bục di động 2 (Bục nâng chéo)** | **WHITE** | **3459** | **2479** | **224** | **49** | **MOVING**<br>$dx=213, dy=-258$<br>$T=5.0\text{ s}, \phi=0$<br>`style: "smooth"` | Rắn khi Đen. Dao động chéo đưa người chơi từ sàn vực $Y=2479$ lên thềm trạm đo $Y=2221$. |
| 26 | `item_mvnpcctq_p` | Sàn đón vực sâu | GRAY | 3459 | 2650 | 224 | 46 | Static | Rắn khi Trắng/Đen. Sàn đón khẩn cấp dưới bục 2 (đặt CP7). |
| 27 | `item_mvnpp93j_q` | Thềm trạm đo | FIXED | 3989 | 2100 | 438 | 62 | Static | **RẮN TUYỆT ĐỐI**. Thềm đá cố định dẫn vào buồng đo (đặt CP8). |

### D. Cấu trúc bản đồ (Map Structure)
Bản đồ L03 có kích thước khổng lồ $W = 6235\text{ px}, H = 3238\text{ px}$ (sâu gấp 2 lần các màn trước). Bản đồ tổ chức thành cấu trúc giếng 3 tầng:
1. **Phân đoạn 1 — Giếng rơi tự do tầng 1 ($X = 0 \to 520, Y = 34 \to 1265$):** Thả rơi thẳng đứng từ độ cao $Y=310$ qua khoảng trống giếng sâu gần $1000\text{ px}$, lách qua bậc ngăn `item_mvnnogav_3` tại $Y=787$ để đáp xuống chiếu nghỉ $Y=1265$.
2. **Phân đoạn 2 — Hốc ngách bí mật tầng 1 ($X = 0 \to 864, Y = 1265 \to 1482$):** Đường ngách rẽ nhánh sang phải chứa Quang Điểm 1 (`L03_QD01_NGACH_GRAY`) được bảo vệ bởi các khối FIXED.
3. **Phân đoạn 3 — Thang máy thẳng đứng & Tầng trung gian ($X = 985 \to 1900, Y = 1265 \to 1842$):** Nhảy lên Bục di động 1 `item_mvnodpwx_a`. Căn nhịp nâng hạ $4.0\text{ s}$ để thu thập Quang Điểm 2 (`item_mvnod5fd_9` tại $X=1139, Y=1797$) và vượt qua khe hẹp giữa hai trụ FIXED `item_mvnongn3_d` và `item_mvnon53h_c`.
4. **Phân đoạn 4 — Vách cản đa màu & Bục chéo biên độ lớn ($X = 2063 \to 3200, Y = 1663 \to 2789$):** Vượt qua 3 vách cản Trắng-Xám-Đen, nhặt Quang Điểm 3 (`item_mvnopbeo_e` tại $X=2158, Y=1540$). Đón Bục di động 3 `item_mvnp9r6r_o` để lướt chéo một quãng đường $461\text{ px}$ ngang và $676\text{ px}$ dọc qua vực thẳm.
5. **Phân đoạn 5 — Bục nâng chéo 2 & Buồng đo đáy giếng ($X = 3459 \to 5500+, Y = 2100 \to 2650$):** Chuyển sang Bục di động 2 `item_mvnoqbwi_g` mang màu Trắng (người chơi phải giữ pha Đen để đứng vững). Bục nâng đưa người chơi lên thềm FIXED `item_mvnpp93j_q` tại $Y=2100$, dẫn thẳng vào buồng Newton đặt tại $C_X=5201, C_Y=2100$.

### E. Giai đoạn 1 — Gameplay Vượt Chướng Ngại Vật (Stage 1 — Platforming Gameplay)
1. **Xuất phát:** Người chơi xuất hiện tại CP0 ($X=245, Y_{\text{feet}}=262$) trên đỉnh miệng giếng.
2. **Hành động:**
   - Bước ra mép giếng và thả rơi tự do. Điều khiển hướng rơi sang trái/phải để không bị va chạm ngoài ý muốn với các vách ngăn.
   - Ghé vào hốc ngách an toàn tại $Y=1430$ kích hoạt CP2 và nhặt Quang Điểm 1.
   - Tiếp cận giếng thang máy tầng 2: Bước lên bục di động Đen `item_mvnodpwx_a`. Cần chú ý giữ pha Trắng để đứng vững; nếu đổi nhầm sang pha Đen trên bục, người chơi sẽ bị tụt xuyên qua bục rơi xuống đáy hố.
   - Nhảy vượt qua kẽ hẹp giữa hai trụ FIXED, kích hoạt CP4 và CP5.
   - Băng qua chuỗi vách cản đa màu tại $X=2291 \to 2859$, đổi pha tương ứng để xuyên qua từng vách.
   - Nhảy đón bục di động chéo `item_mvnp9r6r_o` lúc bục chạm điểm thấp nhất, đứng vững trong suốt hành trình bay chéo qua vực thẳm.
   - Chuyển tiếp sang bục nâng chéo Trắng `item_mvnoqbwi_g` (chuyển sang pha Đen), để bục đưa lên thềm trạm đo.
3. **Chuyển tiếp:** Bước chân lên thềm đá FIXED `item_mvnpp93j_q` tại $X=3989, Y=2100$, kích hoạt Checkpoint cuối CP8 ($X=4326$), tiến vào buồng Newton đáy giếng.

### F. Giai đoạn 2 — Gameplay Đánh Dấu Bằng Phím X (Stage 2 — Marking Gameplay Using X)
- **Đặc điểm buồng đo L03:**
  - Tọa độ tâm: $C_X = 5201, C_Y = 2100$. Chế độ vào tự động (`orbitStart: "auto"`).
  - Bậc vòng đo: Vòng trong $i=2$, Vòng ngoài $k=7$ (Bậc vòng $k=7$ tạo độ mở bán kính lớn nhất trong toàn bộ trò chơi, tốc độ quét tương đối ở mép ngoài là cao nhất).
  - Thanh $K$ ($k=7$): Bề rộng $150\text{ px}$, chu kỳ xoay $T=11.2\text{ s}$, hướng quay ngược chiều kim đồng hồ (`direction: -1`).
  - Thanh $I$ ($i=2$): Bề rộng $140\text{ px}$, chu kỳ xoay $T=10.5\text{ s}$, hướng quay thuận chiều kim đồng hồ (`direction: 1`).
- **Thao tác đánh dấu bằng phím X:**
  - **Dấu 1 (Khóa K):** Nhảy lên thanh bục xoay ngoài $K$. Khi cảm thấy góc quay thuận lợi, nhấn **phím X**. Điểm $K$ được khóa, thiết lập vĩnh viễn trục đường kính chuẩn $K-O-K'$. HUD báo: `"K ĐÃ KHÓA — TRỤC K-O ĐƯỢC XÁC LẬP"`.
  - **Dấu 2 (Khóa I):** Chuyển sang thanh bục xoay trong $I$. Chờ hai thanh xoay ngược chiều nhau cho đến khi thanh $I$ trùng với trục đường kính $K-O$, nhấn **phím X**. Dung sai cho phép là $48\text{ px}$. HUD báo: `"I ĐÃ KHÓA — [Độ chính xác]%"`.
  - **Dấu 3 (Khóa K'):** Nhảy trở lại thanh $K$. Đợi thanh $K$ xoay sang nửa đối diện thẳng hàng với trục đường kính $K-O$, nhấn **phím X** (dung sai $54\text{ px}$).
- **Kết thúc thắng lợi:** Khi điểm $K'$ được chốt thành công, buồng đo kích hoạt hiệu ứng hoàn thành toàn phần `'NEWTON_REVEAL'`. Vì L03 là màn chơi cuối cùng của gói chiến dịch (`quangPackIndex = 2 === __QP_PACK__.length - 1`), hộp thoại chiến thắng chung cuộc hiển thị thông báo phá đảo toàn bộ trò chơi: `"ĐÃ HOÀN THÀNH TOÀN BỘ CHIẾN DỊCH CONTRAST"` cùng tổng số Quang Điểm đã nhặt trên cả 3 màn.

### G. Điều kiện Thắng và Thất bại (Victory and Failure)
- **Thắng:** Khóa thành công 3 điểm $K, I, K'$ bằng phím X tại Buồng đo đáy giếng.
- **Thất bại:** Rơi ra ngoài đáy vực thế giới ($Y > 3238$) hoặc bị bục di động ép vào trần cứng gây kẹp chết (`crush`). Hồi sinh tại Checkpoint gần nhất; không mất Quang Điểm.

### H. Bằng chứng mã nguồn (Source Evidence)
- Khai báo màn chơi và 27 khối: Dòng 25 (`window.__CE_LEVELS__[2]`).
- Động học 3 bục di động: Dòng 25 (trường `motion` của `item_mvnodpwx_a`, `item_mvnoqbwi_g`, `item_mvnp9r6r_o`) và dòng 916–960 (`editorResolveMovingSolids`).
- Xử lý hoàn thành toàn bộ gói màn chơi: Dòng 2864–3030 (`quangShowComplete`, `quangCounts`).

---

## 5. SO SÁNH KHỐI ĐỊA HÌNH LIÊN MÀN CHƠI (CROSS-LEVEL BLOCK COMPARISON)

### 5.1. Bảng đối chiếu số lượng khối giữa 3 màn chơi

| Tiêu chí thống kê | Màn L01 | Màn L02 | Màn L03 | Ghi chú kỹ thuật |
|:---|:---:|:---:|:---:|:---|
| **Tổng số khối địa hình (`terrain`)** | **16** | **15** | **27** | Dữ liệu gốc trong mảng `terrain` của từng màn |
| Khối màu TRẮNG (WHITE) | 6 | 3 | 5 | Khối pha dương ($I \ge 0.78$) |
| Khối màu XÁM (GRAY) | 4 | 2 | 8 | Khối vùng tinh chỉnh ($0.22 < I < 0.78$) |
| Khối màu ĐEN (BLACK) | 5 | 8 | 5 | Khối pha âm ($I \le 0.22$) |
| Khối CỐ ĐỊNH (FIXED) | 1 | 2 | 9 | Khối `alwaysSolid: true`, không đổi màu, không thể xuyên thấu |
| **Khối tĩnh (Static blocks)** | **16** | **15** | **24** | Khối cố định vị trí AABB |
| **Khối di động (Moving platforms)** | **0** | **0** | **3** | Có trường `motion` với $dx, dy, T, \text{phase}$ |
| Bục bóng mờ (Ghost platforms) | 0 | 2 | 0 | Đặt tên định danh GHOST trong L02 |
| Bẫy sát thương chủ động (Hazards) | 0 | 0 | 0 | Cả 3 màn không có bẫy cưa; nguy cơ tử vong đến từ rơi vực và ép chết |
| Số điểm Checkpoint | 6 | 5 | 9 | Các mốc lưu tọa độ và pha hồi sinh |
| Số Quang Điểm thu thập | 2 | 2 | 3 | Các vật phẩm sáng nhặt được lưu vào `localStorage` |

### 5.2. Nhận xét về sự phân bố và tiến trình thiết kế khối
- **Tỷ lệ khối FIXED tăng vọt ở L03:** L01 chỉ có 1 khối FIXED (làm sảnh đệm buồng đo). L02 có 2 khối. L03 có tới **9 khối FIXED** (chiếm 33% địa hình) đóng vai trò khung đỡ chịu lực cho giếng sâu và tạo trần cứng phục vụ cơ chế ép chết.
- **Sự chuyển dịch cơ chế động:** L01 và L02 hoàn toàn thuần túy là thử thách nhảy trên khối tĩnh và đổi màu. L03 là màn duy nhất tích hợp động học nền tảng di động mang theo người chơi.

---

## 6. SO SÁNH LUỒNG CHƠI LIÊN MÀN CHƠI (CROSS-LEVEL GAMEPLAY COMPARISON)

### 6.1. So sánh Giai đoạn 1 (Stage 1 — Platforming Challenges)
- **L01 (Cửa tương phản):** Đi ngang ($6169 \times 1693$). Chướng ngại vật là các bức tường đứng chắn ngang đường. Thao tác chính: đổi màu trước khi chạm tường để đi xuyên qua.
- **L02 (Cầu thang ma):** Dốc đứng lên cao ($6144 \times 1562$). Chướng ngại vật là khoảng cách giữa các bục nhảy hở chân. Thao tác chính: căn nhịp đổi màu giữa không trung (airborne swapping) để bục tiếp theo trở thành thể rắn đón chân.
- **L03 (Giếng ba tầng):** Rơi tự do & Vực sâu 2 chiều ($6235 \times 3238$). Chướng ngại vật là quán tính rơi tốc độ cao, các khe hẹp và bục di động dao động lệch pha. Thao tác chính: kết hợp điều khiển rơi tự do, đón bục nâng di động, và duy trì pha an toàn để không bị lọt sàn hoặc kẹp chết trần.

### 6.2. So sánh Giai đoạn 2 (Stage 2 — Marking Gameplay Using X)

| Đặc tính giai đoạn đo | Màn L01 | Màn L02 | Màn L03 |
|:---|:---:|:---:|:---:|
| **Tọa độ tâm buồng đo ($C_X, C_Y$)** | $(5135, 659)$ | $(5110, 528)$ | $(5201, 2100)$ |
| **Bậc vòng đo ($i / k$)** | $i=3, k=6$ | $i=2, k=4$ | $i=2, k=7$ (Khó nhất) |
| **Chế độ vào buồng (`orbitStart`)** | `auto` (Hút tự động) | `dock` (Neo thủ công) | `auto` (Hút tự động) |
| **Quy trình bấm phím X** | 3 lần: $K \to I \to K'$ | 3 lần: $K \to I \to K'$ | 3 lần: $K \to I \to K'$ |
| **Dung sai khóa điểm $I$ / $K'$** | $48\text{ px} / 54\text{ px}$ | $48\text{ px} / 54\text{ px}$ | $48\text{ px} / 54\text{ px}$ |
| **Tốc độ góc tương đối** | Trung bình | Chậm nhất (vòng nhỏ) | Nhanh nhất (vòng $k=7$ quét rộng) |
| **Hành động sau hoàn thành** | Mở khóa Màn L02 | Mở khóa Màn L03 | Phá đảo toàn bộ Chiến dịch Pack |

---

## 7. CÁC CÂU HỎI KỸ THUẬT CẦN XÁC NHẬN (QUESTIONS FOR JOINT REVIEW)

1. **Xác nhận ánh xạ phím X trên môi trường di động và tay cầm:**
   - Trong mã nguồn HTML máy tính, phím **X** là phím độc quyền dùng để bắn tia đo / đánh dấu (`pressed.has('KeyX')`), trong khi Space/W chỉ dùng để nhảy.
   - Khi chuyển sang Unity cho PC/Android: Chúng ta sẽ cấu hình một nút cảm ứng ảo riêng biệt mang nhãn `"MARK [X]"` xuất hiện khi bước vào Buồng Newton, hay sử dụng nút hành động đa năng (Contextual Action Button)?
2. **Quy chuẩn tọa độ rơi chết (`deathY`):**
   - Trong cả 3 màn L01, L02, L03, trường `deathY` trong JSON đều là `null` (mã nguồn tự tính rơi tự do theo chiều cao viewport/world).
   - Khi tạo Level JSON chuẩn cho Unity, nhóm phân tích đề xuất đặt giá trị số cứng:
     - L01: `deathY = 1750`
     - L02: `deathY = 1620`
     - L03: `deathY = 3300`  
     Đề nghị Trưởng dự án xác nhận quy chuẩn này.
3. **Phạm vi chuyển đổi 2 màn chơi Legacy (`setupEasyLevel` / `setupHardLevel`):**
   - Bản phân tích đã làm rõ rằng gói chiến dịch chính thức gồm **đúng 3 màn L01, L02, L03**. Hai màn thủ tục cũ không thuộc tuyến chơi chính. Đề nghị xác nhận chỉ đóng gói 3 màn L01, L02, L03 vào chiến dịch chính thức.

---

> **TÀI LIỆU HOÀN TẤT VÀ SẴN SÀNG CHO PHIÊN XEM XÉT DUYỆT (AWAITING USER REVIEW)**  
> *Cam kết: Không có mã Unity hay tệp Level JSON nào được tạo ra trong phiên phân tích này.*
