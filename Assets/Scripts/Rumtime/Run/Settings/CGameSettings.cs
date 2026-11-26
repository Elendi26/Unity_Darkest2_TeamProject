using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 설정 (배경음·효과음 음량, 해상도) → PlayerPrefs 에 저장하고, 바뀌면 알린다
public static class CGameSettings
{
	#region 상수
	private const string KEY_BGM = "Settings.BgmVolume";
	private const string KEY_SFX = "Settings.SfxVolume";
	private const string KEY_WIDTH = "Settings.ResolutionWidth";
	private const string KEY_HEIGHT = "Settings.ResolutionHeight";
	private const string KEY_FULLSCREEN = "Settings.Fullscreen";

	// 설정 창에서 고를 수 있는 해상도
	private static readonly Vector2Int[] RESOLUTIONS =
	{
		new Vector2Int(2560, 1440),
		new Vector2Int(1920, 1080),
		new Vector2Int(1600, 900),
		new Vector2Int(1366, 768),
		new Vector2Int(1360, 768),
		new Vector2Int(1280, 800),
		new Vector2Int(1280, 768),
		new Vector2Int(1280, 720),
		new Vector2Int(1176, 664),
	};
	#endregion

	#region 이벤트
	// 음량이 바뀌었을 때 → CAudioVolume 이 구독한다
	public static event Action OnVolumeChanged;
	#endregion

	#region 내부 변수
	private static bool _isLoaded;
	private static float _bgmVolume = 1f;
	private static float _sfxVolume = 1f;
	#endregion

	// 배경음 음량 (0~1)
	public static float BgmVolume
	{
		get
		{
			EnsureLoaded();
			return _bgmVolume;
		}
	}

	// 효과음 음량 (0~1)
	public static float SfxVolume
	{
		get
		{
			EnsureLoaded();
			return _sfxVolume;
		}
	}

	// 게임을 켜면 저장된 해상도부터 맞춘다
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		_isLoaded = false;
		EnsureLoaded();

		// 사운드 매니저는 타이틀 씬에 놓여 있다 → 씬이 열릴 때마다 저장된 음량을 넘겨준다
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneLoaded += HandleSceneLoaded;

		if (PlayerPrefs.HasKey(KEY_WIDTH) && PlayerPrefs.HasKey(KEY_HEIGHT))
		{
			ApplyResolution(PlayerPrefs.GetInt(KEY_WIDTH), PlayerPrefs.GetInt(KEY_HEIGHT), PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1);
		}
	}

	private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		SyncSoundManager();
	}

	// 팀 사운드 매니저(BGM·SFX 채널)에 음량을 넘긴다 → 없으면 넘기지 않는다 (Instance 로 새로 만들지 않게 찾기만)
	private static void SyncSoundManager()
	{
		CSoundManager soundManager = UnityEngine.Object.FindFirstObjectByType<CSoundManager>();

		if (soundManager == null)
		{
			return;
		}

		soundManager.SetBgmVolume(BgmVolume);
		soundManager.SetSfxVolume(SfxVolume);
	}

	private static void EnsureLoaded()
	{
		if (_isLoaded)
		{
			return;
		}

		_isLoaded = true;
		_bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, 1f));
		_sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, 1f));
	}

	// 적용해 둔 해상도 → 없으면 지금 화면 크기 (에디터는 해상도를 바꿔도 Game 창 크기가 그대로라 저장 값을 믿는다)
	public static Vector2Int GetAppliedResolution()
	{
		if (PlayerPrefs.HasKey(KEY_WIDTH) && PlayerPrefs.HasKey(KEY_HEIGHT))
		{
			return new Vector2Int(PlayerPrefs.GetInt(KEY_WIDTH), PlayerPrefs.GetInt(KEY_HEIGHT));
		}

		return new Vector2Int(Screen.width, Screen.height);
	}

	// 적용해 둔 전체 화면 여부 → 없으면 지금 화면 모드
	public static bool GetAppliedFullscreen()
	{
		if (PlayerPrefs.HasKey(KEY_FULLSCREEN))
		{
			return PlayerPrefs.GetInt(KEY_FULLSCREEN) == 1;
		}

		return Screen.fullScreenMode != FullScreenMode.Windowed;
	}

	public static float GetVolume(EAudioCategory category)
	{
		return category == EAudioCategory.Bgm ? BgmVolume : SfxVolume;
	}

	public static void SetBgmVolume(float volume)
	{
		EnsureLoaded();
		_bgmVolume = Mathf.Clamp01(volume);
		PlayerPrefs.SetFloat(KEY_BGM, _bgmVolume);
		SyncSoundManager();
		OnVolumeChanged?.Invoke();
	}

	public static void SetSfxVolume(float volume)
	{
		EnsureLoaded();
		_sfxVolume = Mathf.Clamp01(volume);
		PlayerPrefs.SetFloat(KEY_SFX, _sfxVolume);
		SyncSoundManager();
		OnVolumeChanged?.Invoke();
	}

	// 해상도를 바꾸고 저장한다
	public static void SetResolution(int width, int height, bool fullscreen)
	{
		if (width <= 0 || height <= 0)
		{
			return;
		}

		ApplyResolution(width, height, fullscreen);

		PlayerPrefs.SetInt(KEY_WIDTH, width);
		PlayerPrefs.SetInt(KEY_HEIGHT, height);
		PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
		PlayerPrefs.Save();
	}

	private static void ApplyResolution(int width, int height, bool fullscreen)
	{
		FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

		Screen.SetResolution(width, height, mode);
	}

	// 음량은 끌 때마다 저장하지 않고 창을 닫을 때 한 번에 쓴다
	public static void Save()
	{
		PlayerPrefs.Save();
	}

	// 고를 수 있는 해상도 → 정해 둔 목록 중 모니터보다 크지 않은 것만, 작은 것부터
	public static void GetResolutions(List<Vector2Int> result)
	{
		result.Clear();

		Vector2Int max = GetMonitorMaxResolution();

		for (int i = 0; i < RESOLUTIONS.Length; i++)
		{
			Vector2Int size = RESOLUTIONS[i];

			if (max.x > 0 && (size.x > max.x || size.y > max.y))
			{
				continue;
			}

			result.Add(size);
		}

		// 모니터가 목록의 가장 작은 것보다도 작으면 → 목록을 그대로 쓴다
		if (result.Count == 0)
		{
			result.AddRange(RESOLUTIONS);
		}

		result.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
	}

	// 목록에 없는 크기(예전에 적용한 값 등)일 때 → 넓이가 가장 가까운 것
	public static int FindNearestResolution(List<Vector2Int> list, Vector2Int size)
	{
		int best = -1;
		long bestDiff = long.MaxValue;
		long area = (long)size.x * size.y;

		for (int i = 0; i < list.Count; i++)
		{
			long diff = Math.Abs((long)list[i].x * list[i].y - area);

			if (diff < bestDiff)
			{
				bestDiff = diff;
				best = i;
			}
		}

		return best;
	}

	// 지금 모니터의 해상도 (전체 화면은 이 크기로) → 모를 때는 지금 화면 크기
	public static Vector2Int GetMonitorResolution()
	{
		Display display = Display.main;

		if (display != null && display.systemWidth > 0 && display.systemHeight > 0)
		{
			return new Vector2Int(display.systemWidth, display.systemHeight);
		}

		Resolution current = Screen.currentResolution;

		if (current.width > 0 && current.height > 0)
		{
			return new Vector2Int(current.width, current.height);
		}

		return new Vector2Int(Screen.width, Screen.height);
	}

	// 모니터가 지원하는 가장 큰 크기 → 모를 때는 (0, 0)
	private static Vector2Int GetMonitorMaxResolution()
	{
		Vector2Int max = Vector2Int.zero;
		Resolution[] resolutions = Screen.resolutions;

		for (int i = 0; i < resolutions.Length; i++)
		{
			max.x = Mathf.Max(max.x, resolutions[i].width);
			max.y = Mathf.Max(max.y, resolutions[i].height);
		}

		return max;
	}
}
