using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

public class HeroTeamController : Singleton<HeroTeamController>
{
    public enum SelectedHero
    {
        HeroA,
        HeroB
    }
    
    public enum MoveInputSource
    {
        Keyboard,
        External,
        KeyboardAndExternal
    }

    [Header("Heroes")] [SerializeField] private HeroActor heroA;
    [SerializeField] private HeroActor heroB;

    [Header("Control")] [SerializeField] private SelectedHero selectedHero = SelectedHero.HeroA;
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    [Header("Team Movement")] [SerializeField]
    private float teamMoveSpeed = 6f;

    [SerializeField] private float followerSpeedMultiplier = 1.5f;

    [Header("Follow Formation")] [SerializeField]
    private float followDistance = 1.1f;

    [SerializeField] private float followSideOffset = 0f;
    [SerializeField] private float formationTargetSmoothTime = 0.06f;

    [Header("Follower Smooth")] [SerializeField]
    private float followerStopDistance = 0.08f;

    [SerializeField] private float followerMoveSmoothTime = 0.08f;

    [Header("Input")] 
    [SerializeField] private MoveInputSource moveInputSource = MoveInputSource.KeyboardAndExternal;
    [SerializeField] private float inputSmoothTime = 0.04f;
    [SerializeField] private float moveInputThreshold = 0.05f;
    [SerializeField] private Vector3 externalMoveDirection;
    [SerializeField] private bool hasExternalInput;

    [Header("Runtime Debug")] [SerializeField]
    private Vector3 rawMoveDirection;

    [SerializeField] private Vector3 smoothMoveDirection;
    [SerializeField] private Vector3 lastMoveDirection = Vector3.forward;

    [Header("Movement Constraint")] [SerializeField]
    private bool useMovementConstraint = true;

    [SerializeField] private Vector2 minLimit = new Vector2(-5f, -3f); // x, z
    [SerializeField] private Vector2 maxLimit = new Vector2(5f, 3f); // x, z
    [SerializeField] private bool constrainFollowerTarget = true;

    private Vector3 inputSmoothVelocity;
    private Vector3 smoothedFollowerTarget;
    private Vector3 followerTargetVelocity;
    private Vector3 followerMoveVelocity;
    private bool wasManualMoving;

    private bool blockTeamMovement;

    public HeroActor HeroA => heroA;
    public HeroActor HeroB => heroB;
    public float TeamMoveSpeed => teamMoveSpeed;
    public SelectedHero CurrentSelectedHero => selectedHero;
    public event Action<HeroActor, HeroActor, SelectedHero> ControlledHeroChanged;

    public bool UseMovementConstraint => useMovementConstraint;
    public Vector2 MinLimit => minLimit;
    public Vector2 MaxLimit => maxLimit;

    public float MovementWidth => Mathf.Abs(maxLimit.x - minLimit.x);
    public float MovementDepth => Mathf.Abs(maxLimit.y - minLimit.y);
    public float MovementArea => MovementWidth * MovementDepth;

    private void Start()
    {
        ResetFollowerSmoothTarget();
        GameEventManager.Subscribe(GameEvents.OnWaveStart, UnblockTeamControl);
        GameEventManager.Subscribe(GameEvents.OnStageLost, BlockTeamControl);
        GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
    }

    private void Update()
    {
        if(blockTeamMovement)
            return;
        ReadInput();
        SmoothInput();
        HandleSwitchInput();
        HandleTeamMovement();
    }

    public void SwitchHero()
    {
        selectedHero = selectedHero == SelectedHero.HeroA
            ? SelectedHero.HeroB
            : SelectedHero.HeroA;

        ResetFollowerSmoothTarget();
        NotifyControlledHeroChanged();
    }

    public void SelectHeroA()
    {
        selectedHero = SelectedHero.HeroA;
        ResetFollowerSmoothTarget();
    }

    public void SelectHeroB()
    {
        selectedHero = SelectedHero.HeroB;
        ResetFollowerSmoothTarget();
    }

    public void SetHeroes(HeroActor heroA, HeroActor heroB)
    {
        this.heroA = heroA;
        this.heroB = heroB;

        selectedHero = SelectedHero.HeroA;

        ResetFollowerSmoothTarget();
    }

    private void ReadInput()
    {
        Vector3 keyboardInput = Vector3.zero;

        if (moveInputSource == MoveInputSource.Keyboard ||
            moveInputSource == MoveInputSource.KeyboardAndExternal)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");

            keyboardInput = new Vector3(x, 0f, z);

            if (keyboardInput.sqrMagnitude > 1f)
                keyboardInput.Normalize();
        }

        switch (moveInputSource)
        {
            case MoveInputSource.Keyboard:
                rawMoveDirection = keyboardInput;
                break;

            case MoveInputSource.External:
                rawMoveDirection = externalMoveDirection;
                break;

            case MoveInputSource.KeyboardAndExternal:
                rawMoveDirection = hasExternalInput ? externalMoveDirection : keyboardInput;
                break;
        }
    }

    private void HandleSwitchInput()
    {
        if (Input.GetKeyDown(switchKey))
            SwitchHero();
    }

    private Vector3 ClampPositionToMovementLimit(Vector3 position)
    {
        if (!useMovementConstraint)
            return position;

        position.x = Mathf.Clamp(position.x, minLimit.x, maxLimit.x);
        position.z = Mathf.Clamp(position.z, minLimit.y, maxLimit.y);

        return position;
    }

    private void HandleTeamMovement()
    {
        HeroActor controlledHero = GetControlledHero();
        HeroActor followerHero = GetFollowerHero();

        if (controlledHero == null || followerHero == null)
            return;

        bool isManualMoving =
            rawMoveDirection.sqrMagnitude > moveInputThreshold * moveInputThreshold;

        if (isManualMoving != wasManualMoving)
        {
            wasManualMoving = isManualMoving;

            if (isManualMoving)
            {
                ResetFollowerSmoothTarget();
            }
            else
            {
                controlledHero.StopTeamControl();
                followerHero.StopTeamControl();
                ResetFollowerVelocity();
                Debug.Log("Manual move End");
            }
        }

        if (!isManualMoving)
            return;

        HandleManualMovement(controlledHero, followerHero);
    }
    
    private void SmoothInput()
    {
        smoothMoveDirection = Vector3.SmoothDamp(
            smoothMoveDirection,
            rawMoveDirection,
            ref inputSmoothVelocity,
            inputSmoothTime
        );

        if (smoothMoveDirection.sqrMagnitude < 0.001f)
            smoothMoveDirection = Vector3.zero;
    }

    private void HandleManualMovement(HeroActor controlledHero, HeroActor followerHero)
    {
        Vector3 moveDir = smoothMoveDirection;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        if (moveDir.sqrMagnitude <= 0.001f)
            return;

        Vector3 controlledPosition = controlledHero.transform.position;

        Vector3 desiredNextPosition =
            controlledPosition + moveDir * teamMoveSpeed * Time.deltaTime;

        Vector3 clampedNextPosition =
            ClampPositionToMovementLimit(desiredNextPosition);

        Vector3 constrainedMove = clampedNextPosition - controlledPosition;

        if (constrainedMove.sqrMagnitude <= 0.000001f)
        {
            controlledHero.StopTeamControl();
            followerHero.StopTeamControl();
            ResetFollowerVelocity();
            return;
        }

        Vector3 constrainedMoveDir = constrainedMove.normalized;

        lastMoveDirection = constrainedMoveDir;

        float constrainedSpeed = constrainedMove.magnitude / Time.deltaTime;

        controlledHero.ManualMoveByTeam(
            constrainedMoveDir,
            constrainedSpeed
        );

        Vector3 rawFollowerTarget = GetBehindFormationPosition(
            controlledHero.transform.position,
            constrainedMoveDir
        );

        if (constrainFollowerTarget)
            rawFollowerTarget = ClampPositionToMovementLimit(rawFollowerTarget);

        smoothedFollowerTarget = Vector3.SmoothDamp(
            smoothedFollowerTarget,
            rawFollowerTarget,
            ref followerTargetVelocity,
            formationTargetSmoothTime
        );

        if (constrainFollowerTarget)
            smoothedFollowerTarget = ClampPositionToMovementLimit(smoothedFollowerTarget);

        followerHero.FollowByTeam(
            smoothedFollowerTarget,
            teamMoveSpeed * followerSpeedMultiplier,
            followerStopDistance,
            followerMoveSmoothTime,
            ref followerMoveVelocity
        );
    }

    private Vector3 GetBehindFormationPosition(Vector3 leaderPosition, Vector3 moveDirection)
    {
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.001f)
            moveDirection = lastMoveDirection;

        if (moveDirection.sqrMagnitude <= 0.001f)
            moveDirection = Vector3.forward;

        moveDirection.Normalize();

        Vector3 target = leaderPosition - moveDirection * followDistance;

        if (followSideOffset > 0.001f)
        {
            Vector3 sideDir = Vector3.Cross(Vector3.up, moveDirection).normalized;
            target += sideDir * followSideOffset;
        }

        return target;
    }


    private void ResetFollowerSmoothTarget()
    {
        ResetFollowerVelocity();

        HeroActor controlledHero = GetControlledHero();

        if (controlledHero == null)
            return;

        Vector3 moveDir =
            rawMoveDirection.sqrMagnitude > moveInputThreshold * moveInputThreshold
                ? rawMoveDirection.normalized
                : lastMoveDirection;

        smoothedFollowerTarget = GetBehindFormationPosition(
            controlledHero.transform.position,
            moveDir
        );
    }

    private void ResetFollowerVelocity()
    {
        followerTargetVelocity = Vector3.zero;
        followerMoveVelocity = Vector3.zero;
    }


    private void EnsureSelectedHeroIsValid()
    {
        if (selectedHero == SelectedHero.HeroA && heroA == null && heroB != null)
        {
            selectedHero = SelectedHero.HeroB;
            return;
        }

        if (selectedHero == SelectedHero.HeroB && heroB == null && heroA != null)
        {
            selectedHero = SelectedHero.HeroA;
        }
    }

    private void NotifyControlledHeroChanged()
    {
        ControlledHeroChanged?.Invoke(GetControlledHero(), GetFollowerHero(), selectedHero);
    }

    private void OnStageCleared(int _)
    {
        BlockTeamControl();
    }

    private void BlockTeamControl()
    {
        blockTeamMovement = true;
    }
    
    private void UnblockTeamControl()
    {
        blockTeamMovement = false;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!useMovementConstraint)
            return;

        Vector3 center = new Vector3(
            (minLimit.x + maxLimit.x) * 0.5f,
            0f,
            (minLimit.y + maxLimit.y) * 0.5f
        );

        Vector3 size = new Vector3(
            Mathf.Abs(maxLimit.x - minLimit.x),
            0.05f,
            Mathf.Abs(maxLimit.y - minLimit.y)
        );

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }

    public HeroActor GetControlledHero()
    {
        return selectedHero == SelectedHero.HeroA ? heroA : heroB;
    }

    public HeroActor GetFollowerHero()
    {
        return selectedHero == SelectedHero.HeroA ? heroB : heroA;
    }

    public void SetMovementConstraint(bool enabled)
    {
        useMovementConstraint = enabled;
    }

    public void SetMovementConstraintBounds(Vector2 min, Vector2 max)
    {
        minLimit = min;
        maxLimit = max;
    }

    public void SetMovementConstraintBounds(float minX, float maxX, float minZ, float maxZ)
    {
        minLimit = new Vector2(minX, minZ);
        maxLimit = new Vector2(maxX, maxZ);
    }
    
    public void SetMoveInput(Vector2 input)
    {
        Vector3 dir = new Vector3(input.x, 0f, input.y);

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        externalMoveDirection = dir;
        hasExternalInput = dir.sqrMagnitude > 0.001f;
    }

    public void ClearMoveInput()
    {
        externalMoveDirection = Vector3.zero;
        hasExternalInput = false;
    }

    public override UniTask InitializeAsync()
    {
        return UniTask.CompletedTask;
    }
}