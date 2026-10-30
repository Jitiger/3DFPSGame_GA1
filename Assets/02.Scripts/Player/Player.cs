using UnityEngine;
using System;

public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;
    private float _health;
    private Animator _animator;
    private int _injuredLayerIndex = -1;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private WeaponMode _weaponMode = WeaponMode.Normal;
    public WeaponMode WeaponMode => _weaponMode;


    //Action 자료형: 함수(들)를 저장할 수 있다.
    public event Action OnDamaged;
    public event Action<WeaponMode> OnWeaponModeChanged;
    [SerializeField] private UI_HitEffect _uiHitEffect;
    [SerializeField] private GameObject _meleeWeapon;
    [SerializeField] private GameObject _gunWeapon;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex("Injured Layer");
        _health = _maxHealth;
        _meleeWeapon.SetActive(_weaponMode == WeaponMode.Melee);
        _gunWeapon.SetActive(_weaponMode != WeaponMode.Melee);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeWeaponMode(WeaponMode.Normal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeWeaponMode(WeaponMode.Sniper);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeWeaponMode(WeaponMode.Melee);
        }
    }

    private void ChangeWeaponMode(WeaponMode mode)
    {
        _weaponMode = mode;
        _meleeWeapon.SetActive(mode == WeaponMode.Melee);
        _gunWeapon.SetActive(mode != WeaponMode.Melee);
        OnWeaponModeChanged?.Invoke(mode);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;

        // 체력에 따라 Injured Layer의 가중치를 바꿔준다.
        float healthPercent = _health / _maxHealth;
        float injured = Mathf.InverseLerp(0.5f, 0f, healthPercent);
        if (_injuredLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_injuredLayerIndex, injured);
        }

        OnDamaged?.Invoke();

        _uiHitEffect.Fade(0.7f);

        Debug.Log($"플레이어가 맞았다. 데미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다!");
            GameManager.Instance.GameOver();
        }
    }
}
