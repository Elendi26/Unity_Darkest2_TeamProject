using System.Collections.Generic;
using UnityEngine;

// 노드에 도착하는 지점 길 오른쪽에 그 노드에 맞는 건축물을 세운다
//  ㄴ 출발할 때 도착 지점(arc)을 받아 두고 → 그 자리 길이 깔리면 세운다
//  ㄴ 마차가 한참 지나가면 지운다
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
	//  ㄴ 도착 직전에 카메라를 미리 돌리려고 쓴다
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
				_path.ClearDecorAround(landmark.Go.transform.position, _treeClearRadius, _hiddenMaterial, _groundKeywords);
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

		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		Vector3 position = center + right * (_path.RoadHalfWidth + _sideOffset);

		// 길가 장식 땅 높이에 맞춘다
		//  ㄴ 장식 타일에는 충돌체가 없어 레이로는 못 찾는다 → 길 바닥(움푹한 곳) 높이에 서면 땅에 파묻힌다
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
}
