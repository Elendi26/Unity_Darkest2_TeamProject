using System;
using System.Collections.Generic;

// 노드에 도착하면 팝업으로 선택지를 보여 주고, 고른 선택지의 비용을 내고 효과를 적용한다
public class CNodeEventHandler : INodeHandler
{
	#region 내부 변수
	private readonly ENodeType _type;
	private readonly CNodeEventData _event;
	private readonly CRoadEventPopup _popup;
	private readonly CRunContext _ctx;
	private readonly CStagecoach _stagecoach;
	private readonly List<string> _labels = new List<string>();
	private readonly List<bool> _interactable = new List<bool>();
	private Action _onComplete;
	#endregion

	public ENodeType Type => _type;

	public CNodeEventHandler(ENodeType type, CNodeEventData nodeEvent, CRoadEventPopup popup, CRunContext ctx, CStagecoach stagecoach)
	{
		_type = type;
		_event = nodeEvent;
		_popup = popup;
		_ctx = ctx;
		_stagecoach = stagecoach;
	}

	public void Enter(CNodeInstance node, Action onComplete)
	{
		_onComplete = onComplete;

		CPrint.Log($"{_type} 노드 이벤트 : {_event.Title} (ID {node.Id}, 행 {node.Row})");
		ShowOptions();
	}

	private void ShowOptions()
	{
		_labels.Clear();
		_interactable.Clear();

		IReadOnlyList<CNodeEventOption> options = _event.Options;
		int count = options != null ? options.Count : 0;

		for (int i = 0; i < count; i++)
		{
			CNodeEventOption option = options[i];

			if (option == null)
			{
				_labels.Add("-");
				_interactable.Add(false);
				continue;
			}

			_labels.Add(option.Cost > 0 ? $"{option.Label} ({GetValueName(option.CostType)} {option.Cost})" : option.Label);
			_interactable.Add(CanChoose(option));
		}

		// 나가기는 항상 맨 아래
		_labels.Add(_event.LeaveLabel);
		_interactable.Add(true);

		_popup.Show(_event.Title, _event.Body, _labels, _interactable, Choose);
	}

	private void Choose(int index)
	{
		IReadOnlyList<CNodeEventOption> options = _event.Options;
		int count = options != null ? options.Count : 0;

		if (index < 0 || index >= count)
		{
			Finish();
			return;
		}

		CNodeEventOption option = options[index];

		if (option == null || !CanChoose(option))
		{
			ShowOptions();
			return;
		}

		if (option.Cost > 0)
		{
			_ctx.RunData.Add(option.CostType, -option.Cost);
		}

		if (option.Upgrade != null)
		{
			_stagecoach.TryEquip(option.Upgrade);
		}

		CRunEffectUtil.ApplyAll(option.Effects, _ctx);
		CPrint.Log($"{_type} 노드 이벤트 → {option.Label}");

		// 닫히지 않는 선택지는 남은 자원으로 버튼을 다시 그린다
		if (!option.CloseAfter)
		{
			ShowOptions();
			return;
		}

		Finish();
	}

	private void Finish()
	{
		_popup.Close();

		// 콜백 전에 비운다
		Action onComplete = _onComplete;

		_onComplete = null;
		onComplete?.Invoke();
	}

	// 비용이 되고, 업그레이드가 있으면 달 수 있어야 고를 수 있다
	private bool CanChoose(CNodeEventOption option)
	{
		if (option.Cost > 0 && !_ctx.RunData.Has(option.CostType, option.Cost))
		{
			return false;
		}

		return option.Upgrade == null || (_stagecoach != null && _stagecoach.CanEquip(option.Upgrade));
	}

	private static string GetValueName(ERunValue type)
	{
		switch (type)
		{
			case ERunValue.Gold:
				return "골드";
			case ERunValue.Faction:
				return "문장";
			case ERunValue.Torch:
				return "횃불";
			case ERunValue.Wheels:
				return "바퀴";
			case ERunValue.Armor:
				return "장갑";
		}

		return type.ToString();
	}
}
