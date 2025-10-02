using System.Collections;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private bool isGrounded;
    public float groundCheckRadius = 0.2f;
    public bool isFaceRight = true;
    public float wallSlidingSpeed = 5f;
    public float wallCheckRadius = 0.2f;
    

    private bool canDash = true;
    private bool isDashing;
    private float dashingPower = 20f;
    private float dashingTime = 0.2f;
    private float dashingCooldown = 1f;

    private Rigidbody2D rb;
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public TrailRenderer tr;


    private bool canDoubleJump = false;

    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(5f, 10f);

    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float moveInput = Input.GetAxis("Horizontal");
        if (isDashing) return;
        HandleJumpInput();
        wallSlide();
        wallJump();
        
        if (!isWallJumping)
        {
            flip();
        }
        setAnimation(moveInput);

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

    }

    private void FixedUpdate()
    {
        if (isDashing) return;
        //creates a circle with raidus 0.2 that check if player hits the ground layer 
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
        // Reset wall states when grounded
        if (isGrounded)
        {
            isWallSliding = false;
            isWallJumping = false;
        }

        if (!isWallJumping && !isWallSliding)
        {
            float moveInput = Input.GetAxis("Horizontal");
            // Clamp small values to zero to avoid sticking
            if (Mathf.Abs(moveInput) < 0.1f) moveInput = 0f;
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        }
    }

    private void setAnimation(float moveInput)
    {
        if (isGrounded)
        {
            if(moveInput == 0)
            {
                animator.Play("Player_Idle");
            }
            else
            {
                animator.Play("Player_Run");
            }
        }
        else//if player is in the air, that means its either going up or down, so we need jump or fall animation
        {
            if(rb.velocityY > 0)// if player is going up
            {
                animator.Play("Player_Jump");
            }
            else
            {
                animator.Play("Player_Fall");
            }
        }
    }

    private void flip()
    {
        if((isFaceRight && rb.velocity.x < 0f) || (!isFaceRight && rb.velocity.x > 0f))
        {
            isFaceRight = !isFaceRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void HandleJumpInput()
    {
        // Regular jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            canDoubleJump = true;
        }
        // Wall jump (takes priority over double jump)
        else if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            isWallJumping = true;
            float wallJumpDir = -transform.localScale.x;
            rb.velocity = new Vector2(wallJumpDir * wallJumpingPower.x, wallJumpingPower.y);
            canDoubleJump = false; // Reset double jump after wall jump
            Debug.Log("Player is wall jumping");
            if (transform.localScale.x != wallJumpDir)
            {
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                isFaceRight = !isFaceRight;
            }
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
        // Double jump (only if not wall sliding)
        else if (Input.GetButtonDown("Jump") && !isGrounded && !isWallSliding && canDoubleJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            canDoubleJump = false;
            animator.Play("Player_DoubleJump");
            Debug.Log("Player double jumped");
        }
    }
    private void wallSlide()
    {
        if (isTouchingWall && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            Debug.Log("Player is wall sliding");
            rb.velocity = new Vector2(rb.velocity.x, -wallSlidingSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void wallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if(Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            Debug.Log("Player is wall jumping");

            if (transform.localScale.x != wallJumpingDirection)
            {
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                isFaceRight = !isFaceRight;
            }
        }

        Invoke(nameof(StopWallJumping),wallJumpingDuration);
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

}
