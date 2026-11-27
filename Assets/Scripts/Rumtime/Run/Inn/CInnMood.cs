using UnityEngine;
using UnityEngine.UI;

// 여관 분위기
public class CInnMood : MonoBehaviour
{
	#region 상수
	private const int VIGNETTE_SIZE = 256;
	#endregion

	#region 인스펙터
	// 여관 씬 조명 세기에 곱하는 값 (1 이면 그대로)
	[SerializeField, Range(0f, 1f)] private float _lightScale = 0.6f;
	// 주변광에 곱하는 값 (여관이 열려 있는 동안만, 닫히면 되돌린다)
	[SerializeField, Range(0f, 1f)] private float _ambientScale = 0.5f;
	// 화면 전체에 덮는 어두운 막
	[SerializeField] private Color _shadeColor = new Color(0.02f, 0.01f, 0.01f, 0.25f);
	// 가장자리 어둡게 → 색의 알파가 가장 바깥 진하기
	[SerializeField] private Color _vignetteColor = new Color(0f, 0f, 0f, 0.8f);
	// 비네트가 시작하는 거리 (0 = 화면 중앙, 1 = 가장자리)
	[SerializeField, Range(0f, 1f)] private float _vignetteStart = 0.35f;
	// 메뉴 캔버스(40)보다 아래
	[SerializeField] private int _sortingOrder = 30;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private static Sprite _vignette;
	// 낮추기 전 주변광 → 여관이 닫힐 때 되돌린다
	private bool _ambientDimmed;
	private Color _ambientSky;
	private Color _ambientEquator;
	private Color _ambientGround;
	private float _ambientIntensity;
	#endregion

	private void Start()
	{
		int lightCount = DimLights();

		DimAmbient();

		BuildOverlay();

		if (_logState)
		{
			CPrint.Log($"CInnMood : 조명 {lightCount}개 x{_lightScale}, 주변광 x{_ambientScale}");
		}
	}

	// 이 씬에 있는 조명만 낮춘다 (주행 씬 조명은 그대로)
	private int DimLights()
	{
		GameObject[] roots = gameObject.scene.GetRootGameObjects();
		int count = 0;

		for (int i = 0; i < roots.Length; i++)
		{
			Light[] lights = roots[i].GetComponentsInChildren<Light>(true);

			for (int l = 0; l < lights.Length; l++)
			{
				if (lights[l] == null)
				{
					continue;
				}

				lights[l].intensity *= _lightScale;
				count++;
			}
		}

		return count;
	}

	private void OnDestroy()
	{
		RestoreAmbient();
	}

	// 주변광은 씬마다가 아니라 화면 전체에 하나 (여관이 덧붙여 열리면 주행 씬 값이 쓰인다)
	private void DimAmbient()
	{
		if (_ambientDimmed)
		{
			return;
		}

		_ambientSky = RenderSettings.ambientSkyColor;
		_ambientEquator = RenderSettings.ambientEquatorColor;
		_ambientGround = RenderSettings.ambientGroundColor;
		_ambientIntensity = RenderSettings.ambientIntensity;
		_ambientDimmed = true;

		RenderSettings.ambientSkyColor = _ambientSky * _ambientScale;
		RenderSettings.ambientEquatorColor = _ambientEquator * _ambientScale;
		RenderSettings.ambientGroundColor = _ambientGround * _ambientScale;
		RenderSettings.ambientIntensity = _ambientIntensity * _ambientScale;
	}

	private void RestoreAmbient()
	{
		if (!_ambientDimmed)
		{
			return;
		}

		_ambientDimmed = false;
		RenderSettings.ambientSkyColor = _ambientSky;
		RenderSettings.ambientEquatorColor = _ambientEquator;
		RenderSettings.ambientGroundColor = _ambientGround;
		RenderSettings.ambientIntensity = _ambientIntensity;
	}

	private void BuildOverlay()
	{
		RectTransform canvas = CUIBuilder.CreateOverlayCanvas("InnMoodCanvas", transform, _sortingOrder, false);

		if (_shadeColor.a > 0f)
		{
			Stretch(CUIBuilder.CreateImage("Shade", canvas, null, _shadeColor).rectTransform);
		}

		if (_vignetteColor.a > 0f)
		{
			Stretch(CUIBuilder.CreateImage("Vignette", canvas, GetVignette(_vignetteStart), _vignetteColor).rectTransform);
		}
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	private static Sprite GetVignette(float start)
	{
		if (_vignette != null)
		{
			return _vignette;
		}

		Texture2D texture = new Texture2D(VIGNETTE_SIZE, VIGNETTE_SIZE, TextureFormat.RGBA32, false);
		Color[] pixels = new Color[VIGNETTE_SIZE * VIGNETTE_SIZE];
		float half = VIGNETTE_SIZE * 0.5f;

		texture.name = "InnMoodVignette";
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;

		for (int y = 0; y < VIGNETTE_SIZE; y++)
		{
			for (int x = 0; x < VIGNETTE_SIZE; x++)
			{
				// 모서리가 1 을 넘도록 → 네 귀퉁이가 가장 어둡다
				float dx = (x + 0.5f - half) / half;
				float dy = (y + 0.5f - half) / half;
				float distance = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f;
				float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, 1f, distance));

				pixels[y * VIGNETTE_SIZE + x] = new Color(1f, 1f, 1f, alpha);
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		_vignette = Sprite.Create(texture, new Rect(0f, 0f, VIGNETTE_SIZE, VIGNETTE_SIZE), new Vector2(0.5f, 0.5f), 100f);
		_vignette.name = "InnMoodVignette";

		return _vignette;
	}
}
