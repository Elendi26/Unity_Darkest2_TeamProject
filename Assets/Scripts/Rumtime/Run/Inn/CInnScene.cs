using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 여관 씬 → 가운데 메뉴(보급상·마차 정비공·떠나기·저장하고 메인 메뉴로)를 띄우고, 누른 버튼을 여관 처리기에 알린다
public class CInnScene : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private string _title = "여관";
	[SerializeField] private string _shopLabel = "보급상";
	[SerializeField] private string _garageLabel = "마차 정비공";
	[SerializeField] private string _leaveLabel = "떠나기";
	[SerializeField] private string _saveQuitLabel = "저장하고 메인 메뉴로";
	[SerializeField] private Font _font;
	[SerializeField] private Vector2 _buttonSize = new Vector2(360f, 64f);
	[SerializeField] private float _buttonSpacing = 16f;
	// 제목·버튼을 감싸는 가운데 패널
	[SerializeField] private float _panelPadding = 40f;
	[SerializeField] private float _titleHeight = 80f;
	[SerializeField] private Color _panelColor = new Color(0.05f, 0.045f, 0.04f, 0.95f);
	[SerializeField] private Color _buttonColor = new Color(0.14f, 0.12f, 0.1f, 1f);
	[SerializeField] private Color _textColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	[SerializeField] private Color _disabledTextColor = new Color(0.45f, 0.42f, 0.38f, 1f);
	[SerializeField] private int _sortingOrder = 40;
	// 마차 정비공 화면 (비우면 자식에서 찾는다, 없으면 정비소도 일반 상점 화면으로 연다)
	[SerializeField] private CGarageView _garageView;
	[SerializeField] private bool _logState = true;
	#endregion

	// 마차 정비공 화면 (없으면 null)
	public CGarageView GarageView => _garageView != null ? _garageView : (_garageView = GetComponentInChildren<CGarageView>(true));

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
	private Text _shopText;
	private Text _garageText;
	private Text _saveQuitText;
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
		SetButton(_shopButton, _shopText, shop);
		SetButton(_garageButton, _garageText, garage);
		SetButton(_saveQuitButton, _saveQuitText, saveQuit);
	}

	private void SetButton(Button button, Text label, bool available)
	{
		if (button == null)
		{
			return;
		}

		button.interactable = available;
		label.color = available ? _textColor : _disabledTextColor;
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
		_menu = menu.gameObject.AddComponent<CanvasGroup>();

		// 화면 가운데 패널 하나 → 제목 아래로 버튼 넷을 세로로 쌓는다
		const int BUTTON_COUNT = 4;
		float buttonsHeight = _buttonSize.y * BUTTON_COUNT + _buttonSpacing * (BUTTON_COUNT - 1);
		Vector2 panelSize = new Vector2(_buttonSize.x + _panelPadding * 2f, _panelPadding * 2f + _titleHeight + buttonsHeight);
		Image panelImage = CUIBuilder.CreateImage("Panel", menu, null, _panelColor);
		RectTransform panel = panelImage.rectTransform;

		panelImage.raycastTarget = true;
		panel.anchorMin = new Vector2(0.5f, 0.5f);
		panel.anchorMax = new Vector2(0.5f, 0.5f);
		panel.pivot = new Vector2(0.5f, 0.5f);
		panel.anchoredPosition = Vector2.zero;
		panel.sizeDelta = panelSize;

		Text title = CreateText("Title", panel, _title, 44);

		title.rectTransform.anchorMin = new Vector2(0f, 1f);
		title.rectTransform.anchorMax = new Vector2(1f, 1f);
		title.rectTransform.pivot = new Vector2(0.5f, 1f);
		title.rectTransform.anchoredPosition = new Vector2(0f, -_panelPadding);
		title.rectTransform.sizeDelta = new Vector2(0f, _titleHeight);

		float y = -(_panelPadding + _titleHeight);
		float step = _buttonSize.y + _buttonSpacing;

		_shopButton = CreateButton("ShopButton", panel, _shopLabel, y, NotifyShop, out _shopText);
		_garageButton = CreateButton("GarageButton", panel, _garageLabel, y - step, NotifyGarage, out _garageText);
		CreateButton("LeaveButton", panel, _leaveLabel, y - step * 2f, NotifyLeave, out _);
		_saveQuitButton = CreateButton("SaveQuitButton", panel, _saveQuitLabel, y - step * 3f, NotifySaveQuit, out _saveQuitText);
	}

	private Button CreateButton(string name, RectTransform parent, string label, float y, UnityEngine.Events.UnityAction onClick, out Text text)
	{
		Image image = CUIBuilder.CreateImage(name, parent, null, _buttonColor);
		RectTransform rect = image.rectTransform;

		image.raycastTarget = true;
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, y);
		rect.sizeDelta = _buttonSize;

		Button button = image.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;

		colors.highlightedColor = new Color(1.4f, 1.3f, 1.1f, 1f);
		colors.pressedColor = new Color(0.8f, 0.75f, 0.7f, 1f);
		colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
		button.colors = colors;
		button.onClick.AddListener(onClick);

		text = CreateText("Label", rect, label, 28);
		text.rectTransform.anchorMin = Vector2.zero;
		text.rectTransform.anchorMax = Vector2.one;
		text.rectTransform.offsetMin = Vector2.zero;
		text.rectTransform.offsetMax = Vector2.zero;

		return button;
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
		Notify(OnShopClicked, _shopLabel);
	}

	private void NotifyGarage()
	{
		Notify(OnGarageClicked, _garageLabel);
	}

	private void NotifyLeave()
	{
		Notify(OnLeaveClicked, _leaveLabel);
	}

	private void NotifySaveQuit()
	{
		Notify(OnSaveQuitClicked, _saveQuitLabel);
	}

	private void Notify(Action action, string label)
	{
		if (_logState)
		{
			CPrint.Log($"CInnScene : {label}");
		}

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
