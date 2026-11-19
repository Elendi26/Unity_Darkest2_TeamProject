using System.Collections.Generic;
using UnityEngine;

// 도로 이벤트 자리 길 한가운데에 물건을 놓는다
public class CRoadPropSpawner : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CRoadPath _path;
	[SerializeField] private CCoach3DView _coach;
	[SerializeField] private float _forwardOffset = 1.5f;
	[SerializeField] private float _blockerDistance = 9f;
	[SerializeField] private float _scale = 1f;
	[SerializeField] private float _heightOffset = 0f;
	[SerializeField] private float _randomYaw = 25f;
	[SerializeField] private float _despawnBehind = 60f;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private class CProp
	{
		public GameObject Prefab;
		public string Name;
		public float Arc;
		public GameObject Go;
		public bool IsBlocker;
	}

	private readonly List<CProp> _props = new List<CProp>();
	private Transform _root;
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
			CPrint.Warn("CRoadPropSpawner : 길(CRoadPath) 이나 마차(CCoach3DView) 가 없어 길 위 물건을 놓지 않습니다.");
			enabled = false;
			return;
		}

		_root = new GameObject("RoadProps").transform;
		_root.SetParent(transform, false);
	}

	public void Request(CRoadEventData data, float eventArc)
	{
		if (data == null || data.RoadPropPrefab == null)
		{
			return;
		}

		float offset = data.BlocksRoad ? _blockerDistance : _forwardOffset;

		_props.Add(new CProp { Prefab = data.RoadPropPrefab, Name = data.Id, Arc = eventArc + offset, IsBlocker = data.BlocksRoad });
	}

	private void Update()
	{
		if (_root == null || _props.Count == 0)
		{
			return;
		}

		float coachArc = _path.TryProject(_coach.transform.position, out float arc, out _, out _) ? arc : float.MinValue;

		for (int i = _props.Count - 1; i >= 0; i--)
		{
			CProp prop = _props[i];

			// 한참 지나갔다 → 지운다
			if (prop.Arc < coachArc - _despawnBehind)
			{
				if (prop.Go != null)
				{
					Destroy(prop.Go);
				}

				_props.RemoveAt(i);
				continue;
			}

			if (prop.Go == null)
			{
				TrySpawn(prop);
			}
		}
	}

	public bool OpenNearestBlocker()
	{
		if (_path == null || _coach == null)
		{
			return false;
		}

		float coachArc = _path.TryProject(_coach.transform.position, out float arc, out _, out _) ? arc : 0f;
		CRoadBarricade nearest = null;
		float nearestDistance = float.MaxValue;

		for (int i = 0; i < _props.Count; i++)
		{
			CProp prop = _props[i];

			if (!prop.IsBlocker || prop.Go == null)
			{
				continue;
			}

			CRoadBarricade barricade = prop.Go.GetComponent<CRoadBarricade>();
			float distance = Mathf.Abs(prop.Arc - coachArc);

			if (barricade == null || barricade.IsOpen || distance >= nearestDistance)
			{
				continue;
			}

			nearest = barricade;
			nearestDistance = distance;
		}

		if (nearest == null)
		{
			return false;
		}

		nearest.Open();
		return true;
	}

	private void TrySpawn(CProp prop)
	{
		if (!_path.IsBuilt(prop.Arc) || !_path.TryGetPoint(prop.Arc, out Vector3 center, out Vector3 forward))
		{
			return;
		}

		forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;

		if (forward.sqrMagnitude < 0.01f)
		{
			forward = Vector3.forward;
		}

		float yaw = prop.IsBlocker ? 0f : Random.Range(-_randomYaw, _randomYaw);
		Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, yaw, 0f);

		center.y += _heightOffset;
		prop.Go = Instantiate(prop.Prefab, center, rotation, _root);
		prop.Go.transform.localScale *= _scale;

		if (_logState)
		{
			CPrint.Log($"CRoadPropSpawner : {prop.Name} 물건을 {prop.Arc:F0}m 길 위에 놓음");
		}
	}
}
