using System.Collections;
using TMPro;
using UnityEngine;
using System;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;

    private int _remainingZombieCount = 0;
    private int _maxZombieCount = 0;

    public int RemainingZombieCount => _remainingZombieCount;
    public int MaxZombieCount => _maxZombieCount;

    public event Action OnZombieCountChanged;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);

        _state = GameState.Playing;
    }

    public void GameOver()
    {
        _state = GameState.GameOver;

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
    }

    public void RegisterZombie()
    {
        _remainingZombieCount++;
        _maxZombieCount++;

        OnZombieCountChanged?.Invoke();
    }

    public void HandleZombieDied()
    {
        _remainingZombieCount--;

        OnZombieCountChanged?.Invoke();
    }

}

