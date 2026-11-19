using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 미니맵 Viewport 에서 마우스 휠을 받아 CMapView 로 넘긴다
public class CMapWheelReceiver : MonoBehaviour, IScrollHandler
{
	// 휠을 굴렸을 때 부를 처리 (CMapView 가 넣는다)
	public Action<PointerEventData> OnWheel { get; set; }

	public void OnScroll(PointerEventData eventData)
	{
		OnWheel?.Invoke(eventData);
	}
}
