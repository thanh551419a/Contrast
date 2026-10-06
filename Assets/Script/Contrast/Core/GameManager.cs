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
    /// Single coordinator for menu, gameplay and level editing.
    /// Gameplay still has one Update path for Player movement.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private GameControlUI gameControlUI;
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private LevelEditorUI levelEditorUI;
        [SerializeField] private LevelEditorController levelEditor;
        [SerializeField] private PlayerController player;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private CameraFollow cameraFollow;

        public GameState State { get; private set; } = GameState.MainMenu;
        public float KillY => levelLoader != null ? levelLoader.KillY : -50f;
        public PlayerController Player => player;
        public LevelLoader LevelLoader => levelLoader;
        public CameraFollow CameraFollow => cameraFollow;

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
            mainMenuUI ??= FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            levelEditorUI ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
            levelEditor ??= FindAnyObjectByType<LevelEditorController>(FindObjectsInactive.Include);

            if (gameOverUI == null)
                gameOverUI = new GameObject("GameOverUI").AddComponent<GameOverUI>();

            if (gameControlUI == null)
                gameControlUI = new GameObject("GameControlUI").AddComponent<GameControlUI>();

            if (mainMenuUI == null)
                mainMenuUI = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();

            if (levelEditor == null)
                levelEditor = new GameObject("LevelEditorController").AddComponent<LevelEditorController>();

            if (levelEditorUI == null)
                levelEditorUI = new GameObject("LevelEditorUI").AddComponent<LevelEditorUI>();

            if (player != null)
                player.gameObject.SetActive(false);
        }

        private void Start()
        {
            SetState(GameState.MainMenu);
        }

        // The only gameplay Update path.
        private void Update()
        {
            if (State == GameState.Editing || State == GameState.MainMenu)
                return;

            if (State != GameState.Playing)
            {
                if (State == GameState.GameOver && RestartKeyPressed())
                    Restart();
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

        public void StartGame()
        {
            Debug.Log("[FLOW][START_GAME] entering gameplay flow");
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(true);

            SetState(GameState.Playing);

            levelLoader?.LoadAndInitializeLevel();
        }

        public void StartGameWithLevelData(LevelData data)
        {
            Debug.Log("[FLOW][START_GAME_CUSTOM] entering gameplay flow with custom level data");
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(true);

            SetState(GameState.Playing);

            if (data != null && levelLoader != null)
                levelLoader.BuildRuntimeLevel(data);
            else
                levelLoader?.LoadAndInitializeLevel();
        }

        public void OpenEditor()
        {
            Debug.Log("[FLOW][OPEN_EDITOR] entering edit flow");
            SetState(GameState.Editing);

            if (player != null)
                player.gameObject.SetActive(false);

            levelEditor?.StartEditing();
            levelEditorUI?.Show(true);
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("[FLOW][RETURN_MENU] leaving editor/gameplay");
            levelEditor?.StopEditing();
            levelEditorUI?.Show(false);

            if (player != null)
                player.gameObject.SetActive(false);

            SetState(GameState.MainMenu);
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

        public void Restart()
        {
            Debug.Log("[FLOW][RESTART] reload full level");
            if (State == GameState.Editing)
                return;

            if (player != null)
                player.gameObject.SetActive(true);

            SetState(GameState.Playing);
            levelLoader?.LoadAndInitializeLevel();
        }

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
            gameControlUI?.Show(state == GameState.Playing);
            levelEditorUI?.Show(state == GameState.Editing);

            if (gameOverUI != null)
            {
                bool showResult = state == GameState.GameOver || state == GameState.Won;
                string title = state == GameState.Won ? "YOU WIN" : "GAME OVER";
                gameOverUI.ShowResult(showResult, title, state == GameState.GameOver);
            }
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
