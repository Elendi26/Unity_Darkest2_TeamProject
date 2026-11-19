using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CDriving3DScene : MonoBehaviour, INodeArrivalPresenter
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CRoadPath _path;
	[SerializeField] private CMapController _map;
	[SerializeField] private float _forkLead = 40f;
	[SerializeField] private float _arriveStraight = 30f;
	[SerializeField] private float _arriveBrakeDecel = 3.5f;
	[SerializeField] private float _arriveCreepSpeed = 1.5f;
	[SerializeField] private float _eventStraightBefore = 30f;
	[SerializeField] private float _eventStraightAfter = 15f;
	[SerializeField] private CForkSignView _forkSigns;
	[SerializeField] private CNodeEventBanner _eventBanner;
	[SerializeField] private CRoadPropSpawner _roadProps;
	[SerializeField] private CRoadHazardView _hazards;
	[SerializeField] private float _bumpJolt = 0.9f;
	[SerializeField] private float _bumpInterval = 0.2f;
	[SerializeField] private CCoach3DView _coach;
	[SerializeField] private CChaseCamera _chaseCamera;
	[SerializeField] private float _startBackOffset = 10f;
	[SerializeField] private bool _keepOnRoad = true;
	[SerializeField] private float _coachHalfWidth = 1.3f;
	[SerializeField] private float _fallLimit = 15f;
	[SerializeField] private Light _torchLight;
	[SerializeField] private InputAction _driveAction = new InputAction("Drive", InputActionType.Value);
	[SerializeField] private InputAction _brakeAction = new InputAction("Brake", InputActionType.Button, "<Keyboard>/space");
	[SerializeField] private CCoachTorchView _torchView;
	[SerializeField] private CInventoryView _inventoryView;
	[SerializeField] private CNodeLandmarkSpawner _landmarks;
	[SerializeField] private float _arrivalFocusLead = 15f;
	[SerializeField] private float _arrivalFocusTime = 1.2f;
	[SerializeField] private float _arrivalFocusRange = 60f;
	[SerializeField, Range(0f, 1f)] private float _arriveAutoThrottle = 0.7f;
	[SerializeField] private float _arriveSteerLook = 12f;
	[SerializeField] private bool _stopWhileInventoryOpen = false;

	[Header("갈림길 꾹 눌러 고르기")]
	[SerializeField] private float _forkStopMargin = 3f;
	[SerializeField] private float _forkFollowLook = 9f;
	[SerializeField] private float _forkSharpAngle = 50f;
	[SerializeField] private float _forkTurnSpeed = 4f;
	[SerializeField] private float _forkSteerFullAngle = 80f;
	[SerializeField, Range(0.1f, 1f)] private float _forkSteerMax = 0.65f;
	[SerializeField, Range(0f, 1f)] private float _forkAutoThrottle = 0.6f;
	[SerializeField] private bool _debugAlwaysDrive = false;
	[SerializeField] private bool _logInput = false;
	#endregion

	#region 내부 변수
	private CRoadTravel _travel;
	private CRunValue _torch;
	private CCoachPhysics _physics;
	private float _farthestArc;
	private float _arriveArc = -1f;
	private bool _isAutoArriving;
	private readonly List<float> _blockArcs = new List<float>();
	private bool _isBlockedEvent;
	private CMapRunner _runner;
	private readonly List<CNodeLink> _forkLinks = new List<CNodeLink>();
	private int _chosenBranch = -1;
	private int _signBranch = -1;
	private bool _isAtFork;
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

		if (_inventoryView == null)
		{
			_inventoryView = FindFirstObjectByType<CInventoryView>(FindObjectsInactive.Include);
		}

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
		_travel.OnStateChanged += HandleTravelStateChanged;

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

		if (_forkSigns != null)
		{
			_forkSigns.OnChosen -= HandleSignChosen;
		}

		if (_travel != null)
		{
			_travel.OnRoadEventStarted -= HandleRoadEventStarted;
			_travel.OnStateChanged -= HandleTravelStateChanged;
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
		TickArrivalFocus();
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

		if (_chaseCamera == null)
		{
			Camera sceneCamera = FindSceneCamera();

			if (sceneCamera != null)
			{
				_chaseCamera = sceneCamera.GetComponent<CChaseCamera>();

				if (_chaseCamera == null)
				{
					_chaseCamera = sceneCamera.gameObject.AddComponent<CChaseCamera>();
				}

				CPrint.Log($"CDriving3DScene : {sceneCamera.name} 카메라가 마차를 따라갑니다.");
			}
		}

		if (_chaseCamera != null)
		{
			_chaseCamera.SetTarget(_coach.transform);
		}
		else
		{
			CPrint.Warn("CDriving3DScene : 이 씬에서 카메라를 못 찾아 마차를 따라가지 않습니다. 인스펙터의 Chase Camera 를 채우세요.");
		}

		if (_landmarks == null)
		{
			_landmarks = GetComponent<CNodeLandmarkSpawner>();
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

		if (canDrive)
		{
			ApplyForkControl(ref input, ref brake);
			ApplyAutoArrive(ref input, ref brake);
			ApplyArriveBrake(ref input, ref brake);
		}

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

		if (_roadProps == null)
		{
			_roadProps = GetComponent<CRoadPropSpawner>();

			if (_roadProps == null)
			{
				_roadProps = gameObject.AddComponent<CRoadPropSpawner>();
			}
		}

		if (_eventBanner == null)
		{
			_eventBanner = GetComponent<CNodeEventBanner>();

			if (_eventBanner == null)
			{
				_eventBanner = gameObject.AddComponent<CNodeEventBanner>();
			}
		}

		_path.OnBranchChosen += HandleBranchChosen;
		_forkSigns.OnChosen += HandleSignChosen;
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
		_runner.SetArrivalPresenter(this);

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
		_runner.SetArrivalPresenter(null);
		_runner = null;
	}

	private void HandleMapStateChanged(EMapState prev, EMapState next)
	{
		// 노드 이벤트가 끝났다 → 카메라는 다시 마차로
		if (prev == EMapState.InNode && _chaseCamera != null)
		{
			_chaseCamera.ReleaseFocus();
		}

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

		// 외길 → 표지판 없이 바로 간다 (표지판은 갈림길에서만)
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
		_isAtFork = true;
		_signBranch = -1;
	}

	// 표지판을 끝까지 눌러 골랐다 → 그 갈래로 자동 조향해 들어간다
	private void HandleSignChosen(int branch)
	{
		if (!_isAtFork)
		{
			return;
		}

		_signBranch = branch;

		if (_logInput)
		{
			CPrint.Log($"CDriving3DScene : 표지판으로 {branch}번 갈래 선택 → 자동 조향");
		}
	}

	// 갈림길에서는 A·D 가 표지판 고르기에 쓰인다 → 조향은 막고, 고르기 전엔 갈림길 앞에서 멈춘다
	private void ApplyForkControl(ref Vector2 input, ref bool brake)
	{
		if (!_isAtFork || _path == null || !_path.HasFork)
		{
			return;
		}

		if (_signBranch < 0)
		{
			input.x = 0f;

			// 정지 지점에 닿으면 멈추고, 그때부터 표지판을 꾹 눌러 고를 수 있다
			bool isAtStop = GetTravelled() >= _path.ForkArc - _forkStopMargin;

			if (isAtStop)
			{
				input.y = 0f;
				brake = true;
			}

			_forkSigns.SetHoldEnabled(isAtStop);
			return;
		}

		input.y = Mathf.Max(input.y, _forkAutoThrottle);

		// 갈래 위 마차 자리보다 조금 앞 점을 계속 쫓는다 → 갈래가 꺾여도 길을 따라 돈다
		if (!_path.TryGetBranchFollowPoint(_signBranch, _coach.transform.position, _forkFollowLook, out Vector3 target))
		{
			return;
		}

		float angle = GetAngleToPoint(target);
		float sharp = Mathf.InverseLerp(_forkSharpAngle * 0.5f, _forkSharpAngle, Mathf.Abs(angle));
		float steerMax = Mathf.Lerp(_forkSteerMax, 1f, sharp);

		input.x = Mathf.Clamp(angle / Mathf.Max(1f, _forkSteerFullAngle), -steerMax, steerMax);

		// 급히 꺾는 곳에서는 천천히 → 회전 반경 안에 들어오게
		if (sharp > 0f && _physics.ForwardSpeed > _forkTurnSpeed)
		{
			input.y = 0f;
			brake = true;
		}
	}

	private float GetAngleToPoint(Vector3 target)
	{
		Transform coach = _coach.transform;
		Vector3 toTarget = Vector3.ProjectOnPlane(target - coach.position, Vector3.up);
		Vector3 forward = Vector3.ProjectOnPlane(coach.forward, Vector3.up);

		if (toTarget.sqrMagnitude < 0.01f || forward.sqrMagnitude < 0.01f)
		{
			return 0f;
		}

		return Vector3.SignedAngle(forward, toTarget, Vector3.up);
	}

	// 마차 앞 방향과 목표 점 사이 각도로 핸들 양을 정한다
	private float GetSteerToPoint(Vector3 target)
	{
		float angle = GetAngleToPoint(target);

		return Mathf.Clamp(angle / Mathf.Max(1f, _forkSteerFullAngle), -_forkSteerMax, _forkSteerMax);
	}

	private void HandleBranchChosen(int branch)
	{
		_isAtFork = false;

		if (_signBranch >= 0 && _signBranch != branch)
		{
			CPrint.Warn($"CDriving3DScene : 표지판은 {_signBranch}번인데 마차가 {branch}번 갈래로 들어갔습니다. 들어간 갈래로 갑니다.");
		}

		_signBranch = -1;

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
		PlanNextFork(link);
	}

	private void PlanNextFork(CNodeLink link)
	{
		CStagecoachConfig config = _controller != null ? _controller.Config : null;
		CNodeInstance node = link != null ? link.To : null;

		if (node == null || config == null)
		{
			return;
		}

		float arrive = _farthestArc + config.TravelDistance;

		_arriveArc = arrive;
		_isAutoArriving = false;
		_blockArcs.Clear();

		// 직선 전용 길(출발→교차로, 성채→여관) → 출발부터 도착까지 통째로 곧은 길 예약
		bool straightOnly = link.RouteData != null && link.RouteData.StraightOnly;
		float straightFrom = straightOnly ? _farthestArc : arrive - _arriveStraight;

		_path.RequestStraight(straightFrom, arrive + _forkLead);

		// 도착 지점 길 오른쪽에 노드 건축물
		if (_landmarks != null)
		{
			_landmarks.Request(node.TypeData, arrive);
		}

		// 도로 이벤트 위치도 출발할 때 정해진다
		if (_travel != null)
		{
			for (int i = 0; i < _travel.EventCount; i++)
			{
				float eventArc = _farthestArc + _travel.GetEventPosition(i) * config.TravelDistance;
				CRoadEventData eventData = _travel.GetEventData(i);

				if (eventData != null && eventData.BlocksRoad)
				{
					_blockArcs.Add(eventArc);
				}


				_path.RequestStraight(eventArc - _eventStraightBefore, eventArc + _eventStraightAfter);

				// 험한 길 잔해처럼 길 위에 놓는 물건
				if (_roadProps != null)
				{
					_roadProps.Request(_travel.GetEventData(i), eventArc);
				}
			}
		}

		if (node.OutLinks.Count < 2)
		{
			return;
		}

		_path.RequestFork(arrive + _forkLead, node.OutLinks.Count);
	}
	#endregion

	// 다음에 멈춰야 할 곳 → 아직 안 지난 방책 앞 지점 중 가장 가까운 곳, 없으면 도착 지점
	private float GetStopArc()
	{
		float travelled = GetTravelled();
		float stop = _arriveArc;

		for (int i = 0; i < _blockArcs.Count; i++)
		{
			if (_blockArcs[i] > travelled && _blockArcs[i] < stop)
			{
				stop = _blockArcs[i];
			}
		}

		return stop;
	}

	// 길을 막는 이벤트(전투)가 끝나 다시 달리게 되면 → 방책 한쪽을 치워 길을 연다
	private void HandleTravelStateChanged(ETravelState prev, ETravelState next)
	{
		if (prev != ETravelState.RoadEvent || !_isBlockedEvent)
		{
			return;
		}

		_isBlockedEvent = false;

		if (_roadProps == null || !_roadProps.OpenNearestBlocker())
		{
			CPrint.Warn("CDriving3DScene : 길을 막는 이벤트가 끝났는데 열 방책이 없습니다.");
		}
	}

	private void ApplyAutoArrive(ref Vector2 input, ref bool brake)
	{
		if (!_isAutoArriving || _arriveAutoThrottle <= 0f || brake)
		{
			return;
		}

		if (_travel == null || _travel.State != ETravelState.Driving)
		{
			return;
		}

		input.y = Mathf.Max(input.y, _arriveAutoThrottle);
		input.x = GetSteerAlongRoad();
	}

	// 길 가운데 선을 따라가도록 핸들을 꺾는다
	private float GetSteerAlongRoad()
	{
		if (_path == null || !_path.TryGetPoint(GetTravelled() + _arriveSteerLook, out Vector3 target, out _))
		{
			return 0f;
		}

		return GetSteerToPoint(target);
	}

	// 도착 지점이 가까우면 남은 거리 안에 멈출 수 있는 속도로 미리 줄인다
	private void ApplyArriveBrake(ref Vector2 input, ref bool brake)
	{
		if (_arriveArc < 0f || _travel == null || _travel.State != ETravelState.Driving)
		{
			return;
		}

		float remaining = GetStopArc() - GetTravelled();

		if (remaining < 0f)
		{
			return;
		}

		float allowed = Mathf.Max(_arriveCreepSpeed, Mathf.Sqrt(2f * _arriveBrakeDecel * remaining));
		float speed = _physics.ForwardSpeed;

		if (speed > allowed)
		{
			input.y = 0f;
			brake = true;
		}
		else if (speed > allowed * 0.9f && input.y > 0f)
		{
			// 한계 가까이에선 더 밟지 않는다
			input.y = 0f;
		}
	}

	private bool CanDrive()
	{
		// 짐칸이 열려 있으면 입력을 막고 브레이크
		if (_stopWhileInventoryOpen && _inventoryView != null && _inventoryView.IsOpen)
		{
			return false;
		}

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

	// 마차를 따라갈 카메라 → 이 씬에 있는 카메라를 먼저 찾는다
	private Camera FindSceneCamera()
	{
		Camera fallback = null;
		Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

		for (int i = 0; i < cameras.Length; i++)
		{
			Camera camera = cameras[i];

			if (camera == null || camera.gameObject.scene != gameObject.scene || camera.targetTexture != null)
			{
				continue;
			}

			if (camera.CompareTag("MainCamera"))
			{
				return camera;
			}

			if (fallback == null)
			{
				fallback = camera;
			}
		}

		return fallback != null ? fallback : Camera.main;
	}

	#region 도착 연출
	// 도착 지점이 가까워지면 미리 카메라를 건축물 쪽으로 돌린다
	private void TickArrivalFocus()
	{
		if (_landmarks == null || _chaseCamera == null || _chaseCamera.IsFocusing)
		{
			return;
		}

		if (_travel == null || _travel.State != ETravelState.Driving)
		{
			return;
		}

		GameObject landmark = _landmarks.FindAhead(GetTravelled(), _arrivalFocusLead);

		if (landmark != null)
		{
			FocusLandmark(landmark);
			_isAutoArriving = true;
		}
	}

	private void FocusLandmark(GameObject landmark)
	{
		CNodeLandmarkTile tile = landmark.GetComponent<CNodeLandmarkTile>();

		if (tile != null && tile.HasCustomCamera)
		{
			_chaseCamera.FocusFrom(tile.CameraPoint, tile.LookPoint);
			return;
		}

		// 노드 장면 → 장면 전체가 아니라 건물만 씬 뷰처럼 낮게 비춘다
		if (tile != null && tile.ClearRoot != null)
		{
			_chaseCamera.FocusBuilding(tile.ClearRoot, tile.FocusDistanceScale);
			return;
		}

		_chaseCamera.Focus(landmark.transform);
	}

	// 노드에 도착 → 카메라가 건축물로 옮겨 가고, 잠시 뒤 노드 이벤트
	public void Present(CNodeInstance node, Action onDone)
	{
		// 카메라 연출이 끝나면 이벤트 이름 띠를 띄우고, 띠를 눌러야 이벤트가 시작된다
		Action original = onDone;

		onDone = () => ShowEventBanner(node, original);

		if (_chaseCamera == null)
		{
			onDone?.Invoke();
			return;
		}

		if (!_chaseCamera.IsFocusing)
		{
			GameObject landmark = _landmarks != null && _coach != null ? _landmarks.FindNearest(_coach.transform.position, _arrivalFocusRange) : null;

			if (landmark == null)
			{
				onDone?.Invoke();
				return;
			}

			FocusLandmark(landmark);
		}

		if (_arrivalFocusTime <= 0f)
		{
			onDone?.Invoke();
			return;
		}

		StartCoroutine(CoArrivalFocus(onDone));
	}

	private void ShowEventBanner(CNodeInstance node, Action onDone)
	{
		if (_eventBanner == null || node == null)
		{
			onDone?.Invoke();
			return;
		}

		CNodeTypeData type = node.TypeData;

		_eventBanner.Show(GetEventTitle(type), type != null ? type.Icon : null, onDone);
	}

	// 상점이 이벤트보다 먼저 열린다 → 이름도 같은 순서로 고르고, 둘 다 없으면 노드 이름
	private static string GetEventTitle(CNodeTypeData type)
	{
		if (type != null && type.Shop != null && !string.IsNullOrEmpty(type.Shop.Title))
		{
			return type.Shop.Title;
		}

		if (type != null && type.Event != null && !string.IsNullOrEmpty(type.Event.Title))
		{
			return type.Event.Title;
		}

		return type != null ? type.DisplayName : "???";
	}

	private IEnumerator CoArrivalFocus(Action onDone)
	{
		// 노드 이벤트 창이 시간을 멈춰도 흐르게 실제 시간으로 잰다
		yield return new WaitForSecondsRealtime(_arrivalFocusTime);

		onDone?.Invoke();
	}
	#endregion

	private void HandleRoadEventStarted(CRoadEventData data)
	{
		_isBlockedEvent = data != null && data.BlocksRoad;

		if (_isBlockedEvent)
		{
			return;
		}

		// 위험 연출이 있는 이벤트 (위태로운 길) → 길가 숲에서 공격이 날아와 마차에 맞는다
		if (data != null && data.Hazard != ERoadHazard.None)
		{
			if (_hazards == null)
			{
				_hazards = GetComponent<CRoadHazardView>();

				if (_hazards == null)
				{
					_hazards = gameObject.AddComponent<CRoadHazardView>();
				}
			}

			_hazards.Play(data.Hazard, _coach, _physics);
			return;
		}

		// 길 위에 물건이 있는 이벤트 → 그 위를 지나가며 여러 번 덜컹거린다
		if (data != null && data.RoadPropPrefab != null && data.BumpCount > 0)
		{
			StartCoroutine(CoRoughBumps(data.BumpCount));
			return;
		}

		if (_coach != null)
		{
			_coach.Bump();
		}
	}

	// 좌우 바퀴를 번갈아 튀기며 마차 몸통도 들썩이게 한다
	private IEnumerator CoRoughBumps(int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (_coach != null)
			{
				_coach.Bump();
			}

			if (_physics != null)
			{
				_physics.Jolt(_bumpJolt * UnityEngine.Random.Range(0.7f, 1.1f), i % 2 == 0);
			}

			yield return new WaitForSeconds(_bumpInterval * UnityEngine.Random.Range(0.8f, 1.2f));
		}
	}

	private void HandleTorchChanged(ERunValue type, int prev, int current)
	{
		ApplyTorch();
	}

	// 횃불이 줄수록 마차 횃불이 약해지고 주변이 어두워진다
	private void ApplyTorch()
	{
		if (_torch == null)
		{
			return;
		}

		if (_torchView == null && _torchLight != null)
		{
			_torchView = _torchLight.GetComponent<CCoachTorchView>();

			if (_torchView == null)
			{
				_torchView = _torchLight.gameObject.AddComponent<CCoachTorchView>();
			}
		}

		if (_torchView != null)
		{
			_torchView.SetLevel(_torch.Ratio);
		}
	}
}
