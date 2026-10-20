using UnityEngine;
using UnityEngine.UI;

// 플레이어의 현재 스태미나를 바와 숫자로 표시한다. 
public class StaminaUI : MonoBehaviour
{
    [SerializeField] private PlayerMove _player;
    [SerializeField] private Slider _staminaBar;
    [SerializeField] private Text _staminaText;

    private void Awake()
    {
        if (_player == null || _staminaBar == null || _staminaText == null)
        {
            Debug.LogError("StaminaUI의 플레이어, 바, 텍스트 연결을 확인하세요.", this);
            enabled = false;
            return;
        }

        _staminaBar.minValue = 0f;
        _staminaBar.maxValue = _player.MaximumStamina;
    }

    private void LateUpdate()
    {
        _staminaBar.value = _player.CurrentStamina;
        _staminaText.text = $"STAMINA  {Mathf.CeilToInt(_player.CurrentStamina)} / {Mathf.RoundToInt(_player.MaximumStamina)}";
    }
}
