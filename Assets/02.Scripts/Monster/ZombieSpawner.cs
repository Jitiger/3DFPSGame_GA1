using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform _spawnPoint;

    [Header("풀링")]
    [SerializeField, Min(1)] private int _poolSize = 10;
    [SerializeField, Min(1)] private int _maxAliveCount = 1;
    [SerializeField, Min(0.1f)] private float _spawnInterval = 3f;
    [SerializeField, Min(0.1f)] private float _spawnSearchRadius = 3f;

    private readonly List<Zombie> _pool = new List<Zombie>();
    private float _spawnTimer;

    public int PoolCount => _pool.Count;
    public int ActiveCount
    {
        get
        {
            int count = 0;
            foreach (Zombie zombie in _pool)
            {
                if (zombie.gameObject.activeSelf) count++;
            }
            return count;
        }
    }

    private void Start()
    {
        // 비활성 부모 아래에서 준비하면 미사용 좀비의 Start가 실행되지 않는다.
        GameObject poolRoot = new GameObject("ZombiePool");
        poolRoot.transform.SetParent(transform, false);
        poolRoot.SetActive(false);
        for (int i = 0; i < _poolSize; i++)
        {
            Zombie zombie = Instantiate(_zombiePrefab, poolRoot.transform);
            zombie.SetPool(this);
            zombie.gameObject.SetActive(false);
            _pool.Add(zombie);
        }
        poolRoot.SetActive(true);
        Spawn();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < _spawnInterval) return;
        _spawnTimer = 0f;
        if (ActiveCount < _maxAliveCount) Spawn();
    }

    public Zombie Spawn()
    {
        NavMeshHit hit;
        int areaMask = _zombiePrefab.GetComponent<NavMeshAgent>().areaMask;
        if (!NavMesh.SamplePosition(_spawnPoint.position, out hit, _spawnSearchRadius, areaMask))
        {
            Debug.LogWarning("좀비 생성 지점 주변에 NavMesh가 없습니다.", this);
            return null;
        }

        foreach (Zombie zombie in _pool)
        {
            if (zombie.gameObject.activeSelf) continue;
            return zombie.Spawn(hit.position, _spawnPoint.rotation) ? zombie : null;
        }
        return null;
    }

    public void Return(Zombie zombie)
    {
        if (!_pool.Contains(zombie) || !zombie.gameObject.activeSelf) return;
        zombie.gameObject.SetActive(false);
    }
}
