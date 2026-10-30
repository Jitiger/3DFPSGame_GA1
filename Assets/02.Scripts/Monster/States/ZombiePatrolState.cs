using UnityEngine;

internal sealed class ZombiePatrolState : ZombieStateBase
{
    private Vector3 _pointA;
    private Vector3 _pointB;
    private Vector3 _target;

    public override bool CanJump => true;

    public ZombiePatrolState(Zombie zombie) : base(zombie)
    {
        ResetPatrol();
    }

    public void ResetPatrol()
    {
        _pointA = Zombie.StartPosition + Vector3.left * Zombie.PatrolDistance;
        _pointB = Zombie.StartPosition + Vector3.right * Zombie.PatrolDistance;
        _target = _pointA;
    }

    public override void Update()
    {
        float playerDistance = Vector3.Distance(Zombie.transform.position, Zombie.PlayerPosition);
        if (playerDistance <= Zombie.DetectDistance)
        {
            Zombie.ChangeState(ZombieState.Trace);
            return;
        }

        Vector3 direction = _target - Zombie.transform.position;
        direction.y = 0f;
        if (direction.magnitude <= Zombie.PatrolArrivalDistance)
        {
            _target = _target == _pointA ? _pointB : _pointA;
            Zombie.ChangeState(ZombieState.Idle, "MoveToIdle");
            return;
        }

        Zombie.Agent.SetDestination(_target);
    }
}
