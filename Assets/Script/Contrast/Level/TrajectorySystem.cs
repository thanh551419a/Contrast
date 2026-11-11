using System.Collections.Generic;
using Contrast.Core;
using UnityEngine;

namespace Contrast.Level
{
    /// <summary>
    /// Central coordinator for all active trajectories.
    /// Called once from <see cref="GameManager.Update"/> while Playing,
    /// before player movement, so that platform AABBs are current when
    /// the collision pipeline runs.
    ///
    /// Also provides rider-carry: if a player is grounded on a moving
    /// platform, the platform's frame displacement is applied to the player
    /// through the standard collision resolver pipeline.
    /// </summary>
    public static class TrajectorySystem
    {
        /// <summary>
        /// Advance all trajectories on currently registered platforms.
        /// Must be called exactly once per gameplay frame.
        /// </summary>
        public static void UpdateAll(float deltaTime)
        {
            List<ColorPlatform> platforms = ColorPlatform.All;
            for (int i = 0; i < platforms.Count; i++)
            {
                ColorPlatform platform = platforms[i];
                if (platform == null)
                    continue;

                Trajectory traj = platform.GetTrajectory();
                if (traj != null && traj.IsEnabled)
                    traj.ProcessUpdate(deltaTime);
            }
        }

        /// <summary>
        /// Reset all trajectory phases. Called on level build/restart/retry.
        /// </summary>
        public static void ResetAll()
        {
            List<ColorPlatform> platforms = ColorPlatform.All;
            for (int i = 0; i < platforms.Count; i++)
            {
                ColorPlatform platform = platforms[i];
                if (platform == null)
                    continue;

                Trajectory traj = platform.GetTrajectory();
                if (traj != null)
                    traj.ResetPhase();
            }
        }

        /// <summary>
        /// Find which moving platform (if any) the player was standing on
        /// and return its frame displacement for rider-carry.
        /// Evaluates against platform geometry prior to this frame's displacement.
        /// </summary>
        public static Vector2 GetRiderDisplacement(
            Vector2 playerCenter,
            Vector2 playerSize,
            Contrast.Color.LogicalColor playerColor)
        {
            return GetRiderDisplacement(playerCenter, playerSize, playerColor, null, false);
        }

        /// <summary>
        /// Find which moving platform (if any) the player was standing on
        /// and return its frame displacement for rider-carry, prioritizing the
        /// authoritative grounded platform from the previous frame and detaching
        /// immediately when the player is jumping.
        /// </summary>
        public static Vector2 GetRiderDisplacement(
            Vector2 playerCenter,
            Vector2 playerSize,
            Contrast.Color.LogicalColor playerColor,
            ColorPlatform groundedPlatform,
            bool isJumping)
        {
            if (isJumping)
                return Vector2.zero;

            const float groundEpsilon = 0.05f;

            float playerBottom = playerCenter.y - playerSize.y * 0.5f;
            float playerLeft   = playerCenter.x - playerSize.x * 0.5f;
            float playerRight  = playerCenter.x + playerSize.x * 0.5f;

            if (groundedPlatform != null &&
                TryGetPlatformCarryDisplacement(
                    groundedPlatform,
                    playerCenter,
                    playerBottom,
                    playerLeft,
                    playerRight,
                    playerColor,
                    groundEpsilon,
                    out Vector2 groundedDisp,
                    out _))
            {
                return groundedDisp;
            }

            List<ColorPlatform> platforms = ColorPlatform.All;
            float bestGap = float.PositiveInfinity;
            Vector2 bestDisp = Vector2.zero;

            for (int i = 0; i < platforms.Count; i++)
            {
                ColorPlatform platform = platforms[i];
                if (platform == null || platform == groundedPlatform)
                    continue;

                if (TryGetPlatformCarryDisplacement(
                    platform,
                    playerCenter,
                    playerBottom,
                    playerLeft,
                    playerRight,
                    playerColor,
                    groundEpsilon,
                    out Vector2 disp,
                    out float gap))
                {
                    if (gap < bestGap)
                    {
                        bestGap = gap;
                        bestDisp = disp;
                    }
                }
            }

            return bestDisp;
        }

        private static bool TryGetPlatformCarryDisplacement(
            ColorPlatform platform,
            Vector2 playerCenter,
            float playerBottom,
            float playerLeft,
            float playerRight,
            Contrast.Color.LogicalColor playerColor,
            float groundEpsilon,
            out Vector2 disp,
            out float gap)
        {
            disp = Vector2.zero;
            gap = float.PositiveInfinity;

            if (platform == null || !platform.gameObject.activeInHierarchy || !platform.enabled)
                return false;

            if (!platform.IsUniversal && platform.Logical != Contrast.Color.LogicalColor.Universal && platform.RawGrayscaleColor < 254.5f && platform.Logical == playerColor)
                return false;

            Trajectory traj = platform.GetTrajectory();
            if (traj == null || !traj.IsEnabled)
                return false;

            disp = traj.FrameDisplacement;
            if (disp == Vector2.zero)
                return false;

            Rect aabb = platform.GetAabb();

            // Platform geometry before this frame's displacement
            float prevYMax = aabb.yMax - disp.y;
            float prevXMin = aabb.xMin - disp.x;
            float prevXMax = aabb.xMax - disp.x;

            // Check horizontal overlap with prior platform position
            bool hOverlap = playerRight > prevXMin + 0.0001f &&
                            playerLeft  < prevXMax - 0.0001f;
            if (!hOverlap)
                return false;

            // Check if player's feet were resting on top of platform
            gap = Mathf.Abs(playerBottom - prevYMax);
            return gap <= groundEpsilon && playerCenter.y >= prevYMax;
        }
    }
}
