using System.Collections.Generic;
using UnityEngine;

public class CRoadPath : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CRoadTileSet _tileSet;
	[SerializeField] private Transform _follow;
	[SerializeField] private float _aheadDistance = 140f;
	[SerializeField] private float _behindDistance = 40f;
	[SerializeField] private int _startBackCells = 3;
	[SerializeField] private int _startStraightCells = 6;
	[SerializeField] private int _minStraightRun = 2;
	[SerializeField, Range(0f, 1f)] private float _turnChance = 0.35f;
	[SerializeField, Range(1, 2)] private int _maxTurnSteps = 1;
	[SerializeField] private int _decorRadius = 3;
	[SerializeField] private float _roadHalfWidth = 4.5f;
	[SerializeField] private int _seed = 0;
	[SerializeField] private bool _buildWalls = true;
	[SerializeField] private float _wallOffset = 0.5f;
	[SerializeField] private float _wallHeight = 8f;
	[SerializeField] private float _wallThickness = 1f;
	[SerializeField] private bool _flatFloor = true;
	[SerializeField, Range(0.05f, 1f)] private float _floorSmoothing = 0.3f;
	[SerializeField] private float _floorLift = 0f;
	[SerializeField] private float _floorSampleSide = 2f;
	[SerializeField] private float _branchLength = 40f;
	[SerializeField] private string[] _forkRoadMaterials = { "road" };
	[SerializeField] private string[] _junctionHideMaterials = { "grass" };
	[SerializeField] private float _forkFlatHeight = 0.15f;
	[SerializeField] private string[] _forkRampMaterials = { "road", "trench", "tiling", "ground" };
	[SerializeField] private bool _logState = false;
	[SerializeField] private bool _logFork = true;
	#endregion

	#region 이벤트
	public event System.Action<int> OnBranchChosen;
	#endregion

	#region 내부 변수
	private const int STRAIGHT_A = 4;
	private const int STRAIGHT_B = 0;
	private const int DIAGONAL_A = 5;
	private const int DIAGONAL_B = 1;
	private const int TURN_A = 4;
	private const int TURN_B = 1;
	private const int CURVE_SAMPLES = 6;
	private const int MAX_BRANCHES = 3;
	private const int FORK_STRAIGHTEN_CELLS = 3;
	private const int FORK_OPEN_TILES = 2;
	private const string RAMP_SUFFIX = "_ramp";
	private const float FORK_LAYER_GAP = 0.01f;

	private static readonly Vector2Int[] DIRS =
	{
		new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
		new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
	};

	// 갈래 수별 나가는 방향 (왼쪽부터). 갈림길은 항상 정면(0)을 보고 있을 때 놓는다
	private static readonly int[] FORK_EXITS_2 = { 7, 1 };
	private static readonly int[] FORK_EXITS_3 = { 7, 0, 1 };

	private class CPlaced
	{
		public GameObject Go;
		public List<Vector2Int> Cells;
		public float Arc;
		public float StartArc;
		public int EntryHeading;
		public Vector2Int Main;
		public GameObject Walls;
		// 건축물 자리라 나무를 숨긴 장식 타일
		public bool DecorCleared;
		public GameObject Floor;
		public bool Decorated;
		// 갈림길 갈래 번호 (-1 = 갈래 아님)
		public int Branch = -1;
		// 고르지 않은 갈래 → 다시 깔기 대상에서 뺀다
		public bool Abandoned;
	}

	private class CChain
	{
		public List<Vector3> Points;
		public List<float> Arcs;
		public List<float> Heights;
		public Vector2Int Cell;
		public int Heading;
		public int StraightLeft;
		public float FrontArc;
	}

	private readonly Dictionary<Vector2Int, int> _occupied = new Dictionary<Vector2Int, int>();
	private readonly List<CPlaced> _roadPieces = new List<CPlaced>();
	private readonly List<CPlaced> _decorPieces = new List<CPlaced>();
	private readonly List<CChain> _branches = new List<CChain>();
	private readonly List<Vector2> _straightZones = new List<Vector2>();
	private readonly Dictionary<string, Mesh> _filteredMeshes = new Dictionary<string, Mesh>();
	private List<Vector3> _points = new List<Vector3>();
	private List<float> _arcs = new List<float>();
	private List<float> _heights = new List<float>();
	private float _forkArc;
	private int _forkCount;
	private float _forkStartArc;
	private System.Random _random;
	private Vector2Int _cell;
	private int _heading;
	private int _straightLeft;
	private int _frontY;
	private int _placedCount;
	private int _placingBranch = -1;
	private float _frontArc;
	private float _followArc;
	private PhysicMaterial _wallMaterial;
	#endregion

	public float RoadHalfWidth => _roadHalfWidth;
	public bool HasFork => _forkCount >= 2 || _branches.Count > 0;

	// 이 지점에 길가 물건을 놓아도 되는지
	//  ㄴ 갈래가 깔린 동안이나 예약된 갈림길 근처는 길이 다시 깔릴 수 있어서 피한다
	public bool IsStableArc(float arc, float margin)
	{
		if (_branches.Count > 0)
		{
			return false;
		}

		if (_forkCount >= 2 && arc > _forkArc - margin)
		{
			return false;
		}

		return arc < _frontArc;
	}

	// 이 지점까지 길이 깔렸는지
	public bool IsBuilt(float arc)
	{
		return arc < _frontArc;
	}

	// 길가 장식 타일의 땅 높이 (타일은 모두 길 오브젝트 높이 0 에 깔린다)
	public float DecorGroundHeight => transform.position.y;

	// 건축물 자리를 비운다 → 반경 안 길가 장식에서 나무·풀 같은 것만 숨기고 땅은 남긴다
	//  ㄴ 장식 타일은 땅+나무가 합쳐진 메시 하나 → 타일을 끄면 땅까지 사라진다
	//  ㄴ 그래서 땅 재질이 아닌 서브메시만 hiddenMaterial(아무것도 안 그림)로 바꾼다
	public void ClearDecorAround(Vector3 center, float radius, Material hiddenMaterial, string[] groundKeywords)
	{
		if (hiddenMaterial == null)
		{
			return;
		}

		float sqrRadius = radius * radius;
		Vector3 local = transform.InverseTransformPoint(center);
		Vector2Int centerCell = new Vector2Int(Mathf.RoundToInt(local.x / _tileSet.CellSize), Mathf.RoundToInt(local.z / _tileSet.CellSize));

		for (int i = 0; i < _decorPieces.Count; i++)
		{
			CPlaced piece = _decorPieces[i];

			if (piece.Go == null || piece.DecorCleared)
			{
				continue;
			}

			// 건물이 선 칸을 차지한 타일 + 반경 안 칸 중심이 있는 타일만
			if (!piece.Cells.Contains(centerCell) && (radius <= 0f || !IsNearCells(piece, center, sqrRadius)))
			{
				continue;
			}

			piece.DecorCleared = true;
			HideNonGround(piece.Go, hiddenMaterial, groundKeywords);
		}
	}

	// 타일이 차지한 칸 중심 하나라도 반경 안이면 가깝다
	private bool IsNearCells(CPlaced piece, Vector3 center, float sqrRadius)
	{
		for (int i = 0; i < piece.Cells.Count; i++)
		{
			Vector3 delta = transform.TransformPoint(CellToLocal(piece.Cells[i])) - center;

			delta.y = 0f;

			if (delta.sqrMagnitude <= sqrRadius)
			{
				return true;
			}
		}

		return false;
	}

	private static void HideNonGround(GameObject go, Material hiddenMaterial, string[] groundKeywords)
	{
		MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);

		for (int i = 0; i < renderers.Length; i++)
		{
			Material[] materials = renderers[i].sharedMaterials;

			for (int j = 0; j < materials.Length; j++)
			{
				if (!IsGroundMaterial(materials[j], groundKeywords))
				{
					materials[j] = hiddenMaterial;
				}
			}

			renderers[i].sharedMaterials = materials;
		}
	}

	private static bool IsGroundMaterial(Material material, string[] groundKeywords)
	{
		if (material == null || groundKeywords == null)
		{
			return false;
		}

		for (int i = 0; i < groundKeywords.Length; i++)
		{
			if (!string.IsNullOrEmpty(groundKeywords[i]) && material.name.Contains(groundKeywords[i]))
			{
				return true;
			}
		}

		return false;
	}

	public void SetFollow(Transform target)
	{
		_follow = target;
	}

	public void RequestStraight(float from, float to)
	{
		if (to <= from)
		{
			return;
		}

		_straightZones.Add(new Vector2(from, to));

		if (_logFork)
		{
			CPrint.Log($"CRoadPath : {from:F0}m ~ {to:F0}m 곧은 길 예약");
		}

		float followArc = _followArc;

		if (_follow != null && TryProject(_follow.position, out float arc, out _, out _))
		{
			followArc = Mathf.Max(followArc, arc);
		}

		float cut = Mathf.Max(from - _tileSet.CellSize * FORK_STRAIGHTEN_CELLS, followArc + _tileSet.CellSize);

		if (TruncateFrom(cut))
		{
			Extend();
			Physics.SyncTransforms();
		}
	}

	public void RequestFork(float arc, int count)
	{
		if (count < 2 || _branches.Count > 0)
		{
			return;
		}

		if (count > MAX_BRANCHES)
		{
			CPrint.Warn($"CRoadPath : 갈래는 {MAX_BRANCHES}개까지입니다. {count}개 중 {MAX_BRANCHES}개만 놓습니다.");
			count = MAX_BRANCHES;
		}

		_forkArc = arc;
		_forkCount = count;

		if (_logFork)
		{
			CPrint.Log($"CRoadPath : {arc:F0}m 지점에 {count}갈래 갈림길 예약");
		}

		if (TruncateFrom(arc - _tileSet.CellSize * FORK_STRAIGHTEN_CELLS))
		{
			Extend();
			Physics.SyncTransforms();
		}
	}

	public bool TryProject(Vector3 worldPosition, out float arc, out float lateral, out Vector3 right)
	{
		if (_branches.Count == 0)
		{
			return ProjectOn(_points, _arcs, worldPosition, out arc, out lateral, out right, out _);
		}

		return FindNearestBranch(worldPosition, out arc, out lateral, out right, out _, out _) >= 0;
	}

	private int FindNearestBranch(Vector3 worldPosition, out float arc, out float lateral, out Vector3 right, out float bestSqr, out float secondSqr)
	{
		arc = 0f;
		lateral = 0f;
		right = transform.right;
		bestSqr = float.MaxValue;
		secondSqr = float.MaxValue;

		int best = -1;

		for (int i = 0; i < _branches.Count; i++)
		{
			CChain branch = _branches[i];

			if (!ProjectOn(branch.Points, branch.Arcs, worldPosition, out float a, out float l, out Vector3 r, out float sqr))
			{
				continue;
			}

			if (sqr >= bestSqr)
			{
				secondSqr = Mathf.Min(secondSqr, sqr);
				continue;
			}

			secondSqr = bestSqr;
			bestSqr = sqr;
			best = i;
			arc = a;
			lateral = l;
			right = r;
		}

		return best;
	}

	private bool ProjectOn(List<Vector3> points, List<float> arcs, Vector3 worldPosition, out float arc, out float lateral, out Vector3 right, out float bestSqr)
	{
		arc = 0f;
		lateral = 0f;
		right = transform.right;
		bestSqr = float.MaxValue;

		if (points.Count < 2)
		{
			return false;
		}

		Vector3 local = transform.InverseTransformPoint(worldPosition);

		local.y = 0f;

		for (int i = 0; i < points.Count - 1; i++)
		{
			Vector3 a = points[i];
			Vector3 ab = points[i + 1] - a;
			float lengthSqr = ab.sqrMagnitude;

			if (lengthSqr < 0.0001f)
			{
				continue;
			}

			float t = Mathf.Clamp01(Vector3.Dot(local - a, ab) / lengthSqr);
			Vector3 closest = a + ab * t;
			float sqr = (local - closest).sqrMagnitude;

			if (sqr >= bestSqr)
			{
				continue;
			}

			Vector3 localRight = Vector3.Cross(Vector3.up, ab).normalized;

			bestSqr = sqr;
			arc = Mathf.Lerp(arcs[i], arcs[i + 1], t);
			lateral = Vector3.Dot(local - closest, localRight);
			right = transform.TransformDirection(localRight);
		}

		return true;
	}

	public bool TryGetPoint(float arc, out Vector3 position, out Vector3 forward)
	{
		return GetPointOn(_points, _arcs, arc, out position, out forward);
	}

	public bool TryGetBranchPoint(int branch, float distance, out Vector3 position)
	{
		position = transform.position;

		if (branch < 0 || branch >= _branches.Count)
		{
			return false;
		}

		return GetPointOn(_branches[branch].Points, _branches[branch].Arcs, _forkStartArc + distance, out position, out _);
	}

	private bool GetPointOn(List<Vector3> points, List<float> arcs, float arc, out Vector3 position, out Vector3 forward)
	{
		position = transform.position;
		forward = transform.forward;

		if (points.Count < 2)
		{
			return false;
		}

		int i = 0;

		while (i < points.Count - 2 && arcs[i + 1] < arc)
		{
			i++;
		}

		float length = Mathf.Max(0.0001f, arcs[i + 1] - arcs[i]);
		float t = Mathf.Clamp01((arc - arcs[i]) / length);
		Vector3 local = Vector3.Lerp(points[i], points[i + 1], t);

		position = transform.TransformPoint(local);
		forward = transform.TransformDirection((points[i + 1] - points[i]).normalized);

		return true;
	}

	private void Awake()
	{
		if (_tileSet == null)
		{
			CPrint.Warn("CRoadPath : Tile Set 이 비어 있습니다.");
			enabled = false;
			return;
		}

		_random = _seed != 0 ? new System.Random(_seed) : new System.Random();
		_cell = new Vector2Int(0, -_startBackCells);
		_frontY = _cell.y;
		_heading = 0;
		_straightLeft = _startStraightCells + _startBackCells;

		float startDistance = _startBackCells * _tileSet.CellSize;

		while (_frontArc < startDistance + _aheadDistance)
		{
			PlaceNext();
		}

		Physics.SyncTransforms();
	}

	private void Update()
	{
		if (_follow == null)
		{
			return;
		}

		if (TryProject(_follow.position, out float arc, out _, out _))
		{
			_followArc = Mathf.Max(_followArc, arc);
		}

		bool changed = false;

		// 갈림길에서 고르는 중이면 마차가 어느 갈래로 들어갔는지 본다
		if (_branches.Count > 0)
		{
			TryChooseBranch();
		}

		if (Extend())
		{
			changed = true;
		}

		if (RemoveBehind())
		{
			changed = true;
		}

		if (changed)
		{
			Physics.SyncTransforms();
		}
	}

	#region 길 놓기
	// 마차 앞 _aheadDistance 까지 깐다. 갈림길 자리에 닿으면 갈림길을 놓고 멈춘다
	private bool Extend()
	{
		bool changed = false;

		while (_branches.Count == 0 && _frontArc < _followArc + _aheadDistance)
		{
			if (IsForkReached())
			{
				PlaceFork();
				return true;
			}

			PlaceNext();
			changed = true;
		}

		return changed;
	}

	private void PlaceNext()
	{
		PlaceTile(DecideHeading(), true, true, true);
	}

	private void PlaceTile(int next, bool leftWall, bool rightWall, bool decorate, GameObject shared = null, bool roadOnly = false, bool ramp = false)
	{
		int entry = (_heading + 4) % 8;
		GameObject go = shared;
		List<Vector2Int> cells = new List<Vector2Int>();

		if (shared == null)
		{
			CRoadTileEntry[] list;
			int a;
			int b;

			if (next == _heading)
			{
				bool diagonal = _heading % 2 == 1;

				list = diagonal ? _tileSet.Diagonal : _tileSet.Straight;
				a = diagonal ? DIAGONAL_A : STRAIGHT_A;
				b = diagonal ? DIAGONAL_B : STRAIGHT_B;
			}
			else
			{
				list = _tileSet.Turn;
				a = TURN_A;
				b = TURN_B;
			}

			if (!TryChoose(list, a, b, entry, next, out CRoadTileEntry tile, out int rotation, out bool mirror))
			{
				CPrint.Error($"CRoadPath : {entry} → {next} 타일을 만들 수 없습니다.");
				enabled = false;
				_frontArc = float.MaxValue;
				return;
			}

			go = Spawn(tile, _cell, rotation, mirror, "Road");
			AddColliders(go);
			cells = GetCells(tile, _cell, rotation, mirror);

			if (roadOnly)
			{
				FilterSubMeshes(go, _forkRoadMaterials, true, _junctionHideMaterials);

				DisableColliders(go);
			}

			if (ramp)
			{
				RampUp(go, _heading);
			}

			if (_logState)
			{
				CPrint.Log($"CRoadPath : {_cell} {tile.Prefab.name} 회전 {rotation * 90} 반전 {mirror}");
			}
		}

		CPlaced placed = new CPlaced
		{
			Go = shared == null ? go : null,
			Cells = cells,
			Main = _cell,
			StartArc = _frontArc,
			EntryHeading = _heading,
			Branch = _placingBranch
		};

		Occupy(placed.Cells);

		int wallFrom = Mathf.Max(0, _points.Count - 1);

		AddCurve(_cell, entry, next);
		placed.Arc = _frontArc;
		placed.Walls = BuildWalls(wallFrom, _placedCount, leftWall, rightWall);
		placed.Floor = BuildFloor(wallFrom, go, _placedCount);
		_roadPieces.Add(placed);

		_heading = next;
		_cell += DIRS[next];
		_frontY = Mathf.Max(_frontY, _cell.y);
		_placedCount++;

		if (decorate)
		{
			DecorateBehind();
		}
	}

	private void DecorateBehind()
	{
		int last = _roadPieces.Count - 1 - (_decorRadius + 2);

		for (int i = 0; i <= last; i++)
		{
			CPlaced piece = _roadPieces[i];

			if (piece.Decorated)
			{
				continue;
			}

			piece.Decorated = true;
			FillDecor(piece.Main, piece.Arc);
		}
	}

	private void DecorateBeforeFork()
	{
		for (int i = 0; i < _roadPieces.Count; i++)
		{
			CPlaced piece = _roadPieces[i];

			if (piece.Decorated || piece.StartArc >= _forkStartArc)
			{
				continue;
			}

			piece.Decorated = true;
			FillDecor(piece.Main, piece.Arc);
		}
	}

	private bool IsStraightAhead()
	{
		float lead = _tileSet.CellSize * FORK_STRAIGHTEN_CELLS;

		if (_forkCount >= 2 && _frontArc >= _forkArc - lead)
		{
			return true;
		}

		for (int i = 0; i < _straightZones.Count; i++)
		{
			if (_frontArc >= _straightZones[i].x - lead && _frontArc < _straightZones[i].y)
			{
				return true;
			}
		}

		return false;
	}

	private int DecideHeading()
	{
		if (IsStraightAhead())
		{
			int forkOffset = _heading <= 4 ? _heading : _heading - 8;

			_straightLeft = 0;

			return forkOffset == 0 ? _heading : (_heading - System.Math.Sign(forkOffset) + 8) % 8;
		}

		if (_straightLeft > 0)
		{
			_straightLeft--;
			return _heading;
		}

		if (_random.NextDouble() > _turnChance)
		{
			return _heading;
		}

		int offset = _heading <= 4 ? _heading : _heading - 8;
		int step;

		if (offset == 0)
		{
			step = _random.Next(2) == 0 ? -1 : 1;
		}
		else if (Mathf.Abs(offset) >= _maxTurnSteps || _random.NextDouble() < 0.6)
		{
			step = -System.Math.Sign(offset);
		}
		else
		{
			step = System.Math.Sign(offset);
		}

		_straightLeft = _minStraightRun;

		return (_heading + step + 8) % 8;
	}

	private static void AddRequiredCells(int side, List<Vector2Int> required)
	{
		if (side % 2 == 0)
		{
			return;
		}

		required.Add(new Vector2Int(0, DIRS[side].y));
	}

	private bool TryChoose(CRoadTileEntry[] list, int a, int b, int entry, int exit, out CRoadTileEntry tile, out int rotation, out bool mirror)
	{
		tile = null;
		rotation = 0;
		mirror = false;

		if (list == null || list.Length == 0)
		{
			return false;
		}

		List<Vector2Int> required = new List<Vector2Int>();

		AddRequiredCells(entry, required);
		AddRequiredCells(exit, required);

		bool hasFallback = false;
		int start = _random.Next(list.Length);

		for (int i = 0; i < list.Length; i++)
		{
			CRoadTileEntry candidate = list[(start + i) % list.Length];

			if (candidate == null || candidate.Prefab == null)
			{
				continue;
			}

			for (int m = 0; m < 2; m++)
			{
				for (int r = 0; r < 4; r++)
				{
					int ma = MapDir(a, r, m == 1);
					int mb = MapDir(b, r, m == 1);

					if (!((ma == entry && mb == exit) || (ma == exit && mb == entry)))
					{
						continue;
					}

					if (Covers(candidate, r, m == 1, required))
					{
						tile = candidate;
						rotation = r;
						mirror = m == 1;
						return true;
					}

					if (!hasFallback)
					{
						tile = candidate;
						rotation = r;
						mirror = m == 1;
						hasFallback = true;
					}
				}
			}
		}

		if (hasFallback)
		{
			CPrint.Warn($"CRoadPath : {entry} → {exit} 꼭짓점 옆 칸을 덮는 타일이 없습니다. 바닥이 빌 수 있습니다.");
		}

		return hasFallback;
	}

	private static bool Covers(CRoadTileEntry tile, int rotation, bool mirror, List<Vector2Int> required)
	{
		for (int i = 0; i < required.Count; i++)
		{
			bool found = false;

			for (int x = tile.Min.x; x <= tile.Max.x && !found; x++)
			{
				for (int y = tile.Min.y; y <= tile.Max.y && !found; y++)
				{
					found = MapCell(new Vector2Int(x, y), rotation, mirror) == required[i];
				}
			}

			if (!found)
			{
				return false;
			}
		}

		return true;
	}

	private static int MapDir(int dir, int rotation, bool mirror)
	{
		int d = mirror ? (8 - dir) % 8 : dir;

		return (d + rotation * 2) % 8;
	}

	private static Vector2Int MapCell(Vector2Int cell, int rotation, bool mirror)
	{
		Vector2Int c = mirror ? new Vector2Int(-cell.x, cell.y) : cell;

		for (int i = 0; i < rotation; i++)
		{
			c = new Vector2Int(c.y, -c.x);
		}

		return c;
	}

	private void AddCurve(Vector2Int cell, int entry, int exit)
	{
		float half = _tileSet.CellSize * 0.5f;
		Vector3 center = CellToLocal(cell);
		Vector3 p0 = center + new Vector3(DIRS[entry].x, 0f, DIRS[entry].y) * half;
		Vector3 p1 = center + new Vector3(DIRS[exit].x, 0f, DIRS[exit].y) * half;

		if (_points.Count == 0)
		{
			_points.Add(p0);
			_arcs.Add(0f);
		}

		for (int i = 1; i <= CURVE_SAMPLES; i++)
		{
			float t = i / (float)CURVE_SAMPLES;
			float u = 1f - t;
			Vector3 point = u * u * p0 + 2f * u * t * center + t * t * p1;

			_frontArc += Vector3.Distance(_points[_points.Count - 1], point);
			_points.Add(point);
			_arcs.Add(_frontArc);
		}
	}
	#endregion

	#region 갈림길
	private bool IsForkReached()
	{
		return _forkCount >= 2 && _heading == 0 && _frontArc >= _forkArc;
	}

	private void PlaceFork()
	{
		int[] exits = _forkCount == 2 ? FORK_EXITS_2 : FORK_EXITS_3;
		bool threeWay = exits.Length == 3;
		CChain main = SaveChain(new CChain { Points = _points, Arcs = _arcs, Heights = _heights });

		_forkStartArc = _frontArc;

		CPlaced junction = PlaceJunction(threeWay ? _tileSet.Fork3 : _tileSet.Fork2);

		if (junction != null && threeWay)
		{
			FilterSubMeshes(junction.Go, _junctionHideMaterials, false);
		}

		for (int i = 0; i < exits.Length; i++)
		{
			CChain branch = new CChain
			{
				Points = new List<Vector3>(main.Points),
				Arcs = new List<float>(main.Arcs),
				Heights = new List<float>(main.Heights),
				Cell = main.Cell,
				Heading = main.Heading,
				FrontArc = main.FrontArc
			};

			LoadChain(branch);
			_placingBranch = i;

			for (int n = 0; n < FORK_OPEN_TILES; n++)
			{
				GameObject shared = n == 0 && junction != null && !threeWay ? junction.Go : null;
				bool roadOnly = n == 0 && threeWay;

				PlaceTile(exits[i], i == 0, i == exits.Length - 1, false, shared, roadOnly, n > 0 && threeWay);

				if (roadOnly)
				{
					GameObject placed = _roadPieces[_roadPieces.Count - 1].Go;

					if (placed != null)
					{
						placed.transform.localPosition += Vector3.up * (FORK_LAYER_GAP * (i + 1));
					}
				}
			}

			while (_frontArc < _forkStartArc + _branchLength)
			{
				PlaceTile(exits[i], true, true, false);
			}

			_branches.Add(SaveChain(branch));
		}

		_placingBranch = -1;
		LoadChain(main);
		_forkCount = 0;

		DecorateBeforeFork();

		if (junction != null)
		{
			// 갈림길 칸을 다 지나간 뒤에 지운다
			junction.Arc = _forkStartArc + _tileSet.CellSize;
		}

		if (_logFork)
		{
			string tile = junction != null ? junction.Go.name : "커브 겹치기";

			if (threeWay)
			{
				tile += " + 길 바닥만 남긴 커브·직선";
			}

			CPrint.Log($"CRoadPath : {_forkStartArc:F0}m 지점에 {exits.Length}갈래 갈림길을 놓음 ({tile})");
		}
	}

	private CPlaced PlaceJunction(CRoadTileEntry[] list)
	{
		if (list == null || list.Length == 0)
		{
			return null;
		}

		int start = _random.Next(list.Length);

		for (int i = 0; i < list.Length; i++)
		{
			CRoadTileEntry tile = list[(start + i) % list.Length];

			if (tile == null || tile.Prefab == null)
			{
				continue;
			}

			CPlaced placed = new CPlaced
			{
				Go = Spawn(tile, _cell, 0, false, "Fork"),
				Cells = GetCells(tile, _cell, 0, false),
				Main = _cell,
				StartArc = _frontArc,
				Arc = _frontArc,
				EntryHeading = _heading
			};

			Occupy(placed.Cells);
			_roadPieces.Add(placed);

			return placed;
		}

		return null;
	}

	private void FilterSubMeshes(GameObject go, string[] names, bool match, string[] except = null)
	{
		MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);

		for (int i = 0; i < renderers.Length; i++)
		{
			MeshFilter filter = renderers[i].GetComponent<MeshFilter>();

			if (filter == null || filter.sharedMesh == null)
			{
				continue;
			}

			Mesh source = filter.sharedMesh;
			Material[] materials = renderers[i].sharedMaterials;
			List<int> keep = new List<int>();

			for (int s = 0; s < materials.Length && s < source.subMeshCount; s++)
			{
				if (HasName(materials[s], names) == match && !HasName(materials[s], except))
				{
					keep.Add(s);
				}
			}

			if (keep.Count == 0)
			{
				renderers[i].enabled = false;
				continue;
			}

			string key = $"{source.GetInstanceID()}:{string.Join(",", keep)}:{_forkFlatHeight}";

			if (!_filteredMeshes.TryGetValue(key, out Mesh filtered))
			{
				filtered = BuildSubMeshes(source, keep, _forkFlatHeight);
				_filteredMeshes.Add(key, filtered);
			}

			if (filtered == null)
			{
				continue;
			}

			Material[] kept = new Material[keep.Count];

			for (int k = 0; k < keep.Count; k++)
			{
				kept[k] = materials[keep[k]];
			}

			filter.sharedMesh = filtered;
			renderers[i].sharedMaterials = kept;
		}
	}

	private static bool HasName(Material material, string[] names)
	{
		if (material == null || names == null)
		{
			return false;
		}

		for (int i = 0; i < names.Length; i++)
		{
			if (!string.IsNullOrEmpty(names[i])
				&& material.name.IndexOf(names[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}

		return false;
	}

	private static Mesh BuildSubMeshes(Mesh source, List<int> keep, float maxHeight)
	{
		if (!source.isReadable)
		{
			CPrint.Warn($"CRoadPath : {source.name} 메시의 Read/Write 가 꺼져 있어 서브메시를 고르지 못합니다.");
			return null;
		}

		Mesh mesh = Instantiate(source);

		mesh.name = $"{source.name}_filtered";
		mesh.subMeshCount = keep.Count;

		for (int k = 0; k < keep.Count; k++)
		{
			mesh.SetTriangles(source.GetTriangles(keep[k]), k);
		}

		Vector3[] vertices = mesh.vertices;

		for (int v = 0; v < vertices.Length; v++)
		{
			vertices[v].y = Mathf.Min(vertices[v].y, maxHeight);
		}

		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	private void RampUp(GameObject go, int heading)
	{
		Vector3 center = CellToLocal(_cell);
		Vector3 dir = new Vector3(DIRS[heading].x, 0f, DIRS[heading].y);
		float half = dir.magnitude * _tileSet.CellSize * 0.5f;

		dir.Normalize();

		MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);

		for (int i = 0; i < filters.Length; i++)
		{
			Mesh source = filters[i].sharedMesh;
			MeshRenderer renderer = filters[i].GetComponent<MeshRenderer>();

			if (source == null || renderer == null)
			{
				continue;
			}

			if (!source.isReadable)
			{
				CPrint.Warn($"CRoadPath : {source.name} 메시의 Read/Write 가 꺼져 있어 경사로를 만들지 못합니다.");
				continue;
			}

			Material[] materials = renderer.sharedMaterials;
			bool[] ground = new bool[source.vertexCount];

			for (int s = 0; s < materials.Length && s < source.subMeshCount; s++)
			{
				if (!HasName(materials[s], _forkRampMaterials))
				{
					continue;
				}

				int[] triangles = source.GetTriangles(s);

				for (int t = 0; t < triangles.Length; t++)
				{
					ground[triangles[t]] = true;
				}
			}

			Mesh mesh = Instantiate(source);
			Vector3[] vertices = mesh.vertices;
			Transform meshTransform = filters[i].transform;

			for (int v = 0; v < vertices.Length; v++)
			{
				if (!ground[v] || vertices[v].y <= _forkFlatHeight)
				{
					continue;
				}

				Vector3 local = transform.InverseTransformPoint(meshTransform.TransformPoint(vertices[v]));
				float along = Vector3.Dot(local - center, dir);
				float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-half, half, along));

				vertices[v].y = Mathf.Lerp(_forkFlatHeight, vertices[v].y, blend);
			}

			mesh.name = $"{source.name}{RAMP_SUFFIX}";
			mesh.vertices = vertices;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			filters[i].sharedMesh = mesh;
		}
	}

	private static void DestroyRampMeshes(GameObject go)
	{
		MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);

		for (int i = 0; i < filters.Length; i++)
		{
			Mesh mesh = filters[i].sharedMesh;

			if (mesh != null && mesh.name.EndsWith(RAMP_SUFFIX))
			{
				Destroy(mesh);
			}
		}
	}

	private void OnDestroy()
	{
		foreach (Mesh mesh in _filteredMeshes.Values)
		{
			if (mesh != null)
			{
				Destroy(mesh);
			}
		}

		_filteredMeshes.Clear();
	}

	private void TryChooseBranch()
	{
		int best = FindNearestBranch(_follow.position, out _, out _, out _, out float bestSqr, out float secondSqr);

		if (best < 0 || Mathf.Sqrt(secondSqr) - Mathf.Sqrt(bestSqr) < _roadHalfWidth)
		{
			return;
		}

		LoadChain(_branches[best]);
		_straightLeft = _minStraightRun;
		_branches.Clear();
		AbandonOtherBranches(best);

		if (_logFork)
		{
			CPrint.Log($"CRoadPath : 갈림길에서 {best}번 갈래 선택 (왼쪽부터 0)");
		}

		OnBranchChosen?.Invoke(best);
	}

	// 고르지 않은 갈래는 뒤에서 지워질 때까지 보이게 두되, 다시 깔기에서는 뺀다
	//  ㄴ 안 빼면 가운데 갈래를 골랐을 때 왼쪽 갈래 타일부터 잘라 마차 밑 길까지 지운다
	private void AbandonOtherBranches(int chosen)
	{
		for (int i = 0; i < _roadPieces.Count; i++)
		{
			CPlaced piece = _roadPieces[i];

			if (piece.Branch < 0)
			{
				continue;
			}

			piece.Abandoned = piece.Branch != chosen;
			piece.Branch = -1;
		}
	}

	private CChain SaveChain(CChain chain)
	{
		chain.Cell = _cell;
		chain.Heading = _heading;
		chain.StraightLeft = _straightLeft;
		chain.FrontArc = _frontArc;

		return chain;
	}

	private void LoadChain(CChain chain)
	{
		_points = chain.Points;
		_arcs = chain.Arcs;
		_heights = chain.Heights;
		_cell = chain.Cell;
		_heading = chain.Heading;
		_straightLeft = chain.StraightLeft;
		_frontArc = chain.FrontArc;
	}

	private bool TruncateFrom(float arc)
	{
		if (_branches.Count > 0 || _frontArc <= arc)
		{
			return false;
		}

		int cut = _roadPieces.FindIndex(p => !p.Abandoned && p.StartArc >= arc);

		if (cut < 0)
		{
			return false;
		}

		CPlaced first = _roadPieces[cut];

		for (int i = _roadPieces.Count - 1; i >= cut; i--)
		{
			if (_roadPieces[i].Abandoned)
			{
				continue;
			}

			DestroyPiece(_roadPieces[i]);
			_roadPieces.RemoveAt(i);
		}

		_cell = first.Main;
		_heading = first.EntryHeading;
		_frontArc = first.StartArc;
		_frontY = _cell.y;
		_straightLeft = 0;

		int keep = _arcs.FindLastIndex(a => a <= first.StartArc + 0.001f) + 1;

		_points.RemoveRange(keep, _points.Count - keep);
		_arcs.RemoveRange(keep, _arcs.Count - keep);

		if (_heights.Count > keep)
		{
			_heights.RemoveRange(keep, _heights.Count - keep);
		}

		int refillY = _cell.y - 2;

		for (int i = _decorPieces.Count - 1; i >= 0; i--)
		{
			if (!_decorPieces[i].Cells.Exists(c => c.y >= _cell.y - 1))
			{
				continue;
			}

			refillY = Mathf.Min(refillY, _decorPieces[i].Main.y - 1);
			DestroyPiece(_decorPieces[i]);
			_decorPieces.RemoveAt(i);
		}

		for (int i = 0; i < _roadPieces.Count; i++)
		{
			if (_roadPieces[i].Main.y >= refillY)
			{
				_roadPieces[i].Decorated = false;
			}
		}

		if (_logState)
		{
			CPrint.Log($"CRoadPath : 갈림길을 넣으려고 {first.StartArc:F0}m 뒤 길을 다시 깝니다.");
		}

		return true;
	}
	#endregion

	#region 숲 장식
	private void FillDecor(Vector2Int around, float arc)
	{
		CRoadTileEntry[] decor = _tileSet.Decor;

		if (decor == null || decor.Length == 0)
		{
			return;
		}

		int limitY = _frontY - 2;

		for (int dy = -1; dy <= 1; dy++)
		{
			for (int dx = -_decorRadius; dx <= _decorRadius; dx++)
			{
				Vector2Int cell = new Vector2Int(around.x + dx, around.y + dy);

				if (cell.y > limitY || IsOccupied(cell))
				{
					continue;
				}

				TryPlaceDecor(cell, limitY, around, arc);
			}
		}
	}

	private void TryPlaceDecor(Vector2Int cell, int limitY, Vector2Int owner, float arc)
	{
		CRoadTileEntry[] decor = _tileSet.Decor;
		int start = _random.Next(decor.Length);

		for (int i = 0; i < decor.Length; i++)
		{
			CRoadTileEntry tile = decor[(start + i) % decor.Length];

			if (tile == null || tile.Prefab == null)
			{
				continue;
			}

			int rotation = _random.Next(4);
			List<Vector2Int> cells = GetCells(tile, cell, rotation, false);

			if (!CanUse(cells, limitY, owner))
			{
				continue;
			}

			GameObject go = Spawn(tile, cell, rotation, false, "Decor");

			Occupy(cells);
			_decorPieces.Add(new CPlaced { Go = go, Cells = cells, Arc = arc, Main = cell });
			return;
		}
	}

	private bool CanUse(List<Vector2Int> cells, int limitY, Vector2Int owner)
	{
		for (int i = 0; i < cells.Count; i++)
		{
			if (cells[i].y > limitY || IsOccupied(cells[i]) || Mathf.Abs(cells[i].x - owner.x) > _decorRadius + 1)
			{
				return false;
			}
		}

		return true;
	}
	#endregion

	#region 칸 관리
	private List<Vector2Int> GetCells(CRoadTileEntry tile, Vector2Int main, int rotation, bool mirror)
	{
		List<Vector2Int> cells = new List<Vector2Int>();

		for (int x = tile.Min.x; x <= tile.Max.x; x++)
		{
			for (int y = tile.Min.y; y <= tile.Max.y; y++)
			{
				cells.Add(main + MapCell(new Vector2Int(x, y), rotation, mirror));
			}
		}

		return cells;
	}

	private bool IsOccupied(Vector2Int cell)
	{
		return _occupied.TryGetValue(cell, out int count) && count > 0;
	}

	private void Occupy(List<Vector2Int> cells)
	{
		for (int i = 0; i < cells.Count; i++)
		{
			_occupied.TryGetValue(cells[i], out int count);
			_occupied[cells[i]] = count + 1;
		}
	}

	private void Release(List<Vector2Int> cells)
	{
		for (int i = 0; i < cells.Count; i++)
		{
			if (!_occupied.TryGetValue(cells[i], out int count))
			{
				continue;
			}

			if (count <= 1)
			{
				_occupied.Remove(cells[i]);
			}
			else
			{
				_occupied[cells[i]] = count - 1;
			}
		}
	}

	// 마차 뒤로 _behindDistance 이상 지난 타일 지우기
	private bool RemoveBehind()
	{
		float limit = _followArc - _behindDistance;
		bool removed = RemoveBefore(_roadPieces, limit);

		_straightZones.RemoveAll(zone => zone.y < limit);

		// 장식은 옆에 있어 카메라에 더 오래 보인다 → 조금 더 늦게 지운다
		removed |= RemoveBefore(_decorPieces, limit - _tileSet.CellSize * 2f);

		// 길 가운데 선도 앞부분을 정리
		int cut = 0;

		while (cut < _arcs.Count - 2 && _arcs[cut + 1] < limit - _behindDistance)
		{
			cut++;
		}

		if (cut > 30)
		{
			_points.RemoveRange(0, cut);
			_arcs.RemoveRange(0, cut);
			_heights.RemoveRange(0, Mathf.Min(cut, _heights.Count));
		}

		return removed;
	}

	private bool RemoveBefore(List<CPlaced> list, float limit)
	{
		bool removed = false;

		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i].Arc >= limit)
			{
				continue;
			}

			DestroyPiece(list[i]);
			list.RemoveAt(i);
			removed = true;
		}

		return removed;
	}

	private void DestroyPiece(CPlaced piece)
	{
		Release(piece.Cells);

		if (piece.Go != null)
		{
			DestroyRampMeshes(piece.Go);
			Destroy(piece.Go);
		}

		if (piece.Walls != null)
		{
			Destroy(piece.Walls);
		}

		if (piece.Floor != null)
		{
			Destroy(piece.Floor);
		}
	}
	#endregion

	#region 생성 도우미
	private GameObject Spawn(CRoadTileEntry tile, Vector2Int cell, int rotation, bool mirror, string prefix)
	{
		GameObject go = Instantiate(tile.Prefab, transform);

		go.name = $"{prefix}_{_placedCount}_{tile.Prefab.name}";
		go.transform.localPosition = CellToLocal(cell);
		go.transform.localRotation = Quaternion.Euler(0f, rotation * 90f, 0f);
		go.transform.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f);

		return go;
	}

	private static void DisableColliders(GameObject go)
	{
		Collider[] colliders = go.GetComponentsInChildren<Collider>(true);

		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = false;
		}
	}

	// 바퀴(WheelCollider)가 밟을 수 있게 길 타일에는 충돌체를 붙인다
	private static void AddColliders(GameObject go)
	{
		MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);

		for (int i = 0; i < filters.Length; i++)
		{
			if (filters[i].sharedMesh == null)
			{
				continue;
			}

			MeshCollider existing = filters[i].GetComponent<MeshCollider>();

			// 충돌체는 있는데 메시가 빠져 있으면 바닥이 없는 것과 같다 → 보이는 메시로라도 채운다
			if (existing != null && existing.sharedMesh == null)
			{
				CPrint.Warn($"CRoadPath : {go.name} 충돌 메시가 비어 있어 보이는 메시로 대신합니다. 프리팹의 MeshCollider 메시를 채우세요.");
				existing.sharedMesh = filters[i].sharedMesh;
				continue;
			}

			if (filters[i].GetComponent<Collider>() != null)
			{
				continue;
			}

			filters[i].gameObject.AddComponent<MeshCollider>().sharedMesh = filters[i].sharedMesh;
		}
	}

	private GameObject BuildWalls(int from, int index, bool left, bool right)
	{
		if (!_buildWalls || (!left && !right) || _points.Count - from < 2)
		{
			return null;
		}

		if (_wallMaterial == null)
		{
			_wallMaterial = new PhysicMaterial("RoadWall")
			{
				bounciness = 0f,
				dynamicFriction = 0f,
				staticFriction = 0f,
				bounceCombine = PhysicMaterialCombine.Minimum,
				frictionCombine = PhysicMaterialCombine.Minimum
			};
		}

		GameObject root = new GameObject($"Walls_{index}");

		root.transform.SetParent(transform, false);

		float side = _roadHalfWidth + _wallOffset + _wallThickness * 0.5f;

		for (int i = from; i < _points.Count - 1; i++)
		{
			Vector3 a = _points[i];
			Vector3 ab = _points[i + 1] - a;
			float length = ab.magnitude;

			if (length < 0.01f)
			{
				continue;
			}

			Vector3 dir = ab / length;
			Vector3 sideDir = Vector3.Cross(Vector3.up, dir);
			Vector3 middle = a + ab * 0.5f + Vector3.up * (_wallHeight * 0.5f - 2f);
			Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
			Vector3 size = new Vector3(_wallThickness, _wallHeight, length + _wallThickness);

			if (right)
			{
				AddWall(root.transform, middle + side * sideDir, rotation, size);
			}

			if (left)
			{
				AddWall(root.transform, middle - side * sideDir, rotation, size);
			}
		}

		return root;
	}

	private GameObject BuildFloor(int from, GameObject tile, int index)
	{
		if (!_flatFloor || _points.Count - from < 2)
		{
			return null;
		}

		Collider[] colliders = tile.GetComponentsInChildren<Collider>();

		Physics.SyncTransforms();

		for (int i = _heights.Count; i < _points.Count; i++)
		{
			float sampled = SampleHeight(colliders, _points[i], i);
			float height = _heights.Count > 0 ? Mathf.Lerp(_heights[_heights.Count - 1], sampled, _floorSmoothing) : sampled;

			_heights.Add(height);
		}

		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = false;
		}

		GameObject root = new GameObject($"Floor_{index}");

		root.transform.SetParent(transform, false);

		float width = (_roadHalfWidth + _wallOffset + _wallThickness + 1f) * 2f;
		const float THICKNESS = 1f;

		for (int i = from; i < _points.Count - 1; i++)
		{
			Vector3 a = new Vector3(_points[i].x, _heights[i] + _floorLift, _points[i].z);
			Vector3 b = new Vector3(_points[i + 1].x, _heights[i + 1] + _floorLift, _points[i + 1].z);
			Vector3 ab = b - a;
			float length = ab.magnitude;

			if (length < 0.01f)
			{
				continue;
			}

			Quaternion rotation = Quaternion.LookRotation(ab / length, Vector3.up);
			GameObject floor = new GameObject("Floor");

			floor.transform.SetParent(root.transform, false);
			floor.transform.localPosition = a + ab * 0.5f - rotation * Vector3.up * (THICKNESS * 0.5f);
			floor.transform.localRotation = rotation;

			BoxCollider box = floor.AddComponent<BoxCollider>();

			box.size = new Vector3(width, THICKNESS, length + 0.5f);
		}

		return root;
	}

	private float SampleHeight(Collider[] colliders, Vector3 localPoint, int index)
	{
		Vector3 forward = index + 1 < _points.Count ? _points[index + 1] - localPoint : localPoint - _points[Mathf.Max(0, index - 1)];
		Vector3 side = forward.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, forward.normalized) * _floorSampleSide : Vector3.zero;
		float sum = 0f;
		int count = 0;

		for (int s = -1; s <= 1; s++)
		{
			Vector3 world = transform.TransformPoint(localPoint + side * s);
			Ray ray = new Ray(world + Vector3.up * 20f, Vector3.down);
			float best = float.MaxValue;

			for (int c = 0; c < colliders.Length; c++)
			{
				if (colliders[c] != null && colliders[c].enabled && colliders[c].Raycast(ray, out RaycastHit hit, 40f) && hit.distance < best)
				{
					best = hit.distance;
				}
			}

			if (best < float.MaxValue)
			{
				sum += transform.InverseTransformPoint(ray.GetPoint(best)).y;
				count++;
			}
		}

		if (count > 0)
		{
			return sum / count;
		}

		return _heights.Count > 0 ? _heights[_heights.Count - 1] : 0f;
	}

	private void AddWall(Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 size)
	{
		GameObject wall = new GameObject("Wall");

		wall.transform.SetParent(parent, false);
		wall.transform.localPosition = localPosition;
		wall.transform.localRotation = localRotation;

		BoxCollider box = wall.AddComponent<BoxCollider>();

		box.size = size;
		box.sharedMaterial = _wallMaterial;
	}

	private Vector3 CellToLocal(Vector2Int cell)
	{
		return new Vector3(cell.x, 0f, cell.y) * _tileSet.CellSize;
	}
	#endregion

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;

		for (int i = 0; i < _points.Count - 1; i++)
		{
			Gizmos.DrawLine(transform.TransformPoint(_points[i]), transform.TransformPoint(_points[i + 1]));
		}
	}
}
