using UnityEngine;

[System.Serializable]
public class CRoadTileEntry
{
	public GameObject Prefab;
	public Vector2Int Min;
	public Vector2Int Max;
}

[CreateAssetMenu(menuName = "Stagecoach/Road Tile Set", fileName = "RoadTileSet")]
public class CRoadTileSet : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private float _cellSize = 10f;
	[SerializeField] private CRoadTileEntry[] _straight;
	[SerializeField] private CRoadTileEntry[] _diagonal;
	[SerializeField] private CRoadTileEntry[] _turn;
	[SerializeField] private CRoadTileEntry[] _decor;
	[SerializeField] private CRoadTileEntry[] _fork2;
	[SerializeField] private CRoadTileEntry[] _fork3;
	#endregion

	// 격자 한 칸 크기 (m)
	public float CellSize => _cellSize;
	// 직선 타일들
	public CRoadTileEntry[] Straight => _straight;
	// 대각선 타일들
	public CRoadTileEntry[] Diagonal => _diagonal;
	// 45도 커브 타일들
	public CRoadTileEntry[] Turn => _turn;
	// 장식 타일들
	public CRoadTileEntry[] Decor => _decor;
	// 2갈래 갈림길 타일들
	public CRoadTileEntry[] Fork2 => _fork2;
	// 3갈래 갈림길 타일들
	public CRoadTileEntry[] Fork3 => _fork3;
}
