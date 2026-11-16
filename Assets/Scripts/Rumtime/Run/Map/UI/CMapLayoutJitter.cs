using UnityEngine;

public static class CMapLayoutJitter
{
	public static Vector2 Get(int seed, int nodeId)
	{
		System.Random random = new System.Random(unchecked(seed * 486187739 + nodeId * 16777619));
		float x = (float)(random.NextDouble() * 2.0 - 1.0);
		float y = (float)(random.NextDouble() * 2.0 - 1.0);

		return new Vector2(x, y);
	}
}
