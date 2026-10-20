using System.Collections.Generic;
using Contrast.Data;
using UnityEngine;

namespace Contrast.Level.EditorInteraction
{
    /// <summary>
    /// Encapsulates the visual presentation, gizmo handles, and path LineRenderers
    /// for trajectory visualization in the level editor.
    /// Also performs spatial hit-testing for trajectory handles.
    /// </summary>
    public sealed class TrajectoryHandleVisualizer
    {
        private readonly float handleSize;
        private readonly float handleHitRadius;

        private GameObject handleRoot;
        private readonly Dictionary<TrajectoryHandleKind, GameObject> handles =
            new Dictionary<TrajectoryHandleKind, GameObject>();

        private LineRenderer trajectoryLineRenderer;
        private LineRenderer boundsLineRenderer;

        public TrajectoryHandleVisualizer(float handleSize = 0.16f, float handleHitRadius = 0.35f)
        {
            this.handleSize = handleSize;
            this.handleHitRadius = handleHitRadius;
        }

        public void EnsureInitialized(Transform parent)
        {
            if (handleRoot != null)
                return;

            handleRoot = new GameObject("TrajectoryHandlesRoot");
            handleRoot.transform.SetParent(parent, false);

            CreateHandle(TrajectoryHandleKind.Origin, new UnityEngine.Color(1f, 0.9f, 0.2f, 1f));
            CreateHandle(TrajectoryHandleKind.LineEnd, new UnityEngine.Color(0.2f, 0.9f, 1f, 1f));
            CreateHandle(TrajectoryHandleKind.CircleCenter, new UnityEngine.Color(1f, 0.9f, 0.2f, 1f));
            CreateHandle(TrajectoryHandleKind.CircleRadius, new UnityEngine.Color(1f, 0.3f, 0.8f, 1f));
            CreateHandle(TrajectoryHandleKind.ArcStart, new UnityEngine.Color(0.4f, 1f, 0.4f, 1f));
            CreateHandle(TrajectoryHandleKind.ArcEnd, new UnityEngine.Color(0.4f, 1f, 0.4f, 1f));
            CreateHandle(TrajectoryHandleKind.BoundsMinX, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));
            CreateHandle(TrajectoryHandleKind.BoundsMaxX, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));
            CreateHandle(TrajectoryHandleKind.BoundsMinY, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));
            CreateHandle(TrajectoryHandleKind.BoundsMaxY, new UnityEngine.Color(1f, 0.6f, 0.1f, 1f));

            trajectoryLineRenderer = CreatePreviewLine("TrajectoryPreview", new UnityEngine.Color(0.2f, 1f, 0.4f, 0.85f), 0.06f);
            boundsLineRenderer = CreatePreviewLine("BoundsPreview", new UnityEngine.Color(1f, 0.7f, 0.2f, 0.75f), 0.04f);
        }

        private void CreateHandle(TrajectoryHandleKind kind, UnityEngine.Color color)
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

        /// <summary>
        /// Perform hit testing against all currently active trajectory handles.
        /// Returns TrajectoryHandleKind.None if no handle is within handleHitRadius.
        /// </summary>
        public TrajectoryHandleKind HitTest(Vector2 worldPos, TrajectoryData cfg)
        {
            if (cfg == null || !cfg.enabled || Trajectory.Validate(cfg) != null)
                return TrajectoryHandleKind.None;

            if (cfg.curveType == TrajectoryCurveType.Line)
            {
                Vector2 origin = new Vector2(cfg.originX, cfg.originY);
                if (Vector2.Distance(worldPos, origin) <= handleHitRadius)
                    return TrajectoryHandleKind.Origin;

                if (cfg.domainMode == TrajectoryDomainMode.ParameterRange)
                {
                    Vector2 lineEnd = Trajectory.EvaluateWorldPoint(cfg, 1f);
                    if (Vector2.Distance(worldPos, lineEnd) <= handleHitRadius)
                        return TrajectoryHandleKind.LineEnd;
                }
                else if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                {
                    Vector2 bMinX = new Vector2(cfg.boundsXMin, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f);
                    Vector2 bMaxX = new Vector2(cfg.boundsXMax, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f);
                    Vector2 bMinY = new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMin);
                    Vector2 bMaxY = new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMax);

                    if (Vector2.Distance(worldPos, bMinX) <= handleHitRadius) return TrajectoryHandleKind.BoundsMinX;
                    if (Vector2.Distance(worldPos, bMaxX) <= handleHitRadius) return TrajectoryHandleKind.BoundsMaxX;
                    if (Vector2.Distance(worldPos, bMinY) <= handleHitRadius) return TrajectoryHandleKind.BoundsMinY;
                    if (Vector2.Distance(worldPos, bMaxY) <= handleHitRadius) return TrajectoryHandleKind.BoundsMaxY;
                }
            }
            else if (cfg.curveType == TrajectoryCurveType.Circle)
            {
                Vector2 center = new Vector2(cfg.originX, cfg.originY);
                if (Vector2.Distance(worldPos, center) <= handleHitRadius)
                    return TrajectoryHandleKind.CircleCenter;

                Vector2 radPos = center + Vector2.right * cfg.radius;
                if (Vector2.Distance(worldPos, radPos) <= handleHitRadius)
                    return TrajectoryHandleKind.CircleRadius;

                float angleSpan = Mathf.Abs(cfg.paramMax - cfg.paramMin);
                if (angleSpan < 355f)
                {
                    Vector2 arcStart = Trajectory.EvaluateWorldPoint(cfg, 0f);
                    Vector2 arcEnd = Trajectory.EvaluateWorldPoint(cfg, 1f);

                    if (Vector2.Distance(worldPos, arcStart) <= handleHitRadius) return TrajectoryHandleKind.ArcStart;
                    if (Vector2.Distance(worldPos, arcEnd) <= handleHitRadius) return TrajectoryHandleKind.ArcEnd;
                }
            }

            return TrajectoryHandleKind.None;
        }

        /// <summary>
        /// Update handle positions and LineRenderers based on current trajectory configuration.
        /// </summary>
        public void UpdateVisualization(TrajectoryData cfg, bool isEditing)
        {
            if (handleRoot == null)
                return;

            if (cfg == null || !cfg.enabled || !isEditing || Trajectory.Validate(cfg) != null)
            {
                HideAll();
                return;
            }

            // 1. Draw curve preview
            Vector2[] pts = Trajectory.GetPreviewPoints(cfg, 64);
            if (trajectoryLineRenderer != null)
            {
                trajectoryLineRenderer.positionCount = pts.Length;
                for (int i = 0; i < pts.Length; i++)
                    trajectoryLineRenderer.SetPosition(i, new Vector3(pts[i].x, pts[i].y, -0.8f));
            }

            // 2. Spatial bounds preview
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

                SetHandle(TrajectoryHandleKind.BoundsMinX, new Vector2(cfg.boundsXMin, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f), true);
                SetHandle(TrajectoryHandleKind.BoundsMaxX, new Vector2(cfg.boundsXMax, (cfg.boundsYMin + cfg.boundsYMax) * 0.5f), true);
                SetHandle(TrajectoryHandleKind.BoundsMinY, new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMin), true);
                SetHandle(TrajectoryHandleKind.BoundsMaxY, new Vector2((cfg.boundsXMin + cfg.boundsXMax) * 0.5f, cfg.boundsYMax), true);
            }
            else
            {
                if (boundsLineRenderer != null) boundsLineRenderer.positionCount = 0;
                HideHandle(TrajectoryHandleKind.BoundsMinX);
                HideHandle(TrajectoryHandleKind.BoundsMaxX);
                HideHandle(TrajectoryHandleKind.BoundsMinY);
                HideHandle(TrajectoryHandleKind.BoundsMaxY);
            }

            // 3. Handles according to curve type
            if (cfg.curveType == TrajectoryCurveType.Line)
            {
                SetHandle(TrajectoryHandleKind.Origin, new Vector2(cfg.originX, cfg.originY), true);

                if (cfg.domainMode == TrajectoryDomainMode.ParameterRange)
                {
                    SetHandle(TrajectoryHandleKind.LineEnd, Trajectory.EvaluateWorldPoint(cfg, 1f), true);
                }
                else
                {
                    HideHandle(TrajectoryHandleKind.LineEnd);
                }

                HideHandle(TrajectoryHandleKind.CircleCenter);
                HideHandle(TrajectoryHandleKind.CircleRadius);
                HideHandle(TrajectoryHandleKind.ArcStart);
                HideHandle(TrajectoryHandleKind.ArcEnd);
            }
            else if (cfg.curveType == TrajectoryCurveType.Circle)
            {
                SetHandle(TrajectoryHandleKind.CircleCenter, new Vector2(cfg.originX, cfg.originY), true);
                SetHandle(TrajectoryHandleKind.CircleRadius, new Vector2(cfg.originX + cfg.radius, cfg.originY), true);

                float angleSpan = Mathf.Abs(cfg.paramMax - cfg.paramMin);
                if (angleSpan < 355f)
                {
                    SetHandle(TrajectoryHandleKind.ArcStart, Trajectory.EvaluateWorldPoint(cfg, 0f), true);
                    SetHandle(TrajectoryHandleKind.ArcEnd, Trajectory.EvaluateWorldPoint(cfg, 1f), true);
                }
                else
                {
                    HideHandle(TrajectoryHandleKind.ArcStart);
                    HideHandle(TrajectoryHandleKind.ArcEnd);
                }

                HideHandle(TrajectoryHandleKind.Origin);
                HideHandle(TrajectoryHandleKind.LineEnd);
            }
        }

        private void SetHandle(TrajectoryHandleKind kind, Vector2 position, bool visible)
        {
            if (!handles.TryGetValue(kind, out GameObject go) || go == null)
                return;

            go.transform.position = new Vector3(position.x, position.y, -1f);
            go.transform.localScale = Vector3.one * handleSize;
            go.SetActive(visible);
        }

        private void HideHandle(TrajectoryHandleKind kind)
        {
            if (handles.TryGetValue(kind, out GameObject go) && go != null)
                go.SetActive(false);
        }

        public void HideAll()
        {
            foreach (GameObject go in handles.Values)
            {
                if (go != null)
                    go.SetActive(false);
            }

            if (trajectoryLineRenderer != null) trajectoryLineRenderer.positionCount = 0;
            if (boundsLineRenderer != null) boundsLineRenderer.positionCount = 0;
        }

        public void Cleanup()
        {
            if (handleRoot != null)
            {
                Object.Destroy(handleRoot);
                handleRoot = null;
            }
            handles.Clear();
        }
    }
}
