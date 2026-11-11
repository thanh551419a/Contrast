using System;
using System.IO;
using System.Text;
using Contrast.Core;
using Contrast.Data;
using Contrast.Level;
using Contrast.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Contrast.Diagnostics
{
    /// <summary>Opt-in, read-only observer. Never writes gameplay state or consumes input.</summary>
    public sealed class GameplayParityObserver : MonoBehaviour
    {
        [Serializable] private sealed class InputSnapshot
        {
            public bool left, right, jumpHeld, jumpPressed, fastFall, tuneLeft, tuneRight;
            public bool xHeld, xPressed, xReleased;
        }
        [Serializable] private sealed class Record
        {
            public int schemaVersion = 1;
            public string runId, environment = "Unity", levelId, eventName, observationPhase;
            public int frameIndex;
            public float simulationTime, deltaTime;
            public string objectId, objectType;
            public Vector2 position, size, appliedDisplacement;
            public bool hasDisplacement;
            public InputSnapshot input;
            public string movementState, color, newtonStage, message, levelSource;
            public bool grounded;
            public float phase;
            public int newtonMarkers;
        }

        private StreamWriter writer;
        private readonly string runId = Guid.NewGuid().ToString("N");
        private PlayerController subscribedPlayer;
        private LevelData observedLevel;
        private string previousState, previousNewton;
        private int previousMarkers = -1;
        private Vector2 previousPosition;
        private bool hasPreviousPosition;
        private bool writeFailed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string output = Environment.GetEnvironmentVariable("CONTRAST_PARITY_LOG");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-contrastParityLog") output = args[i + 1];
            if (string.IsNullOrWhiteSpace(output)) return;
            var host = new GameObject("GameplayParityObserver (diagnostic only)");
            DontDestroyOnLoad(host);
            var observer = host.AddComponent<GameplayParityObserver>();
            try
            {
                string path = Path.GetFullPath(output);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                observer.writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                Application.logMessageReceived += observer.ObserveLog;
                observer.Write(observer.BaseRecord("run_start", "observer_install"));
                observer.writer.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PARITY_DIAGNOSTICS] Could not create new log: " + ex.Message);
                Destroy(host);
            }
        }

        private Record BaseRecord(string eventName, string phaseName)
        {
            var gm = GameManager.Instance;
            return new Record
            {
                runId = runId, eventName = eventName, observationPhase = phaseName,
                levelId = gm?.LevelLoader?.CurrentLevelData?.levelId ?? "unknown",
                levelSource = gm?.CurrentLevelSource?.ToString(),
                frameIndex = Time.frameCount, simulationTime = Time.time, deltaTime = Time.deltaTime,
                objectId = "runtime", objectType = "runtime"
            };
        }

        private void Write(Record record)
        {
            if (writer == null || writeFailed) return;
            try { writer.WriteLine(JsonUtility.ToJson(record)); }
            catch (Exception) { writeFailed = true; }
        }

        private void ObserveLog(string message, string stackTrace, LogType type)
        {
            if (message.StartsWith("[PARITY_DIAGNOSTICS]", StringComparison.Ordinal)) return;
            if (!(message.StartsWith("[FLOW]", StringComparison.Ordinal) ||
                  message.StartsWith("[INPUT]", StringComparison.Ordinal) ||
                  message.StartsWith("[INTENT]", StringComparison.Ordinal) ||
                  message.StartsWith("[COLLISION]", StringComparison.Ordinal) ||
                  message.StartsWith("[RESOLVE]", StringComparison.Ordinal) ||
                  message.StartsWith("[CHECKPOINT]", StringComparison.Ordinal) ||
                  message.StartsWith("[NEWTON_FINALE]", StringComparison.Ordinal))) return;
            var r = BaseRecord("phase_log", "existing_runtime_log");
            r.message = message;
            Write(r);
        }

        private static InputSnapshot ReadKeyboard()
        {
            var snapshot = new InputSnapshot();
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                snapshot.left = kb.aKey.isPressed; snapshot.right = kb.dKey.isPressed;
                snapshot.jumpHeld = kb.wKey.isPressed || kb.spaceKey.isPressed;
                snapshot.jumpPressed = kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                snapshot.fastFall = kb.sKey.isPressed;
                snapshot.tuneLeft = kb.leftArrowKey.isPressed; snapshot.tuneRight = kb.rightArrowKey.isPressed;
                snapshot.xHeld = kb.xKey.isPressed; snapshot.xPressed = kb.xKey.wasPressedThisFrame;
                snapshot.xReleased = kb.xKey.wasReleasedThisFrame;
            }
#endif
            return snapshot;
        }

        private void ObserveNewtonRequest()
        {
            var r = BaseRecord("newton_request_observed", "existing_player_event");
            r.objectId = "player"; r.objectType = "player";
            var n = EasyNewtonFinaleController.Instance;
            r.newtonStage = n != null ? n.CurrentStep.ToString() : "absent";
            r.newtonMarkers = n != null ? n.MarkerCount : 0;
            Write(r);
        }

        private void LateUpdate()
        {
            if (writer == null || writeFailed) return;
            var gm = GameManager.Instance;
            var pc = gm?.Player;
            if (pc != subscribedPlayer)
            {
                if (subscribedPlayer != null) subscribedPlayer.OnNewtonMarkRequested -= ObserveNewtonRequest;
                subscribedPlayer = pc;
                if (pc != null) pc.OnNewtonMarkRequested += ObserveNewtonRequest;
            }
            var current = gm?.LevelLoader?.CurrentLevelData;
            if (!ReferenceEquals(current, observedLevel))
            {
                observedLevel = current;
                hasPreviousPosition = false;
                Write(BaseRecord("level_initialized_observed", "late_update"));
            }
            string state = gm != null ? gm.State.ToString() : "absent";
            if (state != previousState)
            {
                var transition = BaseRecord("game_state_changed", "late_update");
                transition.message = previousState + " -> " + state;
                Write(transition); previousState = state;
            }
            if (pc != null && pc.gameObject.activeInHierarchy)
            {
                var r = BaseRecord("object_sample", "late_update_final_observed");
                r.objectId = "player"; r.objectType = "player";
                r.position = pc.transform.position; r.size = pc.PlayerSize;
                r.input = ReadKeyboard(); r.movementState = pc.State.ToString();
                r.color = pc.CurrentColor.ToString(); r.phase = pc.CurrentPhase; r.grounded = pc.IsGrounded;
                r.hasDisplacement = hasPreviousPosition;
                r.appliedDisplacement = hasPreviousPosition ? r.position - previousPosition : Vector2.zero;
                previousPosition = r.position; hasPreviousPosition = true;
                var n = EasyNewtonFinaleController.Instance;
                r.newtonStage = n != null ? n.CurrentStep.ToString() : "absent";
                r.newtonMarkers = n != null ? n.MarkerCount : 0;
                Write(r);
                if (r.newtonStage != previousNewton || r.newtonMarkers != previousMarkers)
                {
                    var nr = BaseRecord("newton_state_changed", "late_update");
                    nr.newtonStage = r.newtonStage; nr.newtonMarkers = r.newtonMarkers;
                    Write(nr); previousNewton = r.newtonStage; previousMarkers = r.newtonMarkers;
                }
            }
            foreach (var p in ColorPlatform.All)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                var r = BaseRecord("object_sample", "late_update_final_observed");
                var n = EasyNewtonFinaleController.Instance;
                r.objectId = n != null && n.PlatformK == p ? "newton_K" : n != null && n.PlatformI == p ? "newton_I" : p.name;
                r.objectType = "platform"; r.position = p.Position; r.size = p.Size;
                r.color = p.Logical.ToString(); Write(r);
            }
            foreach (var marker in LevelMarker.All)
            {
                if (marker == null || !marker.gameObject.activeInHierarchy) continue;
                var r = BaseRecord("object_sample", "late_update_final_observed");
                r.objectId = marker.name; r.objectType = marker.Type.ToString();
                r.position = marker.Position; r.size = marker.Size;
                r.message = "consumed=" + marker.IsConsumed; Write(r);
            }
            try { writer.Flush(); } catch (Exception) { writeFailed = true; }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= ObserveLog;
            if (subscribedPlayer != null) subscribedPlayer.OnNewtonMarkRequested -= ObserveNewtonRequest;
            if (writer == null) return;
            Write(BaseRecord("run_end", "observer_destroy"));
            try { writer.Dispose(); } catch (Exception) { }
            writer = null;
        }
    }
}
