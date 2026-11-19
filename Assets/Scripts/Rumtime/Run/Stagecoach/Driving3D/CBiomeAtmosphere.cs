using UnityEngine;

public class CBiomeAtmosphere : MonoBehaviour
{
	#region 인스펙터
	[Header("안개 (원작 숲 : 선형 20~175m, 청록)")]
	[SerializeField] private bool _applyFog = true;
	[SerializeField] private Color _fogColor = new Color(0.341f, 0.478f, 0.49f, 1f);
	[SerializeField] private float _fogStart = 20f;
	[SerializeField] private float _fogEnd = 175f;

	[Header("주변광 (원작 숲)")]
	[SerializeField] private bool _applyAmbient = true;
	[SerializeField] private Color _ambientColor = new Color(0.348f, 0.426f, 0.5f, 1f);

	[Header("카메라 배경 (비우면 CChaseCamera.CurrentCamera)")]
	[SerializeField] private Camera _camera;
	[SerializeField] private bool _backgroundAsFog = true;

	[Header("먼 숲 실루엣")]
	[SerializeField] private GameObject _skylinePrefab;
	[SerializeField] private Transform _follow;
	[SerializeField] private Vector3 _skylineOffset = new Vector3(-40.18f, 15.48f, 164.27f);
	[SerializeField] private float _skylineTurnSmooth = 0.6f;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private bool _savedFog;
	private Color _savedFogColor;
	private FogMode _savedFogMode;
	private float _savedFogStart;
	private float _savedFogEnd;
	private UnityEngine.Rendering.AmbientMode _savedAmbientMode;
	private Color _savedAmbientColor;
	private bool _saved;
	private Transform _skylineRoot;
	private float _skylineYaw;
	private bool _hasYaw;
	#endregion

	private void Start()
	{
		SaveSettings();
		ApplySettings();

		if (_follow == null)
		{
			CCoach3DView coach = FindFirstObjectByType<CCoach3DView>();

			if (coach != null)
			{
				_follow = coach.transform;
			}
		}

		CreateSkyline();
	}

	private void LateUpdate()
	{
		ApplyCameraBackground();
		MoveSkyline();
	}

	private void OnDestroy()
	{
		RestoreSettings();
	}

	private void SaveSettings()
	{
		_savedFog = RenderSettings.fog;
		_savedFogColor = RenderSettings.fogColor;
		_savedFogMode = RenderSettings.fogMode;
		_savedFogStart = RenderSettings.fogStartDistance;
		_savedFogEnd = RenderSettings.fogEndDistance;
		_savedAmbientMode = RenderSettings.ambientMode;
		_savedAmbientColor = RenderSettings.ambientLight;
		_saved = true;
	}

	private void ApplySettings()
	{
		if (_applyFog)
		{
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogColor = _fogColor;
			RenderSettings.fogStartDistance = _fogStart;
			RenderSettings.fogEndDistance = _fogEnd;
		}

		if (_applyAmbient)
		{
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
			RenderSettings.ambientLight = _ambientColor;
		}

		if (_logState)
		{
			CPrint.Log($"CBiomeAtmosphere : 안개 {_fogStart:F0}~{_fogEnd:F0}m · 주변광 적용");
		}
	}

	private void RestoreSettings()
	{
		if (!_saved)
		{
			return;
		}

		RenderSettings.fog = _savedFog;
		RenderSettings.fogColor = _savedFogColor;
		RenderSettings.fogMode = _savedFogMode;
		RenderSettings.fogStartDistance = _savedFogStart;
		RenderSettings.fogEndDistance = _savedFogEnd;
		RenderSettings.ambientMode = _savedAmbientMode;
		RenderSettings.ambientLight = _savedAmbientColor;
	}

	// 카메라는 나중에 생기거나 바뀔 수 있다 → 매 프레임 확인
	private void ApplyCameraBackground()
	{
		if (!_backgroundAsFog)
		{
			return;
		}

		Camera camera = _camera != null ? _camera : CChaseCamera.CurrentCamera;

		if (camera == null || (camera.clearFlags == CameraClearFlags.SolidColor && camera.backgroundColor == _fogColor))
		{
			return;
		}

		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = _fogColor;
	}

	private void CreateSkyline()
	{
		if (_skylinePrefab == null)
		{
			return;
		}

		_skylineRoot = new GameObject("SkylineRoot").transform;
		_skylineRoot.SetParent(transform, false);

		GameObject skyline = Instantiate(_skylinePrefab, _skylineRoot);

		skyline.transform.localPosition = _skylineOffset;
		skyline.transform.localRotation = Quaternion.identity;

		MoveSkyline();
	}

	// 대상 위치를 따라가고, 진행 방향으로 천천히 돈다
	private void MoveSkyline()
	{
		if (_skylineRoot == null || _follow == null)
		{
			return;
		}

		Vector3 forward = Vector3.ProjectOnPlane(_follow.forward, Vector3.up);
		float yaw = forward.sqrMagnitude > 0.001f ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg : _skylineYaw;

		if (!_hasYaw || _skylineTurnSmooth <= 0f)
		{
			_skylineYaw = _skylineTurnSmooth <= 0f && _hasYaw ? _skylineYaw : yaw;
			_hasYaw = true;
		}
		else
		{
			_skylineYaw = Mathf.LerpAngle(_skylineYaw, yaw, 1f - Mathf.Exp(-_skylineTurnSmooth * Time.deltaTime));
		}

		// 높이는 대상(마차)이 선 땅 기준
		_skylineRoot.SetPositionAndRotation(_follow.position, Quaternion.Euler(0f, _skylineYaw, 0f));
	}
}
