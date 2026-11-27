using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 튜토리얼 동안 처음 겪는 상황마다 짧은 팁 카드를 띄운다 → 설정의 '튜토리얼 팁' 체크로 끄고 켠다
public class CTutorialTipDirector : MonoBehaviour
{
	#region 상수
	private const int SORTING_ORDER = 840;
	private const float CARD_WIDTH = 560f;
	private const float PADDING = 22f;
	private const float TITLE_HEIGHT = 34f;
	private const float BUTTON_HEIGHT = 40f;
	private const float FADE_TIME = 0.2f;
	private const int BODY_FONT_SIZE = 22;

	private static readonly Color PANEL_COLOR = new Color(0.05f, 0.045f, 0.04f, 0.94f);
	private static readonly Color LINE_COLOR = new Color(0.45f, 0.38f, 0.26f, 1f);
	private static readonly Color TITLE_COLOR = new Color(0.95f, 0.78f, 0.38f, 1f);
	private static readonly Color TEXT_COLOR = new Color(0.9f, 0.84f, 0.7f, 1f);
	private static readonly Color BUTTON_COLOR = new Color(0.14f, 0.12f, 0.1f, 1f);
	#endregion

	#region 인스펙터
	// 화면 위쪽 가운데 기준 카드 위치
	[SerializeField] private Vector2 _cardPosition = new Vector2(0f, -150f);
	// 확인을 안 눌러도 이 시간이 지나면 닫힌다 (0 이면 누를 때까지)
	[SerializeField] private float _autoHideSeconds = 12f;
	// 카드 하나를 닫고 다음 카드까지 쉬는 시간
	[SerializeField] private float _gapSeconds = 0.6f;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	// 이번 실행에서 이미 띄운 팁 → 새 고해를 시작하면 비운다
	private static readonly HashSet<ETutorialTip> _seen = new HashSet<ETutorialTip>();

	private readonly Queue<ETutorialTip> _queue = new Queue<ETutorialTip>();
	private bool _isTutorial;

	private CStagecoachController _controller;
	private CRoadTravel _travel;
	private CRunData _runData;
	private CMapController _mapController;
	private CMapRunner _runner;

	private RectTransform _canvas;
	private CanvasGroup _group;
	private RectTransform _card;
	private Text _title;
	private Text _body;
	private Font _font;

	private bool _isShowing;
	private float _shownTime;
	private float _hiddenTime;
	#endregion

	#region 자동 생성
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_seen.Clear();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;

		// 처음 열린 씬은 sceneLoaded 가 이미 지나갔다
		TryAttach(SceneManager.GetActiveScene());
	}

	private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		TryAttach(scene);
	}

	// 마차 씬이고, 아직 붙은 게 없으면 그 씬에 하나 만든다 (씬이 내려가면 함께 사라진다)
	private static void TryAttach(Scene scene)
	{
		if (FindFirstObjectByType<CTutorialTipDirector>() != null)
		{
			return;
		}

		GameObject[] roots = scene.GetRootGameObjects();
		bool hasCoach = false;

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] != null && roots[i].GetComponentInChildren<CStagecoachController>(true) != null)
			{
				hasCoach = true;
				break;
			}
		}

		if (!hasCoach)
		{
			return;
		}

		GameObject go = new GameObject("TutorialTipDirector");

		SceneManager.MoveGameObjectToScene(go, scene);
		go.AddComponent<CTutorialTipDirector>();
	}

	// 새 고해는 늘 튜토리얼 씬부터라 프로필의 완료 기록 대신 씬의 흐름 설정을 본다
	private static bool IsTutorialScene(Scene scene)
	{
		GameObject[] roots = scene.GetRootGameObjects();

		for (int i = 0; i < roots.Length; i++)
		{
			if (roots[i] == null)
			{
				continue;
			}

			CRunSceneFlow flow = roots[i].GetComponentInChildren<CRunSceneFlow>(true);

			if (flow != null)
			{
				return flow.CompletesTutorial;
			}
		}

		return false;
	}

	// 새 고해 → 팁을 처음부터 다시 띄운다
	public static void ResetSeen()
	{
		_seen.Clear();
	}
	#endregion

	private void Awake()
	{
		_isTutorial = IsTutorialScene(gameObject.scene);
		_font = CUIBuilder.GetDefaultFont();
	}

	private void OnEnable()
	{
		CGameSettings.OnTutorialTipsChanged += HandleTipsChanged;
	}

	private void OnDisable()
	{
		CGameSettings.OnTutorialTipsChanged -= HandleTipsChanged;
	}

	private void OnDestroy()
	{
		UnBind();
	}

	private void Update()
	{
		if (!_isTutorial)
		{
			return;
		}

		TryBind();
		TickCard();
	}

	#region 상황 감지
	// 마차·지도는 씬이 열린 뒤에 만들어질 수 있다 → 생길 때까지 매 프레임 찾는다
	private void TryBind()
	{
		if (_controller == null)
		{
			_controller = FindFirstObjectByType<CStagecoachController>();
		}

		if (_controller != null && _travel == null && _controller.Travel != null)
		{
			_travel = _controller.Travel;
			_travel.OnStateChanged += HandleTravelStateChanged;
			_travel.OnRoadEventStarted += HandleRoadEventStarted;
			_travel.OnRoadCombatStarted += HandleRoadCombatStarted;
		}

		if (_controller != null && _runData == null && _controller.RunData != null)
		{
			_runData = _controller.RunData;
			_runData.OnValueChanged += HandleValueChanged;
		}

		if (_mapController == null)
		{
			_mapController = FindFirstObjectByType<CMapController>();

			if (_mapController != null)
			{
				_mapController.OnRunnerCreated += BindRunner;
			}
		}

		if (_mapController != null && _runner == null && _mapController.Runner != null)
		{
			BindRunner(_mapController.Runner);
		}
	}

	private void BindRunner(CMapRunner runner)
	{
		if (runner == null || runner == _runner)
		{
			return;
		}

		if (_runner != null)
		{
			_runner.OnStateChanged -= HandleMapStateChanged;
			_runner.OnNodeEntered -= HandleNodeEntered;
		}

		_runner = runner;
		_runner.OnStateChanged += HandleMapStateChanged;
		_runner.OnNodeEntered += HandleNodeEntered;
	}

	private void UnBind()
	{
		if (_travel != null)
		{
			_travel.OnStateChanged -= HandleTravelStateChanged;
			_travel.OnRoadEventStarted -= HandleRoadEventStarted;
			_travel.OnRoadCombatStarted -= HandleRoadCombatStarted;
			_travel = null;
		}

		if (_runData != null)
		{
			_runData.OnValueChanged -= HandleValueChanged;
			_runData = null;
		}

		if (_mapController != null)
		{
			_mapController.OnRunnerCreated -= BindRunner;
			_mapController = null;
		}

		if (_runner != null)
		{
			_runner.OnStateChanged -= HandleMapStateChanged;
			_runner.OnNodeEntered -= HandleNodeEntered;
			_runner = null;
		}
	}

	private void HandleTravelStateChanged(ETravelState prev, ETravelState next)
	{
		if (next == ETravelState.Driving)
		{
			Notify(ETutorialTip.Departure);
		}
	}

	private void HandleMapStateChanged(EMapState prev, EMapState next)
	{
		if (next == EMapState.SelectingPath)
		{
			Notify(ETutorialTip.Fork);
		}
	}

	private void HandleNodeEntered(CNodeInstance node)
	{
		Notify(ETutorialTip.NodeArrive);
	}

	private void HandleRoadEventStarted(CRoadEventData data)
	{
		Notify(ETutorialTip.RoadEvent);
	}

	private void HandleRoadCombatStarted(CRoadEventData data)
	{
		Notify(ETutorialTip.RoadCombat);
	}

	private void HandleValueChanged(ERunValue type, int prev, int current)
	{
		if (type == ERunValue.Torch && current < prev)
		{
			Notify(ETutorialTip.TorchDown);
		}
		else if (type == ERunValue.Gold && current > prev)
		{
			Notify(ETutorialTip.GoldGain);
		}
	}

	// 설정에서 끄면 → 떠 있는 카드와 기다리는 카드를 모두 치운다
	private void HandleTipsChanged(bool isOn)
	{
		if (isOn)
		{
			return;
		}

		_queue.Clear();
		Hide();
	}
	#endregion

	#region 카드
	private void Notify(ETutorialTip tip)
	{
		if (!_isTutorial || !CGameSettings.TutorialTips || _seen.Contains(tip) || _queue.Contains(tip))
		{
			return;
		}

		_queue.Enqueue(tip);
	}

	private void TickCard()
	{
		float now = Time.unscaledTime;

		if (_isShowing)
		{
			_group.alpha = Mathf.Clamp01((now - _shownTime) / FADE_TIME);

			if (_autoHideSeconds > 0f && now - _shownTime >= _autoHideSeconds)
			{
				Hide();
			}

			return;
		}

		// 화면이 검게 덮여 있는 동안(씬 전환 페이드)은 기다린다
		if (_queue.Count == 0 || now - _hiddenTime < _gapSeconds || CRunScreenFade.IsCovered)
		{
			return;
		}

		ShowNext(_queue.Dequeue());
	}

	private void ShowNext(ETutorialTip tip)
	{
		if (!GetText(tip, out string title, out string body))
		{
			return;
		}

		_seen.Add(tip);
		EnsureCard();

		_title.text = title;
		_body.text = body;

		// 본문 줄 수에 맞춰 카드 높이를 정한다
		float bodyWidth = CARD_WIDTH - PADDING * 2f;
		TextGenerationSettings settings = _body.GetGenerationSettings(new Vector2(bodyWidth, 0f));
		float bodyHeight = _body.cachedTextGeneratorForLayout.GetPreferredHeight(body, settings) / _body.pixelsPerUnit;
		float height = PADDING + TITLE_HEIGHT + 10f + bodyHeight + 14f + BUTTON_HEIGHT + PADDING;

		_card.sizeDelta = new Vector2(CARD_WIDTH, height);
		_body.rectTransform.sizeDelta = new Vector2(bodyWidth, bodyHeight);

		_group.alpha = 0f;
		_canvas.gameObject.SetActive(true);
		_isShowing = true;
		_shownTime = Time.unscaledTime;

		if (_logState)
		{
			CPrint.Log($"CTutorialTipDirector : 팁 '{title}' 표시");
		}
	}

	private void Hide()
	{
		if (!_isShowing)
		{
			return;
		}

		_isShowing = false;
		_hiddenTime = Time.unscaledTime;
		_canvas.gameObject.SetActive(false);
	}

	private void EnsureCard()
	{
		if (_canvas != null)
		{
			return;
		}

		// 카드만 클릭을 받는다 → 나머지 화면은 그대로 누를 수 있다
		_canvas = CUIBuilder.CreateOverlayCanvas("TutorialTipCanvas", transform, SORTING_ORDER, true);
		_group = _canvas.gameObject.AddComponent<CanvasGroup>();

		Image panel = CUIBuilder.CreateImage("Card", _canvas, null, PANEL_COLOR);

		panel.raycastTarget = true;
		_card = panel.rectTransform;
		_card.anchorMin = new Vector2(0.5f, 1f);
		_card.anchorMax = new Vector2(0.5f, 1f);
		_card.pivot = new Vector2(0.5f, 1f);
		_card.anchoredPosition = _cardPosition;

		// 위쪽 금색 줄
		Image line = CUIBuilder.CreateImage("TopLine", _card, null, LINE_COLOR);

		line.rectTransform.anchorMin = new Vector2(0f, 1f);
		line.rectTransform.anchorMax = new Vector2(1f, 1f);
		line.rectTransform.pivot = new Vector2(0.5f, 1f);
		line.rectTransform.anchoredPosition = Vector2.zero;
		line.rectTransform.sizeDelta = new Vector2(0f, 3f);

		_title = CreateText("Title", _card, 26, TITLE_COLOR, TextAnchor.MiddleLeft);
		_title.fontStyle = FontStyle.Bold;
		PlaceTop(_title.rectTransform, PADDING, new Vector2(CARD_WIDTH - PADDING * 2f, TITLE_HEIGHT));

		_body = CreateText("Body", _card, BODY_FONT_SIZE, TEXT_COLOR, TextAnchor.UpperLeft);
		_body.horizontalOverflow = HorizontalWrapMode.Wrap;
		_body.verticalOverflow = VerticalWrapMode.Overflow;
		_body.lineSpacing = 1.1f;
		PlaceTop(_body.rectTransform, PADDING + TITLE_HEIGHT + 10f, new Vector2(CARD_WIDTH - PADDING * 2f, 0f));

		CreateButton();
		_canvas.gameObject.SetActive(false);
	}

	// 카드 오른쪽 아래 '확인'
	private void CreateButton()
	{
		Image image = CUIBuilder.CreateImage("ConfirmButton", _card, null, BUTTON_COLOR);
		RectTransform rect = image.rectTransform;

		image.raycastTarget = true;
		rect.anchorMin = new Vector2(1f, 0f);
		rect.anchorMax = new Vector2(1f, 0f);
		rect.pivot = new Vector2(1f, 0f);
		rect.anchoredPosition = new Vector2(-PADDING, PADDING);
		rect.sizeDelta = new Vector2(120f, BUTTON_HEIGHT);

		Button button = image.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;

		colors.highlightedColor = new Color(1.4f, 1.3f, 1.1f, 1f);
		colors.pressedColor = new Color(0.8f, 0.75f, 0.7f, 1f);
		button.colors = colors;
		button.onClick.AddListener(Hide);

		Text label = CreateText("Label", rect, 20, TEXT_COLOR, TextAnchor.MiddleCenter);

		label.text = "확인";
		label.rectTransform.anchorMin = Vector2.zero;
		label.rectTransform.anchorMax = Vector2.one;
		label.rectTransform.offsetMin = Vector2.zero;
		label.rectTransform.offsetMax = Vector2.zero;
	}

	private Text CreateText(string name, RectTransform parent, int size, Color color, TextAnchor alignment)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();

		text.font = _font;
		text.fontSize = size;
		text.color = color;
		text.alignment = alignment;
		text.raycastTarget = false;

		return text;
	}

	// 카드 위쪽 가운데 기준, top 만큼 내려서 놓는다
	private static void PlaceTop(RectTransform rect, float top, Vector2 size)
	{
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, -top);
		rect.sizeDelta = size;
	}
	#endregion

	#region 팁 내용
	private static bool GetText(ETutorialTip tip, out string title, out string body)
	{
		switch (tip)
		{
			case ETutorialTip.Departure:
				title = "고해의 길";
				body = "I 키로 짐칸을, M 키로 지도를 엽니다.\n파티 초상을 누르면 영웅 정보와 장신구 칸을 볼 수 있습니다.";
				return true;

			case ETutorialTip.Fork:
				title = "갈림길";
				body = "표지판을 꾹 누르거나 A · W · D (← · ↑ · →) 키를 누르고 있으면 갈 길을 고릅니다.\n지도(M)에서 앞에 놓인 노드를 미리 볼 수 있습니다.";
				return true;

			case ETutorialTip.NodeArrive:
				title = "노드 도착";
				body = "노드에 닿으면 띠가 내려옵니다.\n띠를 누르거나 Enter 키를 누르면 이벤트가 시작됩니다.";
				return true;

			case ETutorialTip.RoadEvent:
				title = "길 위의 사건";
				body = "길에서 사건을 만나면 선택지를 고릅니다.\n고른 결과에 따라 자원을 얻거나 잃습니다.";
				return true;

			case ETutorialTip.RoadCombat:
				title = "전투";
				body = "길이 막히거나 습격을 받으면 전투가 벌어집니다.\n이기면 유물과 횃불을 얻고, 운이 좋으면 장신구도 얻습니다.";
				return true;

			case ETutorialTip.TorchDown:
				title = "횃불";
				body = "횃불 수치에 따라 밝기 단계가 바뀝니다.\n마차 횃불에 마우스를 올리면 지금 수치를 볼 수 있습니다.";
				return true;

			case ETutorialTip.GoldGain:
				title = "유물";
				body = "유물은 상점에서 물건을 살 때 쓰는 재화입니다.\n길가 수풀이나 전투에서 얻을 수 있습니다.";
				return true;
		}

		title = null;
		body = null;
		return false;
	}
	#endregion
}
