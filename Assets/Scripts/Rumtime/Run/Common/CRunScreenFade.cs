using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 덧붙여 여는 씬(전투·교차로) 들어가고 나올 때 화면을 검게 덮는다

public class CRunScreenFade : MonoBehaviour
{
	#region 상수
	private const int SORTING_ORDER = 1001;
	private const float SAFETY_DELAY = 3f;
	private const float SAFETY_FADE = 0.4f;
	#endregion

	#region 내부 변수
	private static CRunScreenFade _instance;
	private CanvasGroup _group;
	private int _version;
	#endregion

	// 지금 검게 덮여 있는지
	public static bool IsCovered => _instance != null && _instance._group.alpha >= 1f;

	// 검게 덮는다 (실제 시간 → 일시정지 중에도 돈다)
	public static IEnumerator Cover(float duration)
	{
		return GetOrCreate().FadeTo(1f, duration);
	}

	// 덮개를 걷는다
	public static IEnumerator Uncover(float duration)
	{
		if (_instance == null)
		{
			yield break;
		}

		yield return _instance.FadeTo(0f, duration);
	}

	// 바로 걷는다 (다음 씬의 검은 시작 화면이 이어받을 때)
	public static void Clear()
	{
		if (_instance == null)
		{
			return;
		}

		_instance._version++;
		_instance._group.alpha = 0f;
		_instance._group.blocksRaycasts = false;
	}

	// 덮은 채로 씬이 바뀌었는데 아무도 걷지 않으면 → 잠시 뒤 스스로 걷는다 (화면이 검게 멈추지 않게)
	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode != LoadSceneMode.Single || _group.alpha <= 0f)
		{
			return;
		}

		StartCoroutine(UncoverLater());
	}

	private IEnumerator UncoverLater()
	{
		int version = _version;

		yield return new WaitForSecondsRealtime(SAFETY_DELAY);

		if (version == _version && _group.alpha > 0f)
		{
			yield return FadeTo(0f, SAFETY_FADE);
		}
	}

	private static CRunScreenFade GetOrCreate()
	{
		if (_instance != null)
		{
			return _instance;
		}

		GameObject root = new GameObject("RunScreenFade", typeof(RectTransform));

		DontDestroyOnLoad(root);

		Canvas canvas = root.AddComponent<Canvas>();

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = SORTING_ORDER;

		Image image = root.AddComponent<Image>();

		image.color = Color.black;
		image.raycastTarget = true;

		_instance = root.AddComponent<CRunScreenFade>();
		_instance._group = root.AddComponent<CanvasGroup>();
		_instance._group.alpha = 0f;
		_instance._group.blocksRaycasts = false;
		SceneManager.sceneLoaded += _instance.HandleSceneLoaded;
		return _instance;
	}

	private IEnumerator FadeTo(float target, float duration)
	{
		// 새 페이드가 시작되면 앞의 것은 멈춘다
		int version = ++_version;
		float start = _group.alpha;
		float time = 0f;

		// 덮는 동안은 클릭을 막는다
		_group.blocksRaycasts = true;

		while (time < duration && version == _version)
		{
			time += Time.unscaledDeltaTime;
			_group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(time / duration));
			yield return null;
		}

		if (version != _version)
		{
			yield break;
		}

		_group.alpha = target;
		_group.blocksRaycasts = target > 0f;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;

		if (_instance == this)
		{
			_instance = null;
		}
	}
}
