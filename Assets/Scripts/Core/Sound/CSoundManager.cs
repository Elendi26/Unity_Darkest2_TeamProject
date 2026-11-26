using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CSoundManager : CManagerSingleton<CSoundManager>
{
    protected override bool IsDontDestroy => true;

    #region 인스펙터
    [Header("SFX 데이터베이스")]
    [SerializeField] private CAudioClipSO _audioClipDB;

    [Header("채널 볼륨 (0 ~ 1)")]
    [Range(0f, 1f)]
    [SerializeField] private float _masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float _bgmVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 1f;

    [Header("SFX 풀 크기")]
    [SerializeField] private int _sfxPoolSize = 10;
    #endregion

    #region 내부 변수
    private Transform _root;
    private CObjectPool<AudioSource> _sfxSourcePool;

    private AudioSource _introSource;
    private AudioSource _bgmSource;

    private readonly List<AudioSource> _activeSfxList = new List<AudioSource>();
    private readonly Dictionary<AudioSource, CancellationTokenSource> _sfxCtsMap = new Dictionary<AudioSource, CancellationTokenSource>();

    private AudioClip _currentLoopClip;
    private float _modeBgmScale = 1f;
    private CancellationTokenSource _bgmCts;
    #endregion

    #region 프로퍼티
    public float MasterVolume
    {
        get
        {
            return _masterVolume;
        }
    }

    public float BgmVolume
    {
        get
        {
            return _bgmVolume;
        }
    }

    public float SfxVolume
    {
        get
        {
            return _sfxVolume;
        }
    }
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        _masterVolume = Mathf.Clamp01(_masterVolume);
        _bgmVolume = Mathf.Clamp01(_bgmVolume);
        _sfxVolume = Mathf.Clamp01(_sfxVolume);

        RefreshBgmVolume();
        RefreshSfxVolumes();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        _root = transform;
        _sfxSourcePool = new CObjectPool<AudioSource>(CreateSfxSource, null, _sfxPoolSize);
        CreateBgmSources();
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            StopSoundAll();
            CancelBgmTask();
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 전체 마스터 볼륨을 조절
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        RefreshBgmVolume();
        RefreshSfxVolumes();
    }

    /// <summary>
    /// BGM 채널 볼륨을 조절
    /// </summary>
    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        RefreshBgmVolume();
    }

    /// <summary>
    /// SFX 채널 볼륨을 조절
    /// </summary>
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        RefreshSfxVolumes();
    }

    private float GetEffectiveBgmVolume()
    {
        return _masterVolume * _bgmVolume * _modeBgmScale;
    }

    private float GetEffectiveSfxVolume()
    {
        return _masterVolume * _sfxVolume;
    }

    private void RefreshBgmVolume()
    {
        float effective = GetEffectiveBgmVolume();
        if (_introSource != null)
        {
            _introSource.volume = effective;
        }

        if (_bgmSource != null)
        {
            _bgmSource.volume = effective;
        }
    }

    private void RefreshSfxVolumes()
    {
        float effective = GetEffectiveSfxVolume();
        for (int i = 0; i < _activeSfxList.Count; i++)
        {
            AudioSource source = _activeSfxList[i];
            if (source == null)
            {
                continue;
            }

            source.volume = effective;
        }
    }

    private AudioSource CreateSfxSource()
    {
        GameObject obj = new GameObject("SfxSource");
        obj.transform.SetParent(_root);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    private void CreateBgmSources()
    {
        _introSource = CreateBgmSlot("BgmIntroSource");
        _bgmSource = CreateBgmSlot("BgmMainSource");
    }

    private AudioSource CreateBgmSlot(string slotName)
    {
        GameObject obj = new GameObject(slotName);
        obj.transform.SetParent(_root);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    private AudioClip GetSfxClip(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (_audioClipDB == null)
        {
            CPrint.Error("CSoundManager : _audioClipDB(CAudioClipSO)가 NULL입니다.");
            return null;
        }

        return _audioClipDB.GetClip(id);
    }

    /// <summary>
    /// 문자열 ID로 SFX 클립을 조회해 1회성으로 재생하고 종료 시 풀에 반환합니다.
    /// </summary>
    /// <param name="id">CAudioClipSO에 등록된 SFX ID</param>
    /// <returns>재생 중인 AudioSource 참조(실패 시 null)</returns>
    public AudioSource PlaySfxOneShot(string id)
    {
        AudioClip clip = GetSfxClip(id);
        if (clip == null)
        {
            return null;
        }

        AudioSource source = _sfxSourcePool.GetItem();
        if (source == null)
        {
            return null;
        }

        source.clip = clip;
        source.loop = false;
        source.volume = GetEffectiveSfxVolume();
        source.Play();
        _activeSfxList.Add(source);

        WatchSfxFinishAsync(source, ResetSfxToken(source)).Forget();
        return source;
    }

    /// <summary>
    /// 여러 SFX ID를 전달해 동시에 겹쳐 재생합니다.
    /// </summary>
    /// <param name="clipIds">동시 재생할 SFX ID 목록</param>
    public void PlaySfxGroup(params string[] clipIds)
    {
        if (clipIds == null || clipIds.Length == 0)
        {
            return;
        }

        for (int i = 0; i < clipIds.Length; i++)
        {
            PlaySfxOneShot(clipIds[i]);
        }
    }

    private void ReleaseSfxSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        CancelSfxToken(source);
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = 1f;
        _activeSfxList.Remove(source);
        _sfxSourcePool.ReleaseItem(source);
    }

    private CancellationToken ResetSfxToken(AudioSource source)
    {
        CancelSfxToken(source);
        CancellationTokenSource cts = new CancellationTokenSource();
        _sfxCtsMap[source] = cts;
        return cts.Token;
    }

    private void CancelSfxToken(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        if (_sfxCtsMap.TryGetValue(source, out CancellationTokenSource cts))
        {
            cts.Cancel();
            cts.Dispose();
            _sfxCtsMap.Remove(source);
        }
    }

    private async UniTaskVoid WatchSfxFinishAsync(AudioSource source, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && source != null && source.isPlaying)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        ReleaseSfxSource(source);
    }

    /// <summary>
    /// CGameModeDataSO의 BGM 클립 설정을 기준으로 재생합니다.
    /// 인트로가 있으면 인트로 -> 루프를 dspTime으로 예약 재생하며,
    /// KeepBgmIfSameLoop가 true이고 동일한 루프 클립이 재생 중이면 유지합니다.
    /// </summary>
    /// <param name="modeData">대상 게임 모드 데이터 SO</param>
    public void PlayModeBgm(CGameModeDataSO modeData)
    {
        if (modeData == null)
        {
            return;
        }

        _modeBgmScale = modeData.BgmVolumeScale;

        AudioClip introClip = modeData.BgmIntroClip;
        AudioClip loopClip = modeData.BgmLoopClip;

        if (modeData.KeepBgmIfSameLoop &&
            loopClip != null &&
            _currentLoopClip == loopClip &&
            _bgmSource != null &&
            _bgmSource.isPlaying)
        {
            RefreshBgmVolume();
            return;
        }

        StopBgm();

        _currentLoopClip = loopClip;
        float effectiveVolume = GetEffectiveBgmVolume();
        double startDspTime = AudioSettings.dspTime + 0.05d;

        if (introClip != null)
        {
            SetupBgmSlot(_introSource, introClip, loop: false, effectiveVolume);
            _introSource.PlayScheduled(startDspTime);

            if (loopClip != null)
            {
                double introDuration = (double)introClip.samples / introClip.frequency;
                SetupBgmSlot(_bgmSource, loopClip, loop: true, effectiveVolume);
                _bgmSource.PlayScheduled(startDspTime + introDuration);
                WatchIntroFinishAsync(_introSource, ResetBgmToken()).Forget();
            }
            else
            {
                WatchBgmSlotFinishAsync(_introSource, ResetBgmToken()).Forget();
            }
        }
        else if (loopClip != null)
        {
            SetupBgmSlot(_bgmSource, loopClip, loop: true, effectiveVolume);
            _bgmSource.PlayScheduled(startDspTime);
        }
    }

    /// <summary>
    /// CGameModeDataSO에 지정된 BGM 아웃트로를 재생합니다.
    /// </summary>
    /// <param name="modeData">대상 게임 모드 데이터 SO</param>
    public void PlayModeBgmOutro(CGameModeDataSO modeData)
    {
        if (modeData == null)
        {
            return;
        }

        _modeBgmScale = modeData.BgmVolumeScale;

        AudioClip outroClip = modeData.BgmOutroClip;
        StopBgm();

        if (outroClip == null)
        {
            return;
        }

        float effectiveVolume = GetEffectiveBgmVolume();
        SetupBgmSlot(_bgmSource, outroClip, loop: false, effectiveVolume);
        _bgmSource.PlayScheduled(AudioSettings.dspTime + 0.02d);
        WatchBgmSlotFinishAsync(_bgmSource, ResetBgmToken()).Forget();
    }

    /// <summary>
    /// 현재 재생 중인 BGM과 모든 SFX를 즉시 정지합니다.
    /// </summary>
    public void StopSoundAll()
    {
        StopBgm();

        for (int i = _activeSfxList.Count - 1; i >= 0; i--)
        {
            ReleaseSfxSource(_activeSfxList[i]);
        }
    }

    private void StopBgm()
    {
        CancelBgmTask();
        ResetBgmSlot(_introSource);
        ResetBgmSlot(_bgmSource);
        _currentLoopClip = null;
    }

    private void SetupBgmSlot(AudioSource slot, AudioClip clip, bool loop, float volume)
    {
        if (slot == null)
        {
            return;
        }

        slot.Stop();
        slot.clip = clip;
        slot.loop = loop;
        slot.volume = volume;
    }

    private void ResetBgmSlot(AudioSource slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.Stop();
        slot.clip = null;
        slot.loop = false;
        slot.volume = 1f;
    }

    private CancellationToken ResetBgmToken()
    {
        CancelBgmTask();
        _bgmCts = new CancellationTokenSource();
        return _bgmCts.Token;
    }

    private void CancelBgmTask()
    {
        if (_bgmCts != null)
        {
            _bgmCts.Cancel();
            _bgmCts.Dispose();
            _bgmCts = null;
        }
    }

    private async UniTaskVoid WatchIntroFinishAsync(AudioSource introSource, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && introSource != null && introSource.isPlaying)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (introSource != null && !introSource.isPlaying)
        {
            ResetBgmSlot(introSource);
        }
    }

    private async UniTaskVoid WatchBgmSlotFinishAsync(AudioSource slot, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && slot != null && slot.isPlaying)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        ResetBgmSlot(slot);
    }
}