using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipDatabase", menuName = "Database/AudioClipDatabase")]
public class CAudioClipSO : CDataDatabaseSO<CAudioClipDataSO>
{
    /// <summary>
    /// 문자열 ID를 통해 SFX 오디오 클립을 반환합니다.
    /// </summary>
    public AudioClip GetClip(string id)
    {
        if (TryGet(id, out CAudioClipDataSO data) && data != null)
        {
            return data.Clip;
        }

        CPrint.Warn($"해당 ID의 SFX 데이터를 찾을 수 없습니다: {id}");
        return null;
    }
}