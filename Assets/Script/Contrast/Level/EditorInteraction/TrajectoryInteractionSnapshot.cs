using Contrast.Data;
using UnityEngine;

namespace Contrast.Level.EditorInteraction
{
    /// <summary>
    /// Immutable record of trajectory and platform state captured when an editing operation begins.
    /// Used for delta calculations during interactive preview and rollback on cancellation.
    /// </summary>
    public sealed class TrajectoryInteractionSnapshot
    {
        public TrajectoryData InitialConfig { get; }
        public Vector2 PlatformInitialPosition { get; }
        public Vector2 PlatformInitialSize { get; }
        public float PlatformInitialColor { get; }

        public Vector2 DragStartWorld { get; }
        public Vector2 InitialOrigin { get; }
        public Vector2 InitialBoundsMin { get; }
        public Vector2 InitialBoundsMax { get; }

        public float InitialRotation { get; }
        public float InitialScaleX { get; }
        public float InitialRadius { get; }
        public float InitialParamMin { get; }
        public float InitialParamMax { get; }
        public float InitialPlatformPhase { get; }

        public TrajectoryInteractionSnapshot(
            ColorPlatform platform,
            TrajectoryData config,
            Vector2 dragStartWorld)
        {
            DragStartWorld = dragStartWorld;
            PlatformInitialPosition = platform != null ? platform.Position : Vector2.zero;
            PlatformInitialSize = platform != null ? platform.Size : Vector2.one;
            PlatformInitialColor = platform != null ? platform.RawGrayscaleColor : 255f;

            if (config != null)
            {
                InitialConfig = config.Clone();
                InitialOrigin = new Vector2(config.originX, config.originY);
                InitialBoundsMin = new Vector2(config.boundsXMin, config.boundsYMin);
                InitialBoundsMax = new Vector2(config.boundsXMax, config.boundsYMax);
                InitialRotation = config.rotation;
                InitialScaleX = config.scaleX;
                InitialRadius = config.radius;
                InitialParamMin = config.paramMin;
                InitialParamMax = config.paramMax;

                Trajectory.ProjectPointOntoTrajectory(config, PlatformInitialPosition, out float t);
                InitialPlatformPhase = t;
            }
            else
            {
                InitialConfig = null;
                InitialOrigin = PlatformInitialPosition;
                InitialBoundsMin = new Vector2(-10f, -10f);
                InitialBoundsMax = new Vector2(10f, 10f);
                InitialRotation = 0f;
                InitialScaleX = 5f;
                InitialRadius = 2f;
                InitialParamMin = 0f;
                InitialParamMax = 1f;
                InitialPlatformPhase = 0f;
            }
        }
    }
}
