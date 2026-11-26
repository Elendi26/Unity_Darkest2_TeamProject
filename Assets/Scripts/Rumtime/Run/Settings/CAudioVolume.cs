using UnityEngine;

// AudioSource 에 붙이면 설정의 배경음·효과음 음량을 따른다 (원래 볼륨 × 설정 음량)
[RequireComponent(typeof(AudioSource))]
public class CAudioVolume : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private EAudioCategory _category = EAudioCategory.Sfx;
	#endregion

	#region 내부 변수
	private AudioSource _source;
	// 설정을 곱하기 전 원래 볼륨
	private float _baseVolume = 1f;
	#endregion

	private void Awake()
	{
		_source = GetComponent<AudioSource>();
		_baseVolume = _source.volume;
	}

	private void OnEnable()
	{
		CGameSettings.OnVolumeChanged += Apply;
		Apply();
	}

	private void OnDisable()
	{
		CGameSettings.OnVolumeChanged -= Apply;
	}

	private void Apply()
	{
		if (_source == null)
		{
			return;
		}

		_source.volume = _baseVolume * CGameSettings.GetVolume(_category);
	}
}
