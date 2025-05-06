using UnityEngine;

public abstract class PlayerState
{
    protected PlayerScript player;
    
    public PlayerState(PlayerScript player)
    {
        this.player = player;
    }
    
    public virtual void Enter() { }
    public virtual void Update(Vector2 input) { }
    public virtual void Exit() { }
}
