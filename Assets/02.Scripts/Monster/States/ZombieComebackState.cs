using UnityEngine;

internal sealed class ZombieComebackState : ZombieStateBase
{
    public override bool CanJump => true;

    public ZombieComebackState(Zombie zombie) : base(zombie) { }

    public override void Update()
    {
        if (Vector3.Distance(Zombie.transform.position, Zombie.StartPosition) < Zombie.Agent.stoppingDistance)
        {
            Zombie.ChangeState(ZombieState.Idle, "MoveToIdle");
            return;
        }

        Zombie.Agent.SetDestination(Zombie.StartPosition);
    }
}
