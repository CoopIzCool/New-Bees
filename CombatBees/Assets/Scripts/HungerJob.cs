using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct HungerJob : IJobParallelFor
{
	[ReadOnly] public NativeArray<bool> isActive;
	public NativeArray<int> resourceTargetIndex;
	[ReadOnly] public NativeArray<bool> resourceDead;
	[ReadOnly] public NativeArray<bool> resourceStacked;
	[ReadOnly] public NativeArray<bool> resourceTopOfStack;
	[ReadOnly] public NativeArray<float3> beePosition;
	[ReadOnly] public NativeArray<float3> resourcePosition;

    public void Execute(int index)
    {
		
		
	}
}
