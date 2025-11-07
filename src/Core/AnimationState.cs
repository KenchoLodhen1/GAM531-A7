namespace OpenTK_Sprite_Animation
{
    /// <summary>
    /// Possible animation states for the character
    /// </summary>
    public enum AnimationState
    {
        Idle,
        Walk,
        Run,
        RunAttack,
        Attack1,
        Attack2,
        Attack3,
        Jump,
        Defend
    }

    /// <summary>
    /// Direction the character is facing
    /// </summary>
    public enum FacingDirection
    {
        Right,
        Left
    }
}