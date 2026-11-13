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
	// 횃불 수치에 맞춰 마차 횃불 불빛을 바꾸는 곳 (비우면 _torchLight 에 붙여 쓴다)
	[SerializeField] private CCoachTorchView _torchView;
	[SerializeField] private CInventoryView _inventoryView;
	// 노드 도착 지점에 건축물을 세우는 곳 (비우면 같은 오브젝트에서 찾는다)
	[SerializeField] private CNodeLandmarkSpawner _landmarks;
	// 도착 지점 몇 m 앞부터 카메라가 건축물 쪽으로 돌지
	[SerializeField] private float _arrivalFocusLead = 25f;
	// 도착한 뒤 건축물을 더 비추는 시간 → 끝나면 노드 이벤트 (0 이면 바로 이벤트)
	[SerializeField] private float _arrivalFocusTime = 1.2f;
	// 마차에서 이 거리 안의 건축물만 비춘다
	[SerializeField] private float _arrivalFocusRange = 60f;
	// 짐칸을 보는 동안 마차를 세울지 (끄면 미니맵처럼 열어 둔 채 달린다)
	[SerializeField] private bool _stopWhileInventoryOpen = false;
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

		if (_forkLinks.Count == 1)
		{
			if (_forkSigns != null)
			{
				_forkSigns.ShowAhead(_path, _forkLinks[0], GetTravelled() + _forkLead);
			}

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
	//  ㄴ 다른 씬(UI 매니저 등)을 같이 띄우면 Camera.main 이 그 씬 카메라일 수 있다
	//  ㄴ 렌더 텍스처용 카메라(초상화 등)는 뺀다
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
	// 도착 지점이 가까워지면 미리 카메라를 건축물 쪽으로 돌린다 (마차가 지나친 뒤가 아니라 직전에)
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
			_chaseCamera.Focus(landmark.transform);
		}
	}

	// 노드에 도착 → (아직 안 돌았으면) 카메라가 건축물로 옮겨 가고, 잠시 뒤 노드 이벤트
	public void Present(CNodeInstance node, Action onDone)
	{
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

			_chaseCamera.Focus(landmark.transform);
		}

		if (_arrivalFocusTime <= 0f)
		{
			onDone?.Invoke();
			return;
		}

		StartCoroutine(CoArrivalFocus(onDone));
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
		if (_coach != null)
		{
			_coach.Bump();
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
