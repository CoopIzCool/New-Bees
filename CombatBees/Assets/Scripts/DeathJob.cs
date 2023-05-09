using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct DeathJob : IJobParallelFor
{
	[ReadOnly] public NativeArray<bool> isActive;
	[ReadOnly] public NativeArray<bool> isDead;
	public NativeArray<float> deathTimer;
	public NativeArray<float3> beeVelocities;
	[ReadOnly] public float gravity;
	[ReadOnly] public float deltaTime;
    public void Execute(int index)
	{ 
		if(isActive[index] && isDead[index])
		{
			beeVelocities[index] += new float3(0,gravity * deltaTime,0);
			deathTimer[index] -= deltaTime / 10f;
		}
	}
}
