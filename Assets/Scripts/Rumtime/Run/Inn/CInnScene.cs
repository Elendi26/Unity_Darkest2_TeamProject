using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class CInnScene : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private string _title = "여관";
	[SerializeField] private string _shopLabel = "보급상";
	[SerializeField] private string _garageLabel = "마차 정비공";
	[SerializeField] private string _leaveLabel = "여관 떠나기";
	[SerializeField] private string _saveQuitLabel = "저장하고 메인 메뉴로";
	// 화면 아래에 하나를 골라 띄우는 대사
	[SerializeField] private string[] _flavorLines =
	{
		"불씨가 남아 있는 한, 길은 아직 끝나지 않았다.",
		"잠시 짐을 내려놓아라. 내일의 길은 오늘보다 험하다.",
		"따뜻한 불 앞에서도 칼자루를 놓는 자는 없다.",
	};
	[SerializeField] private Font _font;
	// 아래 메뉴 아이콘 (저장하고 메인 메뉴로는 비우면 문 아이콘을 그려 쓴다)
	[SerializeField] private Sprite _shopIcon;
	[SerializeField] private Sprite _garageIcon;
	[SerializeField] private Sprite _saveQuitIcon;
	// 화살표 모양 판 크기 (아이콘 버튼 / 떠나기)
	[SerializeField] private Vector2 _iconButtonSize = new Vector2(124f, 76f);
	[SerializeField] private float _iconSize = 50f;
	[SerializeField] private Vector2 _leaveButtonSize = new Vector2(300f, 76f);
	// 판끼리 겹치는 폭 (화살표 끝이 다음 판 홈에 물리게)
	[SerializeField] private float _buttonSpacing = -14f;
	[SerializeField] private Color _plateColor = new Color(0.07f, 0.06f, 0.055f, 0.95f);
	[SerializeField] private Color _plateBorderColor = new Color(0.42f, 0.4f, 0.37f, 1f);
	[SerializeField] private Color _iconColor = new Color(0.82f, 0.8f, 0.76f, 1f);
	[SerializeField] private Color _leaveTextColor = new Color(0.86f, 0.7f, 0.42f, 1f);
	// 화면 가장자리와의 여백
	[SerializeField] private float _margin = 32f;
	[SerializeField] private float _bottomBarHeight = 100f;
	[SerializeField] private Vector2 _titleSize = new Vector2(520f, 84f);
	[SerializeField] private Color _panelColor = new Color(0.03f, 0.025f, 0.02f, 0.82f);
	[SerializeField] private Color _accentColor = new Color(0.62f, 0.2f, 0.12f, 1f);
	[SerializeField] private Color _textColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	[SerializeField] private Color _flavorColor = new Color(0.82f, 0.74f, 0.56f, 1f);
	[SerializeField] private Color _disabledTextColor = new Color(0.45f, 0.42f, 0.38f, 1f);
	[SerializeField] private int _sortingOrder = 40;
	// 마차 정비공 화면 (비우면 자식에서 찾는다, 없으면 정비소도 일반 상점 화면으로 연다)
	[SerializeField] private CGarageView _garageView;
	#endregion

	// 마차 정비공 화면 (없으면 null)
	public CGarageView GarageView => _garageView != null ? _garageView : (_garageView = GetComponentInChildren<CGarageView>(true));

	// 메뉴 UI 루트 → 메뉴와 함께 보였다 숨었다 할 UI(파티 이름표 등)를 여기에 붙인다
	public RectTransform MenuRoot { get; private set; }

	#region 이벤트
	public event Action OnShopClicked;
	public event Action OnGarageClicked;
	public event Action OnLeaveClicked;
	public event Action OnSaveQuitClicked;
	#endregion

	#region 내부 변수
	private CanvasGroup _menu;
	private Button _shopButton;
	private Button _garageButton;
	private Button _saveQuitButton;
	// 버튼을 못 쓸 때 어둡게 할 아이콘
	private Graphic _shopGraphic;
	private Graphic _garageGraphic;
	private Graphic _saveQuitGraphic;
	#endregion

	private void Awake()
	{
		if (_font == null)
		{
			_font = CUIBuilder.GetDefaultFont();
		}

		BuildMenu();
	}

	private void Start()
	{
		// 여관 씬만 따로 재생할 때 → 주행 씬의 이벤트 시스템이 없으니 하나 만든다
		if (FindFirstObjectByType<EventSystem>() == null)
		{
			GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

			eventSystem.transform.SetParent(transform, false);
		}
	}

	public void SetMenuVisible(bool visible)
	{
		if (_menu == null)
		{
			return;
		}

		_menu.alpha = visible ? 1f : 0f;
		_menu.interactable = visible;
		_menu.blocksRaycasts = visible;
	}

	public void SetAvailable(bool shop, bool garage, bool saveQuit)
	{
		SetButton(_shopButton, _shopGraphic, shop);
		SetButton(_garageButton, _garageGraphic, garage);
		SetButton(_saveQuitButton, _saveQuitGraphic, saveQuit);
	}

	private void SetButton(Button button, Graphic icon, bool available)
	{
		if (button == null)
		{
			return;
		}

		button.interactable = available;

		if (icon != null)
		{
			icon.color = available ? _iconColor : _disabledTextColor;
		}
	}

	#region 메뉴 만들기
	private void BuildMenu()
	{
		RectTransform canvas = CUIBuilder.CreateOverlayCanvas("InnMenuCanvas", transform, _sortingOrder, true);
		RectTransform menu = CUIBuilder.CreateRect("Menu", canvas);

		menu.anchorMin = Vector2.zero;
		menu.anchorMax = Vector2.one;
		menu.offsetMin = Vector2.zero;
		menu.offsetMax = Vector2.zero;
		menu.pivot = new Vector2(0.5f, 0.5f);
		_menu = menu.gameObject.AddComponent<CanvasGroup>();
		MenuRoot = menu;

		BuildTitle(menu);
		BuildBottomBar(menu);
		BuildFlavorLine(menu);
	}

	// 왼쪽 위 제목 띠 → 왼쪽 끝 붉은 줄 + 제목
	private void BuildTitle(RectTransform menu)
	{
		Image band = CUIBuilder.CreateImage("TitleBand", menu, null, _panelColor);
		RectTransform rect = band.rectTransform;

		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = new Vector2(_margin, -_margin);
		rect.sizeDelta = _titleSize;

		Image accent = CUIBuilder.CreateImage("Accent", rect, null, _accentColor);

		accent.rectTransform.anchorMin = Vector2.zero;
		accent.rectTransform.anchorMax = new Vector2(0f, 1f);
		accent.rectTransform.pivot = new Vector2(0f, 0.5f);
		accent.rectTransform.anchoredPosition = Vector2.zero;
		accent.rectTransform.sizeDelta = new Vector2(10f, 0f);

		Text title = CreateText("Title", rect, _title, 44);

		title.alignment = TextAnchor.MiddleLeft;
		Stretch(title.rectTransform, new Vector2(36f, 0f), Vector2.zero);
	}

	// 아래쪽 메뉴 줄 → 왼쪽부터 보급상·마차 정비공·저장하고 메인 메뉴로, 오른쪽 끝에 떠나기
	private void BuildBottomBar(RectTransform menu)
	{
		Image barImage = CUIBuilder.CreateImage("BottomBar", menu, null, _panelColor);
		RectTransform bar = barImage.rectTransform;

		// 뒤쪽(주행 화면) 클릭이 새지 않게
		barImage.raycastTarget = true;
		bar.anchorMin = Vector2.zero;
		bar.anchorMax = new Vector2(1f, 0f);
		bar.pivot = new Vector2(0.5f, 0f);
		bar.anchoredPosition = Vector2.zero;
		bar.sizeDelta = new Vector2(0f, _bottomBarHeight);

		Image line = CUIBuilder.CreateImage("TopLine", bar, null, _accentColor);

		line.rectTransform.anchorMin = new Vector2(0f, 1f);
		line.rectTransform.anchorMax = Vector2.one;
		line.rectTransform.pivot = new Vector2(0.5f, 1f);
		line.rectTransform.anchoredPosition = Vector2.zero;
		line.rectTransform.sizeDelta = new Vector2(0f, 3f);

		// 화살표 판을 왼쪽부터 이어 붙인다 → 보급상 > 마차 정비공 > 저장하고 메인 메뉴로 > 떠나기
		Sprite plate = CInnMenuArt.GetPlate(_plateColor, _plateBorderColor);
		Sprite saveQuitIcon = _saveQuitIcon != null ? _saveQuitIcon : CInnMenuArt.GetDoorIcon();
		float x = _margin;
		float step = _iconButtonSize.x + _buttonSpacing;

		// 나중에 만든 판이 앞 판의 화살표 끝을 덮지 않게 → 오른쪽 것부터 만든다
		CreatePlateButton("LeaveButton", bar, plate, new Vector2(x + step * 3f, 0f), _leaveButtonSize, _leaveLabel, null, NotifyLeave, out _);
		_saveQuitButton = CreatePlateButton("SaveQuitButton", bar, plate, new Vector2(x + step * 2f, 0f), _iconButtonSize, _saveQuitLabel, saveQuitIcon, NotifySaveQuit, out _saveQuitGraphic);
		_garageButton = CreatePlateButton("GarageButton", bar, plate, new Vector2(x + step, 0f), _iconButtonSize, _garageLabel, _garageIcon, NotifyGarage, out _garageGraphic);
		_shopButton = CreatePlateButton("ShopButton", bar, plate, new Vector2(x, 0f), _iconButtonSize, _shopLabel, _shopIcon, NotifyShop, out _shopGraphic);
	}

	// 메뉴 줄 바로 위 가운데 한 줄 대사
	private void BuildFlavorLine(RectTransform menu)
	{
		if (_flavorLines == null || _flavorLines.Length == 0)
		{
			return;
		}

		string line = _flavorLines[UnityEngine.Random.Range(0, _flavorLines.Length)];
		Text flavor = CreateText("FlavorLine", menu, line, 26);
		RectTransform rect = flavor.rectTransform;

		flavor.color = _flavorColor;
		flavor.fontStyle = FontStyle.Italic;
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = new Vector2(1f, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.anchoredPosition = new Vector2(0f, _bottomBarHeight + 18f);
		rect.sizeDelta = new Vector2(-_margin * 2f, 40f);

		Shadow shadow = flavor.gameObject.AddComponent<Shadow>();

		shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
		shadow.effectDistance = new Vector2(2f, -2f);
	}

	private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = offsetMin;
		rect.offsetMax = offsetMax;
	}

	// 화살표 판 버튼 → 아이콘이 있으면 아이콘(+ 올리면 이름 풍선), 없으면 글자
	private Button CreatePlateButton(string name, RectTransform parent, Sprite plate, Vector2 position, Vector2 size, string label, Sprite icon, UnityEngine.Events.UnityAction onClick, out Graphic content)
	{
		Image image = CUIBuilder.CreateImage(name, parent, plate, Color.white);
		RectTransform rect = image.rectTransform;

		image.raycastTarget = true;
		image.type = Image.Type.Simple;
		// 투명한 모서리는 클릭되지 않게
		image.alphaHitTestMinimumThreshold = 0.1f;
		rect.anchorMin = new Vector2(0f, 0.5f);
		rect.anchorMax = new Vector2(0f, 0.5f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;

		Button button = image.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;

		colors.highlightedColor = new Color(1.6f, 1.45f, 1.2f, 1f);
		colors.pressedColor = new Color(0.8f, 0.75f, 0.7f, 1f);
		colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
		button.colors = colors;
		button.onClick.AddListener(onClick);

		if (icon == null)
		{
			Text text = CreateText("Label", rect, label, 30);

			text.color = _leaveTextColor;
			// 왼쪽 홈·오른쪽 끝을 피해 가운데에
			Stretch(text.rectTransform, new Vector2(size.y * 0.35f, 0f), new Vector2(-size.y * 0.35f, 0f));
			content = text;

			return button;
		}

		Image iconImage = CUIBuilder.CreateImage("Icon", rect, icon, _iconColor);

		iconImage.preserveAspect = true;
		iconImage.raycastTarget = false;
		iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		iconImage.rectTransform.anchoredPosition = Vector2.zero;
		iconImage.rectTransform.sizeDelta = new Vector2(_iconSize, _iconSize);
		content = iconImage;

		AddTooltip(image.gameObject, rect, label);

		return button;
	}

	// 아이콘만으로는 뜻을 모를 수 있다 → 마우스를 올리면 판 위에 이름을 띄운다
	private void AddTooltip(GameObject target, RectTransform rect, string label)
	{
		Image back = CUIBuilder.CreateImage("Tooltip", rect, null, _panelColor);
		RectTransform tip = back.rectTransform;

		back.raycastTarget = false;
		tip.anchorMin = new Vector2(0.5f, 1f);
		tip.anchorMax = new Vector2(0.5f, 1f);
		tip.pivot = new Vector2(0.5f, 0f);
		tip.anchoredPosition = new Vector2(0f, 10f);
		tip.sizeDelta = new Vector2(Mathf.Max(140f, label.Length * 26f + 32f), 44f);

		Text text = CreateText("Label", tip, label, 24);

		Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
		tip.gameObject.SetActive(false);

		EventTrigger trigger = target.AddComponent<EventTrigger>();

		AddTrigger(trigger, EventTriggerType.PointerEnter, () => tip.gameObject.SetActive(true));
		AddTrigger(trigger, EventTriggerType.PointerExit, () => tip.gameObject.SetActive(false));
		AddTrigger(trigger, EventTriggerType.PointerClick, () => tip.gameObject.SetActive(false));
	}

	private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry();

		entry.eventID = type;
		entry.callback.AddListener(_ => action());
		trigger.triggers.Add(entry);
	}

	private Text CreateText(string name, RectTransform parent, string value, int size)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();

		text.font = _font;
		text.fontSize = size;
		text.color = _textColor;
		text.alignment = TextAnchor.MiddleCenter;
		text.raycastTarget = false;
		text.text = value;

		return text;
	}
	#endregion

	#region 버튼 알림
	private void NotifyShop()
	{
		Notify(OnShopClicked);
	}

	private void NotifyGarage()
	{
		Notify(OnGarageClicked);
	}

	private void NotifyLeave()
	{
		Notify(OnLeaveClicked);
	}

	private void NotifySaveQuit()
	{
		Notify(OnSaveQuitClicked);
	}

	private void Notify(Action action)
	{
		// 여관 씬만 따로 재생하면 받을 곳이 없다
		if (action == null)
		{
			CPrint.Warn("CInnScene : 여관 처리기가 연결되지 않았습니다. 주행 씬에서 여관 노드로 들어와야 동작합니다.");
			return;
		}

		action.Invoke();
	}
	#endregion
}
