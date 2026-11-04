using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HeroStatus_UI : MonoBehaviour
{
	#region 인스펙터
	[Header("직업 이름")]
	[SerializeField] private TextMeshProUGUI _jobTitle;

	[Header("영웅 이름")]
	[SerializeField] private TextMeshProUGUI _heroName;

	[Header("영웅 한마디")]
	[SerializeField] private TextMeshProUGUI _heroSmalltalk;

	[Header("영웅 특성 텍스트 위치")]
	[SerializeField] private GameObject _heroSpec;

	[Header("특성 텍스트 프리팹")]
	[SerializeField] private GameObject _traitPrefab;



	[Header("위치 및 대상 표시할 이미지")]
	[SerializeField] private Image _indicator;

	[Header("캐릭터 모델 띄울 이미지 공간")]
	[SerializeField] private Transform _heroModelSpace;

	#endregion

	// 내부 변수

	// 현재 띄워져있는 히어로 모델링을 기억할 변수
	private GameObject _currentModel = null;


	/// <summary>
	/// 히어로 SO 파일 데이터를 참조하여 매니저에게 전달받은 SO데이터를 각 함수를 불러와 화면을 동기화시키는 함수
	/// </summary>
	public void UpdateStatus(HeroDataSO data)
	{
		if(data == null)
		{
			// 모든 텍스트 지우고, 모델 파괴
			_jobTitle.text = string.Empty;
            _heroName.text = string.Empty;
			_heroSmalltalk.text = string.Empty;
			_indicator = null;

			// 생성되어있는 프리팹 파괴
			for (int i = 0; i < _heroSpec.transform.childCount; i++)
			{
				Destroy(_heroSpec.transform.GetChild(i).gameObject);
			}

			// 3D 모델 파괴
			if(_currentModel != null)
			{
				Destroy( _currentModel.gameObject);
			}
			
		}

		else
		{
			TextUpdate(data);
			TraitsUpdate(data);
			ModelUpdate(data);
		}


    }

	/// <summary>
	/// 텍스트 연결해서 텍스트 갱신하기
	/// </summary>
	/// <param name="data"></param>
	private void TextUpdate(HeroDataSO data)
	{
		_jobTitle.text = data.JobTitle;
		_heroName.text = data.HeroName;
		_heroSmalltalk.text = data.HeroSmallTalk;
		_indicator.sprite = data.HeroRank;
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="data"></param>
	private void TraitsUpdate(HeroDataSO data)
	{
		for(int i = 0; i < _heroSpec.transform.childCount; i++)
		{
			Destroy(_heroSpec.transform.GetChild(i).gameObject);
		}

		foreach(var text in data.HeroSpecs)
		{
			GameObject trait = Instantiate(_traitPrefab, _heroSpec.transform);

            trait.GetComponent<TextMeshProUGUI>().text = text;
        }

		
	}
	

	/// <summary>
	/// 현재 모델링이 있는지 확인하고, 있으면 기존 데이터 지우고 생성, 없으면 지금 데이터로 바로 생성
	/// </summary>
	/// <param name="data"></param>
	private void ModelUpdate(HeroDataSO data)
	{
		if(_currentModel != null)
		{
			Destroy( _currentModel );

		}

		_currentModel = Instantiate(data.HeroModel, _heroModelSpace.position, _heroModelSpace.rotation);
		
	}
}
