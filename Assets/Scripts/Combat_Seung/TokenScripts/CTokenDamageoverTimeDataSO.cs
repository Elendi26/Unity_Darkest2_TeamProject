using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SDamageoverTime
{
    private EDamageoverTime _dotName;
    private ETokenTriggerPoint _dotTriggerPoint;
    private int _dotDamage;
    private int _dotRemainingTurn;

    public SDamageoverTime(EDamageoverTime dotName, int dotDamage, int dotTurn)
    {
        _dotName = dotName;
        _dotTriggerPoint = ETokenTriggerPoint.TurnStart;
        _dotDamage = dotDamage;
        _dotRemainingTurn = dotTurn;
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
