using UnityEngine;

internal sealed class ZombieTraceState : ZombieStateBase
{
    public override bool CanJump => true;

    public ZombieTraceState(Zombie zombie) : base(zombie) { }

    public override void Update()
    {
        if (Vector3.Distance(Zombie.transform.position, Zombie.StartPosition) > Zombie.ComebackDistance)
        {
            Zombie.ChangeState(ZombieState.Comeback);
            return;
        }

        if (Vector3.Distance(Zombie.transform.position, Zombie.PlayerPosition) <= Zombie.AttackRange)
        {
            Zombie.ChangeState(ZombieState.Attack, "MoveToAttackDelay");
            return;
        }

        Zombie.Agent.SetDestination(Zombie.PlayerPosition);
    }
}
