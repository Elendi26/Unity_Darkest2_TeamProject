using UnityEngine;

// 타이틀에서 모험을 시작한다 → 새 고해는 늘 튜토리얼(출발 → 교차로)부터. 이어하기는 프로필에 저장된 런으로
public class CTitleRunStarter : MonoBehaviour
{
	#region 인스펙터
	// 튜토리얼 게임 모드 ID (CGameManager 에는 튜토리얼 ID 가 없어 여기서 정한다)
	[SerializeField] private string _tutorialModeId = "Tutorial";
	// 이어하기 → 저장된 씬이 이 경로면 튜토리얼 모드로, 아니면 Node 모드로 연다
	[SerializeField] private string _tutorialScenePath = "Assets/Scenes/Tutorial/TutorialScene.unity";
	[SerializeField] private bool _logState = true;
	#endregion

	// 새 고해 버튼
	public void OnClickNewRun()
	{
		CGameManager gameManager = GetGameManager();

		if (gameManager == null)
		{
			return;
		}

		// 새 고해는 늘 출발 길(튜토리얼 씬)부터 → 교차로에서 파티를 꾸려야 전투를 치를 수 있다
		int slot = CTutorialProgress.CurrentSlot;
		string modeId = GetTutorialModeId(gameManager);

		// 새 모험은 지난 런을 이어받지 않는다
		CRunCarry.Take();
		CRunProfileSave.ClearRun(slot);
		CTutorialTipDirector.ResetSeen();

		if (_logState)
		{
			CPrint.Log($"CTitleRunStarter : 슬롯 {slot} 새 고해 → {modeId}");
		}

		gameManager.ChangeGameMode(modeId);
	}

	// 고해 계속하기 버튼
	public void OnClickContinue()
	{
		CGameManager gameManager = GetGameManager();

		if (gameManager == null)
		{
			return;
		}

		int slot = CTutorialProgress.CurrentSlot;
		CRunSaveData save = CRunProfileSave.LoadRun(slot);

		if (save == null)
		{
			CPrint.Warn($"CTitleRunStarter : 슬롯 {slot} 에 이어할 런이 없습니다.");
			return;
		}

		if (!CRunCarry.Restore(save))
		{
			return;
		}

		string modeId = save.ScenePath == _tutorialScenePath ? GetTutorialModeId(gameManager) : gameManager.NodeModeId;

		if (_logState)
		{
			CPrint.Log($"CTitleRunStarter : 슬롯 {slot} 이어하기 → {modeId}");
		}

		gameManager.ChangeGameMode(modeId);
	}

	private static CGameManager GetGameManager()
	{
		CGameManager gameManager = CGameManager.Instance;
		CAddressableSceneManager sceneManager = CAddressableSceneManager.Instance;

		if (gameManager == null || sceneManager == null)
		{
			CPrint.Warn("CTitleRunStarter : 게임 매니저나 씬 매니저가 없어 시작하지 않습니다.");
			return null;
		}

		if (sceneManager.IsTransitioning)
		{
			CPrint.Warn("CTitleRunStarter : 씬 전환 중이라 버튼을 무시합니다.");
			return null;
		}

		return gameManager;
	}

	// 튜토리얼 모드가 없으면 바로 주행으로
	private string GetTutorialModeId(CGameManager gameManager)
	{
		if (gameManager.GetGameModeData(_tutorialModeId) != null)
		{
			return _tutorialModeId;
		}

		CPrint.Warn($"CTitleRunStarter : 튜토리얼 모드({_tutorialModeId})가 없어 바로 주행을 시작합니다.");
		return gameManager.NodeModeId;
	}
}
