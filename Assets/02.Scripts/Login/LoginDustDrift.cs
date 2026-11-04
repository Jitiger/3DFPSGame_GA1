using UnityEngine;
using UnityEngine.UI;

// 로그인 화면 연출 전용. 먼지 텍스처의 UV를 계속 밀어서 재가 떠다니는 것처럼 보이게 한다.
// 파티클 시스템 없이 RawImage 한 장으로 처리한다.
public class LoginDustDrift : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RawImage _layer;

    [Header("스탯")]
    [SerializeField] private float _speedX = 0.012f;
    [SerializeField] private float _speedY = 0.02f;
    [SerializeField] private float _swayAmount = 0.012f;
    [SerializeField] private float _swaySeconds = 11f;

    private float _offsetX;
    private float _offsetY;

    private void Update()
    {
        if (_layer == null) return;

        _offsetX += _speedX * Time.deltaTime;
        _offsetY += _speedY * Time.deltaTime;

        float sway = Mathf.Sin(Time.time * Mathf.PI * 2f / _swaySeconds) * _swayAmount;

        Rect uv = _layer.uvRect;
        uv.x = Mathf.Repeat(_offsetX + sway, 1f);
        uv.y = Mathf.Repeat(_offsetY, 1f);
        _layer.uvRect = uv;
    }
}
