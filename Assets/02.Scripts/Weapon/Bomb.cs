using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고, 풀에서 다시 사용할 수 있게 비활성화한다.
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _damage = 100f;

    private void OnCollisionEnter(Collision collision)
    {
        // 같은 물리 프레임에 여러 번 충돌해도 폭발은 한 번만 처리한다.
        if (gameObject.activeSelf == false)
        {
            return;
        }

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        ApplyAreaDamage();

        // 목록에는 그대로 남겨두고, 다음에 꺼낼 수 있게 비활성화한다.
        gameObject.SetActive(false);
    }

    private void ApplyAreaDamage()
    {
        // 폭발 범위 안의 드럼통과 좀비를 찾는다.
        Collider[] colliders = Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider col in colliders)
        {
            DamageInfo damageInfo = new DamageInfo
            {
                Value = _damage,
                HitPoint = col.ClosestPoint(transform.position),
                // 폭발 중심에서 가장 가까운 콜라이더 표면을 기준으로 방향을 구한다.
                HitNormal = (transform.position - col.ClosestPoint(transform.position)).normalized
            };
            col.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}
