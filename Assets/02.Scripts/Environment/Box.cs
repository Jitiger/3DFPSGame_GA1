using UnityEngine;

// 체력이 다하면 원래 메쉬를 나눈 파편으로 교체한다.
public class Box : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject _brokenBoxPrefab;

    [Header("스탯")]
    [SerializeField] private float _health = 10f;

    [Header("파편")]
    [SerializeField] private float _breakForce = 3f;
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private float _upwardsModifier = 0.2f;
    [SerializeField] private float _fragmentLifetime = 3f;

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damageInfo.Value;
        if (_health > 0f)
        {
            return;
        }

        if (_brokenBoxPrefab != null)
        {
            GameObject fragments = Instantiate(_brokenBoxPrefab, transform.position, transform.rotation);
            fragments.transform.localScale = transform.lossyScale;

            foreach (Rigidbody body in fragments.GetComponentsInChildren<Rigidbody>())
            {
                body.AddExplosionForce(_breakForce, damageInfo.HitPoint, _explosionRadius, _upwardsModifier, ForceMode.Impulse);
            }

            Destroy(fragments, _fragmentLifetime);
        }

        Destroy(gameObject);
    }
}
