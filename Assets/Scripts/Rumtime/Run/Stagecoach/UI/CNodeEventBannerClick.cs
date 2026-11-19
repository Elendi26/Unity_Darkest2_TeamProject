using UnityEngine;
using UnityEngine.EventSystems;

// 이벤트 이름 띠 바탕이 받은 포인터 이벤트를 CNodeEventBanner 로 넘긴다
public class CNodeEventBannerClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	// 이벤트를 넘길 배너
	public CNodeEventBanner Owner { get; set; }

	public void OnPointerClick(PointerEventData eventData)
	{
		if (Owner != null)
		{
			Owner.HandleClick(eventData);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (Owner != null)
		{
			Owner.HandleEnter();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (Owner != null)
		{
			Owner.HandleExit();
		}
	}
}
