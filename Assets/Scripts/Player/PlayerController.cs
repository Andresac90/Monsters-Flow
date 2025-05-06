using UnityEngine;

#region Command Pattern Classes
public class MoveCommand : ICommand
{
    private CharacterController _controller;
    private Vector3 _direction;
    private float _speed;

    public MoveCommand(CharacterController controller, Vector3 direction, float speed)
    {
        _controller = controller;
        _direction = direction;
        _speed = speed;
    }

    public void Execute()
    {
        _controller.Move(_direction * _speed * Time.deltaTime);
    }
}

public class LookCommand : ICommand
{
    private Transform _playerTransform;
    private Transform _cameraTransform;
    private Vector2 _lookInput;
    private float _sensitivity;
    private float _xRotation;

    public LookCommand(Transform player, Transform camera, Vector2 lookInput, float sensitivity, float xRotation)
    {
        _playerTransform = player;
        _cameraTransform = camera;
        _lookInput = lookInput;
        _sensitivity = sensitivity;
        _xRotation = xRotation;
    }

    public float GetXRotation() => _xRotation;

    public void Execute()
    {
        float mouseX = _lookInput.x * _sensitivity * Time.deltaTime;
        float mouseY = _lookInput.y * _sensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -80f, 80f);

        _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        _playerTransform.Rotate(Vector3.up * mouseX);
    }
}

public class JumpCommand : ICommand
{
    private PlayerController _player;

    public JumpCommand(PlayerController player)
    {
        _player = player;
    }

    public void Execute()
    {
        if (_player.IsGrounded)
        {
            _player.ApplyJump();
        }
    }
}

public class MegaJumpCommand : ICommand
{
    private PlayerController _player;

    public MegaJumpCommand(PlayerController player)
    {
        _player = player;
    }

    public void Execute()
    {
        if (_player.IsGrounded)
        {
            _player.ApplyMegaJump();
        }
    }
}
#endregion

//This is used to force the PlayerController to be attached to a GameObject with a CharacterController component
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private bool canSprint = false;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; } 
    public float SprintSpeed { get => sprintSpeed; set => sprintSpeed = value; } 

    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 100f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float megaJumpHeight = 9f;
    [SerializeField] private float gravity = -9.81f;
    public float JumpHeight { get => jumpHeight; set => jumpHeight = value; } 

    private CharacterController characterController;
    private Transform cameraTransform;
    private float xRotation = 0f;
    private float velocityY = 0f;
    private bool isGrounded = false;

    public bool IsGrounded => isGrounded;
    public bool HasMegaJumps = false;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraTransform = transform.GetChild(0);

        //Hide Mouse Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        isGrounded = characterController.isGrounded;
        HandleMovement();
        HandleLook();
        HandleMegaJump();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float currentSpeed = (canSprint && Input.GetKey(KeyCode.LeftShift)) ? sprintSpeed : moveSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        ICommand moveCommand = new MoveCommand(characterController, move, currentSpeed);
        moveCommand.Execute();
    }

    private void HandleLook()
    {
        Vector2 lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        LookCommand lookCommand = new LookCommand(transform, cameraTransform, lookInput, lookSensitivity, xRotation);
        lookCommand.Execute();
        xRotation = lookCommand.GetXRotation();
    }

    private void HandleMegaJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            ICommand jumpCommand = HasMegaJumps ? new MegaJumpCommand(this) : new JumpCommand(this);
            jumpCommand.Execute();
        }
    }

    public void ApplyJump()
    {
        velocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    public void ApplyMegaJump()
    {
        velocityY = Mathf.Sqrt(megaJumpHeight * -2f * gravity);
    }

    private void ApplyGravity()
    {
        if (isGrounded)
        {
            if (velocityY < 0) 
            {
                velocityY = -1f; // Prevents gravity from accumulating negatively
            }
        }
        else
        {
            velocityY += gravity * Time.deltaTime;
        }

        Vector3 gravityMovement = new Vector3(0, velocityY, 0);
        characterController.Move(gravityMovement * Time.deltaTime);
    }
}