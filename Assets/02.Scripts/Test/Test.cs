// 수업용 C# 문법 예제. 빌드에는 포함하지 않는다.
#if UNITY_EDITOR
using UnityEngine;

public class Test : MonoBehaviour
{
    private int _score = 0;

    private void Start()
    {
        Debug.Log(_score);
    }
}
#endif
