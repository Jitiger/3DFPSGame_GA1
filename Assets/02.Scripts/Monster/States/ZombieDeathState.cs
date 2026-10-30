using System.Collections;
using UnityEngine;

internal sealed class ZombieDeathState : ZombieStateBase
{
    private Coroutine _coroutine;

    public ZombieDeathState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        Zombie.Animator.SetTrigger("Death");
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
        while (Zombie.IsJumping) yield return null;
        Zombie.StopAgent();
        yield return new WaitForSeconds(2f);
        _coroutine = null;
        Zombie.ReturnToPool();
    }
}
