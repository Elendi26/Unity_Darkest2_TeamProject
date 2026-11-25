using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CObjectPoolContainer<T>
{
    /// <summary>
    /// 풀이 관리하는 실제 데이터(인스턴스)
    /// </summary>
    public T Item { get; set; }

    /// <summary>
    /// 현재 사용 중인지 여부
    /// </summary>
    public bool Used { get; private set; }

    /// <summary>
    /// 아이템을 사용 상태로 변경합니다.
    /// </summary>
    public void Consume()
    {
        Used = true;
    }

    /// <summary>
    /// 아이템을 미사용(반환) 상태로 변경합니다.
    /// </summary>
    public void Release()
    {
        Used = false;
    }
}