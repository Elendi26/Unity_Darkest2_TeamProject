using UnityEngine;
using UnityEngine.UI;

// 노드 사이를 이동하는 동안 진행도 막대와 역마차 아이콘을 보여준다
public class CTravelProgressView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CanvasGroup _group;
	[SerializeField] private Image _fill;
	[SerializeField] private RectTransform _coach;
	[SerializeField] private Text _stateText;
	[SerializeField] private bool _hideWhenIdle = true;
	#endregion

	#region 내부 변수
	private CRoadTravel _travel;
	#endregion

	public void Bind(CRoadTravel travel)
	{
		UnBind();

		_travel = travel;

		if (_travel == null)
		{
			return;
		}

		_travel.OnStateChanged += HandleStateChanged;
		_travel.OnProgress += HandleProgress;

		HandleProgress(_travel.Progress);
		HandleStateChanged(ETravelState.None, _travel.State);
	}

	public void UnBind()
	{
		if (_travel != null)
		{
			_travel.OnStateChanged -= HandleStateChanged;
			_travel.OnProgress -= HandleProgress;
		}

		_travel = null;
	}

	private void OnDestroy()
	{
		UnBind();
	}

	private void HandleStateChanged(ETravelState prev, ETravelState next)
	{
		// 출발할 때 막대를 처음으로 되돌린다
		if (next == ETravelState.Departing)
		{
			HandleProgress(0f);
		}

		SetVisible(!_hideWhenIdle || next != ETravelState.None);

		if (_stateText != null)
		{
			_stateText.text = GetStateLabel(next);
		}
	}

	private void HandleProgress(float progress)
	{
		if (_fill != null)
		{
			_fill.fillAmount = progress;
		}

		if (_coach != null)
		{
			_coach.anchorMin = new Vector2(progress, 0.5f);
			_coach.anchorMax = new Vector2(progress, 0.5f);
			_coach.anchoredPosition = Vector2.zero;
		}
	}

	private void SetVisible(bool isVisible)
	{
		if (_group == null)
		{
			return;
		}

		_group.alpha = isVisible ? 1f : 0f;
		_group.blocksRaycasts = isVisible;
	}

	private string GetStateLabel(ETravelState state)
	{
		switch (state)
		{
			case ETravelState.Departing:
				return "출발";
			case ETravelState.Driving:
				return "이동 중";
			case ETravelState.RoadEvent:
				return "도로 이벤트";
			case ETravelState.Arriving:
				return "도착";
		}

		return string.Empty;
	}
}
