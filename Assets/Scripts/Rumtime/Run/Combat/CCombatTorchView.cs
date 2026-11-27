using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 씬 횃불(팀원 TorchManager·횃불 UI)을 런 횃불과 잇고, 마차 횃불처럼 불빛을 낸다

public class CCombatTorchView : MonoBehaviour
{
	#region 상수
	private const int RADIAL_SIZE = 64;
	private const int LINE_WIDTH = 128;
	private const int LINE_HEIGHT = 16;
	private const int COMBAT_TORCH_MAX = 100;
	#endregion

	#region 인스펙터
	// 횃불 0 → 가득 일 때 크기 (픽셀) → 마차 횃불의 빛무리·빛줄기와 같은 비율로 키운다
	[SerializeField] private Vector2 _glowSize = new Vector2(60f, 240f);
	[SerializeField] private Vector2 _coreSize = new Vector2(18f, 64f);
	[SerializeField] private Vector2 _streakLength = new Vector2(100f, 900f);
	[SerializeField] private float _streakThickness = 12f;
	[SerializeField] private Color _glowColor = new Color(1f, 0.55f, 0.2f, 1f);
	[SerializeField] private Color _coreColor = new Color(1f, 0.85f, 0.55f, 1f);
	[SerializeField] private Vector2 _glowAlpha = new Vector2(0.2f, 0.85f);
	[SerializeField] private float _streakAlpha = 0.6f;
	[SerializeField, Range(0f, 0.5f)] private float _flickerAmount = 0.12f;
	[SerializeField] private float _flickerSpeed = 7f;
	[SerializeField] private float _levelSmooth = 3f;

	[Header("툴팁 (팀원 TochiPopup)")]
	// 폭은 그대로 두고 높이를 폭의 이 비율로 낮춘다
	[SerializeField] private float _popupAspect = 0.26f;
	// 낮춘 판 안에서 글자 묶음을 옮길 거리 (판 기준) → 위아래 여백을 맞춘다
	[SerializeField] private float _popupContentShift = -6f;
	// 화면 윗변에서 띄울 거리 (캔버스 기준)
	[SerializeField] private float _popupTopMargin = 0f;
	// 툴팁 글자 크기 배율 (글자마다 제자리에서 줄인다)
	[SerializeField] private float _popupTextScale = 0.75f;

	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private CRunValue _runTorch;
	private TorchManager _manager;
	private RectTransform _glow;
	private RectTransform _core;
	private RectTransform _streak;
	private RawImage _glowImage;
	private RawImage _coreImage;
	private RawImage _streakImage;
	private Texture2D _radialTexture;
	private Texture2D _lineTexture;
	private float _level;
	private float _seed;
	// 마지막으로 맞춘 전투 쪽 수치 → 달라지면 전투 쪽에서 바꾼 것
	private int _lastCombatValue = -1;
	private bool _isPushed;
	#endregion

	public void Setup(CRunValue runTorch, TorchManager manager, Image fireImage, RectTransform popupPanel)
	{
		_runTorch = runTorch;
		_manager = manager;
		_seed = Random.Range(0f, 100f);

		if (_runTorch != null)
		{
			_runTorch.OnChanged += HandleRunChanged;
		}

		if (fireImage != null)
		{
			Build(fireImage);
		}

		FitPopup(popupPanel);

		_level = GetTargetLevel();
		TickFlame();
	}

	// 글자마다 제 중심에서 줄인다 → 자리는 그대로, 글씨만 작아진다
	private void ScalePopupTexts(RectTransform panel)
	{
		if (Mathf.Approximately(_popupTextScale, 1f) || _popupTextScale <= 0f)
		{
			return;
		}

		TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);

		for (int i = 0; i < texts.Length; i++)
		{
			Transform text = texts[i].transform;

			text.localScale = new Vector3(text.localScale.x * _popupTextScale, text.localScale.y * _popupTextScale, text.localScale.z);
		}
	}

	// 툴팁 판 높이만 낮추고 화면 윗변에 붙인다
	private void FitPopup(RectTransform panel)
	{
		if (panel == null || _popupAspect <= 0f || panel.localScale.y <= 0f)
		{
			return;
		}

		float width = panel.rect.width * panel.localScale.x;
		float targetHeight = width * _popupAspect / panel.localScale.y;
		float cut = panel.rect.height - targetHeight;

		if (cut > 0f)
		{
			panel.sizeDelta = new Vector2(panel.sizeDelta.x, panel.sizeDelta.y - cut);

			for (int i = 0; i < panel.childCount; i++)
			{
				RectTransform child = panel.GetChild(i) as RectTransform;

				if (child != null)
				{
					child.anchoredPosition += new Vector2(0f, _popupContentShift);
				}
			}
		}

		ScalePopupTexts(panel);

		Canvas canvas = panel.GetComponentInParent<Canvas>();

		if (canvas == null)
		{
			return;
		}

		// 화면 윗변과 판 윗변의 차이만큼 올린다 (월드 좌표로 맞춰 부모 위치와 상관없게)
		RectTransform root = (RectTransform)canvas.rootCanvas.transform;
		Vector3[] rootCorners = new Vector3[4];
		Vector3[] panelCorners = new Vector3[4];

		root.GetWorldCorners(rootCorners);
		panel.GetWorldCorners(panelCorners);

		float margin = _popupTopMargin * root.lossyScale.y;

		panel.position += new Vector3(0f, rootCorners[1].y - margin - panelCorners[1].y, 0f);
	}

	private void OnDestroy()
	{
		if (_runTorch != null)
		{
			_runTorch.OnChanged -= HandleRunChanged;
		}

		if (_radialTexture != null)
		{
			Destroy(_radialTexture);
		}

		if (_lineTexture != null)
		{
			Destroy(_lineTexture);
		}
	}

	private void Update()
	{
		SyncTorch();
		TickFlame();
	}

	#region 수치 맞추기
	private void SyncTorch()
	{
		if (_manager == null || _runTorch == null)
		{
			return;
		}

		// 전투 쪽 Start 의 무작위 수치를 런 수치로 덮는다
		if (!_isPushed)
		{
			_isPushed = true;
			PushToCombat();

			return;
		}

		int combat = _manager.CurrentTorchValue;

		if (combat == _lastCombatValue)
		{
			return;
		}

		// 전투 쪽에서 바뀌었다 → 런에 넣는다
		_lastCombatValue = combat;

		int target = Mathf.RoundToInt(combat / (float)COMBAT_TORCH_MAX * _runTorch.Max);

		if (target != _runTorch.Current)
		{
			_runTorch.Add(target - _runTorch.Current);

			if (_logState)
			{
				CPrint.Log($"CCombatTorchView : 전투에서 횃불 {combat} → 런 횃불 {_runTorch.Current}");
			}
		}
	}

	private void HandleRunChanged(ERunValue type, int prev, int current)
	{
		PushToCombat();
	}

	private void PushToCombat()
	{
		if (_manager == null || _runTorch == null)
		{
			return;
		}

		_manager.SetTorchValue(Mathf.RoundToInt(_runTorch.Ratio * COMBAT_TORCH_MAX));
		_lastCombatValue = _manager.CurrentTorchValue;
	}

	private float GetTargetLevel()
	{
		if (_manager != null)
		{
			return Mathf.Clamp01(_manager.CurrentTorchValue / (float)COMBAT_TORCH_MAX);
		}

		return _runTorch != null ? _runTorch.Ratio : 1f;
	}
	#endregion

	#region 불빛
	// 마차 횃불처럼 수치에 따라 커지고 일렁인다
	private void TickFlame()
	{
		if (_glow == null)
		{
			return;
		}

		_level = Mathf.MoveTowards(_level, GetTargetLevel(), _levelSmooth * Time.unscaledDeltaTime);

		float noise = Mathf.PerlinNoise(Time.unscaledTime * _flickerSpeed, _seed) - 0.5f;
		float flicker = 1f + noise * 2f * _flickerAmount;
		float glow = Mathf.Lerp(_glowSize.x, _glowSize.y, _level) * flicker;
		float core = Mathf.Lerp(_coreSize.x, _coreSize.y, _level) * flicker;
		float alpha = Mathf.Lerp(_glowAlpha.x, _glowAlpha.y, _level) * flicker;

		_glow.sizeDelta = new Vector2(glow, glow);
		_core.sizeDelta = new Vector2(core, core * 1.3f);
		_streak.sizeDelta = new Vector2(Mathf.Lerp(_streakLength.x, _streakLength.y, _level) * flicker, _streakThickness);

		_glowImage.color = WithAlpha(_glowColor, Mathf.Clamp01(alpha));
		_coreImage.color = WithAlpha(_coreColor, Mathf.Clamp01(alpha + 0.15f));
		_streakImage.color = WithAlpha(_glowColor, Mathf.Clamp01(_streakAlpha * _level * flicker));
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		color.a = alpha;
		return color;
	}

	// 불꽃 칸 자리에 빛을 깐다
	private void Build(Image fireImage)
	{
		RectTransform fire = fireImage.rectTransform;
		Transform parent = fire.parent;
		int index = fire.GetSiblingIndex();

		_radialTexture = CreateRadialTexture();
		_lineTexture = CreateLineTexture();

		_streakImage = CreateLayer("TorchStreak", parent, fire, _lineTexture, out _streak);
		_glowImage = CreateLayer("TorchGlow", parent, fire, _radialTexture, out _glow);
		_coreImage = CreateLayer("TorchCore", parent, fire, _radialTexture, out _core);

		_streak.SetSiblingIndex(index + 1);
		_glow.SetSiblingIndex(index + 2);
		_core.SetSiblingIndex(index + 3);

		// 그림이 없으면 흰 네모만 보인다 → 숨긴다
		if (fireImage.sprite == null)
		{
			fireImage.enabled = false;
		}
	}

	private static RawImage CreateLayer(string name, Transform parent, RectTransform at, Texture texture, out RectTransform rect)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));

		go.transform.SetParent(parent, false);
		rect = (RectTransform)go.transform;
		rect.anchorMin = at.anchorMin;
		rect.anchorMax = at.anchorMax;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = at.anchoredPosition;

		RawImage image = go.AddComponent<RawImage>();

		image.texture = texture;
		image.raycastTarget = false;
		return image;
	}

	// 가운데가 밝고 바깥으로 부드럽게 사라지는 원
	private static Texture2D CreateRadialTexture()
	{
		Texture2D texture = new Texture2D(RADIAL_SIZE, RADIAL_SIZE, TextureFormat.RGBA32, false);

		texture.name = "CombatTorchRadial";
		texture.wrapMode = TextureWrapMode.Clamp;

		float half = (RADIAL_SIZE - 1) * 0.5f;

		for (int y = 0; y < RADIAL_SIZE; y++)
		{
			for (int x = 0; x < RADIAL_SIZE; x++)
			{
				float distance = Mathf.Clamp01(new Vector2((x - half) / half, (y - half) / half).magnitude);
				float alpha = 1f - distance;

				texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha * alpha));
			}
		}

		texture.Apply();
		return texture;
	}

	// 가로로 길고 양 끝·위아래로 사라지는 빛줄기
	private static Texture2D CreateLineTexture()
	{
		Texture2D texture = new Texture2D(LINE_WIDTH, LINE_HEIGHT, TextureFormat.RGBA32, false);

		texture.name = "CombatTorchStreak";
		texture.wrapMode = TextureWrapMode.Clamp;

		float halfX = (LINE_WIDTH - 1) * 0.5f;
		float halfY = (LINE_HEIGHT - 1) * 0.5f;

		for (int y = 0; y < LINE_HEIGHT; y++)
		{
			for (int x = 0; x < LINE_WIDTH; x++)
			{
				float across = 1f - Mathf.Clamp01(Mathf.Abs(x - halfX) / halfX);
				float vertical = 1f - Mathf.Clamp01(Mathf.Abs(y - halfY) / halfY);

				texture.SetPixel(x, y, new Color(1f, 1f, 1f, across * across * vertical * vertical));
			}
		}

		texture.Apply();
		return texture;
	}
	#endregion
}
