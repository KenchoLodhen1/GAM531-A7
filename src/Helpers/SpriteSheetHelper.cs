using OpenTK.Graphics.OpenGL4;

namespace OpenTK_Sprite_Animation
{
    /// <summary>
    /// Helper class for calculating and setting sprite sheet UV coordinates
    /// </summary>
    public static class SpriteSheetHelper
    {
        // All frames are 86x86 pixels
        private const float FrameSize = 86f;

        /// <summary>
        /// Gets the frame count for a given animation state
        /// </summary>
        public static int GetFrameCount(AnimationState state)
        {
            return state switch
            {
                AnimationState.Idle => 4,
                AnimationState.Walk => 8,
                AnimationState.Run => 7,
                AnimationState.RunAttack => 6,
                AnimationState.Attack1 => 5,
                AnimationState.Attack2 => 4,
                AnimationState.Attack3 => 4,
                AnimationState.Jump => 6,
                AnimationState.Defend => 5,
                _ => 1
            };
        }

        /// <summary>
        /// Gets the texture filename for a given animation state
        /// </summary>
        public static string GetTextureFileName(AnimationState state)
        {
            return state switch
            {
                AnimationState.Idle => "Idle.png",
                AnimationState.Walk => "Walk.png",
                AnimationState.Run => "Run.png",
                AnimationState.RunAttack => "Run+Attack.png",
                AnimationState.Attack1 => "Attack 1.png",
                AnimationState.Attack2 => "Attack 2.png",
                AnimationState.Attack3 => "Attack 3.png",
                AnimationState.Jump => "Jump.png",
                AnimationState.Defend => "Defend.png",
                _ => "Idle.png"
            };
        }

        /// <summary>
        /// Sets the UV coordinates for the current sprite frame from a horizontal strip
        /// </summary>
        public static void SetSpriteFrame(int shader, int frame, AnimationState state, float sheetWidth)
        {
            // Get how many frames this animation has
            int frameCount = GetFrameCount(state);

            // Clamp frame index to valid range
            frame = frame % frameCount;

            // Use equal divisions of the texture (prevents size jitter)
            float w = 1f / frameCount;
            float x = frame * w;

            // Full height since each texture is a single row
            float y = 0f;
            float h = 1f;

            // Upload to shader uniforms
            GL.UseProgram(shader);
            int offsetLoc = GL.GetUniformLocation(shader, "uOffset");
            int sizeLoc = GL.GetUniformLocation(shader, "uSize");
            GL.Uniform2(offsetLoc, x, y);
            GL.Uniform2(sizeLoc, w, h);
        }
    }
}