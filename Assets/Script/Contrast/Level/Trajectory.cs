using Contrast.Data;
using UnityEngine;

namespace Contrast.Level
{
    /// <summary>
    /// Reusable trajectory component for <see cref="ColorPlatform"/>.
    ///
    /// Architecture:
    ///   Curve definition  — Line or Circle (extensible via <see cref="TrajectoryCurveType"/>).
    ///   Transform         — origin, rotation, scale (shared).
    ///   Domain            — ParameterRange or SpatialBounds.
    ///   Playback          — Loop, PingPong, Once with bounded phase [0..1].
    ///
    /// One component supports horizontal, vertical, diagonal, full-circle,
    /// and arc motion. Horizontal = Line(rotation=0). Vertical = Line(rotation=90).
    /// Half-circle = Circle(angleMin=0, angleMax=180, PingPong).
    ///
    /// The platform GameObject is never rotated. Rotation applies only to the
    /// trajectory math so that <see cref="ColorPlatform.GetAabb"/> remains axis-aligned.
    ///
    /// Coordinate contract:
    ///   Line   — origin is the phase=0 position. The platform starts here.
    ///   Circle — origin is the circle center. At phase=0 the platform is at
    ///            center + (radius*cos(angleMin), radius*sin(angleMin)).
    ///   On level start/restart, phase resets to 0 and the platform snaps to
    ///   the initial evaluated point with no teleport.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Trajectory : MonoBehaviour
    {
        // ── Serialized configuration ───────────────────────────────
        [SerializeField] private TrajectoryData config;

        // ── Runtime state ──────────────────────────────────────────
        private float phase;            // bounded [0..1]
        private int   pingPongDir = 1;  // +1 forward, -1 backward
        private Vector2 previousWorldPosition;
        private bool  initialized;
        private string validationError;

        // ── Public accessors ───────────────────────────────────────
        public TrajectoryData Config => config;
        public bool IsEnabled => config != null && config.enabled;
        public float Phase => phase;
        public string ValidationError => validationError;

        /// <summary>
        /// Frame displacement of the platform due to trajectory motion.
        /// Used for rider-carry: players standing on a moving platform
        /// receive this displacement.
        /// </summary>
        public Vector2 FrameDisplacement { get; private set; }

        // ── Initialization ─────────────────────────────────────────

        /// <summary>
        /// Assign configuration from deserialized level data or editor.
        /// Resets phase to 0 and snaps to the initial point.
        /// </summary>
        public void SetConfig(TrajectoryData data)
        {
            config = data;
            ResetPhase();
        }

        public static Vector2 GetInitialPosition(TrajectoryData cfg)
        {
            if (cfg == null || !cfg.enabled)
                return cfg != null ? new Vector2(cfg.originX, cfg.originY) : Vector2.zero;
            float startPhase = cfg.reverseDirection ? 1f : 0f;
            return EvaluateWorldPoint(cfg, startPhase);
        }

        /// <summary>
        /// Reset phase and snap platform to its initial trajectory point.
        /// Called on level build/restart/import.
        /// </summary>
        public void ResetPhase()
        {
            phase = (config != null && config.reverseDirection) ? 1f : 0f;
            pingPongDir = (config != null && config.reverseDirection) ? -1 : 1;
            FrameDisplacement = Vector2.zero;
            validationError = null;
            initialized = false;

            if (config == null || !config.enabled)
                return;

            validationError = Validate(config);
            if (validationError != null)
                return;

            Vector2 startPos = EvaluateWorldPoint(config, phase);
            transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
            previousWorldPosition = startPos;
            initialized = true;
        }

        // ── Runtime update (called by TrajectorySystem) ────────────

        /// <summary>
        /// Advance the trajectory phase and move the platform.
        /// Must be called once per gameplay frame, before collision queries.
        /// </summary>
        public void ProcessUpdate(float deltaTime)
        {
            FrameDisplacement = Vector2.zero;

            if (config == null || !config.enabled)
                return;

            if (validationError != null)
                return;

            if (!initialized)
            {
                validationError = Validate(config);
                if (validationError != null)
                    return;
                phase = config.reverseDirection ? 1f : 0f;
                pingPongDir = config.reverseDirection ? -1 : 1;
                Vector2 sp = EvaluateWorldPoint(config, phase);
                transform.position = new Vector3(sp.x, sp.y, transform.position.z);
                previousWorldPosition = sp;
                initialized = true;
            }

            if (config.duration <= 0f)
                return;

            // PingPong duration represents a full trip out and back (0 -> 1 -> 0).
            // Hence traversing one way (0 -> 1) takes duration / 2 seconds.
            float phaseSpeed = (config.playbackMode == TrajectoryPlaybackMode.PingPong)
                ? (2f / config.duration)
                : (1f / config.duration);

            float phaseStep = phaseSpeed * Mathf.Max(0f, deltaTime);

            switch (config.playbackMode)
            {
                case TrajectoryPlaybackMode.Loop:
                {
                    float loopDir = config.reverseDirection ? -1f : 1f;
                    phase = Mathf.Repeat(phase + phaseStep * loopDir, 1f);
                    break;
                }

                case TrajectoryPlaybackMode.PingPong:
                    phase += phaseStep * pingPongDir;
                    if (phase >= 1f)
                    {
                        float overshoot = phase - 1f;
                        int bounces = (int)overshoot;
                        float rem = overshoot - bounces;
                        if (bounces % 2 == 0)
                        {
                            phase = 1f - rem;
                            pingPongDir = -1;
                        }
                        else
                        {
                            phase = rem;
                            pingPongDir = 1;
                        }
                        phase = Mathf.Clamp01(phase);
                    }
                    else if (phase <= 0f)
                    {
                        float overshoot = -phase;
                        int bounces = (int)overshoot;
                        float rem = overshoot - bounces;
                        if (bounces % 2 == 0)
                        {
                            phase = rem;
                            pingPongDir = 1;
                        }
                        else
                        {
                            phase = 1f - rem;
                            pingPongDir = -1;
                        }
                        phase = Mathf.Clamp01(phase);
                    }
                    break;

                case TrajectoryPlaybackMode.Once:
                    if (config.reverseDirection)
                        phase = Mathf.Clamp01(phase - phaseStep);
                    else
                        phase = Mathf.Clamp01(phase + phaseStep);
                    break;
            }

            // Evaluate position
            Vector2 worldPos = EvaluateWorldPoint(config, phase);

            FrameDisplacement = worldPos - previousWorldPosition;
            previousWorldPosition = worldPos;

            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        // ── Curve evaluation (static, pure math) ───────────────────

        /// <summary>
        /// Evaluate the world-space position on the trajectory at the given
        /// normalized phase [0..1].
        /// </summary>
        public static Vector2 EvaluateWorldPoint(TrajectoryData cfg, float normalizedPhase)
        {
            if (cfg == null)
                return Vector2.zero;

            float t = Mathf.Clamp01(normalizedPhase);

            switch (cfg.curveType)
            {
                case TrajectoryCurveType.Line:
                    return EvaluateLine(cfg, t);

                case TrajectoryCurveType.Circle:
                    return EvaluateCircle(cfg, t);

                default:
                    return new Vector2(cfg.originX, cfg.originY);
            }
        }

        // ── Line evaluator ─────────────────────────────────────────

        private static Vector2 EvaluateLine(TrajectoryData cfg, float t)
        {
            Vector2 origin = new Vector2(cfg.originX, cfg.originY);

            float pMin, pMax;
            GetLineDomain(cfg, out pMin, out pMax);

            if (Mathf.Approximately(pMin, pMax))
                return origin;

            // Map normalized phase to parameter u
            float u = Mathf.Lerp(pMin, pMax, t);

            // Normalized basis: C(u) = (u * scaleX, 0)
            float localX = u * cfg.scaleX;

            // Apply rotation in degrees
            float rad = cfg.rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 rotated = new Vector2(
                localX * cos,
                localX * sin);

            return origin + rotated;
        }

        /// <summary>
        /// Get the effective parameter range for a line, accounting for
        /// SpatialBounds domain mode if selected.
        /// </summary>
        public static void GetLineDomain(TrajectoryData cfg, out float pMin, out float pMax)
        {
            if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
            {
                // Clip the infinite transformed line against the axis-aligned bounds.
                ClipLineAgainstBounds(cfg, out pMin, out pMax);
            }
            else
            {
                pMin = cfg.paramMin;
                pMax = cfg.paramMax;
            }
        }

        /// <summary>
        /// Clip the infinite line P(u) = origin + (cos*scaleX, sin*scaleX)*u against
        /// the axis-aligned rectangle [boundsXMin..boundsXMax, boundsYMin..boundsYMax].
        /// Produces the parameter interval [pMin, pMax] of the valid segment.
        /// </summary>
        public static void ClipLineAgainstBounds(
            TrajectoryData cfg,
            out float pMin,
            out float pMax)
        {
            Vector2 origin = new Vector2(cfg.originX, cfg.originY);
            float rad = cfg.rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 dir = new Vector2(cos * cfg.scaleX, sin * cfg.scaleX);

            float tMin = float.NegativeInfinity;
            float tMax = float.PositiveInfinity;

            // X bounds
            if (!ClipAxis(origin.x, dir.x, cfg.boundsXMin, cfg.boundsXMax, ref tMin, ref tMax))
            {
                pMin = 0f;
                pMax = 0f;
                return;
            }

            // Y bounds
            if (!ClipAxis(origin.y, dir.y, cfg.boundsYMin, cfg.boundsYMax, ref tMin, ref tMax))
            {
                pMin = 0f;
                pMax = 0f;
                return;
            }

            if (tMin > tMax)
            {
                pMin = 0f;
                pMax = 0f;
                return;
            }

            pMin = tMin;
            pMax = tMax;
        }

        /// <summary>Liang-Barsky single-axis clip.</summary>
        private static bool ClipAxis(
            float originCoord, float dirCoord,
            float boundsMin, float boundsMax,
            ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(dirCoord) < 1e-8f)
            {
                // Parallel to axis: origin must be inside bounds
                return originCoord >= boundsMin && originCoord <= boundsMax;
            }

            float t1 = (boundsMin - originCoord) / dirCoord;
            float t2 = (boundsMax - originCoord) / dirCoord;

            if (t1 > t2)
            {
                float tmp = t1; t1 = t2; t2 = tmp;
            }

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);

            return tMin <= tMax;
        }

        // ── Circle evaluator ───────────────────────────────────────

        private static Vector2 EvaluateCircle(TrajectoryData cfg, float t)
        {
            Vector2 center = new Vector2(cfg.originX, cfg.originY);
            float r = Mathf.Max(0.001f, cfg.radius);

            // Map phase to angle in degrees
            float angleDeg = Mathf.Lerp(cfg.paramMin, cfg.paramMax, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            return center + new Vector2(
                r * Mathf.Cos(angleRad),
                r * Mathf.Sin(angleRad));
        }

        /// <summary>
        /// Project a world-space point onto the trajectory path, returning the closest point
        /// on the curve and its normalized phase t in [0, 1].
        /// </summary>
        public static Vector2 ProjectPointOntoTrajectory(TrajectoryData cfg, Vector2 point, out float normalizedT)
        {
            if (cfg == null || !cfg.enabled)
            {
                normalizedT = 0f;
                return point;
            }

            switch (cfg.curveType)
            {
                case TrajectoryCurveType.Line:
                {
                    Vector2 a = EvaluateWorldPoint(cfg, 0f);
                    Vector2 b = EvaluateWorldPoint(cfg, 1f);
                    Vector2 ab = b - a;
                    float abSqr = ab.sqrMagnitude;
                    if (abSqr < 1e-8f)
                    {
                        normalizedT = 0f;
                        return a;
                    }
                    float t = Vector2.Dot(point - a, ab) / abSqr;
                    normalizedT = Mathf.Clamp01(t);
                    return a + normalizedT * ab;
                }

                case TrajectoryCurveType.Circle:
                {
                    Vector2 center = new Vector2(cfg.originX, cfg.originY);
                    Vector2 d = point - center;
                    float angleDeg;
                    if (d.sqrMagnitude < 1e-8f)
                    {
                        angleDeg = cfg.paramMin;
                    }
                    else
                    {
                        angleDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                        if (angleDeg < 0f) angleDeg += 360f;
                    }

                    float span = cfg.paramMax - cfg.paramMin;
                    if (Mathf.Abs(span) >= 355f)
                    {
                        float relAngle = Mathf.Repeat(angleDeg - cfg.paramMin, 360f);
                        normalizedT = Mathf.Clamp01(relAngle / Mathf.Abs(span));
                        return EvaluateWorldPoint(cfg, normalizedT);
                    }
                    else
                    {
                        float bestT = 0f;
                        float minAngleDiff = float.MaxValue;
                        const int samples = 36;
                        for (int i = 0; i <= samples; i++)
                        {
                            float candT = (float)i / samples;
                            float candAngle = Mathf.Lerp(cfg.paramMin, cfg.paramMax, candT);
                            float diff = Mathf.Abs(Mathf.DeltaAngle(candAngle, angleDeg));
                            if (diff < minAngleDiff)
                            {
                                minAngleDiff = diff;
                                bestT = candT;
                            }
                        }

                        float step = 1f / samples;
                        float tLow = Mathf.Max(0f, bestT - step);
                        float tHigh = Mathf.Min(1f, bestT + step);
                        for (int i = 0; i < 10; i++)
                        {
                            float tMid1 = tLow + (tHigh - tLow) * 0.382f;
                            float tMid2 = tLow + (tHigh - tLow) * 0.618f;
                            float diff1 = Mathf.Abs(Mathf.DeltaAngle(Mathf.Lerp(cfg.paramMin, cfg.paramMax, tMid1), angleDeg));
                            float diff2 = Mathf.Abs(Mathf.DeltaAngle(Mathf.Lerp(cfg.paramMin, cfg.paramMax, tMid2), angleDeg));
                            if (diff1 < diff2)
                                tHigh = tMid2;
                            else
                                tLow = tMid1;
                        }

                        normalizedT = (tLow + tHigh) * 0.5f;
                        return EvaluateWorldPoint(cfg, normalizedT);
                    }
                }

                default:
                {
                    normalizedT = 0f;
                    return new Vector2(cfg.originX, cfg.originY);
                }
            }
        }

        // ── Validation ─────────────────────────────────────────────

        /// <summary>
        /// Returns null if valid, or a human-readable error string.
        /// </summary>
        public static string Validate(TrajectoryData cfg)
        {
            if (cfg == null)
                return null; // null means stationary, not an error

            if (!cfg.enabled)
                return null;

            if (cfg.duration <= 0f)
                return "Duration must be positive.";

            switch (cfg.curveType)
            {
                case TrajectoryCurveType.Line:
                    return ValidateLine(cfg);

                case TrajectoryCurveType.Circle:
                    return ValidateCircle(cfg);

                default:
                    return $"Unknown curve type: {cfg.curveType}";
            }
        }

        private static string ValidateLine(TrajectoryData cfg)
        {
            if (Mathf.Abs(cfg.scaleX) < 1e-6f)
                return "Line scale must be non-zero.";

            if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
            {
                if (cfg.boundsXMin >= cfg.boundsXMax)
                    return "Spatial bounds X: min must be less than max.";
                if (cfg.boundsYMin >= cfg.boundsYMax)
                    return "Spatial bounds Y: min must be less than max.";

                ClipLineAgainstBounds(cfg, out float pMin, out float pMax);
                if (Mathf.Approximately(pMin, pMax))
                    return "Line does not intersect the spatial bounds rectangle.";
            }
            else
            {
                if (Mathf.Approximately(cfg.paramMin, cfg.paramMax))
                    return "Parameter min equals max — no motion path.";
            }

            return null;
        }

        private static string ValidateCircle(TrajectoryData cfg)
        {
            if (cfg.radius <= 0f)
                return "Circle radius must be positive.";

            float angleDeg = cfg.paramMax - cfg.paramMin;
            if (Mathf.Abs(angleDeg) < 0.01f)
                return "Circle angle range is zero — no motion path.";

            if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
                return "SpatialBounds domain mode is not supported for Circle.";

            return null;
        }

        // ── Editor preview helpers ─────────────────────────────────

        /// <summary>
        /// Produce a list of world-space points for drawing the trajectory path.
        /// </summary>
        public static Vector2[] GetPreviewPoints(TrajectoryData cfg, int segments = 64)
        {
            if (cfg == null || !cfg.enabled)
                return System.Array.Empty<Vector2>();

            if (Validate(cfg) != null)
                return System.Array.Empty<Vector2>();

            Vector2[] points = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                points[i] = EvaluateWorldPoint(cfg, t);
            }
            return points;
        }

        // ── Gizmo drawing (Scene view) ──────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (config == null || !config.enabled)
                return;

            Vector2[] points = GetPreviewPoints(config, 64);
            if (points.Length < 2)
                return;

            Gizmos.color = new UnityEngine.Color(0f, 1f, 0.4f, 0.8f);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Gizmos.DrawLine(
                    new Vector3(points[i].x, points[i].y, -0.5f),
                    new Vector3(points[i + 1].x, points[i + 1].y, -0.5f));
            }

            // Draw origin marker
            Gizmos.color = UnityEngine.Color.yellow;
            Vector3 originPos = new Vector3(config.originX, config.originY, -0.5f);
            Gizmos.DrawWireSphere(originPos, 0.15f);
        }
    }
}
