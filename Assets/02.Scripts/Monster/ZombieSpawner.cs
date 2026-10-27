using System;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform _spawnPoint;

    private void Start()
    {
        Instantiate(_zombiePrefab, _spawnPoint.position, _spawnPoint.rotation);
    }
}