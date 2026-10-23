using System.Collections.Generic;
using UnityEngine;

// 마우스 오른쪽 버튼을 누르는 동안 힘을 모으고, 손을 떼면 폭탄을 던진다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private int _maxBombCount = 10;
    [SerializeField, Min(1)] private int _poolSize = 10;

    private int _bombCount = 0;
    private readonly List<Bomb> _bombPool = new List<Bomb>();

    private void Awake()
    {
        // 시작할 때 폭탄을 미리 만들고, 비활성화한 채 목록에 보관한다.
        for (int i = 0; i < _poolSize; i++)
        {
            Bomb bomb = Instantiate(_bombPrefab, _firePoint.position, Quaternion.identity);
            bomb.gameObject.SetActive(false);
            _bombPool.Add(bomb);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (_bombCount < _maxBombCount)
        {
            if (Input.GetButton("Fire2"))
            {
                _throwPower += 10f * Time.deltaTime;
            }

            if (Input.GetButtonUp("Fire2"))
            {
                Shoot();
                _throwPower = 15f;
            }
        }
    }

    private Bomb GetBomb()
    {
        // 목록에서 사용 중이 아닌 폭탄을 찾아 돌려준다.
        foreach (Bomb bomb in _bombPool)
        {
            if (bomb.gameObject.activeSelf == false)
            {
                return bomb;
            }
        }

        // 모든 폭탄이 사용 중이면 이번에는 던지지 않는다.
        return null;
    }

    private void Shoot()
    {
        // 2. 새로 생성하는 대신, 풀에 보관한 폭탄을 가져온다.
        Bomb bomb = GetBomb();
        if (bomb == null)
        {
            Debug.LogWarning("사용 가능한 폭탄이 없습니다.");
            return;
        }

        bomb.transform.SetPositionAndRotation(_firePoint.position, Quaternion.identity);
        bomb.gameObject.SetActive(true);

        // 3. 이전 사용 때의 이동 속도와 회전 속도를 지우고 다시 던진다.
        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);

        // 풀로 반환되어도 사용 횟수는 되돌리지 않는다.
        _bombCount++;
    }
}