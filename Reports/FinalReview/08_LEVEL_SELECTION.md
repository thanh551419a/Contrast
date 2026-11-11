# 08 — DIRECT LEVEL SELECTION & CAMPAIGN MODE (`Reports/FinalReview/08_LEVEL_SELECTION.md`)

## 1. Reviewer Requirement

- When clicking **Chơi thử** on the Main Menu, players and reviewers must be able to:
  1. **CHƠI TỪ ĐẦU (Play From Start):** Play the sequential 3-level campaign (`L01 → L02 → L03`).
  2. **CHỌN MÀN (Direct Level Selection):** Immediately launch `L01`, `L02`, or `L03` without having to beat earlier levels first.
  3. **QUAY LẠI MENU CHÍNH (Back to Main Menu):** Return cleanly to the primary menu buttons.
- Preserve existing **LOAD LEVEL**, **LEVEL EDITOR**, and **THOÁT GAME** buttons on the Main Menu.

---

## 2. Implementation (`MainMenuUI.cs`, `GameManager.cs`, `EasyNewtonFinaleController.cs`)

1. **`MainMenuUI.cs` Play Mode Sub-Panel (`PlaySubMenuPanel`):**
   - Clicking **CHƠI THỬ** (`PlayButton`) opens `PlaySubMenuPanel` (and hides `PrimaryMenuGroup`), presenting:
     - **`PlayFromStartButton` (`CHƠI TỪ ĐẦU (L01 → L03)`):** Calls `gameManager.StartCampaign()`.
     - **Direct Level Selection Section (`HOẶC CHỌN MÀN CHƠI NGAY`):**
       - **`SelectLevel_L01` (`L01 — Cánh cửa không phải vật cản`):** Calls `gameManager.PlayDirectLevel(0)`.
       - **`SelectLevel_L02` (`L02 — Cầu thang ma`):** Calls `gameManager.PlayDirectLevel(1)`.
       - **`SelectLevel_L03` (`L03 — Giếng ba tầng`):** Calls `gameManager.PlayDirectLevel(2)`.
     - **`BackToMainMenuButton` (`← QUAY LẠI MENU CHÍNH`):** Returns to `PrimaryMenuGroup`.
   - Whenever `MainMenuUI.Show()` is called (e.g., returning to Main Menu from gameplay), `ShowPrimaryMenu()` resets the view so the primary menu is shown first.
2. **`GameManager.PlayDirectLevel(int levelIndex)` (`Assets/Script/Contrast/Core/GameManager.cs`):**
   - Validates `levelIndex` (`0..2`), activates campaign progression starting at `campaignIndex = levelIndex`, constructs the `LevelSource.FromResource(CampaignResourcePaths[levelIndex], CampaignTitles[levelIndex])`, and calls `StartGameplayFromSource(src, GameplayEntryContext.MainMenuPlay)`.
   - Completing `L01` advances to `L02`; completing `L02` advances to `L03`; completing `L03` displays the Victory screen.
3. **Newton Chamber Direct-Level Entry (`EasyNewtonFinaleController.cs`):**
   - In `L01`, `L02`, and `L03`, `EasyNewtonFinaleController` initializes automatically when the level loads, starts orbital platforms `K` and `I` (`orbitStart = "auto"`), and enables the `X`-key `K → I → K'` measurement sequence whether the level was entered via **CHƠI TỪ ĐẦU**, **CHỌN MÀN**, **LOAD LEVEL**, or **Editor Test Play**.

