using UnityEngine;

// 왼쪽 위 마차 상태 (바퀴·장갑 칸)
//  ㄴ 금화·파벌은 짐칸 패널 아래, 횃불은 마차 횃불 툴팁에서 보여준다
public class CStagecoachHUD : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CRunValuePipView _wheelsView;
	[SerializeField] private CRunValuePipView _armorView;
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
	}

	private static void BindValue(CRunValuePipView view, CRunValue value)
	{
		if (view != null)
		{
			view.Bind(value);
		}
	}
}
