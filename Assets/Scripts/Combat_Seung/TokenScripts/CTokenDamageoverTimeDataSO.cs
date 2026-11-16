using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SDamageoverTime
{
    private EDamageoverTime _tokenName;
    private ETokenTriggerPoint _tokenTriggerPoint;
    private int _tokenDamage;
    private int _tokenCurrentStack;

    public SDamageoverTime(EDamageoverTime tokenName, ETokenTriggerPoint tokenTriggerPoint, int tokenDamage, int tokenStack)
    {
        _tokenName = tokenName;
        _tokenTriggerPoint = tokenTriggerPoint;
        _tokenDamage = tokenDamage;
        _tokenCurrentStack = tokenStack;
    }
}

[CreateAssetMenu(menuName = "2D3D/SO/TokenDamageoverTimeDataSO", fileName = "TokenDamageoverTimeData")]
public class CTokenDamageoverTimeDataSO : ScriptableObject
{
    [Header("토큰 이미지")]
    [SerializeField] private Sprite _bleed;
    [SerializeField] private Sprite _blight;
    [SerializeField] private Sprite _burn;

    public Sprite DamageoverTimeUI(EDamageoverTime tokenName)
    {
        switch (tokenName)
        {
            case EDamageoverTime.Bleed:

                return _bleed;

            case EDamageoverTime.Blight:

                return _blight;

            case EDamageoverTime.Burn:

                return _burn;

            default:

                return default;
        }
    }
}
