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
	private readonly List<string> _labels = new List<string>();
	private readonly List<bool> _interactable = new List<bool>();
	private Action _onComplete;
	#endregion

	public ENodeType Type => _type;

	public CNodeEventHandler(ENodeType type, CNodeEventData nodeEvent, CRoadEventPopup popup, CRunContext ctx)
	{
		_type = type;
		_event = nodeEvent;
		_popup = popup;
		_ctx = ctx;
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

			string label = GetLabelWithEffect(option);

			_labels.Add(option.Cost > 0 ? $"{label} ({CRunValueTable.GetName(option.CostType)} {option.Cost})" : label);
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

	// 선택지 이름 뒤에 효과를 붙인다 (예: 치료받기 · 체력 20% 회복)
	private static string GetLabelWithEffect(CNodeEventOption option)
	{
		if (option.Effects == null || option.Effects.Count == 0)
		{
			return option.Label;
		}

		List<string> texts = new List<string>();

		for (int i = 0; i < option.Effects.Count; i++)
		{
			string text = option.Effects[i] is IRunEffectText effectText ? effectText.GetText() : null;

			if (!string.IsNullOrEmpty(text))
			{
				texts.Add(text);
			}
		}

		return texts.Count > 0 ? $"{option.Label} · {string.Join(", ", texts)}" : option.Label;
	}

	// 비용이 되어야 고를 수 있다
	private bool CanChoose(CNodeEventOption option)
	{
		return option.Cost <= 0 || _ctx.RunData.Has(option.CostType, option.Cost);
	}

}
