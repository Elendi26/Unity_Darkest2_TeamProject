using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 여관 마차 정비공 → 바퀴·장갑 수리 + 마차 칸에 단 아이템 보기·떼기 (짐칸에서 '장착' 으로 단다)
public class CGarageView : MonoBehaviour, IInventoryUseHandler
{
	#region 인스펙터
	[SerializeField] private Font _font;
	// 주행 씬 HUD(10) 보다 위 → 짐칸 패널만 이보다 위로 올린다
	[SerializeField] private int _sortingOrder = 15;
	[SerializeField] private int _inventorySortingOrder = 20;
	[Header("그림")]
	// 왼쪽 정비공 그림 (상점 데이터에 그림이 없으면 이 그림)
	[SerializeField] private Sprite _portrait;
	// 정비공 뒤 작업장 실루엣
	[SerializeField] private Sprite _sideArt;
	[SerializeField] private Color _sideArtColor = new Color(0.32f, 0.3f, 0.28f, 0.55f);
	[SerializeField] private Sprite _slotBackground;
	[SerializeField] private Sprite _slotFrame;
	[SerializeField] private Sprite _lockIcon;
	// 바퀴·장갑 칸 아이콘 (비우면 CRunValueTable 아이콘)
	[SerializeField] private Sprite _wheelIcon;
	[SerializeField] private Sprite _armorIcon;
	[Header("수리")]
	// 바퀴·장갑 칸 옆 수리 버튼 → 누를 때마다 _repairAmount 칸을 _repairCurrency 로 고친다
	[SerializeField] private ERunValue _repairCurrency = ERunValue.Faction;
	[SerializeField] private int _repairCost = 8;
	[SerializeField] private int _repairAmount = 1;
	[SerializeField] private Sprite _repairIcon;
	// 수리 버튼 바탕(검은 마름모)·테두리
	[SerializeField] private Sprite _repairBackground;
	[SerializeField] private Sprite _repairBorder;
	[SerializeField] private Color _repairBorderColor = new Color(0.6f, 0.58f, 0.55f, 1f);
	// 마우스를 올리면 테두리·렌치 색
	[SerializeField] private Color _repairHoverColor = new Color(1f, 0.78f, 0.32f, 1f);
	[SerializeField] private Sprite _repairCurrencyIcon;
	[SerializeField] private string _wheelRepairInfo = "바퀴 : 험한 길을 달리거나 장애물에 부딪히면 손상됨";
	[SerializeField] private string _armorRepairInfo = "장갑 : 습격이나 화살 세례를 받으면 손상됨";
	[Header("배치")]
	[SerializeField] private int _slotCount = 4;
	[SerializeField] private float _slotSize = 112f;
	[SerializeField] private float _slotSpacing = 24f;
	// 뒤 화면(여관 메뉴·주행 씬)을 완전히 가리는 바탕
	[SerializeField] private Color _dimColor = new Color(0.016f, 0.014f, 0.013f, 1f);
	[SerializeField] private Color _textColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	[SerializeField] private Color _emptyPipColor = new Color(0.3f, 0.28f, 0.25f, 1f);
	[Header("대사")]
	[SerializeField] private string[] _lines =
	{
		"바퀴가 성해야 멀리 가는 법이지.",
		"길이 험할수록 마차도 단단해야 하네.",
		"고칠 데가 있으면 말만 하게.",
	};
	// 상점이 열리면 오른쪽 짐칸 패널도 연다 (비우면 씬에서 찾는다)
	[SerializeField] private CInventoryView _inventoryView;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private const float TITLE_CREST_SHIFT = 60f;
	private const int MAX_PIPS = 6;
	private const float PIP_SIZE = 40f;
	private const float PIP_GAP = 8f;
	private const float PIP_Y = 10f;
	private const float REPAIR_SIZE = 64f;
	// 설명 상자 높이 (가운데 기준 0.3 앵커에서) → 정비 칸 위 / 수리 버튼 위
	private const float SLOT_TOOLTIP_Y = 70f;
	private const float REPAIR_TOOLTIP_Y = 170f;

	// 수리 버튼 하나 (바퀴 또는 장갑)
	private class CRepairButton
	{
		public ERunValue Part;
		public float GroupX;
		public RectTransform Rect;
		public Button Button;
		public Image Icon;
		public Image Border;
		public Text Price;
	}

	// 마차 칸 하나 → 칸 종류 + 단 아이템 (비면 null)
	private struct SCoachSlot
	{
		public EStagecoachSlot Slot;
		public CStagecoachUpgradeData Upgrade;
	}

	private readonly List<SCoachSlot> _coachSlots = new List<SCoachSlot>();
	private CRepairButton _wheelRepair;
	private CRepairButton _armorRepair;
	private CRunData _runData;
	private ERunValue _hoveredRepair = ERunValue.None;

	private readonly List<CShopSlotView> _slots = new List<CShopSlotView>();
	private readonly List<RectTransform> _slotRects = new List<RectTransform>();
	private readonly List<GameObject> _locks = new List<GameObject>();
	private readonly Image[] _wheelPips = new Image[MAX_PIPS];
	private readonly Image[] _armorPips = new Image[MAX_PIPS];
	private CanvasGroup _group;
	private Image _portraitImage;
	private Image _crestImage;
	private Image _bannerImage;
	private Text _title;
	private Text _subtitle;
	private Text _line;
	private Text _wallet;
	private RectTransform _tooltip;
	private Text _tooltipText;
	private Vector2 _titleBase;
	private Vector2 _subtitleBase;
	private Action _onLeave;
	private CStagecoach _stagecoach;
	private int _hovered = -1;
	private bool _openedInventory;
	#endregion

	// 화면이 열려 있는지
	public bool IsOpen => _onLeave != null;

	private void Awake()
	{
		if (_font == null)
		{
			_font = CUIBuilder.GetDefaultFont();
		}

		BuildLayout();
		SetVisible(false);
	}

	private void Update()
	{
		// ESC → 떠나기
		if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			Leave();
		}
	}

	public void SetStagecoach(CStagecoach stagecoach, CRunData runData)
	{
		_stagecoach = stagecoach;
		_runData = runData;
	}

	#region 열기·닫기
	public void SetLook(Sprite portrait, Sprite crest, Sprite banner)
	{
		Sprite shown = portrait != null ? portrait : _portrait;

		_portraitImage.sprite = shown;
		_portraitImage.enabled = shown != null;

		_crestImage.sprite = crest;
		_crestImage.enabled = crest != null;

		float shift = crest != null ? TITLE_CREST_SHIFT : 0f;

		_title.rectTransform.anchoredPosition = _titleBase + new Vector2(shift, 0f);
		_subtitle.rectTransform.anchoredPosition = _subtitleBase + new Vector2(shift, 0f);

		_bannerImage.sprite = banner;
		_bannerImage.enabled = banner != null;
	}

	public void Open(string title, string body, Action onLeave)
	{
		_onLeave = onLeave;
		_title.text = title;
		_subtitle.text = body;
		_hovered = -1;
		_line.text = PickLine();

		// 달거나 떼면 칸·바퀴·장갑 최대치가 바뀐다
		if (_stagecoach != null)
		{
			_stagecoach.OnEquipped += HandleEquipChanged;
			_stagecoach.OnUnequipped += HandleEquipChanged;
		}

		SetVisible(true);
		RefreshAll();
		OpenInventory();
	}

	public void Close()
	{
		if (_stagecoach != null)
		{
			_stagecoach.OnEquipped -= HandleEquipChanged;
			_stagecoach.OnUnequipped -= HandleEquipChanged;
		}

		_onLeave = null;
		SetVisible(false);
		CloseInventory();
	}

	private void OnDestroy()
	{
		if (_stagecoach != null)
		{
			_stagecoach.OnEquipped -= HandleEquipChanged;
			_stagecoach.OnUnequipped -= HandleEquipChanged;
		}
	}

	private void HandleEquipChanged(CStagecoachUpgradeData upgrade)
	{
		RefreshAll();
	}
	#endregion

	#region 짐칸 '장착'
	public string UseLabel => "장착";

	public bool Handles(CItemDataBase item)
	{
		return item is CStagecoachUpgradeData;
	}

	public bool CanUse(int index)
	{
		return IsOpen && _stagecoach != null && _stagecoach.CanEquipFromInventory(index);
	}

	public void Use(int index)
	{
		if (!CanUse(index))
		{
			return;
		}

		CItemDataBase item = _stagecoach.Inventory.Stacks[index].Item;

		if (_stagecoach.TryEquipFromInventory(index) && _logState)
		{
			CPrint.Log($"CGarageView : {item.DisplayName} 장착");
		}
	}
	#endregion

	#region 갱신
	private void RefreshAll()
	{
		RefreshWallet();
		RefreshStats();
		RefreshSlots();
	}

	// 수리에 쓰는 재화
	private void RefreshWallet()
	{
		CRunValue currency = _runData != null ? _runData.Get(_repairCurrency) : null;

		_wallet.text = currency != null ? $"{CRunValueTable.GetName(_repairCurrency)} {currency.Current}" : string.Empty;
	}

	// 마차 칸 목록 → 단 것 먼저(단 순서대로), 남은 칸은 빈칸. 칸을 안 쓰는 것(바퀴·장갑 보강)은 바퀴·장갑 칸 수로만 보인다
	private void BuildCoachSlots()
	{
		_coachSlots.Clear();

		if (_stagecoach == null)
		{
			return;
		}

		IReadOnlyList<CStagecoachUpgradeData> equipped = _stagecoach.Equipped;
		int used = 0;

		for (int i = 0; i < equipped.Count; i++)
		{
			if (equipped[i] != null && equipped[i].Slot != EStagecoachSlot.None)
			{
				_coachSlots.Add(new SCoachSlot { Slot = equipped[i].Slot, Upgrade = equipped[i] });
				used++;
			}
		}

		for (int i = used; i < _stagecoach.GetSlotCount(); i++)
		{
			_coachSlots.Add(new SCoachSlot { Slot = EStagecoachSlot.None, Upgrade = null });
		}
	}

	// 마차 칸 줄 → 없는 칸은 자물쇠
	private void RefreshSlots()
	{
		BuildCoachSlots();

		int count = _coachSlots.Count;

		for (int i = 0; i < _slots.Count; i++)
		{
			bool hasSlot = i < count;

			_slots[i].gameObject.SetActive(hasSlot);
			_locks[i].SetActive(!hasSlot);

			if (!hasSlot)
			{
				continue;
			}

			int index = i;
			CStagecoachUpgradeData upgrade = _coachSlots[i].Upgrade;
			CShopCardInfo info = new CShopCardInfo
			{
				Name = upgrade != null ? upgrade.DisplayName : "빈 칸",
				Icon = upgrade != null ? upgrade.Icon : null,
				// 짐칸이 모자라 못 떼는 칸은 '가득 참' 으로
				State = upgrade != null && !_stagecoach.CanUnequipToInventory(upgrade) ? EShopCardState.Full : EShopCardState.Available,
			};

			_slots[i].Bind(info, _slotFrame, null, () => Unequip(index), isOver => Hover(index, isOver));
		}

		if (count > _slots.Count)
		{
			CPrint.Warn($"CGarageView : 화면 칸 {_slots.Count}개보다 마차 칸({count})이 많아 일부가 보이지 않습니다.");
		}

		RefreshTooltip();
	}

	private static string GetSlotName(EStagecoachSlot slot)
	{
		switch (slot)
		{
			case EStagecoachSlot.General:
				return "일반";
			case EStagecoachSlot.Trophy:
				return "전리품";
			case EStagecoachSlot.Pet:
				return "애완동물";
			case EStagecoachSlot.Flame:
				return "불꽃";
		}

		return "보강";
	}

	// 바퀴·장갑 → 최대치만큼 칸, 남은 만큼 밝게
	private void RefreshStats()
	{
		RefreshPips(_wheelPips, GetPart(ERunValue.Wheels), _wheelRepair);
		RefreshPips(_armorPips, GetPart(ERunValue.Armor), _armorRepair);
		RefreshTooltip();
	}

	private CRunValue GetPart(ERunValue part)
	{
		if (_stagecoach == null)
		{
			return null;
		}

		return part == ERunValue.Wheels ? _stagecoach.Wheels : _stagecoach.Armor;
	}

	// 칸(최대치만큼) + 수리 버튼을 한 줄로 가운데 정렬한다
	private void RefreshPips(Image[] pips, CRunValue value, CRepairButton repair)
	{
		int current = value != null ? value.Current : 0;
		int max = value != null ? Mathf.Min(value.Max, pips.Length) : 0;
		float width = max * PIP_SIZE + Mathf.Max(0, max - 1) * PIP_GAP + PIP_GAP * 2f + REPAIR_SIZE;
		float startX = repair.GroupX - width * 0.5f;

		for (int i = 0; i < pips.Length; i++)
		{
			pips[i].gameObject.SetActive(i < max);
			pips[i].color = i < current ? Color.white : _emptyPipColor;
			pips[i].rectTransform.anchoredPosition = new Vector2(startX + PIP_SIZE * 0.5f + i * (PIP_SIZE + PIP_GAP), PIP_Y);
		}

		repair.Rect.anchoredPosition = new Vector2(startX + width - REPAIR_SIZE * 0.5f, PIP_Y + 4f);

		bool canRepair = CanRepair(repair.Part, out bool canAfford);

		repair.Button.interactable = canRepair;
		RefreshRepairHighlight(repair);
		repair.Price.text = _repairCost.ToString();
		repair.Price.color = canAfford ? _textColor : new Color(0.85f, 0.3f, 0.25f, 1f);
	}

	// 깎인 칸이 있고 패물이 넉넉해야 고칠 수 있다
	private bool CanRepair(ERunValue part, out bool canAfford)
	{
		CRunValue value = GetPart(part);

		canAfford = _runData != null && _runData.Has(_repairCurrency, _repairCost);

		return value != null && value.Current < value.Max && canAfford;
	}

	private void Repair(ERunValue part)
	{
		if (!IsOpen || _stagecoach == null || !CanRepair(part, out _))
		{
			return;
		}

		if (!_stagecoach.TryRepair(part, _repairAmount, _repairCurrency, _repairCost))
		{
			return;
		}

		if (_logState)
		{
			CPrint.Log($"CGarageView : {CRunValueTable.GetName(part)} 수리 (-{CRunValueTable.GetName(_repairCurrency)} {_repairCost})");
		}

		RefreshWallet();
		RefreshStats();
	}

	private void HoverRepair(ERunValue part, bool isOver)
	{
		if (isOver)
		{
			_hoveredRepair = part;
		}
		else if (_hoveredRepair == part)
		{
			_hoveredRepair = ERunValue.None;
		}

		RefreshRepairHighlight(_wheelRepair);
		RefreshRepairHighlight(_armorRepair);
		RefreshTooltip();
	}

	// 마우스가 올라간 수리 버튼 → 테두리·렌치를 금색으로
	private void RefreshRepairHighlight(CRepairButton repair)
	{
		bool isHovered = repair.Part == _hoveredRepair;

		if (repair.Border != null)
		{
			repair.Border.color = isHovered ? _repairHoverColor : _repairBorderColor;
		}

		if (isHovered)
		{
			repair.Icon.color = _repairHoverColor;
		}
		else
		{
			repair.Icon.color = repair.Button.interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
		}
	}

	// 수리 버튼 위 설명 → 무엇이 깎이는지 + 가격 + 막힌 이유
	private bool ShowRepairTooltip()
	{
		if (_hoveredRepair == ERunValue.None)
		{
			return false;
		}

		CRepairButton repair = _hoveredRepair == ERunValue.Wheels ? _wheelRepair : _armorRepair;
		CRunValue value = GetPart(_hoveredRepair);
		string info = _hoveredRepair == ERunValue.Wheels ? _wheelRepairInfo : _armorRepairInfo;
		string state = string.Empty;

		if (value != null && value.Current >= value.Max)
		{
			state = $"  <color=#d9604a>({CShopView.GetStateName(EShopCardState.Full)})</color>";
		}
		else if (!CanRepair(_hoveredRepair, out bool canAfford) && !canAfford)
		{
			state = $"  <color=#d9604a>({CShopView.GetStateName(EShopCardState.TooExpensive)})</color>";
		}

		_tooltipText.text = $"<b>{CRunValueTable.GetName(_hoveredRepair)} 수리</b>  <color=#c9b27a>{CRunValueTable.GetName(_repairCurrency)} {_repairCost}</color>{state}\n<size=18>{info}</size>";
		_tooltip.anchoredPosition = new Vector2(repair.Rect.anchoredPosition.x, REPAIR_TOOLTIP_Y);
		_tooltip.gameObject.SetActive(true);
		return true;
	}

	private void Hover(int index, bool isOver)
	{
		if (isOver)
		{
			_hovered = index;
		}
		else if (_hovered == index)
		{
			_hovered = -1;
		}

		RefreshTooltip();
	}

	// 올린 칸 위에 설명 상자
	private void RefreshTooltip()
	{
		if (ShowRepairTooltip())
		{
			return;
		}

		if (_hovered < 0 || _hovered >= _coachSlots.Count || _hovered >= _slotRects.Count)
		{
			_tooltip.gameObject.SetActive(false);
			return;
		}

		SCoachSlot slot = _coachSlots[_hovered];
		string slotName = $"<color=#c9b27a>{GetSlotName(slot.Slot)}</color>";

		if (slot.Upgrade == null)
		{
			_tooltipText.text = $"<b>빈 칸</b>\n<size=18>짐칸에서 마차 아이템을 골라 '장착' 을 누르면 여기에 단다.</size>";
		}
		else
		{
			string hint = _stagecoach.CanUnequipToInventory(slot.Upgrade) ? "누르면 떼어 짐칸에 넣는다." : "<color=#d9604a>짐칸 자리가 모자라 뗄 수 없다.</color>";

			_tooltipText.text = $"<b>{slot.Upgrade.DisplayName}</b>  {slotName}\n<size=18>{slot.Upgrade.Description}\n{hint}</size>";
		}
		_tooltip.anchoredPosition = new Vector2(_slotRects[_hovered].anchoredPosition.x, SLOT_TOOLTIP_Y);
		_tooltip.gameObject.SetActive(true);
	}

	private string PickLine()
	{
		if (_lines == null || _lines.Length == 0)
		{
			return string.Empty;
		}

		return _lines[UnityEngine.Random.Range(0, _lines.Length)];
	}

	// 단 칸을 누르면 떼어 짐칸으로
	private void Unequip(int index)
	{
		if (!IsOpen || _stagecoach == null || index < 0 || index >= _coachSlots.Count)
		{
			return;
		}

		CStagecoachUpgradeData upgrade = _coachSlots[index].Upgrade;

		if (upgrade == null)
		{
			return;
		}

		if (!_stagecoach.TryUnequipToInventory(upgrade))
		{
			CPrint.Log($"CGarageView : {upgrade.DisplayName} 를 뗄 수 없음 (짐칸 자리 부족)");
			return;
		}

		if (_logState)
		{
			CPrint.Log($"CGarageView : {upgrade.DisplayName} 떼어 짐칸으로");
		}
	}

	private void Leave()
	{
		if (!IsOpen)
		{
			return;
		}

		_onLeave();
	}
	#endregion

	#region 짐칸
	private void OpenInventory()
	{
		if (_inventoryView == null)
		{
			_inventoryView = FindFirstObjectByType<CInventoryView>(FindObjectsInactive.Include);
		}

		if (_inventoryView == null)
		{
			return;
		}

		RaiseInventory(true);
		_inventoryView.SetUseHandler(this);

		// 원래 열려 있었으면 닫을 때도 그대로 둔다
		_openedInventory = !_inventoryView.IsOpen;

		if (_openedInventory)
		{
			_inventoryView.Open();
		}
	}

	private void CloseInventory()
	{
		if (_inventoryView == null)
		{
			return;
		}

		_inventoryView.SetUseHandler(null);

		if (_openedInventory)
		{
			_inventoryView.Close();
		}

		RaiseInventory(false);
		_openedInventory = false;
	}

	// 정비공 화면이 주행 HUD 를 덮으므로 → 짐칸 패널만 자기 캔버스로 그 위에 올린다
	private void RaiseInventory(bool raise)
	{
		Canvas canvas = _inventoryView.GetComponent<Canvas>();

		if (canvas == null)
		{
			if (!raise)
			{
				return;
			}

			canvas = _inventoryView.gameObject.AddComponent<Canvas>();
			_inventoryView.gameObject.AddComponent<GraphicRaycaster>();
		}

		canvas.overrideSorting = raise;
		canvas.sortingOrder = raise ? _inventorySortingOrder : 0;
	}
	#endregion

	private void SetVisible(bool isVisible)
	{
		_group.alpha = isVisible ? 1f : 0f;
		_group.interactable = isVisible;
		_group.blocksRaycasts = isVisible;
	}

	#region 화면 만들기
	private void BuildLayout()
	{
		RectTransform canvas = CUIBuilder.CreateOverlayCanvas("GarageCanvas", transform, _sortingOrder, true);
		RectTransform layout = CUIBuilder.CreateRect("GarageLayout", canvas);

		Stretch(layout, Vector2.zero, Vector2.one);
		_group = layout.gameObject.AddComponent<CanvasGroup>();

		// 뒤 화면을 덮는다
		Image dim = CUIBuilder.CreateImage("Dim", layout, null, _dimColor);

		Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
		dim.raycastTarget = true;

		// 왼쪽 : 작업장 실루엣 → 정비공 그림
		if (_sideArt != null)
		{
			Image side = CUIBuilder.CreateImage("SideArt", layout, _sideArt, _sideArtColor);

			Stretch(side.rectTransform, new Vector2(0f, 0f), new Vector2(0.3f, 1f));
			side.preserveAspect = true;
		}

		_portraitImage = CUIBuilder.CreateImage("Portrait", layout, _portrait, Color.white);
		Stretch(_portraitImage.rectTransform, new Vector2(0f, 0f), new Vector2(0.27f, 0.9f));
		_portraitImage.preserveAspect = true;
		_portraitImage.enabled = _portrait != null;

		// 오른쪽 위 : 실루엣 장식
		_bannerImage = CUIBuilder.CreateImage("Banner", layout, null, Color.white);
		_bannerImage.rectTransform.anchorMin = new Vector2(1f, 1f);
		_bannerImage.rectTransform.anchorMax = new Vector2(1f, 1f);
		_bannerImage.rectTransform.pivot = new Vector2(1f, 1f);
		_bannerImage.rectTransform.anchoredPosition = new Vector2(-20f, -6f);
		_bannerImage.rectTransform.sizeDelta = new Vector2(560f, 152f);
		_bannerImage.preserveAspect = true;
		_bannerImage.enabled = false;

		BuildHeader(layout);
		BuildCenter(layout);
	}

	// 왼쪽 위 : 뒤로가기 + 문장 + 제목 + 설명
	private void BuildHeader(RectTransform layout)
	{
		Image back = CUIBuilder.CreateImage("BackButton", layout, null, new Color(0.12f, 0.1f, 0.08f, 0.95f));

		Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(58f, 58f));
		back.raycastTarget = true;
		back.gameObject.AddComponent<Button>().onClick.AddListener(Leave);

		Text arrow = CreateText("Arrow", back.rectTransform, "◀", 30, TextAnchor.MiddleCenter);

		Stretch(arrow.rectTransform, Vector2.zero, Vector2.one);

		_crestImage = CUIBuilder.CreateImage("Crest", layout, null, Color.white);
		Place(_crestImage.rectTransform, new Vector2(0f, 1f), new Vector2(112f, -40f), new Vector2(72f, 80f));
		_crestImage.preserveAspect = true;
		_crestImage.enabled = false;

		_title = CreateText("Title", layout, "마차 정비공", 44, TextAnchor.MiddleLeft);
		PlaceLeft(_title.rectTransform, new Vector2(84f, -30f), new Vector2(520f, 58f));
		_title.fontStyle = FontStyle.Bold;

		_subtitle = CreateText("Subtitle", layout, string.Empty, 20, TextAnchor.UpperLeft);
		PlaceLeft(_subtitle.rectTransform, new Vector2(84f, -78f), new Vector2(460f, 60f));
		_subtitle.color = new Color(_textColor.r, _textColor.g, _textColor.b, 0.7f);

		_titleBase = _title.rectTransform.anchoredPosition;
		_subtitleBase = _subtitle.rectTransform.anchoredPosition;
	}

	// 가운데 : 바퀴·장갑 → 정비 칸 → 대사 → 지갑
	private void BuildCenter(RectTransform layout)
	{
		RectTransform center = CUIBuilder.CreateRect("Center", layout);

		Stretch(center, new Vector2(0.28f, 0f), new Vector2(0.76f, 1f));

		BuildStats(center);
		BuildSlots(center);

		_line = CreateText("Line", center, string.Empty, 26, TextAnchor.MiddleCenter);
		Stretch(_line.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.11f));
		_line.fontStyle = FontStyle.Italic;

		_wallet = CreateText("Wallet", center, string.Empty, 20, TextAnchor.MiddleCenter);
		Stretch(_wallet.rectTransform, new Vector2(0f, 0.015f), new Vector2(1f, 0.055f));
		_wallet.color = new Color(0.95f, 0.78f, 0.35f, 1f);
	}

	// 바퀴 칸 묶음(왼쪽) · 장갑 칸 묶음(오른쪽)
	private void BuildStats(RectTransform center)
	{
		RectTransform row = CUIBuilder.CreateRect("Stats", center);

		Stretch(row, new Vector2(0f, 0.32f), new Vector2(1f, 0.39f));

		Sprite wheelIcon = _wheelIcon != null ? _wheelIcon : CRunValueTable.GetIcon(ERunValue.Wheels);
		Sprite armorIcon = _armorIcon != null ? _armorIcon : CRunValueTable.GetIcon(ERunValue.Armor);

		BuildPipGroup(row, "Wheels", -200f, _wheelPips, wheelIcon);
		BuildPipGroup(row, "Armor", 200f, _armorPips, armorIcon);
		_wheelRepair = BuildRepairButton(row, ERunValue.Wheels, -200f);
		_armorRepair = BuildRepairButton(row, ERunValue.Armor, 200f);
	}

	// 칸 위치는 RefreshPips 에서 최대치에 맞춰 가운데 정렬한다
	private void BuildPipGroup(RectTransform row, string name, float x, Image[] pips, Sprite icon)
	{
		for (int i = 0; i < pips.Length; i++)
		{
			Image pip = CUIBuilder.CreateImage($"{name}_{i}", row, icon, Color.white);

			Place(pip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, PIP_Y), new Vector2(PIP_SIZE, PIP_SIZE));
			pip.preserveAspect = true;
			pips[i] = pip;
		}
	}

	// 칸 오른쪽 수리 버튼 → 검은 마름모 바탕 + 테두리 + 렌치, 아래 패물 가격
	private CRepairButton BuildRepairButton(RectTransform row, ERunValue part, float x)
	{
		// 클릭·마우스 판정만 받는 투명 바탕
		Image root = CUIBuilder.CreateImage($"{part}Repair", row, null, new Color(0f, 0f, 0f, 0f));

		Place(root.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, PIP_Y + 4f), new Vector2(REPAIR_SIZE, REPAIR_SIZE));
		root.raycastTarget = true;

		// 꼭짓점이 동서남북 → 그림이 없으면 정사각형을 45도 돌려 마름모로 그린다
		Image border = _repairBorder != null ? null : CreateDiamond("Border", root.rectTransform, REPAIR_SIZE * 0.74f, _repairBorderColor);
		Image fill = _repairBackground != null ? CUIBuilder.CreateImage("Fill", root.rectTransform, _repairBackground, Color.white) : CreateDiamond("Fill", root.rectTransform, REPAIR_SIZE * 0.64f, new Color(0.04f, 0.037f, 0.034f, 1f));

		if (_repairBackground != null)
		{
			Stretch(fill.rectTransform, Vector2.zero, Vector2.one);
			fill.preserveAspect = true;
		}

		if (_repairBorder != null)
		{
			border = CUIBuilder.CreateImage("Border", root.rectTransform, _repairBorder, _repairBorderColor);
			Stretch(border.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
			border.preserveAspect = true;
		}

		Image icon = CUIBuilder.CreateImage("Wrench", root.rectTransform, _repairIcon, Color.white);

		Stretch(icon.rectTransform, new Vector2(0.24f, 0.24f), new Vector2(0.76f, 0.76f));
		icon.preserveAspect = true;
		icon.enabled = _repairIcon != null;

		Button button = root.gameObject.AddComponent<Button>();

		button.transition = Selectable.Transition.None;
		button.onClick.AddListener(() => Repair(part));

		// 마우스를 올리면 설명 상자
		EventTrigger trigger = root.gameObject.AddComponent<EventTrigger>();

		AddTrigger(trigger, EventTriggerType.PointerEnter, () => HoverRepair(part, true));
		AddTrigger(trigger, EventTriggerType.PointerExit, () => HoverRepair(part, false));

		Image currency = CUIBuilder.CreateImage("CurrencyIcon", root.rectTransform, _repairCurrencyIcon, Color.white);

		Place(currency.rectTransform, new Vector2(0.5f, 0f), new Vector2(-12f, -10f), new Vector2(20f, 20f));
		currency.preserveAspect = true;
		currency.enabled = _repairCurrencyIcon != null;

		Text price = CreateText("Price", root.rectTransform, string.Empty, 20, TextAnchor.MiddleLeft);

		Place(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(18f, -10f), new Vector2(40f, 24f));
		price.gameObject.AddComponent<Outline>().effectColor = Color.black;

		return new CRepairButton { Part = part, GroupX = x, Rect = root.rectTransform, Button = button, Icon = icon, Border = border, Price = price };
	}

	// 정사각형을 45도 돌린 마름모
	private static Image CreateDiamond(string name, RectTransform parent, float size, Color color)
	{
		Image diamond = CUIBuilder.CreateImage(name, parent, null, color);

		Place(diamond.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
		diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
		return diamond;
	}

	private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };

		entry.callback.AddListener(_ => action());
		trigger.triggers.Add(entry);
	}

	// 정비 칸 줄 → 상품이 없는 칸은 자물쇠
	private void BuildSlots(RectTransform center)
	{
		RectTransform row = CUIBuilder.CreateRect("Slots", center);

		Stretch(row, new Vector2(0f, 0.13f), new Vector2(1f, 0.3f));

		float step = _slotSize + _slotSpacing;
		float startX = -step * (_slotCount - 1) * 0.5f;

		for (int i = 0; i < _slotCount; i++)
		{
			float x = startX + i * step;
			RectTransform root = CUIBuilder.CreateRect($"Slot_{i}", row);

			Place(root, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(_slotSize, _slotSize + 34f));
			_slots.Add(CreateSlot(root));
			SetupSlotDrag(root, i);
			_slotRects.Add(root);
			_locks.Add(CreateLock(row, i, x));
		}

		// 설명 상자 → 올린 칸 위
		Image box = CUIBuilder.CreateImage("Tooltip", center, null, new Color(0.06f, 0.05f, 0.045f, 0.96f));

		_tooltip = box.rectTransform;
		Place(_tooltip, new Vector2(0.5f, 0.3f), new Vector2(0f, 70f), new Vector2(420f, 110f));
		_tooltipText = CreateText("Text", _tooltip, string.Empty, 22, TextAnchor.UpperLeft);
		Stretch(_tooltipText.rectTransform, Vector2.zero, Vector2.one);
		_tooltipText.rectTransform.offsetMin = new Vector2(14f, 10f);
		_tooltipText.rectTransform.offsetMax = new Vector2(-14f, -10f);
		_tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
		_tooltip.gameObject.SetActive(false);
	}

	// 칸 → 바탕 / 아이콘 / 테두리 / 개수 / 상태 / 가격 (고물상 칸과 같은 구성)
	private CShopSlotView CreateSlot(RectTransform root)
	{
		CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
		Image background = CUIBuilder.CreateImage("Background", root, _slotBackground, _slotBackground != null ? Color.white : new Color(0.12f, 0.1f, 0.09f, 1f));

		Place(background.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -_slotSize * 0.5f), new Vector2(_slotSize, _slotSize));
		background.raycastTarget = true;

		Image icon = CUIBuilder.CreateImage("Icon", background.rectTransform, null, Color.white);

		Stretch(icon.rectTransform, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f));
		icon.preserveAspect = true;

		Image frame = CUIBuilder.CreateImage("Frame", background.rectTransform, null, Color.white);

		Stretch(frame.rectTransform, Vector2.zero, Vector2.one);

		Text count = CreateText("Count", background.rectTransform, string.Empty, 20, TextAnchor.UpperLeft);

		Stretch(count.rectTransform, Vector2.zero, Vector2.one);
		count.rectTransform.offsetMin = new Vector2(10f, 6f);
		count.rectTransform.offsetMax = new Vector2(-6f, -6f);
		count.gameObject.AddComponent<Outline>().effectColor = Color.black;

		Text state = CreateText("State", background.rectTransform, string.Empty, 20, TextAnchor.MiddleCenter);

		Stretch(state.rectTransform, Vector2.zero, Vector2.one);
		state.color = new Color(0.95f, 0.5f, 0.4f, 1f);
		state.fontStyle = FontStyle.Bold;
		state.gameObject.AddComponent<Outline>().effectColor = Color.black;

		Image priceIcon = CUIBuilder.CreateImage("PriceIcon", root, null, Color.white);

		Place(priceIcon.rectTransform, new Vector2(0.5f, 0f), new Vector2(-22f, 14f), new Vector2(24f, 24f));
		priceIcon.preserveAspect = true;

		Text price = CreateText("Price", root, string.Empty, 22, TextAnchor.MiddleLeft);

		Place(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(22f, 14f), new Vector2(60f, 28f));

		// 정비공은 팔지 않는다 → 가격 줄은 숨긴다
		priceIcon.gameObject.SetActive(false);
		price.gameObject.SetActive(false);

		CShopSlotView slot = root.gameObject.AddComponent<CShopSlotView>();

		slot.Setup(frame, icon, count, price, priceIcon, state, group);
		return slot;
	}

	// 짐칸에서 마차 아이템을 끌어와 놓으면 장착, 단 칸을 끌어 짐칸에 놓으면 해제 (짐칸 쪽이 받는다)
	private void SetupSlotDrag(RectTransform root, int index)
	{
		Image highlight = CUIBuilder.CreateImage("DropHighlight", root, null, new Color(1f, 0.8f, 0.4f, 0.3f));

		Place(highlight.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -_slotSize * 0.5f), new Vector2(_slotSize, _slotSize));

		CItemDropZone zone = root.gameObject.AddComponent<CItemDropZone>();

		zone.Setup(CanAcceptDrop, AcceptDrop, highlight);

		CItemDragSource source = root.gameObject.AddComponent<CItemDragSource>();

		source.Setup(() => MakeDragPayload(index));
	}

	private CItemDragPayload MakeDragPayload(int index)
	{
		if (!IsOpen || index < 0 || index >= _coachSlots.Count || _coachSlots[index].Upgrade == null)
		{
			return null;
		}

		return new CItemDragPayload { Source = EItemDragSource.Coach, Item = _coachSlots[index].Upgrade };
	}

	private bool CanAcceptDrop(CItemDragPayload payload)
	{
		return IsOpen && _stagecoach != null && payload.Source == EItemDragSource.Inventory && payload.Item is CStagecoachUpgradeData && _stagecoach.CanEquipFromInventory(payload.InventoryIndex);
	}

	private void AcceptDrop(CItemDragPayload payload)
	{
		if (_stagecoach.TryEquipFromInventory(payload.InventoryIndex) && _logState)
		{
			CPrint.Log($"CGarageView : {payload.Item.DisplayName} 끌어서 장착");
		}
	}

	private GameObject CreateLock(RectTransform row, int index, float x)
	{
		Image background = CUIBuilder.CreateImage($"Locked_{index}", row, _slotBackground, new Color(0.45f, 0.42f, 0.4f, 0.6f));

		Place(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 17f), new Vector2(_slotSize, _slotSize));

		Image icon = CUIBuilder.CreateImage("Lock", background.rectTransform, _lockIcon, new Color(1f, 1f, 1f, 0.5f));

		Stretch(icon.rectTransform, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));
		icon.preserveAspect = true;
		icon.enabled = _lockIcon != null;

		return background.gameObject;
	}

	private Text CreateText(string name, RectTransform parent, string value, int size, TextAnchor alignment)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();

		text.font = _font;
		text.text = value;
		text.fontSize = size;
		text.alignment = alignment;
		text.color = _textColor;
		text.supportRichText = true;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		text.raycastTarget = false;
		return text;
	}

	// 앵커 사이를 꽉 채운다
	private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
	{
		rect.anchorMin = min;
		rect.anchorMax = max;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	// 앵커 한 점에 크기·위치로 둔다 (피벗은 가운데)
	private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
	{
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	// 왼쪽 위 앵커에 왼쪽 가운데 피벗으로 둔다 (글이 position.x 에서 시작)
	private static void PlaceLeft(RectTransform rect, Vector2 position, Vector2 size)
	{
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}
	#endregion
}
