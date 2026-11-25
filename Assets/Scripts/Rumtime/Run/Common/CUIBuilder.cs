using UnityEngine;
using UnityEngine.UI;

// 코드로 UI 를 만드는 화면(갈림길 표지판, 이벤트 띠, 고물상)이 함께 쓰는 생성 함수 모음
public static class CUIBuilder
{
	private const string DEFAULT_FONT = "LegacyRuntime.ttf";
	private static readonly Vector2 REFERENCE_RESOLUTION = new Vector2(1920f, 1080f);

	public static Font GetDefaultFont()
	{
		return Resources.GetBuiltinResource<Font>(DEFAULT_FONT);
	}

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

	public static RectTransform CreateRect(string name, RectTransform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		RectTransform rect = (RectTransform)go.transform;

		rect.SetParent(parent, false);

		return rect;
	}

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
