using System;
using System.Collections.Generic;
using UnityEngine;

// 상점 처리기(CShopNodeHandler)가 쓰는 화면
public interface IShopView
{
	// 화면이 열려 있는지
	bool IsOpen { get; }

	void SetLook(Sprite portrait, Sprite crest, Sprite banner);
	void Open(string title, string body, Action onLeave);
	void SetWallet(string text);
	void SetCards(IReadOnlyList<CShopCardInfo> cards, Action<int> onBuy);
	void Close();
}
