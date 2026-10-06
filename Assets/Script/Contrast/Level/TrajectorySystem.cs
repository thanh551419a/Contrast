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
            const float groundEpsilon = 0.05f;

            float playerBottom = playerCenter.y - playerSize.y * 0.5f;
            float playerLeft   = playerCenter.x - playerSize.x * 0.5f;
            float playerRight  = playerCenter.x + playerSize.x * 0.5f;

            List<ColorPlatform> platforms = ColorPlatform.All;
            for (int i = 0; i < platforms.Count; i++)
            {
                ColorPlatform platform = platforms[i];
                if (platform == null)
                    continue;

                // Only consider blocking platforms
                if (!platform.gameObject.activeInHierarchy || !platform.enabled)
                    continue;
                if (!platform.IsUniversal && platform.Logical == playerColor)
                    continue;

                Trajectory traj = platform.GetTrajectory();
                if (traj == null || !traj.IsEnabled)
                    continue;

                Vector2 disp = traj.FrameDisplacement;
                if (disp == Vector2.zero)
                    continue;

                Rect aabb = platform.GetAabb();

                // Platform geometry before this frame's displacement
                float prevYMax = aabb.yMax - disp.y;
                float prevXMin = aabb.xMin - disp.x;
                float prevXMax = aabb.xMax - disp.x;

                // Check horizontal overlap with prior platform position
                bool hOverlap = playerRight > prevXMin + 0.001f &&
                                playerLeft  < prevXMax - 0.001f;
                if (!hOverlap)
                    continue;

                // Check if player's feet were resting on top of platform
                float gap = Mathf.Abs(playerBottom - prevYMax);
                if (gap <= groundEpsilon && playerCenter.y >= prevYMax)
                    return disp;
            }

            return Vector2.zero;
        }
    }
}
