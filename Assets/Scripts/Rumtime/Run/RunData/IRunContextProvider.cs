// 런 데이터(CRunContext)를 넘겨주는 쪽이 구현한다
public interface IRunContextProvider
{
	// 진행 중인 런 데이터 (준비 전이면 null)
	CRunContext Context { get; }
}
