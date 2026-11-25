using UnityEngine;
using UnityEngine.SceneManagement;

public class CInnTestRunner : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CInnScene _inn;
	[SerializeField] private CStagecoachConfig _config;
	[SerializeField] private CShopData _shopData;
	[SerializeField] private CShopData _garageData;
	// 테스트용 상점 화면과 그 캔버스 (주행 씬에서 불러오면 지운다)
	[SerializeField] private CShopView _testShopView;
	[SerializeField] private GameObject _testUiRoot;
	[Header("테스트 런 데이터")]
	[SerializeField] private int _startGold = 300;
	[SerializeField] private int _startFaction = 40;
	[SerializeField] private int _startTorch = 60;
	[SerializeField] private int _maxTorch = 100;
	// 수리를 시험할 수 있게 처음부터 깎아 둔다
	[SerializeField] private int _wheelDamage = 1;
	[SerializeField] private int _armorDamage = 1;
	[SerializeField] private CItemDataBase[] _startItems;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private CRunContext _ctx;
	private CStagecoach _stagecoach;
	private CInnSession _session;
	#endregion

	private void Start()
	{
		// 주행 씬 위에 불러온 경우 → 여관 처리기가 맡는다
		if (SceneManager.sceneCount > 1)
		{
			if (_testUiRoot != null)
			{
				Destroy(_testUiRoot);
			}

			enabled = false;
			return;
		}

		if (_inn == null)
		{
			_inn = FindFirstObjectByType<CInnScene>();
		}

		if (_inn == null || _config == null)
		{
			CPrint.Warn("CInnTestRunner : 여관 씬(CInnScene)이나 역마차 설정이 없어 테스트를 시작할 수 없습니다.");
			return;
		}

		CreateTestRun();

		_session = new CInnSession(_inn, null, _ctx, _stagecoach, _testShopView, _shopData, _garageData);
		_session.Begin(Leave);

		if (_logState)
		{
			CPrint.Log($"CInnTestRunner : 여관 단독 테스트 시작 (유물 {_startGold}, 패물 {_startFaction}, 바퀴 -{_wheelDamage}, 장갑 -{_armorDamage})");
		}
	}

	private void OnDestroy()
	{
		if (_session != null)
		{
			_session.End();
			_session = null;
		}
	}

	// CStagecoachController 와 같은 순서로 런 데이터를 만든다
	private void CreateTestRun()
	{
		CRunData runData = new CRunData();

		runData.Register(ERunValue.Gold, _startGold, int.MaxValue);
		runData.Register(ERunValue.Faction, _startFaction, int.MaxValue);
		runData.Register(ERunValue.Torch, _startTorch, _maxTorch);

		_stagecoach = new CStagecoach(_config, runData);
		runData.Add(ERunValue.Wheels, -_wheelDamage);
		runData.Add(ERunValue.Armor, -_armorDamage);

		if (_startItems != null)
		{
			for (int i = 0; i < _startItems.Length; i++)
			{
				if (_startItems[i] != null)
				{
					_stagecoach.Inventory.Add(_startItems[i], 1);
				}
			}
		}

		_ctx = new CRunContext
		{
			RunData = runData,
			Random = new System.Random(),
		};
	}

	// 테스트에서는 내릴 씬이 없다 → 메뉴를 다시 띄운다
	private void Leave()
	{
		if (_logState)
		{
			CPrint.Log("CInnTestRunner : 떠나기 (단독 테스트라 여관에 그대로 남습니다)");
		}

		_inn.SetMenuVisible(true);
	}
}
