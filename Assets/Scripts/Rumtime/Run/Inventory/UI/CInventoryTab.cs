using System;
using UnityEngine.UI;

// 짐칸 분류 탭 하나 (버튼 + 아이콘 + 거를 분류)
[Serializable]
public class CInventoryTab
{
	public Button Button;
	public Image Icon;
	// 켜면 분류와 상관없이 모두 보여준다
	public bool IsAll;
	public EItemCategory Category;
	// 탭을 고르면 패널 제목에 띄울 이름
	public string Title = "모든 아이템";
}
