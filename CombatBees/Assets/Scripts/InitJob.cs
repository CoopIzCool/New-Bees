using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct InitJob : IJobParallelFor
{
	public NativeArray<float3> beeVelocities;
	public NativeArray<float3> beeSmoothDirections;
	public NativeArray<bool> dead;
	public NativeArray<float> deathTimer;
	public NativeArray<int> targetIndex;
	public NativeArray<int> resourceTargetIndex;
	public NativeArray<bool> isAttacking;
	public NativeArray<bool> isHoldingResource;
	public NativeArray<bool> isActive;

	public void Execute(int index)
	{
		beeVelocities[index] = float3.zero;
		beeSmoothDirections[index] = float3.zero;
		dead[index] = true;
		deathTimer[index] = 1.0f;
		targetIndex[index] = -1;
		resourceTargetIndex[index] = -1;
		isAttacking[index] = false;
		isHoldingResource[index] = false;
		isActive[index] = false;
	}
}
