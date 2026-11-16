using Unity.VisualScripting;
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
    private string _tokenName;
    private int _tokenIndex;
    private ETokenTriggerPoint _tokenTriggerPoint;
    private int _tokenCurrentStack;
    private int _tokenMaxStack;
    private int _tokenDuration;
    private int _tokenRemainingTurn;

    public STokenData(string tokenName, int tokenIndex, ETokenTriggerPoint tokenTriggerPoint, int tokenMaxStack, int tokenDuration)
    {
        _tokenName = tokenName;
        _tokenIndex = tokenIndex;
        _tokenTriggerPoint = tokenTriggerPoint;
        _tokenCurrentStack = 0;
        _tokenMaxStack = tokenMaxStack;
        _tokenDuration = tokenDuration;
        _tokenRemainingTurn = 0;
    }

    public void IncreaseStack()
    {
        if (_tokenCurrentStack >= _tokenMaxStack)
        {
            return;
        }

        _tokenCurrentStack++;
    }

    public bool ReduceStack()
    {
        if (_tokenCurrentStack == 0)
        {
            return false;
        }

        _tokenCurrentStack--;

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
    [Header("토큰 정보")]
    [SerializeField] private string _tokenName;
    [SerializeField] private int _tokenIndex;
    [SerializeField] private ETokenTriggerPoint _tokenTriggerPoint;
    [SerializeField] private int _tokenMaxStack;
    [SerializeField] private int _tokenDuration = -1; // -1 = None

    public STokenData MakeTokenInstance()
    {
        STokenData tokenData = new STokenData(_tokenName, _tokenIndex, _tokenTriggerPoint, _tokenMaxStack, _tokenDuration);

        return tokenData;
    }
}



