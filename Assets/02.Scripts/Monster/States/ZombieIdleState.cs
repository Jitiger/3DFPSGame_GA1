using UnityEngine;

internal sealed class ZombieIdleState : ZombieStateBase
{
    private float _idleTimer;

    public ZombieIdleState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _idleTimer = 0f;
    }

    public override void Update()
    {
        Zombie.StopAgent();

        float distance = Vector3.Distance(Zombie.transform.position, Zombie.PlayerPosition);
        if (distance <= Zombie.DetectDistance)
        {
            Zombie.ChangeState(ZombieState.Trace, "IdleToMove");
            return;
        }

        _idleTimer += Time.deltaTime;
        if (_idleTimer >= Zombie.PatrolWaitTime)
        {
            Zombie.ChangeState(ZombieState.Patrol, "IdleToMove");
        }
    }
}
