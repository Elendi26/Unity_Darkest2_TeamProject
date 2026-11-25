using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipData_", menuName = "Database/AudioClipData")]
public class CAudioClipDataSO : CGameDataSO
{
    #region 인스펙터
    [SerializeField] private AudioClip _clip;
    #endregion

    #region 프로퍼티
    public AudioClip Clip
    {
        get
        {
            return _clip;
        }
    }
    #endregion

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_clip == null)
        {
            CPrint.Warn($"{name} : AudioClip이 비어 있습니다.");
        }
    }
}