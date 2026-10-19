using UnityEngine;

// 마우스를 움직이면 카메라를 그 방향으로 회전시킨다.
public class CameraRotate : MonoBehaviour
{
    public float RotationSpeed = 200f; // 마우스 감도 (1초에 도는 각도)

    // 회전값을 직접 누적할 변수 (eulerAngles는 0~360으로 바뀌기 때문)
    private float _accumulationX = 0f; // 좌우 회전(Y축)
    private float _accumulationY = 0f; // 위아래 회전(X축)

    private void Update()
    {
        // 1. 마우스 입력을 받는다.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2. 입력만큼 회전값을 누적한다. 마우스를 올리면 위를 보도록 Y는 부호를 뒤집는다.
        _accumulationX += mouseX * RotationSpeed * Time.deltaTime;
        _accumulationY += -mouseY * RotationSpeed * Time.deltaTime;

        // 3. 카메라에 넣기 전에, 내 변수에서 먼저 -90 ~ 90도로 제한한다.
        _accumulationY = Mathf.Clamp(_accumulationY, -90f, 90f);

        // 4. 누적한 각도로 카메라를 회전시킨다.
        transform.eulerAngles = new Vector3(_accumulationY, _accumulationX, 0f);
    }
}
