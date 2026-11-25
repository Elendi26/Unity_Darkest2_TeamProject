using System;

// 여관에 머무는 동안 → 아래 메뉴로 보급상·마차 정비공을 열고, 떠나기를 알린다
public class CInnSession
{
	#region 내부 변수
	private readonly CInnScene _inn;
	private readonly CNodeInstance _node;
	private readonly IShopView _shopView;
	private readonly CShopNodeHandler _shop;
	private readonly CGarageView _garageScreen;
	private readonly CShopData _garageData;
	private Action _onLeave;
	private Action _onSaveQuit;
	private bool _isShopRolled;
	#endregion

	// 보급상·정비공 화면 중 하나라도 열려 있는지
	public bool IsViewOpen => (_shopView != null && _shopView.IsOpen) || (_garageScreen != null && _garageScreen.IsOpen);

	public CInnSession(CInnScene inn, CNodeInstance node, CRunContext ctx, CStagecoach stagecoach, IShopView shopView, CShopData shopData, CShopData garageData)
	{
		_inn = inn;
		_node = node;
		_shopView = shopView;

		if (ctx == null)
		{
			CPrint.Warn("CInnSession : 런 데이터가 없어 보급상·정비공을 열 수 없습니다.");
			return;
		}

		if (shopData != null && shopView != null)
		{
			_shop = new CShopNodeHandler(ENodeType.Inn, shopData, shopView, ctx, stagecoach);
		}

		if (garageData == null)
		{
			return;
		}

		// 정비공은 상점이 아니다 → 수리와 마차 아이템 달기·떼기만 하는 전용 화면
		CGarageView garageView = inn.GarageView;

		if (garageView == null)
		{
			CPrint.Warn("CInnSession : 여관 씬에 CGarageView 가 없어 마차 정비공을 열 수 없습니다.");
			return;
		}

		garageView.SetStagecoach(stagecoach, ctx.RunData);
		_garageScreen = garageView;
		_garageData = garageData;
	}

	// onSaveQuit 을 비우면 저장하고 나가기 버튼은 꺼진다 (여관 단독 테스트 등)
	public void Begin(Action onLeave, Action onSaveQuit = null)
	{
		_onLeave = onLeave;
		_onSaveQuit = onSaveQuit;

		_inn.OnShopClicked += OpenShop;
		_inn.OnGarageClicked += OpenGarage;
		_inn.OnLeaveClicked += Leave;
		_inn.OnSaveQuitClicked += SaveQuit;

		_inn.SetAvailable(_shop != null, _garageScreen != null, _onSaveQuit != null);
		_inn.SetMenuVisible(true);
	}

	public void End()
	{
		_inn.OnShopClicked -= OpenShop;
		_inn.OnGarageClicked -= OpenGarage;
		_inn.OnLeaveClicked -= Leave;
		_inn.OnSaveQuitClicked -= SaveQuit;

		_onLeave = null;
		_onSaveQuit = null;
	}

	// 진열은 처음 열 때 한 번만 뽑는다 → 다시 열어도 같은 상품
	private void OpenShop()
	{
		Open(_shop, ref _isShopRolled);
	}

	private void OpenGarage()
	{
		if (_garageScreen == null || IsViewOpen)
		{
			return;
		}

		_inn.SetMenuVisible(false);

		_garageScreen.SetLook(_garageData.Portrait, _garageData.Crest, _garageData.Banner);
		_garageScreen.Open(_garageData.Title, _garageData.Body, CloseGarage);
	}

	private void CloseGarage()
	{
		_garageScreen.Close();
		BackToMenu();
	}

	private void Open(CShopNodeHandler handler, ref bool isRolled)
	{
		if (handler == null || IsViewOpen)
		{
			return;
		}

		_inn.SetMenuVisible(false);

		if (isRolled)
		{
			handler.Reopen(BackToMenu);
			return;
		}

		isRolled = true;
		handler.Enter(_node, BackToMenu);
	}

	private void BackToMenu()
	{
		_inn.SetMenuVisible(true);
	}

	private void Leave()
	{
		if (IsViewOpen)
		{
			return;
		}

		_onLeave?.Invoke();
	}

	// 저장·씬 이동은 여관 처리기에 맡긴다
	private void SaveQuit()
	{
		if (IsViewOpen)
		{
			return;
		}

		_onSaveQuit?.Invoke();
	}
}
