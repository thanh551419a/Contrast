using Contrast.Camera;
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
    /// Single gameplay coordinator. Restart rebuilds the complete runtime level.
    /// Retry only restores the latest checkpoint position + color.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private GameControlUI gameControlUI;
        [SerializeField] private PlayerController player;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private CameraFollow cameraFollow;

        public GameState State { get; private set; } = GameState.Playing;
        public float KillY =>
            levelLoader != null ? levelLoader.KillY : -50f;
        public PlayerController Player => player;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (player == null)
                player = FindAnyObjectByType<PlayerController>();

            if (levelLoader == null)
                levelLoader = FindAnyObjectByType<LevelLoader>();

            if (cameraFollow == null)
                cameraFollow = FindAnyObjectByType<CameraFollow>();

            if (gameOverUI == null)
            {
                gameOverUI =
                    FindAnyObjectByType<GameOverUI>(
                        FindObjectsInactive.Include);
            }

            if (gameOverUI == null)
                gameOverUI =
                    new GameObject("GameOverUI")
                        .AddComponent<GameOverUI>();

            if (gameControlUI == null)
            {
                gameControlUI =
                    FindAnyObjectByType<GameControlUI>(
                        FindObjectsInactive.Include);
            }

            if (gameControlUI == null)
                gameControlUI =
                    new GameObject("GameControlUI")
                        .AddComponent<GameControlUI>();
        }

        private void Start()
        {
            SetPlayer(player);
            SetState(GameState.Playing);

            if (levelLoader != null)
                levelLoader.LoadAndInitializeLevel();
            else
                player?.ResetState();
        }

        // The only Unity Update driving gameplay.
        private void Update()
        {
            if (State != GameState.Playing)
            {
                if (State == GameState.GameOver && RestartKeyPressed())
                    Restart();

                return;
            }

            player?.ProcessUpdate(Time.deltaTime);

            // Level interactions happen after the final player movement.
            levelLoader?.ProcessPlayerInteractions(player);

            cameraFollow?.ProcessCameraUpdate();
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
        /// Rebuilds the complete runtime level and resets the spawn state to
        /// StartPos position + color.
        /// </summary>
        public void Restart()
        {
            SetState(GameState.Playing);

            if (levelLoader != null)
                levelLoader.LoadAndInitializeLevel();
            else
                player?.ResetState();
        }

        /// <summary>
        /// Does not reload JSON and does not recreate checkpoints/platforms.
        /// Only current checkpoint spawn position + color are restored.
        /// </summary>
        public void Retry()
        {
            if (levelLoader == null)
                return;

            SetState(GameState.Playing);
            levelLoader.RetryCurrentSpawn();
        }

        private void SetState(GameState state)
        {
            State = state;

            if (gameControlUI != null)
                gameControlUI.Show(state == GameState.Playing);

            if (gameOverUI != null)
            {
                bool showResult =
                    state == GameState.GameOver ||
                    state == GameState.Won;

                string title =
                    state == GameState.Won
                        ? "YOU WIN"
                        : "GAME OVER";

                bool allowRetry = state == GameState.GameOver;
                gameOverUI.ShowResult(showResult, title, allowRetry);
            }
        }

        private static bool RestartKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null &&
                   (kb.rKey.wasPressedThisFrame ||
                    kb.enterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R) ||
                   Input.GetKeyDown(KeyCode.Return);
#endif
        }
    }
}
