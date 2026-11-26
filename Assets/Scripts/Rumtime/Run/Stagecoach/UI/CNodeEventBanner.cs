using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 노드에 도착하면 화면 아래에 이벤트 이름 띠를 띄우고, 클릭(또는 Enter)하면 그 이벤트를 시작한다
public class CNodeEventBanner : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Font _font;
	[SerializeField] private Vector2 _size = new Vector2(620f, 74f);
	[SerializeField] private float _bottomOffset = 140f;
	[SerializeField] private float _fadeTime = 0.3f;
	[SerializeField] private Color _textColor = new Color(0.86f, 0.8f, 0.68f, 1f);
	[SerializeField] private Color _hoverTextColor = new Color(1f, 0.9f, 0.62f, 1f);
	[SerializeField] private Color _lineColor = new Color(0.78f, 0.64f, 0.38f, 1f);
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private const float DIAMOND_SIZE = 52f;
	private const int GRADIENT_WIDTH = 128;

	private static Sprite _gradientSprite;
	private static Sprite _whiteSprite;

	private RectTransform _canvasRect;
	private RectTransform _root;
	private CanvasGroup _group;
	private Image _icon;
	private Text _label;
	private Action _onConfirm;
	private float _alpha;
	private bool _isShown;
	#endregion

	// 띠가 떠서 클릭을 기다리는 중인지
	public bool IsWaiting => _onConfirm != null;

	public void Show(string title, Sprite icon, Action onConfirm)
	{
		BuildIfNeeded();

		_onConfirm = onConfirm;
		_isShown = true;
		_label.text = title;
		_label.color = _textColor;
		_icon.sprite = icon;
		_icon.enabled = icon != null;
		_root.gameObject.SetActive(true);
		_group.blocksRaycasts = true;

		if (_logState)
		{
			CPrint.Log($"CNodeEventBanner : '{title}' 띠 표시 → 누르면 이벤트 시작");
		}
	}

	public void Hide()
	{
		_onConfirm = null;
		_isShown = false;

		if (_group != null)
		{
			_group.blocksRaycasts = false;
		}
	}

	private void Update()
	{
		if (_root == null)
		{
			return;
		}

		// 서서히 나타나고 사라진다 (이벤트 창이 시간을 멈춰도 흐르게 실제 시간)
		float target = _isShown ? 1f : 0f;
		float speed = _fadeTime > 0f ? Time.unscaledDeltaTime / _fadeTime : 1f;

		_alpha = Mathf.MoveTowards(_alpha, target, speed);
		_group.alpha = _alpha;

		if (!_isShown && _alpha <= 0f && _root.gameObject.activeSelf)
		{
			_root.gameObject.SetActive(false);
		}

		// 키보드로도 들어갈 수 있게
		Keyboard keyboard = Keyboard.current;

		if (IsWaiting && keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
		{
			Confirm();
		}
	}

	public void HandleClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			Confirm();
		}
	}

	public void HandleEnter()
	{
		if (IsWaiting)
		{
			_label.color = _hoverTextColor;
		}
	}

	public void HandleExit()
	{
		_label.color = _textColor;
	}

	// 콜백을 먼저 비우고 부른다 → 두 번 눌러도 한 번만
	private void Confirm()
	{
		if (!IsWaiting)
		{
			return;
		}

		Action onConfirm = _onConfirm;

		Hide();

		if (_logState)
		{
			CPrint.Log($"CNodeEventBanner : '{_label.text}' 선택 → 이벤트 시작");
		}

		onConfirm();
	}

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

		_canvasRect = CUIBuilder.CreateOverlayCanvas("EventBannerCanvas", transform, 6, true);

		// 띠 (가운데 어둡고 양끝으로 사라지는 바탕) → 클릭은 이 바탕이 받는다
		GameObject rootGo = new GameObject("EventBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(CNodeEventBannerClick));

		_root = rootGo.GetComponent<RectTransform>();
		_root.SetParent(_canvasRect, false);
		_root.anchorMin = new Vector2(0.5f, 0f);
		_root.anchorMax = new Vector2(0.5f, 0f);
		_root.pivot = new Vector2(0.5f, 0f);
		_root.anchoredPosition = new Vector2(0f, _bottomOffset);
		_root.sizeDelta = _size;

		Image background = rootGo.GetComponent<Image>();

		background.sprite = GetGradientSprite();
		background.color = new Color(0.04f, 0.035f, 0.03f, 0.88f);
		background.raycastTarget = true;

		rootGo.GetComponent<CNodeEventBannerClick>().Owner = this;

		_group = rootGo.GetComponent<CanvasGroup>();
		_group.alpha = 0f;

		CreateLine("TopLine", 1f);
		CreateLine("BottomLine", 0f);

		float diamondX = -_size.x * 0.5f + 70f;
		Image frame = CUIBuilder.CreateImage("DiamondFrame", _root, GetWhiteSprite(), _lineColor);

		frame.rectTransform.anchoredPosition = new Vector2(diamondX, 0f);
		frame.rectTransform.sizeDelta = new Vector2(DIAMOND_SIZE, DIAMOND_SIZE);
		frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

		Image inner = CUIBuilder.CreateImage("DiamondInner", frame.rectTransform, GetWhiteSprite(), new Color(0.07f, 0.06f, 0.05f, 1f));

		inner.rectTransform.sizeDelta = new Vector2(DIAMOND_SIZE - 5f, DIAMOND_SIZE - 5f);

		_icon = CUIBuilder.CreateImage("Icon", _root, null, Color.white);
		_icon.rectTransform.anchoredPosition = new Vector2(diamondX, 0f);
		_icon.rectTransform.sizeDelta = new Vector2(DIAMOND_SIZE * 0.9f, DIAMOND_SIZE * 0.9f);
		_icon.preserveAspect = true;

		GameObject labelGo = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(Shadow));

		_label = labelGo.GetComponent<Text>();
		_label.rectTransform.SetParent(_root, false);
		_label.rectTransform.anchorMin = Vector2.zero;
		_label.rectTransform.anchorMax = Vector2.one;
		_label.rectTransform.offsetMin = new Vector2(110f, 0f);
		_label.rectTransform.offsetMax = new Vector2(-40f, 0f);
		_label.font = _font;
		_label.fontSize = 36;
		_label.fontStyle = FontStyle.Bold;
		_label.alignment = TextAnchor.MiddleCenter;
		_label.horizontalOverflow = HorizontalWrapMode.Overflow;
		_label.verticalOverflow = VerticalWrapMode.Overflow;
		_label.color = _textColor;
		_label.raycastTarget = false;

		Shadow shadow = labelGo.GetComponent<Shadow>();

		shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
		shadow.effectDistance = new Vector2(2f, -2f);

		_root.gameObject.SetActive(false);
	}

	private void CreateLine(string name, float anchorY)
	{
		Image line = CUIBuilder.CreateImage(name, _root, GetGradientSprite(), _lineColor);
		RectTransform rect = line.rectTransform;

		rect.anchorMin = new Vector2(0f, anchorY);
		rect.anchorMax = new Vector2(1f, anchorY);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = new Vector2(0f, 2f);
	}

	// 가로 단면 → 가운데 꽉 차고 양끝으로 갈수록 투명
	private static Sprite GetGradientSprite()
	{
		if (_gradientSprite != null)
		{
			return _gradientSprite;
		}

		Texture2D texture = new Texture2D(GRADIENT_WIDTH, 1, TextureFormat.RGBA32, false);

		texture.wrapMode = TextureWrapMode.Clamp;

		for (int x = 0; x < GRADIENT_WIDTH; x++)
		{
			float t = Mathf.Abs((x + 0.5f) / GRADIENT_WIDTH - 0.5f) * 2f;
			float alpha = Mathf.Clamp01((1f - t) / 0.35f);

			texture.SetPixel(x, 0, new Color(1f, 1f, 1f, alpha));
		}

		texture.Apply();
		_gradientSprite = Sprite.Create(texture, new Rect(0f, 0f, GRADIENT_WIDTH, 1f), new Vector2(0.5f, 0.5f));

		return _gradientSprite;
	}

	private static Sprite GetWhiteSprite()
	{
		if (_whiteSprite == null)
		{
			Texture2D texture = Texture2D.whiteTexture;

			_whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
		}

		return _whiteSprite;
	}
	#endregion
}

