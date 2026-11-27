using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 아이템을 놓는 곳 → 받을 수 있는지·받았을 때 할 일은 붙인 쪽이 알려 준다
public class CItemDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
	#region 내부 변수
	private Func<CItemDragPayload, bool> _canAccept;
	private Action<CItemDragPayload> _accept;
	// 받을 수 있는 아이템을 끌고 올라왔을 때 켜는 그림 (없어도 된다)
	private Graphic _highlight;
	#endregion

	public void Setup(Func<CItemDragPayload, bool> canAccept, Action<CItemDragPayload> accept, Graphic highlight = null)
	{
		_canAccept = canAccept;
		_accept = accept;
		_highlight = highlight;
		SetHighlight(false);
	}

	public void OnDrop(PointerEventData eventData)
	{
		CItemDragPayload payload = CItemDrag.Current;

		SetHighlight(false);

		if (payload == null || _canAccept == null || !_canAccept(payload))
		{
			return;
		}

		_accept?.Invoke(payload);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		CItemDragPayload payload = CItemDrag.Current;

		SetHighlight(payload != null && _canAccept != null && _canAccept(payload));
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		SetHighlight(false);
	}

	private void OnDisable()
	{
		SetHighlight(false);
	}

	private void SetHighlight(bool on)
	{
		if (_highlight != null)
		{
			_highlight.enabled = on;
		}
	}
}
