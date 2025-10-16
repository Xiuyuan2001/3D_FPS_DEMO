using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色控制器
/// </summary>

public enum MovementState
{ 
    Walk,
    Run,
    Crouch,
    Jump
}


public class PlayerController : MonoBehaviour
{
    // 控制移动组件
    private CharacterController characterController;
    [SerializeField] private Vector3 moveDir;

    [Header("Speed Info")]
    public float speed;
    public float walkSpeed;
    public float runSpeed;
    public float crouchSpeed;
    public float jumpVelocity;

    [Header("Crouch Info")]
    [SerializeField] private float crouchHeight = 0.9f;
    [SerializeField] private float standHeight;
    private float originalHeight;
    private Vector3 originalCenter;
    [SerializeField] private float currentHeight;
    [SerializeField] private Vector3 currentCenter;
    [SerializeField] private float crouchMoveMultiplier = 0.5f;
    [SerializeField] private Vector3 standCenter;
    [SerializeField] private Vector3 crouchCenter;
    [SerializeField] private bool wantToStand;

    [Header("Status")]
    public MovementState state;
    private CollisionFlags collisionFlags;
    public bool isWalk;
    public bool isRun;
    public bool isJump;
    public bool isCrouch;
    public bool isGround;

    [Header("Camera Info")]
    private Transform cameraTransform;
    private float cameraOriginalY;

    [Header("Timer")]
    public float lastStandCheckTime;
    public float autoStandCheckInterval;        // 定时站立检查

    [Header("Audio")]
    public AudioClip walkClip;
    public AudioClip runClip;
    private AudioSource moveAudioSource;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
        moveAudioSource = GetComponent<AudioSource>();

        cameraOriginalY = transform.localPosition.y;

        walkSpeed = 4f;
        runSpeed = 6f;
        crouchSpeed = 2f;
        jumpVelocity = 0f;

        autoStandCheckInterval = 0.1f;

        originalCenter = characterController.center;
        originalHeight = characterController.height;

        isGround = characterController.isGrounded;

        currentCenter = originalCenter;
        currentHeight = originalHeight;
    }

    private void Update()
    {
        Jump();

        CrouchLogic();

        // 定时检查自动站立情况
        if (Time.time - lastStandCheckTime > autoStandCheckInterval)
        {
            TryAutoStand();
            lastStandCheckTime = Time.time;
        }

        PlayerFootSoundSet();        

        Moving();
    }

    private void CrouchLogic()
    {
        Crouch();
        SmoothCrouchTransition();
        AdjustCameraHeight();
    }

    private void Moving()
    {
        // GetAxisRaw - 松开按键立即停止 - GetAxis 滑行一小段距离停止
        float h = Input.GetAxisRaw("Horizontal");  // [-1,1]
        float v = Input.GetAxisRaw("Vertical");    // [-1,1]

        isRun = Input.GetKey(KeyCode.LeftShift);
        isWalk = (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0) ? true : false;

        if (isRun && isGround) 
        {
            state = MovementState.Run;
            speed = runSpeed;
        }
        else if(isGround)
        {
            state = MovementState.Walk;
            speed = walkSpeed;
        }
        else if(isCrouch && isGround)
        {
            state = MovementState.Crouch;
            speed = walkSpeed * crouchMoveMultiplier;
        }

            // 八向移动
            moveDir = (transform.right * h + transform.forward * v).normalized;

        characterController.Move(moveDir * speed * Time.deltaTime);
    }

    private void PlayerFootSoundSet()
    {
        // 在地面 && 在移动
        if (isGround && moveDir.sqrMagnitude > 0) 
        {
            moveAudioSource.clip = isRun ? runClip : walkClip;

            if(!moveAudioSource.isPlaying)
            {
                moveAudioSource.Play();
            }
        }
        else
        {
            moveAudioSource.Pause();
        }


        //if(isCrouch)
        //{
        //    moveAudioSource.Pause();
        //}

    }

    private void Jump()
    {
        isJump = Input.GetKeyDown(KeyCode.Space);

        if (isJump && isGround) 
        {
            isGround = false;
            jumpVelocity = 5f;
        }

        if(!isGround)
        {
            jumpVelocity += 2 * Physics.gravity.y * Time.deltaTime;    // 每帧对速度进行衰减
            Vector3 jump = new Vector3(0, jumpVelocity * Time.deltaTime, 0);   // 将垂直速度转化为本帧的垂直位移
            
            // 应用移动，并且 Move 会返回 collisionFlags 类型的值，可以通过此值判断是否落地
            collisionFlags = characterController.Move(jump);

            // 若与下面（地面）产生碰撞
            if(collisionFlags == CollisionFlags.Below)
            {
                isGround = true;
                jumpVelocity = 0f;
            }
            
            // 在空中
            if(isGround && collisionFlags == CollisionFlags.None)
            {
                isGround = false;  
            }

        }
    }

    private void Crouch()
    {
        if(Input.GetKeyDown(KeyCode.LeftControl) && isGround)
        {
            isCrouch = true;
            wantToStand = false;
            speed = crouchSpeed;
        }
        else if(Input.GetKeyUp(KeyCode.LeftControl))
        {
            wantToStand = true;
        }
    }

    private void TryAutoStand()
    {
        // 只有在处于wantToStand状态（即松开蹲键）和头顶没有障碍时才能结束蹲状态（update中不断检查实现自动起身效果）
        if(wantToStand && !CheckHeadObstruction())
        {
            isCrouch = false;
            wantToStand = false;
            speed = walkSpeed;
        }
    }

    private bool CheckHeadObstruction()
    {
        // 计算检测起点（角色当前位置加上当前高度的一半）
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight / 2);

        // 计算需要检测的距离（从当前高度到站立高度的差值）
        float checkDistance = standHeight - currentHeight + 0.1f; // 添加一点容差

        // 使用射线检测头顶是否有障碍物
        if (Physics.Raycast(rayStart, Vector3.up, out RaycastHit hit, checkDistance))
        {
            // 如果有障碍物，返回true
            return true;
        }

        // 没有障碍物，返回false
        return false;
    }

    private void SmoothCrouchTransition()
    {
        float targetHeight = isCrouch ? crouchHeight : standHeight;
        Vector3 targetCenter = isCrouch ? new Vector3(originalCenter.x,crouchHeight/2,originalCenter.z) : originalCenter;

        // 平滑过渡高度和中心点
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, 20 * Time.deltaTime);
        currentCenter = Vector3.Lerp(currentCenter, targetCenter, 20 * Time.deltaTime);

        characterController.height = currentHeight;
        characterController.center = currentCenter;
    }

    private void AdjustCameraHeight()
    {
        if (cameraTransform == null) return;

        float targetCameraY = isCrouch ? cameraOriginalY - (standHeight - crouchHeight) : cameraOriginalY;

        Vector3 newCameraPos = cameraTransform.localPosition;
        newCameraPos.y = Mathf.Lerp(newCameraPos.y, targetCameraY, 20 * Time.deltaTime);
        cameraTransform.localPosition = newCameraPos;
    }
}
