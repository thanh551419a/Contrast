using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Core;
using Contrast.Data;
using Contrast.Player;
using Contrast.UI;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Contrast.Level
{
    /// <summary>
    /// Runtime level editor. It reuses LevelLoader's JSON schema and runtime
    /// construction. Editor interaction itself is independent from gameplay
    /// Physics2D and works directly against the same AABB/data used by gameplay.
    /// </summary>
    public sealed class LevelEditorController : MonoBehaviour
    {
        private enum SelectionKind
        {
            None,
            Platform,
            Marker
        }

        private enum HandleKind
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TrajOrigin,
            TrajLineEnd,
            TrajCircleCenter,
            TrajCircleRadius,
            TrajArcStart,
            TrajArcEnd,
            TrajBoundsMinX,
            TrajBoundsMaxX,
            TrajBoundsMinY,
            TrajBoundsMaxY
        }

        [Header("Dependencies")]
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelEditorUI ui;

        [Header("Placement Defaults")]
        [SerializeField] private Vector2 defaultPlatformSize = Vector2.one;
        [SerializeField] private float defaultPlatformColor = 255f;
        [SerializeField] private Vector2 defaultMarkerSize = Vector2.one;
        [SerializeField] private float defaultMarkerColor = 0f;

        [Header("Editor Handles")]
        [SerializeField] private float handleSize = 0.16f;
        [SerializeField] private float handleHitRadius = 0.35f;

        [Header("Editor Debug")]
        [SerializeField] private bool logEditorInteraction = true;

        private bool editing;
        private bool paletteDragging;
        private bool mapPointerDragging;
        private bool mapPanning;
        private Vector2 panStartScreen;
        private Vector3 panStartCameraPosition;
        private LevelMarkerType pendingPaletteType;
        private bool pendingPaletteIsMarker;

        private SelectionKind selectionKind;
        private ColorPlatform selectedPlatform;
        private LevelMarker selectedMarker;
        private HandleKind activeHandle;

        private Vector2 dragStartWorld;
        private Vector2 dragStartPosition;
        private Vector2 dragStartSize;

        // Trajectory drag initial state
        private Vector2 dragStartTrajOrigin;
        private Vector2 dragStartBoundsMin;
        private Vector2 dragStartBoundsMax;
        private float dragStartTrajRotation;
        private float dragStartTrajScale;
        private float dragStartTrajRadius;
        private float dragStartParamMin;
        private float dragStartParamMax;
        private float dragStartTrajT;

        private GameObject handleRoot;
        private readonly Dictionary<HandleKind, GameObject> handles =
            new Dictionary<HandleKind, GameObject>();

        private LineRenderer trajectoryLineRenderer;
        private LineRenderer boundsLineRenderer;

        public bool IsEditing => editing;
        public ColorPlatform SelectedPlatform => selectedPlatform;

        private void Awake()
        {
            levelLoader ??= FindAnyObjectByType<LevelLoader>();
            ui ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
        }

        public void StartEditing()
        {
            editing = true;
            EnsureDependencies();
            EnsureHandleRoot();
            ClearSelection();
            ui?.Show(true);
        }

        public void StopEditing()
        {
            editing = false;
            paletteDragging = false;
            mapPointerDragging = false;
            mapPanning = false;
            activeHandle = HandleKind.None;
            ClearSelection();
            ui?.Show(false);
        }

        public void BeginPaletteDrag(LevelMarkerType type, bool marker)
        {
            if (!editing)
                return;

            pendingPaletteType = type;
            pendingPaletteIsMarker = marker;
            paletteDragging = true;
        }

        public void EndPaletteDrag(Vector2 screenPosition)
        {
            if (!paletteDragging)
                return;

            paletteDragging = false;

            if (IsPointerOverUi())
                return;

            Vector2 world = ScreenToWorld(screenPosition);
            CreateFromPalette(pendingPaletteType, pendingPaletteIsMarker, world);
        }

        public void RequestImport()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel(
                "Import Contrast Level",
                Application.dataPath,
                "json");

            if (!string.IsNullOrEmpty(path))
                ImportJson(path);
#else
            ui?.SetStatus(
                "Import trong build cần đường dẫn file trong StreamingAssets.");
#endif
        }

        public void ImportJson(string path)
        {
            if (!editing)
                return;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ui?.SetStatus("JSON file không tồn tại.");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                LevelData data = JsonUtility.FromJson<LevelData>(json);

                if (data == null)
                {
                    ui?.SetStatus("Không thể đọc LevelData từ JSON.");
                    return;
                }

                if (!ValidateImportedData(data, out string error))
                {
                    ui?.SetStatus(error);
                    return;
                }

                EnsureDependencies();
                levelLoader.BuildRuntimeLevel(data);
                ClearSelection();
                ui?.SetStatus("Import level thành công.");
            }
            catch (Exception ex)
            {
                ui?.SetStatus("Import lỗi: " + ex.Message);
            }
        }

        public void RequestExport()
        {
            if (!editing)
                return;

            LevelData data = BuildExportData();
            string json = JsonUtility.ToJson(data, true);

#if UNITY_EDITOR
            string path = EditorUtility.SaveFilePanel(
                "Export Contrast Level",
                Application.dataPath,
                "ContrastLevel",
                "json");

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                ui?.SetStatus("Export level thành công.");
            }
#else
            string path = Path.Combine(
                Application.persistentDataPath,
                "ContrastLevel.json");
            File.WriteAllText(path, json);
            ui?.SetStatus("Export: " + path);
#endif
        }

        public void RequestPlayLevel()
        {
            if (!editing)
                return;

            LevelData data = BuildExportData();
            ClearSelection();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGameWithLevelData(data);
            }
            else
            {
                levelLoader?.BuildRuntimeLevel(data);
            }
        }

        public void SelectPlatform(ColorPlatform platform)
        {
            if (platform == null)
            {
                ClearSelection();
                return;
            }

            selectionKind = SelectionKind.Platform;
            selectedPlatform = platform;
            selectedMarker = null;
            activeHandle = HandleKind.None;

            // Ensure platform center is on the trajectory line if trajectory is active
            Trajectory traj = platform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            if (cfg != null && cfg.enabled && Trajectory.Validate(cfg) == null)
            {
                Vector2 onTraj = Trajectory.ProjectPointOntoTrajectory(cfg, platform.Position, out _);
                if ((onTraj - platform.Position).sqrMagnitude > 1e-6f)
                {
                    platform.SetEditorData(onTraj, platform.Size, platform.RawGrayscaleColor);
                }
            }

            ui?.SetSelectedPlatform(platform);
            RefreshHandles();
        }

        public void ApplySelectedPlatformFromUI(
            Vector2 position,
            Vector2 size,
            float rawColor)
        {
            if (selectedPlatform == null)
                return;

            Vector2 delta = position - selectedPlatform.Position;

            selectedPlatform.SetEditorData(
                position,
                size,
                rawColor);

            Trajectory traj = selectedPlatform.GetTrajectory();
            if (traj != null && traj.Config != null && traj.Config.enabled && delta != Vector2.zero)
            {
                traj.Config.originX += delta.x;
                traj.Config.originY += delta.y;
                if (traj.Config.domainMode == TrajectoryDomainMode.SpatialBounds)
                {
                    traj.Config.boundsXMin += delta.x;
                    traj.Config.boundsXMax += delta.x;
                    traj.Config.boundsYMin += delta.y;
                    traj.Config.boundsYMax += delta.y;
                }
                ui?.RefreshSelectedPlatform(selectedPlatform);
            }

            RefreshHandles();
        }

        public void ApplySelectedTrajectoryFromUI(TrajectoryData data)
        {
            if (selectedPlatform == null)
                return;

            Trajectory traj = selectedPlatform.GetTrajectory();
            if (traj == null)
            {
                selectedPlatform.InitializeTrajectory(data);
            }
            else
            {
                traj.SetConfig(data != null ? data.Clone() : null);
            }

            if (data != null && data.enabled && Trajectory.Validate(data) == null)
            {
                Vector2 onTraj = Trajectory.ProjectPointOntoTrajectory(data, selectedPlatform.Position, out _);
                selectedPlatform.SetEditorData(onTraj, selectedPlatform.Size, selectedPlatform.RawGrayscaleColor);
                ui?.RefreshSelectedPlatform(selectedPlatform);
            }

            RefreshHandles();
            LogEditor($"[EDITOR][TRAJECTORY] platform={selectedPlatform.name} enabled={data?.enabled}");
        }

        public void SelectMarker(LevelMarker marker)
        {
            if (marker == null)
            {
                ClearSelection();
                return;
            }

            selectionKind = SelectionKind.Marker;
            selectedMarker = marker;
            selectedPlatform = null;
            activeHandle = HandleKind.None;
            ui?.ClearSelectedPlatform();
            RefreshHandles();
        }

        private void Update()
        {
            if (!editing)
                return;

            Vector2 screenPosition = ScreenPointerPosition();
            bool pointerOverUi = IsPointerOverUi();

            // Middle mouse = pan the editor camera.
            if (MiddlePointerDown())
            {
                if (!pointerOverUi)
                    BeginMapPan(screenPosition);
                else
                    LogEditor($"[EDITOR][PAN_BLOCKED_UI] screen={screenPosition}");
            }

            if (MiddlePointerHeld())
            {
                if (pointerOverUi)
                    CancelMapPan("Pointer entered UI while panning");
                else
                    ContinueMapPan(screenPosition);
            }

            if (MiddlePointerUp())
            {
                EndMapPan();
            }

            if (paletteDragging)
                return;

            if (PointerDown())
            {
                if (!pointerOverUi)
                    BeginMapPointer(screenPosition);
                else
                    LogEditor($"[EDITOR][MAP_POINTER_BLOCKED_UI] screen={screenPosition}");
            }

            if (PointerHeld() && mapPointerDragging)
            {
                if (pointerOverUi)
                {
                    CancelMapPointerDrag("Pointer entered UI while dragging");
                }
                else
                {
                    ContinueMapPointer(screenPosition);
                }
            }

            if (PointerUp())
            {
                EndMapPointerDrag();
            }

            // Delete key shortcut — guarded so typing in an InputField
            // does not accidentally delete the selected platform.
            if (DeleteKeyPressed() && !IsInputFieldFocused())
            {
                DeleteSelectedPlatform();
            }

            // Ensure selected platform center stays on the trajectory line every frame in edit
            if (selectedPlatform != null && !mapPointerDragging)
            {
                Trajectory traj = selectedPlatform.GetTrajectory();
                TrajectoryData cfg = traj != null ? traj.Config : null;
                if (cfg != null && cfg.enabled && Trajectory.Validate(cfg) == null)
                {
                    Vector2 onTraj = Trajectory.ProjectPointOntoTrajectory(cfg, selectedPlatform.Position, out _);
                    if ((onTraj - selectedPlatform.Position).sqrMagnitude > 1e-6f)
                    {
                        selectedPlatform.SetEditorData(onTraj, selectedPlatform.Size, selectedPlatform.RawGrayscaleColor);
                        ui?.RefreshSelectedPlatform(selectedPlatform);
                    }
                }
            }

            RefreshHandles();
        }

        private void BeginMapPointer(Vector2 screenPosition)
        {
            Vector2 world = ScreenToWorld(screenPosition);

            HandleKind handle = HitHandle(world);
            if (handle != HandleKind.None && selectedPlatform != null)
            {
                mapPointerDragging = true;
                activeHandle = handle;
                dragStartWorld = world;
                dragStartPosition = selectedPlatform.Position;
                dragStartSize = selectedPlatform.Size;

                Trajectory traj = selectedPlatform.GetTrajectory();
                TrajectoryData cfg = traj != null ? traj.Config : null;
                if (cfg != null)
                {
                    dragStartTrajOrigin = new Vector2(cfg.originX, cfg.originY);
                    dragStartBoundsMin = new Vector2(cfg.boundsXMin, cfg.boundsYMin);
                    dragStartBoundsMax = new Vector2(cfg.boundsXMax, cfg.boundsYMax);
                    dragStartTrajRotation = cfg.rotation;
                    dragStartTrajScale = cfg.scaleX;
                    dragStartTrajRadius = cfg.radius;
                    dragStartParamMin = cfg.paramMin;
                    dragStartParamMax = cfg.paramMax;
                    Trajectory.ProjectPointOntoTrajectory(cfg, selectedPlatform.Position, out dragStartTrajT);
                }

                LogEditor($"[EDITOR][DRAG_START_HANDLE] platform={selectedPlatform.name} handle={activeHandle} world={world}");
                return;
            }

            ColorPlatform platform = HitPlatform(world);
            if (platform != null)
            {
                SelectPlatform(platform);
                mapPointerDragging = true;
                activeHandle = HandleKind.None;
                dragStartWorld = world;
                dragStartPosition = platform.Position;
                dragStartSize = platform.Size;

                Trajectory traj = platform.GetTrajectory();
                TrajectoryData cfg = traj != null ? traj.Config : null;
                if (cfg != null)
                {
                    dragStartTrajOrigin = new Vector2(cfg.originX, cfg.originY);
                    dragStartBoundsMin = new Vector2(cfg.boundsXMin, cfg.boundsYMin);
                    dragStartBoundsMax = new Vector2(cfg.boundsXMax, cfg.boundsYMax);
                    Trajectory.ProjectPointOntoTrajectory(cfg, platform.Position, out dragStartTrajT);
                }

                LogEditor($"[EDITOR][DRAG_START_PLATFORM] platform={platform.name} world={world}");
                return;
            }

            LevelMarker marker = HitMarker(world);
            if (marker != null)
            {
                SelectMarker(marker);
                mapPointerDragging = true;
                activeHandle = HandleKind.None;
                dragStartWorld = world;
                dragStartPosition = marker.Position;
                dragStartSize = marker.Size;
                LogEditor($"[EDITOR][DRAG_START_MARKER] marker={marker.name} world={world}");
                return;
            }

            mapPointerDragging = false;
            ClearSelection();
        }

        private void ContinueMapPointer(Vector2 screenPosition)
        {
            if (selectionKind == SelectionKind.None)
                return;

            Vector2 world = ScreenToWorld(screenPosition);
            Vector2 delta = world - dragStartWorld;

            if (selectedPlatform != null && selectionKind == SelectionKind.Platform)
            {
                // Platform body dragging
                if (activeHandle == HandleKind.None)
                {
                    selectedPlatform.SetEditorData(
                        dragStartPosition + delta,
                        dragStartSize,
                        selectedPlatform.RawGrayscaleColor);

                    Trajectory traj = selectedPlatform.GetTrajectory();
                    TrajectoryData cfg = traj != null ? traj.Config : null;
                    if (cfg != null && cfg.enabled)
                    {
                        cfg.originX = dragStartTrajOrigin.x + delta.x;
                        cfg.originY = dragStartTrajOrigin.y + delta.y;
                        if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                        {
                            cfg.boundsXMin = dragStartBoundsMin.x + delta.x;
                            cfg.boundsXMax = dragStartBoundsMax.x + delta.x;
                            cfg.boundsYMin = dragStartBoundsMin.y + delta.y;
                            cfg.boundsYMax = dragStartBoundsMax.y + delta.y;
                        }
                    }

                    ui?.RefreshSelectedPlatform(selectedPlatform);
                    RefreshHandles();
                    return;
                }

                // Trajectory handles
                Trajectory pTraj = selectedPlatform.GetTrajectory();
                TrajectoryData tCfg = pTraj != null ? pTraj.Config : null;
                if (tCfg != null && IsTrajHandle(activeHandle))
                {
                    switch (activeHandle)
                    {
                        case HandleKind.TrajOrigin:
                        case HandleKind.TrajCircleCenter:
                            tCfg.originX = dragStartTrajOrigin.x + delta.x;
                            tCfg.originY = dragStartTrajOrigin.y + delta.y;
                            break;

                        case HandleKind.TrajLineEnd:
                            Vector2 lineVec = world - new Vector2(tCfg.originX, tCfg.originY);
                            float pMaxAbs = Mathf.Max(0.001f, Mathf.Abs(tCfg.paramMax));
                            tCfg.scaleX = Mathf.Max(0.1f, lineVec.magnitude / pMaxAbs);
                            float angleDeg = Mathf.Atan2(lineVec.y, lineVec.x) * Mathf.Rad2Deg;
                            if (tCfg.paramMax < 0f) angleDeg += 180f;
                            tCfg.rotation = angleDeg;
                            break;

                        case HandleKind.TrajCircleRadius:
                            Vector2 c = new Vector2(tCfg.originX, tCfg.originY);
                            tCfg.radius = Mathf.Max(0.1f, Vector2.Distance(world, c));
                            break;

                        case HandleKind.TrajArcStart:
                            Vector2 cS = new Vector2(tCfg.originX, tCfg.originY);
                            float aS = Mathf.Atan2(world.y - cS.y, world.x - cS.x) * Mathf.Rad2Deg;
                            if (aS < 0) aS += 360f;
                            tCfg.paramMin = aS;
                            break;

                        case HandleKind.TrajArcEnd:
                            Vector2 cE = new Vector2(tCfg.originX, tCfg.originY);
                            float aE = Mathf.Atan2(world.y - cE.y, world.x - cE.x) * Mathf.Rad2Deg;
                            if (aE < 0) aE += 360f;
                            tCfg.paramMax = aE;
                            break;

                        case HandleKind.TrajBoundsMinX:
                            tCfg.boundsXMin = Mathf.Min(world.x, tCfg.boundsXMax - 0.5f);
                            break;

                        case HandleKind.TrajBoundsMaxX:
                            tCfg.boundsXMax = Mathf.Max(world.x, tCfg.boundsXMin + 0.5f);
                            break;

                        case HandleKind.TrajBoundsMinY:
                            tCfg.boundsYMin = Mathf.Min(world.y, tCfg.boundsYMax - 0.5f);
                            break;

                        case HandleKind.TrajBoundsMaxY:
                            tCfg.boundsYMax = Mathf.Max(world.y, tCfg.boundsYMin + 0.5f);
                            break;
                    }

                    // Keep platform center on the trajectory in real-time per frame
                    Vector2 newPos = Trajectory.EvaluateWorldPoint(tCfg, dragStartTrajT);
                    selectedPlatform.SetEditorData(
                        newPos,
                        selectedPlatform.Size,
                        selectedPlatform.RawGrayscaleColor);

                    ui?.RefreshSelectedPlatform(selectedPlatform);
                    RefreshHandles();
                    return;
                }

                // Platform resize handles
                Vector2 size = dragStartSize;
                Vector2 position = dragStartPosition;

                switch (activeHandle)
                {
                    case HandleKind.Left:
                        size.x = Mathf.Max(0.1f, dragStartSize.x - delta.x);
                        position.x = dragStartPosition.x + delta.x * 0.5f;
                        break;

                    case HandleKind.Right:
                        size.x = Mathf.Max(0.1f, dragStartSize.x + delta.x);
                        position.x = dragStartPosition.x + delta.x * 0.5f;
                        break;

                    case HandleKind.Top:
                        size.y = Mathf.Max(0.1f, dragStartSize.y + delta.y);
                        position.y = dragStartPosition.y + delta.y * 0.5f;
                        break;

                    case HandleKind.Bottom:
                        size.y = Mathf.Max(0.1f, dragStartSize.y - delta.y);
                        position.y = dragStartPosition.y + delta.y * 0.5f;
                        break;
                }

                pTraj = selectedPlatform.GetTrajectory();
                tCfg = pTraj != null ? pTraj.Config : null;
                if (tCfg != null && tCfg.enabled)
                {
                    Vector2 posDelta = position - dragStartPosition;
                    tCfg.originX = dragStartTrajOrigin.x + posDelta.x;
                    tCfg.originY = dragStartTrajOrigin.y + posDelta.y;
                    if (tCfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                    {
                        tCfg.boundsXMin = dragStartBoundsMin.x + posDelta.x;
                        tCfg.boundsXMax = dragStartBoundsMax.x + posDelta.x;
                        tCfg.boundsYMin = dragStartBoundsMin.y + posDelta.y;
                        tCfg.boundsYMax = dragStartBoundsMax.y + posDelta.y;
                    }
                }

                selectedPlatform.SetEditorData(
                    position,
                    size,
                    selectedPlatform.RawGrayscaleColor);

                ui?.RefreshSelectedPlatform(selectedPlatform);
                RefreshHandles();
                return;
            }

            if (selectedMarker != null &&
                selectionKind == SelectionKind.Marker &&
                activeHandle == HandleKind.None)
            {
                selectedMarker.SetEditorPosition(dragStartPosition + delta);
            }
        }

        private void BeginMapPan(Vector2 screenPosition)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                LogEditor("[EDITOR][PAN_START_FAILED] Camera.main = null");
                return;
            }

            mapPanning = true;
            panStartScreen = screenPosition;
            panStartCameraPosition = cam.transform.position;
            LogEditor($"[EDITOR][PAN_START] screen={screenPosition} camera={panStartCameraPosition}");
        }

        private void ContinueMapPan(Vector2 screenPosition)
        {
            if (!mapPanning)
                return;

            if (IsPointerOverUi())
                return;

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            Vector3 p0 = cam.ScreenToWorldPoint(new Vector3(panStartScreen.x, panStartScreen.y, Mathf.Abs(panStartCameraPosition.z)));
            Vector3 p1 = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(panStartCameraPosition.z)));
            Vector3 worldDiff = p1 - p0;

            cam.transform.position = new Vector3(
                panStartCameraPosition.x - worldDiff.x,
                panStartCameraPosition.y - worldDiff.y,
                panStartCameraPosition.z);

            if (logEditorInteraction && Time.frameCount % 10 == 0)
            {
                LogEditor($"[EDITOR][PAN_MOVE] screen={screenPosition} worldDiff={worldDiff} camera={cam.transform.position}");
            }
        }

        private void EndMapPan()
        {
            if (!mapPanning)
                return;

            mapPanning = false;
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            LogEditor($"[EDITOR][PAN_END] camera={(cam != null ? cam.transform.position.ToString() : "null")}");
        }

        private void CancelMapPan(string reason)
        {
            if (!mapPanning)
                return;

            mapPanning = false;
            LogEditor($"[EDITOR][PAN_CANCEL] reason={reason}");
        }

        private void EndMapPointerDrag()
        {
            if (!mapPointerDragging)
                return;

            mapPointerDragging = false;
            activeHandle = HandleKind.None;
            LogEditor("[EDITOR][DRAG_END]");
        }

        private void CancelMapPointerDrag(string reason)
        {
            mapPointerDragging = false;
            activeHandle = HandleKind.None;
            LogEditor($"[EDITOR][DRAG_CANCEL] reason={reason}");
        }

        private void LogEditor(string message)
        {
            if (logEditorInteraction)
                Debug.Log(message);
        }

        private void CreateFromPalette(
            LevelMarkerType type,
            bool marker,
            Vector2 worldPosition)
        {
            if (marker)
            {
                if (type == LevelMarkerType.StartPos &&
                    HasMarker(LevelMarkerType.StartPos))
                {
                    ui?.SetStatus("ERROR: map chỉ được có 1 StartPos.");
                    return;
                }

                if (type == LevelMarkerType.End &&
                    HasMarker(LevelMarkerType.End))
                {
                    ui?.SetStatus("ERROR: map chỉ có 1 End theo JSON schema.");
                    return;
                }

                GameObject go = new GameObject(type.ToString());
                go.transform.SetParent(FindOrCreateEditorRoot(), false);

                LevelMarker markerComponent = go.AddComponent<LevelMarker>();
                markerComponent.Initialize(
                    type,
                    worldPosition,
                    defaultMarkerSize,
                    type == LevelMarkerType.End ? 255f : defaultMarkerColor);

                SelectMarker(markerComponent);
                ui?.SetStatus(type + " added.");
                return;
            }

            GameObject platformObject =
                new GameObject("ColorPlatform_" + ColorPlatform.All.Count);
            platformObject.transform.SetParent(FindOrCreatePlatformRoot(), false);

            ColorPlatform platform = platformObject.AddComponent<ColorPlatform>();
            platform.Initialize(
                worldPosition,
                defaultPlatformSize,
                defaultPlatformColor);

            // Initialize disabled trajectory for new platform.
            platform.InitializeTrajectory(null);

            SelectPlatform(platform);
            ui?.SetStatus("Platform added.");
        }

        private bool ValidateImportedData(LevelData data, out string error)
        {
            error = null;
            if (data.platforms != null)
            {
                for (int i = 0; i < data.platforms.Length; i++)
                {
                    if (data.platforms[i] == null)
                    {
                        error = "JSON chứa platform null tại index " + i + ".";
                        return false;
                    }
                }
            }
            return true;
        }

        public LevelData BuildExportData()
        {
            LevelData source = levelLoader != null ? levelLoader.CurrentLevelData : null;
            LevelData data = source ?? new LevelData();

            // Filter out any null/destroyed entries that might remain.
            ColorPlatform.All.RemoveAll(p => p == null || p.gameObject == null);

            data.platforms = new ColorPlatformData[ColorPlatform.All.Count];
            data.spawnPoint = null;

            for (int i = 0; i < ColorPlatform.All.Count; i++)
            {
                ColorPlatform p = ColorPlatform.All[i];
                Trajectory traj = p.GetTrajectory();
                TrajectoryData trajData = null;
                Vector2 exportPos = p.Position;

                if (traj != null && traj.Config != null && traj.Config.enabled)
                {
                    trajData = traj.Config.Clone();
                    // Authoritative coordinate contract: saved position is the initial evaluated point
                    exportPos = Trajectory.GetInitialPosition(trajData);
                }

                data.platforms[i] = new ColorPlatformData
                {
                    position = new Vector2Data(exportPos.x, exportPos.y),
                    size = new Vector2Data(p.Size.x, p.Size.y),
                    rawGrayscaleColor = p.RawGrayscaleColor,
                    trajectory = trajData
                };
            }

            data.startPos = null;
            data.checkPoint = null;
            data.end = null;

            List<CheckPointData> checkpoints = new List<CheckPointData>();

            for (int i = 0; i < LevelMarker.All.Count; i++)
            {
                LevelMarker marker = LevelMarker.All[i];
                if (marker == null)
                    continue;

                Vector2Data pos = new Vector2Data(marker.Position.x, marker.Position.y);
                Vector2Data size = new Vector2Data(marker.Size.x, marker.Size.y);

                switch (marker.Type)
                {
                    case LevelMarkerType.StartPos:
                        data.startPos = new StartPosData
                        {
                            position = pos,
                            size = size,
                            color = marker.RawColor
                        };
                        break;

                    case LevelMarkerType.CheckPoint:
                        checkpoints.Add(new CheckPointData
                        {
                            position = pos,
                            size = size,
                            color = marker.RawColor
                        });
                        break;

                    case LevelMarkerType.End:
                        data.end = new EndData
                        {
                            position = pos,
                            size = size
                        };
                        break;
                }
            }

            data.checkPoint = checkpoints.ToArray();
            return data;
        }

        private bool HasMarker(LevelMarkerType type)
        {
            for (int i = 0; i < LevelMarker.All.Count; i++)
            {
                LevelMarker marker = LevelMarker.All[i];
                if (marker != null && marker.Type == type && !marker.IsConsumed)
                    return true;
            }
            return false;
        }

        private ColorPlatform HitPlatform(Vector2 world)
        {
            for (int i = ColorPlatform.All.Count - 1; i >= 0; i--)
            {
                ColorPlatform platform = ColorPlatform.All[i];
                if (platform == null)
                    continue;

                Rect rect = platform.GetAabb();
                if (rect.Contains(world))
                    return platform;
            }

            return null;
        }

        private LevelMarker HitMarker(Vector2 world)
        {
            for (int i = LevelMarker.All.Count - 1; i >= 0; i--)
            {
                LevelMarker marker = LevelMarker.All[i];
                if (marker == null)
                    continue;

                Contrast.Player.Aabb bounds = marker.GetAabb();
                if (world.x >= bounds.Min.x &&
                    world.x <= bounds.Max.x &&
                    world.y >= bounds.Min.y &&
                    world.y <= bounds.Max.y)
                {
                    return marker;
                }
            }

            return null;
        }

        private HandleKind HitHandle(Vector2 world)
        {
            if (selectedPlatform == null)
                return HandleKind.None;

            // Platform resize handles
            Rect r = selectedPlatform.GetAabb();
            Vector2 left = new Vector2(r.xMin, r.center.y);
            Vector2 right = new Vector2(r.xMax, r.center.y);
            Vector2 top = new Vector2(r.center.x, r.yMax);
            Vector2 bottom = new Vector2(r.center.x, r.yMin);

            if (Vector2.Distance(world, left) <= handleHitRadius)
                return HandleKind.Left;
            if (Vector2.Distance(world, right) <= handleHitRadius)
                return HandleKind.Right;
            if (Vector2.Distance(world, top) <= handleHitRadius)
                return HandleKind.Top;
            if (Vector2.Distance(world, bottom) <= handleHitRadius)
                return HandleKind.Bottom;

            // Trajectory handles
            Trajectory traj = selectedPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            if (cfg != null && cfg.enabled && Trajectory.Validate(cfg) == null)
            {
                if (cfg.curveType == TrajectoryCurveType.Line)
                {
                    Vector2 origin = new Vector2(cfg.originX, cfg.originY);
                    if (Vector2.Distance(world, origin) <= handleHitRadius)
                        return HandleKind.TrajOrigin;

                    if (cfg.domainMode == TrajectoryDomainMode.ParameterRange)
                    {
                        Vector2 lineEnd = Trajectory.EvaluateWorldPoint(cfg, 1f);
                        if (Vector2.Distance(world, lineEnd) <= handleHitRadius)
                            return HandleKind.TrajLineEnd;
                    }
                    else if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                    {
                        Vector2 bMinX = new Vector2(cfg.boundsXMin, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f);
                        Vector2 bMaxX = new Vector2(cfg.boundsXMax, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f);
                        Vector2 bMinY = new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMin);
                        Vector2 bMaxY = new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMax);

                        if (Vector2.Distance(world, bMinX) <= handleHitRadius) return HandleKind.TrajBoundsMinX;
                        if (Vector2.Distance(world, bMaxX) <= handleHitRadius) return HandleKind.TrajBoundsMaxX;
                        if (Vector2.Distance(world, bMinY) <= handleHitRadius) return HandleKind.TrajBoundsMinY;
                        if (Vector2.Distance(world, bMaxY) <= handleHitRadius) return HandleKind.TrajBoundsMaxY;
                    }
                }
                else if (cfg.curveType == TrajectoryCurveType.Circle)
                {
                    Vector2 center = new Vector2(cfg.originX, cfg.originY);
                    if (Vector2.Distance(world, center) <= handleHitRadius)
                        return HandleKind.TrajCircleCenter;

                    Vector2 radPos = center + Vector2.right * cfg.radius;
                    if (Vector2.Distance(world, radPos) <= handleHitRadius)
                        return HandleKind.TrajCircleRadius;

                    float angleSpan = Mathf.Abs(cfg.paramMax - cfg.paramMin);
                    if (angleSpan < 355f)
                    {
                        Vector2 arcStart = Trajectory.EvaluateWorldPoint(cfg, 0f);
                        Vector2 arcEnd = Trajectory.EvaluateWorldPoint(cfg, 1f);

                        if (Vector2.Distance(world, arcStart) <= handleHitRadius) return HandleKind.TrajArcStart;
                        if (Vector2.Distance(world, arcEnd) <= handleHitRadius) return HandleKind.TrajArcEnd;
                    }
                }
            }

            return HandleKind.None;
        }

        private static bool IsTrajHandle(HandleKind kind)
        {
            return kind >= HandleKind.TrajOrigin && kind <= HandleKind.TrajBoundsMaxY;
        }

        public void RefreshHandles()
        {
            EnsureHandleRoot();

            if (selectedPlatform == null || !editing)
            {
                SetHandlesVisible(false);
                return;
            }

            // Platform resize handles
            Rect r = selectedPlatform.GetAabb();
            SetHandle(HandleKind.Left, new Vector2(r.xMin, r.center.y), true);
            SetHandle(HandleKind.Right, new Vector2(r.xMax, r.center.y), true);
            SetHandle(HandleKind.Top, new Vector2(r.center.x, r.yMax), true);
            SetHandle(HandleKind.Bottom, new Vector2(r.center.x, r.yMin), true);

            // Trajectory preview and handles
            Trajectory traj = selectedPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;

            if (cfg != null && cfg.enabled && Trajectory.Validate(cfg) == null)
            {
                // Draw curve preview
                Vector2[] pts = Trajectory.GetPreviewPoints(cfg, 64);
                if (trajectoryLineRenderer != null)
                {
                    trajectoryLineRenderer.positionCount = pts.Length;
                    for (int i = 0; i < pts.Length; i++)
                        trajectoryLineRenderer.SetPosition(i, new Vector3(pts[i].x, pts[i].y, -0.8f));
                }

                // Spatial bounds preview
                if (cfg.curveType == TrajectoryCurveType.Line &&
                    cfg.domainMode == TrajectoryDomainMode.SpatialBounds &&
                    boundsLineRenderer != null)
                {
                    boundsLineRenderer.positionCount = 5;
                    boundsLineRenderer.SetPosition(0, new Vector3(cfg.boundsXMin, cfg.boundsYMin, -0.7f));
                    boundsLineRenderer.SetPosition(1, new Vector3(cfg.boundsXMax, cfg.boundsYMin, -0.7f));
                    boundsLineRenderer.SetPosition(2, new Vector3(cfg.boundsXMax, cfg.boundsYMax, -0.7f));
                    boundsLineRenderer.SetPosition(3, new Vector3(cfg.boundsXMin, cfg.boundsYMax, -0.7f));
                    boundsLineRenderer.SetPosition(4, new Vector3(cfg.boundsXMin, cfg.boundsYMin, -0.7f));

                    SetHandle(HandleKind.TrajBoundsMinX, new Vector2(cfg.boundsXMin, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f), true);
                    SetHandle(HandleKind.TrajBoundsMaxX, new Vector2(cfg.boundsXMax, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f), true);
                    SetHandle(HandleKind.TrajBoundsMinY, new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMin), true);
                    SetHandle(HandleKind.TrajBoundsMaxY, new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMax), true);
                }
                else
                {
                    if (boundsLineRenderer != null) boundsLineRenderer.positionCount = 0;
                    HideHandle(HandleKind.TrajBoundsMinX);
                    HideHandle(HandleKind.TrajBoundsMaxX);
                    HideHandle(HandleKind.TrajBoundsMinY);
                    HideHandle(HandleKind.TrajBoundsMaxY);
                }

                if (cfg.curveType == TrajectoryCurveType.Line)
                {
                    SetHandle(HandleKind.TrajOrigin, new Vector2(cfg.originX, cfg.originY), true);
                    if (cfg.domainMode == TrajectoryDomainMode.ParameterRange)
                    {
                        SetHandle(HandleKind.TrajLineEnd, Trajectory.EvaluateWorldPoint(cfg, 1f), true);
                    }
                    else
                    {
                        HideHandle(HandleKind.TrajLineEnd);
                    }

                    HideHandle(HandleKind.TrajCircleCenter);
                    HideHandle(HandleKind.TrajCircleRadius);
                    HideHandle(HandleKind.TrajArcStart);
                    HideHandle(HandleKind.TrajArcEnd);
                }
                else if (cfg.curveType == TrajectoryCurveType.Circle)
                {
                    SetHandle(HandleKind.TrajCircleCenter, new Vector2(cfg.originX, cfg.originY), true);
                    SetHandle(HandleKind.TrajCircleRadius, new Vector2(cfg.originX + cfg.radius, cfg.originY), true);

                    float angleSpan = Mathf.Abs(cfg.paramMax - cfg.paramMin);
                    if (angleSpan < 355f)
                    {
                        SetHandle(HandleKind.TrajArcStart, Trajectory.EvaluateWorldPoint(cfg, 0f), true);
                        SetHandle(HandleKind.TrajArcEnd, Trajectory.EvaluateWorldPoint(cfg, 1f), true);
                    }
                    else
                    {
                        HideHandle(HandleKind.TrajArcStart);
                        HideHandle(HandleKind.TrajArcEnd);
                    }

                    HideHandle(HandleKind.TrajOrigin);
                    HideHandle(HandleKind.TrajLineEnd);
                }
            }
            else
            {
                if (trajectoryLineRenderer != null) trajectoryLineRenderer.positionCount = 0;
                if (boundsLineRenderer != null) boundsLineRenderer.positionCount = 0;

                HideHandle(HandleKind.TrajOrigin);
                HideHandle(HandleKind.TrajLineEnd);
                HideHandle(HandleKind.TrajCircleCenter);
                HideHandle(HandleKind.TrajCircleRadius);
                HideHandle(HandleKind.TrajArcStart);
                HideHandle(HandleKind.TrajArcEnd);
                HideHandle(HandleKind.TrajBoundsMinX);
                HideHandle(HandleKind.TrajBoundsMaxX);
                HideHandle(HandleKind.TrajBoundsMinY);
                HideHandle(HandleKind.TrajBoundsMaxY);
            }
        }

        private void SetHandle(HandleKind kind, Vector2 position, bool visible)
        {
            if (!handles.TryGetValue(kind, out GameObject go) || go == null)
                return;

            go.transform.position = new Vector3(position.x, position.y, -1f);
            go.transform.localScale = Vector3.one * handleSize;
            go.SetActive(visible);
        }

        private void HideHandle(HandleKind kind)
        {
            if (handles.TryGetValue(kind, out GameObject go) && go != null)
                go.SetActive(false);
        }

        private void SetHandlesVisible(bool visible)
        {
            foreach (GameObject go in handles.Values)
            {
                if (go != null)
                    go.SetActive(visible);
            }

            if (!visible)
            {
                if (trajectoryLineRenderer != null) trajectoryLineRenderer.positionCount = 0;
                if (boundsLineRenderer != null) boundsLineRenderer.positionCount = 0;
            }
        }

        private void EnsureHandleRoot()
        {
            if (handleRoot != null)
                return;

            handleRoot = new GameObject("EditorHandles");
            handleRoot.transform.SetParent(transform, false);

            // Platform resize handles
            CreateHandle(HandleKind.Left, UnityEngine.Color.white);
            CreateHandle(HandleKind.Right, UnityEngine.Color.white);
            CreateHandle(HandleKind.Top, UnityEngine.Color.white);
            CreateHandle(HandleKind.Bottom, UnityEngine.Color.white);

            // Trajectory control handles
            CreateHandle(HandleKind.TrajOrigin, new UnityEngine.Color(1f, 0.9f, 0.2f, 1f));
            CreateHandle(HandleKind.TrajLineEnd, new UnityEngine.Color(0.2f, 0.9f, 1f, 1f));
            CreateHandle(HandleKind.TrajCircleCenter, new UnityEngine.Color(1f, 0.9f, 0.2f, 1f));
            CreateHandle(HandleKind.TrajCircleRadius, new UnityEngine.Color(1f, 0.3f, 0.8f, 1f));
            CreateHandle(HandleKind.TrajArcStart, new UnityEngine.Color(0.4f, 1f, 0.4f, 1f));
            CreateHandle(HandleKind.TrajArcEnd, new UnityEngine.Color(0.4f, 1f, 0.4f, 1f));
            CreateHandle(HandleKind.TrajBoundsMinX, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));
            CreateHandle(HandleKind.TrajBoundsMaxX, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));
            CreateHandle(HandleKind.TrajBoundsMinY, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));
            CreateHandle(HandleKind.TrajBoundsMaxY, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));

            // Line renderers for preview
            trajectoryLineRenderer = CreatePreviewLine("TrajectoryPreview", new UnityEngine.Color(0.2f, 1f, 0.4f, 0.85f), 0.06f);
            boundsLineRenderer = CreatePreviewLine("BoundsPreview", new UnityEngine.Color(1f, 0.7f, 0.2f, 0.75f), 0.04f);
        }

        private void CreateHandle(HandleKind kind, UnityEngine.Color color)
        {
            GameObject go = new GameObject(kind + "Handle");
            go.transform.SetParent(handleRoot.transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ColorPlatform.GetDefaultSprite();
            sr.sortingOrder = 1005;
            sr.color = color;

            handles[kind] = go;
            go.SetActive(false);
        }

        private LineRenderer CreatePreviewLine(string name, UnityEngine.Color color, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(handleRoot.transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sortingOrder = 990;
            lr.startWidth = width;
            lr.endWidth = width;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lr.material = new Material(shader);

            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = 0;
            return lr;
        }

        private void ClearSelection()
        {
            selectionKind = SelectionKind.None;
            selectedPlatform = null;
            selectedMarker = null;
            activeHandle = HandleKind.None;
            ui?.ClearSelectedPlatform();
            SetHandlesVisible(false);
        }

        /// <summary>
        /// Delete the currently selected platform. Safe to call when nothing
        /// is selected (no-op). Never deletes a LevelMarker.
        /// </summary>
        public void DeleteSelectedPlatform()
        {
            if (selectionKind != SelectionKind.Platform || selectedPlatform == null)
                return;

            ColorPlatform toDelete = selectedPlatform;
            string name = toDelete.name;

            // Clear selection first so UI references and handles are cleaned up.
            ClearSelection();

            // Remove from registry immediately.
            ColorPlatform.All.Remove(toDelete);

            // Destroy GameObject.
            Destroy(toDelete.gameObject);

            LogEditor($"[EDITOR][DELETE] platform={name}");
            ui?.SetStatus($"Deleted {name}.");
        }

        private Transform FindOrCreatePlatformRoot()
        {
            if (levelLoader != null)
            {
                Transform root = levelLoader.PlatformsRoot;
                if (root != null)
                    return root;
            }

            GameObject go = GameObject.Find("Platforms");
            if (go != null)
                return go.transform;

            return new GameObject("Platforms").transform;
        }

        private Transform FindOrCreateEditorRoot()
        {
            if (levelLoader != null)
            {
                Transform root = levelLoader.LevelObjectsRoot;
                if (root != null)
                    return root;
            }

            GameObject go = GameObject.Find("LevelObjects");
            if (go != null)
                return go.transform;

            return new GameObject("LevelObjects").transform;
        }

        private void EnsureDependencies()
        {
            levelLoader ??= FindAnyObjectByType<LevelLoader>();
            ui ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
        }

        private static Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
                return screenPosition;

            Vector3 screen = new Vector3(
                screenPosition.x,
                screenPosition.y,
                Mathf.Abs(cam.transform.position.z));

            Vector3 world = cam.ScreenToWorldPoint(screen);
            return new Vector2(world.x, world.y);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject();
        }

        private static Vector2 ScreenPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        private static bool MiddlePointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.middleButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(2);
#endif
        }

        private static bool MiddlePointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.middleButton.isPressed;
#else
            return Input.GetMouseButton(2);
#endif
        }

        private static bool MiddlePointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.middleButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(2);
#endif
        }

        private static bool PointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool PointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        private static bool PointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        private static bool DeleteKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null &&
                   (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace);
#endif
        }

        /// <summary>
        /// Returns true when a UI InputField currently has focus,
        /// preventing the Delete key from deleting a platform while
        /// the user is typing in a text field.
        /// </summary>
        private static bool IsInputFieldFocused()
        {
            if (EventSystem.current == null)
                return false;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null)
                return false;

            return selected.GetComponent<UnityEngine.UI.InputField>() != null;
        }
    }
}
