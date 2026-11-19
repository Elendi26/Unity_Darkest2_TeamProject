using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[CreateAssetMenu(fileName = "NewCombatModifier", menuName = "Combat/Modifier Data")]
public class CombatModifier_SO : ScriptableObject
{
	#region 인스펙터
	[Header("아이콘")]
	[SerializeField] private Sprite _icon;

	[Header("타이틀")]
	[SerializeField, TextArea] private string _title;

	[Header("설명 텍스트")]
	[SerializeField, TextArea] private string _explain;

	#endregion

	#region 프로퍼티

	public Sprite Icon => _icon;
	public string Title => _title;
	public string Explain => _explain;

	#endregion
}
