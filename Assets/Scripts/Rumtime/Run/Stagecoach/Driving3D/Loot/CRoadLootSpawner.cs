using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CRoadLootSpawner : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CRoadPath _path;
	[SerializeField] private CCoach3DView _coach;
	[SerializeField] private CRoadLootBush[] _bushPrefabs;
	[SerializeField] private CRoadLootEntry[] _entries =
	{
		new CRoadLootEntry { Label = "금화 조금", Weight = 6, Gold = new Vector2Int(3, 8) },
		new CRoadLootEntry { Label = "금화 많이", Weight = 2, Gold = new Vector2Int(12, 20) },
	};
	[SerializeField] private float _firstDistance = 40f;
	[SerializeField] private Vector2 _spacing = new Vector2(30f, 70f);
	[SerializeField] private float _spawnAhead = 70f;
	[SerializeField] private float _despawnBehind = 30f;
	[SerializeField] private float _forkMargin = 40f;
	[SerializeField, Range(0f, 1f)] private float _sideRange = 0.7f;
	[SerializeField] private float _hitRadius = 1.6f;
	[SerializeField] private Vector2 _bushScale = new Vector2(0.8f, 1.2f);
	[SerializeField] private LayerMask _groundMask = ~0;
	[SerializeField] private bool _showPopup = true;
	[SerializeField] private Font _popupFont;
	[SerializeField] private Color _goldPopupColor = new Color(0.95f, 0.78f, 0.35f, 1f);
	[SerializeField] private Color _itemPopupColor = new Color(0.75f, 0.9f, 1f, 1f);
	[SerializeField] private bool _logState = true;
	[SerializeField] private bool _logSpawn = false;
	#endregion

	#region 내부 변수
	private const float VALIDATE_INTERVAL = 0.5f;
	private const float GROUND_PROBE = 6f;
	private readonly List<CRoadLootBush> _bushes = new List<CRoadLootBush>();
	private CCoachPhysics _physics;
	private Transform _root;
	private float _nextArc = -1f;
	private float _validateTimer;
	#endregion

	private void Start()
	{
		if (_controller == null)
		{
			_controller = FindFirstObjectByType<CStagecoachController>();
		}

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
			CPrint.Warn("CRoadLootSpawner : 길(CRoadPath) 이나 마차(CCoach3DView) 가 없어 수풀을 놓지 않습니다.");
			enabled = false;
			return;
		}

		if (_popupFont == null)
		{
			Text text = FindFirstObjectByType<Text>(FindObjectsInactive.Include);

			_popupFont = text != null ? text.font : null;
		}

		_root = new GameObject("RoadLoot").transform;
		_root.SetParent(transform, false);
	}

	private void Update()
	{
		if (!_path.TryProject(_coach.transform.position, out float coachArc, out _, out _))
		{
			return;
		}

		TickSpawn(coachArc);
		TickHit();
		TickCleanup(coachArc);
	}

	#region 놓기
	private void TickSpawn(float coachArc)
	{
		if (_nextArc < 0f)
		{
			_nextArc = coachArc + _firstDistance;
		}

		float limit = coachArc + _spawnAhead;

		while (_nextArc <= limit)
		{
			// 갈림길 근처면 이번 자리는 건너뛴다 → 갈래를 고른 뒤 다시 앞쪽부터 놓는다
			if (!_path.IsStableArc(_nextArc, _forkMargin))
			{
				_nextArc = Mathf.Max(_nextArc, coachArc + _firstDistance);
				return;
			}

			TrySpawn(_nextArc);
			_nextArc += Random.Range(_spacing.x, Mathf.Max(_spacing.x, _spacing.y));
		}
	}

	private void TrySpawn(float arc)
	{
		if (!_path.TryGetPoint(arc, out Vector3 center, out Vector3 forward))
		{
			return;
		}

		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		float lateral = Random.Range(-1f, 1f) * _path.RoadHalfWidth * _sideRange;
		Vector3 position = center + right * lateral;

		// 길 바닥 높이에 맞춘다
		if (Physics.Raycast(position + Vector3.up * GROUND_PROBE, Vector3.down, out RaycastHit hit, GROUND_PROBE * 2f, _groundMask, QueryTriggerInteraction.Ignore))
		{
			position = hit.point;
		}

		CRoadLootBush prefab = PickPrefab();

		if (prefab == null)
		{
			CPrint.Once("RoadLoot.NoPrefab", "CRoadLootSpawner : 수풀 프리팹(_bushPrefabs)이 비어 있어 수풀을 놓지 않습니다.");
			return;
		}

		CRoadLootBush bush = Instantiate(prefab, _root);

		bush.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
		bush.transform.localScale *= Random.Range(_bushScale.x, Mathf.Max(_bushScale.x, _bushScale.y));
		bush.Arc = arc;
		_bushes.Add(bush);

		if (_logSpawn)
		{
			CPrint.Log($"CRoadLootSpawner : {arc:F0}m 지점에 수풀 (좌우 {lateral:F1}m)");
		}
	}

	// 비어 있는 칸은 빼고 고른다 → 하나도 없으면 null (수풀을 놓지 않음)
	private CRoadLootBush PickPrefab()
	{
		if (_bushPrefabs == null || _bushPrefabs.Length == 0)
		{
			return null;
		}

		int start = Random.Range(0, _bushPrefabs.Length);

		for (int i = 0; i < _bushPrefabs.Length; i++)
		{
			CRoadLootBush prefab = _bushPrefabs[(start + i) % _bushPrefabs.Length];

			if (prefab != null)
			{
				return prefab;
			}
		}

		return null;
	}
	#endregion

	#region 부딪힘
	private void TickHit()
	{
		if (_physics == null)
		{
			_physics = _coach.GetComponent<CCoachPhysics>();
		}

		CHorseTeam team = _physics != null ? _physics.Team : null;

		for (int i = 0; i < _bushes.Count; i++)
		{
			CRoadLootBush bush = _bushes[i];

			if (bush == null || bush.IsCollected)
			{
				continue;
			}

			float reach = _hitRadius + bush.Radius;
			bool isHit = IsNear(_coach.transform.position, bush.transform.position, reach);

			if (!isHit && team != null)
			{
				isHit = IsNear(team.transform.position, bush.transform.position, reach);
			}

			if (isHit)
			{
				Collect(bush);
			}
		}
	}

	private static bool IsNear(Vector3 a, Vector3 b, float reach)
	{
		Vector2 delta = new Vector2(a.x - b.x, a.z - b.z);

		return delta.sqrMagnitude <= reach * reach;
	}

	private void Collect(CRoadLootBush bush)
	{
		bush.Collect();
		_coach.Bump();

		CRoadLootEntry entry = Roll();
		bool isItem = false;
		string message = entry != null ? Give(entry, out isItem) : null;

		// 꽝 → 수풀만 터지고 글자는 띄우지 않는다
		if (string.IsNullOrEmpty(message))
		{
			if (_logState)
			{
				CPrint.Log("CRoadLootSpawner : 빈 수풀");
			}

			return;
		}

		if (_logState)
		{
			CPrint.Log($"CRoadLootSpawner : 수풀에서 {message}");
		}

		if (_showPopup)
		{
			CRoadLootPopup.Show(bush.transform.position + Vector3.up * 3f, message, isItem ? _itemPopupColor : _goldPopupColor, _popupFont);
		}
	}

	private CRoadLootEntry Roll()
	{
		int total = 0;

		for (int i = 0; i < _entries.Length; i++)
		{
			if (_entries[i] != null && _entries[i].Weight > 0)
			{
				total += _entries[i].Weight;
			}
		}

		if (total <= 0)
		{
			return null;
		}

		int pick = Random.Range(0, total);

		for (int i = 0; i < _entries.Length; i++)
		{
			if (_entries[i] == null || _entries[i].Weight <= 0)
			{
				continue;
			}

			pick -= _entries[i].Weight;

			if (pick < 0)
			{
				return _entries[i];
			}
		}

		return null;
	}

	// 보상을 주고 띄울 글자를 돌려준다
	private string Give(CRoadLootEntry entry, out bool isItem)
	{
		isItem = false;

		if (_controller == null || _controller.RunData == null)
		{
			CPrint.Warn("CRoadLootSpawner : 런 데이터가 없어 보상을 줄 수 없습니다.");
			return null;
		}

		int gold = entry.Gold.y > 0 ? Random.Range(entry.Gold.x, Mathf.Max(entry.Gold.x, entry.Gold.y) + 1) : 0;
		string itemText = null;

		if (entry.Item != null && entry.ItemCount > 0)
		{
			CInventory inventory = _controller.Stagecoach != null ? _controller.Stagecoach.Inventory : null;
			int added = inventory != null ? inventory.Add(entry.Item, entry.ItemCount) : 0;

			if (added > 0)
			{
				isItem = true;
				itemText = added > 1 ? $"{entry.Item.DisplayName} x{added}" : entry.Item.DisplayName;
			}
			else
			{
				// 짐칸이 꽉 찼다 → 금화로 바꿔 준다
				gold += entry.FullGold;
				itemText = "짐칸이 가득 참";
			}
		}

		int gained = gold > 0 ? _controller.RunData.Add(ERunValue.Gold, gold) : 0;
		string goldText = gained > 0 ? $"금화 +{gained}" : null;

		if (itemText != null && goldText != null)
		{
			return $"{itemText}\n{goldText}";
		}

		return itemText ?? goldText;
	}
	#endregion

	#region 정리
	private void TickCleanup(float coachArc)
	{
		_validateTimer -= Time.deltaTime;

		bool validate = _validateTimer <= 0f;

		if (validate)
		{
			_validateTimer = VALIDATE_INTERVAL;
		}

		for (int i = _bushes.Count - 1; i >= 0; i--)
		{
			CRoadLootBush bush = _bushes[i];

			// 터진 수풀은 스스로 사라진다
			if (bush == null || bush.IsCollected)
			{
				_bushes.RemoveAt(i);
				continue;
			}

			bool isBehind = bush.Arc < coachArc - _despawnBehind;
			bool isOffRoad = validate && IsOffRoad(bush.transform.position);

			if (!isBehind && !isOffRoad)
			{
				continue;
			}

			// 길이 다시 깔려 허공에 남은 수풀도 지운다
			if (isOffRoad && _logSpawn)
			{
				CPrint.Log($"CRoadLootSpawner : 길이 바뀌어 {bush.Arc:F0}m 수풀을 지웁니다.");
			}

			Destroy(bush.gameObject);
			_bushes.RemoveAt(i);
		}
	}

	private bool IsOffRoad(Vector3 position)
	{
		if (!_path.TryProject(position, out _, out float lateral, out _))
		{
			return true;
		}

		return Mathf.Abs(lateral) > _path.RoadHalfWidth + 1f;
	}
	#endregion
}
