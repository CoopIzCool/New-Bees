using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;

public class BeeManager : MonoBehaviour
{
	public Mesh beeMesh;
	public Material beeMaterial;
	public Color[] teamColors;
	public float minBeeSize;
	public float maxBeeSize;
	public float speedStretch;
	public float rotationStiffness;
	[Space(10)]
	[Range(0f, 1f)]
	public float aggression;
	public float flightJitter;
	public float teamAttraction;
	public float teamRepulsion;
	[Range(0f, 1f)]
	public float damping;
	public float chaseForce;
	public float carryForce;
	public float grabDistance;
	public float attackDistance;
	public float attackForce;
	public float hitDistance;
	public float maxSpawnSpeed;
	[Space(10)]
	public int startBeeCount;

	List<Bee> bees;
	List<Bee>[] teamsOfBees;
	List<Bee> pooledBees;

	int activeBatch = 0;
	List<List<Matrix4x4>> beeMatrices;
	List<List<Vector4>> beeColors;

	static BeeManager instance;

	const int beesPerBatch = 1023;
	MaterialPropertyBlock matProps;

	#region ECS Fields
	[SerializeField] public static NativeArray<float3> beePositions;

	public static NativeArray<float3> beeVelocities;
	public NativeArray<float3> beeSmoothPositions;
	public NativeArray<float3> beeSmoothDirections;
	public NativeArray<bool> teams;
	public static NativeArray<float> sizes;
	public NativeArray<int> targetIndex;
	//To do once resources are refactored
	public NativeArray<int> resourceTargetIndex;

	//Equal to False at start
	public static NativeArray<bool> dead;
	//Equal to 1f at start
    public NativeArray<float> deathTimer;
	public NativeArray<bool> isAttacking;
	public NativeArray<bool> isHoldingResource;
	public NativeArray<bool> isActive;

	public NativeArray<int> trueTeamIndex;
	public NativeArray<int> falseTeamIndex;
	//Probably not needed
	//public int index;

	int highestIndex = 0;
	int trueIndex = 0;
	int falseIndex = 0;
	int startSeed = 0;
	List<int> pooledIndex = new List<int>();
	#endregion

	public void OnDestroy()
	{
		beePositions.Dispose();
		beeVelocities.Dispose();
		beeSmoothPositions.Dispose();
		beeSmoothDirections.Dispose();
		teams.Dispose();
		sizes.Dispose();
		targetIndex.Dispose();
		resourceTargetIndex.Dispose();
		dead.Dispose();
		deathTimer.Dispose();
		isAttacking.Dispose();
		isHoldingResource.Dispose();
		isActive.Dispose();
		trueTeamIndex.Dispose();
		falseTeamIndex.Dispose();
	}

	public static void SpawnBee(int team)
	{
		float3 pos = new float3(1.0f, 0, 0) * (-Field.size.x * .4f + Field.size.x * .8f * team);
		bool teamBool = (team % 2 == 0) ? true : false;
		//Vector3 pos = Vector3.right * (-Field.size.x * .4f + Field.size.x * .8f * team);
		instance._SpawnBee(pos, teamBool);
	}

	public static void SpawnBee(Vector3 pos, int team)
	{
		bool teamBool = (team % 2 == 0) ? true : false;
		instance._SpawnBee((float3)pos, teamBool);
	}

	void _SpawnBee(float3 pos, bool team)
	{
		int selectedIndex;
		if(pooledIndex.Count == 0)
		{
			selectedIndex = highestIndex;
			highestIndex++;
		}
		else
		{
			selectedIndex = pooledIndex[pooledIndex.Count - 1];
			pooledIndex.RemoveAt(pooledIndex.Count - 1);
		}

		beePositions[selectedIndex] = pos;
		teams[selectedIndex] = team;
		sizes[selectedIndex] = UnityEngine.Random.Range(minBeeSize, maxBeeSize);
		Vector3 initVelocity = UnityEngine.Random.insideUnitSphere * maxSpawnSpeed;
		beeVelocities[selectedIndex] = (float3)initVelocity;
		dead[selectedIndex] = false;
		isActive[selectedIndex] = true; 
		int teamIndex = (team) ? 0 : 1;

		if(team)
		{
			trueTeamIndex[trueIndex] = selectedIndex;
			trueIndex++;
		}
		else
		{
			falseTeamIndex[falseIndex] = selectedIndex;
			falseIndex++;
		}
		if (beeMatrices[activeBatch].Count == beesPerBatch)
		{
			activeBatch++;
			if (beeMatrices.Count == activeBatch)
			{
				beeMatrices.Add(new List<Matrix4x4>());
				beeColors.Add(new List<Vector4>());
			}
		}
		beeMatrices[activeBatch].Add(Matrix4x4.identity);
		
		beeColors[activeBatch].Add(teamColors[teamIndex]);
	}


	void DeleteBee(int selectedIndex)
	{
		pooledIndex.Add(selectedIndex);
		isActive[selectedIndex] = false;
		if (beeMatrices[activeBatch].Count == 0 && activeBatch > 0)
		{
			activeBatch--;
		}
		beeMatrices[activeBatch].RemoveAt(beeMatrices[activeBatch].Count - 1);
		beeColors[activeBatch].RemoveAt(beeColors[activeBatch].Count - 1);
	}


	void Awake()
	{
		instance = this;
	}
	void Start()
	{
		bees = new List<Bee>(50000);
		teamsOfBees = new List<Bee>[2];
		pooledBees = new List<Bee>(50000);

		beeMatrices = new List<List<Matrix4x4>>();
		beeMatrices.Add(new List<Matrix4x4>());
		beeColors = new List<List<Vector4>>();
		beeColors.Add(new List<Vector4>());

		//dont know if this is needed but check regardless
		//matProps = new MaterialPropertyBlock();

		#region ECS declerations
		beePositions = new NativeArray<float3>(10000, Allocator.Persistent);
		beeVelocities = new NativeArray<float3>(10000, Allocator.Persistent);
		beeSmoothPositions = new NativeArray<float3>(10000, Allocator.Persistent);
		beeSmoothDirections = new NativeArray<float3>(10000, Allocator.Persistent);
		teams = new NativeArray<bool>(10000, Allocator.Persistent);
		sizes = new NativeArray<float>(10000, Allocator.Persistent);
		targetIndex = new NativeArray<int>(10000, Allocator.Persistent);
		resourceTargetIndex = new NativeArray<int>(10000, Allocator.Persistent);
		dead = new NativeArray<bool>(10000, Allocator.Persistent);
		deathTimer = new NativeArray<float>(10000, Allocator.Persistent);
		isAttacking = new NativeArray<bool>(10000, Allocator.Persistent);
		isHoldingResource = new NativeArray<bool>(10000, Allocator.Persistent);
		isActive = new NativeArray<bool>(10000, Allocator.Persistent);
		trueTeamIndex = new NativeArray<int>(5000, Allocator.Persistent);
		falseTeamIndex = new NativeArray<int>(5000, Allocator.Persistent);
		InitJob initJob = new InitJob
		{
			beeVelocities = beeVelocities,
			beeSmoothDirections = beeSmoothDirections,
			dead = dead,
			deathTimer = deathTimer,
			targetIndex = targetIndex,
			resourceTargetIndex = resourceTargetIndex,
			isAttacking = isAttacking,
			isHoldingResource = isHoldingResource,
			isActive = isActive
		};

		JobHandle initHandle = initJob.Schedule(10000, 64);
		initHandle.Complete();
		#endregion

		
		for (int i = 0; i < startBeeCount; i++)
		{
			int team = i % 2;
			SpawnBee(team);
		}

		matProps = new MaterialPropertyBlock();
		matProps.SetVectorArray("_Color", new Vector4[beesPerBatch]);
		
	}

	void FixedUpdate()
	{
		float deltaTime = Time.fixedDeltaTime;
		
		#region Jobs Update
		VelocityJob velocityJob = new VelocityJob
		{
			isAttacking = isAttacking,
			isHoldingResource = isHoldingResource,
			dead = dead,
			beeVelocities = beeVelocities,
			team = teams,
			teamTrue = trueTeamIndex,
			teamTrueMax = trueIndex,
			teamFalse = falseTeamIndex,
			teamFalseMax = falseIndex,
			beePositions = beePositions,
			isActive = isActive,
			flightJitter = flightJitter,
			damping = damping,
			teamAttraction = teamAttraction,
			teamRepulsion = teamRepulsion,
			startSeed = startSeed,
			deltaTime = Time.deltaTime
			
		};

		AngerJob angerJob = new AngerJob
		{
			targetIndex = targetIndex,
			dead = dead,
			beePosition = beePositions,
			beeVelocities = beeVelocities,
			isAttacking = isAttacking,
			isActive = isActive,
			attackDistance = attackDistance,
			chaseForce = chaseForce,
			attackForce = attackForce,
			hitDistance = hitDistance,
			deltaTime = Time.deltaTime
		};

		DeathJob deathJob = new DeathJob
		{
			isActive = isActive,
			isDead = dead,
			deathTimer = deathTimer,
			beeVelocities = beeVelocities,
			gravity = Field.gravity,
			deltaTime = Time.deltaTime
		};

		BeeMovementJob beeMovementJob = new BeeMovementJob
		{
			beePositions = beePositions,
			beeVelocities = beeVelocities,
			smoothPositions = beeSmoothPositions,
			smoothDirections = beeSmoothDirections,
			isHoldingResource = isHoldingResource,
			isAttacking = isAttacking,
			isActive = isActive,
			fieldSize = Field.burstSize,
			resourceSize = ResourceManager.instance.resourceSize,
			rotationStiffness = rotationStiffness,
			deltaTime = Time.deltaTime
		};

		JobHandle velocityHandle = velocityJob.Schedule(beePositions.Length, 64);
		velocityHandle.Complete();

		
		//Loop for Decision and Resources
		for(int i = 0; i < beePositions.Length; i++)
		{
			if(isActive[i])
			{
				if (targetIndex[i] == -1 && resourceTargetIndex[i] == -1)
				{
					if (UnityEngine.Random.value < aggression)
					{
						int enemyIndex = 0;
						//Find a way to make sure only a real index gets called.
						if (teams[i])
						{
							enemyIndex = falseTeamIndex[UnityEngine.Random.Range(0, falseIndex)];
						}
						else
						{
							enemyIndex = trueTeamIndex[UnityEngine.Random.Range(0, trueIndex)];
						}
					}
					else
					{
						resourceTargetIndex[i] = ResourceManager.TryGetRandomResourceIndex();
					}
				}

				if (resourceTargetIndex[i] != -1)
				{
					int resourceIndex = resourceTargetIndex[i];
					int heldIndex = ResourceManager.holderIndex[resourceIndex];
					if (ResourceManager.holderIndex[resourceIndex] == -1)
					{
						if (ResourceManager.dead[resourceIndex])
						{
							resourceTargetIndex[i] = -1;
						}
						else if (ResourceManager.resourceStacked[resourceIndex] && ResourceManager.isTopOfStack[resourceIndex] == false)
						{
							resourceTargetIndex[i] = -1;
						}
						else
						{
							float3 delta = ResourceManager.resourcePosition[resourceIndex] - beePositions[i];
							float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
							if (sqrDist > grabDistance * grabDistance)
							{
								beeVelocities[i] += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
							}
							else if (ResourceManager.resourceStacked[resourceIndex])
							{
								bool success = ResourceManager.GrabResource(i, resourceIndex);
								if(!success)
								{
									resourceTargetIndex[i] = ResourceManager.TryGetRandomResourceIndex();
								}
							}
						}
					}
					else if (ResourceManager.holderIndex[resourceIndex] == i)
					{
						int teamPlacement = (teams[i]) ? 0 : 1;
						float3 targetPos = new float3(-Field.size.x * .45f + Field.size.x * .9f * teamPlacement, 0f, beePositions[i].z);
						float3 delta = targetPos - beePositions[i];
						float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
						beeVelocities[i] += (targetPos - beePositions[i]) * (carryForce * deltaTime / dist);
						if (dist < 1f)
						{
							ResourceManager.RemoveHolderAtIndex(resourceIndex);
							resourceTargetIndex[i] = -1;
						}
						else
						{
							isHoldingResource[i] = true;
						}
					}
					else if (teams[heldIndex] != teams[i])
					{
						targetIndex[i] = heldIndex;
					}
					else if (teams[heldIndex] == teams[i])
					{
						resourceTargetIndex[i] = -1;
					}
				}
				
			}
			
		}
		
		//Maybe figure out a way to implement particles on death if I want a medal or something
		JobHandle angerHandle = angerJob.Schedule(beePositions.Length, 64);
		angerHandle.Complete();
		
		JobHandle deathHandle = deathJob.Schedule(beePositions.Length, 64);
		deathHandle.Complete();

		for(int i = 0; i < highestIndex; i++)
		{
			if(isActive[i] && dead[i])
			{
				if(deathTimer[i] < 0)
				{
					DeleteBee(i);
				}
			}
		}
		
		JobHandle movementHandle = beeMovementJob.Schedule(beePositions.Length, 64);
		movementHandle.Complete();
		
		startSeed++;
        #endregion
    }
    private void Update()
	{
		for (int i = 0; i < highestIndex; i++)
		{
			if(isActive[i])
			{
				
				float size = sizes[i];
				Vector3 scale = new Vector3(size, size, size);
				if (dead[i] == false)
				{
					float3 velocity = beeVelocities[i];
					float velocityMagnitude = (velocity.x * velocity.x) + (velocity.y * velocity.y) + (velocity.z * velocity.z);
					float stretch = Mathf.Max(1f, velocityMagnitude * speedStretch);
					scale.z *= stretch;
					scale.x /= (stretch - 1f) / 5f + 1f;
					scale.y /= (stretch - 1f) / 5f + 1f;
				}
				Quaternion rotation = Quaternion.identity;
				if ((Vector3)beeSmoothDirections[i] != Vector3.zero)
				{
					rotation = Quaternion.LookRotation((Vector3)beeSmoothDirections[i]);
				}
				int colorIndex = (teams[i]) ? 0 : 1;
				Color color = teamColors[colorIndex];
				if (dead[i])
				{
					color *= .75f;
					scale *= Mathf.Sqrt(deathTimer[i]);
				}
				try
				{
					beeMatrices[i / beesPerBatch][i % beesPerBatch] = Matrix4x4.TRS((Vector3)beePositions[i], rotation, scale);
					beeColors[i / beesPerBatch][i % beesPerBatch] = color;
				}
				catch(Exception e)
				{
					//Debug.LogError("Active State of " + i + " is " + isActive[i]);
				}
			}
		}
		for (int i = 0; i <= activeBatch; i++)
		{
			if (beeMatrices[i].Count > 0)
			{
				matProps.SetVectorArray("_Color", beeColors[i]);
				Graphics.DrawMeshInstanced(beeMesh, 0, beeMaterial, beeMatrices[i], matProps);
			}
		}
		
	}
}