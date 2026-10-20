using System;
using System.Collections.Generic;
using System.Text;
using Contrast.Level;
using UnityEngine;

namespace Contrast.Data
{
    /// <summary>
    /// Validates, repairs, and serializes LevelData.
    /// Distinguishes missing vs. default objects in raw JSON, eliminates phantom objects,
    /// repairs invalid trajectory data and camera boundaries, and logs all repairs.
    /// </summary>
    public static class LevelDataSanitizer
    {
        public const string LogPrefix = "[LEVEL_FIX]";

        /// <summary>
        /// Sanitizes LevelData, repairing malformed fields, removing NaN/Infinity values,
        /// and recovering missing or legacy objects.
        /// </summary>
        /// <param name="data">The deserialized LevelData instance.</param>
        /// <param name="rawJson">The original raw JSON text used to verify field presence.</param>
        /// <param name="report">List of all fixes, warnings, and adjustments made.</param>
        /// <returns>The sanitized LevelData instance.</returns>
        public static LevelData Sanitize(LevelData data, string rawJson, out List<string> report)
        {
            report = new List<string>();

            if (data == null)
            {
                report.Add("Input LevelData was null. Generated a new empty LevelData.");
                data = new LevelData();
            }

            // ── 1. Inspect raw JSON for actual object presence ──────────────
            if (!string.IsNullOrWhiteSpace(rawJson))
            {
                if (!IsFieldPresentAndNonNull(rawJson, "end"))
                {
                    if (data.end != null)
                    {
                        data.end = null;
                        report.Add("Raw JSON omits 'end' (or is null). Suppressed default phantom End marker.");
                    }
                }

                if (!IsFieldPresentAndNonNull(rawJson, "cameraBoundary"))
                {
                    if (data.cameraBoundary != null)
                    {
                        data.cameraBoundary = null;
                        report.Add("Raw JSON omits 'cameraBoundary' (or is null). Suppressed default phantom boundary.");
                    }
                }

                if (!IsFieldPresentAndNonNull(rawJson, "startPos"))
                {
                    data.startPos = null;
                }

                if (!IsFieldPresentAndNonNull(rawJson, "spawnPoint"))
                {
                    data.spawnPoint = null;
                }

                // Check for single object "checkPoint": { ... } vs array "checkPoint": [ ... ]
                if ((data.checkPoint == null || data.checkPoint.Length == 0) && IsFieldPresentAndNonNull(rawJson, "checkPoint"))
                {
                    string singleObjJson = ExtractTopLevelFieldSnippet(rawJson, "checkPoint");
                    if (!string.IsNullOrEmpty(singleObjJson) && singleObjJson.TrimStart().StartsWith("{"))
                    {
                        try
                        {
                            CheckPointData singleCp = JsonUtility.FromJson<CheckPointData>(singleObjJson);
                            if (singleCp != null && singleCp.position != null)
                            {
                                data.checkPoint = new CheckPointData[] { singleCp };
                                report.Add("Recovered legacy single checkPoint object from raw JSON into checkPoint array.");
                            }
                        }
                        catch (Exception ex)
                        {
                            report.Add($"Failed to parse single checkPoint object: {ex.Message}");
                        }
                    }
                }
            }

            // ── 2. Platforms Sanitization ──────────────────────────────────
            List<ColorPlatformData> validPlatforms = new List<ColorPlatformData>();
            if (data.platforms != null)
            {
                for (int i = 0; i < data.platforms.Length; i++)
                {
                    ColorPlatformData p = data.platforms[i];
                    if (p == null)
                    {
                        report.Add($"Removed null platform at index {i}.");
                        continue;
                    }

                    // Position validation
                    if (p.position == null ||
                        float.IsNaN(p.position.x) || float.IsNaN(p.position.y) ||
                        float.IsInfinity(p.position.x) || float.IsInfinity(p.position.y))
                    {
                        report.Add($"Removed platform at index {i} due to NaN or Infinity position.");
                        continue;
                    }

                    // Size validation
                    if (p.size == null)
                    {
                        p.size = new Vector2Data(1f, 1f);
                        report.Add($"Platform at index {i} had null size; reset to (1.0, 1.0).");
                    }
                    else
                    {
                        if (float.IsNaN(p.size.x) || float.IsInfinity(p.size.x) || p.size.x <= 0f)
                        {
                            float repairedX = Mathf.Max(0.05f, Mathf.Abs(float.IsNaN(p.size.x) || float.IsInfinity(p.size.x) ? 1f : p.size.x));
                            report.Add($"Repaired non-positive/NaN width on platform {i} to {repairedX:F2}.");
                            p.size.x = repairedX;
                        }

                        if (float.IsNaN(p.size.y) || float.IsInfinity(p.size.y) || p.size.y <= 0f)
                        {
                            float repairedY = Mathf.Max(0.05f, Mathf.Abs(float.IsNaN(p.size.y) || float.IsInfinity(p.size.y) ? 1f : p.size.y));
                            report.Add($"Repaired non-positive/NaN height on platform {i} to {repairedY:F2}.");
                            p.size.y = repairedY;
                        }
                    }

                    // Color validation
                    if (float.IsNaN(p.rawGrayscaleColor) || float.IsInfinity(p.rawGrayscaleColor))
                    {
                        p.rawGrayscaleColor = 255f;
                        report.Add($"Repaired platform {i} color from NaN/Infinity to 255 (Universal).");
                    }
                    else
                    {
                        float clamped = Mathf.Clamp(p.rawGrayscaleColor, 0f, 255f);
                        if (Mathf.Abs(clamped - p.rawGrayscaleColor) > 0.001f)
                        {
                            report.Add($"Clamped platform {i} color from {p.rawGrayscaleColor:F1} to {clamped:F1}.");
                            p.rawGrayscaleColor = clamped;
                        }
                    }

                    // Trajectory validation & repair
                    if (p.trajectory != null && p.trajectory.enabled)
                    {
                        SanitizeTrajectory(p, report, i);
                    }

                    validPlatforms.Add(p);
                }
            }
            data.platforms = validPlatforms.ToArray();

            // ── 3. StartPos Handling ───────────────────────────────────────
            if (data.startPos == null)
            {
                if (data.spawnPoint != null)
                {
                    float legacyColor = data.spawnPoint.initialPlayerColor >= 0f
                        ? data.spawnPoint.initialPlayerColor
                        : data.spawnPoint.color;

                    data.startPos = new StartPosData
                    {
                        position = data.spawnPoint.position ?? new Vector2Data(0f, 1f),
                        size = new Vector2Data(1f, 1f),
                        color = legacyColor
                    };
                    report.Add("Converted legacy spawnPoint to startPos.");
                }
                else
                {
                    Vector2 defPos;
                    if (data.platforms != null && data.platforms.Length > 0 && data.platforms[0].position != null)
                    {
                        defPos = new Vector2(
                            data.platforms[0].position.x,
                            data.platforms[0].position.y + (data.platforms[0].size?.y ?? 1f) * 0.5f + 1f);
                    }
                    else
                    {
                        defPos = new Vector2(0f, 1f);
                    }

                    data.startPos = new StartPosData
                    {
                        position = new Vector2Data(defPos.x, defPos.y),
                        size = new Vector2Data(1f, 1f),
                        color = 0f
                    };
                    report.Add($"Missing StartPos created above platform at ({defPos.x:F1}, {defPos.y:F1}).");
                }
            }
            else
            {
                // Validate existing startPos
                if (data.startPos.position == null ||
                    float.IsNaN(data.startPos.position.x) || float.IsNaN(data.startPos.position.y) ||
                    float.IsInfinity(data.startPos.position.x) || float.IsInfinity(data.startPos.position.y))
                {
                    Vector2 defPos = (data.platforms != null && data.platforms.Length > 0 && data.platforms[0].position != null)
                        ? new Vector2(data.platforms[0].position.x, data.platforms[0].position.y + 1.5f)
                        : new Vector2(0f, 1f);
                    data.startPos.position = new Vector2Data(defPos.x, defPos.y);
                    report.Add($"Repaired startPos NaN/Infinity position to ({defPos.x:F1}, {defPos.y:F1}).");
                }

                if (data.startPos.size == null || data.startPos.size.x <= 0f || data.startPos.size.y <= 0f ||
                    float.IsNaN(data.startPos.size.x) || float.IsNaN(data.startPos.size.y) ||
                    float.IsInfinity(data.startPos.size.x) || float.IsInfinity(data.startPos.size.y))
                {
                    data.startPos.size = new Vector2Data(1f, 1f);
                    report.Add("Repaired startPos size to (1.0, 1.0).");
                }

                if (float.IsNaN(data.startPos.color) || float.IsInfinity(data.startPos.color))
                {
                    data.startPos.color = 0f;
                    report.Add("Repaired startPos NaN/Infinity color to 0.");
                }
                else
                {
                    data.startPos.color = Mathf.Clamp(data.startPos.color, 0f, 255f);
                }
            }

            // ── 4. Checkpoints Validation & Deduplication ──────────────────
            if (data.checkPoint != null)
            {
                List<CheckPointData> validCheckpoints = new List<CheckPointData>();
                HashSet<string> seenPositions = new HashSet<string>();

                for (int i = 0; i < data.checkPoint.Length; i++)
                {
                    CheckPointData cp = data.checkPoint[i];
                    if (cp == null)
                    {
                        report.Add($"Removed null checkpoint at index {i}.");
                        continue;
                    }

                    if (cp.position == null ||
                        float.IsNaN(cp.position.x) || float.IsNaN(cp.position.y) ||
                        float.IsInfinity(cp.position.x) || float.IsInfinity(cp.position.y))
                    {
                        report.Add($"Removed checkpoint at index {i} due to NaN/Infinity position.");
                        continue;
                    }

                    string posKey = $"{Mathf.Round(cp.position.x * 100f) / 100f}_{Mathf.Round(cp.position.y * 100f) / 100f}";
                    if (seenPositions.Contains(posKey))
                    {
                        report.Add($"Removed duplicate checkpoint at ({cp.position.x:F2}, {cp.position.y:F2}).");
                        continue;
                    }
                    seenPositions.Add(posKey);

                    if (cp.size == null || cp.size.x <= 0f || cp.size.y <= 0f ||
                        float.IsNaN(cp.size.x) || float.IsNaN(cp.size.y) ||
                        float.IsInfinity(cp.size.x) || float.IsInfinity(cp.size.y))
                    {
                        cp.size = new Vector2Data(1f, 1f);
                        report.Add($"Repaired checkpoint size at index {i} to (1.0, 1.0).");
                    }

                    if (float.IsNaN(cp.color) || float.IsInfinity(cp.color))
                    {
                        cp.color = 0f;
                        report.Add($"Repaired checkpoint color at index {i} to 0.");
                    }
                    else
                    {
                        cp.color = Mathf.Clamp(cp.color, 0f, 255f);
                    }

                    validCheckpoints.Add(cp);
                }
                data.checkPoint = validCheckpoints.ToArray();
            }

            // ── 5. End Marker Validation ───────────────────────────────────
            if (data.end != null)
            {
                if (data.end.position == null ||
                    float.IsNaN(data.end.position.x) || float.IsNaN(data.end.position.y) ||
                    float.IsInfinity(data.end.position.x) || float.IsInfinity(data.end.position.y))
                {
                    data.end = null;
                    report.Add("Removed End marker with NaN/Infinity position.");
                }
                else if (data.end.size == null || data.end.size.x <= 0f || data.end.size.y <= 0f ||
                         float.IsNaN(data.end.size.x) || float.IsNaN(data.end.size.y) ||
                         float.IsInfinity(data.end.size.x) || float.IsInfinity(data.end.size.y))
                {
                    data.end.size = new Vector2Data(1.5f, 2f);
                    report.Add("Repaired End marker size to (1.5, 2.0).");
                }
            }

            if (data.end == null)
            {
                report.Add("[WARNING] Level does not have an End marker.");
            }

            // ── 6. Camera Boundary Repair / Auto-Generation ────────────────
            bool boundaryInvalid = data.cameraBoundary == null ||
                                   data.cameraBoundary.position == null ||
                                   data.cameraBoundary.size == null ||
                                   float.IsNaN(data.cameraBoundary.position.x) || float.IsNaN(data.cameraBoundary.position.y) ||
                                   float.IsInfinity(data.cameraBoundary.position.x) || float.IsInfinity(data.cameraBoundary.position.y) ||
                                   float.IsNaN(data.cameraBoundary.size.x) || float.IsNaN(data.cameraBoundary.size.y) ||
                                   float.IsInfinity(data.cameraBoundary.size.x) || float.IsInfinity(data.cameraBoundary.size.y) ||
                                   data.cameraBoundary.size.x <= 0f || data.cameraBoundary.size.y <= 0f;

            if (boundaryInvalid)
            {
                data.cameraBoundary = CalculateCameraBoundary(data);
                report.Add($"Calculated camera boundary: center=({data.cameraBoundary.position.x:F1}, {data.cameraBoundary.position.y:F1}), size=({data.cameraBoundary.size.x:F1}, {data.cameraBoundary.size.y:F1}).");
            }

            return data;
        }

        private static void SanitizeTrajectory(ColorPlatformData p, List<string> report, int index)
        {
            TrajectoryData t = p.trajectory;
            if (t == null || !t.enabled)
                return;

            string initialError = Trajectory.Validate(t);
            if (initialError != null)
            {
                report.Add($"Platform {index} trajectory invalid ({initialError}); repairing parameters.");

                if (float.IsNaN(t.originX) || float.IsInfinity(t.originX) || float.IsNaN(t.originY) || float.IsInfinity(t.originY))
                {
                    t.originX = p.position.x;
                    t.originY = p.position.y;
                    report.Add($"Repaired platform {index} trajectory origin to platform position.");
                }

                if (float.IsNaN(t.duration) || float.IsInfinity(t.duration) || t.duration <= 0f)
                {
                    t.duration = 4f;
                    report.Add($"Repaired platform {index} trajectory duration to 4s.");
                }

                if (float.IsNaN(t.radius) || float.IsInfinity(t.radius) || t.radius <= 0f)
                {
                    t.radius = 2f;
                    report.Add($"Repaired platform {index} trajectory radius to 2.");
                }

                if (float.IsNaN(t.scaleX) || float.IsInfinity(t.scaleX) || Mathf.Abs(t.scaleX) < 1e-6f)
                {
                    t.scaleX = 5f;
                    report.Add($"Repaired platform {index} trajectory scaleX to 5.");
                }

                if (float.IsNaN(t.scaleY) || float.IsInfinity(t.scaleY)) t.scaleY = 1f;
                if (float.IsNaN(t.rotation) || float.IsInfinity(t.rotation)) t.rotation = 0f;

                if (!Enum.IsDefined(typeof(TrajectoryCurveType), t.curveType))
                {
                    t.curveType = TrajectoryCurveType.Line;
                    report.Add($"Repaired platform {index} curveType to Line.");
                }

                if (!Enum.IsDefined(typeof(TrajectoryDomainMode), t.domainMode))
                {
                    t.domainMode = TrajectoryDomainMode.ParameterRange;
                    report.Add($"Repaired platform {index} domainMode to ParameterRange.");
                }

                if (!Enum.IsDefined(typeof(TrajectoryPlaybackMode), t.playbackMode))
                {
                    t.playbackMode = TrajectoryPlaybackMode.PingPong;
                    report.Add($"Repaired platform {index} playbackMode to PingPong.");
                }

                if (t.curveType == TrajectoryCurveType.Circle && t.domainMode == TrajectoryDomainMode.SpatialBounds)
                {
                    t.domainMode = TrajectoryDomainMode.ParameterRange;
                    report.Add($"Repaired platform {index} Circle with SpatialBounds to ParameterRange.");
                }

                if (t.curveType == TrajectoryCurveType.Line)
                {
                    if (Mathf.Approximately(t.paramMin, t.paramMax) || float.IsNaN(t.paramMin) || float.IsNaN(t.paramMax))
                    {
                        t.paramMin = 0f;
                        t.paramMax = 1f;
                        report.Add($"Repaired platform {index} Line parameter range to [0, 1].");
                    }

                    if (t.domainMode == TrajectoryDomainMode.SpatialBounds)
                    {
                        if (t.boundsXMin >= t.boundsXMax || float.IsNaN(t.boundsXMin) || float.IsNaN(t.boundsXMax))
                        {
                            t.boundsXMin = t.originX - 10f;
                            t.boundsXMax = t.originX + 10f;
                            report.Add($"Repaired platform {index} spatial bounds X.");
                        }

                        if (t.boundsYMin >= t.boundsYMax || float.IsNaN(t.boundsYMin) || float.IsNaN(t.boundsYMax))
                        {
                            t.boundsYMin = t.originY - 10f;
                            t.boundsYMax = t.originY + 10f;
                            report.Add($"Repaired platform {index} spatial bounds Y.");
                        }

                        Trajectory.ClipLineAgainstBounds(t, out float pMin, out float pMax);
                        if (Mathf.Approximately(pMin, pMax))
                        {
                            t.domainMode = TrajectoryDomainMode.ParameterRange;
                            t.paramMin = 0f;
                            t.paramMax = 1f;
                            report.Add($"Platform {index} Line does not intersect spatial bounds; reverted domain mode to ParameterRange [0, 1].");
                        }
                    }
                }
                else if (t.curveType == TrajectoryCurveType.Circle)
                {
                    if (Mathf.Abs(t.paramMax - t.paramMin) < 0.01f || float.IsNaN(t.paramMin) || float.IsNaN(t.paramMax))
                    {
                        t.paramMin = 0f;
                        t.paramMax = 360f;
                        report.Add($"Repaired platform {index} Circle angle range to [0, 360].");
                    }
                }

                string reValError = Trajectory.Validate(t);
                if (reValError != null)
                {
                    t.enabled = false;
                    report.Add($"Platform {index} trajectory still invalid ({reValError}); disabled trajectory and preserved static platform.");
                }
            }

            // Mandatory trajectory contract: platform position corresponds to the trajectory's initial position
            if (t.enabled)
            {
                Vector2 initialPos = Trajectory.GetInitialPosition(t);
                p.position = new Vector2Data(initialPos.x, initialPos.y);
            }
        }

        private static CameraBoundaryData CalculateCameraBoundary(LevelData data)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            void IncludePoint(float x, float y)
            {
                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                    return;

                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }

            void IncludeBox(Vector2 center, Vector2 size)
            {
                float hx = Mathf.Abs(size.x) * 0.5f;
                float hy = Mathf.Abs(size.y) * 0.5f;
                IncludePoint(center.x - hx, center.y - hy);
                IncludePoint(center.x + hx, center.y + hy);
            }

            if (data.startPos != null && data.startPos.position != null)
                IncludeBox(data.startPos.position.ToVector2(), data.startPos.size?.ToVector2() ?? Vector2.one);

            if (data.checkPoint != null)
            {
                foreach (CheckPointData cp in data.checkPoint)
                {
                    if (cp != null && cp.position != null)
                        IncludeBox(cp.position.ToVector2(), cp.size?.ToVector2() ?? Vector2.one);
                }
            }

            if (data.end != null && data.end.position != null)
                IncludeBox(data.end.position.ToVector2(), data.end.size?.ToVector2() ?? new Vector2(1.5f, 2f));

            if (data.platforms != null)
            {
                foreach (ColorPlatformData p in data.platforms)
                {
                    if (p == null || p.position == null)
                        continue;

                    Vector2 pSize = p.size != null ? p.size.ToVector2() : Vector2.one;
                    IncludeBox(p.position.ToVector2(), pSize);

                    if (p.trajectory != null && p.trajectory.enabled && Trajectory.Validate(p.trajectory) == null)
                    {
                        Vector2[] preview = Trajectory.GetPreviewPoints(p.trajectory, 16);
                        if (preview != null && preview.Length > 0)
                        {
                            foreach (Vector2 pt in preview)
                                IncludeBox(pt, pSize);
                        }
                    }
                }
            }

            if (minX == float.MaxValue)
            {
                minX = -10f;
                maxX = 10f;
                minY = -5f;
                maxY = 5f;
            }

            // Margin around level
            float marginX = 8f;
            float marginY = 6f;

            minX -= marginX;
            maxX += marginX;
            minY -= marginY;
            maxY += marginY;

            // Visible camera span is ~25x15 units; ensure boundary is strictly at least this size
            float width = Mathf.Max(maxX - minX, 30f);
            float height = Mathf.Max(maxY - minY, 18f);

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            return new CameraBoundaryData
            {
                position = new Vector2Data(centerX, centerY),
                size = new Vector2Data(width, height)
            };
        }

        /// <summary>
        /// Inspects the original JSON to determine if a top-level field exists and is non-null.
        /// Prevents default-instantiated objects from JsonUtility from being treated as authored.
        /// </summary>
        public static bool IsFieldPresentAndNonNull(string json, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            int i = 0;
            int len = json.Length;

            while (i < len && json[i] != '{') i++;
            if (i >= len) return false;
            i++; // into root object

            int depth = 1;
            while (i < len)
            {
                char c = json[i];

                if (c == '"')
                {
                    int strStart = i + 1;
                    i++;
                    while (i < len && json[i] != '"')
                    {
                        if (json[i] == '\\') i++;
                        i++;
                    }
                    int strEnd = i;
                    if (i < len) i++;

                    if (depth == 1)
                    {
                        string key = json.Substring(strStart, strEnd - strStart);

                        while (i < len && char.IsWhiteSpace(json[i])) i++;
                        if (i < len && json[i] == ':')
                        {
                            i++;
                            while (i < len && char.IsWhiteSpace(json[i])) i++;

                            if (string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase))
                            {
                                if (i + 4 <= len && json.Substring(i, 4) == "null")
                                    return false;

                                return true;
                            }
                        }
                    }
                    continue;
                }

                if (c == '{' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (depth == 0)
                        break;
                }

                i++;
            }

            return false;
        }

        /// <summary>
        /// Extracts the raw JSON snippet for a top-level field value (e.g. { ... } or [ ... ]).
        /// </summary>
        public static string ExtractTopLevelFieldSnippet(string json, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            int i = 0;
            int len = json.Length;

            while (i < len && json[i] != '{') i++;
            if (i >= len) return null;
            i++;

            int depth = 1;
            while (i < len)
            {
                char c = json[i];

                if (c == '"')
                {
                    int strStart = i + 1;
                    i++;
                    while (i < len && json[i] != '"')
                    {
                        if (json[i] == '\\') i++;
                        i++;
                    }
                    int strEnd = i;
                    if (i < len) i++;

                    if (depth == 1)
                    {
                        string key = json.Substring(strStart, strEnd - strStart);

                        while (i < len && char.IsWhiteSpace(json[i])) i++;
                        if (i < len && json[i] == ':')
                        {
                            i++;
                            while (i < len && char.IsWhiteSpace(json[i])) i++;

                            if (string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase))
                            {
                                int valStart = i;
                                if (json[i] == '{' || json[i] == '[')
                                {
                                    char closeChar = json[i] == '{' ? '}' : ']';
                                    int valDepth = 1;
                                    i++;
                                    while (i < len && valDepth > 0)
                                    {
                                        if (json[i] == '"')
                                        {
                                            i++;
                                            while (i < len && json[i] != '"')
                                            {
                                                if (json[i] == '\\') i++;
                                                i++;
                                            }
                                        }
                                        else if (json[i] == '{' || json[i] == '[')
                                        {
                                            valDepth++;
                                        }
                                        else if (json[i] == '}' || json[i] == ']')
                                        {
                                            valDepth--;
                                        }
                                        i++;
                                    }
                                    return json.Substring(valStart, i - valStart);
                                }
                                else
                                {
                                    while (i < len && json[i] != ',' && json[i] != '}') i++;
                                    return json.Substring(valStart, i - valStart).Trim();
                                }
                            }
                        }
                    }
                    continue;
                }

                if (c == '{' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (depth == 0)
                        break;
                }

                i++;
            }

            return null;
        }

        /// <summary>
        /// Serializes LevelData to clean JSON without phantom objects, omitting legacy spawnPoint
        /// and omitting trajectories on static platforms.
        /// </summary>
        public static string SerializeToJson(LevelData data)
        {
            if (data == null)
                return "{}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            bool hasPrevious = false;

            // 1. startPos
            if (data.startPos != null)
            {
                string startJson = ToJsonSafe(data.startPos);
                sb.Append("  \"startPos\": ").Append(IndentJson(startJson, 2));
                hasPrevious = true;
            }

            // 2. checkPoint (always serialized as array)
            if (data.checkPoint != null && data.checkPoint.Length > 0)
            {
                if (hasPrevious) sb.AppendLine(",");
                sb.AppendLine("  \"checkPoint\": [");
                for (int i = 0; i < data.checkPoint.Length; i++)
                {
                    string cpJson = ToJsonSafe(data.checkPoint[i]);
                    sb.Append(IndentJson(cpJson, 4));
                    if (i < data.checkPoint.Length - 1)
                        sb.AppendLine(",");
                    else
                        sb.AppendLine();
                }
                sb.Append("  ]");
                hasPrevious = true;
            }

            // 3. end - ONLY serialized when an End marker actually exists
            if (data.end != null)
            {
                if (hasPrevious) sb.AppendLine(",");
                string endJson = ToJsonSafe(data.end);
                sb.Append("  \"end\": ").Append(IndentJson(endJson, 2));
                hasPrevious = true;
            }

            // 4. cameraBoundary - if present
            if (data.cameraBoundary != null)
            {
                if (hasPrevious) sb.AppendLine(",");
                string camJson = ToJsonSafe(data.cameraBoundary);
                sb.Append("  \"cameraBoundary\": ").Append(IndentJson(camJson, 2));
                hasPrevious = true;
            }

            // 5. platforms
            if (data.platforms != null && data.platforms.Length > 0)
            {
                if (hasPrevious) sb.AppendLine(",");
                sb.AppendLine("  \"platforms\": [");
                for (int i = 0; i < data.platforms.Length; i++)
                {
                    ColorPlatformData p = data.platforms[i];
                    if (p == null)
                        continue;

                    string pJson;
                    if (p.trajectory != null && p.trajectory.enabled)
                    {
                        pJson = ToJsonSafe(p);
                    }
                    else
                    {
                        ColorPlatformData staticP = new ColorPlatformData
                        {
                            position = p.position,
                            size = p.size,
                            rawGrayscaleColor = p.rawGrayscaleColor,
                            trajectory = null
                        };
                        pJson = ToJsonSafe(staticP);
                    }

                    sb.Append(IndentJson(pJson, 4));
                    if (i < data.platforms.Length - 1)
                        sb.AppendLine(",");
                    else
                        sb.AppendLine();
                }
                sb.Append("  ]");
                hasPrevious = true;
            }
            else
            {
                if (hasPrevious) sb.AppendLine(",");
                sb.Append("  \"platforms\": []");
                hasPrevious = true;
            }

            sb.AppendLine();
            sb.Append("}");

            string result = sb.ToString();

            // Verify no NaN or Infinity tokens in the generated JSON string
            if (result.Contains("NaN") || result.Contains("Infinity") || result.Contains("-Infinity"))
            {
                SafeLogError($"{LogPrefix} Generated JSON contains NaN or Infinity numeric tokens! Sanitizing numeric tokens.");
                result = result.Replace("NaN", "0.0")
                               .Replace("-Infinity", "-1000.0")
                               .Replace("Infinity", "1000.0");
            }

            return result;
        }

        private static void SafeLogError(string message)
        {
            try
            {
                Debug.LogError(message);
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        private static string ToJsonSafe(object obj)
        {
            try
            {
                return JsonUtility.ToJson(obj, true);
            }
            catch
            {
                return FallbackObjectToJson(obj);
            }
        }

        private static string FallbackObjectToJson(object obj)
        {
            if (obj == null) return "null";
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            if (obj is StartPosData sp)
            {
                return $"{{\n  \"position\": {{\n    \"x\": {sp.position.x.ToString("0.0###", inv)},\n    \"y\": {sp.position.y.ToString("0.0###", inv)}\n  }},\n  \"size\": {{\n    \"x\": {sp.size.x.ToString("0.0###", inv)},\n    \"y\": {sp.size.y.ToString("0.0###", inv)}\n  }},\n  \"color\": {sp.color.ToString("0.0###", inv)}\n}}";
            }
            if (obj is CheckPointData cp)
            {
                return $"{{\n  \"position\": {{\n    \"x\": {cp.position.x.ToString("0.0###", inv)},\n    \"y\": {cp.position.y.ToString("0.0###", inv)}\n  }},\n  \"size\": {{\n    \"x\": {cp.size.x.ToString("0.0###", inv)},\n    \"y\": {cp.size.y.ToString("0.0###", inv)}\n  }},\n  \"color\": {cp.color.ToString("0.0###", inv)}\n}}";
            }
            if (obj is EndData ed)
            {
                return $"{{\n  \"position\": {{\n    \"x\": {ed.position.x.ToString("0.0###", inv)},\n    \"y\": {ed.position.y.ToString("0.0###", inv)}\n  }},\n  \"size\": {{\n    \"x\": {ed.size.x.ToString("0.0###", inv)},\n    \"y\": {ed.size.y.ToString("0.0###", inv)}\n  }}\n}}";
            }
            if (obj is CameraBoundaryData cb)
            {
                return $"{{\n  \"position\": {{\n    \"x\": {cb.position.x.ToString("0.0###", inv)},\n    \"y\": {cb.position.y.ToString("0.0###", inv)}\n  }},\n  \"size\": {{\n    \"x\": {cb.size.x.ToString("0.0###", inv)},\n    \"y\": {cb.size.y.ToString("0.0###", inv)}\n  }}\n}}";
            }
            if (obj is ColorPlatformData p)
            {
                StringBuilder psb = new StringBuilder();
                psb.AppendLine("{");
                psb.AppendLine($"  \"position\": {{\n    \"x\": {p.position.x.ToString("0.0###", inv)},\n    \"y\": {p.position.y.ToString("0.0###", inv)}\n  }},");
                psb.AppendLine($"  \"size\": {{\n    \"x\": {p.size.x.ToString("0.0###", inv)},\n    \"y\": {p.size.y.ToString("0.0###", inv)}\n  }},");
                psb.Append($"  \"rawGrayscaleColor\": {p.rawGrayscaleColor.ToString("0.0###", inv)}");
                if (p.trajectory != null && p.trajectory.enabled)
                {
                    psb.AppendLine(",");
                    psb.AppendLine("  \"trajectory\": {");
                    psb.AppendLine("    \"enabled\": true,");
                    psb.AppendLine($"    \"curveType\": {(int)p.trajectory.curveType},");
                    psb.AppendLine($"    \"domainMode\": {(int)p.trajectory.domainMode},");
                    psb.AppendLine($"    \"playbackMode\": {(int)p.trajectory.playbackMode},");
                    psb.AppendLine($"    \"originX\": {p.trajectory.originX.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"originY\": {p.trajectory.originY.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"rotation\": {p.trajectory.rotation.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"scaleX\": {p.trajectory.scaleX.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"scaleY\": {p.trajectory.scaleY.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"paramMin\": {p.trajectory.paramMin.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"paramMax\": {p.trajectory.paramMax.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"boundsXMin\": {p.trajectory.boundsXMin.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"boundsXMax\": {p.trajectory.boundsXMax.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"boundsYMin\": {p.trajectory.boundsYMin.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"boundsYMax\": {p.trajectory.boundsYMax.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"radius\": {p.trajectory.radius.ToString("0.0###", inv)},");
                    psb.AppendLine($"    \"duration\": {p.trajectory.duration.ToString("0.0###", inv)}");
                    psb.Append("  }");
                }
                psb.AppendLine();
                psb.Append("}");
                return psb.ToString();
            }

            return "{}";
        }

        private static string IndentJson(string json, int spaces)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            string pad = new string(' ', spaces);
            string[] lines = json.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0 && !string.IsNullOrEmpty(lines[i]))
                    lines[i] = pad + lines[i];
            }
            return string.Join("\n", lines);
        }
    }
}
