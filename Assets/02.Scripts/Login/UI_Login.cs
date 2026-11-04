using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UI_Login : MonoBehaviour
{
    private const string LoadingLabel = "LOADING";
    private const string ConnectedLabel = "CONNECTED";
    private const string ConnectedStatus = "LINK SECURE";
    private const string ConnectedHint = "CONNECTION ESTABLISHED";
    private const string SyncingHint = "SYNCING SURVIVOR PROFILE";
    private const string GrantedStatus = "ACCESS GRANTED";
    private const float DotInterval = 0.3f;
    private const int DotCycle = 3;
    private const float LoadingBarWidth = 240f;
    private const float CaretInterval = 0.5f;

    // 아이디와 비밀번호 규칙은 Specification 클래스가 들고 있다.
    // 규칙을 바꿀 때 이 화면을 건드리지 않도록 판정과 안내 문구를 모두 그쪽에 맡긴다.
    private static readonly AccountIdSpecification IdSpecification = new AccountIdSpecification();
    private static readonly AccountPasswordSpecification PasswordSpecification = new AccountPasswordSpecification();

    // 부팅 로그. 한 글자씩 찍히다가 끝나면 로그인 폼이 올라온다.
    private static readonly string[] BootLines =
    {
        "> ZS-7 FIELD TERMINAL",
        "> LINKING SATELLITE . . .",
        "> SIGNAL ACQUIRED   62%",
        "> SECTOR 7 - QUARANTINE",
        "> AWAITING CREDENTIALS",
    };

    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _registerPanel;
    [SerializeField] private TMP_InputField _loginId;
    [SerializeField] private TMP_InputField _loginPassword;
    [SerializeField] private TMP_InputField _registerId;
    [SerializeField] private TMP_InputField _registerPassword;
    [SerializeField] private TMP_InputField _registerConfirmation;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private GameObject _bootOverlay;
    [SerializeField] private TMP_Text _bootText;
    [SerializeField] private GameObject _loadingOverlay;
    [SerializeField] private TMP_Text _loadingStatus;
    [SerializeField] private TMP_Text _loadingText;
    [SerializeField] private TMP_Text _loadingHint;
    [SerializeField] private RectTransform _loadingBarFill;
    [SerializeField] private string _gameSceneName = "GameScenes";
    [SerializeField] private bool _loadGameSceneAfterLoading;

    [Header("시연용 계정")]
    // 발표·녹화용으로 미리 열어둔 계정. 비워두면 저장된 계정만 로그인된다.
    [SerializeField] private string _demoId = "sindo";
    [SerializeField] private string _demoPassword = "sindo";

    [Header("스탯")]
    [SerializeField] private float _loadingSeconds = 3f;
    [SerializeField] private float _bootCharSeconds = 0.016f;
    [SerializeField] private float _bootHoldSeconds = 0.55f;


    private bool _isLoading;

    // Tab으로 넘어갈 순서. 패널마다 따로 둔다.
    private TMP_InputField[] _loginFields;
    private TMP_InputField[] _registerFields;

    private void Awake()
    {

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _loginFields = new[] { _loginId, _loginPassword };
        _registerFields = new[] { _registerId, _registerPassword, _registerConfirmation };
        HideOverlays();
    }

    private void Update()
    {
        if (_isLoading) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        // 유니티는 입력칸 사이 Tab 이동을 자동으로 해주지 않아서 직접 옮긴다.
        bool isRegister = _registerPanel.activeInHierarchy;
        if (!isRegister && !_loginPanel.activeInHierarchy) return;

        bool backward = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        MoveFocus(isRegister ? _registerFields : _loginFields, backward);
    }

    private void Start()
    {
        if (_bootOverlay == null || _bootText == null)
        {
            ShowLogin();
            return;
        }
        StartCoroutine(BootRoutine());
    }

    public void ShowLogin()
    {
        ClearPasswords();
        _loginPanel.SetActive(true);
        _registerPanel.SetActive(false);
        _messageText.text = "";
    }

    public void ShowRegister()
    {
        ClearPasswords();
        _loginPanel.SetActive(false);
        _registerPanel.SetActive(true);
        _messageText.text = "";
    }

    public void OnClickRegister()
    {
        try
        {
            string id = _registerId.text.Trim();
            string password = _registerPassword.text;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
            {
                _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
                return;
            }
            if (!IdSpecification.IsSatisfiedBy(id))
            {
                _messageText.text = IdSpecification.ErrorMessage;
                return;
            }
            if (!PasswordSpecification.IsSatisfiedBy(password))
            {
                _messageText.text = PasswordSpecification.ErrorMessage;
                return;
            }
            if (password != _registerConfirmation.text)
            {
                _messageText.text = "비밀번호 확인이 맞지 않습니다.";
                return;
            }
            if (AccountStorage.Exists(id))
            {
                _messageText.text = "이미 있는 아이디입니다.";
                return;
            }
            AccountStorage.Register(id, password);
            _loginId.text = id;
            ShowLogin();
            _messageText.text = "가입했습니다. 로그인해 주세요.";
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            _messageText.text = "계정 파일을 저장하지 못했습니다. 저장 위치를 확인해주세요.";
        }
    }


    public void OnClickLogin()
    {
        // 로딩 연출이 도는 동안 버튼을 다시 눌러도 코루틴이 겹치지 않게 막는다.
        if (_isLoading) return;

        if (string.IsNullOrWhiteSpace(_loginId.text) || string.IsNullOrEmpty(_loginPassword.text))
        {
            _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }

        try
        {
            // 시연용 계정은 저장된 계정을 거치지 않고 바로 접속 연출로 넘어간다.
            if (!IsDemoAccount(_loginId.text, _loginPassword.text)
                && !AccountStorage.CheckPassword(_loginId.text.Trim(), _loginPassword.text))
            {
                _messageText.text = "아이디 또는 비밀번호가 맞지 않습니다.";
                return;
            }
            if (_loadGameSceneAfterLoading && !Application.CanStreamedLevelBeLoaded(_gameSceneName))
            {
                _messageText.text = "게임 씬이 Build Settings에 등록되지 않았습니다.";
                return;
            }
            ClearPasswords();
            _messageText.text = "";
            StartCoroutine(ConnectRoutine());
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is FormatException)
        {
            _messageText.text = "계정 파일을 읽지 못했습니다. 저장 파일을 확인해주세요.";
        }
    }

    // 지금 잡고 있는 입력칸의 다음(Shift면 이전) 칸으로 커서를 옮긴다. 끝에서는 처음으로 돈다.
    private static void MoveFocus(TMP_InputField[] fields, bool backward)
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        int current = -1;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == null || fields[i].gameObject != selected) continue;

            current = i;
            break;
        }

        // 아무 칸도 잡고 있지 않으면 첫 칸(Shift면 마지막 칸)부터 시작한다.
        int next = current < 0
            ? (backward ? fields.Length - 1 : 0)
            : (current + (backward ? -1 : 1) + fields.Length) % fields.Length;

        TMP_InputField field = fields[next];
        if (field == null) return;

        EventSystem.current.SetSelectedGameObject(field.gameObject);
        field.ActivateInputField();
    }

    private bool IsDemoAccount(string id, string password)
    {
        if (string.IsNullOrEmpty(_demoId)) return false;

        return id.Trim() == _demoId && password == _demoPassword;
    }

    // 단말기가 막 켜진 것처럼 부팅 로그를 한 글자씩 찍고 나서 로그인 폼을 띄운다.
    private IEnumerator BootRoutine()
    {
        _loginPanel.SetActive(false);
        _registerPanel.SetActive(false);
        _messageText.text = "";
        _bootOverlay.SetActive(true);

        string full = string.Join("\n", BootLines);
        StringBuilder builder = new StringBuilder(full.Length + 1);
        int shown = 0;
        float timer = 0f;

        while (shown < full.Length)
        {
            timer += Time.deltaTime;
            while (timer >= _bootCharSeconds && shown < full.Length)
            {
                timer -= _bootCharSeconds;
                shown++;
            }

            builder.Length = 0;
            builder.Append(full, 0, shown);
            if (Mathf.Repeat(Time.time, CaretInterval * 2f) < CaretInterval)
            {
                builder.Append('_');
            }
            _bootText.text = builder.ToString();
            yield return null;
        }

        _bootText.text = full;
        yield return new WaitForSeconds(_bootHoldSeconds);

        _bootOverlay.SetActive(false);
        ShowLogin();
    }

    // 인증에 성공하면 단말기 화면을 로딩 표시로 덮고, 정해진 시간이 지나면 접속 완료를 띄운다.
    private IEnumerator ConnectRoutine()
    {
        _isLoading = true;
        if (_loadingStatus != null)
        {
            _loadingStatus.text = GrantedStatus;
        }
        if (_loadingHint != null)
        {
            _loadingHint.text = SyncingHint;
        }
        if (_loadingOverlay != null)
        {
            _loadingOverlay.SetActive(true);
        }

        float elapsed = 0f;
        while (elapsed < _loadingSeconds)
        {
            elapsed += Time.deltaTime;

            if (_loadingText != null)
            {
                int dotCount = 1 + (Mathf.FloorToInt(elapsed / DotInterval) % DotCycle);
                _loadingText.text = LoadingLabel + " " + new string('.', dotCount);
            }

            if (_loadingBarFill != null)
            {
                Vector2 size = _loadingBarFill.sizeDelta;
                size.x = LoadingBarWidth * Mathf.Clamp01(elapsed / _loadingSeconds);
                _loadingBarFill.sizeDelta = size;
            }

            yield return null;
        }

        if (_loadGameSceneAfterLoading)
        {
            SceneManager.LoadScene(_gameSceneName);
            yield break;
        }

        // 씬 전환을 꺼둔 경우에는 접속 완료 화면에서 멈춘다.
        if (_loadingText != null)
        {
            _loadingText.text = ConnectedLabel;
        }
        if (_loadingStatus != null)
        {
            _loadingStatus.text = ConnectedStatus;
        }
        if (_loadingHint != null)
        {
            _loadingHint.text = ConnectedHint;
        }
        if (_loadingBarFill != null)
        {
            Vector2 size = _loadingBarFill.sizeDelta;
            size.x = LoadingBarWidth;
            _loadingBarFill.sizeDelta = size;
        }
    }

    private void HideOverlays()
    {
        _isLoading = false;
        if (_loadingOverlay != null)
        {
            _loadingOverlay.SetActive(false);
        }
        if (_bootOverlay != null)
        {
            _bootOverlay.SetActive(false);
        }
        if (_loadingBarFill != null)
        {
            Vector2 size = _loadingBarFill.sizeDelta;
            size.x = 0f;
            _loadingBarFill.sizeDelta = size;
        }
    }

    private void ClearPasswords()
    {
        _loginPassword.text = "";
        _registerPassword.text = "";
        _registerConfirmation.text = "";
    }
}



