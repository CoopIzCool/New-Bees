using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct AngerJob : IJobParallelFor
{
    public NativeArray<int> targetIndex;
    public NativeArray<bool> dead;
    [ReadOnly] public NativeArray<float3> beePosition;
    public NativeArray<float3> beeVelocities;
    public NativeArray<bool> isAttacking;
    public NativeArray<bool> isActive;
    [ReadOnly] public float attackDistance;
    [ReadOnly] public float chaseForce;
    [ReadOnly] public float attackForce;
    [ReadOnly] public float hitDistance;
    [ReadOnly] public float deltaTime;
   
    public void Execute(int index)
    {
        if(isActive[index] && targetIndex[index] != -1)
        {
            int enemyIndex = targetIndex[index];
            if (dead[enemyIndex])
            {
                targetIndex[index] = -1;
            }
            else
            {
                float3 delta = beePosition[enemyIndex] - beePosition[index];
                float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                if (sqrDist > attackDistance * attackDistance)
                {
                    beeVelocities[index] += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
                }
                else
                {
                    isAttacking[index] = true;
                    beeVelocities[index] += delta * (attackForce * deltaTime / Mathf.Sqrt(sqrDist));
                    if (sqrDist < hitDistance * hitDistance)
                    {
                        //If the particle system doesn't work I'm scrapping it
                        //ParticleManager.SpawnParticle(beePosition[enemyIndex], ParticleType.Blood, beeVelocities[index] * .35f, 2f, 6);
                        dead[enemyIndex] = true;
                        beeVelocities[enemyIndex] *= .5f;
                        targetIndex[index] = -1;
                    }
                }
            }
        }
    }
}
