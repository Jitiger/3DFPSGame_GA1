using System.Collections.Generic;
using System;
using UnityEngine;

// 우클릭으로 힘을 모으고, 손을 떼면 오른팔의 던지기 동작에 맞춰 폭탄을 던진다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private float _chargeSpeed = 10f;
    [SerializeField] private float _maxThrowPower = 45f;
    [SerializeField] private int _maxBombCount = 10;
    [SerializeField, Min(1)] private int _poolSize = 10;

    private Player _player;

    private void Start()
    {
        _player = GetComponent<Player>();
    }

    private int _bombCount = 0;
    public int BombCount => _maxBombCount - _bombCount;
    public event Action<int> OnBombChanged;
    private readonly List<Bomb> _bombPool = new List<Bomb>();
    private Animator _animator;
    private float _initialThrowPower;
    private float _pendingThrowPower;
    private bool _isCharging;
    private bool _isThrowing;
    private bool _hasReleased;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _initialThrowPower = _throwPower;
        for (int i = 0; i < _poolSize; i++)
        {
            Bomb bomb = Instantiate(_bombPrefab, _firePoint.position, Quaternion.identity);
            bomb.OnReturned += OnBombReturned;
            bomb.gameObject.SetActive(false);
            _bombPool.Add(bomb);
        }
    }

    private void OnDestroy()
    {
        foreach (Bomb bomb in _bombPool)
        {
            if (bomb != null) bomb.OnReturned -= OnBombReturned;
        }
    }

    private void OnBombReturned()
    {
        _bombCount--;
        OnBombChanged?.Invoke(BombCount);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing
            || Cursor.visible
            || _player.WeaponMode != WeaponMode.Normal)
        {
            _isCharging = false;
            _throwPower = _initialThrowPower;
            return;
        }

        if (_isThrowing || _bombCount >= _maxBombCount) return;

        if (Input.GetButtonDown("Fire2"))
        {
            _isCharging = true;
            _throwPower = _initialThrowPower;
        }

        if (_isCharging && Input.GetButton("Fire2"))
        {
            _throwPower = Mathf.Min(_throwPower + _chargeSpeed * Time.deltaTime, _maxThrowPower);
        }

        if (_isCharging && Input.GetButtonUp("Fire2"))
        {
            BeginThrow();
        }
    }

    private void BeginThrow()
    {
        _isCharging = false;
        _isThrowing = true;
        _hasReleased = false;
        _pendingThrowPower = _throwPower;
        _throwPower = _initialThrowPower;
        _animator.SetTrigger("ThrowBomb");
    }

    // Animation Event: 팔을 앞으로 뻗는 순간 한 번 호출한다.
    public void ReleaseBomb()
    {
        if (!_isThrowing || _hasReleased || GameManager.Instance.State != GameState.Playing
            || _player.WeaponMode != WeaponMode.Normal)
        {
            return;
        }

        _hasReleased = true;

        Bomb bomb = GetBomb();
        if (bomb == null)
        {
            Debug.LogWarning("사용 가능한 폭탄이 없습니다.");
            return;
        }

        bomb.transform.SetPositionAndRotation(_firePoint.position, Quaternion.identity);
        bomb.gameObject.SetActive(true);

        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.AddForce(Camera.main.transform.forward * _pendingThrowPower, ForceMode.Impulse);
        _bombCount++;
        OnBombChanged?.Invoke(BombCount);
    }

    // Animation Event: 던지기 동작이 끝나면 다음 입력을 받을 수 있게 한다.
    public void FinishBombThrow()
    {
        _isThrowing = false;
        _hasReleased = false;
        _pendingThrowPower = 0f;
    }

    private void OnDisable()
    {
        _isCharging = false;
        _throwPower = _initialThrowPower;
        FinishBombThrow();
    }

    private Bomb GetBomb()
    {
        foreach (Bomb bomb in _bombPool)
        {
            if (!bomb.gameObject.activeSelf) return bomb;
        }

        return null;
    }
}
