using System;

// 노드에 도착한 뒤 이벤트에 들어가기 전 연출 (카메라가 건물을 비추는 등)
public interface INodeArrivalPresenter
{
	void Present(CNodeInstance node, Action onDone);
}
