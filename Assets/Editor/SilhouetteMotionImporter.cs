using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Contrast.Editor
{
    public sealed class SilhouetteMotionImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/SilhouetteMotion/Sheets/"))
                return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 128f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.isReadable = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 4096;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
        }

        [InitializeOnLoadMethod]
        public static void EnsureSilhouetteAnimatorAssets()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    return;
                EnsureAssetsNow();
            };
        }

        public static void EnsureAssetsNow()
        {
            const string rootDir = "Assets/Resources/SilhouetteMotion";
            const string sheetsDir = "Assets/Resources/SilhouetteMotion/Sheets";
            if (!Directory.Exists(sheetsDir))
                return;

            foreach (string mode in new[] { "Idle", "Run", "Jump" })
            {
                string texPath = $"{sheetsDir}/{mode}.png";
                var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
                if (ti != null && (!ti.isReadable || ti.maxTextureSize < 4096 || ti.textureCompression != TextureImporterCompression.Uncompressed))
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.spritePixelsPerUnit = 128f;
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.isReadable = true;
                    ti.npotScale = TextureImporterNPOTScale.None;
                    ti.maxTextureSize = 4096;
                    ti.filterMode = FilterMode.Bilinear;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.SaveAndReimport();
                }
            }

            string controllerPath = $"{rootDir}/SilhouettePlayerAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) != null)
                return;

            AnimationClip idleClip = GetOrCreateClip($"{rootDir}/Idle.anim", 2.40f, 120, true);
            AnimationClip runClip = GetOrCreateClip($"{rootDir}/Run.anim", 0.64f, 32, true);
            AnimationClip jumpClip = GetOrCreateClip($"{rootDir}/Jump.anim", 1.22f, 61, false);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            AnimatorState runState = sm.AddState("Run");
            runState.motion = runClip;

            AnimatorState jumpState = sm.AddState("Jump");
            jumpState.motion = jumpClip;

            AnimatorState fallState = sm.AddState("Fall");
            fallState.motion = jumpClip;

            AnimatorState landState = sm.AddState("Landing");
            landState.motion = jumpClip;

            AnimatorState deathState = sm.AddState("Death");
            deathState.motion = jumpClip;

            AddAnyTransition(sm, idleState, 0);
            AddAnyTransition(sm, runState, 1);
            AddAnyTransition(sm, jumpState, 2);
            AddAnyTransition(sm, fallState, 3);
            AddAnyTransition(sm, landState, 4);
            AddAnyTransition(sm, deathState, 5);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState dst, int stateVal)
        {
            AnimatorStateTransition tr = sm.AddAnyStateTransition(dst);
            tr.hasExitTime = false;
            tr.duration = 0f;
            tr.canTransitionToSelf = false;
            tr.AddCondition(AnimatorConditionMode.Equals, stateVal, "State");
        }

        private static AnimationClip GetOrCreateClip(string path, float duration, int frameCount, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { frameRate = frameCount / duration };
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, path);
                EditorUtility.SetDirty(clip);
            }
            return clip;
        }
    }
}

