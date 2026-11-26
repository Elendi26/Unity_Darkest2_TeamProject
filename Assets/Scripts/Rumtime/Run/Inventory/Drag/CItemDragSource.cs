using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 끌기 시작하는 곳 (짐칸 칸·정비공 칸·장신구 칸) → 끌 때 무엇을 드는지는 붙인 쪽이 알려 준다
public class CItemDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	#region 내부 변수
	// 지금 이 칸에서 들 아이템 (null 이면 끌지 않는다)
	private Func<CItemDragPayload> _getPayload;
	private bool _isDragging;
	#endregion

	public void Setup(Func<CItemDragPayload> getPayload)
	{
		_getPayload = getPayload;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		CItemDragPayload payload = eventData.button == PointerEventData.InputButton.Left ? _getPayload?.Invoke() : null;

		// 들 것이 없으면 끌기를 넘긴다 → 부모 스크롤 등이 받게
		if (payload == null || payload.Item == null)
		{
			GameObject parent = transform.parent != null ? ExecuteEvents.GetEventHandler<IDragHandler>(transform.parent.gameObject) : null;

			eventData.pointerDrag = parent;

			if (parent != null)
			{
				ExecuteEvents.Execute(parent, eventData, ExecuteEvents.beginDragHandler);
			}

			return;
		}

		_isDragging = true;
		CItemDrag.Begin(payload, payload.Item.Icon, eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (_isDragging)
		{
			CItemDrag.Move(eventData);
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!_isDragging)
		{
			return;
		}

		_isDragging = false;
		CItemDrag.End();
	}

	// 끄는 도중 칸이 꺼지면 (창이 닫힘 등) 아이콘을 남기지 않는다
	private void OnDisable()
	{
		if (_isDragging)
		{
			_isDragging = false;
			CItemDrag.End();
		}
	}
}
