using UnityEngine;
using UnityEngine.UI;

// 로그인 화면 연출 전용. 단말기 수신 감도 막대를 불안정하게 오르내리게 한다.
public class LoginSignalBars : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Graphic[] _bars;

    [Header("스탯")]
    [SerializeField] private float _stepSeconds = 0.6f;
    [SerializeField] private float _onAlpha = 1f;
    [SerializeField] private float _offAlpha = 0.16f;
    [SerializeField] private int _minLevel = 2;

    private float _timer;

    private void Start()
    {
        Apply(_bars == null ? 0 : _bars.Length);
    }

    private void Update()
    {
        if (_bars == null || _bars.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < _stepSeconds) return;

        _timer = 0f;
        Apply(Random.Range(_minLevel, _bars.Length + 1));
    }

    private void Apply(int level)
    {
        if (_bars == null) return;

        for (int i = 0; i < _bars.Length; i++)
        {
            Graphic bar = _bars[i];
            if (bar == null) continue;

            Color color = bar.color;
            color.a = i < level ? _onAlpha : _offAlpha;
            bar.color = color;
        }
    }
}
