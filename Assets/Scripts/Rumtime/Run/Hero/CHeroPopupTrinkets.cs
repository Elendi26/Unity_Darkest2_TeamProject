using UnityEngine;
using UnityEngine.UI;

// 영웅 정보 창(HeroPopup_UI)의 장신구 두 칸(Accessories/Acce 1·2)에 끌어다 놓기를 붙인다

public class CHeroPopupTrinkets : MonoBehaviour
{
	#region 상수
	private static readonly string[] SLOT_PATHS = { "Accessories/Acce 1", "Accessories/Acce 2" };
	#endregion

	#region 인스펙터
	// 칸 안쪽 여백 (칸 크기 비율)
	[SerializeField] private float _iconInset = 0.18f;
	[SerializeField] private Color _highlightColor = new Color(1f, 0.8f, 0.4f, 0.3f);
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private class CSlot
	{
		public Image Icon;
		// 빈칸 표시('add' 그림) → 끼우면 숨긴다
		public GameObject EmptyMark;
	}

	private readonly CSlot[] _slots = new CSlot[CHeroTrinkets.SLOT_COUNT];
	private HeroPopup_UI _popup;
	private string _heroId;
	private bool _isDirty = true;
	private bool _isBuilt;
	#endregion

	private void Awake()
	{
		_popup = GetComponent<HeroPopup_UI>();
		Build();
	}

	private void OnEnable()
	{
		CHeroTrinkets.OnChanged += HandleChanged;
		_isDirty = true;
	}

	private void OnDisable()
	{
		CHeroTrinkets.OnChanged -= HandleChanged;
	}

	private void Update()
	{
		// 창 안의 이전·다음 버튼으로 영웅이 바뀌면 다시 그린다
		string heroId = GetHeroId();

		if (heroId != _heroId)
		{
			_heroId = heroId;
			_isDirty = true;
		}

		if (_isDirty)
		{
			Refresh();
		}
	}

	private void HandleChanged(string heroId)
	{
		_isDirty = true;
	}

	private string GetHeroId()
	{
		CEntityBasicDataSO data = _popup != null ? _popup.CurrentHeroDataSO : null;

		return data != null ? data.HeroID : null;
	}

	private void Refresh()
	{
		_isDirty = false;

		if (!_isBuilt)
		{
			return;
		}

		for (int i = 0; i < _slots.Length; i++)
		{
			if (_slots[i] == null)
			{
				continue;
			}

			CTrinketItemData trinket = CHeroTrinkets.Get(_heroId, i);

			// 아이콘 그림은 비워 두어도 칸 전체가 놓는 곳이어야 한다
			_slots[i].Icon.sprite = trinket != null ? trinket.Icon : null;
			_slots[i].Icon.color = trinket != null && trinket.Icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);

			if (_slots[i].EmptyMark != null)
			{
				_slots[i].EmptyMark.SetActive(trinket == null);
			}
		}
	}

	#region 끌어다 놓기
	private CItemDragPayload MakeDragPayload(int slot)
	{
		CTrinketItemData trinket = CHeroTrinkets.Get(_heroId, slot);

		if (trinket == null)
		{
			return null;
		}

		return new CItemDragPayload { Source = EItemDragSource.HeroTrinket, Item = trinket, HeroId = _heroId, HeroSlot = slot };
	}

	private bool CanAccept(int slot, CItemDragPayload payload)
	{
		if (string.IsNullOrEmpty(_heroId))
		{
			return false;
		}

		switch (payload.Source)
		{
			case EItemDragSource.Inventory:
				return CHeroTrinkets.CanEquip(_heroId, slot, payload.Item as CTrinketItemData);
			case EItemDragSource.HeroTrinket:
				return payload.HeroId == _heroId && payload.HeroSlot != slot;
		}

		return false;
	}

	private void Accept(int slot, CItemDragPayload payload)
	{
		bool done = false;

		switch (payload.Source)
		{
			case EItemDragSource.Inventory:
				done = CHeroTrinkets.TryEquipFromInventory(_heroId, slot, payload.Inventory, payload.InventoryIndex);
				break;
			case EItemDragSource.HeroTrinket:
				done = CHeroTrinkets.Swap(_heroId, payload.HeroSlot, slot);
				break;
		}

		if (done && _logState)
		{
			CPrint.Log($"CHeroPopupTrinkets : {_heroId} {slot + 1}번 칸에 {payload.Item.DisplayName}");
		}
	}
	#endregion

	#region 만들기
	private void Build()
	{
		for (int i = 0; i < SLOT_PATHS.Length && i < _slots.Length; i++)
		{
			RectTransform root = transform.Find(SLOT_PATHS[i]) as RectTransform;

			if (root == null)
			{
				CPrint.Warn($"CHeroPopupTrinkets : 영웅 정보 창에서 장신구 칸 '{SLOT_PATHS[i]}' 를 찾지 못했습니다.");
				continue;
			}

			_slots[i] = BuildSlot(root, i);
		}

		_isBuilt = true;
	}

	private CSlot BuildSlot(RectTransform root, int index)
	{
		CSlot slot = new CSlot();

		// 칸 이름이 'add' 로 시작하는 자식 = 빈칸 표시
		for (int i = 0; i < root.childCount; i++)
		{
			if (root.GetChild(i).name.StartsWith("add"))
			{
				slot.EmptyMark = root.GetChild(i).gameObject;
				break;
			}
		}

		// 아이콘 겸 누르는 자리 (칸 전체를 덮는 투명 그림)
		Image icon = CUIBuilder.CreateImage("TrinketIcon", root, null, new Color(1f, 1f, 1f, 0f));

		icon.raycastTarget = true;
		icon.preserveAspect = true;
		icon.rectTransform.anchorMin = new Vector2(_iconInset, _iconInset);
		icon.rectTransform.anchorMax = new Vector2(1f - _iconInset, 1f - _iconInset);
		icon.rectTransform.offsetMin = Vector2.zero;
		icon.rectTransform.offsetMax = Vector2.zero;
		slot.Icon = icon;

		Image highlight = CUIBuilder.CreateImage("DropHighlight", root, null, _highlightColor);

		highlight.rectTransform.anchorMin = Vector2.zero;
		highlight.rectTransform.anchorMax = Vector2.one;
		highlight.rectTransform.offsetMin = Vector2.zero;
		highlight.rectTransform.offsetMax = Vector2.zero;

		int captured = index;
		CItemDropZone zone = icon.gameObject.AddComponent<CItemDropZone>();

		zone.Setup(payload => CanAccept(captured, payload), payload => Accept(captured, payload), highlight);

		CItemDragSource source = icon.gameObject.AddComponent<CItemDragSource>();

		source.Setup(() => MakeDragPayload(captured));

		return slot;
	}
	#endregion
}
