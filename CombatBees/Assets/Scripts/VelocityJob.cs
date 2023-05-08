using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct VelocityJob : IJobParallelFor
{
    public NativeArray<bool> isAttacking;
    public NativeArray<bool> isHoldingResource;
    [ReadOnly] public NativeArray<bool> dead;
    public NativeArray<float3> beeVelocities;
    [ReadOnly] public NativeArray<bool> team;
    [ReadOnly] public NativeArray<int> teamTrue;
    [ReadOnly] public int teamTrueMax;
    public NativeArray<int> teamFalse;
    [ReadOnly] public int teamFalseMax;
    [ReadOnly] public NativeArray<float3> beePositions;
    [ReadOnly] public NativeArray<bool> isActive;
    [ReadOnly] public float flightJitter;
    [ReadOnly] public float damping;
    [ReadOnly] public float teamAttraction;
    [ReadOnly] public float teamRepulsion;
    [ReadOnly] public float deltaTime;
    public void Execute(int index)
    {
        if(isActive[index])
        {
            isAttacking[index] = false;
            isHoldingResource[index] = false;
            if(!dead[index])
            {
                beeVelocities[index] = UnityEngine.Random.insideUnitSphere * (flightJitter * deltaTime);
                beeVelocities[index] *= (1f * damping);
                int attractiveFriendIndex = 0;
                int repellantFriendIndex = 0;
                if (team[index])
                {
                    //Find some way to avoid inactive Index
                    attractiveFriendIndex = teamTrue[UnityEngine.Random.Range(0, teamTrueMax - 1)];
                    repellantFriendIndex = teamTrue[UnityEngine.Random.Range(0, teamTrueMax - 1)];
                }
                else
                {
                    attractiveFriendIndex = teamFalse[UnityEngine.Random.Range(0, teamTrueMax - 1)];
                    repellantFriendIndex = teamFalse[UnityEngine.Random.Range(0, teamTrueMax - 1)];
                }

                if(isActive[attractiveFriendIndex])
                {
                    float3 delta = beePositions[attractiveFriendIndex] - beePositions[index];
                    float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if (dist > 0f)
                    {
                        beeVelocities[index] += delta * (teamAttraction * deltaTime / dist);
                    }
                }

                if(isActive[repellantFriendIndex])
                {
                    float3 delta = beePositions[repellantFriendIndex] - beePositions[index];
                    float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                    if (dist > 0f)
                    {
                        beeVelocities[index] -= delta * (teamRepulsion * deltaTime / dist);
                    }
                }

            }

        }
    }
}
