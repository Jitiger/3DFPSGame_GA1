// 상태 객체는 컴포넌트가 아니다. 좀비마다 new로 만들어 재사용한다.
internal abstract class ZombieStateBase
{
    protected Zombie Zombie { get; }

    public virtual bool CanJump => false;

    protected ZombieStateBase(Zombie zombie)
    {
        Zombie = zombie;
    }

    // 상태에 들어오는 순간 한 번 실행한다.
    public virtual void Enter() { }

    // 현재 상태인 동안 매 프레임 실행한다.
    public virtual void Update() { }

    // 상태를 떠나거나 좀비가 꺼질 때 정리한다.
    public virtual void Exit() { }
}
