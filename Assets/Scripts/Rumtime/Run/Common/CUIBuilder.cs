using UnityEngine;
using UnityEngine.UI;

// 코드로 UI 를 만드는 화면(갈림길 표지판, 이벤트 띠, 고물상)이 함께 쓰는 생성 함수 모음
//  ㄴ 같은 캔버스·이미지 생성 코드가 화면마다 복사돼 있던 것을 한곳에 모았다
public static class CUIBuilder
{
	private const string DEFAULT_FONT = "LegacyRuntime.ttf";
	private static readonly Vector2 REFERENCE_RESOLUTION = new Vector2(1920f, 1080f);

	/// <summary>
	/// 유니티 기본 폰트 / 경태
	/// </summary>
	public static Font GetDefaultFont()
	{
		return Resources.GetBuiltinResource<Font>(DEFAULT_FONT);
	}

	/// <summary>
	/// 1920x1080 기준으로 늘어나는 화면 덮개 캔버스를 parent 아래 만든다 / 경태
	/// </summary>
	public static RectTransform CreateOverlayCanvas(string name, Transform parent, int sortingOrder, bool receiveClicks)
	{
		GameObject canvasGo = receiveClicks
			? new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
			: new GameObject(name, typeof(Canvas), typeof(CanvasScaler));

		canvasGo.transform.SetParent(parent, false);

		Canvas canvas = canvasGo.GetComponent<Canvas>();

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = sortingOrder;

		CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();

		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = REFERENCE_RESOLUTION;
		scaler.matchWidthOrHeight = 0.5f;

		return canvasGo.GetComponent<RectTransform>();
	}

	/// <summary>
	/// 빈 RectTransform 오브젝트를 parent 아래 만든다 / 경태
	/// </summary>
	public static RectTransform CreateRect(string name, RectTransform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		RectTransform rect = (RectTransform)go.transform;

		rect.SetParent(parent, false);

		return rect;
	}

	/// <summary>
	/// 클릭을 받지 않는 Image 를 parent 아래 만든다 / 경태
	/// </summary>
	public static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
	{
		GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
		Image image = go.GetComponent<Image>();

		image.rectTransform.SetParent(parent, false);
		image.sprite = sprite;
		image.color = color;
		image.raycastTarget = false;

		return image;
	}
}
