using UnityEngine;

namespace Contrast.Data
{
    /// <summary>
    /// Curve family selector. Adding a new curve requires only a new enum value
    /// and a matching evaluator branch in Trajectory — no new component or
    /// duplicated playback logic.
    /// </summary>
    public enum TrajectoryCurveType
    {
        Line   = 0,
        Circle = 1
    }

    /// <summary>
    /// How the valid portion of the curve is determined.
    /// </summary>
    public enum TrajectoryDomainMode
    {
        /// <summary>
        /// Use paramMin / paramMax directly on the curve parameter.
        /// For Line: coordinate along local axis.
        /// For Circle: angle in degrees.
        /// </summary>
        ParameterRange = 0,

        /// <summary>
        /// Line only. Clip the infinite line against an axis-aligned bounding
        /// rectangle (boundsXMin..boundsXMax, boundsYMin..boundsYMax).
        /// </summary>
        SpatialBounds  = 1
    }

    /// <summary>
    /// How the platform progresses along the domain.
    /// </summary>
    public enum TrajectoryPlaybackMode
    {
        Loop     = 0,
        PingPong = 1,
        Once     = 2
    }

    /// <summary>
    /// Serializable trajectory configuration.
    ///
    /// Unity JsonUtility serializes enums as their underlying int value.
    /// A missing trajectory field in legacy JSON deserializes as null,
    /// which means "stationary platform" — fully backward-compatible.
    ///
    /// Shared fields live here rather than being duplicated per curve type.
    /// Curve-specific scalar fields are kept to 0–3 (currently only radius
    /// for Circle).
    /// </summary>
    [System.Serializable]
    public class TrajectoryData
    {
        // ── Master switch ──────────────────────────────────────────
        public bool enabled = false;

        // ── Curve type ─────────────────────────────────────────────
        /// <summary>JsonUtility serializes as int (0=Line, 1=Circle).</summary>
        public TrajectoryCurveType curveType = TrajectoryCurveType.Line;

        // ── Domain ─────────────────────────────────────────────────
        public TrajectoryDomainMode domainMode = TrajectoryDomainMode.ParameterRange;

        // ── Playback ───────────────────────────────────────────────
        public TrajectoryPlaybackMode playbackMode = TrajectoryPlaybackMode.PingPong;

        // ── Transform: origin/offset, rotation, scale ──────────────
        /// <summary>
        /// Trajectory anchor in world coordinates.
        /// Line: the starting endpoint (phase=0 position).
        /// Circle: the circle center.
        /// </summary>
        public float originX = 0f;
        public float originY = 0f;

        /// <summary>
        /// Rotation of the trajectory path in degrees (counter-clockwise).
        /// Applies to Line direction. Circle ignores this (uses angle range).
        /// </summary>
        public float rotation = 0f;

        /// <summary>
        /// Scale of the curve. For Line: length of the full segment when
        /// paramMin=0, paramMax=1. For Circle: unused (use radius instead).
        /// </summary>
        public float scaleX = 5f;
        public float scaleY = 1f;

        // ── Domain parameters ──────────────────────────────────────
        /// <summary>
        /// Curve parameter range.
        /// Line: local axis coordinate min/max (0..1 maps to 0..scaleX).
        /// Circle: angle range in degrees (e.g. 0..360 for full, 0..180 for half).
        /// </summary>
        public float paramMin = 0f;
        public float paramMax = 1f;

        /// <summary>Spatial bounds for SpatialBounds domain mode (Line only).</summary>
        public float boundsXMin = -10f;
        public float boundsXMax =  10f;
        public float boundsYMin = -10f;
        public float boundsYMax =  10f;

        // ── Circle-specific ────────────────────────────────────────
        /// <summary>Circle radius in world units. Must be positive.</summary>
        public float radius = 2f;

        // ── Timing ─────────────────────────────────────────────────
        /// <summary>
        /// Duration of one full traversal in seconds.
        /// PingPong: time for a full out-and-back trip.
        /// Loop: time for one revolution/full sweep.
        /// Once: time to reach the end.
        /// Must be positive.
        /// </summary>
        public float duration = 4f;

        /// <summary>
        /// If true, phase advances in reverse at start (1→0 instead of 0→1).
        /// </summary>
        public bool reverseDirection = false;

        // ── Factory helpers ────────────────────────────────────────

        /// <summary>
        /// Create a default disabled trajectory anchored at the given position.
        /// </summary>
        public static TrajectoryData CreateDefault(Vector2 platformPosition)
        {
            return new TrajectoryData
            {
                enabled  = false,
                originX  = platformPosition.x,
                originY  = platformPosition.y,
                curveType = TrajectoryCurveType.Line,
                domainMode = TrajectoryDomainMode.ParameterRange,
                playbackMode = TrajectoryPlaybackMode.PingPong,
                rotation  = 0f,
                scaleX    = 5f,
                scaleY    = 1f,
                paramMin  = 0f,
                paramMax  = 1f,
                radius    = 2f,
                duration  = 4f,
                reverseDirection = false
            };
        }

        /// <summary>
        /// Deep copy so runtime edits do not mutate import data.
        /// </summary>
        public TrajectoryData Clone()
        {
            return new TrajectoryData
            {
                enabled          = enabled,
                curveType        = curveType,
                domainMode       = domainMode,
                playbackMode     = playbackMode,
                originX          = originX,
                originY          = originY,
                rotation         = rotation,
                scaleX           = scaleX,
                scaleY           = scaleY,
                paramMin         = paramMin,
                paramMax         = paramMax,
                boundsXMin       = boundsXMin,
                boundsXMax       = boundsXMax,
                boundsYMin       = boundsYMin,
                boundsYMax       = boundsYMax,
                radius           = radius,
                duration         = duration,
                reverseDirection = reverseDirection
            };
        }
    }
}
