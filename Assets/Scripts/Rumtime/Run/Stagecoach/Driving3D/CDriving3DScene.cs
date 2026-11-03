using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CDriving3DScene : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CRoadPath _path;
	[SerializeField] private CMapController _map;
	[SerializeField] private float _forkLead = 40f;
	[SerializeField] private float _arriveStraight = 30f;
	[SerializeField] private float _eventStraightBefore = 30f;
	[SerializeField] private float _eventStraightAfter = 15f;
	[SerializeField] private CForkSignView _forkSigns;
	[SerializeField] private CCoach3DView _coach;
	[SerializeField] private CChaseCamera _chaseCamera;
	[SerializeField] private float _startBackOffset = 10f;
	[SerializeField] private bool _keepOnRoad = true;
	[SerializeField] private float _coachHalfWidth = 1.3f;
	[SerializeField] private float _fallLimit = 15f;
	[SerializeField] private Light _torchLight;
	[SerializeField] private InputAction _driveAction = new InputAction("Drive", InputActionType.Value);
	[SerializeField] private InputAction _brakeAction = new InputAction("Brake", InputActionType.Button, "<Keyboard>/space");
	[SerializeField] private Vector2 _torchIntensity = new Vector2(0.4f, 3f);
	[SerializeField] private Vector2 _torchRange = new Vector2(6f, 16f);
	[SerializeField] private bool _debugAlwaysDrive = false;
	[SerializeField] private bool _logInput = false;
	#endregion

	#region 내부 변수
	private CRoadTravel _travel;
	private CRunValue _torch;
	private CCoachPhysics _physics;
	private float _farthestArc;
	private CMapRunner _runner;
	private readonly List<CNodeLink> _forkLinks = new List<CNodeLink>();
	private int _chosenBranch = -1;
	#endregion

	private void Awake()
	{
		if (_coach == null || _startBackOffset == 0f)
		{
			return;
		}

		Vector3 back = Vector3.ProjectOnPlane(-_coach.transform.forward, Vector3.up).normalized;

		_coach.transform.position += back * _startBackOffset;
	}

	private void OnEnable()
	{
		SetupDefaultBindings();
		_driveAction.Enable();
		_brakeAction.Enable();
	}

	private void OnDisable()
	{
		_driveAction.Disable();
		_brakeAction.Disable();
	}

	private void Start()
	{
		SetupDriving();

		if (_controller == null)
		{
			_controller = FindFirstObjectByType<CStagecoachController>();
		}

		if (_controller == null || _controller.Travel == null)
		{
			CPrint.Warn("CDriving3DScene : 주행 정보가 없습니다. Debug Always Drive 로만 움직입니다.");
			return;
		}

		_travel = _controller.Travel;
		_travel.OnRoadEventStarted += HandleRoadEventStarted;

		_torch = _controller.RunData.Get(ERunValue.Torch);

		if (_torch != null)
		{
			_torch.OnChanged += HandleTorchChanged;
			ApplyTorch();
		}

		SetupMap();
	}

	private void OnDestroy()
	{
		UnbindRunner();

		if (_map != null)
		{
			_map.OnRunnerCreated -= BindRunner;
		}

		if (_path != null)
		{
			_path.OnBranchChosen -= HandleBranchChosen;
		}

		if (_travel != null)
		{
			_travel.OnRoadEventStarted -= HandleRoadEventStarted;
		}

		if (_torch != null)
		{
			_torch.OnChanged -= HandleTorchChanged;
		}
	}

	private void Update()
	{
		if (_physics == null)
		{
			return;
		}

		CheckFallen();
		TickDrive();
	}

	private void FixedUpdate()
	{
		if (!_keepOnRoad || _physics == null || _path == null)
		{
			return;
		}

		KeepOnPath(_coach.transform.position);
	}

	// 가장 가까운 길 가운데 선 기준으로 좌우를 잰다
	private void KeepOnPath(Vector3 position)
	{
		if (!_path.TryProject(position, out _, out float lateral, out Vector3 right))
		{
			return;
		}

		float limit = Mathf.Max(0f, _path.RoadHalfWidth - _coachHalfWidth);

		_physics.KeepInside(right, lateral, limit);

		CHorseTeam team = _physics.Team;

		if (team != null && _path.TryProject(team.transform.position, out _, out float teamLateral, out Vector3 teamRight))
		{
			team.KeepInside(teamRight, teamLateral, limit);
		}
	}

	private float GetTravelled()
	{
		Vector3 position = _coach.transform.position;

		if (_path != null && _path.TryProject(position, out float arc, out _, out _))
		{
			return arc;
		}

		return position.z;
	}

	private void SetupDriving()
	{
		if (_coach == null)
		{
			CPrint.Warn("CDriving3DScene : Coach 가 비어 있습니다.");
			return;
		}

		_physics = _coach.GetComponent<CCoachPhysics>();

		if (_physics == null)
		{
			_physics = _coach.gameObject.AddComponent<CCoachPhysics>();
			CPrint.Log("CDriving3DScene : 마차에 CCoachPhysics 를 붙였습니다.");
		}

		if (_path != null)
		{
			_path.SetFollow(_coach.transform);
		}

		if (_chaseCamera == null && Camera.main != null)
		{
			_chaseCamera = Camera.main.GetComponent<CChaseCamera>();

			if (_chaseCamera == null)
			{
				_chaseCamera = Camera.main.gameObject.AddComponent<CChaseCamera>();
			}
		}

		if (_chaseCamera != null)
		{
			_chaseCamera.SetTarget(_coach.transform);
		}

		_farthestArc = GetTravelled();

		// 씬에 놓인 마차 위치와 상관없이 길 가운데 위에 다시 올린다
		if (_path != null)
		{
			PlaceCoachOnPath(_farthestArc);
		}
	}

	private void PlaceCoachOnPath(float arc)
	{
		if (_physics == null || !_path.TryGetPoint(arc, out Vector3 position, out Vector3 forward))
		{
			return;
		}

		_physics.Teleport(position + Vector3.up * 2f, Quaternion.LookRotation(forward, Vector3.up));

		if (_logInput)
		{
			CPrint.Log($"CDriving3DScene : 마차를 길 위 {arc:F1}m 지점에 올림");
		}
	}

	// 길 아래로 떨어졌으면 마지막으로 간 지점에 다시 올린다
	private void CheckFallen()
	{
		if (_path == null)
		{
			return;
		}

		if (_coach.transform.position.y > _path.transform.position.y - _fallLimit)
		{
			return;
		}

		CPrint.Warn("CDriving3DScene : 마차가 길 아래로 떨어져 다시 올립니다.");
		PlaceCoachOnPath(_farthestArc);
	}

	private void TickDrive()
	{
		bool canDrive = CanDrive();
		Vector2 input = canDrive ? _driveAction.ReadValue<Vector2>() : Vector2.zero;
		bool brake = !canDrive || _brakeAction.IsPressed();

		if (_logInput && input != Vector2.zero)
		{
			CPrint.Log($"CDriving3DScene : 입력 {input}, 브레이크 {brake}");
		}

		_physics.Drive(input.y, input.x, brake);

		float speed = _physics.ForwardSpeed;

		_coach.Drive(Mathf.Abs(speed) / Mathf.Max(0.1f, _physics.MaxSpeed), speed * Time.deltaTime);

		float travelled = GetTravelled();

		if (travelled > _farthestArc)
		{
			if (_travel != null)
			{
				_travel.Advance(travelled - _farthestArc);
			}

			_farthestArc = travelled;
		}
	}

	#region 갈림길
	// 맵 런너와 이어서, 길을 고를 때가 되면 앞에 갈림길을 놓는다
	private void SetupMap()
	{
		if (_path == null)
		{
			return;
		}

		if (_map == null)
		{
			_map = FindFirstObjectByType<CMapController>();
		}

		if (_map == null)
		{
			CPrint.Warn("CDriving3DScene : CMapController 가 없어 갈림길을 만들지 않습니다.");
			return;
		}

		if (_forkSigns == null)
		{
			_forkSigns = GetComponent<CForkSignView>();

			if (_forkSigns == null)
			{
				_forkSigns = gameObject.AddComponent<CForkSignView>();
			}
		}

		_path.OnBranchChosen += HandleBranchChosen;
		_map.OnRunnerCreated += BindRunner;

		// 맵이 먼저 Start 했다면 런너가 이미 있다
		if (_map.Runner != null)
		{
			BindRunner(_map.Runner);
		}
	}

	private void BindRunner(CMapRunner runner)
	{
		if (runner == null || runner == _runner)
		{
			return;
		}

		UnbindRunner();
		_runner = runner;
		_runner.OnStateChanged += HandleMapStateChanged;

		if (_runner.State == EMapState.SelectingPath)
		{
			PrepareFork();
		}
	}

	private void UnbindRunner()
	{
		if (_runner == null)
		{
			return;
		}

		_runner.OnStateChanged -= HandleMapStateChanged;
		_runner = null;
	}

	private void HandleMapStateChanged(EMapState prev, EMapState next)
	{
		if (next == EMapState.SelectingPath)
		{
			PrepareFork();
		}
	}

	// 지금 노드에서 갈 수 있는 경로를 맵 왼쪽부터 늘어놓는다
	private void PrepareFork()
	{
		_forkLinks.Clear();

		if (_runner.Current == null)
		{
			return;
		}

		_forkLinks.AddRange(_runner.Current.OutLinks);
		_forkLinks.Sort((a, b) => a.To.Lane.CompareTo(b.To.Lane));

		if (_forkLinks.Count == 0)
		{
			return;
		}

		if (_forkLinks.Count == 1)
		{
			SelectLink(_forkLinks[0]);
			return;
		}

		if (_chosenBranch >= 0)
		{
			int branch = _chosenBranch;

			_chosenBranch = -1;
			HandleBranchChosen(branch);
			return;
		}

		if (!_path.HasFork)
		{
			float travelled = GetTravelled();

			_path.RequestStraight(travelled, travelled + _forkLead);
			_path.RequestFork(travelled + _forkLead, _forkLinks.Count);
		}

		_forkSigns.Show(_path, _forkLinks);
	}

	private void HandleBranchChosen(int branch)
	{
		if (_forkSigns != null)
		{
			_forkSigns.Hide();
		}

		if (_runner == null)
		{
			return;
		}

		if (_runner.State != EMapState.SelectingPath)
		{
			_chosenBranch = branch;
			return;
		}

		if (branch < 0 || branch >= _forkLinks.Count)
		{
			CPrint.Warn($"CDriving3DScene : {branch}번 갈래에 맞는 경로가 없습니다.");
			return;
		}

		SelectLink(_forkLinks[branch]);
	}

	private void SelectLink(CNodeLink link)
	{
		if (!_runner.TrySelectPath(link))
		{
			CPrint.Warn($"CDriving3DScene : {link.From.Id} → {link.To.Id} 경로를 고를 수 없습니다.");
			return;
		}

		CPrint.Log($"CDriving3DScene : {link.From.Id} → {link.To.Id} 경로로 출발");
		PlanNextFork(link.To);
	}

	private void PlanNextFork(CNodeInstance node)
	{
		CStagecoachConfig config = _controller != null ? _controller.Config : null;

		if (node == null || config == null)
		{
			return;
		}

		float arrive = _farthestArc + config.TravelDistance;

		_path.RequestStraight(arrive - _arriveStraight, arrive + _forkLead);

		// 도로 이벤트 위치도 출발할 때 정해진다
		if (_travel != null)
		{
			for (int i = 0; i < _travel.EventCount; i++)
			{
				float eventArc = _farthestArc + _travel.GetEventPosition(i) * config.TravelDistance;

				_path.RequestStraight(eventArc - _eventStraightBefore, eventArc + _eventStraightAfter);
			}
		}

		if (node.OutLinks.Count < 2)
		{
			return;
		}

		_path.RequestFork(arrive + _forkLead, node.OutLinks.Count);
	}
	#endregion

	private bool CanDrive()
	{
		if (_debugAlwaysDrive)
		{
			return true;
		}

		// 길을 고르는 중에도 달린다 → 앞에 놓인 갈림길로 들어가 고른다
		if (_runner != null && _runner.State == EMapState.SelectingPath)
		{
			return true;
		}

		return _travel != null && _travel.State == ETravelState.Driving;
	}

	// 인스펙터에서 바인딩을 따로 안 넣었으면 기본 조작키를 넣는다
	private void SetupDefaultBindings()
	{
		if (_driveAction.bindings.Count > 0)
		{
			return;
		}

		_driveAction.AddCompositeBinding("2DVector")
			.With("Up", "<Keyboard>/w")
			.With("Down", "<Keyboard>/s")
			.With("Left", "<Keyboard>/a")
			.With("Right", "<Keyboard>/d");

		_driveAction.AddCompositeBinding("2DVector")
			.With("Up", "<Keyboard>/upArrow")
			.With("Down", "<Keyboard>/downArrow")
			.With("Left", "<Keyboard>/leftArrow")
			.With("Right", "<Keyboard>/rightArrow");

		_driveAction.AddBinding("<Gamepad>/leftStick");
	}

	private void HandleRoadEventStarted(CRoadEventData data)
	{
		if (_coach != null)
		{
			_coach.Bump();
		}
	}

	private void HandleTorchChanged(ERunValue type, int prev, int current)
	{
		ApplyTorch();
	}

	// 횃불이 줄수록 마차 주변이 어두워진다
	private void ApplyTorch()
	{
		if (_torchLight == null || _torch == null)
		{
			return;
		}

		float ratio = _torch.Ratio;

		_torchLight.intensity = Mathf.Lerp(_torchIntensity.x, _torchIntensity.y, ratio);
		_torchLight.range = Mathf.Lerp(_torchRange.x, _torchRange.y, ratio);
	}
}
