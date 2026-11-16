using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "2D3D/SO/TokenStructDataSO", fileName = "TokenStructDataSO")]
public class CTokenStructDataSO : ScriptableObject
{
    [Header("ÅäÅ«µé")]
    [SerializeReference]
    [PolymorphicDropdown]
    private List<ITokenInterface> _tokens;

    public List<STokenData> TokenStructMaker()
    {
        List<STokenData> tokenData = new List<STokenData>();

        for (int i = 0; i < _tokens.Count; ++i)
        {
            tokenData.Add(_tokens[i].MakeTokenInstance());
        }
        
        return tokenData;
    }
}
