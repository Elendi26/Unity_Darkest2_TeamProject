using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 게임 설정 창 → 배경음·효과음 음량, 해상도, 전체 화면을 고르고 '적용'을 누르면 한 번에 바뀐다
public class CSettingsPanel : MonoBehaviour
{
	#region 인스펙터
	// 창을 붙일 곳 (비우면 이 오브젝트) → 일시정지 캔버스 아래에 두면 일시정지와 함께 숨는다
	[SerializeField] private RectTransform _parent;
	// 비우면 부모 아래 TMP 글자에서 가져온다
	[SerializeField] private TMP_FontAsset _font;
	[SerializeField] private string _title = "게임 설정";
	[SerializeField] private Vector2 _panelSize = new Vector2(760f, 520f);
	[SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.6f);
	[SerializeField] private Color _panelColor = new Color(0.05f, 0.045f, 0.04f, 0.97f);
	[SerializeField] private Color _buttonColor = new Color(0.14f, 0.12f, 0.1f, 1f);
	[SerializeField] private Color _trackColor = new Color(0.2f, 0.18f, 0.15f, 1f);
	[SerializeField] private Color _fillColor = new Color(0.62f, 0.48f, 0.26f, 1f);
	[SerializeField] private Color _handleColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	[SerializeField] private Color _textColor = new Color(0.9f, 0.84f, 0.7f, 1f);
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private const float ROW_HEIGHT = 60f;
	private const float LABEL_WIDTH = 200f;
	private const float CONTROL_WIDTH = 380f;
	private const float MIN_PANEL_HEIGHT = 620f;

	private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
	private GameObject _root;
	private Slider _bgmSlider;
	private Slider _sfxSlider;
	private TextMeshProUGUI _bgmValue;
	private TextMeshProUGUI _sfxValue;
	private TextMeshProUGUI _resolutionText;
	private Toggle _fullscreenToggle;
	private Toggle _tutorialTipsToggle;
	private Button _prevResolutionButton;
	private Button _nextResolutionButton;
	// 적용 전 고른 값
	private int _resolutionIndex;
	#endregion

	// 창이 열려 있는지
	public bool IsOpen => _root != null && _root.activeSelf;

	private void Awake()
	{
		if (_parent == null)
		{
			_parent = transform as RectTransform;
		}

		if (_font == null && _parent != null)
		{
			TextMeshProUGUI sample = _parent.GetComponentInChildren<TextMeshProUGUI>(true);

			_font = sample != null ? sample.font : null;
		}

		Build();
		_root.SetActive(false);
	}

	public void Open()
	{
		if (_root == null)
		{
			return;
		}

		Refresh();
		_root.SetActive(true);
		_root.transform.SetAsLastSibling();
	}

	public void Close()
	{
		if (!IsOpen)
		{
			return;
		}

		_root.SetActive(false);
		CGameSettings.Save();
	}

	// 지금 설정 값으로 화면을 맞춘다 (적용 안 하고 닫았던 선택은 버린다)
	private void Refresh()
	{
		_bgmSlider.SetValueWithoutNotify(CGameSettings.BgmVolume);
		_sfxSlider.SetValueWithoutNotify(CGameSettings.SfxVolume);
		UpdateVolumeText();

		// 적용해 둔 해상도부터 보여 준다 (목록에 없으면 가장 가까운 것)
		Vector2Int applied = CGameSettings.GetAppliedResolution();

		CGameSettings.GetResolutions(_resolutions);

		_resolutionIndex = _resolutions.IndexOf(applied);

		if (_resolutionIndex < 0)
		{
			_resolutionIndex = CGameSettings.FindNearestResolution(_resolutions, applied);
		}

		_fullscreenToggle.SetIsOnWithoutNotify(CGameSettings.GetAppliedFullscreen());
		_tutorialTipsToggle.SetIsOnWithoutNotify(CGameSettings.TutorialTips);
		UpdateResolutionText();
	}

	// 전체 화면은 모니터 해상도로 → 고르는 화살표를 잠근다
	private void HandleFullscreenChanged(bool isOn)
	{
		UpdateResolutionText();
	}

	#region 버튼 처리
	// 슬라이더는 숫자만 바꾼다 → 실제 음량은 '적용' 때
	private void HandleVolumeChanged(float value)
	{
		UpdateVolumeText();
	}

	private void PrevResolution()
	{
		if (_resolutions.Count == 0)
		{
			return;
		}

		_resolutionIndex = (_resolutionIndex - 1 + _resolutions.Count) % _resolutions.Count;
		UpdateResolutionText();
	}

	private void NextResolution()
	{
		if (_resolutions.Count == 0)
		{
			return;
		}

		_resolutionIndex = (_resolutionIndex + 1) % _resolutions.Count;
		UpdateResolutionText();
	}

	// 배경음·효과음·해상도·전체 화면을 한 번에 적용
	private void Apply()
	{
		CGameSettings.SetBgmVolume(_bgmSlider.value);
		CGameSettings.SetSfxVolume(_sfxSlider.value);
		CGameSettings.SetTutorialTips(_tutorialTipsToggle.isOn);

		bool fullscreen = _fullscreenToggle.isOn;

		if (fullscreen)
		{
			Vector2Int monitor = CGameSettings.GetMonitorResolution();

			CGameSettings.SetResolution(monitor.x, monitor.y, true);
		}
		else if (_resolutionIndex >= 0 && _resolutionIndex < _resolutions.Count)
		{
			Vector2Int size = _resolutions[_resolutionIndex];

			CGameSettings.SetResolution(size.x, size.y, false);
		}

		CGameSettings.Save();

		if (_logState)
		{
			CPrint.Log($"CSettingsPanel : 적용 → 배경음 {_bgmValue.text}, 효과음 {_sfxValue.text}, 해상도 {_resolutionText.text} {(fullscreen ? "전체 화면" : "창 모드")}, 튜토리얼 팁 {(_tutorialTipsToggle.isOn ? "켬" : "끔")}");
		}
	}

	private void UpdateVolumeText()
	{
		_bgmValue.text = $"{Mathf.RoundToInt(_bgmSlider.value * 100f)}%";
		_sfxValue.text = $"{Mathf.RoundToInt(_sfxSlider.value * 100f)}%";
	}

	private void UpdateResolutionText()
	{
		bool fullscreen = _fullscreenToggle != null && _fullscreenToggle.isOn;

		if (_prevResolutionButton != null)
		{
			_prevResolutionButton.interactable = !fullscreen;
		}

		if (_nextResolutionButton != null)
		{
			_nextResolutionButton.interactable = !fullscreen;
		}

		// 전체 화면 → 지금 모니터 해상도를 보여 준다
		if (fullscreen)
		{
			Vector2Int monitor = CGameSettings.GetMonitorResolution();

			_resolutionText.text = $"{monitor.x} x {monitor.y}";
			return;
		}

		bool hasResolution = _resolutionIndex >= 0 && _resolutionIndex < _resolutions.Count;

		_resolutionText.text = hasResolution ? $"{_resolutions[_resolutionIndex].x} x {_resolutions[_resolutionIndex].y}" : "-";
	}
	#endregion

	#region 창 만들기
	private void Build()
	{
		// 튜토리얼 팁 줄이 늘었다 → 씬에 저장된 크기(520)가 작으면 늘린다
		_panelSize.y = Mathf.Max(_panelSize.y, MIN_PANEL_HEIGHT);

		// 화면 전체를 어둡게 덮어 뒤쪽 일시정지 버튼 클릭을 막는다
		Image dim = CUIBuilder.CreateImage("SettingsPanel", _parent, null, _dimColor);

		dim.raycastTarget = true;
		Stretch(dim.rectTransform);
		_root = dim.gameObject;

		Image panelImage = CUIBuilder.CreateImage("Panel", dim.rectTransform, null, _panelColor);
		RectTransform panel = panelImage.rectTransform;

		panelImage.raycastTarget = true;
		panel.anchorMin = new Vector2(0.5f, 0.5f);
		panel.anchorMax = new Vector2(0.5f, 0.5f);
		panel.pivot = new Vector2(0.5f, 0.5f);
		panel.anchoredPosition = Vector2.zero;
		panel.sizeDelta = _panelSize;

		TextMeshProUGUI title = CreateText("Title", panel, _title, 44f, TextAlignmentOptions.Center);

		Place(title.rectTransform, new Vector2(0f, -40f), new Vector2(_panelSize.x, 70f));

		float y = -140f;

		_bgmSlider = CreateVolumeRow(panel, "배경음", y, HandleVolumeChanged, out _bgmValue);
		y -= ROW_HEIGHT + 20f;
		_sfxSlider = CreateVolumeRow(panel, "효과음", y, HandleVolumeChanged, out _sfxValue);
		y -= ROW_HEIGHT + 20f;
		_resolutionText = CreateSelectRow(panel, "해상도", y, PrevResolution, NextResolution);
		y -= ROW_HEIGHT + 20f;
		_fullscreenToggle = CreateCheckboxRow(panel, "전체 화면", y);
		_fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged);
		y -= ROW_HEIGHT + 20f;
		_tutorialTipsToggle = CreateCheckboxRow(panel, "튜토리얼 팁", y);

		// 아래 버튼 → 적용 (모든 설정) / 닫기 (적용 안 한 선택은 버린다)
		CreateButton("ApplyButton", panel, "적용", new Vector2(-130f, 0f), new Vector2(220f, 60f), Apply, true);
		CreateButton("CloseButton", panel, "닫기", new Vector2(130f, 0f), new Vector2(220f, 60f), Close, true);
	}

	private Slider CreateVolumeRow(RectTransform panel, string label, float y, UnityAction<float> onChanged, out TextMeshProUGUI valueText)
	{
		CreateRowLabel(panel, label, y);

		RectTransform root = CUIBuilder.CreateRect(label + "Slider", panel);

		Place(root, new Vector2(LABEL_WIDTH - _panelSize.x * 0.5f + 40f + CONTROL_WIDTH * 0.5f, y), new Vector2(CONTROL_WIDTH, 24f), 0.5f);

		Image track = CUIBuilder.CreateImage("Background", root, null, _trackColor);

		Stretch(track.rectTransform);

		RectTransform fillArea = CUIBuilder.CreateRect("Fill Area", root);

		Stretch(fillArea);

		Image fill = CUIBuilder.CreateImage("Fill", fillArea, null, _fillColor);

		Stretch(fill.rectTransform);

		RectTransform handleArea = CUIBuilder.CreateRect("Handle Slide Area", root);

		Stretch(handleArea);
		handleArea.offsetMin = new Vector2(12f, 0f);
		handleArea.offsetMax = new Vector2(-12f, 0f);

		Image handle = CUIBuilder.CreateImage("Handle", handleArea, null, _handleColor);

		handle.raycastTarget = true;
		handle.rectTransform.sizeDelta = new Vector2(24f, 16f);

		// 트랙 어디를 눌러도 잡히게
		track.raycastTarget = true;

		Slider slider = root.gameObject.AddComponent<Slider>();

		slider.fillRect = fill.rectTransform;
		slider.handleRect = handle.rectTransform;
		slider.targetGraphic = handle;
		slider.direction = Slider.Direction.LeftToRight;
		slider.minValue = 0f;
		slider.maxValue = 1f;
		slider.onValueChanged.AddListener(onChanged);

		valueText = CreateText(label + "Value", panel, "100%", 28f, TextAlignmentOptions.Right);
		Place(valueText.rectTransform, new Vector2(_panelSize.x * 0.5f - 40f - 50f, y), new Vector2(100f, ROW_HEIGHT), 0.5f);

		return slider;
	}

	private TextMeshProUGUI CreateSelectRow(RectTransform panel, string label, float y, UnityAction onPrev, UnityAction onNext)
	{
		CreateRowLabel(panel, label, y);

		float centerX = LABEL_WIDTH - _panelSize.x * 0.5f + 40f + CONTROL_WIDTH * 0.5f;

		// 글꼴에 화살표 기호가 없을 수 있어 < > 로
		_prevResolutionButton = CreateButton("PrevButton", panel, "<", new Vector2(centerX - CONTROL_WIDTH * 0.5f + 30f, y), new Vector2(60f, 50f), onPrev, false);
		_nextResolutionButton = CreateButton("NextButton", panel, ">", new Vector2(centerX + CONTROL_WIDTH * 0.5f - 30f, y), new Vector2(60f, 50f), onNext, false);

		TextMeshProUGUI value = CreateText(label + "Value", panel, "-", 30f, TextAlignmentOptions.Center);

		Place(value.rectTransform, new Vector2(centerX, y), new Vector2(CONTROL_WIDTH - 140f, ROW_HEIGHT), 0.5f);

		return value;
	}

	// 이름 바로 옆에 체크박스
	private Toggle CreateCheckboxRow(RectTransform panel, string label, float y)
	{
		CreateRowLabel(panel, label, y);

		const float BOX_SIZE = 40f;
		float boxX = -_panelSize.x * 0.5f + 40f + LABEL_WIDTH + BOX_SIZE * 0.5f;

		Image box = CUIBuilder.CreateImage(label + "Checkbox", panel, null, _trackColor);

		box.raycastTarget = true;
		Place(box.rectTransform, new Vector2(boxX, y), new Vector2(BOX_SIZE, BOX_SIZE), 0.5f);

		// 켜지면 안쪽 네모가 보인다
		Image check = CUIBuilder.CreateImage("Checkmark", box.rectTransform, null, _fillColor);

		Stretch(check.rectTransform);
		check.rectTransform.offsetMin = new Vector2(8f, 8f);
		check.rectTransform.offsetMax = new Vector2(-8f, -8f);

		Toggle toggle = box.gameObject.AddComponent<Toggle>();
		ColorBlock colors = toggle.colors;

		colors.highlightedColor = new Color(1.4f, 1.3f, 1.1f, 1f);
		toggle.colors = colors;
		toggle.targetGraphic = box;
		toggle.graphic = check;
		toggle.isOn = true;

		return toggle;
	}

	private void CreateRowLabel(RectTransform panel, string label, float y)
	{
		TextMeshProUGUI text = CreateText(label + "Label", panel, label, 30f, TextAlignmentOptions.Left);

		Place(text.rectTransform, new Vector2(-_panelSize.x * 0.5f + 40f + LABEL_WIDTH * 0.5f, y), new Vector2(LABEL_WIDTH, ROW_HEIGHT), 0.5f);
	}

	// bottom 이 true 면 패널 아래쪽 기준, 아니면 다른 줄과 같은 패널 위쪽 기준 (줄 가운데에 맞춘다)
	private Button CreateButton(string name, RectTransform parent, string label, Vector2 position, Vector2 size, UnityAction onClick, bool bottom)
	{
		Image image = CUIBuilder.CreateImage(name, parent, null, _buttonColor);
		RectTransform rect = image.rectTransform;

		image.raycastTarget = true;

		if (bottom)
		{
			rect.anchorMin = new Vector2(0.5f, 0f);
			rect.anchorMax = rect.anchorMin;
			rect.pivot = new Vector2(0.5f, 0f);
			rect.anchoredPosition = position + new Vector2(0f, 36f);
			rect.sizeDelta = size;
		}
		else
		{
			Place(rect, position, size, 0.5f);
		}

		Button button = image.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;

		colors.highlightedColor = new Color(1.4f, 1.3f, 1.1f, 1f);
		colors.pressedColor = new Color(0.8f, 0.75f, 0.7f, 1f);
		button.colors = colors;
		button.onClick.AddListener(onClick);

		TextMeshProUGUI text = CreateText("Label", rect, label, 28f, TextAlignmentOptions.Center);

		Stretch(text.rectTransform);

		return button;
	}

	private TextMeshProUGUI CreateText(string name, RectTransform parent, string value, float size, TextAlignmentOptions alignment)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();

		if (_font != null)
		{
			text.font = _font;
		}

		text.fontSize = size;
		text.color = _textColor;
		text.alignment = alignment;
		text.raycastTarget = false;
		text.enableWordWrapping = false;
		text.text = value;

		return text;
	}

	// 패널 위쪽 가운데 기준 (pivotY 0.5 면 줄 가운데에 맞춘다)
	private static void Place(RectTransform rect, Vector2 position, Vector2 size, float pivotY = 1f)
	{
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, pivotY);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}
	#endregion
}
