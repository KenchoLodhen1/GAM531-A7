using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenTK_Sprite_Animation
{
    /// <summary>
    /// Character controller managing animation states, physics, and rendering
    /// </summary>
    public class Character
    {
        public float GetPositionX() => _posX;

        // Animation timing
        private float _animTimer;
        private int _currentFrame;

        // State machine
        private AnimationState _state;
        private FacingDirection _facing;

        // Combat state
        private float _attackCooldown;
        private int _attackComboCounter;
        private const float AttackCooldownTime = 0.5f;

        // Physics
        private float _posX, _posY;
        private float _velocityY;
        private bool _isGrounded;

        // Movement constants
        private const float WalkSpeed = 150f;
        private const float RunSpeed = 300f;
        private const float JumpForce = 500f;
        private const float Gravity = 1200f;
        private const float GroundLevel = 150f;

        // Screen boundaries
        private const float MinX = 50f;
        private const float MaxX = 1500f;

        // Animation speeds (seconds per frame)
        private const float IdleFrameTime = 0.2f;
        private const float WalkFrameTime = 0.12f;
        private const float RunFrameTime = 0.08f;
        private const float RunAttackFrameTime = 0.1f;
        private const float Attack1FrameTime = 0.08f;
        private const float Attack2FrameTime = 0.08f;
        private const float Attack3FrameTime = 0.08f;
        private const float JumpFrameTime = 0.1f;
        private const float DefendFrameTime = 0.15f;

        // Texture references
        private Dictionary<AnimationState, int> _textures;
        private Dictionary<AnimationState, float> _textureWidths;

        public Character(Dictionary<AnimationState, int> textures, Dictionary<AnimationState, float> textureWidths, float startX, float startY)
        {
            _textures = textures;
            _textureWidths = textureWidths;
            _posX = startX;
            _posY = startY;
            _velocityY = 0f;
            _isGrounded = true;
            _state = AnimationState.Idle;
            _facing = FacingDirection.Right;
            _currentFrame = 0;
            _animTimer = 0f;
            _attackCooldown = 0f;
            _attackComboCounter = 0;
        }

        /// <summary>
        /// Updates character state, physics, and animation based on input
        /// </summary>
        public void Update(float delta, InputState input)
        {
            AnimationState previousState = _state;

            // Update attack cooldown
            if (_attackCooldown > 0)
            {
                _attackCooldown -= delta;
            }

            // Update physics
            UpdatePhysics(delta, input.Jump);

            // Determine new state based on input and physics state
            DetermineState(input);

            // Apply horizontal movement if walking or running
            if (_state == AnimationState.Walk || _state == AnimationState.Run ||
                _state == AnimationState.RunAttack || _state == AnimationState.Jump)
            {
                UpdateMovement(delta, input);
            }

            // Reset animation when state changes
            if (_state != previousState)
            {
                _currentFrame = 0;
                _animTimer = 0f;
            }

            // Update animation frame
            UpdateAnimation(delta);
        }

        /// <summary>
        /// Determines the current animation state based on physics and input
        /// </summary>
        private void DetermineState(InputState input)
        {
            // Priority 1: Attacking states
            if (IsAttackState(_state) && _currentFrame < SpriteSheetHelper.GetFrameCount(_state) - 1)
            {
                // Continue current attack animation
                return;
            }

            // Priority 2: New attack input
            if (input.Attack && _attackCooldown <= 0)
            {
                _attackCooldown = AttackCooldownTime;

                // Cycle through attack combos
                if (input.Left || input.Right)
                {
                    _state = AnimationState.RunAttack;
                }
                else
                {
                    _attackComboCounter = (_attackComboCounter + 1) % 3;
                    _state = _attackComboCounter switch
                    {
                        0 => AnimationState.Attack1,
                        1 => AnimationState.Attack2,
                        _ => AnimationState.Attack3
                    };
                }
                return;
            }

            // Priority 3: Defending
            if (input.Defend)
            {
                _state = AnimationState.Defend;
                return;
            }

            // Turn-in-place: if grounded, not holding movement, but tapped a direction
            if (_isGrounded && !input.Left && !input.Right && (input.LeftPressed ^ input.RightPressed))
            {
                _facing = input.LeftPressed ? FacingDirection.Left : FacingDirection.Right;
                _state = AnimationState.Idle;
                return;
            }

            // Priority 4: Jumping (in air)
            if (!_isGrounded)
            {
                _state = AnimationState.Jump;
                return;
            }

            // Priority 5: Moving (walk or run)
            if (input.Left || input.Right)
            {
                _state = input.Sprint ? AnimationState.Run : AnimationState.Walk;
                return;
            }

            // Default: Idle
            _state = AnimationState.Idle;
        }

        /// <summary>
        /// Checks if the current state is an attack state
        /// </summary>
        private bool IsAttackState(AnimationState state)
        {
            return state == AnimationState.Attack1 ||
                   state == AnimationState.Attack2 ||
                   state == AnimationState.Attack3 ||
                   state == AnimationState.RunAttack;
        }

        /// <summary>
        /// Updates horizontal movement and facing direction
        /// </summary>
        private void UpdateMovement(float delta, InputState input)
        {
            // Determine base speed based on state
            float baseSpeed = (_state == AnimationState.Run || _state == AnimationState.RunAttack)
                ? RunSpeed
                : WalkSpeed;

            // Reduce horizontal control slightly while in air
            float airControl = _isGrounded ? 1f : 0.7f;
            float speed = baseSpeed * airControl;

            // Handle directional input
            if (input.Right)
            {
                _facing = FacingDirection.Right;
                _posX += speed * delta;
            }
            else if (input.Left)
            {
                _facing = FacingDirection.Left;
                _posX -= speed * delta;
            }

            // Clamp position so sprite stays on screen
            _posX = Math.Clamp(_posX, MinX, MaxX);
        }

        /// <summary>
        /// Updates vertical physics with gravity and jumping
        /// </summary>
        private void UpdatePhysics(float delta, bool jumpPressed)
        {
            // Apply gravity when in air
            if (!_isGrounded)
            {
                _velocityY -= Gravity * delta;
            }

            // Handle jump input (only when grounded)
            if (jumpPressed && _isGrounded)
            {
                _velocityY = JumpForce;
                _isGrounded = false;
            }

            // Update vertical position
            _posY += _velocityY * delta;

            // Ground collision detection
            if (_posY <= GroundLevel)
            {
                _posY = GroundLevel;
                _velocityY = 0f;
                _isGrounded = true;
            }
        }

        /// <summary>
        /// Updates the current animation frame based on elapsed time
        /// </summary>
        private void UpdateAnimation(float delta)
        {
            int frameCount = SpriteSheetHelper.GetFrameCount(_state);
            float frameTime = GetFrameTime(_state);

            _animTimer += delta;
            if (_animTimer >= frameTime)
            {
                _animTimer -= frameTime;
                _currentFrame++;

                // Loop or stop based on animation type
                if (IsAttackState(_state))
                {
                    // Attack animations play once then return to idle
                    if (_currentFrame >= frameCount)
                    {
                        _currentFrame = frameCount - 1;
                    }
                }
                else
                {
                    // Other animations loop
                    _currentFrame = _currentFrame % frameCount;
                }
            }
        }

        /// <summary>
        /// Renders the character at its current position with the current animation frame
        /// </summary>
        public void Render(int shader)
        {
            // Bind the appropriate texture for current state
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _textures[_state]);

            // Update model matrix for character position
            GL.UseProgram(shader);
            int modelLoc = GL.GetUniformLocation(shader, "model");

            // sprite dimensions (your quad is 86×86)
            float scaleX = _facing == FacingDirection.Left ? -1f : 1f;
            float visualScale = 2.5f;

            Matrix4 model =
                Matrix4.CreateScale(visualScale * scaleX, visualScale, 1f) *
                Matrix4.CreateTranslation(_posX, _posY, 0f);

            GL.UniformMatrix4(modelLoc, false, ref model);

            // Set the appropriate sprite frame
            SpriteSheetHelper.SetSpriteFrame(shader, _currentFrame, _state, _textureWidths[_state]);

            // Draw the quad
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        }

        private float GetFrameTime(AnimationState state)
        {
            return state switch
            {
                AnimationState.Idle => IdleFrameTime,
                AnimationState.Walk => WalkFrameTime,
                AnimationState.Run => RunFrameTime,
                AnimationState.RunAttack => RunAttackFrameTime,
                AnimationState.Attack1 => Attack1FrameTime,
                AnimationState.Attack2 => Attack2FrameTime,
                AnimationState.Attack3 => Attack3FrameTime,
                AnimationState.Jump => JumpFrameTime,
                AnimationState.Defend => DefendFrameTime,
                _ => 0.2f
            };
        }
    }
}