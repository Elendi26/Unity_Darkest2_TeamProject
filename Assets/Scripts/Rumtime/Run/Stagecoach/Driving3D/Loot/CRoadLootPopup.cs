using UnityEngine;

// 보상을 얻은 자리에서 떠오르며 사라지는 글자
public class CRoadLootPopup : MonoBehaviour
{
	#region 내부 변수
	private const float LIFE_TIME = 1.4f;
	private const float RISE_SPEED = 1.6f;
	private TextMesh _text;
	private Color _color;
	private float _elapsed;
	#endregion

	public static void Show(Vector3 position, string message, Color color, Font font)
	{
		GameObject go = new GameObject("LootPopup");
		TextMesh text = go.AddComponent<TextMesh>();

		go.transform.position = position;
		text.text = message;
		text.color = color;
		text.fontSize = 64;
		text.characterSize = 0.06f;
		text.anchor = TextAnchor.MiddleCenter;
		text.alignment = TextAlignment.Center;

		if (font != null)
		{
			text.font = font;
			go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
		}

		CRoadLootPopup popup = go.AddComponent<CRoadLootPopup>();

		popup._text = text;
		popup._color = color;
	}

	private void LateUpdate()
	{
		_elapsed += Time.deltaTime;

		if (_elapsed >= LIFE_TIME)
		{
			Destroy(gameObject);
			return;
		}

		transform.position += Vector3.up * RISE_SPEED * Time.deltaTime;

		// 카메라를 바라보게 → 글자가 뒤집혀 보이지 않게
		Camera cam = CChaseCamera.CurrentCamera;

		if (cam != null)
		{
			transform.rotation = cam.transform.rotation;
		}

		// 뒤 절반 동안 흐려진다
		float alpha = Mathf.Clamp01((LIFE_TIME - _elapsed) / (LIFE_TIME * 0.5f));

		_text.color = new Color(_color.r, _color.g, _color.b, _color.a * alpha);
	}
}
