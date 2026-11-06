using UnityEngine;

/*
각 엔티티들 초기 능력치

이름/최대체력/속도/각 저항치/이동가능거리
*/

[CreateAssetMenu(menuName = "2D3D/SO/HeroBasicDataSO", fileName = "HeroBasicData_")]
public class CEntityBasicDataSO : ScriptableObject
{
	#region 인스펙터

	[Header("적/영웅 이름")]
	[SerializeField] private string _name;

	[Header("체력 및 속도")]
	[SerializeField, Min(0)] private int _maxHP;
	[SerializeField, Min(0)] private int _maxStress = 10;
	[SerializeField, Min(0)] private int _speed;

	[Header("저항치")]
	[SerializeField, Min(0)] private float _bleedRes;
	[SerializeField, Min(0)] private float _blightRes;
	[SerializeField, Min(0)] private float _burnRes;
	[SerializeField, Min(0)] private float _diseaseRes;
	[SerializeField, Min(0)] private float _stunRes;
	[SerializeField, Min(0)] private float _moveRes;
	[SerializeField, Min(0)] private float _debuffRes;
	[SerializeField, Min(0)] private float _deathblowRes;

	[Header("이동 가능 거리 / 적일 경우 사이즈")]
	[SerializeField, Min(0)] private int _movement;

	#endregion

	#region 프로퍼티

	public string Name => _name;
	//
    public int MaxHP => _maxHP;
    public int Speed => _speed;
	//
    public float BleedRes => _bleedRes;
    public float BlightRes => _blightRes;
    public float BurnRes => _burnRes;
    public float DiseaseRes => _diseaseRes;
    public float StunRes => _stunRes;
    public float MoveRes => _moveRes;
    public float DebuffRes => _debuffRes;
    public float DeathblowRes => _deathblowRes;
	//
    public int Movement => _movement;

    #endregion

}
