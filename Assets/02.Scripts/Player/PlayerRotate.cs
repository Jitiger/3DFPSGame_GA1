using UnityEngine;

// 마우스를 좌우로 움직이면 플레이어 몸을 좌우로 회전시킨다.
public class PlayerRotate : MonoBehaviour
{
    public float RotationSpeed = 200f; // CameraRotate와 같은 값으로 맞춘다.

    private float _accumulationX = 0f;

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        _accumulationX += mouseX * RotationSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(0f, _accumulationX, 0f);
    }
}
