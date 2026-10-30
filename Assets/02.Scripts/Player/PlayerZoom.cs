using UnityEngine;

// 저격 모드에서 마우스 오른쪽 버튼을 누르고 있는 동안 시야각 좁혀 확대한다.
public class PlayerZoom : MonoBehaviour
{
    private Player _player;

    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalPov = 60f;
    [SerializeField] private float _sniperPov = 15f;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        bool isZooming = GameManager.Instance.State == GameState.Playing
            && _player.WeaponMode == WeaponMode.Sniper && Input.GetMouseButton(1);
        _sniperZoom.SetActive(isZooming);
        _crosshair.SetActive(!isZooming);
        Camera.main.fieldOfView = isZooming ? _sniperPov : _normalPov;
    }
}
