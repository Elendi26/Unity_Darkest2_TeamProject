using UnityEngine;

public class CStagecoachHUD : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CRunValuePipView _wheelsView;
	[SerializeField] private CRunValuePipView _armorView;
	[SerializeField] private CRunValueView _torchView;
	[SerializeField] private CRunValueView _goldView;
	[SerializeField] private CRunValueView _factionView;
	[SerializeField] private CTravelProgressView _travelView;
	#endregion

	private void Start()
	{
		if (_controller == null)
		{
			_controller = FindFirstObjectByType<CStagecoachController>();
		}

		if (_controller == null)
		{
			CPrint.Warn("CStagecoachHUD : 씬에 CStagecoachController 가 없습니다.");
			return;
		}

		if (_controller.RunData == null)
		{
			CPrint.Warn("CStagecoachHUD : 런 데이터가 없습니다. 컨트롤러의 Config 를 확인하세요.");
			return;
		}

		Bind();
	}

	private void Bind()
	{
		CRunData runData = _controller.RunData;

		BindValue(_wheelsView, runData.Get(ERunValue.Wheels));
		BindValue(_armorView, runData.Get(ERunValue.Armor));
		BindValue(_torchView, runData.Get(ERunValue.Torch));
		BindValue(_goldView, runData.Get(ERunValue.Gold));
		BindValue(_factionView, runData.Get(ERunValue.Faction));

		if (_travelView != null)
		{
			_travelView.Bind(_controller.Travel);
		}
	}

	private void BindValue(CRunValuePipView view, CRunValue value)
	{
		if (view != null)
		{
			view.Bind(value);
		}
	}

	private void BindValue(CRunValueView view, CRunValue value)
	{
		if (view != null)
		{
			view.Bind(value);
		}
	}
}
