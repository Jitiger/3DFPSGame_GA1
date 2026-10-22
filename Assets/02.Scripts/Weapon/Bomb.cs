using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고, 풀에서 다시 사용할 수 있게 비활성화한다.
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        // 같은 물리 프레임에 여러 번 충돌해도 폭발은 한 번만 처리한다.
        if (gameObject.activeSelf == false)
        {
            return;
        }

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // 목록에는 그대로 남겨두고, 다음에 꺼낼 수 있게 비활성화한다.
        gameObject.SetActive(false);
    }
}
