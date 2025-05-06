using UnityEngine;

public class IdleState : PlayerState
{
    public IdleState(PlayerScript player) : base(player) { }
    
    public override void Enter()
    {
        player.SetAnimationState(false);
        player.StopFootstepSound();
    }
    
    public override void Update(Vector2 input)
    {
        // In idle state, we don't process movement
    }
}
