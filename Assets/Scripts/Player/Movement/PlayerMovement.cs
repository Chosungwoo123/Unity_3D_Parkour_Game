using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Movement

    [Space(10)] [Header("Movement")] [SerializeField]
    private float moveSpeed;

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

    #region Keybinds

    [Space(10)] [Header("Keybinds")] 
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    #endregion

    [Space(10)]
    [SerializeField] private Transform orientation;

    private float horizontalInput;
    private float verticalInput;

    private bool grounded;
    private bool readyToJump = true;

    private Vector3 moveDirection;

    private Rigidbody rigid;

    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
        rigid.freezeRotation = true;
    }

    private void Update()
    {
        GroundCheck();
        InputUpdate();
        SpeedControl();
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

    private void MoveUpdate()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
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
    }
    
    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rigid.velocity.x, 0f, rigid.velocity.z);
        
        // Limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rigid.velocity = new Vector3(limitedVel.x, rigid.velocity.y, limitedVel.z);
        }
    }
    
    private void Jump()
    {
        // Reset y velocity
        rigid.velocity = new Vector3(rigid.velocity.x, 0f, rigid.velocity.z);
        
        rigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    private void ResetJump()
    {
        readyToJump = true;
    }
}