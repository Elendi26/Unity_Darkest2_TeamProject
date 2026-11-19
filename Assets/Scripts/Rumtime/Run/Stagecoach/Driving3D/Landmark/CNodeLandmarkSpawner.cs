using System.Collections.Generic;
using UnityEngine;

// 노드에 도착하는 지점 길 오른쪽에 그 노드에 맞는 건축물을 세운다
public class CNodeLandmarkSpawner : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CRoadPath _path;
	[SerializeField] private CCoach3DView _coach;
	// 길 가장자리에서 건물 중심까지 거리 (m)
	[SerializeField] private float _sideOffset = 7f;
	// 도착 지점에서 앞으로 얼마나 밀어 둘지 (+면 도착 지점보다 앞)
	[SerializeField] private float _forwardOffset = 0f;
	[SerializeField] private float _scale = 1.2f;
	// 건물 앞면(+Z)이 길을 보게 세운 뒤 더 돌릴 각도 → 모델 앞 방향이 다르면 여기서 돌린다
	[SerializeField] private float _extraYaw = 0f;
	// 땅 높이에서 위(+)/아래(-)로 얼마나 띄울지
	[SerializeField] private float _heightOffset = 0f;
	// 건물이 선 칸 말고도 나무·풀을 숨길 반경 (0 이면 건물이 선 칸만, 땅은 남는다)
	[SerializeField] private float _treeClearRadius = 0f;
	// 장식의 나무·풀 서브메시에 씌울 아무것도 안 그리는 재질
	[SerializeField] private Material _hiddenMaterial;
	// 이름에 이 글자가 들어간 재질은 땅으로 보고 남긴다
	[SerializeField] private string[] _groundKeywords = { "ground", "tiling" };
	// 장면 메시 중 이 높이(길 기준) 아래의 평평한 면은 바닥, 위로 솟은 면은 건물·나무로 본다
	[SerializeField] private float _sceneGroundMaxHeight = 1.5f;
	// 장면 건물·나무 자리를 몇 점(2.5m) 더 넓혀 볼지
	[SerializeField] private int _sceneObjectPadding = 0;
	[SerializeField] private float _despawnBehind = 60f;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private const float CLEAR_INTERVAL = 0.5f;

	private class CLandmark
	{
		public GameObject Prefab;
		public string Name;
		public float Arc;
		public GameObject Go;
		public bool IsTile;
		public Bounds TileBounds;
		public bool TileKeepGround;
		public bool TileOnRoad;
		public Bounds TileClearBounds;
		public Bounds TileRoadBounds;
		public HashSet<Vector2Int> SceneGround;
		public HashSet<Vector2Int> SceneObjects;
		// 장면 길 바닥이 덮은 자리 (만든 길을 숨길 곳)
		public HashSet<Vector2Int> SceneRoad;
	}

	private readonly List<CLandmark> _landmarks = new List<CLandmark>();
	private Transform _root;
	private float _clearTimer;
	#endregion

	private void Start()
	{
		if (_path == null)
		{
			_path = FindFirstObjectByType<CRoadPath>();
		}

		if (_coach == null)
		{
			_coach = FindFirstObjectByType<CCoach3DView>();
		}

		if (_path == null || _coach == null)
		{
			CPrint.Warn("CNodeLandmarkSpawner : 길(CRoadPath) 이나 마차(CCoach3DView) 가 없어 건축물을 세우지 않습니다.");
			enabled = false;
			return;
		}

		// 인스펙터가 비어 있으면 셰이더로 직접 만든다
		if (_hiddenMaterial == null)
		{
			Shader shader = Shader.Find("Hidden/RoadDecorHidden");

			if (shader != null)
			{
				_hiddenMaterial = new Material(shader) { name = "RoadDecorHidden (Runtime)" };
			}
		}

		if (_hiddenMaterial == null)
		{
			CPrint.Warn("CNodeLandmarkSpawner : _hiddenMaterial 이 비어 있어 건물 주변 나무를 숨기지 않습니다.");
		}

		_root = new GameObject("NodeLandmarks").transform;
		_root.SetParent(transform, false);
	}

	// 노드 도착 지점에 건축물 예약 → CDriving3DScene 이 출발할 때 부른다
	public void Request(CNodeTypeData node, float arriveArc)
	{
		if (node == null || node.LandmarkPrefab == null)
		{
			return;
		}

		_landmarks.Add(new CLandmark { Prefab = node.LandmarkPrefab, Name = node.Type.ToString(), Arc = arriveArc + _forwardOffset });
	}

	// 마차와 가장 가까운 세워진 건축물 (도착 연출에서 카메라가 볼 곳) → 없으면 null
	public GameObject FindNearest(Vector3 position, float maxDistance)
	{
		GameObject nearest = null;
		float best = maxDistance * maxDistance;

		for (int i = 0; i < _landmarks.Count; i++)
		{
			GameObject go = _landmarks[i].Go;

			if (go == null)
			{
				continue;
			}

			Vector3 delta = go.transform.position - position;

			delta.y = 0f;

			if (delta.sqrMagnitude <= best)
			{
				best = delta.sqrMagnitude;
				nearest = go;
			}
		}

		return nearest;
	}

	// 마차가 도착 지점 lead(m) 앞까지 왔을 때 그 건축물 → 없으면 null
	public GameObject FindAhead(float coachArc, float lead)
	{
		for (int i = 0; i < _landmarks.Count; i++)
		{
			CLandmark landmark = _landmarks[i];

			if (landmark.Go == null)
			{
				continue;
			}

			if (coachArc >= landmark.Arc - lead && coachArc <= landmark.Arc)
			{
				return landmark.Go;
			}
		}

		return null;
	}

	private void Update()
	{
		if (_path == null || _coach == null)
		{
			return;
		}

		if (!_path.TryProject(_coach.transform.position, out float coachArc, out _, out _))
		{
			return;
		}

		_clearTimer -= Time.deltaTime;

		bool clear = _clearTimer <= 0f;

		if (clear)
		{
			_clearTimer = CLEAR_INTERVAL;
		}

		for (int i = _landmarks.Count - 1; i >= 0; i--)
		{
			CLandmark landmark = _landmarks[i];

			// 한참 지나갔다 → 지운다
			if (landmark.Arc < coachArc - _despawnBehind)
			{
				if (landmark.Go != null)
				{
					Destroy(landmark.Go);
				}

				_landmarks.RemoveAt(i);
				continue;
			}

			if (landmark.Go == null)
			{
				TrySpawn(landmark);
				continue;
			}

			// 길이 앞으로 깔리며 새로 생긴 장식도 계속 치운다
			if (clear)
			{
				ClearAround(landmark);
			}
		}
	}

	private void TrySpawn(CLandmark landmark)
	{
		if (!_path.IsBuilt(landmark.Arc))
		{
			return;
		}

		if (!_path.TryGetPoint(landmark.Arc, out Vector3 center, out Vector3 forward))
		{
			return;
		}

		forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;

		CNodeLandmarkTile tile = landmark.Prefab.GetComponent<CNodeLandmarkTile>();

		if (tile != null)
		{
			SpawnTile(landmark, tile, center, forward);
			return;
		}

		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		Vector3 position = center + right * (_path.RoadHalfWidth + _sideOffset);

		// 길가 장식 땅 높이에 맞춘다
		position.y = _path.DecorGroundHeight + _heightOffset;

		// 길 쪽(-right)을 보게 세운다
		Quaternion rotation = Quaternion.LookRotation(-right, Vector3.up) * Quaternion.Euler(0f, _extraYaw, 0f);

		landmark.Go = Instantiate(landmark.Prefab, position, rotation, _root);
		landmark.Go.transform.localScale *= _scale;

		_path.ClearDecorAround(position, _treeClearRadius, _hiddenMaterial, _groundKeywords);

		if (_logState)
		{
			CPrint.Log($"CNodeLandmarkSpawner : {landmark.Name} 건축물을 {landmark.Arc:F0}m 지점 오른쪽에 세움");
		}
	}

	// 건축물 주변 장식을 숨긴다
	private void ClearAround(CLandmark landmark)
	{
		if (landmark.IsTile)
		{
			Transform t = landmark.Go.transform;

			// 장면 건물·나무가 선 자리만 비우고, 장면 바닥만 있는 곳은 길가 장식이 그대로 채운다
			if (landmark.SceneGround != null)
			{
				_path.ClearDecorByCoverage(landmark.SceneGround, landmark.SceneObjects, landmark.TileKeepGround, _hiddenMaterial, _groundKeywords);
			}
			else
			{
				// 메시를 못 읽으면 예전처럼 장면 상자 안을 비운다
				_path.ClearDecorInBox(t.TransformPoint(landmark.TileClearBounds.center), landmark.TileClearBounds.extents, t.rotation, landmark.TileKeepGround, _hiddenMaterial, _groundKeywords);
			}

			if (landmark.TileOnRoad)
			{
				// 장면 길이 거의 다 덮은 타일만 숨긴다 → 장면 길이 모자란 곳은 만든 길이 남아 끊기지 않게
				if (landmark.SceneRoad != null)
				{
					_path.HideRoadByCoverage(landmark.SceneRoad);
				}
				else
				{
					_path.HideRoadInBox(t.TransformPoint(landmark.TileRoadBounds.center), landmark.TileRoadBounds.extents, t.rotation);
				}
			}

			return;
		}

		_path.ClearDecorAround(landmark.Go.transform.position, _treeClearRadius, _hiddenMaterial, _groundKeywords);
	}

	private void SpawnTile(CLandmark landmark, CNodeLandmarkTile tile, Vector3 center, Vector3 forward)
	{
		if (tile.AlignToRoad)
		{
			SpawnTileOnRoad(landmark, tile, center, forward);
			return;
		}

		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		Vector3 side = tile.PlaceOnLeft ? -right : right;
		Vector3 position = center;

		// 길가 장식 땅보다 살짝 아래 → 겹치는 바닥은 장식·길이 위에 보인다
		position.y = _path.DecorGroundHeight - tile.Sink;

		landmark.Go = Instantiate(landmark.Prefab, position, Quaternion.LookRotation(side, Vector3.up), _root);
		landmark.IsTile = true;
		landmark.TileKeepGround = !tile.HideDecorGround;

		CNodeLandmarkTile spawned = landmark.Go.GetComponent<CNodeLandmarkTile>();

		landmark.TileBounds = GetLocalBounds(landmark.Go.transform, spawned.BoundsRoot);

		// 장면 앞쪽 끝(로컬 -Z) 이 길 가장자리 + 틈에 오게 바깥쪽으로 민다
		float push = _path.RoadHalfWidth + tile.RoadGap - landmark.TileBounds.min.z;

		// 장면 가운데(로컬 X)가 도착 지점 옆에 오게 길 방향으로 맞춘다
		Vector3 localX = Vector3.Cross(Vector3.up, side).normalized;

		landmark.Go.transform.position += side * push - localX * landmark.TileBounds.center.x;

		landmark.TileClearBounds = GetClearBounds(landmark, tile);
		BuildSceneCoverage(landmark);

		ClearAround(landmark);

		if (_logState)
		{
			CPrint.Log($"CNodeLandmarkSpawner : {landmark.Name} 노드 장면을 {landmark.Arc:F0}m 지점 길 {(tile.PlaceOnLeft ? "왼쪽" : "오른쪽")}에 놓음 (폭 {landmark.TileBounds.size.x:F0}m, 깊이 {landmark.TileBounds.size.z:F0}m)");
		}
	}

	private void SpawnTileOnRoad(CLandmark landmark, CNodeLandmarkTile tile, Vector3 center, Vector3 forward)
	{
		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		Vector3 position = center - right * tile.RoadX - forward * tile.StopZ;

		// 장면 속 길 높이를 진행 길 높이에 맞추고, 겹치는 바닥이 깜빡이지 않게 살짝 더 내린다
		position.y = center.y - tile.RoadY - tile.Sink;

		landmark.Go = Instantiate(landmark.Prefab, position, Quaternion.LookRotation(forward, Vector3.up), _root);
		landmark.IsTile = true;
		landmark.TileOnRoad = true;
		landmark.TileKeepGround = !tile.HideDecorGround;
		landmark.TileBounds = GetLocalBounds(landmark.Go.transform, landmark.Go.GetComponent<CNodeLandmarkTile>().BoundsRoot);
		landmark.TileRoadBounds = GetLocalBounds(landmark.Go.transform, landmark.Go.GetComponent<CNodeLandmarkTile>().RoadBoundsRoot);

		landmark.TileClearBounds = GetClearBounds(landmark, tile);
		BuildSceneCoverage(landmark);

		ClearAround(landmark);

		if (_logState)
		{
			CPrint.Log($"CNodeLandmarkSpawner : {landmark.Name} 노드 장면을 {landmark.Arc:F0}m 지점 길에 겹쳐 놓음 (장면 속 길 X {tile.RoadX:F0}, 정지 Z {tile.StopZ:F0})");
		}
	}

	// 놓인 장면 메시를 읽어 바닥·건물이 덮은 자리를 길 로컬 점으로 기록한다 → 못 읽으면 null (상자 방식)
	private void BuildSceneCoverage(CLandmark landmark)
	{
		CNodeLandmarkTile tile = landmark.Go.GetComponent<CNodeLandmarkTile>();
		// 장면 전체 메시를 읽는다 → ClearRoot 는 장면마다 소품 덩어리 하나만 가리키기도 해서 쓰지 않는다
		Transform source = landmark.Go.transform;
		HashSet<Vector2Int> ground = new HashSet<Vector2Int>();
		HashSet<Vector2Int> objects = new HashSet<Vector2Int>();

		bool isBuilt = CNodeGroundCoverage.TryBuild
			(
			source,
			_path.transform,
			_path.CoverageSampleSize,
			_sceneGroundMaxHeight,
			ground,
			objects,
			_sceneObjectPadding
			);

		if (!isBuilt)
		{
			CPrint.Warn($"CNodeLandmarkSpawner : {landmark.Name} 장면 메시가 Read/Write 꺼짐이라 상자 범위로 장식을 지웁니다.");
			return;
		}

		landmark.SceneGround = ground;
		landmark.SceneObjects = objects;

		if (landmark.TileOnRoad && tile != null)
		{
			HashSet<Vector2Int> road = new HashSet<Vector2Int>();
			HashSet<Vector2Int> unused = new HashSet<Vector2Int>();

			if (CNodeGroundCoverage.TryBuild(tile.RoadBoundsRoot, _path.transform, _path.CoverageSampleSize, _sceneGroundMaxHeight, road, unused))
			{
				landmark.SceneRoad = road;
			}
		}

		if (_logState)
		{
			CPrint.Log($"CNodeLandmarkSpawner : {landmark.Name} 장면이 덮은 자리 바닥 {ground.Count}점 · 건물·나무 {objects.Count}점");
		}
	}

	private static Bounds GetClearBounds(CLandmark landmark, CNodeLandmarkTile prefabTile)
	{
		// 프리팹 쪽 참조는 프리팹 안을 가리킨다 → 놓인 장면의 컴포넌트로 다시 찾는다
		CNodeLandmarkTile tile = landmark.Go.GetComponent<CNodeLandmarkTile>();

		if (tile == null)
		{
			tile = prefabTile;
		}

		if (tile.ClearRoot == null)
		{
			return new Bounds(landmark.TileBounds.center, landmark.TileBounds.size * tile.ClearScale);
		}

		Bounds bounds = GetLocalBounds(landmark.Go.transform, tile.ClearRoot);

		bounds.Expand(tile.ClearMargin * 2f);

		return bounds;
	}

	// source 아래 메시들을 root 로컬 좌표로 감싼 상자
	private static Bounds GetLocalBounds(Transform root, Transform source)
	{
		MeshFilter[] filters = source.GetComponentsInChildren<MeshFilter>(true);
		Bounds bounds = new Bounds();
		bool hasBounds = false;

		for (int i = 0; i < filters.Length; i++)
		{
			Mesh mesh = filters[i].sharedMesh;

			if (mesh == null)
			{
				continue;
			}

			Bounds b = mesh.bounds;

			for (int c = 0; c < 8; c++)
			{
				Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
				Vector3 local = root.InverseTransformPoint(filters[i].transform.TransformPoint(corner));

				if (!hasBounds)
				{
					bounds = new Bounds(local, Vector3.zero);
					hasBounds = true;
				}
				else
				{
					bounds.Encapsulate(local);
				}
			}
		}

		return bounds;
	}
}
