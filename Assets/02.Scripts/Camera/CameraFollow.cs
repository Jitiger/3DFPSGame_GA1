using UnityEngine;

// 목표(Target)의 위치를 따라다니는 카메라
public class CameraFollow : MonoBehaviour
{
    public Transform Target; // 따라갈 대상 (Player의 CameraPosition)

    private void LateUpdate()
    {
        transform.position = Target.position;
    }
}
