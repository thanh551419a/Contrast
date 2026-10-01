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
    /// Minimal coordinator: finds the player/loader/camera, loads the level,
    /// and handles game-over/restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameOverUI gameOverUI;
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
            {
                gameOverUI =
                    new GameObject("GameOverUI")
                        .AddComponent<GameOverUI>();
            }
        }

        private void Start()
        {
            SetPlayer(player);
            SetState(GameState.Playing);

            if (levelLoader != null)
            {
                levelLoader.LoadAndInitializeLevel();
            }
            else
            {
                player?.ResetState();
            }
        }

        // Single Unity gameplay Update entry point. Player simulation and
        // camera processing are explicitly ordered here. No FixedUpdate or
        // LateUpdate drives gameplay movement.
        private void Update()
        {
            if (State == GameState.GameOver)
            {
                if (RestartKeyPressed())
                    Restart();

                return;
            }

            player?.ProcessUpdate(Time.deltaTime);
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

        public void Restart()
        {
            SetState(GameState.Playing);

            if (levelLoader != null)
                levelLoader.LoadAndInitializeLevel();
            else
                player?.ResetState();
        }

        private void SetState(GameState state)
        {
            State = state;

            if (gameOverUI != null)
                gameOverUI.Show(state == GameState.GameOver);
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
