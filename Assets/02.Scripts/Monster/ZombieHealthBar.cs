using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
    }

    private void OnEnable()
    {
        // 켜질 때 구독하고, 꺼져 있는 동안 바뀐 체력도 반영한다.
        _zombie.OnDamaged += RefreshHealth;
        RefreshHealth();
    }

    private void Start()
    {
        // 모든 Awake가 끝난 뒤 초기 체력을 표시한다.
        RefreshHealth();
    }

    private void OnDisable()
    {
        _zombie.OnDamaged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        // 빌보드는 카메라 방향에 맞춰 매 프레임 갱신한다.
        _slider.transform.forward = Camera.main.transform.forward;
    }

    private void RefreshHealth()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}