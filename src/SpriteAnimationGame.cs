using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

namespace OpenTK_Sprite_Animation
{
    public class SpriteAnimationGame : GameWindow
    {
        private Character _character;
        private int _shaderProgram;
        private int _vao, _vbo;
        private Dictionary<AnimationState, int> _textures;
        private Dictionary<AnimationState, float> _textureWidths;
        private int _backgroundTex;
        private int _backgroundWidth;
        private int _backgroundHeight;

        // Camera
        private float _cameraX = 0f;
        private const float CameraSmoothness = 5f;
        private const float SceneWidth = 1600f;

        private KeyboardState _prevKeyboard;

        public SpriteAnimationGame()
            : base(
                new GameWindowSettings(),
                new NativeWindowSettings { Size = (800, 600), Title = "Advanced Sprite Animation" })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.4f, 1f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shaderProgram = ShaderHelper.CreateShaderProgram();

            // Load all animation textures
            _textures = new Dictionary<AnimationState, int>();
            _textureWidths = new Dictionary<AnimationState, float>();

            LoadAnimationTexture(AnimationState.Idle);
            LoadAnimationTexture(AnimationState.Walk);
            LoadAnimationTexture(AnimationState.Run);
            LoadAnimationTexture(AnimationState.RunAttack);
            LoadAnimationTexture(AnimationState.Attack1);
            LoadAnimationTexture(AnimationState.Attack2);
            LoadAnimationTexture(AnimationState.Attack3);
            LoadAnimationTexture(AnimationState.Jump);
            LoadAnimationTexture(AnimationState.Defend);

            // Create quad geometry (86x86 sprite size)
            float w = 86f, h = 86f;
            float[] vertices =
            {
                -w/2, -h/2, 0f, 0f,
                 w/2, -h/2, 1f, 0f,
                 w/2,  h/2, 1f, 1f,
                -w/2,  h/2, 0f, 1f
            };

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Position attribute
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // Texture coordinate attribute
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            // Load background (scenario)
            _backgroundTex = TextureLoader.LoadTexture(System.IO.Path.Combine("Assets", "Sprites", "Background.png"));

            // Get background dimensions
            using var bgImg = SixLabors.ImageSharp.Image.Load<Rgba32>(System.IO.Path.Combine("Assets", "Sprites", "Background.png"));
            _backgroundWidth = bgImg.Width;
            _backgroundHeight = bgImg.Height;

            // Setup shader uniforms
            GL.UseProgram(_shaderProgram);

            int texLoc = GL.GetUniformLocation(_shaderProgram, "uTexture");
            GL.Uniform1(texLoc, 0);

            _character = new Character(_textures, _textureWidths, 400, 150);
        }

        private void LoadAnimationTexture(AnimationState state)
        {
            string filename = SpriteSheetHelper.GetTextureFileName(state);
            string path = System.IO.Path.Combine("Assets", "Sprites", filename);

            _textures[state] = TextureLoader.LoadTexture(path);

            // Get texture width for UV calculations
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
            _textureWidths[state] = img.Width;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (_prevKeyboard == null)
                _prevKeyboard = KeyboardState;

            base.OnUpdateFrame(e);

            // Gather input from keyboard
            var k = KeyboardState;

            bool leftDown = k.IsKeyDown(Keys.Left) || k.IsKeyDown(Keys.A);
            bool rightDown = k.IsKeyDown(Keys.Right) || k.IsKeyDown(Keys.D);

            bool prevLeftDown = _prevKeyboard.IsKeyDown(Keys.Left) || _prevKeyboard.IsKeyDown(Keys.A);
            bool prevRightDown = _prevKeyboard.IsKeyDown(Keys.Right) || _prevKeyboard.IsKeyDown(Keys.D);

            var input = new InputState
            {
                Left = leftDown,
                Right = rightDown,
                Jump = k.IsKeyDown(Keys.Space) || k.IsKeyDown(Keys.W),
                Sprint = k.IsKeyDown(Keys.LeftShift) || k.IsKeyDown(Keys.RightShift),
                Attack = k.IsKeyDown(Keys.J) || k.IsKeyDown(Keys.Z),
                Defend = k.IsKeyDown(Keys.K) || k.IsKeyDown(Keys.X),

                LeftPressed = leftDown && !prevLeftDown,
                RightPressed = rightDown && !prevRightDown
            };

            if (_character != null)
                _character.Update((float)e.Time, input);

            // Camera Follow
            float targetCameraX = _character.GetPositionX() - 400f;
            _cameraX += (targetCameraX - _cameraX) * (CameraSmoothness * (float)e.Time);

            // Clamp camera to scene edges
            _cameraX = Math.Clamp(_cameraX, 0f, SceneWidth - 800f);

            _prevKeyboard = k;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(_shaderProgram);
            GL.BindVertexArray(_vao);

            // Update projection matrix with current camera position
            int projLoc = GL.GetUniformLocation(_shaderProgram, "projection");
            Matrix4 ortho = Matrix4.CreateOrthographicOffCenter(_cameraX, _cameraX + 800, 0, 600, -1, 1);
            GL.UniformMatrix4(projLoc, false, ref ortho);

            // Draw parallax background
            GL.BindTexture(TextureTarget.Texture2D, _backgroundTex);

            // Keep original image proportions
            float scaledHeight = 600f;
            float aspectRatio = (float)_backgroundWidth / _backgroundHeight;
            float scaledWidth = scaledHeight * aspectRatio;

            // Draw multiple copies of the background to cover the visible area
            float startX = (float)Math.Floor(_cameraX / scaledWidth) * scaledWidth;
            int tilesNeeded = (int)Math.Ceiling(800f / scaledWidth) + 2;

            for (int i = 0; i < tilesNeeded; i++)
            {
                float tileX = startX + (i * scaledWidth);

                Matrix4 bgModel =
                    Matrix4.CreateScale(scaledWidth / 86f, scaledHeight / 86f, 1f) *
                    Matrix4.CreateTranslation(tileX + scaledWidth / 2, 300f, 0f); // Centered vertically

                GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref bgModel);
                SpriteSheetHelper.SetSpriteFrame(_shaderProgram, 0, AnimationState.Idle, 1f);
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            }

            // Draw character
            _character.Render(_shaderProgram);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            GL.DeleteProgram(_shaderProgram);

            foreach (var texture in _textures.Values)
            {
                GL.DeleteTexture(texture);
            }

            GL.DeleteTexture(_backgroundTex);
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            base.OnUnload();
        }
    }
}