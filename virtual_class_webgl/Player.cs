using System;
using UnityEngine;
using Fusion;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms;
using Fusion.Addons.SimpleKCC;

public class Player: NetworkBehaviour
{

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statusText;

    [Networked] public NetworkString<_16> PlayerName { get; set; }

    [Networked] public int AvatarIndex { get; set; }

    [Networked] private NetworkBool IsWalking { get; set; }
    [Networked] private NetworkBool IsJumping { get; set; }
    [Networked] private NetworkBool IsSit { get; set; }

    [Networked] private NetworkId CurrentChairId { get; set; }

    private Chair currentChair;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private GameObject avatarInstance;

    private Animator animator;

    private CharacterController _cc;
    //private SimpleKCC _kcc;

    private Camera mainCamera;

    [SerializeField] private GameObject cameraArm;

    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;

    [SerializeField] private float gravity = 20f;

    private float applySpeed = 2f;

    private Vector3 velocity;
    private bool jumpPressed;

    public float moveSpeed = 2f;

    public float jumpForce = 5f;
    public float gravityValue = -9.81f;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        //_kcc = GetComponent<SimpleKCC>();
    }

    private void Start()
    {
        // camera set to player
        //Camera.main.orthographic = false;
        //Camera.main.transform.SetParent(transform);
        //Camera.main.transform.localPosition = new Vector3(0f, 3f, -8f);
        //Camera.main.transform.localEulerAngles = new Vector3(10f, 0f, 0f);

        mainCamera = Camera.main;
        applySpeed = walkSpeed;
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // 
            Camera.main.GetComponent<CameraFollow>().SetTarget(cameraArm.transform);
            //
            PlayerName = DataManager.Instance.NickName;
            AvatarIndex = DataManager.Instance.AvatarIndex;
            // 
            SpawnAvatar();
        }
        else
        {
            // �ٸ� �÷��̾��� �ƹ�Ÿ ����
            SpawnAvatar();
        }

        nameText.text = PlayerName.ToString();
    }

    private void SpawnAvatar()
    {
        if (avatarInstance != null)
        {
            Destroy(avatarInstance);
        }

        var pos = transform.position;
        pos.y = pos.y - 0.75f;

        avatarInstance = Instantiate(DataManager.Instance.avatarPrefabs[AvatarIndex], pos, transform.rotation);
        avatarInstance.transform.SetParent(transform);
        avatarInstance.transform.localPosition = new Vector3(0f, -0.75f, 0f);

        animator = avatarInstance.GetComponent<Animator>();

        //_cc.Move(new Vector3(12f, 1f, 10f));
        //_kcc.SetPosition(new Vector3(13, 1, 13));
    }

    private void Update()
    {
    }

    public override void FixedUpdateNetwork()
    {        
        DataManager.Instance.CurrentUserCount = Runner.SessionInfo.PlayerCount;

        if (HasStateAuthority == false) { return; }

        // display player info
        nameText.text = PlayerName.ToString();

        if (GetInput(out NetworkInputData data))
        {
            if(!IsSit)
            {
                HandleMovement(data);
                if(data.buttons.IsSet(NetworkInputData.BUTTON_INTERACT))
                {
                    CheckForChair();
                }
            }
            else
            {
                if(data.buttons.IsSet(NetworkInputData.BUTTON_INTERACT))
                {
                    RPC_StandUp();
                }
            }
        }
    }

    private void CheckForChair()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f);
        bool nearChair = false;

        foreach(Collider collider in colliders)
        {
            Chair chair = collider.GetComponent<Chair>();
            if(chair != null && !chair.IsOccupied)
            {
                nearChair = true;
                if(GetInput(out NetworkInputData data) && data.buttons.IsSet(NetworkInputData.BUTTON_INTERACT))
                {
                    RPC_SitOn(chair);
                    break;
                }
            }
        }

        if(Object.HasStateAuthority)
        {
        }
    }

    //[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SitOn(Chair chair)
    {
        if(!chair.IsOccupied)
        {
            IsSit = true;
            CurrentChairId = chair.Object.Id;
            chair.IsOccupied = true;

            originalPosition = transform.position;
            originalRotation = transform.rotation;

            transform.position = chair.dockPoint.position + new Vector3(0f, 0.4f, -0.1f);
            transform.rotation = chair.dockPoint.rotation;

            chair.HideSitInfoCanvas();

            RPC_SitAnimation(IsSit);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StandUp()
    {
        if(IsSit)
        {
            IsSit = false;
            Chair chair = Runner.FindObject(CurrentChairId).GetComponent<Chair>();
            if(chair != null)
            {
                chair.IsOccupied = false;
            }
            CurrentChairId = default;

            transform.position = originalPosition;
            transform.rotation = originalRotation;

            chair.ShowSitInfoCanvas();

            RPC_SitAnimation(IsSit);
        }
    }

    public void HandleMovement(NetworkInputData data)
    {
        if (_cc.isGrounded)
        {
            velocity = new Vector3(0, -1, 0);
        }

        Vector3 moveDir = data.direction * moveSpeed * Runner.DeltaTime;

        var cdir = Camera.main.transform.TransformDirection(moveDir);
        cdir.y = 0;
        cdir.Normalize();
        transform.LookAt(transform.position + cdir);
        cdir = cdir * moveSpeed * Runner.DeltaTime;

        velocity.y += gravityValue * Runner.DeltaTime;

        if (jumpPressed && _cc.isGrounded)
        {
            velocity.y += jumpForce;
        }

        _cc.Move(cdir + velocity * Runner.DeltaTime);

        if (moveDir != Vector3.zero)
        {
            gameObject.transform.forward = cdir;
            IsWalking = true;
        }
        else
        {
            IsWalking = false;
        }

        RPC_PlayWalkAnimation(IsWalking);

        jumpPressed = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayWalkAnimation(bool isMove)
    {
        if (animator != null)
        {
            if(isMove)
            {
                animator.SetBool("isWalking", true);
            }
            else
            {
                animator.SetBool("isWalking", false);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SitAnimation(bool isSit)
    {
        if(animator != null)
        {
            animator.SetBool("isSit", isSit);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayJumpAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("doJump");
        }
    }

    private void Move(Vector3 movement)
    {
        IsWalking = movement.magnitude > 0.1f;

        if(movement != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(movement);
        }

        _cc.Move(movement * applySpeed * Runner.DeltaTime);

        if(animator != null)
        {
            animator.SetBool("isWalking", IsWalking);
        }
    }

    private void Jump(bool shouldJump)
    {
        if(shouldJump && _cc.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * gravity);
            IsJumping = true;
        }

        if(animator != null)
        {
            animator.SetTrigger("doJump");
        }
    }

    private void Sit(bool shouldSit)
    {
        IsSit = shouldSit;

        if(animator != null)
        {
            animator.SetBool("isSit", IsSit);
        }
    }

    private void ApplyGravity()
    {
        if (_cc.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            IsJumping = false;
        }

        velocity.y += gravity * Runner.DeltaTime;
        _cc.Move(velocity * Runner.DeltaTime);
    }

    // RpcSources : ������ �� �ִ� �Ǿ�
    // RpcTargets : �Ǿ ����Ǵ� �Ǿ�
    // 1. All : ��ο��� ���� / ���� ���� ��� �Ǿ ���ؼ� �����(���� ����)
    // 2. Proxies : �� ���� ���� / ��ü�� ���Ͽ� �Է� ���� �Ǵ� ���� ������ ���� ���� �ʴ� �Ǿ ���� �����
    // 3. InputAuthority : �Է� ���� �ִ� �Ǿ ���� / ��ü�� ���� �Է� ������ �ִ� �Ǿ ���� �����
    // 4. StateAuthority : ���� ���� �ִ� �Ǿ ���� / ��ü�� ���� ���� ������ �ִ� �Ǿ ���� �����
    // RpcInfo
    // - Tick : � ������ ƽ�� ���۵Ǿ�����
    // - Source : � �÷��̾�(PlayerRef)�� ���´���
    // - Channel : Unrealiable �Ǵ� Reliable RPC�� ���´��� ����
    // - IsInvokeLocal : �� RPC�� ���� ȣ���� ���� �÷��̾������� ����
    // * ���� ������ HostMode�� �������� �ʾ����� �̰� ���� ������ ��� ���� �÷��̾ �ȴ�. (�⺻�� ���� ��忩�� �׷� ��)

}
