using UnityEngine;

public class Move : MonoBehaviour
{
    public enum MoveType { PingPong, Circle }
    public enum SwimDirection { HeadFirst, TailFirst }
    
    // 新增：动作状态机，用于区分“正在游”和“停下转身”
    private enum ActionState { Swimming, Turning }

    [Header("Basic Settings")]
    public MoveType moveType = MoveType.PingPong;
    [Range(0.1f, 10f)] public float moveSpeed = 1.5f;
    
    [Header("Rotation Settings")]
    [Tooltip("转身速度 (度/秒)")]
    public float turnSpeed = 150f;
    public SwimDirection swimDirection = SwimDirection.HeadFirst;

    [Header("Ping Pong Settings")]
    [Tooltip("没有指定路径点时，自动往前游的距离")]
    public float autoSwimDistance = 5f;
    public Transform[] waypoints;

    [Header("Circle Settings")]
    public float circleRadius = 3f;
    public float circleSpeed = 40f;

    // --- Private Variables ---
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _isMovingForward = true;
    private int _currentWaypointIndex = 0;
    private int _waypointDirection = 1;
    
    private float _circleAngle;
    private Vector3 _circleCenter;

    // 状态控制变量
    private ActionState _currentState = ActionState.Swimming;
    private Quaternion _targetRotation;

    private void Start()
    {
        _startPos = transform.position;
        
        if (moveType == MoveType.PingPong)
        {
            _targetPos = _startPos + transform.forward * autoSwimDistance;
            
            // 初始化朝向
            Vector3 initialDir = (_targetPos - _startPos).normalized;
            if (initialDir != Vector3.zero)
            {
                Vector3 lookDir = (swimDirection == SwimDirection.HeadFirst) ? initialDir : -initialDir;
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            _circleCenter = _startPos + transform.right * circleRadius;
            _circleAngle = 180f;
        }
    }

    private void Update()
    {
        if (moveType == MoveType.PingPong)
        {
            if (waypoints != null && waypoints.Length > 0)
                MoveWithWaypoints();
            else
                MoveAutoPingPong();
        }
        else
        {
            // 绕圈模式不需要停顿，保持一边游一边转
            MoveCircle();
        }
    }

    private void MoveAutoPingPong()
    {
        if (_currentState == ActionState.Swimming)
        {
            Vector3 currentTarget = _isMovingForward ? _targetPos : _startPos;
            Vector3 directionVector = currentTarget - transform.position;
            float distanceThisFrame = moveSpeed * Time.deltaTime;

            // 1. 如果游到了目标点
            if (directionVector.magnitude <= distanceThisFrame)
            {
                // 停在目标点
                transform.position = currentTarget;
                _isMovingForward = !_isMovingForward; 
                
                // 计算掉头后的新方向
                Vector3 newTarget = _isMovingForward ? _targetPos : _startPos;
                Vector3 newDir = (newTarget - transform.position).normalized;
                
                // 切换为转身状态
                SetTargetRotation(newDir);
                _currentState = ActionState.Turning;
                return;
            }

            // 2. 如果还没到，就继续直线游
            transform.position += directionVector.normalized * distanceThisFrame;
        }
        else if (_currentState == ActionState.Turning)
        {
            // 3. 执行原地转身动作
            PerformTurn();
        }
    }

    private void MoveWithWaypoints()
    {
        if (_currentState == ActionState.Swimming)
        {
            Vector3 target = waypoints[_currentWaypointIndex].position;
            Vector3 directionVector = target - transform.position;
            float distanceThisFrame = moveSpeed * Time.deltaTime;

            if (directionVector.magnitude <= distanceThisFrame)
            {
                transform.position = target;
                UpdateWaypointIndex();
                
                Vector3 newTarget = waypoints[_currentWaypointIndex].position;
                Vector3 newDir = (newTarget - transform.position).normalized;

                SetTargetRotation(newDir);
                _currentState = ActionState.Turning;
                return;
            }

            transform.position += directionVector.normalized * distanceThisFrame;
        }
        else if (_currentState == ActionState.Turning)
        {
            PerformTurn();
        }
    }

    // 设置转身的目标角度
    private void SetTargetRotation(Vector3 newMoveDirection)
    {
        if (newMoveDirection == Vector3.zero) return;
        
        Vector3 lookDir = (swimDirection == SwimDirection.HeadFirst) ? newMoveDirection : -newMoveDirection;
        _targetRotation = Quaternion.LookRotation(lookDir);
    }

    // 执行原地转身
    private void PerformTurn()
    {
        // 原地旋转身体
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, turnSpeed * Time.deltaTime);

        // 如果角度相差不到 1 度，说明转完身了
        if (Quaternion.Angle(transform.rotation, _targetRotation) < 1f)
        {
            transform.rotation = _targetRotation; // 强制对齐最后一点点偏差
            _currentState = ActionState.Swimming; // 切换回游泳状态，继续前进
        }
    }

    private void UpdateWaypointIndex()
    {
        if (waypoints.Length <= 1) return;

        if (_currentWaypointIndex == waypoints.Length - 1) _waypointDirection = -1;
        else if (_currentWaypointIndex == 0) _waypointDirection = 1;

        _currentWaypointIndex += _waypointDirection;
    }

    private void MoveCircle()
    {
        _circleAngle += circleSpeed * Time.deltaTime;
        float radians = _circleAngle * Mathf.Deg2Rad;
        
        Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * circleRadius;
        Vector3 nextPosition = _circleCenter + offset;
        Vector3 moveDir = (nextPosition - transform.position).normalized;

        if (moveDir != Vector3.zero)
        {
            Vector3 lookDir = (swimDirection == SwimDirection.HeadFirst) ? moveDir : -moveDir;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
        
        transform.position = nextPosition;
    }
}