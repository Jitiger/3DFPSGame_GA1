using UnityEngine;

// 파티클 재생이 완료되면 게임오브젝트를 파괴하거나 비활성화한다.
public class AutoDestroyParticle : MonoBehaviour
{
    public bool OnlyDeactivate = false;

    private ParticleSystem _ps;

    private void Start()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (_ps != null && !_ps.IsAlive(true))
        {
            if (OnlyDeactivate)
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }
    }
}
