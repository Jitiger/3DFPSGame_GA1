using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;
    [SerializeField] private float _fadeTime = 0.3f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
    }

    public void Flash(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;

        StartCoroutine(Flash_Coroutine());
    }

    public IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;
        yield return new WaitForSeconds(_flashTime);
        _image.enabled = false;
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);

        StartCoroutine(Fade_Coroutine());
        _image.enabled = true;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }

    private IEnumerator Fade_Coroutine()
    {
        float elapsedTime = 0f;

        float startAlpha = _image.color.a;

        while (elapsedTime < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / _fadeTime);
            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;
            yield return null; // 한 프레임씩 쉰다!
        }

        SetAlpha(0f);
        _image.enabled = false;
    }

    private void Start()
    {
        Player player = GameObject.FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Flash(1f);
    }
}