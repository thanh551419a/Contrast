# BÁO CÁO PHÁT HÀNH BẢN PORTABLE WINDOWS — CONTRAST
**PORTABLE WINDOWS BUILD & RELEASE VALIDATION REPORT**

---

## 1. THÔNG TIN PHÁT HÀNH (RELEASE METADATA)

| Thông số | Giá trị |
| :--- | :--- |
| **Thời điểm đóng gói (Build Timestamp)** | `2026-11-02 17:12:42 (UTC+7)` |
| **Phiên bản Unity (Unity Engine Version)** | `6000.5.8f1 (64-bit)` |
| **Nền tảng mục tiêu (Target Platform)** | Windows Standalone x64 (`Direct3D 11 / Direct3D 12`) |
| **Đường dẫn thư mục phân phối** | `Release/Windows/CONTRAST_Windows/` |
| **Tệp thực thi chính (Executable)** | `Release/Windows/CONTRAST_Windows/Game.exe` |
| **Đường dẫn tệp ZIP phân phối (Final Deliverable)** | `Release/Windows/CONTRAST_Windows_Portable.zip` |
| **Dung lượng tệp ZIP (Archive Size)** | `44,731,430 bytes` (~42.66 MB) |
| **Mã băm SHA-256 (Checksum)** | `DE8C9910C8966B82FD2F9D825FC1AB4AC5FDD2C6A06426B0287B9D5EB3FE8CCE` |

---

## 2. DANH MỤC TỆP TIN TRONG GÓI PORTABLE (PACKAGE FILE MANIFEST)

Gói ZIP `CONTRAST_Windows_Portable.zip` chứa đầy đủ runtime Unity độc lập, bảo toàn cấu trúc đường dẫn tương đối (200 tệp tin):

```
CONTRAST_Windows/
├── Game.exe                          # Tệp thực thi chính của trò chơi (647 KB)
├── UnityPlayer.dll                   # Unity Standalone Player runtime engine (27.7 MB)
├── DirectML.dll                      # DirectML GPU acceleration module (12.7 MB)
├── dstorage.dll                      # DirectStorage runtime API (165 KB)
├── dstoragecore.dll                  # DirectStorage core engine (1.3 MB)
├── UnityCrashHandler32.exe           # Unity crash reporting handler (1.5 MB)
├── README.txt                        # Hướng dẫn giải nén, điều khiển, phím tắt & lối chơi
├── D3D12/
│   └── D3D12Core.dll                 # DirectX 12 runtime component
├── MonoBleedingEdge/                 # Đầy đủ môi trường thực thi Mono / .NET Runtime
│   ├── EmbedRuntime/
│   └── etc/mono/
└── Game_Data/                        # Dữ liệu trò chơi, tài nguyên nhúng & script binaries
    ├── app.info                      # Định danh ứng dụng (Contrast / ContrastTeam)
    ├── boot.config                   # Cấu hình khởi động engine
    ├── globalgamemanagers            # Quản lý cấu hình toàn cục engine
    ├── level0                        # Scene chính đóng gói sẵn (SampleScene)
    ├── resources.assets              # TOÀN BỘ 3 MÀN CHƠI (L01, L02, L03) ĐÃ ĐƯỢC NHÚNG SẴN
    ├── sharedassets0.assets          # Tài nguyên chia sẻ của cảnh
    └── Managed/                      # Toàn bộ mã nguồn C# đã được biên dịch
        ├── Assembly-CSharp.dll       # Gameplay, GameManager, LevelLoader, UI, Newton Controllers
        ├── UnityEngine.dll
        ├── UnityEngine.UI.dll
        └── ...
```

---

## 3. XÁC MINH TÀI NGUYÊN MÀN CHƠI ĐƯỢC NHÚNG (BUILT-IN LEVELS VERIFICATION)

Trò chơi **KHÔNG** yêu cầu tải file ngoài, không yêu cầu kết nối internet và không phụ thuộc vào `AppData`:

1. **L01 — Cánh cửa không phải vật cản** (`Assets/Resources/Levels/L01.json`):
   - Đã được biên dịch trực tiếp vào `Game_Data/resources.assets`.
   - Nạp qua `Resources.Load<TextAsset>("Levels/L01")`.
   - Cơ chế phòng quang học Newton: Trục $K$ ($k=4$), $I$ ($i=2$), $K'$ ($k'=4$).
2. **L02 — Cầu thang ma** (`Assets/Resources/Levels/L02.json`):
   - Đã được biên dịch trực tiếp vào `Game_Data/resources.assets`.
   - Nạp qua `Resources.Load<TextAsset>("Levels/L02")`.
   - Cơ chế phòng quang học Newton: Trục $K$ ($k=5$), $I$ ($i=2$), $K'$ ($k'=5$).
3. **L03 — Giếng ba tầng** (`Assets/Resources/Levels/L03.json`):
   - Đã được biên dịch trực tiếp vào `Game_Data/resources.assets`.
   - Nạp qua `Resources.Load<TextAsset>("Levels/L03")`.
   - Cơ chế phòng quang học Newton: Trục $K$ ($k=7$), $I$ ($i=2$), $K'$ ($k'=7$).

---

## 4. PHÂN ĐỊNH 3 LUỒNG TẢI MÀN CHƠI (DETERMINISTIC LEVEL LOADING)

Tại Menu chính (`MainMenuUI`), 3 thẻ chức năng được phân tách rõ ràng và hỗ trợ điều khiển bằng cả chuột lẫn phím tắt:

1. **Thẻ 1: CHƠI THỬ (Phím [1] hoặc Chuột)**
   - Nạp trực tiếp từ tài nguyên đóng gói (`LevelSourceType.PackagedResource`).
   - Có độ ưu tiên tuyệt đối: Không đọc từ `AppData` hay thư mục phát triển; các file cũ trong `AppData` không bao giờ ghi đè được màn gốc.
   - Bắt đầu ngay lập tức tại L01.
2. **Thẻ 2: LOAD LEVEL (Phím [2] hoặc Chuột)**
   - Mở màn hình `LevelSelectUI` để chọn các file JSON tùy chỉnh được lưu trên đĩa trong thư mục `Levels/`.
   - Giữ nguyên toàn bộ dữ liệu người dùng, không xóa file.
3. **Thẻ 3: EDIT LEVEL (Phím [3] hoặc Chuột)**
   - Mở trình thiết kế màn chơi trực quan (`LevelEditorController`).

---

## 5. TIẾN TRÌNH CHIẾN DỊCH TỰ ĐỘNG (CAMPAIGN PROGRESSION)

- **L01 → L02**:
  - Người chơi hoàn thành chuỗi đo đạc quang học $K \to I \to K'$ tại phòng Newton của L01.
  - Sau thời gian hiển thị kết quả đo đạc ($t \ge 5.6s$), sự kiện `OnPlayerWon()` được kích hoạt.
  - `GameManager` hiển thị thông báo chuyển màn `"HOÀN THÀNH MÀN CHƠI! CHUẨN BỊ BƯỚC VÀO L02 — CẦU THANG MA..."` và tự động khởi tạo L02.
- **L02 → L03**:
  - Hoàn thành đo đạc Newton tại L02 -> tự động dọn dẹp thực thể, camera, checkpoint và nạp L03.
- **L03 → Hoàn thành Chiến dịch (Campaign Complete)**:
  - Hoàn thành đo đạc Newton tại L03 -> `GameManager` kích hoạt màn hình kết thúc chiến dịch (`GameOverUI.ShowCampaignComplete`).
  - Cung cấp 2 lựa chọn rõ ràng:
    - **PLAY AGAIN**: Chơi lại toàn bộ chiến dịch từ L01.
    - **MAIN MENU**: Quay trở lại Menu chính.

---

## 6. KẾT QUẢ KIỂM THỬ GIẢI NÉN ĐỘC LẬP (CLEAN-EXTRACTION TEST RESULTS)

Kiểm thử được thực hiện trên thư mục tạm cô lập bên ngoài workspace: `C:\Users\thanh\AppData\Local\Temp\ContrastPortableCleanTest`.

| Bước kiểm thử | Kết quả | Chi tiết chứng cứ |
| :--- | :---: | :--- |
| **Giải nén ZIP độc lập** | **PASS** | `ZipFile::ExtractToDirectory` giải nén thành công 200 tệp |
| **Kiểm tra tệp thực thi** | **PASS** | `Game.exe` tồn tại, kích thước 647,168 bytes |
| **Kiểm tra tài liệu README** | **PASS** | `README.txt` đầy đủ hướng dẫn phím tắt và tiến trình |
| **Khởi chạy độc lập không có Editor/Hub** | **PASS** | `Game.exe` chạy mượt mà, PID `22068`, không có lỗi khởi động |
| **Kiểm tra tải đồ họa (Direct3D 11)** | **PASS** | Khởi tạo thành công `GfxDevice` trên NVIDIA RTX 2050 |
| **Nạp tài nguyên nhúng** | **PASS** | `resources.assets` nạp sạch sẽ, không có cảnh báo script thất lạc |
| **Độ trễ đóng ngắt tiến trình** | **PASS** | Tiến trình thoát êm dịu, không giữ khóa tệp |

---

## 7. TỔNG HỢP KIỂM THỬ TỰ ĐỘNG (AUTOMATED TEST MATRIX)

1. **Bộ kiểm thử CLI độc lập (`Tests/ContrastTests/ContrastTests.csproj`)**:
   - `347/347` ca kiểm thử **PASS** (100%).
   - Bao gồm: Quỹ đạo động lực học, bộ nhớ đệm, sanitizer dữ liệu, chế độ T-Mode bay tự do, và kiểm tra tính hợp lệ của toàn bộ 3 file level JSON.
2. **Bộ kiểm thử nội suy Unity Editor (`EditorLevelTests.RunAllTestsBatch`)**:
   - `385/385` ca kiểm thử **PASS** (100%).
   - Bao gồm: Đóng gói PackagedResource, chuỗi đo giao thoa Newton $K-I-K'$, camera theo sát người chơi trong phòng xoay, và quy trình chuyển màn tự động.
3. **Tổng số ca kiểm thử xác minh**: **732 ca kiểm thử PASS (0 FAILED, 0 BLOCKED)**.

---

## 8. HƯỚNG DẪN DÀNH CHO NGƯỜI NHẬN BẢN GAME

Người nhận tệp `CONTRAST_Windows_Portable.zip` chỉ cần thực hiện 3 bước:
1. Nhấp chuột phải vào `CONTRAST_Windows_Portable.zip` -> Chọn **Extract All...** (Giải nén tất cả).
2. Mở thư mục vừa giải nén `CONTRAST_Windows`.
3. Nhấp đúp vào **`Game.exe`** để bắt đầu chơi ngay lập tức.
