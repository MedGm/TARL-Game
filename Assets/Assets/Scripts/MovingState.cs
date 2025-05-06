using UnityEngine;

public class MovingState : PlayerState
{
    public MovingState(PlayerScript player) : base(player) { }
    
    public override void Enter()
    {
        player.SetAnimationState(true);
    }
    
    public override void Update(Vector2 input)
    {
        if (input.sqrMagnitude > 0)
        {
            // Calculate the target position
            Vector2 targetPosition = (Vector2)player.transform.position + input * player.movementSpeed * Time.deltaTime;
            
            // Only move if the position is walkable
            if (player.IsWalkable(targetPosition))
            {
                player.MovePlayer(input);
                
                // Play footstep sound
                player.PlayFootstepSound();
                player.UpdateFootstepTimer();
            }
            else
            {
                // Can't move, switch back to idle
                player.ChangeState(new IdleState(player));
            }
        }
    }
    
    public override void Exit()
    {
        player.StopFootstepSound();
    }
}
