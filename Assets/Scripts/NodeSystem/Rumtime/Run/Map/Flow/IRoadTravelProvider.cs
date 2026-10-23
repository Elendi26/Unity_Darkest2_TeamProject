// 맵에 이동 담당(IRoadTravel)을 넘겨주는 쪽이 구현한다
public interface IRoadTravelProvider
{
	// 경로 이동을 맡을 객체 (준비 전이면 null)
	IRoadTravel Travel { get; }
}
