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
        [SerializeField] private LevelEditorUI levelEditorUI;
        [SerializeField] private LevelEditorController levelEditor;
        [SerializeField] private PlayerController player;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private CameraFollow cameraFollow;

        public GameState State { get; private set; } = GameState.MainMenu;
        public LevelSource CurrentLevelSource { get; private set; }
        public GameplayEntryContext CurrentEntryContext { get; private set; } = GameplayEntryContext.MainMenuGameplay;
        public float KillY => levelLoader != null ? levelLoader.KillY : -50f;
        public PlayerController Player => player;
        public LevelLoader LevelLoader => levelLoader;
        public CameraFollow CameraFollow => cameraFollow;
        public LevelSelectUI LevelSelectUI => levelSelectUI;
        public LoadingOverlayUI LoadingOverlayUI => loadingOverlayUI;
        public InGameMenuUI InGameMenuUI => inGameMenuUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            player ??= FindAnyObjectByType<PlayerController>();
            levelLoader ??= FindAnyObjectByType<LevelLoader>();
            cameraFollow ??= FindAnyObjectByType<CameraFollow>();
            gameOverUI ??= FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            gameControlUI ??= FindAnyObjectByType<GameControlUI>(FindObjectsInactive.Include);
            inGameMenuUI ??= FindAnyObjectByType<InGameMenuUI>(FindObjectsInactive.Include);
            mainMenuUI ??= FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            levelSelectUI ??= FindAnyObjectByType<LevelSelectUI>(FindObjectsInactive.Include);
            loadingOverlayUI ??= FindAnyObjectByType<LoadingOverlayUI>(FindObjectsInactive.Include);
            levelEditorUI ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
            levelEditor ??= FindAnyObjectByType<LevelEditorController>(FindObjectsInactive.Include);

            if (gameOverUI == null)
                gameOverUI = new GameObject("GameOverUI").AddComponent<GameOverUI>();

            if (gameControlUI == null)
                gameControlUI = new GameObject("GameControlUI").AddComponent<GameControlUI>();

            if (inGameMenuUI == null)
                inGameMenuUI = new GameObject("InGameMenuUI").AddComponent<InGameMenuUI>();

            if (mainMenuUI == null)
                mainMenuUI = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();

            if (levelSelectUI == null)
                levelSelectUI = new GameObject("LevelSelectUI").AddComponent<LevelSelectUI>();

            if (loadingOverlayUI == null)
                loadingOverlayUI = new GameObject("LoadingOverlayUI").AddComponent<LoadingOverlayUI>();

            if (levelEditor == null)
                levelEditor = new GameObject("LevelEditorController").AddComponent<LevelEditorController>();

            if (levelEditorUI == null)
                levelEditorUI = new GameObject("LevelEditorUI").AddComponent<LevelEditorUI>();

            if (player != null)
                player.gameObject.SetActive(false);

            // Ensure persistent level directory and default level template exist on first launch
            LevelRepository.EnsureDefaultLevelFile();
        }

        private void Start()
        {
            SetState(GameState.MainMenu);
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
            Vector2 riderDisp = TrajectorySystem.GetRiderDisplacement(
                (Vector2)player.transform.position + player.PlayerOffset,
                player.PlayerSize,
                player.CurrentColor);
            player.SetRiderDisplacement(riderDisp);

            // 3. Normal player movement pipeline.
            player.ProcessUpdate(Time.deltaTime);
            levelLoader?.ProcessPlayerInteractions(player);
            cameraFollow?.ProcessCameraUpdate();
        }

        /// <summary>
        /// Transitions from Main Menu to Level Selection screen.
        /// </summary>
        public void StartGame()
        {
            Debug.Log("[FLOW][START_GAME] opening level select screen");
            CurrentEntryContext = GameplayEntryContext.MainMenuGameplay;
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(false);

            SetState(GameState.LevelSelect);
            levelSelectUI?.OpenForPlay();
        }

        /// <summary>
        /// Loads and starts a level from a disk file path with loading overlay and sanitization.
        /// </summary>
        public void PlayLevelFromFile(string filePath)
        {
            CurrentEntryContext = GameplayEntryContext.MainMenuGameplay;
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
            Debug.Log("[FLOW][OPEN_EDITOR] entering edit flow");
            levelSelectUI?.Show(false);
            loadingOverlayUI?.Show(false);
            inGameMenuUI?.Show(false);

            SetState(GameState.Editing);

            if (player != null)
                player.gameObject.SetActive(false);

            levelEditor?.StartEditing();
            levelEditorUI?.Show(true);
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
        /// Exits gameplay and returns to the appropriate destination based on how gameplay started:
        /// - MainMenuGameplay -> returns to top-level MainMenuUI.
        /// - EditorTestPlay -> returns directly to the LevelEditor session, restoring the working copy.
        /// </summary>
        public void ReturnFromGameplay()
        {
            Debug.Log($"[FLOW][RETURN_GAMEPLAY] returning from gameplay session with context={CurrentEntryContext}");
            inGameMenuUI?.Show(false);

            if (CurrentEntryContext == GameplayEntryContext.EditorTestPlay)
            {
                OpenEditor();
            }
            else
            {
                ReturnToMainMenu();
            }
        }

        public void SetPlayer(PlayerController pc)
        {
            player = pc;
            if (cameraFollow != null && player != null)
                cameraFollow.Target = player.transform;
        }

        public void OnPlayerDied()
        {
            if (State != GameState.Playing)
                return;
            SetState(GameState.GameOver);
        }

        public void OnPlayerWon()
        {
            if (State != GameState.Playing)
                return;
            SetState(GameState.Won);
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
                LevelRepository.EnsureDefaultLevelFile();
                string[] files = LevelRepository.ListLevelFiles();
                if (files != null && files.Length > 0)
                    CurrentLevelSource = LevelSource.FromFile(files[0]);
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

            if (CurrentLevelSource.SourceType == LevelSourceType.FilePath)
            {
                string path = CurrentLevelSource.FilePath;
                Debug.Log($"[FLOW][LEVEL_FILE] load path={path}");

                string rawJson = LevelRepository.ReadAllText(path);
                LevelData data = JsonUtility.FromJson<LevelData>(rawJson);
                data = LevelDataSanitizer.Sanitize(data, rawJson, out var report);
                if (report != null && report.Count > 0)
                {
                    Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                    foreach (string fix in report)
                        Debug.Log($"[LEVEL_FIX] {fix}");
                }
                levelLoader?.BuildRuntimeLevel(data);
            }
            else
            {
                string rawJson = CurrentLevelSource.InMemoryJson;
                LevelData data = JsonUtility.FromJson<LevelData>(rawJson);
                data = LevelDataSanitizer.Sanitize(data, rawJson, out var report);
                if (report != null && report.Count > 0)
                {
                    Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                    foreach (string fix in report)
                        Debug.Log($"[LEVEL_FIX] {fix}");
                }
                levelLoader?.BuildRuntimeLevel(data);
            }

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
            if (levelLoader == null)
                return;

            if (player != null)
                player.gameObject.SetActive(true);

            SetState(GameState.Playing);
            levelLoader.RetryCurrentSpawn();
        }

        private void SetState(GameState state)
        {
            GameState previous = State;
            State = state;
            Debug.Log($"[FLOW][STATE] {previous} -> {State}");

            mainMenuUI?.Show(state == GameState.MainMenu);
            levelSelectUI?.Show(state == GameState.LevelSelect);
            loadingOverlayUI?.Show(state == GameState.Loading);
            gameControlUI?.Show(state == GameState.Playing);
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
