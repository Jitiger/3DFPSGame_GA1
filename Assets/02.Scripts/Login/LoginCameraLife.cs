using UnityEngine;

// 로그인 화면 연출 전용. 고정 카메라에 아주 작은 손떨림과 느린 드리프트를 더해
// 정지 사진처럼 보이지 않게 한다. 게임 플레이 카메라와는 무관하다.
public class LoginCameraLife : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _shakeAmount = 0.035f;
    [SerializeField] private float _shakeSpeed = 0.5f;
    [SerializeField] private float _tiltAmount = 0.4f;
    [SerializeField] private float _driftAmount = 0.16f;
    [SerializeField] private float _driftSeconds = 26f;
    [SerializeField] private float _breathAmount = 0.05f;
    [SerializeField] private float _breathSeconds = 7f;

    private Vector3 _basePosition;
    private Quaternion _baseRotation;
    private float _seed;

    private void Awake()
    {
        _basePosition = transform.localPosition;
        _baseRotation = transform.localRotation;
        _seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        // 펄린 노이즈는 0~1이라 -0.5를 빼서 좌우로 고르게 흔들리게 만든다.
        float t = Time.time * _shakeSpeed;
        float nx = Mathf.PerlinNoise(_seed, t) - 0.5f;
        float ny = Mathf.PerlinNoise(_seed + 11f, t) - 0.5f;
        float nz = Mathf.PerlinNoise(_seed + 23f, t) - 0.5f;

        float drift = Mathf.Sin(Time.time * Mathf.PI * 2f / _driftSeconds);
        float breath = Mathf.Sin(Time.time * Mathf.PI * 2f / _breathSeconds);

        Vector3 offset = new Vector3(
            nx * _shakeAmount * 2f + drift * _driftAmount,
            ny * _shakeAmount * 2f + breath * _breathAmount,
            nz * _shakeAmount);

        transform.localPosition = _basePosition + offset;
        transform.localRotation = _baseRotation * Quaternion.Euler(
            ny * _tiltAmount,
            nx * _tiltAmount,
            nz * _tiltAmount * 0.6f);
    }
}
