using UnityEngine;

// 로그인 화면 연출 전용. 단말기 화면 위쪽 상태 문구를 왼쪽으로 흘려보낸다.
// 부모에 RectMask2D가 붙어 있어 화면 밖으로 나간 글자는 잘린다.
public class LoginTicker : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform _content;

    [Header("스탯")]
    [SerializeField] private float _speed = 26f;
    [SerializeField] private float _startX = 170f;
    [SerializeField] private float _resetX = -900f;

    private void Update()
    {
        if (_content == null) return;

        Vector2 position = _content.anchoredPosition;
        position.x -= _speed * Time.deltaTime;
        if (position.x <= _resetX)
        {
            position.x = _startX;
        }
        _content.anchoredPosition = position;
    }
}
