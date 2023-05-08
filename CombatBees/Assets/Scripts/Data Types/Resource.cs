using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource {
	public Vector3 position;
	public bool stacked;
	public int stackIndex;
	public int gridX;
	public int gridY;
	public int holderIndex;
	public Vector3 velocity;
	public bool dead;
	public int resourceIndex;

	public Resource(Vector3 myPosition, int index) {
		position = myPosition;
		resourceIndex = index;
		holderIndex = -1;
	}
}
