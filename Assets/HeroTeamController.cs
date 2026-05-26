using System;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

public class HeroTeamController : MonoBehaviour
{
    public enum SelectedHero
    {
        HeroA,
        HeroB
    }

    [Header("Heroes")]
    [SerializeField] private HeroActor heroA;
    [SerializeField] private HeroActor heroB;

    [Header("Control")]
    [SerializeField] private SelectedHero selectedHero = SelectedHero.HeroA;
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    [Header("Team Movement")]
    [SerializeField] private float teamMoveSpeed = 6f;
    [SerializeField] private float followerSpeedMultiplier = 1.5f;

    [Header("Follow Formation")]
    [SerializeField] private float followDistance = 1.1f;
    [SerializeField] private float followSideOffset = 0f;
    [SerializeField] private float formationTargetSmoothTime = 0.06f;

    [Header("Follower Smooth")]
    [SerializeField] private float followerStopDistance = 0.08f;
    [SerializeField] private float followerMoveSmoothTime = 0.08f;

    [Header("Input")]
    [SerializeField] private float inputSmoothTime = 0.04f;
    [SerializeField] private float moveInputThreshold = 0.05f;

    [Header("Runtime Debug")]
    [SerializeField] private Vector3 rawMoveDirection;
    [SerializeField] private Vector3 smoothMoveDirection;
    [SerializeField] private Vector3 lastMoveDirection = Vector3.forward;

    private Vector3 inputSmoothVelocity;
    private Vector3 smoothedFollowerTarget;
    private Vector3 followerTargetVelocity;
    private Vector3 followerMoveVelocity;
    private bool wasManualMoving;
    
    public HeroActor HeroA => heroA;
    public HeroActor HeroB => heroB;
    public float TeamMoveSpeed => teamMoveSpeed;
    public SelectedHero CurrentSelectedHero => selectedHero;
    public event Action<HeroActor, HeroActor, SelectedHero> ControlledHeroChanged;

    private void Start()
    {
        ResetFollowerSmoothTarget();
    }

    private void Update()
    {
        ReadInput();
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
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        rawMoveDirection = new Vector3(x, 0f, z);

        if (rawMoveDirection.sqrMagnitude > 1f)
            rawMoveDirection.Normalize();

        smoothMoveDirection = Vector3.SmoothDamp(
            smoothMoveDirection,
            rawMoveDirection,
            ref inputSmoothVelocity,
            inputSmoothTime
        );

        if (smoothMoveDirection.sqrMagnitude < 0.001f)
            smoothMoveDirection = Vector3.zero;
    }

    private void HandleSwitchInput()
    {
        if (Input.GetKeyDown(switchKey))
            SwitchHero();
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
            }
        }

        if (!isManualMoving)
            return;

        HandleManualMovement(controlledHero, followerHero);
    }

    private void HandleManualMovement(HeroActor controlledHero, HeroActor followerHero)
    {
        Vector3 moveDir = rawMoveDirection;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        if (moveDir.sqrMagnitude <= 0.001f)
            return;

        lastMoveDirection = moveDir;

        controlledHero.ManualMoveByTeam(
            smoothMoveDirection,
            teamMoveSpeed
        );

        Vector3 rawFollowerTarget = GetBehindFormationPosition(
            controlledHero.transform.position,
            moveDir
        );

        smoothedFollowerTarget = Vector3.SmoothDamp(
            smoothedFollowerTarget,
            rawFollowerTarget,
            ref followerTargetVelocity,
            formationTargetSmoothTime
        );

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

    public HeroActor GetControlledHero()
    {
        return selectedHero == SelectedHero.HeroA ? heroA : heroB;
    }

    public HeroActor GetFollowerHero()
    {
        return selectedHero == SelectedHero.HeroA ? heroB : heroA;
    }
}