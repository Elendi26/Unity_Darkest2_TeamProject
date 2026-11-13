using UnityEngine;

// 마차 지붕 횃불 → 횃불 수치(0~1)에 맞춰 불빛·빛무리·가로 빛줄기 밝기를 바꾼다
//  ㄴ 수치가 줄면 빛이 약해지고 비추는 거리도 줄어든다
//  ㄴ 조금씩 일렁이게(Perlin) 해서 불처럼 보이게 한다
[RequireComponent(typeof(Light))]
public class CCoachTorchView : MonoBehaviour
{
	#region 인스펙터
	// 마차 모델 안에서 횃불이 붙은 자리 이름 → 찾으면 그 자리로 옮긴다
	[SerializeField] private string _anchorName = "Torch_VFX_Anchor";
	[SerializeField] private Vector3 _anchorOffset = new Vector3(0f, 0.25f, 0f);
	// 이 거리 안의 물체는 그림자를 만들지 않는다 (횃불 틀이 빛을 가리지 않게)
	[SerializeField] private float _shadowNearPlane = 1f;
	// 횃불 0 → 가득 일 때 불빛 세기·거리
	[SerializeField] private Vector2 _intensity = new Vector2(0.6f, 5f);
	[SerializeField] private Vector2 _range = new Vector2(8f, 24f);
	[SerializeField] private Color _lightColor = new Color(1f, 0.6f, 0.3f, 1f);
	// 빛무리(동그란 번짐) 크기
	[SerializeField] private Vector2 _glowSize = new Vector2(0.5f, 2.4f);
	// 가로 빛줄기 길이·두께
	[SerializeField] private Vector2 _streakLength = new Vector2(1.5f, 14f);
	[SerializeField] private float _streakThickness = 0.22f;
	[SerializeField] private Color _glowColor = new Color(1f, 0.55f, 0.2f, 1f);
	[SerializeField] private float _streakAlpha = 0.8f;
	// 일렁임
	[SerializeField, Range(0f, 0.5f)] private float _flickerAmount = 0.12f;
	[SerializeField] private float _flickerSpeed = 7f;
	// 수치가 바뀔 때 따라가는 속도
	[SerializeField] private float _levelSmooth = 3f;
	// 비우면 Hidden/CoachTorchGlow 를 찾아 쓴다 (빌드에 넣으려면 여기 넣어 둔다)
	[SerializeField] private Shader _glowShader;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private const int GLOW_TEXTURE_SIZE = 128;

	private Light _light;
	private Transform _glow;
	private Transform _streak;
	private Material _glowMaterial;
	private Material _streakMaterial;
	private Texture2D _glowTexture;
	private float _level = 1f;
	private float _targetLevel = 1f;
	private float _seed;
	#endregion

	private void Awake()
	{
		_light = GetComponent<Light>();
		_seed = Random.Range(0f, 100f);
	}

	private void Start()
	{
		MoveToAnchor();

		// 횃불 틀(등) 메시가 바로 옆이라 그림자에 빛이 막힌다 → 가까운 그림자는 무시
		if (_light != null)
		{
			_light.shadowNearPlane = Mathf.Max(_light.shadowNearPlane, _shadowNearPlane);
		}

		CreateGlow();
		Apply(1f);
	}

	private void OnDestroy()
	{
		if (_glowMaterial != null)
		{
			Destroy(_glowMaterial);
		}

		if (_streakMaterial != null)
		{
			Destroy(_streakMaterial);
		}

		if (_glowTexture != null)
		{
			Destroy(_glowTexture);
		}
	}

	// 횃불 수치 비율 (0~1) → CDriving3DScene 이 횃불 값이 바뀔 때 부른다
	public void SetLevel(float ratio, bool instant = false)
	{
		_targetLevel = Mathf.Clamp01(ratio);

		if (instant)
		{
			_level = _targetLevel;
		}
	}

	private void LateUpdate()
	{
		_level = Mathf.MoveTowards(_level, _targetLevel, _levelSmooth * Time.deltaTime);

		float noise = Mathf.PerlinNoise(_seed, Time.time * _flickerSpeed) * 2f - 1f;

		Apply(1f + noise * _flickerAmount);
		FaceCamera();
	}

	private void Apply(float flicker)
	{
		// 수치가 0 이어도 아주 희미하게는 남긴다
		float level = _level;

		if (_light != null)
		{
			_light.color = _lightColor;
			_light.intensity = Mathf.Lerp(_intensity.x, _intensity.y, level) * flicker;
			_light.range = Mathf.Lerp(_range.x, _range.y, level);
		}

		float glowSize = Mathf.Lerp(_glowSize.x, _glowSize.y, level) * flicker;

		if (_glow != null)
		{
			_glow.localScale = new Vector3(glowSize, glowSize, 1f);
		}

		if (_streak != null)
		{
			float length = Mathf.Lerp(_streakLength.x, _streakLength.y, level) * flicker;

			_streak.localScale = new Vector3(length, _streakThickness * Mathf.Lerp(0.6f, 1f, level), 1f);
		}

		if (_glowMaterial != null)
		{
			_glowMaterial.color = _glowColor * Mathf.Lerp(0.35f, 1f, level);
		}

		if (_streakMaterial != null)
		{
			_streakMaterial.color = _glowColor * (_streakAlpha * level);
		}
	}

	// 빛무리·빛줄기는 늘 카메라를 본다 → 빛줄기는 화면에서 가로로 보인다
	private void FaceCamera()
	{
		Camera camera = CChaseCamera.CurrentCamera;

		if (camera == null)
		{
			return;
		}

		Quaternion rotation = camera.transform.rotation;

		if (_glow != null)
		{
			_glow.rotation = rotation;
		}

		if (_streak != null)
		{
			_streak.rotation = rotation;
		}
	}

	private void MoveToAnchor()
	{
		Transform searchRoot = transform.parent != null ? transform.parent : transform;
		Transform anchor = FindChild(searchRoot, _anchorName);

		if (anchor == null)
		{
			if (_logState)
			{
				CPrint.Warn($"CCoachTorchView : {_anchorName} 를 못 찾아 지금 자리에서 불을 켭니다.");
			}

			return;
		}

		// 횃불 자리 오브젝트는 꺼져 있다 → 그 밑에 넣으면 불도 같이 꺼진다
		//  ㄴ 같은 부모 밑, 같은 자리로만 옮긴다
		transform.SetParent(anchor.parent, false);
		transform.localPosition = anchor.localPosition + anchor.localRotation * _anchorOffset;
		transform.localRotation = Quaternion.identity;

		if (_logState)
		{
			CPrint.Log($"CCoachTorchView : 횃불 불빛을 {anchor.name} 에 붙임");
		}
	}

	private static Transform FindChild(Transform root, string name)
	{
		if (root.name == name)
		{
			return root;
		}

		for (int i = 0; i < root.childCount; i++)
		{
			Transform found = FindChild(root.GetChild(i), name);

			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private void CreateGlow()
	{
		Shader shader = _glowShader != null ? _glowShader : Shader.Find("Hidden/CoachTorchGlow");

		if (shader == null)
		{
			CPrint.Warn("CCoachTorchView : Hidden/CoachTorchGlow 셰이더가 없어 빛무리를 그리지 않습니다.");
			return;
		}

		_glowTexture = CreateGlowTexture();
		_glowMaterial = new Material(shader) { name = "CoachTorchGlow (Runtime)", mainTexture = _glowTexture };
		_streakMaterial = new Material(shader) { name = "CoachTorchStreak (Runtime)", mainTexture = _glowTexture };
		_glow = CreateQuad("TorchGlow", _glowMaterial);
		_streak = CreateQuad("TorchStreak", _streakMaterial);
	}

	private Transform CreateQuad(string name, Material material)
	{
		GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

		quad.name = name;
		Destroy(quad.GetComponent<Collider>());
		quad.transform.SetParent(transform, false);

		MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();

		meshRenderer.sharedMaterial = material;
		meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;

		return quad.transform;
	}

	// 가운데가 밝고 가장자리로 부드럽게 사라지는 동그라미
	private static Texture2D CreateGlowTexture()
	{
		Texture2D texture = new Texture2D(GLOW_TEXTURE_SIZE, GLOW_TEXTURE_SIZE, TextureFormat.RGBA32, false);
		float half = (GLOW_TEXTURE_SIZE - 1) * 0.5f;

		texture.wrapMode = TextureWrapMode.Clamp;

		for (int y = 0; y < GLOW_TEXTURE_SIZE; y++)
		{
			for (int x = 0; x < GLOW_TEXTURE_SIZE; x++)
			{
				float dx = (x - half) / half;
				float dy = (y - half) / half;
				float distance = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
				float value = Mathf.Pow(1f - distance, 2.4f);

				texture.SetPixel(x, y, new Color(value, value, value, value));
			}
		}

		texture.Apply();

		return texture;
	}
}
