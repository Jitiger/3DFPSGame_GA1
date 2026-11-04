using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 로그인 화면 연출 전용. 타이틀을 가끔 한 번씩 옆으로 찢고 색을 어긋나게 해
// 오래된 송출 화면 같은 글리치를 만든다.
public class LoginGlitch : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform _target;
    [SerializeField] private Graphic _redGhost;
    [SerializeField] private Graphic _cyanGhost;

    [Header("스탯")]
    [SerializeField] private float _minInterval = 2.5f;
    [SerializeField] private float _maxInterval = 7f;
    [SerializeField] private float _burstSeconds = 0.16f;
    [SerializeField] private float _stepSeconds = 0.03f;
    [SerializeField] private float _offsetAmount = 16f;
    [SerializeField] private float _ghostSpread = 8f;
    [SerializeField] private float _ghostAlpha = 0.55f;

    private Vector2 _basePosition;

    private void Awake()
    {
        if (_target == null)
        {
            _target = GetComponent<RectTransform>();
        }
        _basePosition = _target.anchoredPosition;
    }

    private void Start()
    {
        SetGhost(_redGhost, Vector2.zero, 0f);
        SetGhost(_cyanGhost, Vector2.zero, 0f);
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        WaitForSeconds step = new WaitForSeconds(_stepSeconds);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_minInterval, _maxInterval));

            float endTime = Time.time + _burstSeconds;
            while (Time.time < endTime)
            {
                float slide = Random.Range(-_offsetAmount, _offsetAmount);
                _target.anchoredPosition = _basePosition + new Vector2(slide, Random.Range(-3f, 3f));
                SetGhost(_redGhost, new Vector2(-_ghostSpread, 2f), _ghostAlpha);
                SetGhost(_cyanGhost, new Vector2(_ghostSpread, -2f), _ghostAlpha);
                yield return step;
            }

            _target.anchoredPosition = _basePosition;
            SetGhost(_redGhost, Vector2.zero, 0f);
            SetGhost(_cyanGhost, Vector2.zero, 0f);
        }
    }

    private void SetGhost(Graphic ghost, Vector2 offset, float alpha)
    {
        if (ghost == null) return;

        ghost.rectTransform.anchoredPosition = offset;
        Color color = ghost.color;
        color.a = alpha;
        ghost.color = color;
    }
}
