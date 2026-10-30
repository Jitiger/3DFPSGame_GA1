using UnityEngine;

internal sealed class ZombieAttackState : ZombieStateBase
{
    private float _attackTimer;

    public ZombieAttackState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _attackTimer = 0f;
    }

    public override void Update()
    {
        Zombie.StopAgent();

        float distance = Vector3.Distance(Zombie.transform.position, Zombie.PlayerPosition);
        if (distance > Zombie.AttackRange)
        {
            Zombie.ChangeState(ZombieState.Trace, "AttackMove");
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= Zombie.AttackInterval)
        {
            Zombie.Animator.SetTrigger("StartAttack");
            _attackTimer = 0f;
        }
    }
}
