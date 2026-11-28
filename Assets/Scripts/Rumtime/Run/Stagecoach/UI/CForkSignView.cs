using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 갈림길에서만 갈래마다 삼각형 표지판을 띄우고, 꾹 눌러 갈래를 고르게 한다
public class CForkSignView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Font _font;
	[SerializeField] private Sprite _unknownIcon;
	[SerializeField] private Sprite _triangleSprite;
	[SerializeField] private Color _triangleColor = new Color(0.32f, 0.3f, 0.28f, 1f);
	[SerializeField] private float _signDistance = 12f;
	[SerializeField] private float _signHeight = 3f;
	[SerializeField] private Vector2 _triangleSize = new Vector2(150f, 100f);
	[SerializeField] private float _iconSize = 54f;
	[SerializeField] private float _signWidth = 230f;
	[SerializeField] private float _screenMargin = 20f;
	[SerializeField] private Color _nameBackground = new Color(0.08f, 0.07f, 0.06f, 0.9f);
	[SerializeField] private float _holdDuration = 0.8f;
	[SerializeField] private float _holdDecay = 2f;
	[SerializeField] private Color _holdColor = new Color(1f, 0.78f, 0.3f, 1f);
	[SerializeField, Range(0f, 1f)] private float _unchosenAlpha = 0.35f;
	[SerializeField] private float _sparkSize = 34f;
	[SerializeField] private float _sparkFlicker = 18f;
	[SerializeField] private float _borderThickness = 3f;
	[SerializeField] private float _borderGlow = 9f;
	[SerializeField, Range(0f, 1f)] private float _borderGlowAlpha = 0.45f;
	[SerializeField] private float _borderInset = 3f;
	#endregion

	#region 이벤트
	// 표지판을 끝까지 눌러 갈래를 골랐을 때 (왼쪽부터 0)
	public event Action<int> OnChosen;
	#endregion

	#region 내부 변수
	private const int MAX_SIGNS = 3;
	private const float NAME_HEIGHT = 34f;
	private const float DESC_HEIGHT = 48f;
	private const int TRIANGLE_TEXTURE_SIZE = 128;

	private class CSign
	{
		public RectTransform Rect;
		public Image Icon;
		public Text Name;
		public Text Desc;
		public CForkSignBorder Border;
		public Image Spark;
		public CanvasGroup Group;
		public float Hold;
		public bool NeedRelease;
	}

	private static Sprite _sparkSprite;
	private static Sprite _fallbackTriangle;

	private readonly List<CSign> _signs = new List<CSign>();
	private RectTransform _canvasRect;
	private CRoadPath _path;
	private int _count;
	private bool _isChoosing;
	private bool _canHold;
	#endregion

	public void Show(CRoadPath path, IReadOnlyList<CNodeLink> links)
	{
		if (path == null || links == null || links.Count < 2)
		{
			Hide();
			return;
		}

		BuildIfNeeded();

		_path = path;
		_count = Mathf.Min(links.Count, MAX_SIGNS);
		_isChoosing = true;
		_canHold = false;

		for (int i = 0; i < _signs.Count; i++)
		{
			_signs[i].Rect.gameObject.SetActive(false);
			ResetHold(_signs[i]);

			if (i < _count)
			{
				Fill(_signs[i], links[i], i, _count);
			}
		}
	}

	public void SetHoldEnabled(bool isEnabled)
	{
		_canHold = isEnabled;
	}

	public void Hide()
	{
		_isChoosing = false;
		_count = 0;
		_path = null;

		for (int i = 0; i < _signs.Count; i++)
		{
			_signs[i].Rect.gameObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		if (_path == null || _count == 0)
		{
			return;
		}

		// 플레이 중 스크립트가 다시 컴파일되면 표지판 목록만 비고 개수는 남는다 → 개수를 맞춘다
		_count = Mathf.Min(_count, _signs.Count);

		Camera camera = CChaseCamera.CurrentCamera;

		for (int i = 0; i < _count; i++)
		{
			CSign sign = _signs[i];

			// 갈래가 아직 안 깔렸으면 위치를 모른다 → 깔릴 때까지 숨긴다
			if (camera == null || !_path.TryGetBranchPoint(i, _signDistance, out Vector3 world))
			{
				sign.Rect.gameObject.SetActive(false);
				continue;
			}

			Vector3 screen = camera.WorldToScreenPoint(world + Vector3.up * _signHeight);

			if (screen.z <= 0f)
			{
				sign.Rect.gameObject.SetActive(false);
				continue;
			}

			sign.Rect.gameObject.SetActive(true);
			sign.Rect.position = ClampToScreen(sign.Rect, screen);
		}

		TickHold();
	}

	private Vector3 ClampToScreen(RectTransform rect, Vector3 screen)
	{
		float scale = _canvasRect != null ? _canvasRect.lossyScale.x : 1f;
		Vector2 half = rect.sizeDelta * 0.5f * scale;
		float margin = _screenMargin * scale;

		screen.x = Mathf.Clamp(screen.x, half.x + margin, Screen.width - half.x - margin);
		screen.y = Mathf.Clamp(screen.y, half.y + margin, Screen.height - half.y - margin);
		screen.z = 0f;

		return screen;
	}

	private void Fill(CSign sign, CNodeLink link, int index, int count)
	{
		// 정찰 못 한 지점·경로는 표지판에도 ??? 로 → 지도와 같은 정보만 보여 준다
		CNodeTypeData type = link.To != null && link.To.IsRevealed ? link.To.TypeData : null;
		Sprite icon = type != null && type.Icon != null ? type.Icon : _unknownIcon;
		string routeName = link.IsRevealed ? (link.RouteData != null ? link.RouteData.DisplayName : "평범한 길") : "알 수 없는 길";

		sign.Icon.sprite = icon;
		sign.Icon.enabled = icon != null;
		sign.Name.text = type != null ? type.DisplayName : "미확인";
		sign.Desc.text = $"{routeName}\n<size=15>{GetKeyHint(index, count)}</size>";
	}

	#region 꾹 눌러 고르기
	// 표지판 위에서 마우스를 누르고 있거나 맞는 키(A·W·D)를 누르고 있으면 게이지가 찬다
	private void TickHold()
	{
		if (!_isChoosing)
		{
			return;
		}

		// 갈림길 앞에 서기 전에는 고를 수 없다 → 달려오며 누른 W 가 가운데 길을 골라 버리지 않게
		if (!_canHold)
		{
			for (int i = 0; i < _count; i++)
			{
				_signs[i].Hold = 0f;
				_signs[i].NeedRelease = true;
				SetHoldFill(_signs[i], 0f);
			}

			return;
		}

		int held = GetHeldSign();
		float duration = Mathf.Max(0.05f, _holdDuration);
		float dt = Time.unscaledDeltaTime;

		for (int i = 0; i < _count; i++)
		{
			CSign sign = _signs[i];

			if (i == held)
			{
				sign.Hold += dt;
			}
			else
			{
				sign.Hold = Mathf.Max(0f, sign.Hold - dt * _holdDecay);
			}

			SetHoldFill(sign, Mathf.Clamp01(sign.Hold / duration));

			if (sign.Hold >= duration)
			{
				Choose(i);
				return;
			}
		}
	}

	// 지금 누르고 있는 표지판 (없으면 -1). 고를 수 있게 된 뒤 새로 누른 것만 친다
	private int GetHeldSign()
	{
		int held = -1;

		for (int i = 0; i < _count; i++)
		{
			CSign sign = _signs[i];
			bool isHeld = sign.Rect.gameObject.activeSelf && IsSignHeld(i);

			// 손을 떼면 다시 누를 수 있다
			if (!isHeld)
			{
				sign.NeedRelease = false;
				continue;
			}

			if (held < 0 && !sign.NeedRelease)
			{
				held = i;
			}
		}

		return held;
	}

	// 표지판 위에서 마우스를 누르고 있거나, 맞는 키를 누르고 있는지
	private bool IsSignHeld(int index)
	{
		Mouse mouse = Mouse.current;

		// 화면 오버레이 캔버스라 카메라는 null
		if (mouse != null && mouse.leftButton.isPressed
			&& RectTransformUtility.RectangleContainsScreenPoint(_signs[index].Rect, mouse.position.ReadValue(), null))
		{
			return true;
		}

		Keyboard keyboard = Keyboard.current;

		return keyboard != null && IsKeyHeld(keyboard, index, _count);
	}

	// 왼쪽 표지판 A·←, 가운데 W·↑, 오른쪽 D·→ (GetKeyHint 와 같은 배치)
	private static bool IsKeyHeld(Keyboard keyboard, int index, int count)
	{
		if (index == 0)
		{
			return keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
		}

		if (index == count - 1)
		{
			return keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
		}

		return keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
	}

	private void Choose(int index)
	{
		_isChoosing = false;

		// 고른 표지판만 밝게 남기고 나머지는 흐리게
		for (int i = 0; i < _count; i++)
		{
			CSign sign = _signs[i];

			sign.Group.alpha = i == index ? 1f : _unchosenAlpha;
			SetHoldFill(sign, i == index ? 1f : 0f);
		}

		OnChosen?.Invoke(index);
	}

	// 테두리 게이지를 채우고, 게이지 맨 앞에 불꽃을 둔다 (비었거나 꽉 차면 불꽃은 숨김)
	private void SetHoldFill(CSign sign, float fill)
	{
		sign.Border.Fill = fill;

		bool showSpark = fill > 0.001f && fill < 0.999f;

		if (sign.Spark.gameObject.activeSelf != showSpark)
		{
			sign.Spark.gameObject.SetActive(showSpark);
		}

		if (!showSpark)
		{
			return;
		}

		// 게이지 끝 = 둘레 위 fill 지점
		sign.Spark.rectTransform.anchoredPosition = sign.Border.GetPoint(fill);

		// 일렁임 → 크기·밝기가 빠르게 흔들린다
		float time = Time.unscaledTime * _sparkFlicker;
		float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(time, sign.Rect.GetInstanceID() * 0.01f) + 0.1f * Mathf.Sin(time * 1.7f);
		Color color = sign.Spark.color;

		sign.Spark.rectTransform.localScale = Vector3.one * flicker;
		color.a = Mathf.Clamp01(flicker);
		sign.Spark.color = color;
	}

	private void ResetHold(CSign sign)
	{
		sign.Hold = 0f;
		sign.Group.alpha = 1f;
		SetHoldFill(sign, 0f);
	}
	#endregion

	#region 안내 글자
	private static string GetKeyHint(int index, int count)
	{
		if (index == 0)
		{
			return "◀ A 꾹 누르기";
		}

		if (index == count - 1)
		{
			return "D ▶ 꾹 누르기";
		}

		return "▲ W 꾹 누르기";
	}
	#endregion

	#region UI 만들기
	private void BuildIfNeeded()
	{
		if (_canvasRect != null)
		{
			return;
		}

		if (_font == null)
		{
			_font = CUIBuilder.GetDefaultFont();
		}

		_canvasRect = CUIBuilder.CreateOverlayCanvas("ForkSignCanvas", transform, 5, false);

		for (int i = 0; i < MAX_SIGNS; i++)
		{
			_signs.Add(CreateSign(i));
		}
	}

	// 위에서부터 삼각형(안에 아이콘, 테두리 게이지) → 이름 띠 → 설명 두 줄
	private CSign CreateSign(int index)
	{
		float height = _triangleSize.y + NAME_HEIGHT + DESC_HEIGHT;
		GameObject root = new GameObject($"ForkSign_{index}", typeof(RectTransform), typeof(CanvasGroup));
		RectTransform rect = root.GetComponent<RectTransform>();

		rect.SetParent(_canvasRect, false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.zero;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = new Vector2(_signWidth, height);

		CanvasGroup group = root.GetComponent<CanvasGroup>();

		group.blocksRaycasts = false;

		Color triangleColor = _triangleSprite != null ? _triangleColor : Color.white;
		Image triangle = CUIBuilder.CreateImage("Triangle", rect, GetTriangleSprite(), triangleColor);
		RectTransform triangleRect = triangle.rectTransform;

		SetTop(triangleRect, 0f, _triangleSize);

		Image icon = CUIBuilder.CreateImage("Icon", triangleRect, null, Color.white);
		RectTransform iconRect = icon.rectTransform;

		iconRect.anchorMin = new Vector2(0.5f, 0f);
		iconRect.anchorMax = new Vector2(0.5f, 0f);
		iconRect.pivot = new Vector2(0.5f, 0f);
		iconRect.anchoredPosition = new Vector2(0f, _triangleSize.y * 0.1f);
		iconRect.sizeDelta = new Vector2(_iconSize, _iconSize);
		icon.preserveAspect = true;

		// 이름 띠
		Image nameBack = CUIBuilder.CreateImage("NameBack", rect, null, _nameBackground);

		SetTop(nameBack.rectTransform, _triangleSize.y, new Vector2(_triangleSize.x + 20f, NAME_HEIGHT));

		Text name = CreateText("Name", nameBack.rectTransform, 22, FontStyle.Bold);

		Stretch(name.rectTransform);

		// 꾹 누르기 게이지 → 위 꼭짓점에서 오른쪽 빗변 → 밑변 → 왼쪽 빗변을 따라 꼭짓점으로 돌아오며 찬다
		GameObject borderGo = new GameObject("HoldBorder", typeof(RectTransform), typeof(CForkSignBorder));
		RectTransform borderRect = borderGo.GetComponent<RectTransform>();
		CForkSignBorder border = borderGo.GetComponent<CForkSignBorder>();

		borderRect.SetParent(triangleRect, false);
		Stretch(borderRect);
		border.Color = _holdColor;
		border.Setup(_borderThickness, _borderGlow, _borderGlowAlpha, _borderInset);
		border.Fill = 0f;

		// 게이지 끝 불꽃 → 테두리 칸 가운데 기준 좌표로 옮긴다 (변 막대보다 위에 그려지게 나중에 만든다)
		Image spark = CUIBuilder.CreateImage("Spark", borderRect, GetSparkSprite(), Color.Lerp(_holdColor, Color.white, 0.55f));
		RectTransform sparkRect = spark.rectTransform;

		sparkRect.anchorMin = new Vector2(0.5f, 0.5f);
		sparkRect.anchorMax = new Vector2(0.5f, 0.5f);
		sparkRect.pivot = new Vector2(0.5f, 0.5f);
		sparkRect.sizeDelta = new Vector2(_sparkSize, _sparkSize);
		spark.gameObject.SetActive(false);

		// 경로 이름 + 키 안내
		Text desc = CreateText("Desc", rect, 17, FontStyle.Normal);

		SetTop(desc.rectTransform, _triangleSize.y + NAME_HEIGHT + 4f, new Vector2(_signWidth, DESC_HEIGHT));
		desc.alignment = TextAnchor.UpperCenter;
		desc.color = new Color(0.85f, 0.82f, 0.75f, 1f);

		Shadow shadow = desc.gameObject.AddComponent<Shadow>();

		shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
		shadow.effectDistance = new Vector2(1.5f, -1.5f);

		root.SetActive(false);

		return new CSign { Rect = rect, Icon = icon, Name = name, Desc = desc, Border = border, Spark = spark, Group = group };
	}

	private Text CreateText(string name, RectTransform parent, int size, FontStyle style)
	{
		GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
		Text text = go.GetComponent<Text>();

		text.rectTransform.SetParent(parent, false);
		text.font = _font;
		text.fontSize = size;
		text.fontStyle = style;
		text.supportRichText = true;
		text.alignment = TextAnchor.MiddleCenter;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		text.color = Color.white;
		text.raycastTarget = false;

		return text;
	}

	// 부모 위쪽 가운데에서 top 만큼 내려온 곳에 size 크기로 둔다
	private static void SetTop(RectTransform rect, float top, Vector2 size)
	{
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, -top);
		rect.sizeDelta = size;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	private Sprite GetTriangleSprite()
	{
		return _triangleSprite != null ? _triangleSprite : GetFallbackTriangle();
	}

	private static Sprite GetFallbackTriangle()
	{
		if (_fallbackTriangle != null)
		{
			return _fallbackTriangle;
		}

		int size = TRIANGLE_TEXTURE_SIZE;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		Color fill = new Color(0.05f, 0.05f, 0.05f, 0.92f);
		Color border = new Color(0.75f, 0.72f, 0.65f, 1f);
		float borderWidth = size * 0.035f;

		texture.wrapMode = TextureWrapMode.Clamp;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				// 밑변은 y = 0, 꼭짓점은 (size/2, size)
				float half = (size - y) * 0.5f;
				float fromCenter = Mathf.Abs(x + 0.5f - size * 0.5f);
				float edge = half - fromCenter;

				if (edge < 0f)
				{
					texture.SetPixel(x, y, Color.clear);
				}
				else
				{
					texture.SetPixel(x, y, edge < borderWidth || y < borderWidth ? border : fill);
				}
			}
		}

		texture.Apply();
		_fallbackTriangle = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));

		return _fallbackTriangle;
	}

	// 불꽃 → 가운데가 하얗게 밝고 바깥으로 부드럽게 사라지는 원 (색은 Image.color 로 입힌다)
	private static Sprite GetSparkSprite()
	{
		if (_sparkSprite != null)
		{
			return _sparkSprite;
		}

		int size = 64;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

		texture.wrapMode = TextureWrapMode.Clamp;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float t = Mathf.Clamp01(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size * 0.5f));
				float core = Mathf.Clamp01(1f - t * 4f);
				float glow = Mathf.Pow(1f - t, 3f);

				texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow * 0.8f)));
			}
		}

		texture.Apply();
		_sparkSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));

		return _sparkSprite;
	}
	#endregion
}
