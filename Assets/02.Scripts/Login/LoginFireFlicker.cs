using UnityEngine;

// 로그인 화면 연출 전용. 복도에 놓인 불빛을 모닥불처럼 불규칙하게 일렁이게 한다.
[RequireComponent(typeof(Light))]
public class LoginFireFlicker : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _flickerAmount = 0.35f;
    [SerializeField] private float _flickerSpeed = 2.4f;
    [SerializeField] private float _positionJitter = 0.1f;

    private Light _light;
    private float _baseIntensity;
    private Vector3 _basePosition;
    private float _seed;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
        _basePosition = transform.localPosition;
        _seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        // 느린 일렁임과 빠른 떨림을 섞어야 불꽃처럼 보인다.
        float slow = Mathf.PerlinNoise(_seed, Time.time * _flickerSpeed);
        float fast = Mathf.PerlinNoise(_seed + 7f, Time.time * _flickerSpeed * 3.1f);
        float mix = slow * 0.7f + fast * 0.3f;

        _light.intensity = _baseIntensity * Mathf.Lerp(1f - _flickerAmount, 1f + _flickerAmount * 0.35f, mix);
        transform.localPosition = _basePosition + new Vector3(slow - 0.5f, fast - 0.5f, mix - 0.5f) * _positionJitter;
    }
}
