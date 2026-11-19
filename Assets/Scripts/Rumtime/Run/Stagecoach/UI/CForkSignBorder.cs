using UnityEngine;
using UnityEngine.UI;

public class CForkSignBorder : MonoBehaviour
{
	#region 내부 변수
	private const int EDGE_COUNT = 3;
	private const int PROFILE_HEIGHT = 32;

	private readonly Image[] _edges = new Image[EDGE_COUNT];
	private readonly Vector2[] _points = new Vector2[EDGE_COUNT + 1];
	private readonly float[] _lengths = new float[EDGE_COUNT];
	private RectTransform _rect;
	private Sprite _edgeSprite;
	private Texture2D _edgeTexture;
	private float _thickness = 3f;
	private float _glowWidth = 9f;
	private float _glowAlpha = 0.45f;
	private float _inset = 3f;
	private float _total;
	private float _fill;
	private Color _color = Color.white;
	private Vector2 _builtSize = new Vector2(-1f, -1f);
	#endregion

	// 둘레 중 얼마나 찼는지 (0 ~ 1)
	public float Fill
	{
		get => _fill;
		set
		{
			_fill = Mathf.Clamp01(value);
			ApplyFill();
		}
	}

	// 선 색
	public Color Color
	{
		get => _color;
		set
		{
			_color = value;

			for (int i = 0; i < EDGE_COUNT; i++)
			{
				if (_edges[i] != null)
				{
					_edges[i].color = _color;
				}
			}
		}
	}

	public void Setup(float thickness, float glowWidth, float glowAlpha, float inset)
	{
		_rect = (RectTransform)transform;
		_thickness = Mathf.Max(0.5f, thickness);
		_glowWidth = Mathf.Max(0f, glowWidth);
		_glowAlpha = Mathf.Clamp01(glowAlpha);
		_inset = inset;

		BuildEdgeSprite();

		for (int i = 0; i < EDGE_COUNT; i++)
		{
			if (_edges[i] == null)
			{
				_edges[i] = CreateEdge(i);
			}

			_edges[i].sprite = _edgeSprite;
		}

		_builtSize = new Vector2(-1f, -1f);
		LayoutEdges();
		ApplyFill();
	}

	public Vector2 GetPoint(float fill)
	{
		LayoutEdges();

		float remain = Mathf.Clamp01(fill) * _total;

		for (int i = 0; i < EDGE_COUNT; i++)
		{
			if (remain <= _lengths[i])
			{
				return Vector2.Lerp(_points[i], _points[i + 1], _lengths[i] > 0f ? remain / _lengths[i] : 0f);
			}

			remain -= _lengths[i];
		}

		return _points[EDGE_COUNT];
	}

	private void OnRectTransformDimensionsChange()
	{
		LayoutEdges();
	}

	private void OnDestroy()
	{
		if (_edgeSprite != null)
		{
			Destroy(_edgeSprite);
		}

		if (_edgeTexture != null)
		{
			Destroy(_edgeTexture);
		}
	}

	// 앞 변부터 차례로 채운다
	private void ApplyFill()
	{
		LayoutEdges();

		float remain = _fill * _total;

		for (int i = 0; i < EDGE_COUNT; i++)
		{
			if (_edges[i] == null)
			{
				continue;
			}

			float amount = _lengths[i] > 0f ? Mathf.Clamp01(remain / _lengths[i]) : 0f;

			_edges[i].fillAmount = amount;
			_edges[i].enabled = amount > 0f;
			remain -= _lengths[i];
		}
	}

	// 위 꼭짓점 → 오른쪽 아래 → 왼쪽 아래 → 위 꼭짓점 (칸 크기가 바뀔 때만 다시 계산)
	private void LayoutEdges()
	{
		if (_rect == null)
		{
			_rect = (RectTransform)transform;
		}

		Rect rect = _rect.rect;

		if (rect.size == _builtSize)
		{
			return;
		}

		_builtSize = rect.size;

		// 빗변이 기울어 있어 옆·위는 안쪽 거리를 조금 더 준다
		float bottom = rect.yMin + _inset;

		_points[0] = new Vector2(rect.center.x, rect.yMax - _inset * 2f);
		_points[1] = new Vector2(rect.xMax - _inset * 2f, bottom);
		_points[2] = new Vector2(rect.xMin + _inset * 2f, bottom);
		_points[3] = _points[0];

		_total = 0f;

		for (int i = 0; i < EDGE_COUNT; i++)
		{
			Vector2 from = _points[i];
			Vector2 to = _points[i + 1];
			Vector2 dir = (to - from).normalized;

			_lengths[i] = Vector2.Distance(from, to);
			_total += _lengths[i];

			if (_edges[i] == null)
			{
				continue;
			}

			// 모서리에서 끊겨 보이지 않게 앞뒤로 굵기 절반씩 늘린다
			RectTransform edge = _edges[i].rectTransform;
			float height = _thickness + _glowWidth * 2f;

			edge.anchoredPosition = from - dir * (_thickness * 0.5f);
			edge.sizeDelta = new Vector2(_lengths[i] + _thickness, height);
			edge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
		}
	}

	private Image CreateEdge(int index)
	{
		GameObject go = new GameObject($"Edge_{index}", typeof(RectTransform), typeof(Image));
		Image image = go.GetComponent<Image>();
		RectTransform rect = image.rectTransform;

		rect.SetParent(transform, false);
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0f, 0.5f);

		image.type = Image.Type.Filled;
		image.fillMethod = Image.FillMethod.Horizontal;
		image.fillOrigin = (int)Image.OriginHorizontal.Left;
		image.fillAmount = 0f;
		image.color = _color;
		image.raycastTarget = false;

		return image;
	}

	// 세로 단면 → 가운데 굵기만큼 꽉 차고, 위아래로 갈수록 투명해지는 빛 번짐
	private void BuildEdgeSprite()
	{
		if (_edgeSprite != null)
		{
			Destroy(_edgeSprite);
		}

		if (_edgeTexture != null)
		{
			Destroy(_edgeTexture);
		}

		float height = _thickness + _glowWidth * 2f;
		float coreHalf = _thickness * 0.5f / height;

		_edgeTexture = new Texture2D(4, PROFILE_HEIGHT, TextureFormat.RGBA32, false);
		_edgeTexture.wrapMode = TextureWrapMode.Clamp;

		for (int y = 0; y < PROFILE_HEIGHT; y++)
		{
			// 가운데에서 떨어진 정도 (0 = 가운데, 0.5 = 끝)
			float t = Mathf.Abs((y + 0.5f) / PROFILE_HEIGHT - 0.5f);
			float alpha;

			if (t <= coreHalf)
			{
				alpha = 1f;
			}
			else
			{
				float falloff = Mathf.Clamp01(1f - (t - coreHalf) / Mathf.Max(0.0001f, 0.5f - coreHalf));

				alpha = falloff * falloff * _glowAlpha;
			}

			for (int x = 0; x < 4; x++)
			{
				_edgeTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
			}
		}

		_edgeTexture.Apply();
		_edgeSprite = Sprite.Create(_edgeTexture, new Rect(0f, 0f, 4f, PROFILE_HEIGHT), new Vector2(0.5f, 0.5f));
	}
}
