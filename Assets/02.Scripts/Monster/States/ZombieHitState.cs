using System.Collections;
using UnityEngine;

internal sealed class ZombieHitState : ZombieStateBase
{
    private Coroutine _coroutine;

    public ZombieHitState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        Zombie.Animator.SetTrigger("Hit");
        _coroutine = Zombie.StartCoroutine(React_Coroutine());
    }

    public override void Exit()
    {
        if (_coroutine != null)
        {
            Zombie.StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private IEnumerator React_Coroutine()
    {
        // 피격돼도 점프 이동은 끊지 않고 착지한 뒤 멈춘다.
        while (Zombie.IsJumping) yield return null;
        Zombie.StopAgent();
        yield return new WaitForSeconds(0.5f);

        _coroutine = null;
        Zombie.ChangeState(ZombieState.Trace, "IdleToMove");
    }
}
