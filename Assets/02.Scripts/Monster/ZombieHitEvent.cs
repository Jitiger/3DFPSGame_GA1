using UnityEngine;

public class ZombieHitEvent : MonoBehaviour
{
    private Zombie _zombie;

    private void Start()
    {
        _zombie = GetComponentInParent<Zombie>();
        if (_zombie == null)
            Debug.LogError($"{name}: Zombie 컴포넌트를 찾을 수 없습니다.", this);
    }

    public void HitEvent()
    {
        _zombie?.AttackAction();
    }
}