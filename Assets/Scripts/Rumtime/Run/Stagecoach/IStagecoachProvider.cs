// 역마차(CStagecoach)를 넘겨주는 쪽이 구현한다
public interface IStagecoachProvider
{
	// 진행 중인 역마차 (준비 전이면 null)
	CStagecoach Stagecoach { get; }
}
