// 짐칸 '사용' 버튼을 잠시 다른 화면이 맡는다 (예: 정비공 화면이 열려 있으면 마차 아이템을 '장착')
public interface IInventoryUseHandler
{
	// 버튼에 띄울 글자
	string UseLabel { get; }

	// 이 아이템을 맡는지 (안 맡으면 원래 '사용' 으로)
	bool Handles(CItemDataBase item);
	bool CanUse(int index);
	void Use(int index);
}
