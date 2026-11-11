using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Contrast.Level;
using UnityEngine;

namespace Contrast.Camera
{
    public enum CameraMode
    {
        NormalPlatformingFollow,
        NewtonFixed,
        NewtonPlayerFollow,
        NewtonResultReveal,
        TestModeFreeMove
    }

    public struct CameraTransitionTraceEntry
    {
        public int Frame;
        public float DeltaTime;
        public float PlayerWorldX;
        public float PlatformLeftBoundaryX;
        public string TargetCameraMode;
        public Vector3 CurrentCameraPosition;
        public Vector3 DesiredCameraPosition;
        public float CurrentOrthographicSize;
        public float DesiredOrthographicSize;
        public float MinScale;
        public float MaxScale;
        public Vector3 PositionVelocity;
        public float ScaleVelocity;
        public bool ReversedThisFrame;
    }

    /// <summary>
    /// Single authoritative camera controller in the frame pipeline.
    /// Bounded by the level boundary rectangle and supports:
    /// - Coordinate-driven bidirectional reversible Newton camera transitions
    ///   controlled by playerWorldX relative to the left boundary (platformBounds.min.x)
    ///   of the platform immediately before Newton Ring.
    /// - Explicit minScale and maxScale orthographic size bounds with outward velocity clamping.
    /// - Simultaneous position + scale SmoothDamp interpolation that reverses continuously
    ///   from the actual current camera state without restarting from endpoints.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool hasBoundary = false;
        [SerializeField] private Vector2 boundaryCenter = Vector2.zero;
        [SerializeField] private Vector2 boundarySize = new Vector2(40f, 20f);

        private UnityEngine.Camera cam;

        [Header("Camera State Management")]
        [SerializeField] private CameraMode currentMode = CameraMode.NormalPlatformingFollow;

        [Header("Scale Bounds (Orthographic Size)")]
        [SerializeField] private float minScale = 5.0f;
        [SerializeField] private float maxScale = 8.6f;

        [Header("Reversible Transition Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.25f;
        [SerializeField] private float scaleSmoothTime = 0.25f;

        private float normalOrthoSize = 5.0f;
        private float targetOrthoSize = 5.0f;
        private float zoomLerpSpeed = 2.5f;

        // Fixed Newton marking camera parameters (HTML newtonViewLayout parity)
        private Vector3 newtonFixedPosition;
        private float newtonFixedOrthoSize = 8.6f;
        private float newtonFixedLerpSpeed = 3.2f;

        // Coordinate-driven bidirectional Newton transition state
        private bool hasNewtonThreshold = false;
        private ColorPlatform preNewtonPlatform;
        private float fallbackPlatformLeftBoundaryX = float.PositiveInfinity;
        private CameraMode targetCameraMode = CameraMode.NormalPlatformingFollow;
        private Vector3 desiredCameraPosition;
        private float desiredOrthographicSize = 5.0f;
        private Vector3 positionVelocity = Vector3.zero;
        private float scaleVelocity = 0f;
        private bool isNewtonTransitionActive = false;
        private bool explicitEndpointLock = false;
        private Vector3 lastPlayerFollowTarget;
        private bool hasLastPlayerFollowSample = false;

        // Reveal parameters (for NewtonResultReveal)
        private Vector3 revealPosition;
        private float revealOrthoSize = 9.6f;
        private float revealLerpSpeed = 3.2f;

        // Bounded frame-by-frame transition trace (Task G)
        public const int MaxTraceCapacity = 2048;
        private readonly List<CameraTransitionTraceEntry> transitionTrace = new List<CameraTransitionTraceEntry>(512);
        private int traceFrameCounter = 0;
        public bool RecordTransitionTrace { get; set; } = true;
        public IReadOnlyList<CameraTransitionTraceEntry> TransitionTrace => transitionTrace;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public bool HasBoundary => hasBoundary;
        public Vector2 BoundaryCenter => boundaryCenter;
        public Vector2 BoundarySize => boundarySize;

        public CameraMode CurrentMode => currentMode;
        public CameraMode TargetCameraMode => targetCameraMode;
        public bool IsOverridden => currentMode == CameraMode.NewtonResultReveal || currentMode == CameraMode.NewtonFixed;
        public Vector3 NewtonFixedPosition => newtonFixedPosition;
        public float NewtonFixedOrthoSize => newtonFixedOrthoSize;
        public float TargetOrthoSize => targetOrthoSize;

        public float MinScale
        {
            get => minScale;
            set
            {
                minScale = Mathf.Max(0.1f, value);
                if (maxScale < minScale) maxScale = minScale;
            }
        }

        public float MaxScale
        {
            get => maxScale;
            set
            {
                maxScale = Mathf.Max(minScale, value);
            }
        }

        public float PositionSmoothTime
        {
            get => positionSmoothTime;
            set => positionSmoothTime = Mathf.Max(0.01f, value);
        }

        public float ScaleSmoothTime
        {
            get => scaleSmoothTime;
            set => scaleSmoothTime = Mathf.Max(0.01f, value);
        }

        public ColorPlatform PreNewtonPlatform => preNewtonPlatform;
        public bool HasNewtonThreshold => hasNewtonThreshold;

        public float PlatformLeftBoundaryX
        {
            get
            {
                if (preNewtonPlatform != null)
                {
                    return preNewtonPlatform.GetAabb().xMin;
                }
                return fallbackPlatformLeftBoundaryX;
            }
        }

        public Vector3 CurrentCameraPosition => transform.position;
        public Vector3 DesiredCameraPosition => desiredCameraPosition;
        public float CurrentOrthographicSize => (cam != null && cam.orthographic) ? cam.orthographicSize : desiredOrthographicSize;
        public float DesiredOrthographicSize => desiredOrthographicSize;
        public Vector3 PositionVelocity => positionVelocity;
        public float ScaleVelocity => scaleVelocity;
        public bool IsNewtonTransitionActive => isNewtonTransitionActive;

        public UnityEngine.Camera UnityCamera
        {
            get
            {
                if (cam == null) cam = GetComponent<UnityEngine.Camera>();
                return cam;
            }
        }

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
        }

        public void SetBoundary(Vector2 center, Vector2 size)
        {
            boundaryCenter = center;
            boundarySize = size;
            hasBoundary = true;
        }

        public void ClearBoundary()
        {
            hasBoundary = false;
        }

        /// <summary>
        /// Configures coordinate-driven bidirectional reversible Newton camera control.
        /// Trigger threshold is the authoritative world-space left boundary (platformBounds.min.x)
        /// of the platform immediately before Newton Ring.
        /// </summary>
        public void ConfigureNewtonTransition(
            ColorPlatform platformBeforeNewton,
            float fallbackLeftBoundaryX,
            Vector3 newtonWideWorldCenter,
            float normalFollowOrthoSize,
            float newtonWideOrthoSize)
        {
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();

            preNewtonPlatform = platformBeforeNewton;
            fallbackPlatformLeftBoundaryX = fallbackLeftBoundaryX;

            float safeNormal = Mathf.Max(0.5f, normalFollowOrthoSize);
            float safeNewton = Mathf.Max(0.5f, newtonWideOrthoSize);

            minScale = Mathf.Min(safeNormal, safeNewton);
            maxScale = Mathf.Max(safeNormal, safeNewton);
            normalOrthoSize = minScale;
            newtonFixedOrthoSize = maxScale;
            newtonFixedPosition = new Vector3(newtonWideWorldCenter.x, newtonWideWorldCenter.y, transform.position.z);

            hasNewtonThreshold = true;
            explicitEndpointLock = false;

            float thresholdX = PlatformLeftBoundaryX;
            float playerX = target != null ? target.position.x : float.NegativeInfinity;

            if (playerX > thresholdX)
            {
                targetCameraMode = CameraMode.NewtonFixed;
                currentMode = CameraMode.NewtonFixed;
                desiredOrthographicSize = maxScale;
                desiredCameraPosition = newtonFixedPosition;
                isNewtonTransitionActive = true;
            }
            else
            {
                targetCameraMode = CameraMode.NormalPlatformingFollow;
                currentMode = CameraMode.NormalPlatformingFollow;
                desiredOrthographicSize = minScale;
                desiredCameraPosition = CalculateCurrentPlayerFollowTarget();
                float currentScale = (cam != null && cam.orthographic) ? cam.orthographicSize : minScale;
                isNewtonTransitionActive = Mathf.Abs(currentScale - minScale) > 0.001f;
            }
        }

        public void ClearNewtonTransitionConfig()
        {
            hasNewtonThreshold = false;
            preNewtonPlatform = null;
            fallbackPlatformLeftBoundaryX = float.PositiveInfinity;
            isNewtonTransitionActive = false;
            explicitEndpointLock = false;
            positionVelocity = Vector3.zero;
            scaleVelocity = 0f;
            hasLastPlayerFollowSample = false;
        }

        /// <summary>
        /// Called on Player respawn to determine the appropriate target camera mode from
        /// the restored Player position without resetting the camera to an arbitrary endpoint.
        /// </summary>
        public void OnPlayerRespawned()
        {
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();
            explicitEndpointLock = false;
            hasLastPlayerFollowSample = false;

            if (!hasNewtonThreshold || target == null)
            {
                currentMode = CameraMode.NormalPlatformingFollow;
                targetCameraMode = CameraMode.NormalPlatformingFollow;
                desiredOrthographicSize = minScale;
                desiredCameraPosition = CalculateCurrentPlayerFollowTarget();
                transform.position = desiredCameraPosition;
                if (cam != null && cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Clamp(minScale, minScale, maxScale);
                }
                positionVelocity = Vector3.zero;
                scaleVelocity = 0f;
                return;
            }

            float thresholdX = PlatformLeftBoundaryX;
            float playerX = target.position.x;

            if (playerX > thresholdX)
            {
                targetCameraMode = CameraMode.NewtonFixed;
                currentMode = CameraMode.NewtonFixed;
                desiredOrthographicSize = maxScale;
                desiredCameraPosition = new Vector3(newtonFixedPosition.x, newtonFixedPosition.y, transform.position.z);
                isNewtonTransitionActive = true;
            }
            else if (playerX < thresholdX)
            {
                targetCameraMode = CameraMode.NormalPlatformingFollow;
                currentMode = CameraMode.NormalPlatformingFollow;
                desiredOrthographicSize = minScale;
                desiredCameraPosition = CalculateCurrentPlayerFollowTarget();

                float currentScale = (cam != null && cam.orthographic) ? cam.orthographicSize : minScale;
                if (Mathf.Abs(currentScale - minScale) > 0.001f)
                {
                    isNewtonTransitionActive = true;
                }
                else
                {
                    isNewtonTransitionActive = false;
                    transform.position = desiredCameraPosition;
                    positionVelocity = Vector3.zero;
                    scaleVelocity = 0f;
                }
            }
        }

        /// <summary>
        /// Locks the camera to an HTML-derived fixed world-space Newton framing.
        /// Preserved for direct endpoint setup and backward compatibility.
        /// </summary>
        public void SetNewtonFixed(Vector3 fixedWorldPosition, float orthoSize = 9.4f, float lerpSpeed = 3.2f)
        {
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();
            currentMode = CameraMode.NewtonFixed;
            targetCameraMode = CameraMode.NewtonFixed;
            newtonFixedPosition = new Vector3(fixedWorldPosition.x, fixedWorldPosition.y, transform.position.z);
            newtonFixedOrthoSize = orthoSize;
            maxScale = Mathf.Max(minScale, orthoSize);
            targetOrthoSize = maxScale;
            desiredOrthographicSize = maxScale;
            desiredCameraPosition = newtonFixedPosition;
            newtonFixedLerpSpeed = lerpSpeed;
            positionVelocity = Vector3.zero;
            scaleVelocity = 0f;
            explicitEndpointLock = true;
            transform.position = newtonFixedPosition;
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(orthoSize, minScale, maxScale);
            }
            Debug.Log($"[CAMERA] Mode=NewtonFixed pos=({newtonFixedPosition.x:F3},{newtonFixedPosition.y:F3}) orthoSize={orthoSize:F3}");
        }

        public void SetNewtonPlayerFollow(float newtonOrthoSize = 6.5f, float lerpSpeed = 2.5f)
        {
            currentMode = CameraMode.NewtonPlayerFollow;
            targetOrthoSize = newtonOrthoSize;
            zoomLerpSpeed = lerpSpeed;
        }

        public void SetNewtonReveal(Vector3 centerPosition, float targetSize = 9.6f, float lerpSpeed = 3.2f)
        {
            currentMode = CameraMode.NewtonResultReveal;
            targetCameraMode = CameraMode.NewtonResultReveal;
            revealPosition = centerPosition;
            revealOrthoSize = targetSize;
            targetOrthoSize = targetSize;
            revealLerpSpeed = lerpSpeed;
            explicitEndpointLock = false;
        }

        /// <summary>
        /// Restores normal platforming follow mode with standard orthographic zoom.
        /// </summary>
        public void ResetToNormal(float defaultOrthoSize = 5f, float lerpSpeed = 3f)
        {
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();
            currentMode = CameraMode.NormalPlatformingFollow;
            targetCameraMode = CameraMode.NormalPlatformingFollow;
            normalOrthoSize = defaultOrthoSize;
            minScale = Mathf.Max(0.1f, Mathf.Min(defaultOrthoSize, maxScale));
            targetOrthoSize = minScale;
            desiredOrthographicSize = minScale;
            zoomLerpSpeed = lerpSpeed;
            positionVelocity = Vector3.zero;
            scaleVelocity = 0f;
            isNewtonTransitionActive = false;
            explicitEndpointLock = false;
            hasLastPlayerFollowSample = false;

            if (target != null)
            {
                desiredCameraPosition = CalculateCurrentPlayerFollowTarget();
                transform.position = desiredCameraPosition;
            }

            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = minScale;
            }
        }

        public void SetOverride(Vector3 position, float orthoSize, float lerpSpeed = 3f)
        {
            SetNewtonReveal(position, orthoSize, lerpSpeed);
        }

        private CameraMode previousMode = CameraMode.NormalPlatformingFollow;

        public void SetTestModeFreeMove(bool enabled)
        {
            if (enabled)
            {
                if (currentMode != CameraMode.TestModeFreeMove)
                    previousMode = currentMode;
                currentMode = CameraMode.TestModeFreeMove;
            }
            else
            {
                if (currentMode == CameraMode.TestModeFreeMove)
                    currentMode = previousMode;
            }
        }

        public void ClearOverride()
        {
            ResetToNormal();
            if (cam != null && cam.orthographic)
                cam.orthographicSize = normalOrthoSize;
        }

        /// <summary>
        /// Calculates the live Player-follow camera target position using the Player's
        /// current authoritative world coordinates and the level camera boundary.
        /// </summary>
        public Vector3 CalculateCurrentPlayerFollowTarget()
        {
            if (target == null)
                return transform.position;

            Vector3 targetPos = target.position;
            float desiredX = targetPos.x;
            float desiredY = targetPos.y;

            if (hasBoundary && cam != null)
            {
                float vertExtent = cam.orthographic
                    ? minScale
                    : Mathf.Abs(transform.position.z - targetPos.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float aspect = cam.aspect > 0.01f ? cam.aspect : (16f / 9f);
                float horzExtent = vertExtent * aspect;

                float minX = boundaryCenter.x - boundarySize.x * 0.5f;
                float maxX = boundaryCenter.x + boundarySize.x * 0.5f;
                float minY = boundaryCenter.y - boundarySize.y * 0.5f;
                float maxY = boundaryCenter.y + boundarySize.y * 0.5f;

                desiredX = boundarySize.x >= horzExtent * 2f
                    ? Mathf.Clamp(desiredX, minX + horzExtent, maxX - horzExtent)
                    : boundaryCenter.x;

                desiredY = boundarySize.y >= vertExtent * 2f
                    ? Mathf.Clamp(desiredY, minY + vertExtent, maxY - vertExtent)
                    : boundaryCenter.y;
            }

            return new Vector3(desiredX, desiredY, transform.position.z);
        }

        public void ProcessCameraUpdate()
        {
            ProcessCameraUpdate(Time.deltaTime);
        }

        public void ProcessCameraUpdate(float deltaTime)
        {
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();

            // Mode: TestModeFreeMove - instant 1:1 player centering with world rolling underneath
            if (currentMode == CameraMode.TestModeFreeMove)
            {
                if (target != null)
                {
                    transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
                    if (cam != null && cam.orthographic)
                    {
                        cam.orthographicSize = minScale;
                    }
                }
                return;
            }

            // Mode: NewtonResultReveal - cinematic framing of center O to display measurements & formulas
            if (currentMode == CameraMode.NewtonResultReveal)
            {
                float dt = deltaTime > 0f ? deltaTime : Time.deltaTime;
                float smoothT = 1f - Mathf.Exp(-revealLerpSpeed * Mathf.Max(0f, dt));
                transform.position = Vector3.Lerp(
                    transform.position,
                    new Vector3(revealPosition.x, revealPosition.y, transform.position.z),
                    smoothT);

                if (cam != null && cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, revealOrthoSize, smoothT);
                }
                return;
            }

            // Direct explicit lock fallback (when SetNewtonFixed was called without threshold crossing in legacy tests)
            if (explicitEndpointLock)
            {
                if (hasNewtonThreshold && target != null)
                {
                    float px = target.position.x;
                    float thresh = PlatformLeftBoundaryX;
                    if (px > thresh)
                    {
                        // Player is inside Newton zone; release explicit override into normal threshold logic
                        explicitEndpointLock = false;
                        targetCameraMode = CameraMode.NewtonFixed;
                        isNewtonTransitionActive = true;
                    }
                    else
                    {
                        transform.position = new Vector3(newtonFixedPosition.x, newtonFixedPosition.y, transform.position.z);
                        if (cam != null && cam.orthographic)
                        {
                            cam.orthographicSize = Mathf.Clamp(newtonFixedOrthoSize, minScale, maxScale);
                        }
                        return;
                    }
                }
                else
                {
                    transform.position = new Vector3(newtonFixedPosition.x, newtonFixedPosition.y, transform.position.z);
                    if (cam != null && cam.orthographic)
                    {
                        cam.orthographicSize = Mathf.Clamp(newtonFixedOrthoSize, minScale, maxScale);
                    }
                    return;
                }
            }

            if (target == null) return;

            // Coordinate-driven bidirectional reversible Newton camera control (Task E)
            if (hasNewtonThreshold)
            {
                UpdateBidirectionalNewtonCamera(deltaTime);
                return;
            }

            // Standard NormalPlatformingFollow when no Newton chamber exists
            if (cam != null && cam.orthographic)
            {
                if (Mathf.Abs(cam.orthographicSize - targetOrthoSize) > 0.001f)
                {
                    float dt = deltaTime > 0f ? deltaTime : Time.deltaTime;
                    float smoothT = 1f - Mathf.Exp(-zoomLerpSpeed * Mathf.Max(0f, dt));
                    cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, smoothT);
                }
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minScale, maxScale);
            }

            desiredCameraPosition = CalculateCurrentPlayerFollowTarget();
            desiredOrthographicSize = minScale;
            transform.position = desiredCameraPosition;
        }

        private void UpdateBidirectionalNewtonCamera(float deltaTime)
        {
            float dt = deltaTime > 0f ? deltaTime : (Time.deltaTime > 0f ? Time.deltaTime : (1f / 60f));
            float playerWorldX = target.position.x;
            float thresholdX = PlatformLeftBoundaryX;

            CameraMode prevTargetMode = targetCameraMode;

            // E2: Compare playerWorldX against thresholdX = platformBounds.min.x
            // Exact equality (playerWorldX == thresholdX) preserves previous desired mode.
            if (playerWorldX > thresholdX)
            {
                targetCameraMode = CameraMode.NewtonFixed;
                isNewtonTransitionActive = true;
            }
            else if (playerWorldX < thresholdX)
            {
                targetCameraMode = CameraMode.NormalPlatformingFollow;
            }

            bool reversedThisFrame = (targetCameraMode != prevTargetMode);
            currentMode = targetCameraMode;

            Vector3 livePlayerFollowTarget = CalculateCurrentPlayerFollowTarget();
            Vector3 playerFollowDelta = hasLastPlayerFollowSample
                ? (livePlayerFollowTarget - lastPlayerFollowTarget)
                : Vector3.zero;
            lastPlayerFollowTarget = livePlayerFollowTarget;
            hasLastPlayerFollowSample = true;

            // E3 & E4: Compute desired target position and clamped target scale
            desiredOrthographicSize = targetCameraMode == CameraMode.NewtonFixed
                ? maxScale
                : minScale;
            desiredOrthographicSize = Mathf.Clamp(desiredOrthographicSize, minScale, maxScale);

            desiredCameraPosition = targetCameraMode == CameraMode.NewtonFixed
                ? new Vector3(newtonFixedPosition.x, newtonFixedPosition.y, transform.position.z)
                : livePlayerFollowTarget;

            Vector3 currentPos = transform.position;
            float currentScale = (cam != null && cam.orthographic) ? cam.orthographicSize : minScale;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);

            Vector3 newPos;
            float newScale;

            if (!isNewtonTransitionActive && targetCameraMode == CameraMode.NormalPlatformingFollow)
            {
                // Normal 1:1 follow when completely outside Newton transition
                newPos = desiredCameraPosition;
                newScale = minScale;
                positionVelocity = Vector3.zero;
                scaleVelocity = 0f;
            }
            else
            {
                // When returning toward FollowPlayer, progressively couple camera to live Player motion
                // as zoom approaches minScale so a moving Player is tracked with zero steady-state lag
                // and zero final-frame snap, while preserving 100% continuity on reversal frames.
                if (targetCameraMode == CameraMode.NormalPlatformingFollow && !reversedThisFrame)
                {
                    float zoomSpan = Mathf.Max(0.0001f, maxScale - minScale);
                    float zoomOutRatio = Mathf.Clamp01((currentScale - minScale) / zoomSpan);
                    float followCoupling = 1f - zoomOutRatio;
                    currentPos += new Vector3(
                        playerFollowDelta.x * followCoupling,
                        playerFollowDelta.y * followCoupling,
                        0f);
                }

                newPos = Vector3.SmoothDamp(
                    currentPos,
                    desiredCameraPosition,
                    ref positionVelocity,
                    positionSmoothTime,
                    Mathf.Infinity,
                    dt);
                newPos.z = transform.position.z;

                newScale = Mathf.SmoothDamp(
                    currentScale,
                    desiredOrthographicSize,
                    ref scaleVelocity,
                    scaleSmoothTime,
                    Mathf.Infinity,
                    dt);

                // E3: Strict bounds enforcement and outward velocity correction at limits
                if (newScale <= minScale)
                {
                    newScale = minScale;
                    if (scaleVelocity < 0f)
                        scaleVelocity = 0f;
                }
                else if (newScale >= maxScale)
                {
                    newScale = maxScale;
                    if (scaleVelocity > 0f)
                        scaleVelocity = 0f;
                }

                // Check smooth endpoint convergence without snapping
                float posDist = Vector2.Distance(newPos, desiredCameraPosition);
                float scaleDiff = Mathf.Abs(newScale - desiredOrthographicSize);

                if (targetCameraMode == CameraMode.NewtonFixed)
                {
                    if (posDist <= 0.0005f && scaleDiff <= 0.0005f)
                    {
                        newPos = desiredCameraPosition;
                        newScale = maxScale;
                        positionVelocity = Vector3.zero;
                        scaleVelocity = 0f;
                    }
                }
                else if (targetCameraMode == CameraMode.NormalPlatformingFollow)
                {
                    if (posDist <= 0.0005f && scaleDiff <= 0.0005f)
                    {
                        newPos = desiredCameraPosition;
                        newScale = minScale;
                        positionVelocity = Vector3.zero;
                        scaleVelocity = 0f;
                        isNewtonTransitionActive = false;
                    }
                }
            }

            transform.position = newPos;
            if (cam != null && cam.orthographic)
            {
                cam.orthographicSize = Mathf.Clamp(newScale, minScale, maxScale);
            }

            if (RecordTransitionTrace)
            {
                traceFrameCounter++;
                if (transitionTrace.Count >= MaxTraceCapacity)
                {
                    transitionTrace.RemoveAt(0);
                }

                transitionTrace.Add(new CameraTransitionTraceEntry
                {
                    Frame = traceFrameCounter,
                    DeltaTime = dt,
                    PlayerWorldX = playerWorldX,
                    PlatformLeftBoundaryX = thresholdX,
                    TargetCameraMode = targetCameraMode == CameraMode.NewtonFixed ? "NewtonWide" : "FollowPlayer",
                    CurrentCameraPosition = transform.position,
                    DesiredCameraPosition = desiredCameraPosition,
                    CurrentOrthographicSize = (cam != null && cam.orthographic) ? cam.orthographicSize : newScale,
                    DesiredOrthographicSize = desiredOrthographicSize,
                    MinScale = minScale,
                    MaxScale = maxScale,
                    PositionVelocity = positionVelocity,
                    ScaleVelocity = scaleVelocity,
                    ReversedThisFrame = reversedThisFrame
                });
            }
        }

        public void ClearTransitionTrace()
        {
            transitionTrace.Clear();
            traceFrameCounter = 0;
        }

        public void ExportTransitionTraceCsv(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder(transitionTrace.Count * 140 + 256);
            sb.AppendLine("Frame,DeltaTime,PlayerWorldX,PlatformLeftBoundaryX,TargetCameraMode,CurrentCameraPosX,CurrentCameraPosY,DesiredCameraPosX,DesiredCameraPosY,CurrentOrthographicSize,DesiredOrthographicSize,MinScale,MaxScale,PositionVelocityX,PositionVelocityY,ScaleVelocity,ReversedThisFrame");

            CultureInfo inv = CultureInfo.InvariantCulture;
            for (int i = 0; i < transitionTrace.Count; i++)
            {
                CameraTransitionTraceEntry e = transitionTrace[i];
                sb.Append(e.Frame.ToString(inv)).Append(',')
                  .Append(e.DeltaTime.ToString("F5", inv)).Append(',')
                  .Append(e.PlayerWorldX.ToString("F5", inv)).Append(',')
                  .Append(e.PlatformLeftBoundaryX.ToString("F5", inv)).Append(',')
                  .Append(e.TargetCameraMode).Append(',')
                  .Append(e.CurrentCameraPosition.x.ToString("F5", inv)).Append(',')
                  .Append(e.CurrentCameraPosition.y.ToString("F5", inv)).Append(',')
                  .Append(e.DesiredCameraPosition.x.ToString("F5", inv)).Append(',')
                  .Append(e.DesiredCameraPosition.y.ToString("F5", inv)).Append(',')
                  .Append(e.CurrentOrthographicSize.ToString("F5", inv)).Append(',')
                  .Append(e.DesiredOrthographicSize.ToString("F5", inv)).Append(',')
                  .Append(e.MinScale.ToString("F5", inv)).Append(',')
                  .Append(e.MaxScale.ToString("F5", inv)).Append(',')
                  .Append(e.PositionVelocity.x.ToString("F5", inv)).Append(',')
                  .Append(e.PositionVelocity.y.ToString("F5", inv)).Append(',')
                  .Append(e.ScaleVelocity.ToString("F5", inv)).Append(',')
                  .Append(e.ReversedThisFrame ? "True" : "False")
                  .AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public Rect GetVisibleWorldRect()
        {
            if (cam == null) cam = GetComponent<UnityEngine.Camera>();
            float ortho = (cam != null && cam.orthographic && cam.orthographicSize > 0.01f)
                ? cam.orthographicSize
                : targetOrthoSize;
            float aspect = (cam != null && cam.aspect > 0.01f)
                ? cam.aspect
                : (16f / 9f);
            float halfH = ortho;
            float halfW = ortho * aspect;
            return new Rect(
                transform.position.x - halfW,
                transform.position.y - halfH,
                halfW * 2f,
                halfH * 2f);
        }

        public bool IsWorldPointInView(Vector2 worldPoint, float margin = 0f)
        {
            Rect r = GetVisibleWorldRect();
            return worldPoint.x >= r.xMin + margin &&
                   worldPoint.x <= r.xMax - margin &&
                   worldPoint.y >= r.yMin + margin &&
                   worldPoint.y <= r.yMax - margin;
        }

        public string GetDiagnosticsLog(string activeRail = "None")
        {
            Vector3 playerWorld = target != null ? target.position : Vector3.zero;
            Vector3 cameraWorld = transform.position;
            Vector3 playerViewport = (cam != null && target != null)
                ? cam.WorldToViewportPoint(playerWorld)
                : Vector3.zero;
            bool playerVisible = (playerViewport.x >= 0f && playerViewport.x <= 1f &&
                                  playerViewport.y >= 0f && playerViewport.y <= 1f &&
                                  playerViewport.z > 0f);
            float ortho = cam != null ? cam.orthographicSize : 0f;

            return $"[CAMERA] State={currentMode}\n" +
                   $"[CAMERA] Target={(target != null ? target.name : "None")}\n" +
                   $"[CAMERA] PlayerWorld=({playerWorld.x:F2}, {playerWorld.y:F2}, {playerWorld.z:F2})\n" +
                   $"[CAMERA] CameraWorld=({cameraWorld.x:F2}, {cameraWorld.y:F2}, {cameraWorld.z:F2})\n" +
                   $"[CAMERA] PlayerScreen=({playerViewport.x:F2}, {playerViewport.y:F2})\n" +
                   $"[CAMERA] OrthographicSize={ortho:F2}\n" +
                   $"[CAMERA] ActiveRail={activeRail}\n" +
                   $"[CAMERA] PlayerVisible={playerVisible}";
        }

        private void OnDrawGizmosSelected()
        {
            if (!hasBoundary) return;
            Gizmos.color = UnityEngine.Color.cyan;
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
        }
    }
}
