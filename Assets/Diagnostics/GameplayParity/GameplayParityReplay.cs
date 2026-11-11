using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Contrast.Color;
using Contrast.Core;
using Contrast.Input;
using Contrast.Player;
using UnityEngine;

namespace Contrast.Diagnostics
{
    /// <summary>Opt-in test setup using existing loader and input seams; never moves the Player.</summary>
    public sealed class GameplayParityReplay : MonoBehaviour
    {
        private string levelPath, scenario, replayLog;
        private float duration;
        private ScriptedAdapter adapter;
        private PlayerInput input;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string[] args = Environment.GetCommandLineArgs();
            string path = Argument(args, "-contrastParityReplay", null);
            if (string.IsNullOrEmpty(path)) return;
            var host = new GameObject("GameplayParityReplay (test input only)");
            DontDestroyOnLoad(host);
            var runner = host.AddComponent<GameplayParityReplay>();
            runner.levelPath = Path.GetFullPath(path);
            runner.scenario = Argument(args, "-contrastParityScenario", "idle");
            runner.replayLog = Argument(args, "-contrastParityReplayLog", null);
            runner.duration = float.TryParse(Argument(args, "-contrastParityDuration", "4"), NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds)
                ? Mathf.Clamp(seconds, 0.1f, 120f) : 4f;
            runner.StartCoroutine(runner.Run());
        }

        private static string Argument(string[] args, string key, string fallback)
        {
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return fallback;
        }

        private IEnumerator Run()
        {
            float deadline = Time.realtimeSinceStartup + 60f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            var gm = GameManager.Instance;
            if (gm == null || !File.Exists(levelPath))
            {
                Debug.LogError("[PARITY_REPLAY] Missing GameManager or canonical JSON: " + levelPath);
                Application.Quit(2); yield break;
            }
            // This is the existing public load entry used by level selection.
            gm.PlayLevelFromFile(levelPath);
            while (gm.State != GameState.Playing && Time.realtimeSinceStartup < deadline) yield return null;
            if (gm.State != GameState.Playing || gm.Player == null)
            {
                Debug.LogError("[PARITY_REPLAY] Normal level load did not enter Playing.");
                Application.Quit(3); yield break;
            }
            input = gm.Player.GetComponent<PlayerInput>();
            adapter = new ScriptedAdapter(scenario, replayLog);
            input.SetCustomAdapter(adapter);
            Debug.Log("[PARITY_REPLAY] Started scenario=" + scenario + " canonical=" + levelPath);
            deadline = Time.realtimeSinceStartup + duration * 4f + 30f;
            while (adapter.Elapsed < duration && Time.realtimeSinceStartup < deadline && gm.State == GameState.Playing)
                yield return null;
            bool completed = adapter.Elapsed >= duration;
            Debug.Log("[PARITY_REPLAY] Finished scenario=" + scenario + " elapsed=" + adapter.Elapsed.ToString("R", CultureInfo.InvariantCulture) + " state=" + gm.State + " timelineCompleted=" + completed);
            input.SetCustomAdapter(null);
            adapter.Dispose(); adapter = null;
            // Test-process lifecycle only; never force victory or a gameplay state.
            yield return null;
            Application.Quit(completed ? 0 : 4);
        }

        private void OnDestroy()
        {
            if (input != null) input.SetCustomAdapter(null);
            adapter?.Dispose();
        }

        private sealed class ScriptedAdapter : IPlatformInputAdapter, IDisposable
        {
            [Serializable] private sealed class InputRecord
            {
                public int schemaVersion = 1;
                public string environment = "Unity", eventName = "input_collected", observationPhase = "custom_adapter_reset", scenario;
                public int frameIndex;
                public float simulationTime, scenarioTime, deltaTime, horizontal, phaseTune;
                public bool jumpPressed, jumpHeld, xPressed, xHeld, xReleased;
            }
            private readonly string scenario;
            private readonly StreamWriter log;
            private float previous = -1f;
            private bool previousX;
            public float Elapsed { get; private set; }

            public ScriptedAdapter(string scenario, string path)
            {
                this.scenario = scenario;
                if (!string.IsNullOrEmpty(path))
                {
                    string full = Path.GetFullPath(path);
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    log = new StreamWriter(new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
                }
            }
            private bool During(float a, float b) => Elapsed >= a && Elapsed < b;
            private bool Pulse(float t) => Elapsed >= t && previous < t;
            private bool XHeld => scenario == "x" && (During(1f, 2.5f) || During(2.75f, 3.25f));
            public float GetHorizontalAxis()
            {
                if (scenario == "horizontal") return During(1f, 2f) ? 1f : During(2.5f, 3.5f) ? -1f : 0f;
                return (scenario == "combined" && During(1f, 3f)) || (scenario == "x" && During(2.75f, 3.25f)) ? 1f : 0f;
            }
            public bool GetJumpDown() => (scenario == "jump" || scenario == "combined") && Pulse(1f);
            public bool GetFastFall() => false;
            public float GetPhaseTuneAxis() => scenario == "phase" ? (During(1f, 2f) ? 1f : During(2.5f, 3.5f) ? -1f : 0f) : 0f;
            public bool GetNewtonMarkDown() => XHeld && !previousX;
            public int GetColorChunkStep() => 0;
            public bool TryGetDirectColor(out LogicalColor color) { color = LogicalColor.White; return false; }
            public void ResetFrameState()
            {
                if (log != null)
                {
                    log.WriteLine(JsonUtility.ToJson(new InputRecord
                    {
                        scenario = scenario, frameIndex = Time.frameCount, simulationTime = Time.time,
                        scenarioTime = Elapsed, deltaTime = Time.deltaTime, horizontal = GetHorizontalAxis(),
                        phaseTune = GetPhaseTuneAxis(), jumpPressed = GetJumpDown(), jumpHeld = (scenario == "jump" || scenario == "combined") && During(1f, 1.2f), xHeld = XHeld,
                        xPressed = GetNewtonMarkDown(), xReleased = previousX && !XHeld
                    }));
                    log.Flush();
                }
                previousX = XHeld;
                previous = Elapsed;
                Elapsed += Mathf.Max(0f, Time.deltaTime);
            }
            public void Dispose() { log?.Dispose(); }
        }
    }
}

