using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전투 배경 → 전투가 벌어진 길가(마차 옆)를 한 장 찍어 전투 카메라 맨 뒤에 깐다
public class CCombatBackdrop
{
	#region 상수
	private const int MAX_WIDTH = 1920;
	private const int SORTING_ORDER = -1000;
	// 전투 카메라 먼 면 바로 앞에 배경을 둔다 (먼 면 거리 비율) → 영웅·적은 늘 배경 앞에 그려진다
	private const float PLANE_RATIO = 0.95f;
	private const int SHADE_HEIGHT = 64;
	#endregion

	#region 내부 변수
	private readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
	private readonly List<Behaviour> _hiddenBehaviours = new List<Behaviour>();
	private RenderTexture _texture;
	private Texture2D _shade;
	private GameObject _canvas;
	private bool _isWorldHidden;
	private bool _prevFog;
	#endregion

	public bool HasTexture => _texture != null;

	// 마차 옆에서 길 바깥을 본다 → 화면 오른쪽이 마차가 가던 쪽 (영웅 왼쪽·적 오른쪽과 맞춘다)
	public bool Capture(Transform anchor, Camera template, float height, float sideOffset, float fov, float pitch)
	{
		ReleaseTexture();

		if (anchor == null)
		{
			return false;
		}

		Vector3 forward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up);

		if (forward.sqrMagnitude < 0.0001f)
		{
			forward = Vector3.forward;
		}

		forward.Normalize();

		// 마차 왼쪽 → 이쪽을 보면 화면 오른쪽이 마차 앞
		Vector3 look = Vector3.Cross(forward, Vector3.up);
		int width = Mathf.Min(Screen.width, MAX_WIDTH);
		int heightPx = Mathf.Max(1, Mathf.RoundToInt(width * (float)Screen.height / Mathf.Max(1, Screen.width)));

		_texture = new RenderTexture(width, heightPx, 24);
		_texture.name = "CombatBackdrop";
		_texture.Create();

		GameObject go = new GameObject("CombatBackdropCapture");
		Camera camera = go.AddComponent<Camera>();

		// 주행 카메라의 배경색·하늘·컬링을 그대로 쓴다 (CopyFrom 은 위치도 따라오니 그 뒤에 옮긴다)
		if (template != null)
		{
			camera.CopyFrom(template);
		}

		camera.enabled = false;
		camera.orthographic = false;
		camera.fieldOfView = fov;
		camera.transform.SetPositionAndRotation
			(
			anchor.position + Vector3.up * height + look * sideOffset,
			Quaternion.LookRotation(look, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f)
			);
		camera.targetTexture = _texture;
		camera.Render();
		camera.targetTexture = null;
		Object.Destroy(go);

		return true;
	}

	// 주행 씬의 그려지는 것·조명·안개를 끈다 → 전투 카메라에 주행 월드가 비치거나 영웅에 빛이 겹치지 않게
	public void HideWorld(Scene scene)
	{
		if (_isWorldHidden || !scene.IsValid() || !scene.isLoaded)
		{
			return;
		}

		_isWorldHidden = true;

		Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null || !renderers[i].enabled || renderers[i].gameObject.scene != scene)
			{
				continue;
			}

			renderers[i].enabled = false;
			_hiddenRenderers.Add(renderers[i]);
		}

		HideBehaviours<Light>(scene);
		HideBehaviours<Terrain>(scene);

		_prevFog = RenderSettings.fog;
		RenderSettings.fog = false;
	}

	private void HideBehaviours<T>(Scene scene) where T : Behaviour
	{
		T[] all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == null || !all[i].enabled || all[i].gameObject.scene != scene)
			{
				continue;
			}

			all[i].enabled = false;
			_hiddenBehaviours.Add(all[i]);
		}
	}

	// 찍어 둔 길가를 전투 카메라 맨 뒤에 깐다 (전투 씬에 넣어 씬과 함께 내려가게)
	public void Show(Camera camera, Scene combatScene, Color tint, float shadeAlpha)
	{
		if (_texture == null || camera == null || _canvas != null)
		{
			return;
		}

		_canvas = new GameObject("CombatBackdrop", typeof(RectTransform));

		if (combatScene.IsValid() && combatScene.isLoaded)
		{
			SceneManager.MoveGameObjectToScene(_canvas, combatScene);
		}

		Canvas canvas = _canvas.AddComponent<Canvas>();

		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.worldCamera = camera;
		canvas.planeDistance = camera.farClipPlane * PLANE_RATIO;
		canvas.sortingOrder = SORTING_ORDER;

		RawImage image = CreateFill("Image", _canvas.transform);

		image.texture = _texture;
		image.color = tint;

		// 아래쪽을 어둡게 → 영웅·적 발밑과 아래 전투 UI 로 자연스럽게 이어지게
		if (shadeAlpha > 0f)
		{
			RawImage shade = CreateFill("Shade", _canvas.transform);

			shade.texture = GetShadeTexture();
			shade.color = new Color(0f, 0f, 0f, shadeAlpha);
		}
	}

	private static RawImage CreateFill(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));

		go.transform.SetParent(parent, false);

		RectTransform rect = (RectTransform)go.transform;

		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;

		RawImage image = go.AddComponent<RawImage>();

		image.raycastTarget = false;
		return image;
	}

	// 아래가 짙고 가운데가 투명, 위가 살짝 짙은 세로 그러데이션
	private Texture2D GetShadeTexture()
	{
		if (_shade != null)
		{
			return _shade;
		}

		_shade = new Texture2D(1, SHADE_HEIGHT, TextureFormat.RGBA32, false);
		_shade.name = "CombatBackdropShade";
		_shade.wrapMode = TextureWrapMode.Clamp;

		for (int y = 0; y < SHADE_HEIGHT; y++)
		{
			float t = y / (float)(SHADE_HEIGHT - 1);
			float bottom = Mathf.Clamp01((0.55f - t) / 0.55f);
			float top = Mathf.Clamp01((t - 0.75f) / 0.25f) * 0.5f;
			float alpha = Mathf.Max(bottom * bottom, top);

			_shade.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
		}

		_shade.Apply();
		return _shade;
	}

	// 숨긴 것을 되돌리고 배경을 치운다
	public void Release()
	{
		for (int i = 0; i < _hiddenRenderers.Count; i++)
		{
			if (_hiddenRenderers[i] != null)
			{
				_hiddenRenderers[i].enabled = true;
			}
		}

		for (int i = 0; i < _hiddenBehaviours.Count; i++)
		{
			if (_hiddenBehaviours[i] != null)
			{
				_hiddenBehaviours[i].enabled = true;
			}
		}

		_hiddenRenderers.Clear();
		_hiddenBehaviours.Clear();

		if (_isWorldHidden)
		{
			RenderSettings.fog = _prevFog;
			_isWorldHidden = false;
		}

		if (_canvas != null)
		{
			Object.Destroy(_canvas);
			_canvas = null;
		}

		ReleaseTexture();

		if (_shade != null)
		{
			Object.Destroy(_shade);
			_shade = null;
		}
	}

	private void ReleaseTexture()
	{
		if (_texture == null)
		{
			return;
		}

		_texture.Release();
		Object.Destroy(_texture);
		_texture = null;
	}
}
