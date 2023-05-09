using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct BeeMovementJob : IJobParallelFor
{
    public NativeArray<float3> beePositions;
    public NativeArray<float3> beeVelocities;
	public NativeArray<float3> smoothPositions;
	public NativeArray<float3> smoothDirections;
	[ReadOnly] public NativeArray<bool> isHoldingResource;
	[ReadOnly] public NativeArray<bool> isAttacking;
	[ReadOnly] public NativeArray<bool> isActive;
	[ReadOnly] public float3 fieldSize;
	[ReadOnly] public float resourceSize;
	[ReadOnly] public float rotationStiffness;
	[ReadOnly] public float deltaTime;

	public void Execute(int index)
    {
		if(isActive[index])
		{

			float3 beePos = beePositions[index];
			float3 beeVel = beeVelocities[index];

			beePos += deltaTime * beeVel;
			//test
			if (System.Math.Abs(beePos.x) > fieldSize.x * .5f)
			{
				beePos.x = (fieldSize.x * .5f) * Mathf.Sign(beePos.x);
				beeVel.x *= -.5f;
				beeVel.y *= .8f;
				beeVel.z *= .8f;
			}

			if (System.Math.Abs(beePos.z) > fieldSize.z * .5f)
			{
				beePos.z = (fieldSize.z * .5f) * Mathf.Sign(beePos.z);
				beeVel.z *= -.5f;
				beeVel.x *= .8f;
				beeVel.y *= .8f;
			}

			float resourceModifier = 0f;
			if (isHoldingResource[index])
			{
				resourceModifier = resourceSize;
			}

			if (System.Math.Abs(beePos.y) > fieldSize.y * .5f - resourceModifier)
			{
				beePos.y = (fieldSize.y * .5f - resourceModifier) * Mathf.Sign(beePos.y);
				beeVel.y *= -.5f;
				beeVel.z *= .8f;
				beeVel.x *= .8f;
			}
			beePositions[index] = beePos;
			beeVelocities[index] = beeVel;
			float3 oldSmoothPos = smoothPositions[index];
			if (isAttacking[index] == false)
			{
				smoothPositions[index] = Vector3.Lerp(smoothPositions[index], beePositions[index], deltaTime * rotationStiffness);
			}
			else
			{
				smoothPositions[index] = beePositions[index];
			}
			smoothDirections[index] = smoothPositions[index] - oldSmoothPos;
		}

	}
}
