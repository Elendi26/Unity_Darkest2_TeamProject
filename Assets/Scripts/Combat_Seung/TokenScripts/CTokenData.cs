using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public enum ETokenTriggerPoint
{
    None,
    //
    Critical,
    Guard,
    UseToTarget,
    Targeted,
    Damaged,
    //
    RoundStart,
    TurnStart,
    TurnOver,
    Combo
}

public interface ITokenInterface
{
    public abstract STokenData MakeTokenInstance();
}

public struct STokenData
{
    [Header("토큰명은 비트마스크라 복수 선택 가능, 주의할 것 복수 선택 금지")]
    private ETokenBitMask _tokenName;
    private ETokenTriggerPoint _tokenTriggerPoint;
    private int _tokenCurrentStack;
    private int _tokenMaxStack;
    private int _tokenDuration;
    private int _tokenRemainingTurn;

    public STokenData(ETokenBitMask tokenBitMask, ETokenTriggerPoint tokenTriggerPoint, int tokenMaxStack, int tokenDuration)
    {
        _tokenName = tokenBitMask;
        _tokenTriggerPoint = tokenTriggerPoint;
        _tokenCurrentStack = 0;
        _tokenMaxStack = tokenMaxStack;
        _tokenDuration = tokenDuration;
        _tokenRemainingTurn = 0;
    }

    public bool CheckTokenByBitMask(ETokenBitMask token)
    {
        if (_tokenName == token)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    public void CheckTriggerPoint(ETokenTriggerPoint trigger)
    {

    }


    public void IncreaseStack(int increase = 1)
    {
        if (_tokenCurrentStack >= _tokenMaxStack)
        {
            return;
        }

        _tokenCurrentStack += increase;
    }

    public bool ReduceStack(int reduce = 1)
    {
        if (_tokenCurrentStack == 0)
        {
            return false;
        }

        _tokenCurrentStack -= reduce;

        if (_tokenCurrentStack == 0)
        {
            return false;
        }

        return true;
    }

    public void DecrementTurn()
    {
        _tokenRemainingTurn--;
    }

    public void ResetTurn()
    {
        _tokenRemainingTurn = _tokenDuration;
    }
}



[System.Serializable]
public class CTokenData : ITokenInterface
{
    [Header("토큰 정보, 토큰 명은 복수 선택 금지")]
    [SerializeField] private ETokenBitMask _tokenName;
    [SerializeField] private ETokenTriggerPoint _tokenTriggerPoint;
    [SerializeField] private int _tokenMaxStack;
    [SerializeField] private int _tokenDuration = -1; // -1 = None

    public STokenData MakeTokenInstance()
    {
        STokenData tokenData = new STokenData(_tokenName, _tokenTriggerPoint, _tokenMaxStack, _tokenDuration);

        return tokenData;
    }
}



