using UnityEngine;

public class CNodeLandmarkTile : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private float _roadGap = 1f;
	[SerializeField] private float _sink = 0.04f;
	[SerializeField] private Transform _cameraPoint;
	[SerializeField] private Transform _lookPoint;
	[SerializeField] private bool _placeOnLeft = true;
	[SerializeField] private bool _hideDecorGround = true;
	[SerializeField] private Transform _boundsRoot;
	[SerializeField, Range(0.2f, 1f)] private float _clearScale = 0.65f;
	[SerializeField] private Transform _clearRoot;
	[SerializeField] private float _clearMargin = 1f;
	[SerializeField] private float _focusDistanceScale = 1f;

	[Header("장면 속 길을 진행 길에 겹치기 (원작 노드는 길이 장면 안을 지나간다)")]
	[SerializeField] private bool _alignToRoad = false;
	[SerializeField] private float _roadX = 0f;
	[SerializeField] private float _stopZ = 0f;
	[SerializeField] private float _roadY = 0f;
	[SerializeField] private Transform _roadBoundsRoot;
	#endregion

	public float RoadGap => _roadGap;
	public float Sink => _sink;
	public bool HasCustomCamera => _cameraPoint != null && _lookPoint != null;
	public Transform CameraPoint => _cameraPoint;
	public Transform LookPoint => _lookPoint;
	public bool PlaceOnLeft => _placeOnLeft;
	public bool HideDecorGround => _hideDecorGround;
	public Transform BoundsRoot => _boundsRoot != null ? _boundsRoot : transform;
	public float ClearScale => _clearScale;
	public Transform ClearRoot => _clearRoot;
	public float ClearMargin => _clearMargin;
	public float FocusDistanceScale => _focusDistanceScale;
	public bool AlignToRoad => _alignToRoad;
	public float RoadX => _roadX;
	public float StopZ => _stopZ;
	public float RoadY => _roadY;
	public Transform RoadBoundsRoot => _roadBoundsRoot != null ? _roadBoundsRoot : BoundsRoot;
}
