using UnityEngine;


[CreateAssetMenu(fileName = "GameModeDatabase", menuName = "Database/GameModeDatabase")]
public class CGameModeDatabaseSO : CDataDatabaseSO<CGameModeDataSO>
{
    /// <summary>
    /// 문자열 ID를 통해 게임 모드 데이터를 검색하여 반환합니다.
    /// </summary>
    /// <param name="id">모드 고유 ID</param>
    public CGameModeDataSO GetGameModeData(string id)
    {
        if (TryGet(id, out var data))
        {
            return data;
        }

        CPrint.Warn($"해당 ID의 게임 모드 데이터를 찾을 수 없습니다: {id}");
        return null;
    }
}