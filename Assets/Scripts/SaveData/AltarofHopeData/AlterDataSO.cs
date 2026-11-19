using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 노드별 촛불 비용만 정의하고 해금해서 저장된 데이터들은 프로필 데이터에 따로 수치로 저장
/// </summary>
[CreateAssetMenu(fileName = "NewAltarNodeData", menuName = "AltarSO/AltarNodeData")]
public class AlterDataSO : ScriptableObject
{
	#region 인스펙터
	[Header("고유 ID")]
	[SerializeField] private string _nodeID;

	[Header("노드 이름")]
	[SerializeField] private string _nodeName;

	[Header("메뉴 카테고리")]
	[SerializeField] private AltarMenu _categori;

	[Header("노드 아이콘 이미지")]
	[SerializeField] private Sprite _iconImage;

	[Header("노드 해금 비용")]
	[SerializeField] private int[] _nodeCost;

	#endregion
	// 내부 변수



	// 프로퍼티

	public string NodeId => _nodeID;
	public string NodeName => _nodeName;
	public AltarMenu Categori => _categori;
	public Sprite IconImage => _iconImage;
	public int[] NodeCost => _nodeCost;
	public int TotalNodeCost => _nodeCost.Length;



	/// <summary>
	/// 노드 전체에 필요한 촛불 갯수를 반환하는 함수
	/// </summary>
	public int SumNodeCost()
	{
		int sumMaxCost = 0;
		if(_nodeCost == null)
		{
			sumMaxCost = 0;
		}

		for(int i = 0; i < _nodeCost.Length -1; i++)
		{
			sumMaxCost += _nodeCost[i];
		}

		return sumMaxCost;
	}

	/// <summary>
	/// 특정 노드까지 필요한 촛불 갯수를 반환하는 함수
	/// </summary>
	/// <param name="index"></param>
	public int SumSpecificNodeCost(int index)
	{
		int sumSpeCost = 0;
		if(index < 0 || _nodeCost == null)
		{
			return sumSpeCost = 0;
		}	


		// 모든 노드를 해금한 상태에서 해금을 시도할 경우 에러가 나지 않도록 최대 인덱스를 제한해야한다.
		int maxIndex = Mathf.Min(index, _nodeCost.Length - 1);

		// <=인 이유는 클릭하고 있는 노드를 포함하여 촛불 개수를 더해야하기 때문이다.
		for (int i = 0; i <= maxIndex; i++)
		{
			sumSpeCost += _nodeCost[i];
		}

		return sumSpeCost;
    }

	/// <summary>
	/// 개별 노드마다 필요한 촛불 개수를 카운팅하여 확인하는 함수
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public int NeedNodeCostIndi(int index)
	{
		if(_nodeCost == null)
		{
			return 0;
		}

		// 노드 단계가 0단계 이상, 최대단계 이하일 때만
		if(index >= 0 && index < _nodeCost.Length)
		{
			return _nodeCost[index];
		}

		// 해금 불가능할 때
		else
		{
			return -1;
		}

	}

}
