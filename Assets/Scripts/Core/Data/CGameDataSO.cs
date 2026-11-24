using UnityEngine;

public abstract class CGameDataSO : ScriptableObject
{
    #region 인스펙터
    [SerializeField] private string _id = "";
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 데이터를 구분하기 위한 고유 ID
    /// </summary>
    public string Id => _id;
    #endregion

    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_id))
        {
            CPrint.Warn($"{name} : ID가 비어 있습니다.");
        }
    }
}