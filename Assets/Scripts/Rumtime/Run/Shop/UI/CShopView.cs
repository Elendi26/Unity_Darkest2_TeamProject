using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 왼쪽 상인 그림·제목, 가운데 분류 탭·상품 칸 격자·설명 줄, 오른쪽은 짐칸 패널
public class CShopView : MonoBehaviour, IShopView
{
	#region 인스펙터
	[SerializeField] private CanvasGroup _group;
	[SerializeField] private Font _font;
	[Header("그림")]
	// 왼쪽 상인 그림
	[SerializeField] private Sprite _portrait;
	// 칸 바탕
	[SerializeField] private Sprite _slotBackground;
	// 칸 테두리 (기본·전투·장신구·마차·희귀 이상)
	[SerializeField] private Sprite _frameDefault;
	[SerializeField] private Sprite _frameCombat;
	[SerializeField] private Sprite _frameTrinket;
	[SerializeField] private Sprite _frameCoach;
	[SerializeField] private Sprite _frameRare;
	// 가격 옆 금화
	[SerializeField] private Sprite _goldIcon;
	// 가격 옆 패물
	[SerializeField] private Sprite _factionIcon;
	// 분류 탭 아이콘 (전체·보급·전투·장신구·마차 순)
	[SerializeField] private Sprite[] _tabIcons;
	[Header("배치")]
	[SerializeField] private int _columns = 5;
	[SerializeField] private float _slotSize = 112f;
	[SerializeField] private float _slotSpacing = 26f;
	[SerializeField] private Color _panelColor = new Color(0.05f, 0.045f, 0.04f, 0.94f);
	[SerializeField] private Color _textColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	[SerializeField] private Color _tabOnColor = new Color(0.95f, 0.85f, 0.6f, 1f);
	[SerializeField] private Color _tabOffColor = new Color(0.4f, 0.37f, 0.33f, 1f);
	// 상점이 열리면 오른쪽 짐칸 패널도 연다 (비우면 씬에서 찾는다)
	[SerializeField] private CInventoryView _inventoryView;
	[SerializeField] private bool _openInventory = true;
	#endregion

	#region 내부 변수
	private const int TAB_COUNT = 5;
	private const float TITLE_CREST_SHIFT = 60f;
	private static readonly string[] TAB_NAMES = { "모든 상품", "보급품", "전투 물품", "장신구", "마차" };

	private readonly List<CShopSlotView> _slots = new List<CShopSlotView>();
	private readonly List<int> _visible = new List<int>();
	private readonly Image[] _tabImages = new Image[TAB_COUNT];
	private IReadOnlyList<CShopCardInfo> _cards;
	private RectTransform _layout;
	private RectTransform _grid;
	private ScrollRect _scroll;
	private Image _portraitImage;
	private Image _crestImage;
	private Image _bannerImage;
	private Vector2 _titleBase;
	private Vector2 _subtitleBase;
	private Text _title;
	private Text _subtitle;
	private Text _tabTitle;
	private Text _detail;
	private Text _wallet;
	private Action _onLeave;
	private Action<int> _onBuy;
	private int _tab;
	private int _hovered = -1;
	private bool _openedInventory;
	#endregion

	// 상점이 열려 있는지
	public bool IsOpen => _onLeave != null;

	private void Awake()
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			transform.GetChild(i).gameObject.SetActive(false);
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

	public void Open(string title, string body, Action onLeave)
	{
		_onLeave = onLeave;
		_title.text = title;
		_subtitle.text = body;
		_tab = 0;
		_hovered = -1;
		RefreshTabs();
		ScrollToTop();
		SetVisible(true);
		OpenInventory();
	}

	public void SetLook(Sprite portrait, Sprite crest, Sprite banner)
	{
		Sprite shown = portrait != null ? portrait : _portrait;

		_portraitImage.sprite = shown;
		_portraitImage.enabled = shown != null;

		_crestImage.sprite = crest;
		_crestImage.enabled = crest != null;

		// 문장이 있으면 제목·설명을 오른쪽으로 민다
		float shift = crest != null ? TITLE_CREST_SHIFT : 0f;

		_title.rectTransform.anchoredPosition = _titleBase + new Vector2(shift, 0f);
		_subtitle.rectTransform.anchoredPosition = _subtitleBase + new Vector2(shift, 0f);

		_bannerImage.sprite = banner;
		_bannerImage.enabled = banner != null;
	}

	public void SetWallet(string text)
	{
		_wallet.text = text;
	}

	public void SetCards(IReadOnlyList<CShopCardInfo> cards, Action<int> onBuy)
	{
		_cards = cards;
		_onBuy = onBuy;
		RefreshSlots();
	}

	public void Close()
	{
		_onLeave = null;
		_onBuy = null;
		SetVisible(false);
		CloseInventory();
	}

	#region 칸 갱신
	private void RefreshSlots()
	{
		_visible.Clear();

		int count = _cards != null ? _cards.Count : 0;

		for (int i = 0; i < count; i++)
		{
			if (IsInTab(_cards[i].Category, _tab))
			{
				_visible.Add(i);
			}
		}

		for (int i = 0; i < _visible.Count; i++)
		{
			while (_slots.Count <= i)
			{
				_slots.Add(CreateSlot(_slots.Count));
			}

			int cardIndex = _visible[i];
			CShopCardInfo info = _cards[cardIndex];

			_slots[i].gameObject.SetActive(true);
			_slots[i].Bind(info, GetFrame(info), GetPriceIcon(info.CostType), () => Buy(cardIndex), isOver => Hover(cardIndex, isOver));
		}

		for (int i = _visible.Count; i < _slots.Count; i++)
		{
			_slots[i].gameObject.SetActive(false);
		}

		RefreshDetail();
	}

	private Sprite GetFrame(CShopCardInfo info)
	{
		if (info.Rarity >= EItemRarity.Rare && _frameRare != null)
		{
			return _frameRare;
		}

		switch (info.Category)
		{
			case EItemCategory.Combat:
				return _frameCombat != null ? _frameCombat : _frameDefault;
			case EItemCategory.Trinket:
				return _frameTrinket != null ? _frameTrinket : _frameDefault;
			case EItemCategory.Coach:
				return _frameCoach != null ? _frameCoach : _frameDefault;
		}

		return _frameDefault;
	}

	// 탭 0 전체, 1 보급(보급·횃불), 2 전투, 3 장신구, 4 마차
	private static bool IsInTab(EItemCategory category, int tab)
	{
		switch (tab)
		{
			case 1:
				return category == EItemCategory.Supply || category == EItemCategory.Torch;
			case 2:
				return category == EItemCategory.Combat;
			case 3:
				return category == EItemCategory.Trinket;
			case 4:
				return category == EItemCategory.Coach;
		}

		return true;
	}

	private void Hover(int cardIndex, bool isOver)
	{
		if (isOver)
		{
			_hovered = cardIndex;
		}
		else if (_hovered == cardIndex)
		{
			_hovered = -1;
		}

		RefreshDetail();
	}

	// 아래 설명 줄 → 올린 상품의 이름·설명·가격 (없으면 안내)
	private void RefreshDetail()
	{
		if (_cards == null || _hovered < 0 || _hovered >= _cards.Count)
		{
			_detail.text = "상품에 마우스를 올리면 설명이 나옵니다. 누르면 삽니다.";
			return;
		}

		CShopCardInfo info = _cards[_hovered];
		string state = info.State == EShopCardState.Available ? string.Empty : $"  <color=#d9604a>({GetStateName(info.State)})</color>";

		_detail.text = $"<b>{info.Name}</b>  <color=#c9b27a>{info.Price}</color>{state}\n<size=18>{info.Description}</size>";
	}

	private Sprite GetPriceIcon(ERunValue currency)
	{
		switch (currency)
		{
			case ERunValue.Gold:
				return _goldIcon;
			case ERunValue.Faction:
				return _factionIcon;
		}

		return null;
	}

	public static string GetStateName(EShopCardState state)
	{
		switch (state)
		{
			case EShopCardState.TooExpensive:
				return "돈 부족";
			case EShopCardState.SoldOut:
				return "품절";
			case EShopCardState.Full:
				return "가득 참";
			case EShopCardState.Maxed:
				return "최대";
		}

		return string.Empty;
	}

	private void SelectTab(int tab)
	{
		if (_tab == tab)
		{
			return;
		}

		_tab = tab;
		_hovered = -1;
		RefreshTabs();
		RefreshSlots();
		ScrollToTop();
	}

	private void ScrollToTop()
	{
		if (_scroll != null)
		{
			_scroll.verticalNormalizedPosition = 1f;
		}
	}

	private void RefreshTabs()
	{
		for (int i = 0; i < TAB_COUNT; i++)
		{
			if (_tabImages[i] != null)
			{
				_tabImages[i].color = i == _tab ? _tabOnColor : _tabOffColor;
			}
		}

		_tabTitle.text = TAB_NAMES[Mathf.Clamp(_tab, 0, TAB_COUNT - 1)];
	}
	#endregion

	private void Buy(int cardIndex)
	{
		if (!IsOpen)
		{
			return;
		}

		_onBuy?.Invoke(cardIndex);
	}

	private void Leave()
	{
		if (!IsOpen)
		{
			return;
		}

		_onLeave();
	}

	private void OpenInventory()
	{
		if (!_openInventory)
		{
			return;
		}

		if (_inventoryView == null)
		{
			_inventoryView = FindFirstObjectByType<CInventoryView>(FindObjectsInactive.Include);
		}

		// 원래 열려 있었으면 닫을 때도 그대로 둔다
		_openedInventory = _inventoryView != null && !_inventoryView.IsOpen;

		if (_openedInventory)
		{
			_inventoryView.Open();
		}
	}

	private void CloseInventory()
	{
		if (_openedInventory && _inventoryView != null)
		{
			_inventoryView.Close();
		}

		_openedInventory = false;
	}

	private void SetVisible(bool isVisible)
	{
		if (_group == null)
		{
			gameObject.SetActive(isVisible);
			return;
		}

		_group.alpha = isVisible ? 1f : 0f;
		_group.interactable = isVisible;
		_group.blocksRaycasts = isVisible;
	}

	#region 화면 만들기
	private void BuildLayout()
	{
		if (_font == null)
		{
			_font = CUIBuilder.GetDefaultFont();
		}

		_layout = CUIBuilder.CreateRect("ShopLayout", (RectTransform)transform);
		Stretch(_layout, Vector2.zero, Vector2.one);

		// 왼쪽 : 상인 그림 (아래 붙임)
		Image portrait = CUIBuilder.CreateImage("Portrait", _layout, _portrait, Color.white);

		Stretch(portrait.rectTransform, new Vector2(0f, 0f), new Vector2(0.28f, 0.9f));
		portrait.preserveAspect = true;
		portrait.enabled = _portrait != null;
		_portraitImage = portrait;

		// 오른쪽 위 : 실루엣 장식 (상점마다 다르다)
		_bannerImage = CUIBuilder.CreateImage("Banner", _layout, null, Color.white);
		_bannerImage.rectTransform.anchorMin = new Vector2(1f, 1f);
		_bannerImage.rectTransform.anchorMax = new Vector2(1f, 1f);
		_bannerImage.rectTransform.pivot = new Vector2(1f, 1f);
		_bannerImage.rectTransform.anchoredPosition = new Vector2(-20f, -6f);
		_bannerImage.rectTransform.sizeDelta = new Vector2(560f, 152f);
		_bannerImage.preserveAspect = true;
		_bannerImage.enabled = false;

		// 왼쪽 위 : 뒤로가기 + 제목 + 설명
		Image back = CUIBuilder.CreateImage("BackButton", _layout, null, new Color(0.12f, 0.1f, 0.08f, 0.95f));

		Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(58f, 58f));
		back.raycastTarget = true;
		back.gameObject.AddComponent<Button>().onClick.AddListener(Leave);

		Text backArrow = CreateText("Arrow", back.rectTransform, "◀", 30, TextAnchor.MiddleCenter);

		Stretch(backArrow.rectTransform, Vector2.zero, Vector2.one);

		_title = CreateText("Title", _layout, "고물상", 44, TextAnchor.MiddleLeft);
		PlaceLeft(_title.rectTransform, new Vector2(84f, -30f), new Vector2(520f, 58f));
		_title.fontStyle = FontStyle.Bold;

		_subtitle = CreateText("Subtitle", _layout, string.Empty, 20, TextAnchor.UpperLeft);
		PlaceLeft(_subtitle.rectTransform, new Vector2(84f, -78f), new Vector2(460f, 60f));
		_subtitle.color = new Color(_textColor.r, _textColor.g, _textColor.b, 0.7f);
		_titleBase = _title.rectTransform.anchoredPosition;
		_subtitleBase = _subtitle.rectTransform.anchoredPosition;

		// 뒤로가기와 제목 사이 : 문장 (상점마다 다르다)
		_crestImage = CUIBuilder.CreateImage("Crest", _layout, null, Color.white);
		Place(_crestImage.rectTransform, new Vector2(0f, 1f), new Vector2(112f, -40f), new Vector2(72f, 80f));
		_crestImage.preserveAspect = true;
		_crestImage.enabled = false;

		// 가운데 : 상품 판
		Image panel = CUIBuilder.CreateImage("Panel", _layout, null, _panelColor);

		Stretch(panel.rectTransform, new Vector2(0.29f, 0.12f), new Vector2(0.75f, 0.95f));

		BuildTabs(panel.rectTransform);

		BuildScroll(panel.rectTransform);

		GridLayoutGroup grid = _grid.gameObject.AddComponent<GridLayoutGroup>();

		grid.cellSize = new Vector2(_slotSize, _slotSize + 34f);
		grid.spacing = new Vector2(_slotSpacing, _slotSpacing);
		grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		grid.constraintCount = Mathf.Max(1, _columns);
		grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
		grid.startAxis = GridLayoutGroup.Axis.Horizontal;
		grid.childAlignment = TextAnchor.UpperLeft;

		// 상품이 많으면 격자가 아래로 늘어나고 스크롤로 본다
		ContentSizeFitter fitter = _grid.gameObject.AddComponent<ContentSizeFitter>();

		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		// 아래 : 설명 줄
		_detail = CreateText("Detail", panel.rectTransform, string.Empty, 22, TextAnchor.UpperLeft);
		Stretch(_detail.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f));
		_detail.rectTransform.pivot = new Vector2(0.5f, 0f);
		_detail.rectTransform.offsetMin = new Vector2(34f, 22f);
		_detail.rectTransform.offsetMax = new Vector2(-34f, 120f);
		_detail.horizontalOverflow = HorizontalWrapMode.Wrap;

		// 판 아래 : 지갑
		_wallet = CreateText("Wallet", _layout, string.Empty, 22, TextAnchor.MiddleCenter);
		Stretch(_wallet.rectTransform, new Vector2(0.29f, 0.05f), new Vector2(0.75f, 0.11f));
		_wallet.color = new Color(0.95f, 0.78f, 0.35f, 1f);
	}

	// 상품 격자 → 보이는 창(가림) + 오른쪽 세로 스크롤바
	private void BuildScroll(RectTransform panel)
	{
		Image viewport = CUIBuilder.CreateImage("Viewport", panel, null, new Color(0f, 0f, 0f, 0f));

		Stretch(viewport.rectTransform, Vector2.zero, Vector2.one);
		viewport.rectTransform.offsetMin = new Vector2(30f, 150f);
		viewport.rectTransform.offsetMax = new Vector2(-50f, -120f);
		// 칸 사이 빈 곳에서도 휠이 먹게
		viewport.raycastTarget = true;
		viewport.gameObject.AddComponent<RectMask2D>();

		_grid = CUIBuilder.CreateRect("Grid", viewport.rectTransform);
		_grid.anchorMin = new Vector2(0f, 1f);
		_grid.anchorMax = new Vector2(1f, 1f);
		_grid.pivot = new Vector2(0.5f, 1f);
		_grid.offsetMin = Vector2.zero;
		_grid.offsetMax = Vector2.zero;

		Image track = CUIBuilder.CreateImage("Scrollbar", panel, null, new Color(0.18f, 0.16f, 0.14f, 0.9f));

		track.rectTransform.anchorMin = new Vector2(1f, 0f);
		track.rectTransform.anchorMax = new Vector2(1f, 1f);
		track.rectTransform.pivot = new Vector2(1f, 0.5f);
		track.rectTransform.offsetMin = new Vector2(-36f, 150f);
		track.rectTransform.offsetMax = new Vector2(-26f, -120f);
		track.raycastTarget = true;

		RectTransform slidingArea = CUIBuilder.CreateRect("SlidingArea", track.rectTransform);

		Stretch(slidingArea, Vector2.zero, Vector2.one);

		Image handle = CUIBuilder.CreateImage("Handle", slidingArea, null, _tabOffColor);

		Stretch(handle.rectTransform, Vector2.zero, Vector2.one);
		handle.raycastTarget = true;

		Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();

		bar.handleRect = handle.rectTransform;
		bar.targetGraphic = handle;
		bar.direction = Scrollbar.Direction.BottomToTop;

		_scroll = viewport.gameObject.AddComponent<ScrollRect>();
		_scroll.viewport = viewport.rectTransform;
		_scroll.content = _grid;
		_scroll.horizontal = false;
		_scroll.vertical = true;
		_scroll.movementType = ScrollRect.MovementType.Clamped;
		_scroll.scrollSensitivity = 40f;
		_scroll.verticalScrollbar = bar;
		_scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
	}

	private void BuildTabs(RectTransform panel)
	{
		for (int i = 0; i < TAB_COUNT; i++)
		{
			Sprite icon = _tabIcons != null && i < _tabIcons.Length ? _tabIcons[i] : null;
			Image tab = CUIBuilder.CreateImage($"Tab_{i}", panel, icon, _tabOffColor);

			Place(tab.rectTransform, new Vector2(0f, 1f), new Vector2(60f + i * 70f, -48f), new Vector2(52f, 52f));
			tab.preserveAspect = true;
			tab.raycastTarget = true;

			int index = i;

			tab.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectTab(index));
			_tabImages[i] = tab;
		}

		_tabTitle = CreateText("TabTitle", panel, TAB_NAMES[0], 26, TextAnchor.MiddleRight);
		Place(_tabTitle.rectTransform, new Vector2(1f, 1f), new Vector2(-180f, -48f), new Vector2(300f, 52f));
	}

	// 칸 → 바탕 / 테두리 / 아이콘 / 개수(왼쪽 위) / 상태 글 / 가격(아래, 금화 + 숫자)
	private CShopSlotView CreateSlot(int index)
	{
		RectTransform root = CUIBuilder.CreateRect($"Slot_{index}", _grid);
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

		Image priceIcon = CUIBuilder.CreateImage("PriceIcon", root, _goldIcon, Color.white);

		Place(priceIcon.rectTransform, new Vector2(0.5f, 0f), new Vector2(-22f, 14f), new Vector2(24f, 24f));
		priceIcon.preserveAspect = true;

		Text price = CreateText("Price", root, string.Empty, 22, TextAnchor.MiddleLeft);

		Place(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(22f, 14f), new Vector2(60f, 28f));

		CShopSlotView slot = root.gameObject.AddComponent<CShopSlotView>();

		slot.Setup(frame, icon, count, price, priceIcon, state, group);
		return slot;
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
