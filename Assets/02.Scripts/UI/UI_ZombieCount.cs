using TMPro;
using UnityEngine;

public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;

    private void Start()
    {
        GameManager.Instance.OnZombieCountChanged += RefreshCount;
        RefreshCount();
    }

    private void RefreshCount()
    {
        _countText.text =
            $"남은 좀비: {GameManager.Instance.RemainingZombieCount} / {GameManager.Instance.MaxZombieCount}";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnZombieCountChanged -= RefreshCount;
        }
    }
}