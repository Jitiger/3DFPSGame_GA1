using System.Collections;
using System;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireInterval = 0.1f;
    [SerializeField] private float _maxBullet = 50f;
    [SerializeField] private float _reloadingTime = 3f;

    [Header("발사선")]
    [SerializeField] private LineRenderer _shotLine;
    [SerializeField] private Transform _lineOrigin;
    [SerializeField, Min(0.01f)] private float _lineDuration = 0.05f;
    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _floashTime = 0.05f;

    private Coroutine[] _muzzleCoroutines;
    private Coroutine _lineCoroutine;
    private float _fireTimer = 0f;
    private float _currentBullet = 0f;
    private Coroutine _reloadCoroutine;
    private Animator _animator;
    private Player _player;
    public int BulletCount => Mathf.FloorToInt(_currentBullet);
    public int MaxBulletCount => Mathf.FloorToInt(_maxBullet);
    public event Action<int, int> OnBulletChanged;

    private void Awake()
    {
        _currentBullet = _maxBullet;
        _animator = GetComponentInChildren<Animator>();
        _player = GetComponent<Player>();
        if (_shotLine != null)
        {
            _shotLine.useWorldSpace = true;
            _shotLine.positionCount = 2;
            _shotLine.enabled = false;
        }

        if (_muzzleFlashes != null)
            _muzzleCoroutines = new Coroutine[_muzzleFlashes.Length];
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 칼을 들고 있을 때는 총을 쏘지 않는다.
        if (_player.WeaponMode == WeaponMode.Melee)
        {
            _animator.SetBool("Fire", false);
            return;
        }

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        bool isMousevisible = Cursor.visible;
        bool wantsToFire = !isMousevisible && Input.GetButton("Fire1") && _currentBullet > 0;
        _animator.SetBool("Fire", wantsToFire);

        if (wantsToFire && _fireTimer <= 0f)
        {
            _currentBullet--;
            OnBulletChanged?.Invoke(BulletCount, MaxBulletCount);
            _fireTimer = _fireInterval;
            Shoot();

            // 이 방식은 이 모노비헤이비어가 등록한 모든 코루틴을 실행 취소
            //StopAllCoroutines();
            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                Debug.Log("재장전 취소");
                _reloadCoroutine = null;
            }
        }

        // 2. R키 누르면 총알 재장전
        if (Input.GetButtonDown("Reload") && _reloadCoroutine == null)
        {
            _reloadCoroutine = StartCoroutine(Reloading_Coroutine());
        }
    }

    private IEnumerator Reloading_Coroutine()
    {
        Debug.Log("재장전 시작");

        yield return new WaitForSeconds(_reloadingTime);

        _currentBullet = _maxBullet;

        OnBulletChanged?.Invoke(BulletCount, MaxBulletCount);
        Debug.Log("재장전 종료");
        _reloadCoroutine = null;
    }

    private void Shoot()
    {
        ShowMuzzleFlash();
        // Ray: 레이저(시작 위치, 날아갈 방향)
        // Physics.RayCast: 레이저를 발사
        // RayCastHit: 레이저가 물체와 부딪히면 그 물체를 

        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        Vector3 endPoint = isHit ? hitInfo.point : ray.GetPoint(_range);
        ShowShotLine(endPoint);
        if (isHit)
        {
            // Debug.Log($"{hitInfo.collider.name}이 총알에 맞았다, 데미지: {_damage}");
            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; // 맞은 자리
            effect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            effect.Play();

            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;
            hitInfo.collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }

    private void ShowShotLine(Vector3 endPoint)
    {
        if (_shotLine == null) return;
        if (_lineCoroutine != null) StopCoroutine(_lineCoroutine);
        _shotLine.SetPosition(0, _lineOrigin.position);
        _shotLine.SetPosition(1, endPoint);
        _shotLine.enabled = true;
        _lineCoroutine = StartCoroutine(HideShotLine_Coroutine());
    }

    private IEnumerator HideShotLine_Coroutine()
    {
        yield return new WaitForSeconds(_lineDuration);
        _shotLine.enabled = false;
        _lineCoroutine = null;
    }

    private void OnDisable()
    {
        if (_lineCoroutine != null) StopCoroutine(_lineCoroutine);
        _lineCoroutine = null;
        if (_shotLine != null) _shotLine.enabled = false;
    }

    private void ShowMuzzleFlash()
    {
        if (_muzzleFlashes == null || _muzzleFlashes.Length == 0) return;
        int idx = UnityEngine.Random.Range(0, _muzzleFlashes.Length);
        GameObject flash = _muzzleFlashes[idx];
        if (flash == null) return;

        // 같은 인덱스에 진행 중인 코루틴이 있으면 먼저 중단한다.
        if (_muzzleCoroutines[idx] != null) StopCoroutine(_muzzleCoroutines[idx]);
        flash.SetActive(true);
        _muzzleCoroutines[idx] = StartCoroutine(HideMuzzleFlash_Coroutine(idx, flash));
    }

    private IEnumerator HideMuzzleFlash_Coroutine(int idx, GameObject flash)
    {
        yield return new WaitForSeconds(_floashTime);
        flash.SetActive(false);
        _muzzleCoroutines[idx] = null;
    }
}