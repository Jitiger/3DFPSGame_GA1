using UnityEngine;

// Animator가 있는 자식 모델의 이벤트를 부모 PlayerBombFire로 전달한다.
public class PlayerBombAnimationEvent : MonoBehaviour
{
    private PlayerBombFire _playerBombFire;

    private void Awake()
    {
        _playerBombFire = GetComponentInParent<PlayerBombFire>();
    }

    public void ReleaseBomb()
    {
        _playerBombFire.ReleaseBomb();
    }

    public void FinishBombThrow()
    {
        _playerBombFire.FinishBombThrow();
    }
}
