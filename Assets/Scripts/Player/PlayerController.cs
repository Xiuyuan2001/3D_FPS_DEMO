using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ɫ������
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
    // �����ƶ����
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
    public float autoStandCheckInterval;        // ��ʱվ�����

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

        // ��ʱ����Զ�վ�����
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
        // GetAxisRaw - �ɿ���������ֹͣ - GetAxis ����һС�ξ���ֹͣ
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

            // �����ƶ�
            moveDir = (transform.right * h + transform.forward * v).normalized;

        characterController.Move(moveDir * speed * Time.deltaTime);
    }

    private void PlayerFootSoundSet()
    {
        // �ڵ��� && ���ƶ�
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
            jumpVelocity += 2 * Physics.gravity.y * Time.deltaTime;    // ÿ֡���ٶȽ���˥��
            Vector3 jump = new Vector3(0, jumpVelocity * Time.deltaTime, 0);   // ����ֱ�ٶ�ת��Ϊ��֡�Ĵ�ֱλ��
            
            // Ӧ���ƶ������� Move �᷵�� collisionFlags ���͵�ֵ������ͨ����ֵ�ж��Ƿ����
            collisionFlags = characterController.Move(jump);

            // �������棨���棩������ײ
            if(collisionFlags == CollisionFlags.Below)
            {
                isGround = true;
                jumpVelocity = 0f;
            }
            
            // �ڿ���
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
        // ֻ���ڴ���wantToStand״̬�����ɿ��׼�����ͷ��û���ϰ�ʱ���ܽ�����״̬��update�в��ϼ��ʵ���Զ�����Ч����
        if(wantToStand && !CheckHeadObstruction())
        {
            isCrouch = false;
            wantToStand = false;
            speed = walkSpeed;
        }
    }

    private bool CheckHeadObstruction()
    {
        // ��������㣨��ɫ��ǰλ�ü��ϵ�ǰ�߶ȵ�һ�룩
        Vector3 rayStart = transform.position + Vector3.up * (currentHeight / 2);

        // ������Ҫ���ľ��루�ӵ�ǰ�߶ȵ�վ���߶ȵĲ�ֵ��
        float checkDistance = standHeight - currentHeight + 0.1f; // ���һ���ݲ�

        // ʹ�����߼��ͷ���Ƿ����ϰ���
        if (Physics.Raycast(rayStart, Vector3.up, out RaycastHit hit, checkDistance))
        {
            // ������ϰ������true
            return true;
        }

        // û���ϰ������false
        return false;
    }

    private void SmoothCrouchTransition()
    {
        float targetHeight = isCrouch ? crouchHeight : standHeight;
        Vector3 targetCenter = isCrouch ? new Vector3(originalCenter.x,crouchHeight/2,originalCenter.z) : originalCenter;

        // ƽ�����ɸ߶Ⱥ����ĵ�
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
