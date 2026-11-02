using System.IO;
using Contrast.Camera;
using Contrast.Data;
using Contrast.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contrast.Level
{
    /// <summary>
    /// Bootstrapper for the standalone LevelEditor.exe executable.
    /// Manages the autonomous editor lifecycle without dependencies on GameManager or gameplay state.
    /// Automatically provisions required runtime systems and launches edit mode on startup.
    /// </summary>
    public sealed class StandaloneLevelEditorBootstrapper : MonoBehaviour
    {
        [Header("Editor Components")]
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelEditorController editorController;
        [SerializeField] private LevelEditorUI editorUI;
        [SerializeField] private LevelSelectUI levelSelectUI;
        [SerializeField] private CameraFollow cameraFollow;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureInfrastructure();
        }

        private void Start()
        {
            // Seed levels if running for the first time
            LevelRepository.EnsureDefaultLevelFile();

            string[] files = LevelRepository.ListLevelFiles();
            if (files != null && files.Length > 0 && File.Exists(files[0]))
            {
                Debug.Log($"[STANDALONE_EDITOR] Initializing with default level: {files[0]}");
                editorController?.ImportJson(files[0]);
            }
            else
            {
                levelLoader?.LoadAndInitializeLevel();
            }

            editorController?.StartEditing();
            editorController?.FitMap();

            Debug.Log("[STANDALONE_EDITOR] Standalone Level Editor started successfully.");
        }

        private void EnsureInfrastructure()
        {
            if (levelLoader == null)
            {
                levelLoader = FindAnyObjectByType<LevelLoader>();
                if (levelLoader == null)
                    levelLoader = gameObject.AddComponent<LevelLoader>();
            }

            if (editorController == null)
            {
                editorController = FindAnyObjectByType<LevelEditorController>();
                if (editorController == null)
                    editorController = gameObject.AddComponent<LevelEditorController>();
            }

            if (editorUI == null)
            {
                editorUI = FindAnyObjectByType<LevelEditorUI>();
                if (editorUI == null)
                    editorUI = gameObject.AddComponent<LevelEditorUI>();
            }

            if (levelSelectUI == null)
            {
                levelSelectUI = FindAnyObjectByType<LevelSelectUI>();
                if (levelSelectUI == null)
                    levelSelectUI = new GameObject("LevelSelectUI").AddComponent<LevelSelectUI>();
            }

            if (cameraFollow == null)
            {
                UnityEngine.Camera cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    cameraFollow = cam.GetComponent<CameraFollow>();
                    if (cameraFollow == null)
                        cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
                }
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
