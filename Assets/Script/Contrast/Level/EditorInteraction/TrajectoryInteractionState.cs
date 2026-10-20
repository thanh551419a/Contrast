using Contrast.Data;
using UnityEngine;

namespace Contrast.Level.EditorInteraction
{
    public enum TrajectoryInteractionPhase
    {
        Idle,
        Dragging
    }

    /// <summary>
    /// Encapsulates the interaction state machine, preview calculation, commit, and rollback
    /// for trajectory manipulation in the viewport.
    /// </summary>
    public sealed class TrajectoryInteractionState
    {
        public TrajectoryInteractionPhase Phase { get; private set; } = TrajectoryInteractionPhase.Idle;
        public TrajectoryHandleKind ActiveHandle { get; private set; } = TrajectoryHandleKind.None;
        public TrajectoryInteractionSnapshot Snapshot { get; private set; }
        public TrajectoryData WorkingPreviewConfig { get; private set; }
        public ColorPlatform TargetPlatform { get; private set; }

        public bool IsDragging => Phase == TrajectoryInteractionPhase.Dragging;

        /// <summary>
        /// Begin an interactive drag operation on a trajectory handle.
        /// </summary>
        public bool BeginDrag(
            TrajectoryHandleKind handle,
            Vector2 worldPosition,
            ColorPlatform platform)
        {
            if (platform == null || !handle.IsTrajectoryHandle())
                return false;

            Trajectory traj = platform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            if (cfg == null || !cfg.enabled)
                return false;

            TargetPlatform = platform;
            ActiveHandle = handle;
            Snapshot = new TrajectoryInteractionSnapshot(platform, cfg, worldPosition);
            WorkingPreviewConfig = cfg.Clone();
            Phase = TrajectoryInteractionPhase.Dragging;
            return true;
        }

        /// <summary>
        /// Calculate preview parameters and the resulting platform position based on pointer world coordinates.
        /// </summary>
        public bool CalculatePreview(Vector2 currentWorldPos, out Vector2 platformPreviewPos)
        {
            platformPreviewPos = TargetPlatform != null ? TargetPlatform.Position : Vector2.zero;

            if (Phase != TrajectoryInteractionPhase.Dragging || WorkingPreviewConfig == null || Snapshot == null)
                return false;

            Vector2 delta = currentWorldPos - Snapshot.DragStartWorld;

            switch (ActiveHandle)
            {
                case TrajectoryHandleKind.Origin:
                case TrajectoryHandleKind.CircleCenter:
                    WorkingPreviewConfig.originX = Snapshot.InitialOrigin.x + delta.x;
                    WorkingPreviewConfig.originY = Snapshot.InitialOrigin.y + delta.y;
                    break;

                case TrajectoryHandleKind.LineEnd:
                    Vector2 lineVec = currentWorldPos - new Vector2(WorkingPreviewConfig.originX, WorkingPreviewConfig.originY);
                    float pMaxAbs = Mathf.Max(0.001f, Mathf.Abs(WorkingPreviewConfig.paramMax));
                    WorkingPreviewConfig.scaleX = Mathf.Max(0.1f, lineVec.magnitude / pMaxAbs);
                    float angleDeg = Mathf.Atan2(lineVec.y, lineVec.x) * Mathf.Rad2Deg;
                    if (WorkingPreviewConfig.paramMax < 0f) angleDeg += 180f;
                    WorkingPreviewConfig.rotation = angleDeg;
                    break;

                case TrajectoryHandleKind.CircleRadius:
                    Vector2 c = new Vector2(WorkingPreviewConfig.originX, WorkingPreviewConfig.originY);
                    WorkingPreviewConfig.radius = Mathf.Max(0.1f, Vector2.Distance(currentWorldPos, c));
                    break;

                case TrajectoryHandleKind.ArcStart:
                    Vector2 cS = new Vector2(WorkingPreviewConfig.originX, WorkingPreviewConfig.originY);
                    float aS = Mathf.Atan2(currentWorldPos.y - cS.y, currentWorldPos.x - cS.x) * Mathf.Rad2Deg;
                    if (aS < 0f) aS += 360f;
                    WorkingPreviewConfig.paramMin = aS;
                    break;

                case TrajectoryHandleKind.ArcEnd:
                    Vector2 cE = new Vector2(WorkingPreviewConfig.originX, WorkingPreviewConfig.originY);
                    float aE = Mathf.Atan2(currentWorldPos.y - cE.y, currentWorldPos.x - cE.x) * Mathf.Rad2Deg;
                    if (aE < 0f) aE += 360f;
                    WorkingPreviewConfig.paramMax = aE;
                    break;

                case TrajectoryHandleKind.BoundsMinX:
                    WorkingPreviewConfig.boundsXMin = Mathf.Min(currentWorldPos.x, WorkingPreviewConfig.boundsXMax - 0.5f);
                    break;

                case TrajectoryHandleKind.BoundsMaxX:
                    WorkingPreviewConfig.boundsXMax = Mathf.Max(currentWorldPos.x, WorkingPreviewConfig.boundsXMin + 0.5f);
                    break;

                case TrajectoryHandleKind.BoundsMinY:
                    WorkingPreviewConfig.boundsYMin = Mathf.Min(currentWorldPos.y, WorkingPreviewConfig.boundsYMax - 0.5f);
                    break;

                case TrajectoryHandleKind.BoundsMaxY:
                    WorkingPreviewConfig.boundsYMax = Mathf.Max(currentWorldPos.y, WorkingPreviewConfig.boundsYMin + 0.5f);
                    break;
            }

            platformPreviewPos = Trajectory.EvaluateWorldPoint(WorkingPreviewConfig, Snapshot.InitialPlatformPhase);
            return true;
        }

        /// <summary>
        /// Commit the working preview state to authoritative platform and trajectory data.
        /// </summary>
        public TrajectoryData Commit(out Vector2 finalPlatformPos)
        {
            finalPlatformPos = TargetPlatform != null ? TargetPlatform.Position : Vector2.zero;

            if (Phase != TrajectoryInteractionPhase.Dragging)
                return null;

            TrajectoryData result = WorkingPreviewConfig != null ? WorkingPreviewConfig.Clone() : null;

            if (TargetPlatform != null && result != null)
            {
                Trajectory traj = TargetPlatform.GetTrajectory();
                if (traj != null)
                {
                    traj.SetConfig(result);
                }

                finalPlatformPos = Trajectory.EvaluateWorldPoint(result, Snapshot != null ? Snapshot.InitialPlatformPhase : 0f);
                TargetPlatform.SetEditorData(finalPlatformPos, TargetPlatform.Size, TargetPlatform.RawGrayscaleColor);
            }

            Phase = TrajectoryInteractionPhase.Idle;
            ActiveHandle = TrajectoryHandleKind.None;
            Snapshot = null;
            WorkingPreviewConfig = null;
            return result;
        }

        /// <summary>
        /// Discard preview changes and restore the authoritative snapshot.
        /// </summary>
        public TrajectoryData Rollback(out Vector2 restoredPlatformPos)
        {
            restoredPlatformPos = TargetPlatform != null ? TargetPlatform.Position : Vector2.zero;

            if (Phase != TrajectoryInteractionPhase.Dragging)
                return null;

            TrajectoryData original = Snapshot != null && Snapshot.InitialConfig != null ? Snapshot.InitialConfig.Clone() : null;

            if (TargetPlatform != null && Snapshot != null)
            {
                Trajectory traj = TargetPlatform.GetTrajectory();
                if (traj != null)
                {
                    traj.SetConfig(original);
                }

                restoredPlatformPos = Snapshot.PlatformInitialPosition;
                TargetPlatform.SetEditorData(
                    Snapshot.PlatformInitialPosition,
                    Snapshot.PlatformInitialSize,
                    Snapshot.PlatformInitialColor);
            }

            Phase = TrajectoryInteractionPhase.Idle;
            ActiveHandle = TrajectoryHandleKind.None;
            Snapshot = null;
            WorkingPreviewConfig = null;
            return original;
        }

        /// <summary>
        /// Reset interaction state when target platform is cleared or destroyed.
        /// </summary>
        public void Reset()
        {
            Phase = TrajectoryInteractionPhase.Idle;
            ActiveHandle = TrajectoryHandleKind.None;
            Snapshot = null;
            WorkingPreviewConfig = null;
            TargetPlatform = null;
        }
    }
}
