using System.Collections;
using System.Collections.Generic;
using System.IO;
using Contrast.Camera;
using Contrast.Data;
using Contrast.Level;
using Contrast.Player;
using Contrast.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Contrast.Core
{
    /// <summary>
    /// Single coordinator for menu, level selection, loading, gameplay and level editing.
    /// Manages state transitions, tracks active LevelSource for deterministic restarts,
    /// and ensures runtime UI elements exist.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private GameControlUI gameControlUI;
        [SerializeField] private InGameMenuUI inGameMenuUI;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private LevelSelectUI levelSelectUI;
        [SerializeField] private LoadingOverlayUI loadingOverlayUI;
        [SerializeField] private VirtualControlsUI virtualControlsUI;
        [SerializeField] private LevelEditorUI levelEditorUI;
        [SerializeField] private LevelEditorController levelEditor;
        [SerializeField] private PlayerController player;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private TestModeUI testModeUI;

        public GameState State { get; private set; } = GameState.MainMenu;
        public LevelSource CurrentLevelSource { get; private set; }
        public GameplayEntryContext CurrentEntryContext { get; private set; } = GameplayEntryContext.MainMenuGameplay;
        public static readonly string[] CampaignLevelResources = new string[]
        {
            "Levels/L01",
            "Levels/L02",
            "Levels/L03"
        };
        public static readonly string[] CampaignLevelTitles = new string[]
        {
            "L01 — Cánh cửa không phải vật cản",
            "L02 — Cầu thang ma",
            "L03 — Giếng ba tầng"
        };
        public int CurrentCampaignIndex { get; private set; } = -1;
        public bool IsCampaignActive => CurrentEntryContext == GameplayEntryContext.Campaign && CurrentCampaignIndex >= 0;
        public float KillY => levelLoader != null ? levelLoader.KillY : -50f;
        public float DeathY => levelLoader != null ? levelLoader.DeathY : -50f;
        public PlayerController Player => player;
        public LevelLoader LevelLoader => levelLoader;
        public CameraFollow CameraFollow => cameraFollow;
        public LevelSelectUI LevelSelectUI => levelSelectUI;
        public LoadingOverlayUI LoadingOverlayUI => loadingOverlayUI;
        public VirtualControlsUI VirtualControlsUI => virtualControlsUI;
        public InGameMenuUI InGameMenuUI => inGameMenuUI;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInstance()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameplaySceneLoaded()
        {
            if (FindAnyObjectByType<GameManager>(FindObjectsInactive.Include) == null)
            {
                Debug.Log("[FLOW][BOOT] No GameManager in active scene -> loading SampleScene automatically.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            player ??= FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            levelLoader ??= FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
            cameraFollow ??= FindAnyObjectByType<CameraFollow>(FindObjectsInactive.Include);
            gameOverUI ??= FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            gameControlUI ??= FindAnyObjectByType<GameControlUI>(FindObjectsInactive.Include);
            inGameMenuUI ??= FindAnyObjectByType<InGameMenuUI>(FindObjectsInactive.Include);
            mainMenuUI ??= FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            loadingOverlayUI ??= FindAnyObjectByType<LoadingOverlayUI>(FindObjectsInactive.Include);
            virtualControlsUI ??= FindAnyObjectByType<VirtualControlsUI>(FindObjectsInactive.Include);

            if (gameOverUI == null)
                gameOverUI = new GameObject("GameOverUI").AddComponent<GameOverUI>();

            if (gameControlUI == null)
                gameControlUI = new GameObject("GameControlUI").AddComponent<GameControlUI>();

            if (virtualControlsUI == null)
                virtualControlsUI = new GameObject("VirtualControlsUI").AddComponent<VirtualControlsUI>();

            if (inGameMenuUI == null)
                inGameMenuUI = new GameObject("InGameMenuUI").AddComponent<InGameMenuUI>();

            if (mainMenuUI == null)
                mainMenuUI = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();

            if (loadingOverlayUI == null)
                loadingOverlayUI = new GameObject("LoadingOverlayUI").AddComponent<LoadingOverlayUI>();

            // Ensure Load Level from JSON and Level Editor UI/Controller are disabled if present
            levelSelectUI ??= FindAnyObjectByType<LevelSelectUI>(FindObjectsInactive.Include);
            if (levelSelectUI != null)
                levelSelectUI.Show(false);

            levelEditor ??= FindAnyObjectByType<LevelEditorController>(FindObjectsInactive.Include);
            if (levelEditor != null)
                levelEditor.StopEditing();

            levelEditorUI ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
            if (levelEditorUI != null)
                levelEditorUI.Show(false);

            testModeUI ??= FindAnyObjectByType<TestModeUI>(FindObjectsInactive.Include);
            if (testModeUI == null)
                testModeUI = new GameObject("TestModeUI").AddComponent<TestModeUI>();

            if (player != null)
                player.gameObject.SetActive(false);
        }

        private void Start()
        {
            SetState(GameState.MainMenu);
        }

        public void SetTestMode(bool enabled)
        {
            if (player != null)
                player.SetTestMode(enabled);

            cameraFollow?.SetTestModeFreeMove(enabled);
            testModeUI?.Show(enabled);
        }

        // The only gameplay Update path.
        private void Update()
        {
            if (State != GameState.Playing)
            {
                if (State == GameState.Paused && EscapeKeyPressed())
                {
                    ResumeGame();
                    return;
                }

                if ((State == GameState.GameOver || State == GameState.Won) && EscapeKeyPressed())
                {
                    ReturnFromGameplay();
                    return;
                }

                if (State == GameState.GameOver && RestartKeyPressed())
                    Restart();
                return;
            }

            if (RKeyPressed())
            {
                Restart();
                return;
            }

            if (player != null && player.IsTestModeActive && EscapeKeyPressed())
            {
                SetTestMode(false);
                return;
            }

            if (TKeyPressed())
            {
                bool nextMode = player != null ? !player.IsTestModeActive : false;
                SetTestMode(nextMode);
                return;
            }

            if (EscapeKeyPressed())
            {
                OpenInGameMenu();
                return;
            }

            if (player == null)
                return;

            // 1. Advance all trajectory-driven platforms BEFORE player movement
            //    so that ColorPlatform.GetAabb() returns current geometry.
            TrajectorySystem.UpdateAll(Time.deltaTime);

            // 2. Apply rider-carry: if the player stands on a moving platform,
            //    pass its frame displacement into the player's movement pipeline.
            //    HTML strictly bypasses moving platform carry during Test Mode.
            Vector2 riderDisp = Vector2.zero;
            if (!player.IsTestModeActive && player.IsGrounded && !player.IsJumping)
            {
                riderDisp = TrajectorySystem.GetRiderDisplacement(
                    (Vector2)player.transform.position + player.PlayerOffset,
                    player.PlayerSize,
                    player.CurrentColor,
                    player.GroundedPlatform,
                    player.IsJumping);
            }
            player.SetRiderDisplacement(riderDisp);

            // 3. Normal player movement pipeline.
            player.ProcessUpdate(Time.deltaTime);
            levelLoader?.ProcessPlayerInteractions(player);
            cameraFollow?.ProcessCameraUpdate();
        }

        /// <summary>
        /// Starts the built-in campaign from L01 directly from packaged resources.
        /// Does not depend on AppData or external level files.
        /// </summary>
        public void StartCampaign()
        {
            Debug.Log("[FLOW][CAMPAIGN] Starting built-in campaign at L01");
            PlayCampaignLevel(0);
        }

        /// <summary>
        /// Directly loads and starts any built-in level (L01=0, L02=1, L03=2) from the Chơi thử -> Chọn màn menu
        /// without requiring prior level completion.
        /// </summary>
        public void PlayDirectLevel(int levelIndex)
        {
            Debug.Log($"[FLOW][DIRECT_LEVEL] Directly starting built-in level index={levelIndex}");
            PlayCampaignLevel(levelIndex);
        }

        public void PlayCampaignLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= CampaignLevelResources.Length)
            {
                Debug.LogError($"[FLOW][CAMPAIGN] Invalid campaign level index: {levelIndex}");
                ReturnToMainMenu();
                return;
            }

            CurrentCampaignIndex = levelIndex;
            CurrentEntryContext = GameplayEntryContext.Campaign;
            string resourcePath = CampaignLevelResources[levelIndex];
            string levelTitle = CampaignLevelTitles[levelIndex];
            Debug.Log($"[FLOW][CAMPAIGN] Loading level {levelIndex + 1}/{CampaignLevelResources.Length}: {levelTitle} ({resourcePath})");
            StartCoroutine(LoadPackagedLevelRoutine(resourcePath, levelTitle));
        }

        private IEnumerator LoadPackagedLevelRoutine(string resourcePath, string levelTitle)
        {
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);
            levelSelectUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(false);

            SetState(GameState.Loading);
            loadingOverlayUI?.Show(true, "LOADING CAMPAIGN...", $"ĐANG TẢI {levelTitle.ToUpper()} · SYNCHRONIZING OPTICAL PHASE");
            yield return null;

            LevelSource source = LevelSource.FromResource(resourcePath, levelTitle);
            string rawJson;
            try
            {
                rawJson = source.LoadRawJson();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][CAMPAIGN] Failed to load resource {resourcePath}: {ex.Message}");
                loadingOverlayUI?.Show(false);
                ReturnToMainMenu();
                yield break;
            }

            LevelData data;
            try
            {
                data = JsonUtility.FromJson<LevelData>(rawJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][CAMPAIGN] Failed to parse JSON for {resourcePath}: {ex.Message}");
                loadingOverlayUI?.Show(false);
                ReturnToMainMenu();
                yield break;
            }

            if (data == null)
            {
                Debug.LogError($"[FLOW][CAMPAIGN] Deserialized LevelData is null for {resourcePath}");
                loadingOverlayUI?.Show(false);
                ReturnToMainMenu();
                yield break;
            }

            try
            {
                data = LevelDataSanitizer.Sanitize(data, rawJson, out var report);
                if (report != null && report.Count > 0)
                {
                    Debug.Log($"[CAMPAIGN_SANITY] Automatically sanitized {report.Count} items.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CAMPAIGN_SANITY] Sanitize warning: {ex.Message}");
            }

            try
            {
                levelLoader?.BuildRuntimeLevel(data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][CAMPAIGN] Runtime level build error: {ex.Message}");
                loadingOverlayUI?.Show(false);
                ReturnToMainMenu();
                yield break;
            }

            CurrentLevelSource = source;

            if (player != null)
                player.gameObject.SetActive(true);

            loadingOverlayUI?.Show(false);
            SetState(GameState.Playing);
        }

        public void OpenLevelSelect()
        {
            Debug.Log("[FLOW][LOAD_LEVEL] Load level from JSON mode is disabled -> returning to 3-level Main Menu");
            ReturnToMainMenu();
        }

        public void StartGame() => StartCampaign();

        /// <summary>
        /// Loads and starts a level from a disk file path with loading overlay and sanitization.
        /// </summary>
        public void PlayLevelFromFile(string filePath)
        {
            CurrentCampaignIndex = -1;
            CurrentEntryContext = GameplayEntryContext.CustomLevel;
            Debug.Log($"[FLOW][START_GAME_FILE] loading level from file: {filePath} (context={CurrentEntryContext})");
            StartCoroutine(LoadLevelFromFileRoutine(filePath));
        }

        private IEnumerator LoadLevelFromFileRoutine(string filePath)
        {
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);
            levelSelectUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(false);

            SetState(GameState.Loading);
            loadingOverlayUI?.Show(true, "LOADING LEVEL...", "ĐANG TẢI & THIẾT LẬP MÀN CHƠI · SYNCHRONIZING OPTICAL PHASE");
            yield return null; // 1-frame pause so the loading overlay renders

            Debug.Log($"[FLOW][LEVEL_FILE] load path={filePath}");

            string rawJson;
            try
            {
                rawJson = LevelRepository.ReadAllText(filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][LEVEL_FILE] Failed to read file: {ex.Message}");
                loadingOverlayUI?.Show(false);
                SetState(GameState.LevelSelect);
                levelSelectUI?.OpenForPlay();
                levelSelectUI?.ShowError($"Không thể đọc file: {ex.Message}");
                yield break;
            }

            LevelData data;
            try
            {
                data = JsonUtility.FromJson<LevelData>(rawJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][LEVEL_FILE] Failed to parse JSON: {ex.Message}");
                loadingOverlayUI?.Show(false);
                SetState(GameState.LevelSelect);
                levelSelectUI?.OpenForPlay();
                levelSelectUI?.ShowError($"JSON không hợp lệ: {ex.Message}");
                yield break;
            }

            if (data == null)
            {
                Debug.LogError("[FLOW][LEVEL_FILE] Deserialized LevelData is null.");
                loadingOverlayUI?.Show(false);
                SetState(GameState.LevelSelect);
                levelSelectUI?.OpenForPlay();
                levelSelectUI?.ShowError("Dữ liệu level rỗng (null).");
                yield break;
            }

            List<string> report;
            try
            {
                data = LevelDataSanitizer.Sanitize(data, rawJson, out report);
                if (report != null && report.Count > 0)
                {
                    Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                    foreach (string fix in report)
                        Debug.Log($"[LEVEL_FIX] {fix}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][LEVEL_FILE] Sanitization error: {ex.Message}");
                loadingOverlayUI?.Show(false);
                SetState(GameState.LevelSelect);
                levelSelectUI?.OpenForPlay();
                levelSelectUI?.ShowError($"Lỗi kiểm tra dữ liệu: {ex.Message}");
                yield break;
            }

            try
            {
                levelLoader?.BuildRuntimeLevel(data);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][LEVEL_FILE] Runtime level build error: {ex.Message}");
                loadingOverlayUI?.Show(false);
                SetState(GameState.LevelSelect);
                levelSelectUI?.OpenForPlay();
                levelSelectUI?.ShowError($"Lỗi khởi tạo màn chơi: {ex.Message}");
                yield break;
            }

            CurrentLevelSource = LevelSource.FromFile(filePath);

            if (player != null)
                player.gameObject.SetActive(true);

            loadingOverlayUI?.Show(false);
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Starts gameplay using custom level data (e.g. from editor TEST PLAY).
        /// Captures an in-memory snapshot for deterministic restarts.
        /// </summary>
        public void StartGameWithLevelData(
            LevelData data,
            string snapshotJson = null,
            GameplayEntryContext entryContext = GameplayEntryContext.EditorTestPlay)
        {
            CurrentEntryContext = entryContext;
            Debug.Log($"[FLOW][START_GAME_CUSTOM] entering gameplay flow with custom level data (context={CurrentEntryContext})");
            StartCoroutine(LoadCustomLevelRoutine(data, snapshotJson));
        }

        private IEnumerator LoadCustomLevelRoutine(LevelData data, string snapshotJson)
        {
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);
            levelSelectUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(false);

            SetState(GameState.Loading);
            loadingOverlayUI?.Show(true, "INITIALIZING TEST PLAY...", "KHỞI CHẠY MÀN CHƠI THỬ NGHIỆM");
            yield return null;

            if (snapshotJson == null && data != null)
                snapshotJson = LevelDataSanitizer.SerializeToJson(data);

            CurrentLevelSource = LevelSource.FromMemory(snapshotJson);

            LevelData toLoad = data;
            if (toLoad == null && !string.IsNullOrEmpty(snapshotJson))
            {
                try { toLoad = JsonUtility.FromJson<LevelData>(snapshotJson); } catch { }
            }

            if (toLoad != null)
            {
                toLoad = LevelDataSanitizer.Sanitize(toLoad, snapshotJson, out var report);
                if (report != null && report.Count > 0)
                {
                    Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                    foreach (string fix in report)
                        Debug.Log($"[LEVEL_FIX] {fix}");
                }
                levelLoader?.BuildRuntimeLevel(toLoad);
            }
            else
            {
                levelLoader?.LoadAndInitializeLevel();
            }

            if (player != null)
                player.gameObject.SetActive(true);

            loadingOverlayUI?.Show(false);
            SetState(GameState.Playing);
        }

        public void OpenEditor()
        {
            Debug.Log("[FLOW][OPEN_EDITOR] Level Editor mode is disabled -> returning to 3-level Main Menu");
            ReturnToMainMenu();
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("[FLOW][RETURN_MENU] leaving editor/gameplay/levelselect");
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);
            levelSelectUI?.Show(false);
            loadingOverlayUI?.Show(false);
            inGameMenuUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(false);

            CurrentCampaignIndex = -1;
            CurrentEntryContext = GameplayEntryContext.MainMenuGameplay;
            SetState(GameState.MainMenu);
        }

        public void OpenInGameMenu()
        {
            if (State != GameState.Playing)
                return;

            Debug.Log("[FLOW][PAUSE] opening in-game menu");
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused)
                return;

            Debug.Log("[FLOW][RESUME] resuming gameplay");
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Exits gameplay and returns to the 3-level MainMenuUI.
        /// </summary>
        public void ReturnFromGameplay()
        {
            Debug.Log($"[FLOW][RETURN_GAMEPLAY] returning from gameplay session with context={CurrentEntryContext}");
            inGameMenuUI?.Show(false);
            ReturnToMainMenu();
        }

        public void SetPlayer(PlayerController pc)
        {
            player = pc;
            if (cameraFollow != null && player != null)
                cameraFollow.Target = player.transform;
        }

        private bool isAutoRespawning;

        public void OnPlayerDied()
        {
            if (State != GameState.Playing || isAutoRespawning)
                return;

            isAutoRespawning = true;
            try
            {
                Debug.Log("[FLOW][AUTO_RESPAWN] Player died -> automatically respawning at latest checkpoint");
                Retry();
            }
            finally
            {
                isAutoRespawning = false;
            }
        }

        public void OnPlayerWon()
        {
            if (State != GameState.Playing)
                return;

            Debug.Log($"[FLOW][WIN] Player won level (context={CurrentEntryContext}, campaignIndex={CurrentCampaignIndex})");

            if (CurrentEntryContext == GameplayEntryContext.Campaign)
            {
                if (CurrentCampaignIndex >= 0 && CurrentCampaignIndex < CampaignLevelResources.Length - 1)
                {
                    StartCoroutine(AdvanceCampaignRoutine(CurrentCampaignIndex + 1));
                    return;
                }
                else
                {
                    SetState(GameState.Won);
                    if (gameOverUI != null)
                    {
                        gameOverUI.ShowCampaignComplete(
                            onPlayAgain: () => StartCampaign(),
                            onMainMenu: () => ReturnToMainMenu()
                        );
                    }
                    return;
                }
            }

            SetState(GameState.Won);
        }

        private IEnumerator AdvanceCampaignRoutine(int nextIndex)
        {
            SetState(GameState.Loading);
            string nextTitle = CampaignLevelTitles[nextIndex];
            loadingOverlayUI?.Show(true, "HOÀN THÀNH MÀN CHƠI!", $"CHUẨN BỊ BƯỚC VÀO {nextTitle.ToUpper()}...");
            yield return new WaitForSeconds(1.2f);
            PlayCampaignLevel(nextIndex);
        }

        /// <summary>
        /// Reloads the exact level currently being played from its tracked source.
        /// Preserves in-memory TEST PLAY level vs. disk file without switching to default.
        /// </summary>
        public void Restart()
        {
            Debug.Log($"[FLOW][RESTART] reload full level from tracked source (context={CurrentEntryContext})");
            if (State == GameState.Editing || State == GameState.LevelSelect || State == GameState.Loading)
                return;

            inGameMenuUI?.Show(false);

            if (CurrentLevelSource == null)
            {
                CurrentLevelSource = LevelSource.FromResource(CampaignLevelResources[0], CampaignLevelTitles[0]);
            }

            if (CurrentLevelSource == null)
            {
                Debug.LogError("[FLOW][RESTART] No level source tracked to restart!");
                return;
            }

            StartCoroutine(RestartRoutine());
        }

        private IEnumerator RestartRoutine()
        {
            if (player != null)
                player.gameObject.SetActive(false);

            SetState(GameState.Loading);
            loadingOverlayUI?.Show(true, "RESTARTING...", "TÁI KHỞI ĐỘNG MÀN CHƠI TỪ NGUỒN GỐC");
            yield return null;

            string rawJson;
            try
            {
                rawJson = CurrentLevelSource.LoadRawJson();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FLOW][RESTART] Failed to load raw JSON from source: {ex.Message}");
                loadingOverlayUI?.Show(false);
                ReturnToMainMenu();
                yield break;
            }

            LevelData data = JsonUtility.FromJson<LevelData>(rawJson);
            data = LevelDataSanitizer.Sanitize(data, rawJson, out var report);
            if (report != null && report.Count > 0)
            {
                Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                foreach (string fix in report)
                    Debug.Log($"[LEVEL_FIX] {fix}");
            }
            levelLoader?.BuildRuntimeLevel(data);

            if (player != null)
                player.gameObject.SetActive(true);

            loadingOverlayUI?.Show(false);
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Retry preserves the checkpoint state and resets trajectory phase without rebuilding level.
        /// </summary>
        public void Retry()
        {
            Debug.Log("[FLOW][RETRY] use latest checkpoint spawn");
            if (player != null)
                player.gameObject.SetActive(true);

            SetState(GameState.Playing);

            if (levelLoader != null)
            {
                levelLoader.RetryCurrentSpawn();
            }
            else if (player != null)
            {
                player.ResetState();
            }
        }

        private void SetState(GameState state)
        {
            if (state != GameState.Playing && player != null && player.IsTestModeActive)
            {
                SetTestMode(false);
            }

            GameState previous = State;
            State = state;
            Debug.Log($"[FLOW][STATE] {previous} -> {State}");

            mainMenuUI?.Show(state == GameState.MainMenu);
            levelSelectUI?.Show(state == GameState.LevelSelect);
            loadingOverlayUI?.Show(state == GameState.Loading);
            gameControlUI?.Show(state == GameState.Playing);
            virtualControlsUI?.Show(state == GameState.Playing);
            inGameMenuUI?.Show(state == GameState.Paused, CurrentEntryContext);
            levelEditorUI?.Show(state == GameState.Editing);

            if (gameOverUI != null)
            {
                bool showResult = state == GameState.GameOver || state == GameState.Won;
                string title = state == GameState.Won ? "YOU WIN" : "GAME OVER";
                gameOverUI.ShowResult(showResult, title, state == GameState.GameOver);
            }
        }

        private static bool EscapeKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool TKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.tKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.T);
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && player != null && player.IsTestModeActive)
            {
                SetTestMode(false);
            }
        }

        private static bool RKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private static bool RestartKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null &&
                   (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return);
#endif
        }
    }
}
