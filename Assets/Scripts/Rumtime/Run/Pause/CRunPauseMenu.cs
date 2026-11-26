using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 주행 중 일시정지 → ESC 로 열고 닫는다. 계속하기 / 저장하고 메인 메뉴로 / 저장하고 게임 종료
public class CRunPauseMenu : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private InputAction _toggleAction = new InputAction("TogglePause", InputActionType.Button, "<Keyboard>/escape");
	[SerializeField] private CanvasGroup _menuGroup;
	// 게임 설정 창 (비우면 설정 버튼은 아무것도 안 한다)
	[SerializeField] private CSettingsPanel _settingsPanel;
	// 원정 포기 때 다시 시작할 튜토리얼 게임 모드 ID
	[SerializeField] private string _tutorialModeId = "Tutorial";
	[SerializeField] private string _title = "일시정지";
	[SerializeField] private string _resumeLabel = "계속하기";
	[SerializeField] private string _mainMenuLabel = "메인 메뉴로 나가기";
	[SerializeField] private Font _font;
	[SerializeField] private Vector2 _buttonSize = new Vector2(360f, 64f);
	[SerializeField] private float _buttonSpacing = 16f;
	[SerializeField] private float _panelPadding = 40f;
	[SerializeField] private float _titleHeight = 80f;
	// 패널 뒤로 화면을 어둡게 덮는 색
	[SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.6f);
	[SerializeField] private Color _panelColor = new Color(0.05f, 0.045f, 0.04f, 0.95f);
	[SerializeField] private Color _buttonColor = new Color(0.14f, 0.12f, 0.1f, 1f);
	[SerializeField] private Color _textColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	// 다른 주행 UI 보다 위에 그린다
	[SerializeField] private int _sortingOrder = 100;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private CanvasGroup _menu;
	private bool _isPaused;
	// 열기 전 시간 배율 → 닫을 때 그대로 돌려준다 (지도 등이 따로 멈춰 둔 경우)
	private float _prevTimeScale = 1f;
	private bool _isLeaving;
	#endregion

	private void Awake()
	{
		if (_menuGroup != null)
		{
			_menu = _menuGroup;
		}
		else
		{
			if (_font == null)
			{
				_font = CUIBuilder.GetDefaultFont();
			}

			BuildMenu();
		}

		SetMenuVisible(false);
	}

	private void OnEnable()
	{
		if (_toggleAction == null)
		{
			return;
		}

		_toggleAction.performed += HandleTogglePerformed;
		_toggleAction.Enable();
	}

	private void OnDisable()
	{
		if (_toggleAction != null)
		{
			_toggleAction.performed -= HandleTogglePerformed;
			_toggleAction.Disable();
		}
	}

	// 멈춘 채로 씬이 내려가도 시간이 멈춰 있지 않게
	private void OnDestroy()
	{
		if (_isPaused)
		{
			Time.timeScale = _prevTimeScale;
		}
	}

	private void HandleTogglePerformed(InputAction.CallbackContext context)
	{
		// 설정 창이 열려 있으면 ESC 는 설정 창만 닫는다
		if (_isPaused && _settingsPanel != null && _settingsPanel.IsOpen)
		{
			_settingsPanel.Close();
			return;
		}

		if (_isPaused)
		{
			Resume();
			return;
		}

		Pause();
	}

	public void Pause()
	{
		if (_isPaused || _isLeaving)
		{
			return;
		}

		// 여관·교차로 등 씬이 겹쳐 열려 있으면 맨 위(마지막에 연) 씬의 일시정지만 연다
		if (!IsTopScene())
		{
			return;
		}

		// 상점은 ESC 로 닫힌다 → 같은 키로 일시정지까지 열리지 않게
		if (IsShopOpen())
		{
			return;
		}

		_isPaused = true;
		_prevTimeScale = Time.timeScale;
		Time.timeScale = 0f;
		SetMenuVisible(true);

		if (_logState)
		{
			CPrint.Log("CRunPauseMenu : 일시정지");
		}
	}

	private bool IsTopScene()
	{
		for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
		{
			Scene scene = SceneManager.GetSceneAt(i);

			// 아직 불러오는 중인 씬은 건너뛴다
			if (!scene.isLoaded)
			{
				continue;
			}

			return scene == gameObject.scene;
		}

		return true;
	}

	private static bool IsShopOpen()
	{
		CShopView[] views = FindObjectsByType<CShopView>(FindObjectsSortMode.None);

		for (int i = 0; i < views.Length; i++)
		{
			if (views[i] != null && views[i].IsOpen)
			{
				return true;
			}
		}

		// 여관 마차 정비공 화면도 ESC 로 닫힌다
		CGarageView[] garages = FindObjectsByType<CGarageView>(FindObjectsSortMode.None);

		for (int i = 0; i < garages.Length; i++)
		{
			if (garages[i] != null && garages[i].IsOpen)
			{
				return true;
			}
		}

		return false;
	}

	public void Resume()
	{
		if (!_isPaused)
		{
			return;
		}

		_isPaused = false;
		Time.timeScale = _prevTimeScale;
		SetMenuVisible(false);

		if (_settingsPanel != null)
		{
			_settingsPanel.Close();
		}

		if (_logState)
		{
			CPrint.Log("CRunPauseMenu : 계속하기");
		}
	}

	// 게임 설정 버튼
	public void OpenSettings()
	{
		if (!_isPaused || _settingsPanel == null)
		{
			return;
		}

		_settingsPanel.Open();
	}

	public void QuitToMainMenu()
	{
		CGameManager gameManager = FindGameManager();

		if (gameManager == null)
		{
			return;
		}

		LeaveTo(gameManager, gameManager.TitleModeId, true);
	}

	// 원정 포기 → 저장된 런을 지우고 튜토리얼부터 새로 시작 (튜토리얼 모드가 없으면 주행 씬부터)
	public void AbandonRun()
	{
		CGameManager gameManager = FindGameManager();

		if (gameManager == null)
		{
			return;
		}

		string modeId = gameManager.GetGameModeData(_tutorialModeId) != null ? _tutorialModeId : gameManager.NodeModeId;

		LeaveTo(gameManager, modeId, false);
	}

	// 씬에 놓인 매니저만 찾는다 → Instance 로 빈 매니저를 새로 만들지 않는다
	private static CGameManager FindGameManager()
	{
		CGameManager gameManager = FindFirstObjectByType<CGameManager>();

		if (gameManager == null || gameManager.GetGameModeData(gameManager.TitleModeId) == null)
		{
			CPrint.Warn("CRunPauseMenu : 게임 모드를 찾을 수 없어 씬을 옮기지 않습니다. 타이틀 씬부터 재생하세요.");
			return null;
		}

		return gameManager;
	}

	private void LeaveTo(CGameManager gameManager, string modeId, bool saveRun)
	{
		if (_isLeaving)
		{
			return;
		}

		CAddressableSceneManager sceneManager = CAddressableSceneManager.Instance;

		if (sceneManager == null || sceneManager.IsTransitioning)
		{
			return;
		}

		if (saveRun)
		{
			SaveRun();
		}
		else
		{
			// 원정 포기 → 런과 튜토리얼 완료 기록을 함께 지운다 (타이틀에서 새로 시작해도 튜토리얼부터)
			int slot = CTutorialProgress.CurrentSlot;

			CRunCarry.Take();
			CRunProfileSave.ClearRun(slot);
			CTutorialProgress.ResetDone(slot);
		}

		// 페이드·씬 로드가 시간 배율에 묶이지 않게 먼저 풀어 둔다
		_isLeaving = true;
		_isPaused = false;
		Time.timeScale = 1f;

		if (_logState)
		{
			CPrint.Log(saveRun ? $"CRunPauseMenu : 저장하고 {modeId} 로 이동합니다." : $"CRunPauseMenu : 원정을 포기하고 {modeId} 부터 다시 시작합니다.");
		}

		gameManager.ChangeGameMode(modeId);
	}

	public void QuitGame()
	{
		if (_isLeaving)
		{
			return;
		}

		SaveRun();

		_isLeaving = true;
		_isPaused = false;
		Time.timeScale = 1f;

		if (_logState)
		{
			CPrint.Log("CRunPauseMenu : 저장하고 게임을 종료합니다.");
		}

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}

	// 이어하기는 지금 주행 씬부터 다시 연다
	private void SaveRun()
	{
		CStagecoachController controller = FindFirstObjectByType<CStagecoachController>();

		if (controller == null || controller.RunData == null)
		{
			CPrint.Warn("CRunPauseMenu : 역마차가 없어 저장하지 않고 나갑니다.");
			return;
		}

		// 여관·교차로에서 저장해도 이어하기는 역마차가 있는 주행 씬으로
		CRunCarry.Save(controller.RunData, controller.Stagecoach, controller.gameObject.scene.path);
	}

	private void SetMenuVisible(bool visible)
	{
		if (_menu == null)
		{
			return;
		}

		_menu.alpha = visible ? 1f : 0f;
		_menu.interactable = visible;
		_menu.blocksRaycasts = visible;
	}

	#region 메뉴 만들기
	private void BuildMenu()
	{
		RectTransform canvas = CUIBuilder.CreateOverlayCanvas("PauseMenuCanvas", transform, _sortingOrder, true);

		// 화면 전체를 어둡게 덮어 뒤쪽 주행 UI 클릭을 막는다
		Image dim = CUIBuilder.CreateImage("Dim", canvas, null, _dimColor);
		RectTransform menu = dim.rectTransform;

		dim.raycastTarget = true;
		menu.anchorMin = Vector2.zero;
		menu.anchorMax = Vector2.one;
		menu.offsetMin = Vector2.zero;
		menu.offsetMax = Vector2.zero;
		_menu = menu.gameObject.AddComponent<CanvasGroup>();

		const int BUTTON_COUNT = 2;
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

		CreateButton("ResumeButton", panel, _resumeLabel, y, Resume);
		CreateButton("MainMenuButton", panel, _mainMenuLabel, y - step, QuitToMainMenu);
	}

	private void CreateButton(string name, RectTransform parent, string label, float y, UnityAction onClick)
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
		button.colors = colors;
		button.onClick.AddListener(onClick);

		Text text = CreateText("Label", rect, label, 28);

		text.rectTransform.anchorMin = Vector2.zero;
		text.rectTransform.anchorMax = Vector2.one;
		text.rectTransform.offsetMin = Vector2.zero;
		text.rectTransform.offsetMax = Vector2.zero;
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
}
