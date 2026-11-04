using UnityEngine;

// 로그인 화면 연출 전용. 배경에 세워둔 좀비들을 카메라 쪽으로 천천히 걸어오게 하고,
// 카메라 앞까지 오면 출발 지점으로 되돌려 같은 행렬을 반복한다.
// 게임 플레이용 Zombie FSM과는 무관하며, 이 씬에서는 Zombie 스크립트를 꺼둔 상태로 쓴다.
public class LoginZombieParade : MonoBehaviour
{
    // ZombieAC의 Move 상태는 달리기(Z_Run_InPlace)라서 연출에 맞지 않는다.
    // 같은 컨트롤러에 들어 있는 걷기 상태를 직접 재생해 천천히 다가오게 한다.
    private const string WalkStateName = "Z_Walk_InPlace";

    [Header("참조")]
    [SerializeField] private Transform[] _zombies;
    [SerializeField] private Transform _camera;

    [Header("스탯")]
    [SerializeField] private float _speed = 0.75f;
    [SerializeField] private float _resetDistance = 4f;
    [SerializeField] private float _walkCycleSpeed = 1f;
    [SerializeField] private float _speedVariation = 0.3f;

    private Vector3[] _startPositions;
    private float[] _speeds;

    private void Start()
    {
        if (_zombies == null) return;

        _startPositions = new Vector3[_zombies.Length];
        _speeds = new float[_zombies.Length];

        for (int i = 0; i < _zombies.Length; i++)
        {
            Transform zombie = _zombies[i];
            if (zombie == null) continue;

            _startPositions[i] = zombie.position;

            // 전부 같은 속도로 오면 행렬이 기계처럼 보여서 개체마다 조금씩 어긋나게 둔다.
            float scale = Random.Range(1f - _speedVariation, 1f + _speedVariation);
            _speeds[i] = _speed * scale;

            // 좀비 모델의 Animator는 자식(모델 프리팹)에 붙어 있다.
            Animator animator = zombie.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.speed = _walkCycleSpeed * scale;

                // 좀비마다 걷기 사이클의 시작 지점을 다르게 줘서 발걸음이 똑같이 겹쳐 보이지 않게 한다.
                animator.Play(WalkStateName, 0, Random.value);
            }
        }
    }

    private void Update()
    {
        if (_zombies == null || _startPositions == null || _speeds == null) return;

        for (int i = 0; i < _zombies.Length; i++)
        {
            Transform zombie = _zombies[i];
            if (zombie == null) continue;

            zombie.Translate(Vector3.forward * (_speeds[i] * Time.deltaTime), Space.Self);

            if (_camera == null) continue;
            if (Vector3.Distance(zombie.position, _camera.position) <= _resetDistance)
            {
                zombie.position = _startPositions[i];
            }
        }
    }
}
