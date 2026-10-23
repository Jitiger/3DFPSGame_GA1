using UnityEngine;

// 공격력과 맞은 위치, 표면 방향을 함께 전달한다.
public struct DamageInfo
{
    public float Value;
    public Vector3 HitPoint;
    public Vector3 HitNormal;
}
