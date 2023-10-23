using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    #region Movement

    [Space(10)] [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    #endregion

    #region Ground Check

    [Space(10)] [Header("Ground Check")] 
    
    [SerializeField] private float playerHeight;
    [SerializeField] private float groundDrag;

    [SerializeField] private LayerMask whatIsGround;
    
    #endregion

    #region Jump

    [Space(10)] [Header("Jump")] 
    
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;

    #endregion
    
    #region Crouching

    [Space(10)]
    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    #endregion

    #region Slope Handling

    [Space(10)] 
    [Header("Slope Handling")] 
    public float maxSlopeAngle;

    private bool exitingSlope;
    
    private RaycastHit slopeHit;

    #endregion

    #region Keybinds

    [Space(10)] [Header("Keybinds")] 
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    #endregion

    #region UI

    [Space(10)] [Header("UI")] 
    public TextMeshProUGUI text_speed;

    #endregion
    
    [Space(10)]
    [SerializeField] private Transform orientation;

    private float horizontalInput;
    private float verticalInput;

    private bool grounded;
    private bool readyToJump = true;

    private Vector3 moveDirection;

    private Rigidbody rigid;

    public MovementState state;
    
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
        rigid.freezeRotation = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        GroundCheck();
        InputUpdate();
        SpeedControl();
        StateHandler();
        HandleDrag();
    }

    private void FixedUpdate()
    {
        MoveUpdate();
    }

    private void GroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void InputUpdate()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        // When to jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();
            
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        
        // Start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rigid.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        
        // Stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }
    
    private void HandleDrag()
    {
        if (grounded)
        {
            rigid.drag = groundDrag;
        }
        else
        {
            rigid.drag = 0;
        }
    }
    
    private void StateHandler()
    {
        // Mode - Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        
        // Mode - Sprinting
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        
        // Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }

        // Move - Air
        else
        {
            state = MovementState.air;
        }
    }

    private void MoveUpdate()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        // On Slope
        if (OnSlope() && !exitingSlope)
        {
            rigid.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rigid.velocity.y > 0)
            { 
                rigid.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        
        // On ground
        if (grounded)
        {
            rigid.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        // In air
        else if (!grounded)
        {
            rigid.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);   
        }
        
        // Turn gravity off while on slope
        rigid.useGravity = !OnSlope();
    }
    
    private void SpeedControl()
    {
        // Limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rigid.velocity.magnitude > moveSpeed)
            {
                rigid.velocity = rigid.velocity.normalized * moveSpeed;
                
                text_speed.SetText("Speed : " + rigid.velocity.magnitude);

                return;
            }
            
            text_speed.SetText("Speed : " + rigid.velocity.magnitude);
        }
        
        // Limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rigid.velocity.x, 0f, rigid.velocity.z);
        
            // Limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rigid.velocity = new Vector3(limitedVel.x, rigid.velocity.y, limitedVel.z);
                
                text_speed.SetText("Speed : " + limitedVel.magnitude);

                return;
            }
            
            text_speed.SetText("Speed : " + flatVel.magnitude);
        }
    }
    
    private void Jump()
    {
        exitingSlope = true;
        
        // Reset y velocity
        rigid.velocity = new Vector3(rigid.velocity.x, 0f, rigid.velocity.z);
        
        rigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);

            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }
    
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}