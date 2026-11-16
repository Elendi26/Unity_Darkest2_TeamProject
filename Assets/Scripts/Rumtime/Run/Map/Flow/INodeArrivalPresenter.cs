using System;

// 노드에 도착한 뒤 이벤트에 들어가기 전 연출 (카메라가 건물을 비추는 등)
//  ㄴ 연출이 끝나면 onDone 을 불러야 노드 이벤트가 시작된다
public interface INodeArrivalPresenter
{
	void Present(CNodeInstance node, Action onDone);
}
