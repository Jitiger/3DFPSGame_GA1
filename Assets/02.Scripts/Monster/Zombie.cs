using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - "상태": 객체가 가질 수 있는 어떤 조건이나 상황
// - "전이": 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기: 제자리에서 플레이어를 기다린다.
    Trace, // 추격: 플레이어를 쫓아간다.
    Comeback, // 복귀: 처음 자리로 돌아간다.
    Attack, // 공격: 일정 시간마다 플레이어를 때린다.
    Hit, // 피격: 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death, // 죽음: 쓰러지고 잠시 뒤 사라진다.
    Patrol, // 순찰: 처음 자리 주변의 두 지점을 오간다.
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _detectDistance = 8; // 플레이어를 발견하는 거리
    [UnityEngine.Serialization.FormerlySerializedAs("_combackDistance")]
    [SerializeField]
    private float _comebackDistance = 15; // 처음 자리에서 이만큼 멀어지면 돌아가는 거리
    [SerializeField] private float _attackRange = 1.5f; // 공격 범위
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5; // 이동속도
    [SerializeField] private float _maxHealth = 30;
    [SerializeField] private float _health;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    [Header("순찰")]
    [SerializeField, Min(0.1f)] private float _patrolDistance = 3f;
    [SerializeField, Min(0f)] private float _patrolWaitTime = 1.5f;
    [SerializeField, Min(0.01f)] private float _patrolArrivalDistance = 0.3f;

    [Header("점프")]
    [SerializeField, Min(0.01f)] private float _jumpDuration = 0.8f;
    [SerializeField, Min(0f)] private float _jumpHeight = 1.5f;

    private Coroutine _jumpCoroutine;
    private bool _isJumping;
    private bool _savedUpdatePosition;
    private bool _savedUpdateRotation;
    private Vector3 _jumpEndPosition;

    private Vector3 _startPosition;
    private GameObject _player;
    private NavMeshAgent _agent;
    private Animator _animator;
    private Dictionary<ZombieState, ZombieStateBase> _states;
    private ZombieStateBase _currentState;
    private ZombieState _state = ZombieState.Idle;
    private ZombieSpawner _pool;
    private bool _isCounted;

    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public ZombieState State => _state;

    public event Action OnDamaged;
    public event Action OnDied;

    // 상태 객체가 필요한 값만 읽도록 제공한다. Inspector 필드는 그대로 유지한다.
    internal float DetectDistance => _detectDistance;
    internal float ComebackDistance => _comebackDistance;
    internal float AttackRange => _attackRange;
    internal float AttackInterval => _attackInterval;
    internal float PatrolDistance => _patrolDistance;
    internal float PatrolWaitTime => _patrolWaitTime;
    internal float PatrolArrivalDistance => _patrolArrivalDistance;
    internal Vector3 StartPosition => _startPosition;
    internal Vector3 PlayerPosition => _player.transform.position;
    internal NavMeshAgent Agent => _agent;
    internal Animator Animator => _animator;
    internal bool IsJumping => _isJumping;

    private void Awake()
    {
        _health = _maxHealth;
    }

    private void Start()
    {
        // 풀에서 먼저 꺼낸 경우에는 Spawn에서 이미 초기화했다.
        if (_currentState == null) BeginLife();
    }

    private void BeginLife()
    {
        _health = _maxHealth;
        _state = ZombieState.Idle;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;
        _agent.autoTraverseOffMeshLink = false;
        _animator = GetComponentInChildren<Animator>();
        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");

        // 상태 객체도 첫 생성 때만 만들고 다음 생명에서 재사용한다.
        if (_states == null)
        {
            _states = new Dictionary<ZombieState, ZombieStateBase>
            {
                { ZombieState.Idle, new ZombieIdleState(this) },
                { ZombieState.Patrol, new ZombiePatrolState(this) },
                { ZombieState.Trace, new ZombieTraceState(this) },
                { ZombieState.Comeback, new ZombieComebackState(this) },
                { ZombieState.Attack, new ZombieAttackState(this) },
                { ZombieState.Hit, new ZombieHitState(this) },
                { ZombieState.Death, new ZombieDeathState(this) }
            };
        }
        ((ZombiePatrolState)_states[ZombieState.Patrol]).ResetPatrol();

        _animator.Rebind();
        _animator.Update(0f);
        StopAgent();
        _currentState = _states[_state];
        _currentState.Enter();

        GameManager.Instance.RegisterZombie();
        _isCounted = true;
        OnDamaged?.Invoke();
    }

    internal void SetPool(ZombieSpawner pool)
    {
        _pool = pool;
    }

    internal bool Spawn(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
        _agent = GetComponent<NavMeshAgent>();
        if (!_agent.Warp(position))
        {
            gameObject.SetActive(false);
            return false;
        }

        BeginLife();
        return true;
    }

    internal void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Return(this);
        }
        else
        {
            // 씬에 직접 배치한 좀비는 기존처럼 제거한다.
            Destroy(gameObject);
        }
    }

    private void UnregisterLivingZombie()
    {
        if (!_isCounted) return;
        _isCounted = false;
        if (GameManager.Instance != null) GameManager.Instance.HandleZombieDied();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_isJumping) return;

        if (_currentState.CanJump && _agent.isOnNavMesh && _agent.isOnOffMeshLink)
        {
            _jumpCoroutine = StartCoroutine(Jump_Coroutine());
            return;
        }

        _currentState.Update();
    }

    internal void ChangeState(ZombieState nextState, string animationTrigger = null)
    {
        // 이전 상태를 정리한 다음 새 상태에 들어간다. 재피격도 다시 Enter한다.
        _currentState.Exit();
        Debug.Log($"{_state} -> {nextState}");
        _state = nextState;
        _currentState = _states[nextState];

        if (animationTrigger != null)
        {
            _animator.SetTrigger(animationTrigger);
        }

        _currentState.Enter();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death) return;

        _health -= damageInfo.Value;
        OnDamaged?.Invoke();

        if (_health > 0f)
        {
            GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            ChangeState(ZombieState.Hit);
        }
        else
        {
            ChangeState(ZombieState.Death);
            UnregisterLivingZombie();
            OnDied?.Invoke();
        }
    }

    private IEnumerator Jump_Coroutine()
    {
        _isJumping = true;
        Vector3 startPosition = transform.position;
        _jumpEndPosition = _agent.currentOffMeshLinkData.endPos + Vector3.up * _agent.baseOffset;
        _savedUpdatePosition = _agent.updatePosition;
        _savedUpdateRotation = _agent.updateRotation;
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        Vector3 direction = _jumpEndPosition - startPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.forward = direction.normalized;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _jumpDuration);
        while (elapsed < duration)
        {
            if (GameManager.Instance.State != GameState.Playing)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float height = 4f * _jumpHeight * t * (1f - t);
            transform.position = Vector3.Lerp(startPosition, _jumpEndPosition, t) + Vector3.up * height;
            yield return null;
        }

        FinishJump();
    }

    private void FinishJump()
    {
        transform.position = _jumpEndPosition;
        if (_agent.enabled && _agent.isOnNavMesh)
        {
            if (_agent.isOnOffMeshLink) _agent.CompleteOffMeshLink();
            _agent.nextPosition = _jumpEndPosition;
        }

        _agent.updatePosition = _savedUpdatePosition;
        _agent.updateRotation = _savedUpdateRotation;
        _isJumping = false;
        _jumpCoroutine = null;
    }

    private void OnEnable()
    {
        // Start 이전에는 상태가 없다. 재활성화 시에는 반응도 다시 시작한다.
        if (_pool == null) _currentState?.Enter();
    }

    private void OnDisable()
    {
        _currentState?.Exit();
        if (_isJumping)
        {
            if (_jumpCoroutine != null) StopCoroutine(_jumpCoroutine);
            FinishJump();
        }
        if (_pool != null) UnregisterLivingZombie();
    }

    public void AttackAction()
    {
        // 피격·사망으로 공격을 떠난 뒤 도착한 애니메이션 이벤트는 무시한다.
        if (_state != ZombieState.Attack) return;

        DamageInfo info = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized,
        };

        _player.GetComponent<IDamageable>().TakeDamage(info);
    }

    internal void StopAgent()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.ResetPath(); // 현재 경로를 지운다.
        _agent.velocity = Vector3.zero; // 가속도가 아니라 현재 속도를 0으로 만든다.
    }
}
