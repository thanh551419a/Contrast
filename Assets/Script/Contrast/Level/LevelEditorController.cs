using System;
using System.Collections.Generic;
using System.IO;
using Contrast.Camera;
using Contrast.Core;
using Contrast.Data;
using Contrast.Level.EditorInteraction;
using Contrast.Player;
using Contrast.UI;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Contrast.Level
{
    /// <summary>
    /// Runtime level editor controller. Coordinates edit mode lifecycle, viewport input routing,
    /// selection state, platform and marker manipulation, import/export, and delegates trajectory
    /// editing to <see cref="TrajectoryEditController"/>.
    /// </summary>
    public sealed class LevelEditorController : MonoBehaviour
    {
        private enum SelectionKind
        {
            None,
            Platform,
            Marker
        }

        private enum ResizeHandleKind
        {
            None,
            Left,
            Right,
            Top,
            Bottom
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
        private ResizeHandleKind activeResizeHandle;
        private TrajectoryHandleKind activeTrajHandle;

        private Vector2 dragStartWorld;
        private Vector2 dragStartPosition;
        private Vector2 dragStartSize;

        // Platform & lockstep trajectory drag initial state
        private Vector2 dragStartTrajOrigin;
        private Vector2 dragStartBoundsMin;
        private Vector2 dragStartBoundsMax;

        private GameObject handleRoot;
        private GameObject selectionBox;
        private SpriteRenderer selectionBoxRenderer;
        private static Sprite cachedHandleSprite;
        private static Sprite cachedSelectionBoxSprite;
        private readonly Dictionary<ResizeHandleKind, GameObject> resizeHandles =
            new Dictionary<ResizeHandleKind, GameObject>();

        private readonly EditorInputRouter inputRouter = new EditorInputRouter();
        private TrajectoryEditController trajectoryController;

        public bool IsEditing => editing;
        public ColorPlatform SelectedPlatform => selectedPlatform;
        public TrajectoryEditController TrajectoryController => trajectoryController;
        public EditorInputRouter InputRouter => inputRouter;

        private LevelData editorWorkingCopy;

        private void Awake()
        {
            levelLoader ??= FindAnyObjectByType<LevelLoader>();
            ui ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
            ui?.SetController(this);
            trajectoryController ??= new TrajectoryEditController(handleSize, handleHitRadius);
        }

        public void StartEditing()
        {
            editing = true;
            EnsureDependencies();
            trajectoryController ??= new TrajectoryEditController(handleSize, handleHitRadius);

            // Rebuild scene from working copy if returning from gameplay or editing
            if (editorWorkingCopy != null)
            {
                levelLoader?.BuildRuntimeLevel(editorWorkingCopy);
            }
            else if (levelLoader != null && levelLoader.CurrentLevelData != null)
            {
                editorWorkingCopy = BuildExportData();
            }

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
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;
            inputRouter.ForceReleasePointer();
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

            if (inputRouter.IsPointerOverUi())
                return;

            Vector2 world = inputRouter.ScreenToWorld(screenPosition);
            CreateFromPalette(pendingPaletteType, pendingPaletteIsMarker, world);
        }

        public void RequestImport()
        {
            if (!editing)
                return;

#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel(
                "Import Contrast Level",
                LevelRepository.GetLevelsDirectory(),
                "json");

            if (!string.IsNullOrEmpty(path))
            {
                ImportJson(path);
                return;
            }
#endif

            // In standalone build (or if user cancelled in-editor file picker):
            if (GameManager.Instance != null && GameManager.Instance.LevelSelectUI != null)
            {
                GameManager.Instance.LevelSelectUI.OpenForImport((selectedPath) =>
                {
                    ImportJson(selectedPath);
                });
            }
            else
            {
                ui?.SetStatus("Không thể mở giao diện chọn file.");
            }
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
                Debug.Log($"[FLOW][LEVEL_FILE] load path={path}");
                string rawJson = LevelRepository.ReadAllText(path);
                LevelData data = JsonUtility.FromJson<LevelData>(rawJson);

                if (data == null)
                {
                    ui?.SetStatus("Không thể đọc LevelData từ JSON.");
                    return;
                }

                data = LevelDataSanitizer.Sanitize(data, rawJson, out List<string> report);

                if (report != null && report.Count > 0)
                {
                    Debug.Log($"[LEVEL_FIX] Automatically fixed {report.Count} issues.");
                    foreach (string item in report)
                        Debug.Log($"[LEVEL_FIX] {item}");
                }

                EnsureDependencies();
                levelLoader?.BuildRuntimeLevel(data);
                editorWorkingCopy = data;
                ClearSelection();

                string statusMsg = (report != null && report.Count > 0)
                    ? $"Import level thành công (đã sửa {report.Count} lỗi)."
                    : "Import level thành công.";
                ui?.SetStatus(statusMsg);
            }
            catch (Exception ex)
            {
                ui?.SetStatus("Import lỗi: " + ex.Message);
                Debug.LogError($"[LEVEL_EDITOR] Import error: {ex.Message}");
            }
        }

        public void RequestExport(string desiredFileName = null)
        {
            if (!editing)
                return;

            LevelData data = BuildExportData();
            editorWorkingCopy = data;
            string json = LevelDataSanitizer.SerializeToJson(data);

            if (string.IsNullOrWhiteSpace(desiredFileName))
                desiredFileName = ui != null ? ui.GetExportFileName() : null;

            string savePath = LevelRepository.GetUniqueSavePath(desiredFileName);
            LevelRepository.WriteAllText(savePath, json);

            string fileNameOnly = Path.GetFileName(savePath);
            ui?.SetExportFileName(fileNameOnly);
            ui?.SetStatus("Export: " + savePath);
            Debug.Log("[LEVEL_EXPORT] Saved: " + savePath);
        }

        public void RequestPlayLevel()
        {
            if (!editing)
                return;

            LevelData data = BuildExportData();
            editorWorkingCopy = data;
            string snapshotJson = LevelDataSanitizer.SerializeToJson(data);
            ClearSelection();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGameWithLevelData(data, snapshotJson, GameplayEntryContext.EditorTestPlay);
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
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;

            trajectoryController.SetTarget(platform, ui);
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
                trajectoryController.ApplyLockstepOffset(
                    new Vector2(traj.Config.originX, traj.Config.originY),
                    new Vector2(traj.Config.boundsXMin, traj.Config.boundsYMin),
                    new Vector2(traj.Config.boundsXMax, traj.Config.boundsYMax),
                    delta,
                    ui);
            }

            RefreshHandles();
            ui?.RefreshSelectedPlatform(selectedPlatform);
        }

        public void ApplySelectedTrajectoryFromUI(TrajectoryData data)
        {
            if (selectedPlatform == null)
                return;

            trajectoryController.ApplyFromUI(data, ui);
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
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;

            trajectoryController.ClearTarget(ui);
            ui?.ClearSelectedPlatform();
            RefreshHandles();
        }

        private void Update()
        {
            if (!editing)
                return;

            Vector2 screenPosition = inputRouter.GetScreenPointerPosition();

            // ── Escape Key: Cancel ongoing interaction / preview ─────
            if (inputRouter.IsCancelShortcutPressed())
            {
                if (mapPointerDragging)
                {
                    CancelMapPointerDrag("Escape key pressed during drag");
                }
                else if (mapPanning)
                {
                    CancelMapPan("Escape key pressed during pan");
                }
                ui?.CancelResetConfirm();
            }

            // ── Middle mouse: camera pan ─────────────────────────────
            if (inputRouter.IsMiddlePointerDown())
            {
                if (inputRouter.CanBeginViewportInteraction())
                {
                    BeginMapPan(screenPosition);
                }
                else
                {
                    LogEditor($"[EDITOR][PAN_BLOCKED_UI] screen={screenPosition}");
                }
            }

            if (inputRouter.IsMiddlePointerHeld())
            {
                if (mapPanning && inputRouter.IsOwner("pan"))
                {
                    ContinueMapPan(screenPosition);
                }
            }

            if (inputRouter.IsMiddlePointerUp())
            {
                if (mapPanning && inputRouter.IsOwner("pan"))
                {
                    EndMapPan();
                }
            }

            if (paletteDragging)
                return;

            // ── Left pointer: viewport interaction ───────────────────
            if (inputRouter.IsLeftPointerDown())
            {
                if (inputRouter.CanBeginViewportInteraction())
                {
                    BeginMapPointer(screenPosition);
                }
                else
                {
                    LogEditor($"[EDITOR][MAP_POINTER_BLOCKED_UI] screen={screenPosition}");
                }
            }

            if (inputRouter.IsLeftPointerHeld() && mapPointerDragging)
            {
                // Once captured, the drag continues even if the pointer sweeps across UI!
                ContinueMapPointer(screenPosition);
            }

            if (inputRouter.IsLeftPointerUp())
            {
                if (mapPointerDragging)
                {
                    EndMapPointerDrag();
                }
            }

            // ── Delete key shortcut (guarded against focused InputField)
            if (inputRouter.IsDeleteShortcutPressed())
            {
                DeleteSelectedPlatform();
            }

            // ── Frame All / Fit Map shortcut ('F') ───────────────────
            if (inputRouter.IsFrameAllShortcutPressed())
            {
                FitMap();
            }

            // ── Mouse scroll: camera zoom ────────────────────────────
            float scroll = inputRouter.GetScrollDelta();
            if (Mathf.Abs(scroll) > 0.01f && !inputRouter.IsPointerOverUi())
            {
                ZoomCamera(scroll, screenPosition);
            }

            // ── Idle platform constraint on trajectory ───────────────
            if (!mapPointerDragging)
            {
                trajectoryController.EnsurePlatformOnTrajectory(ui);
            }

            RefreshHandles();
        }

        private void BeginMapPointer(Vector2 screenPosition)
        {
            Vector2 world = inputRouter.ScreenToWorld(screenPosition);

            // Defocus any active UI InputField so that uncommitted text is committed
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // 1. Hit-test trajectory handles first if a platform with trajectory is selected
            TrajectoryHandleKind trajHandle = trajectoryController.HitHandle(world);
            if (trajHandle != TrajectoryHandleKind.None && selectedPlatform != null)
            {
                activeTrajHandle = trajHandle;
                activeResizeHandle = ResizeHandleKind.None;
                dragStartWorld = world;

                if (trajectoryController.BeginDrag(trajHandle, world))
                {
                    mapPointerDragging = true;
                    inputRouter.CapturePointer(this);
                    LogEditor($"[EDITOR][DRAG_START_HANDLE] platform={selectedPlatform.name} handle={activeTrajHandle} world={world}");
                    return;
                }
            }

            // 2. Hit-test platform resize handles
            ResizeHandleKind resizeHandle = HitResizeHandle(world);
            if (resizeHandle != ResizeHandleKind.None && selectedPlatform != null)
            {
                mapPointerDragging = true;
                activeResizeHandle = resizeHandle;
                activeTrajHandle = TrajectoryHandleKind.None;
                dragStartWorld = world;
                dragStartPosition = selectedPlatform.Position;
                dragStartSize = selectedPlatform.Size;
                CaptureTrajectoryDragStart();

                inputRouter.CapturePointer(this);
                LogEditor($"[EDITOR][DRAG_START_RESIZE] platform={selectedPlatform.name} handle={activeResizeHandle} world={world}");
                return;
            }

            // 3. Hit-test platform body
            ColorPlatform platform = HitPlatform(world);
            if (platform != null)
            {
                SelectPlatform(platform);
                mapPointerDragging = true;
                activeResizeHandle = ResizeHandleKind.None;
                activeTrajHandle = TrajectoryHandleKind.None;
                dragStartWorld = world;
                dragStartPosition = platform.Position;
                dragStartSize = platform.Size;
                CaptureTrajectoryDragStart();

                inputRouter.CapturePointer(this);
                LogEditor($"[EDITOR][DRAG_START_PLATFORM] platform={platform.name} world={world}");
                return;
            }

            // 4. Hit-test level markers
            LevelMarker marker = HitMarker(world);
            if (marker != null)
            {
                SelectMarker(marker);
                mapPointerDragging = true;
                activeResizeHandle = ResizeHandleKind.None;
                activeTrajHandle = TrajectoryHandleKind.None;
                dragStartWorld = world;
                dragStartPosition = marker.Position;
                dragStartSize = marker.Size;

                inputRouter.CapturePointer(this);
                LogEditor($"[EDITOR][DRAG_START_MARKER] marker={marker.name} world={world}");
                return;
            }

            mapPointerDragging = false;
            ClearSelection();
        }

        private void CaptureTrajectoryDragStart()
        {
            if (selectedPlatform == null)
                return;

            Trajectory traj = selectedPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            if (cfg != null)
            {
                dragStartTrajOrigin = new Vector2(cfg.originX, cfg.originY);
                dragStartBoundsMin = new Vector2(cfg.boundsXMin, cfg.boundsYMin);
                dragStartBoundsMax = new Vector2(cfg.boundsXMax, cfg.boundsYMax);
            }
        }

        private void ContinueMapPointer(Vector2 screenPosition)
        {
            if (selectionKind == SelectionKind.None)
                return;

            Vector2 world = inputRouter.ScreenToWorld(screenPosition);
            Vector2 delta = world - dragStartWorld;

            // 1. Trajectory handle dragging: delegated to TrajectoryEditController
            if (activeTrajHandle != TrajectoryHandleKind.None)
            {
                trajectoryController.UpdateDrag(world, ui);
                RefreshHandles();
                return;
            }

            if (selectedPlatform != null && selectionKind == SelectionKind.Platform)
            {
                // 2. Platform body dragging
                if (activeResizeHandle == ResizeHandleKind.None)
                {
                    selectedPlatform.SetEditorData(
                        dragStartPosition + delta,
                        dragStartSize,
                        selectedPlatform.RawGrayscaleColor);

                    trajectoryController.ApplyLockstepOffset(
                        dragStartTrajOrigin,
                        dragStartBoundsMin,
                        dragStartBoundsMax,
                        delta,
                        ui);

                    ui?.RefreshSelectedPlatform(selectedPlatform);
                    RefreshHandles();
                    return;
                }

                // 3. Platform resize handles
                Vector2 size = dragStartSize;
                Vector2 position = dragStartPosition;

                switch (activeResizeHandle)
                {
                    case ResizeHandleKind.Left:
                        size.x = Mathf.Max(0.1f, dragStartSize.x - delta.x);
                        position.x = dragStartPosition.x + delta.x * 0.5f;
                        break;

                    case ResizeHandleKind.Right:
                        size.x = Mathf.Max(0.1f, dragStartSize.x + delta.x);
                        position.x = dragStartPosition.x + delta.x * 0.5f;
                        break;

                    case ResizeHandleKind.Top:
                        size.y = Mathf.Max(0.1f, dragStartSize.y + delta.y);
                        position.y = dragStartPosition.y + delta.y * 0.5f;
                        break;

                    case ResizeHandleKind.Bottom:
                        size.y = Mathf.Max(0.1f, dragStartSize.y - delta.y);
                        position.y = dragStartPosition.y + delta.y * 0.5f;
                        break;
                }

                Vector2 posDelta = position - dragStartPosition;
                trajectoryController.ApplyLockstepOffset(
                    dragStartTrajOrigin,
                    dragStartBoundsMin,
                    dragStartBoundsMax,
                    posDelta,
                    ui);

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
                activeResizeHandle == ResizeHandleKind.None)
            {
                selectedMarker.SetEditorPosition(dragStartPosition + delta);
            }
        }

        private void EndMapPointerDrag()
        {
            if (!mapPointerDragging)
                return;

            if (activeTrajHandle != TrajectoryHandleKind.None)
            {
                trajectoryController.CommitDrag(ui);
            }

            mapPointerDragging = false;
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;
            inputRouter.ReleasePointer(this);
            RefreshHandles();
            LogEditor("[EDITOR][DRAG_END]");
        }

        private void CancelMapPointerDrag(string reason)
        {
            if (!mapPointerDragging)
                return;

            if (activeTrajHandle != TrajectoryHandleKind.None)
            {
                trajectoryController.CancelDrag(ui);
            }
            else if (selectedPlatform != null)
            {
                // Revert platform transform
                selectedPlatform.SetEditorData(
                    dragStartPosition,
                    dragStartSize,
                    selectedPlatform.RawGrayscaleColor);

                trajectoryController.ApplyLockstepOffset(
                    dragStartTrajOrigin,
                    dragStartBoundsMin,
                    dragStartBoundsMax,
                    Vector2.zero,
                    ui);
            }
            else if (selectedMarker != null)
            {
                selectedMarker.SetEditorPosition(dragStartPosition);
            }

            mapPointerDragging = false;
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;
            inputRouter.ReleasePointer(this);
            RefreshHandles();
            LogEditor($"[EDITOR][DRAG_CANCEL] reason={reason}");
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
            inputRouter.CapturePointer("pan");
            LogEditor($"[EDITOR][PAN_START] screen={screenPosition} camera={panStartCameraPosition}");
        }

        private void ContinueMapPan(Vector2 screenPosition)
        {
            if (!mapPanning)
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
            inputRouter.ReleasePointer("pan");
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            LogEditor($"[EDITOR][PAN_END] camera={(cam != null ? cam.transform.position.ToString() : "null")}");
        }

        private void CancelMapPan(string reason)
        {
            if (!mapPanning)
                return;

            mapPanning = false;
            inputRouter.ReleasePointer("pan");
            LogEditor($"[EDITOR][PAN_CANCEL] reason={reason}");
        }

        private void ZoomCamera(float scrollDelta, Vector2 screenPosition)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            float currentSize = cam.orthographicSize;
            float zoomFactor = scrollDelta > 0f ? 0.9f : 1.1f;
            float newSize = Mathf.Clamp(currentSize * zoomFactor, 2.5f, 250f);

            if (Mathf.Approximately(currentSize, newSize))
                return;

            Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(cam.transform.position.z)));
            cam.orthographicSize = newSize;
            Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(cam.transform.position.z)));

            Vector3 diff = mouseWorldBefore - mouseWorldAfter;
            cam.transform.position += new Vector3(diff.x, diff.y, 0f);
        }

        /// <summary>
        /// Computes the comprehensive world-space bounding box of all level content,
        /// including platforms, active trajectory extents, level markers (StartPos, CheckPoints, End),
        /// or existing camera boundaries. Handles empty, single-object, and degenerate levels safely.
        /// </summary>
        public Bounds CalculateLevelBounds()
        {
            bool hasPoint = false;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            void IncludePoint(Vector2 pt)
            {
                if (float.IsNaN(pt.x) || float.IsNaN(pt.y) || float.IsInfinity(pt.x) || float.IsInfinity(pt.y))
                    return;
                hasPoint = true;
                if (pt.x < minX) minX = pt.x;
                if (pt.x > maxX) maxX = pt.x;
                if (pt.y < minY) minY = pt.y;
                if (pt.y > maxY) maxY = pt.y;
            }

            void IncludeAabb(Vector2 center, Vector2 size)
            {
                if (float.IsNaN(center.x) || float.IsNaN(center.y) || float.IsNaN(size.x) || float.IsNaN(size.y))
                    return;
                Vector2 half = new Vector2(Mathf.Abs(size.x) * 0.5f, Mathf.Abs(size.y) * 0.5f);
                IncludePoint(center - half);
                IncludePoint(center + half);
            }

            // 1. All active platforms and their trajectory motion envelopes
            List<ColorPlatform> platforms = ColorPlatform.All;
            for (int i = 0; i < platforms.Count; i++)
            {
                ColorPlatform p = platforms[i];
                if (p == null)
                    continue;

                Rect aabb = p.GetAabb();
                IncludePoint(new Vector2(aabb.xMin, aabb.yMin));
                IncludePoint(new Vector2(aabb.xMax, aabb.yMax));

                // Include trajectory envelope
                Trajectory traj = p.GetTrajectory();
                if (traj != null && traj.Config != null && traj.Config.enabled)
                {
                    TrajectoryData cfg = traj.Config;
                    IncludePoint(new Vector2(cfg.originX, cfg.originY));

                    if (cfg.curveType == TrajectoryCurveType.Line)
                    {
                        IncludePoint(Trajectory.EvaluateWorldPoint(cfg, 0f));
                        IncludePoint(Trajectory.EvaluateWorldPoint(cfg, 0.5f));
                        IncludePoint(Trajectory.EvaluateWorldPoint(cfg, 1f));

                        if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                        {
                            IncludePoint(new Vector2(cfg.boundsXMin, cfg.boundsYMin));
                            IncludePoint(new Vector2(cfg.boundsXMax, cfg.boundsYMax));
                        }
                    }
                    else if (cfg.curveType == TrajectoryCurveType.Circle)
                    {
                        float r = Mathf.Max(0.001f, cfg.radius);
                        IncludePoint(new Vector2(cfg.originX - r, cfg.originY - r));
                        IncludePoint(new Vector2(cfg.originX + r, cfg.originY + r));
                    }
                }
            }

            // 2. All level markers (StartPos, CheckPoints, End)
            List<LevelMarker> markers = LevelMarker.All;
            for (int i = 0; i < markers.Count; i++)
            {
                LevelMarker m = markers[i];
                if (m == null)
                    continue;

                IncludeAabb(m.Position, m.Size);
            }

            // 3. Fallback: Saved camera boundary from CameraFollow or working copy
            if (!hasPoint)
            {
                CameraFollow camFollow = FindAnyObjectByType<CameraFollow>();
                if (camFollow != null && camFollow.HasBoundary)
                {
                    IncludeAabb(camFollow.BoundaryCenter, camFollow.BoundarySize);
                }
                else if (editorWorkingCopy != null && editorWorkingCopy.cameraBoundary != null &&
                         editorWorkingCopy.cameraBoundary.position != null && editorWorkingCopy.cameraBoundary.size != null)
                {
                    IncludeAabb(editorWorkingCopy.cameraBoundary.position.ToVector2(), editorWorkingCopy.cameraBoundary.size.ToVector2());
                }
            }

            // 4. Default fallback if level is completely empty
            if (!hasPoint)
            {
                return new Bounds(Vector3.zero, new Vector3(20f, 10f, 0f));
            }

            Vector2 bCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            Vector2 bSize = new Vector2(Mathf.Max(2f, maxX - minX), Mathf.Max(2f, maxY - minY));
            return new Bounds(bCenter, new Vector3(bSize.x, bSize.y, 0f));
        }

        /// <summary>
        /// Centers and scales the Edit Viewport camera so the entire level fits comfortably
        /// within the usable screen area between overlapping panels (Inspector, Palette, Top/Bottom bars),
        /// preserving aspect ratio with reasonable margins.
        /// </summary>
        public void FitMap()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                LogEditor("[EDITOR][FIT_MAP_FAILED] Camera.main is null.");
                return;
            }

            Bounds bounds = CalculateLevelBounds();

            // Margin: 15% of dimension, at least 2.5 world units
            float marginX = Mathf.Max(2.5f, bounds.size.x * 0.15f);
            float marginY = Mathf.Max(2.5f, bounds.size.y * 0.15f);
            float paddedW = bounds.size.x + 2f * marginX;
            float paddedH = bounds.size.y + 2f * marginY;

            // Usable viewport ratios relative to full screen (1920x1080 canvas)
            // Left Inspector: 360px, Right Palette: 320px -> Usable width = 1240 / 1920
            // Top Bar: 76px, Bottom Help Bar: 38px -> Usable height = 966 / 1080
            const float usableWidthRatio = 1240f / 1920f;
            const float usableHeightRatio = 966f / 1080f;

            float aspect = cam.aspect > 0.01f ? cam.aspect : (16f / 9f);

            float sizeForHeight = paddedH / (2f * usableHeightRatio);
            float sizeForWidth = paddedW / (2f * aspect * usableWidthRatio);

            float targetOrthoSize = Mathf.Clamp(Mathf.Max(sizeForHeight, sizeForWidth), 3.5f, 250f);
            cam.orthographicSize = targetOrthoSize;

            // Viewport usable center offset in normalized screen coordinates
            // Center X is (360 + 1600)/2 = 980 -> +20px from screen center 960 (+20/1920)
            // Center Y is (38 + 1004)/2 = 521 -> -19px from screen center 540 (-19/1080)
            const float normDeltaX = 20f / 1920f;
            const float normDeltaY = -19f / 1080f;

            float worldOffsetX = normDeltaX * (2f * targetOrthoSize * aspect);
            float worldOffsetY = normDeltaY * (2f * targetOrthoSize);

            Vector3 camPos = cam.transform.position;
            cam.transform.position = new Vector3(
                bounds.center.x - worldOffsetX,
                bounds.center.y - worldOffsetY,
                camPos.z);

            ui?.SetStatus("Fit Map: Framed entire level.");
            LogEditor($"[EDITOR][FIT_MAP] bounds={bounds} orthoSize={targetOrthoSize} camPos={cam.transform.position}");
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
            var pres = platformObject.AddComponent<Contrast.Presentation.PlatformVisualPresentation>();
            platform.Initialize(
                worldPosition,
                defaultPlatformSize,
                defaultPlatformColor);
            pres.InitializeVisuals();

            // Initialize disabled trajectory for new platform.
            platform.InitializeTrajectory(null);

            SelectPlatform(platform);
            ui?.SetStatus("Platform added.");
        }

        public LevelData BuildExportData()
        {
            LevelData data = new LevelData();

            // Filter out any null/destroyed entries that might remain.
            ColorPlatform.All.RemoveAll(p => p == null || p.gameObject == null);

            data.platforms = new ColorPlatformData[ColorPlatform.All.Count];

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

            List<CheckPointData> checkpoints = new List<CheckPointData>();

            for (int i = 0; i < LevelMarker.All.Count; i++)
            {
                LevelMarker marker = LevelMarker.All[i];
                if (marker == null || marker.gameObject == null)
                    continue;

                Vector2Data pos = new Vector2Data(marker.Position.x, marker.Position.y);
                Vector2Data size = new Vector2Data(marker.Size.x, marker.Size.y);

                switch (marker.Type)
                {
                    case LevelMarkerType.StartPos:
                        if (data.startPos == null)
                        {
                            data.startPos = new StartPosData
                            {
                                position = pos,
                                size = size,
                                color = marker.RawColor
                            };
                        }
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
                        if (data.end == null)
                        {
                            data.end = new EndData
                            {
                                position = pos,
                                size = size
                            };
                        }
                        break;
                }
            }

            // Restore markers from working copy if they were consumed during gameplay
            if (data.startPos == null && editorWorkingCopy != null && editorWorkingCopy.startPos != null)
            {
                data.startPos = new StartPosData
                {
                    position = new Vector2Data(editorWorkingCopy.startPos.position.x, editorWorkingCopy.startPos.position.y),
                    size = new Vector2Data(editorWorkingCopy.startPos.size.x, editorWorkingCopy.startPos.size.y),
                    color = editorWorkingCopy.startPos.color
                };
            }

            if (checkpoints.Count == 0 && editorWorkingCopy != null && editorWorkingCopy.checkPoint != null)
            {
                foreach (CheckPointData cp in editorWorkingCopy.checkPoint)
                {
                    if (cp != null && cp.position != null)
                    {
                        checkpoints.Add(new CheckPointData
                        {
                            position = new Vector2Data(cp.position.x, cp.position.y),
                            size = cp.size != null ? new Vector2Data(cp.size.x, cp.size.y) : new Vector2Data(1f, 1f),
                            color = cp.color
                        });
                    }
                }
            }

            if (data.end == null && editorWorkingCopy != null && editorWorkingCopy.end != null)
            {
                data.end = new EndData
                {
                    position = new Vector2Data(editorWorkingCopy.end.position.x, editorWorkingCopy.end.position.y),
                    size = editorWorkingCopy.end.size != null ? new Vector2Data(editorWorkingCopy.end.size.x, editorWorkingCopy.end.size.y) : new Vector2Data(1.5f, 2f)
                };
            }

            if (checkpoints.Count > 0)
                data.checkPoint = checkpoints.ToArray();
            else
                data.checkPoint = null;

            // Camera boundary
            CameraFollow camFollow = FindAnyObjectByType<CameraFollow>();
            if (camFollow != null && camFollow.HasBoundary)
            {
                data.cameraBoundary = new CameraBoundaryData
                {
                    position = new Vector2Data(camFollow.BoundaryCenter.x, camFollow.BoundaryCenter.y),
                    size = new Vector2Data(camFollow.BoundarySize.x, camFollow.BoundarySize.y)
                };
            }
            else if (editorWorkingCopy != null && editorWorkingCopy.cameraBoundary != null)
            {
                data.cameraBoundary = new CameraBoundaryData
                {
                    position = new Vector2Data(editorWorkingCopy.cameraBoundary.position.x, editorWorkingCopy.cameraBoundary.position.y),
                    size = new Vector2Data(editorWorkingCopy.cameraBoundary.size.x, editorWorkingCopy.cameraBoundary.size.y)
                };
            }

            // Do not export legacy spawnPoint
            data.spawnPoint = null;

            // Sanitize without modifying working active references
            data = LevelDataSanitizer.Sanitize(data, null, out _);

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

        private ResizeHandleKind HitResizeHandle(Vector2 world)
        {
            if (selectedPlatform == null)
                return ResizeHandleKind.None;

            Rect r = selectedPlatform.GetAabb();
            Vector2 left = new Vector2(r.xMin, r.center.y);
            Vector2 right = new Vector2(r.xMax, r.center.y);
            Vector2 top = new Vector2(r.center.x, r.yMax);
            Vector2 bottom = new Vector2(r.center.x, r.yMin);

            if (Vector2.Distance(world, left) <= handleHitRadius)
                return ResizeHandleKind.Left;
            if (Vector2.Distance(world, right) <= handleHitRadius)
                return ResizeHandleKind.Right;
            if (Vector2.Distance(world, top) <= handleHitRadius)
                return ResizeHandleKind.Top;
            if (Vector2.Distance(world, bottom) <= handleHitRadius)
                return ResizeHandleKind.Bottom;

            return ResizeHandleKind.None;
        }

        public void RefreshHandles()
        {
            EnsureHandleRoot();

            if (selectedPlatform == null || !editing)
            {
                SetHandlesVisible(false);
                if (selectionBox != null)
                    selectionBox.SetActive(false);
                trajectoryController?.RefreshVisualization(false);
                return;
            }

            // Platform resize handles and high-visibility selection outline
            Rect r = selectedPlatform.GetAabb();
            if (selectionBox != null && selectionBoxRenderer != null)
            {
                selectionBox.transform.position = new Vector3(r.center.x, r.center.y, -0.5f);
                selectionBoxRenderer.size = new Vector2(r.width + 0.08f, r.height + 0.08f);
                selectionBox.SetActive(true);
            }

            SetResizeHandle(ResizeHandleKind.Left, new Vector2(r.xMin, r.center.y), true);
            SetResizeHandle(ResizeHandleKind.Right, new Vector2(r.xMax, r.center.y), true);
            SetResizeHandle(ResizeHandleKind.Top, new Vector2(r.center.x, r.yMax), true);
            SetResizeHandle(ResizeHandleKind.Bottom, new Vector2(r.center.x, r.yMin), true);

            // Trajectory preview and handles
            trajectoryController?.RefreshVisualization(editing);
        }

        private void SetResizeHandle(ResizeHandleKind kind, Vector2 position, bool visible)
        {
            if (!resizeHandles.TryGetValue(kind, out GameObject go) || go == null)
                return;

            go.transform.position = new Vector3(position.x, position.y, -1f);
            go.transform.localScale = Vector3.one * handleSize;
            go.SetActive(visible);
        }

        private void SetHandlesVisible(bool visible)
        {
            if (selectionBox != null)
                selectionBox.SetActive(visible);

            foreach (GameObject go in resizeHandles.Values)
            {
                if (go != null)
                    go.SetActive(visible);
            }

            if (!visible)
            {
                trajectoryController?.RefreshVisualization(false);
            }
        }

        private void EnsureHandleRoot()
        {
            if (handleRoot != null)
                return;

            handleRoot = new GameObject("EditorHandles");
            handleRoot.transform.SetParent(transform, false);

            if (selectionBox == null)
            {
                selectionBox = new GameObject("SelectionBox");
                selectionBox.transform.SetParent(handleRoot.transform, false);
                selectionBoxRenderer = selectionBox.AddComponent<SpriteRenderer>();
                selectionBoxRenderer.sprite = GetSelectionBoxSprite();
                selectionBoxRenderer.drawMode = SpriteDrawMode.Sliced;
                selectionBoxRenderer.sortingOrder = 1004;
                selectionBoxRenderer.color = new UnityEngine.Color(0.2f, 0.75f, 1f, 0.85f);
                selectionBox.SetActive(false);
            }

            CreateResizeHandle(ResizeHandleKind.Left, UnityEngine.Color.white);
            CreateResizeHandle(ResizeHandleKind.Right, UnityEngine.Color.white);
            CreateResizeHandle(ResizeHandleKind.Top, UnityEngine.Color.white);
            CreateResizeHandle(ResizeHandleKind.Bottom, UnityEngine.Color.white);

            trajectoryController ??= new TrajectoryEditController(handleSize, handleHitRadius);
            trajectoryController.Initialize(handleRoot.transform);
        }

        private static Sprite GetHandleSprite()
        {
            if (cachedHandleSprite != null)
                return cachedHandleSprite;

            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            UnityEngine.Color borderCol = new UnityEngine.Color(0.06f, 0.10f, 0.16f, 1f);
            UnityEngine.Color innerCol = new UnityEngine.Color(0.92f, 0.96f, 1f, 1f);

            UnityEngine.Color[] pixels = new UnityEngine.Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                    pixels[y * size + x] = isBorder ? borderCol : innerCol;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            cachedHandleSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);

            return cachedHandleSprite;
        }

        private static Sprite GetSelectionBoxSprite()
        {
            if (cachedSelectionBoxSprite != null)
                return cachedSelectionBoxSprite;

            const int res = 32;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            UnityEngine.Color[] pixels = new UnityEngine.Color[res * res];
            const int borderPx = 2;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    bool isBorder = (x < borderPx || x >= res - borderPx || y < borderPx || y >= res - borderPx);
                    pixels[y * res + x] = isBorder ? UnityEngine.Color.white : new UnityEngine.Color(1f, 1f, 1f, 0f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            cachedSelectionBoxSprite = Sprite.Create(
                tex,
                new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f),
                32f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(borderPx, borderPx, borderPx, borderPx));

            return cachedSelectionBoxSprite;
        }

        private void CreateResizeHandle(ResizeHandleKind kind, UnityEngine.Color color)
        {
            GameObject go = new GameObject(kind + "Handle");
            go.transform.SetParent(handleRoot.transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetHandleSprite();
            sr.sortingOrder = 1005;
            sr.color = color;

            resizeHandles[kind] = go;
            go.SetActive(false);
        }

        private void ClearSelection()
        {
            selectionKind = SelectionKind.None;
            selectedPlatform = null;
            selectedMarker = null;
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;
            if (selectionBox != null)
                selectionBox.SetActive(false);
            trajectoryController?.ClearTarget(ui);
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

            // Destroy GameObject safely.
            SafeDestroy(toDelete.gameObject);

            LogEditor($"[EDITOR][DELETE] platform={name}");
            ui?.SetStatus($"Deleted {name}.");
        }

        /// <summary>
        /// Clears all editable level content (platforms, trajectories, markers) from the currently open level,
        /// leaving an empty level-editing canvas while preserving editor infrastructure, UI, and camera view.
        /// Updates authoritative data so subsequent exports and gameplay starts reflect the empty level.
        /// </summary>
        public void ResetLevel()
        {
            // 1. Cancel ongoing interactions safely
            if (mapPointerDragging)
            {
                CancelMapPointerDrag("Level reset");
            }
            if (mapPanning)
            {
                CancelMapPan("Level reset");
            }
            paletteDragging = false;
            activeResizeHandle = ResizeHandleKind.None;
            activeTrajHandle = TrajectoryHandleKind.None;
            inputRouter.ForceReleasePointer();

            // 2. Clear selection and inspector references
            ClearSelection();

            // 3. Clear trajectory visualizations and handles
            trajectoryController?.ClearTarget(ui);
            trajectoryController?.RefreshVisualization(false);

            // Record if level had any content before clearing (for status reporting)
            bool hadContent = ColorPlatform.All.Count > 0 || LevelMarker.All.Count > 0;

            // 4. Destroy all platform GameObjects and clear registry
            Transform platformRoot = FindOrCreatePlatformRoot();
            if (platformRoot != null)
            {
                for (int i = platformRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = platformRoot.GetChild(i);
                    if (child != null)
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
            }

            for (int i = ColorPlatform.All.Count - 1; i >= 0; i--)
            {
                ColorPlatform p = ColorPlatform.All[i];
                if (p != null && p.gameObject != null)
                {
                    SafeDestroy(p.gameObject);
                }
            }
            ColorPlatform.ClearRegistry();

            // 5. Destroy all marker GameObjects and clear registry
            Transform editorRoot = FindOrCreateEditorRoot();
            if (editorRoot != null)
            {
                for (int i = editorRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = editorRoot.GetChild(i);
                    if (child != null)
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
            }

            for (int i = LevelMarker.All.Count - 1; i >= 0; i--)
            {
                LevelMarker m = LevelMarker.All[i];
                if (m != null && m.gameObject != null)
                {
                    SafeDestroy(m.gameObject);
                }
            }
            LevelMarker.ClearRegistry();

            // Sweep any stray or orphaned platforms / markers in the scene
            ColorPlatform[] strayPlatforms = FindObjectsByType<ColorPlatform>();
            for (int i = 0; i < strayPlatforms.Length; i++)
            {
                if (strayPlatforms[i] != null && strayPlatforms[i].gameObject != null)
                    SafeDestroy(strayPlatforms[i].gameObject);
            }

            LevelMarker[] strayMarkers = FindObjectsByType<LevelMarker>();
            for (int i = 0; i < strayMarkers.Length; i++)
            {
                if (strayMarkers[i] != null && strayMarkers[i].gameObject != null)
                    SafeDestroy(strayMarkers[i].gameObject);
            }

            // Also clean redundant roots if any exist outside our canonical roots
            GameObject roguePlat = GameObject.Find("Platforms");
            if (roguePlat != null && roguePlat.transform != platformRoot)
            {
                for (int i = roguePlat.transform.childCount - 1; i >= 0; i--)
                    SafeDestroy(roguePlat.transform.GetChild(i).gameObject);
                SafeDestroy(roguePlat);
            }

            GameObject rogueObjs = GameObject.Find("LevelObjects");
            if (rogueObjs != null && rogueObjs.transform != editorRoot)
            {
                for (int i = rogueObjs.transform.childCount - 1; i >= 0; i--)
                    SafeDestroy(rogueObjs.transform.GetChild(i).gameObject);
                SafeDestroy(rogueObjs);
            }

            // 6. Update authoritative data sources to prevent stale data resurrection
            CameraBoundaryData preservedBoundary = editorWorkingCopy?.cameraBoundary;
            editorWorkingCopy = new LevelData
            {
                platforms = new ColorPlatformData[0],
                startPos = null,
                checkPoint = null,
                end = null,
                spawnPoint = null,
                cameraBoundary = preservedBoundary
            };

            if (levelLoader != null)
            {
                levelLoader.ClearLevel(preserveCameraBoundary: true);
            }

            RefreshHandles();

            if (hadContent)
            {
                ui?.SetStatus("Level reset: All editable level content cleared.");
                LogEditor("[EDITOR][RESET] All editable level content cleared.");
            }
            else
            {
                ui?.SetStatus("Level reset: Level is already empty.");
                LogEditor("[EDITOR][RESET] Level already empty.");
            }
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null)
                return;

            go.SetActive(false);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(go);
                return;
            }
#endif
            Destroy(go);
        }

        private Transform FindOrCreatePlatformRoot()
        {
            if (levelLoader == null)
                levelLoader = FindAnyObjectByType<LevelLoader>();

            if (levelLoader != null)
                return levelLoader.PlatformsRoot;

            GameObject go = GameObject.Find("Platforms");
            if (go != null)
                return go.transform;

            return new GameObject("Platforms").transform;
        }

        private Transform FindOrCreateEditorRoot()
        {
            if (levelLoader == null)
                levelLoader = FindAnyObjectByType<LevelLoader>();

            if (levelLoader != null)
                return levelLoader.LevelObjectsRoot;

            GameObject go = GameObject.Find("LevelObjects");
            if (go != null)
                return go.transform;

            return new GameObject("LevelObjects").transform;
        }

        private void EnsureDependencies()
        {
            levelLoader ??= FindAnyObjectByType<LevelLoader>();
            ui ??= FindAnyObjectByType<LevelEditorUI>(FindObjectsInactive.Include);
            ui?.SetController(this);
        }

        private void OnDestroy()
        {
            trajectoryController?.Cleanup();
        }
    }
}
