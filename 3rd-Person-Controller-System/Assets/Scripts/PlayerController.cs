using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Variables
    [Header("Character Stats")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float jumpHeight = 1.5f;
    public float rollForce = 2f;
    [Range(0.01f, 1f)]
    public float airControlPercentage;
    public float turnSmoothTime = 0.2f;

    [Header("Ground Check")]
    public float groundDetectionStartPoint = 0.5f;
    public float groundDirectionRayDistance = 0.2f;

    [Header("Combat")]
    public float progression = 0.8f;
    public bool mCanRegisterAttack = true;

    Camera cam;
    Rigidbody rb;
    Animator anim;
    TargetDetector detector;
    AnimatorStateInfo animState;

    Vector3 _inputs = Vector3.zero;
    Vector3 _moveDir = Vector3.zero;
    Quaternion targetRotation;

    RaycastHit hit;

    float _speed;
    float _turnSmoothVelocity;
    float _inAirTimer;

    [Header("Character Checks")]
    [SerializeField]
    bool _isGrounded = false;
    [SerializeField]
    bool _isInAir = false;
    [SerializeField]
    bool _isJumping = false;
    [SerializeField]
    public bool _isAiming = false;
    [SerializeField]
    bool _skipGroundCheck = false;
    [SerializeField]
    bool _allowInput = true;
    [SerializeField]
    bool _isAttacking = false;
    [SerializeField]
    bool _isRolling = false;
    [SerializeField]
    bool _rollButtonPressed = false;
    [SerializeField]
    bool _canAim = true;
    #endregion

    #region Unity Methods
    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        detector = GetComponentInChildren<TargetDetector>();

        _speed = walkSpeed;
    }

    void Update()
    {
        animState = anim.GetCurrentAnimatorStateInfo(0);

        HandlePlayerInput();

        CalculateMovement();
        CalculateRotation();

        ApplyRotation();
        HandleJump();
        HandleAttack();
        HandleDodgeRoll();

        HandleAnimation();
    }

    void FixedUpdate()
    {
        HandleVerticalMovement();
        ApplyGroundMovement();
    }

    void LateUpdate()
    {
        if (_isInAir)
            _inAirTimer += Time.deltaTime;
    }
    #endregion

    #region Methods
    void HandlePlayerInput()
    {
        //Inputs for player movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        _inputs = new Vector3(horizontal, 0f, vertical);

        if (!_allowInput || _isInAir || CheckIsInAttackingAnimation())
            _inputs = Vector3.zero;

        if (_inputs.magnitude > 0.1f)
            _inputs.Normalize();

        //Input for player jump
        if (Input.GetKeyDown(KeyCode.Space) && !_isRolling)
            _isJumping = true;

        //Input for player combat
        if (Input.GetMouseButton(0) && _isGrounded)
            _isAttacking = true;
        else
            _isAttacking = false;

        if (Input.GetMouseButton(1) && detector.nearestTarget != null && _canAim)
            _isAiming = true;
        else
            _isAiming = false;

        if (Input.GetKeyDown(KeyCode.LeftShift) && _isGrounded && mCanRegisterAttack)
            _rollButtonPressed = true;
    }

    void CalculateMovement()
    {
        float _gravity = _isGrounded ? 0f : Physics.gravity.y;

        //This is to set different types of movement system when the player is aiming 
        if (!_isAiming)
        {
            _moveDir = transform.forward * _speed * _inputs.magnitude + Vector3.up * _gravity;
        }
        else
        {
            Vector3 _vDir = transform.forward * _inputs.z * _speed;
            Vector3 _hDir = transform.right * _inputs.x * _speed;
            _moveDir = _vDir + _hDir + Vector3.up * _gravity;
        }
    }

    void CalculateRotation()
    {
        if (_isAiming)
        {
            RotateToTarget();
        }
        else
        {
            float angle = Mathf.Atan2(_inputs.x, _inputs.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
            float targetAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, angle, ref _turnSmoothVelocity, turnSmoothTime);
            targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        }
    }

    void RotateToTarget()
    {
        Vector3 targetDir = detector.nearestTarget.transform.position - transform.position;
        targetDir.y = 0f;
        Quaternion desiredRotation = Quaternion.LookRotation(targetDir);
        targetRotation = Quaternion.Slerp(transform.rotation, desiredRotation, 0.2f);
    }

    void ApplyGroundMovement()
    {
        if (_isGrounded)
            rb.velocity = _moveDir;
    }

    void ApplyRotation()
    {
        if (_inputs != Vector3.zero)
            transform.rotation = targetRotation;
    }

    void HandleVerticalMovement()
    {
        if (CheckIsGrounded())
        {
            _isGrounded = true;
            transform.position = Vector3.Lerp(transform.position, hit.point, 0.2f);

            if (_isJumping)
                _isJumping = false;

            if (_isInAir)
            {
                //Just landed
                if (animState.IsName("Falling"))
                    StartCoroutine(Land(1f));
                _isInAir = false;
                _inAirTimer = 0f;
            }
        }
        else
        {
            _isGrounded = false;
            _isInAir = true;
        }
    }

    IEnumerator Land(float timer)
    {
        _allowInput = false;
        yield return new WaitForSeconds(timer);
        _allowInput = true;
    }

    void HandleJump()
    {
        if (_isJumping && _isGrounded)
        {
            float jumpVelocity = Mathf.Sqrt(-2 * Physics.gravity.y * jumpHeight);
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            StartCoroutine(SkipGroundCheck(0.8f));
        }
    }

    IEnumerator SkipGroundCheck(float timer)
    {
        _skipGroundCheck = true;
        yield return new WaitForSeconds(timer);
        _skipGroundCheck = false;
    }

    void HandleAttack()
    {
        mCanRegisterAttack = CheckForAttackWindow();
        if (mCanRegisterAttack)
        {
            //Attack button is pressed and attack window is open
            if (_isAttacking)
                anim.SetTrigger("Attack");
            else
                anim.ResetTrigger("Attack");
        }
    }

    void HandleDodgeRoll()
    {
        if (_rollButtonPressed)
        {
            StartCoroutine(TurnOffAim(1.5f));

            anim.SetTrigger("Roll");
            _isRolling = CheckForRoll();
        }
        else
        {
            anim.ResetTrigger("Roll");
        }

        if (_isRolling)
        {
            float rollAmount = rollForce;
            if (_inputs != Vector3.zero)
                rollAmount = rollForce - 1.0f;
 
            rb.AddForce(transform.forward * rollAmount, ForceMode.Impulse);
            _rollButtonPressed = false;
            _isRolling = CheckForRoll();    //Here we wanna check if we still in rolling animation
        }
    }

    IEnumerator TurnOffAim(float time)
    {
        _canAim = false;
        yield return new WaitForSeconds(time);
        _canAim = true;
    }

    bool CheckIsGrounded()
    {
        if (_skipGroundCheck)
            return false;

        Vector3 origin = transform.position + (Vector3.up * groundDetectionStartPoint);
        float distance = groundDetectionStartPoint + groundDirectionRayDistance;
        return Physics.Raycast(origin, Vector3.down, out hit, distance);
    }

    public bool CheckIsInAttackingAnimation()
    {
        return animState.IsTag("Attack");
    }

    bool CheckForAttackWindow()
    {
        //If we are attacking, we need to check if our current animation time has passed a certain progression
        //to open up our window for next action
        if (CheckIsInAttackingAnimation())
            return animState.normalizedTime > progression;
        return true;
    }

    bool CheckForRoll()
    {
        return animState.IsName("Roll");
    }

    void HandleAnimation()
    {
        anim.SetBool("Aim", _isAiming);

        anim.SetFloat("Speed", _inputs.magnitude, 0.1f, Time.deltaTime);

        anim.SetFloat("AirTimer", _inAirTimer);
        anim.SetBool("Jump", _isJumping);
        anim.SetBool("InAir", _isInAir);

        anim.SetFloat("InputX", _inputs.x, 0.1f, Time.deltaTime);
        anim.SetFloat("InputZ", _inputs.z, 0.1f, Time.deltaTime);

        anim.SetBool("CanAttack", mCanRegisterAttack);
    }
    #endregion 
}
