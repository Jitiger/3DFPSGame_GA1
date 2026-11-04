using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;
    private Transform _cameraTransform;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
        CacheCamera();
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
        // 카메라 참조는 재사용하고 빌보드 방향만 갱신한다.
        if (_cameraTransform == null) CacheCamera();
        if (_cameraTransform == null) return;

        _slider.transform.forward = _cameraTransform.forward;
    }

    private void CacheCamera()
    {
        Camera mainCamera = Camera.main;
        _cameraTransform = mainCamera != null ? mainCamera.transform : null;
    }

    private void RefreshHealth()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
