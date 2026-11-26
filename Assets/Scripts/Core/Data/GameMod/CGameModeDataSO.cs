using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GameModeData_", menuName = "Database/GameModeData")]
public class CGameModeDataSO : CGameDataSO
{
    #region 인스펙터
    [SerializeField] private string _modeName = string.Empty;
    [SerializeField] private AssetReference _sceneReference;
    [SerializeField] private string _sceneAddress = string.Empty;
    [SerializeField] private LoadSceneMode _loadMode = LoadSceneMode.Single;
    [SerializeField] private bool _useFade = true;
    [SerializeField] private bool _isActiveMode = false;

    [Header("BGM 설정")]
    [SerializeField] private AudioClip _bgmIntroClip;
    [SerializeField] private AudioClip _bgmLoopClip;
    [SerializeField] private AudioClip _bgmOutroClip;
    [SerializeField] private bool _keepBgmIfSameLoop = false;
    [SerializeField, Range(0f, 1f)] private float _bgmVolumeScale = 1f;
    #endregion

    #region 프로퍼티
    public string ModeName => _modeName;
    public AssetReference SceneReference => _sceneReference;
    public bool HasValidSceneReference => _sceneReference != null && _sceneReference.RuntimeKeyIsValid();
    public string SceneAddress
    {
        get
        {
            if (HasValidSceneReference)
            {
                return _sceneReference.RuntimeKey.ToString();
            }

            return _sceneAddress;
        }
    }

    public LoadSceneMode LoadMode => _loadMode;
    public bool UseFade => _useFade;
    public bool IsActiveMode => _isActiveMode;

    public AudioClip BgmIntroClip
    {
        get
        {
            return _bgmIntroClip;
        }
    }
    public AudioClip BgmLoopClip
    {
        get
        {
            return _bgmLoopClip;
        }
    }
    public AudioClip BgmOutroClip
    {
        get
        {
            return _bgmOutroClip;
        }
    }
    public bool KeepBgmIfSameLoop
    {
        get
        {
            return _keepBgmIfSameLoop;
        }
    }
    // 모드별 배경음 크기 (설정 음량에 곱한다) / 경태
    public float BgmVolumeScale
    {
        get
        {
            return _bgmVolumeScale;
        }
    }
    #endregion

    protected override void OnValidate()
    {
        base.OnValidate();

        if (!HasValidSceneReference && string.IsNullOrWhiteSpace(_sceneAddress))
        {
            CPrint.Warn($"{name} : 어드레서블 SceneReference와 SceneAddress가 모두 비어 있습니다.");
        }
    }
}