using UnityEngine;

// 로그인 화면 연출 전용. 비상등처럼 라이트 세기를 깜빡이게 한다.
// 켜짐/꺼짐을 바로 바꾸지 않고 목표 세기로 당겨서, 전구가 달아올랐다 식는 느낌을 준다.
[RequireComponent(typeof(Light))]
public class LoginBlinkLight : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _onDuration = 0.5f;
    [SerializeField] private float _offDuration = 0.85f;
    [SerializeField] private float _onIntensity = 9f;
    [SerializeField] private float _offIntensity = 0.5f;
    [SerializeField] private float _fadeSpeed = 26f;
    [SerializeField] private float _startDelay = 0f;

    private Light _light;
    private float _timer;
    private bool _isOn;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _timer = -_startDelay;
        _light.intensity = _offIntensity;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        float duration = _isOn ? _onDuration : _offDuration;
        if (_timer >= duration)
        {
            _timer -= duration;
            _isOn = !_isOn;
        }

        float target = _isOn ? _onIntensity : _offIntensity;
        _light.intensity = Mathf.MoveTowards(_light.intensity, target, _fadeSpeed * Time.deltaTime);
    }
}
