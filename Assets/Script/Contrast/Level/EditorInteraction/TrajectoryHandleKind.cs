namespace Contrast.Level.EditorInteraction
{
    /// <summary>
    /// Identifies specific interactive handles available during trajectory editing.
    /// Distinguishes trajectory handles from platform geometry resize handles.
    /// </summary>
    public enum TrajectoryHandleKind
    {
        None = 0,

        /// <summary>Yellow handle for world anchor (Line origin / phase 0 position).</summary>
        Origin,

        /// <summary>Cyan handle for Line endpoint (adjusts rotation angle and scale/length).</summary>
        LineEnd,

        /// <summary>Yellow handle for Circle center anchor.</summary>
        CircleCenter,

        /// <summary>Magenta handle for Circle radius adjustment.</summary>
        CircleRadius,

        /// <summary>Lime handle for circular arc start angle (paramMin).</summary>
        ArcStart,

        /// <summary>Lime handle for circular arc end angle (paramMax).</summary>
        ArcEnd,

        /// <summary>Orange handle for minimum X bound of SpatialBounds clipping rectangle.</summary>
        BoundsMinX,

        /// <summary>Orange handle for maximum X bound of SpatialBounds clipping rectangle.</summary>
        BoundsMaxX,

        /// <summary>Orange handle for minimum Y bound of SpatialBounds clipping rectangle.</summary>
        BoundsMinY,

        /// <summary>Orange handle for maximum Y bound of SpatialBounds clipping rectangle.</summary>
        BoundsMaxY
    }

    public static class TrajectoryHandleKindExtensions
    {
        public static bool IsTrajectoryHandle(this TrajectoryHandleKind kind)
        {
            return kind >= TrajectoryHandleKind.Origin && kind <= TrajectoryHandleKind.BoundsMaxY;
        }

        public static bool IsSpatialBoundsHandle(this TrajectoryHandleKind kind)
        {
            return kind >= TrajectoryHandleKind.BoundsMinX && kind <= TrajectoryHandleKind.BoundsMaxY;
        }
    }
}
