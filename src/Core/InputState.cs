namespace OpenTK_Sprite_Animation
{
    /// <summary>
    /// Represents the current input state from the player
    /// </summary>
    public struct InputState
    {
        public bool Left;
        public bool Right;
        public bool Jump;
        public bool Sprint;
        public bool Attack;
        public bool Defend;

        // Edge-triggered presses (this-frame only)
        public bool LeftPressed;
        public bool RightPressed;
    }
}