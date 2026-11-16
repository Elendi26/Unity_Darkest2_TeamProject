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
	private float _aheadArc = -1f;
	#endregion

	public void Show(CRoadPath path, IReadOnlyList<CNodeLink> links)
	{
		if (path == null || links == null)
		{
			return;
		}

		BuildIfNeeded();

		_path = path;
		_aheadArc = -1f;
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

	// 갈래가 하나뿐인 길 → 갈림길 없이 길 위 arc 지점에 표지판 하나를 세운다
	public void ShowAhead(CRoadPath path, CNodeLink link, float arc)
	{
		if (path == null || link == null)
		{
			return;
		}

		BuildIfNeeded();

		_path = path;
		_aheadArc = arc;
		_count = 1;

		for (int i = 0; i < _signs.Count; i++)
		{
			_signs[i].Rect.gameObject.SetActive(false);
		}

		Fill(_signs[0], link, 0, 1);

		if (_logState)
		{
			CPrint.Log($"CForkSignView : 외길 표지판 ({arc:F0}m)");
		}
	}

	public void Hide()
	{
		_count = 0;
		_path = null;
		_aheadArc = -1f;

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

			if (camera == null || !TryGetSignPoint(i, out Vector3 world))
			{
				sign.Rect.gameObject.SetActive(false);
				continue;
			}

			Vector3 screen = camera.WorldToScreenPoint(world + Vector3.up * _signHeight);

			// 외길 표지판은 지나치면 끝
			if (screen.z <= 0f && _aheadArc >= 0f)
			{
				Hide();
				return;
			}

			if (screen.z <= 0f)
			{
				sign.Rect.gameObject.SetActive(false);
				continue;
			}

			sign.Rect.gameObject.SetActive(true);
			sign.Rect.position = ClampToScreen(screen);
		}
	}

	private bool TryGetSignPoint(int index, out Vector3 world)
	{
		if (_aheadArc >= 0f)
		{
			return _path.TryGetPoint(_aheadArc, out world, out _);
		}

		return _path.TryGetBranchPoint(index, _signDistance, out world);
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

	// 표지판·디버그 기즈모가 같은 노드 이름을 쓴다
	public static string GetNodeName(CNodeTypeData type)
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
			case ENodeType.Crossroad:
				return "교차로";
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
			case ERouteType.DangerPath:
				return "위태로운 길";
			default:
				return "평범한 길";
		}
	}

	private static string GetKeyHint(int index, int count)
	{
		if (count == 1)
		{
			return "▲ 직진";
		}

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
