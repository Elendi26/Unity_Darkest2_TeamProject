using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CForkSignView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Font _font;
	[SerializeField] private Sprite _unknownIcon;
	[SerializeField] private float _signDistance = 12f;
	[SerializeField] private float _signHeight = 3f;
	[SerializeField] private Vector2 _signSize = new Vector2(220f, 92f);
	[SerializeField] private float _screenMargin = 20f;
	[SerializeField] private Color _background = new Color(0f, 0f, 0f, 0.6f);
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private const int MAX_SIGNS = 3;

	private class CSign
	{
		public RectTransform Rect;
		public Image Icon;
		public Text Label;
	}

	private readonly List<CSign> _signs = new List<CSign>();
	private RectTransform _canvasRect;
	private CRoadPath _path;
	private int _count;
	#endregion

	public void Show(CRoadPath path, IReadOnlyList<CNodeLink> links)
	{
		if (path == null || links == null)
		{
			return;
		}

		BuildIfNeeded();

		_path = path;
		_count = Mathf.Min(links.Count, MAX_SIGNS);

		for (int i = 0; i < _signs.Count; i++)
		{
			_signs[i].Rect.gameObject.SetActive(false);

			if (i < _count)
			{
				Fill(_signs[i], links[i], i, _count);
			}
		}

		if (_logState)
		{
			CPrint.Log($"CForkSignView : 갈림길 표지판 {_count}개");
		}
	}

	public void Hide()
	{
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

		Camera camera = Camera.main;

		for (int i = 0; i < _count; i++)
		{
			CSign sign = _signs[i];

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
			sign.Rect.position = ClampToScreen(screen);
		}
	}

	private Vector3 ClampToScreen(Vector3 screen)
	{
		float scale = _canvasRect != null ? _canvasRect.lossyScale.x : 1f;
		Vector2 half = _signSize * 0.5f * scale;
		float margin = _screenMargin * scale;

		screen.x = Mathf.Clamp(screen.x, half.x + margin, Screen.width - half.x - margin);
		screen.y = Mathf.Clamp(screen.y, half.y + margin, Screen.height - half.y - margin);
		screen.z = 0f;

		return screen;
	}

	private void Fill(CSign sign, CNodeLink link, int index, int count)
	{
		CNodeTypeData type = link.To != null ? link.To.TypeData : null;
		Sprite icon = type != null && type.Icon != null ? type.Icon : _unknownIcon;

		sign.Icon.sprite = icon;
		sign.Icon.enabled = icon != null;
		sign.Label.text = $"{GetNodeName(type)}\n<size=18>{GetRouteName(link.RouteData)}</size>\n<size=16>{GetKeyHint(index, count)}</size>";
	}

	private static string GetNodeName(CNodeTypeData type)
	{
		if (type == null)
		{
			return "???";
		}

		switch (type.Type)
		{
			case ENodeType.Start:
				return "출발";
			case ENodeType.Combat:
				return "전투";
			case ENodeType.Store:
				return "상점";
			case ENodeType.Hospital:
				return "병원";
			case ENodeType.Cache:
				return "보급품";
			case ENodeType.CreatureDen:
				return "괴물 소굴";
			case ENodeType.Inn:
				return "여관";
			case ENodeType.Fortress:
				return "망각의 성채";
			default:
				return type.Type.ToString();
		}
	}

	private static string GetRouteName(CRouteTypeData route)
	{
		if (route == null)
		{
			return "평범한 길";
		}

		switch (route.Type)
		{
			case ERouteType.Safe:
				return "안전한 길";
			case ERouteType.Combat:
				return "위험한 길";
			case ERouteType.RoughPatch:
				return "험한 길";
			default:
				return "평범한 길";
		}
	}

	private static string GetKeyHint(int index, int count)
	{
		if (index == 0)
		{
			return "◀ A";
		}

		if (index == count - 1)
		{
			return "D ▶";
		}

		return "▲ 직진";
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
			_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		}

		GameObject canvasGo = new GameObject("ForkSignCanvas", typeof(Canvas), typeof(CanvasScaler));

		canvasGo.transform.SetParent(transform, false);

		Canvas canvas = canvasGo.GetComponent<Canvas>();

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 5;

		CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();

		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		_canvasRect = canvasGo.GetComponent<RectTransform>();

		for (int i = 0; i < MAX_SIGNS; i++)
		{
			_signs.Add(CreateSign(i));
		}
	}

	private CSign CreateSign(int index)
	{
		GameObject root = new GameObject($"ForkSign_{index}", typeof(RectTransform), typeof(Image));
		RectTransform rect = root.GetComponent<RectTransform>();

		rect.SetParent(_canvasRect, false);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.zero;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = _signSize;

		Image background = root.GetComponent<Image>();

		background.color = _background;
		background.raycastTarget = false;

		// 왼쪽 아이콘
		GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
		RectTransform iconRect = iconGo.GetComponent<RectTransform>();
		float iconSize = _signSize.y - 16f;

		iconRect.SetParent(rect, false);
		iconRect.anchorMin = new Vector2(0f, 0.5f);
		iconRect.anchorMax = new Vector2(0f, 0.5f);
		iconRect.pivot = new Vector2(0f, 0.5f);
		iconRect.anchoredPosition = new Vector2(8f, 0f);
		iconRect.sizeDelta = new Vector2(iconSize, iconSize);

		Image icon = iconGo.GetComponent<Image>();

		icon.preserveAspect = true;
		icon.raycastTarget = false;

		// 오른쪽 글자
		GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
		RectTransform labelRect = labelGo.GetComponent<RectTransform>();

		labelRect.SetParent(rect, false);
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.offsetMin = new Vector2(iconSize + 16f, 4f);
		labelRect.offsetMax = new Vector2(-8f, -4f);

		Text label = labelGo.GetComponent<Text>();

		label.font = _font;
		label.fontSize = 24;
		label.supportRichText = true;
		label.alignment = TextAnchor.MiddleLeft;
		label.color = Color.white;
		label.raycastTarget = false;

		root.SetActive(false);

		return new CSign { Rect = rect, Icon = icon, Label = label };
	}
	#endregion
}
