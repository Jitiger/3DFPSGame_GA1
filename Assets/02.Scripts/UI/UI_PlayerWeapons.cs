using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Sprite[] _modeIcons;
    [SerializeField] private TextMeshProUGUI _modeText;
    [SerializeField] private TextMeshProUGUI _bulletText;
    [SerializeField] private TextMeshProUGUI _bombText;
    [SerializeField] private Player _player;
    [SerializeField] private PlayerGunFire _gunFire;
    [SerializeField] private PlayerBombFire _bombFire;
    private bool _hasStarted;

    private void OnEnable()
    {
        _player.OnWeaponModeChanged += RefreshMode;
        _gunFire.OnBulletChanged += RefreshBullet;
        _bombFire.OnBombChanged += RefreshBomb;
        if (_hasStarted) RefreshAll();
    }

    private void Start()
    {
        _hasStarted = true;
        RefreshAll();
    }

    private void OnDisable()
    {
        _player.OnWeaponModeChanged -= RefreshMode;
        _gunFire.OnBulletChanged -= RefreshBullet;
        _bombFire.OnBombChanged -= RefreshBomb;
    }

    private void RefreshAll()
    {
        RefreshMode(_player.WeaponMode);
        RefreshBullet(_gunFire.BulletCount, _gunFire.MaxBulletCount);
        RefreshBomb(_bombFire.BombCount);
    }

    private void RefreshMode(WeaponMode mode)
    {
        int iconIndex = (int)mode;
        _weaponIcon.sprite = iconIndex < _modeIcons.Length ? _modeIcons[iconIndex] : null;
        _weaponIcon.enabled = _weaponIcon.sprite != null;
        switch (mode)
        {
            case WeaponMode.Normal:
                _modeText.text = "일반 모드";
                break;
            case WeaponMode.Sniper:
                _modeText.text = "저격 모드";
                break;
            case WeaponMode.Melee:
                _modeText.text = "근접 모드";
                break;
        }
    }

    private void RefreshBullet(int count, int maxCount)
    {
        _bulletText.text = $"{count} / {maxCount}";
    }

    private void RefreshBomb(int count)
    {
        _bombText.text = $"폭탄 {count}";
    }
}
