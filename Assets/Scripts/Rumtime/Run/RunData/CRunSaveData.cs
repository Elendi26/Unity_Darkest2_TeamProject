using System;
using System.Collections.Generic;

// 파일에 저장하는 런 기록 → SO 참조 대신 키(Id)·이름 문자열만 들고 있어 JSON 으로 그대로 저장된다
[Serializable]
public class CRunSaveData
{
	// 값 하나 (enum 은 순서가 바뀌어도 깨지지 않게 이름으로 저장)
	[Serializable]
	public struct SValueSave
	{
		public string Type;
		public int Current;
		public int Max;
	}

	// 짐칸 한 칸
	[Serializable]
	public struct SStackSave
	{
		public string ItemId;
		public int Count;
	}

	// 업그레이드로 더 연 슬롯
	[Serializable]
	public struct SSlotSave
	{
		public string Slot;
		public int Count;
	}

	// 영웅 장신구 한 칸
	[Serializable]
	public struct SHeroTrinketSave
	{
		public string HeroId;
		public int Slot;
		public string ItemId;
	}

	// 이어하기를 누르면 열 씬 경로
	public string ScenePath;
	// 자원 (유물·패물·횃불·바퀴·장갑)
	public List<SValueSave> Values = new List<SValueSave>();
	// 짐칸 (유물 칸 포함)
	public List<SStackSave> Stacks = new List<SStackSave>();
	// 장착한 마차 업그레이드 키
	public List<string> EquippedIds = new List<string>();
	// 업그레이드로 더 연 슬롯
	public List<SSlotSave> UnlockedSlots = new List<SSlotSave>();
	// 이번 런에서 교차로 출정으로 파티를 골랐는지 (주행 화면 아래 파티 줄을 띄운다)
	public bool PartyChosen;
	// 영웅이 끼운 장신구
	public List<SHeroTrinketSave> HeroTrinkets = new List<SHeroTrinketSave>();
}
