// Some stupid rigidbody based movement by Dani

using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;

    //Assingables
    public Transform playerCam;
    public Transform orientation;

    //Other
    private Rigidbody rb;

    //Rotation and look
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    //Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;
    private bool surfing;
    private bool onRamp;
    private bool sliding;

    //Vaulting
    private float playerHeight;
    private float fallSpeed;

    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    //Crouch & Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;

    //Jumping
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    public float jumpForce = 550f;


    private Vector3 lastMoveSpeed;

    //Input
    float x, y;
    bool jumping, sprinting, crouching;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;


    private int readyToCounterX;

    private int readyToCounterY;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHeight = GetComponent<CapsuleCollider>().bounds.size.y;
        Instance = this;
    }

    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        MyInput();
        fallSpeed = rb.velocity.y;
        lastMoveSpeed = XZVector(rb.velocity);
        Look();
    }

    public static Vector3 XZVector(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    private void MyInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.LeftControl);

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();
    }

    public void StartCrouch()
    {
        if (!sliding)
        {
            sliding = true;
            base.transform.localScale = crouchScale;
            base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 0.65f, base.transform.position.z);
            if (rb.velocity.magnitude > 0.5f && grounded)
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    public void StopCrouch()
    {
        sliding = false;
        base.transform.localScale = playerScale;
        base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + 0.65f, base.transform.position.z);
    }

    private bool IsHoldingAgainstHorizontalVel(Vector2 vel)
    {
        if (!(vel.x < 0f - threshold) || !(x > 0f))
        {
            if (vel.x > threshold)
            {
                return x < 0f;
            }
            return false;
        }
        return true;
    }

    private bool IsHoldingAgainstVerticalVel(Vector2 vel)
    {
        if (!(vel.y < 0f - threshold) || !(y > 0f))
        {
            if (vel.y > threshold)
            {
                return y < 0f;
            }
            return false;
        }
        return true;
    }

    private void Movement()
    {
        UpdateCollisionChecks();

        if (!grounded)
        {
            rb.AddForce(Vector3.down * 2f);
        }

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);
        //RampMovement(mag);

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.fixedDeltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.6f;
            multiplierV = 0.6f;
            if (IsHoldingAgainstVerticalVel(mag))
            {
                float num7 = Mathf.Abs(mag.y * 0.025f);
                if (num7 < 0.5f)
                {
                    num7 = 0.5f;
                }
                multiplierV = Mathf.Abs(num7);
            }
        }

        if (surfing)
        {
            multiplier = 0.6f;
            multiplierV = 0.3f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }
    private void RampMovement(Vector2 mag)
    {
        if (grounded && onRamp && !surfing && !crouching && !jumping && Math.Abs(x) < 0.05f && Math.Abs(y) < 0.05f)
        {
            rb.useGravity = false;
            if (rb.velocity.y > 0f)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, 0f);
            }
            else if (rb.velocity.y <= 0f && Math.Abs(mag.magnitude) < 1f)
            {
                rb.velocity = Vector3.zero;
            }
        }
        else
        {
            rb.useGravity = true;
        }
    }

    public void Jump()
    {
        if ((grounded) && readyToJump)
        {
            readyToJump = false;
            rb.AddForce(Vector2.up * jumpForce * 1.5f, ForceMode.Impulse);
            rb.AddForce(normalVector * jumpForce * 0.5f, ForceMode.Impulse);
            Vector3 velocity = rb.velocity;
            if (rb.velocity.y < 0.5f)
            {
                rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
            }
            else if (rb.velocity.y > 0f)
            {
                rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
            }
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    public bool IsCrouching()
    {
        return crouching;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private float desiredX;
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -89.8f, 89.8f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping)
        {
            return;
        }
        if (crouching)
        {
            rb.AddForce(moveSpeed * 0.02f * -rb.velocity.normalized * slideCounterMovement);
            return;
        }
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f && readyToCounterX > 1)
        {
            rb.AddForce(moveSpeed * orientation.transform.right * 0.02f * (0f - mag.x) * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f && readyToCounterY > 1)
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * 0.02f * (0f - mag.y) * counterMovement);
        }
        if (IsHoldingAgainstHorizontalVel(mag))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * 0.02f * (0f - mag.x) * counterMovement * 2f);
        }
        if (IsHoldingAgainstVerticalVel(mag))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * 0.02f * (0f - mag.y) * counterMovement * 2f);
        }
        if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2f) + Mathf.Pow(rb.velocity.z, 2f)) > maxSpeed)
        {
            float num = rb.velocity.y;
            Vector3 vector = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(vector.x, num, vector.z);
        }
        if (Math.Abs(x) < 0.05f)
        {
            readyToCounterX++;
        }
        else
        {
            readyToCounterX = 0;
        }
        if (Math.Abs(y) < 0.05f)
        {
            readyToCounterY++;
        }
        else
        {
            readyToCounterY = 0;
        }
    }



    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
	public Vector2 FindVelRelativeToLook()
    {
        float current = orientation.transform.eulerAngles.y;
        float target = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * 57.29578f;
        float num = Mathf.DeltaAngle(current, target);
        float num2 = 90f - num;
        float magnitude = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        return new Vector2(y: magnitude * Mathf.Cos(num * ((float)Math.PI / 180f)), x: magnitude * Mathf.Cos(num2 * ((float)Math.PI / 180f)));
    }

    #region Collision

    private bool cancellingSurf;
    private bool cancellingGrounded;
    private int surfCancel;
    private int groundCancel;

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
                if (Vector3.Angle(Vector3.up, normal) > 1f)
                {
                    onRamp = true;
                }
                else
                {
                    onRamp = false;
                }
            }
            if (IsSurf(normal))
            {
                surfing = true;
                cancellingSurf = false;
                surfCancel = 0;
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private bool IsFloor(Vector3 v)
    {
        return Vector3.Angle(Vector3.up, v) < maxSlopeAngle;
    }

    private bool IsSurf(Vector3 v)
    {
        float num = Vector3.Angle(Vector3.up, v);
        if (num < 89f)
        {
            return num > maxSlopeAngle;
        }
        return false;
    }

    private bool IsWall(Vector3 v)
    {
        return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
    }

    private bool IsRoof(Vector3 v)
    {
        return v.y == -1f;
    }


    private void StopSurf()
    {
        surfing = false;
    }

    private void UpdateCollisionChecks()
    {
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
        }
        else
        {
            groundCancel++;
            if ((float)groundCancel > 5)
            {
                StopGrounded();
            }
        }
        if (!cancellingSurf)
        {
            cancellingSurf = true;
            surfCancel = 1;
            return;
        }
        surfCancel++;
        if ((float)surfCancel > 5f)
        {
            StopSurf();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        int layer = other.gameObject.layer;
        Vector3 normal = other.contacts[0].normal;
        if ((int)whatIsGround != ((int)whatIsGround | (1 << layer)))
        {
            return;
        }
        if (IsFloor(normal))
        {
            Debug.Log("Hit Floor");
            MoveCamera.Instance.BobOnce(new Vector3(0f, fallSpeed, 0f));
        }
        float num = 1.3f;
        if (IsWall(normal))
        {
            Debug.Log("Hit wall");
            Vector3 normalized = lastMoveSpeed.normalized;
            Vector3 vector = base.transform.position + Vector3.up * 1.6f;
            Debug.DrawLine(vector, vector + normalized * num, Color.blue, 10f, false);
            if (!Physics.Raycast(vector, normalized, num, whatIsGround) && Physics.Raycast(vector + normalized * num, Vector3.down, out var hitInfo, 3f, whatIsGround))
            {
                //return;
                Vector3 vector2 = hitInfo.point + Vector3.up * playerHeight * 0.5f;
                MoveCamera.Instance.vaultOffset += base.transform.position - vector2;
                base.transform.position = vector2;
                rb.velocity = lastMoveSpeed * 0.4f;
                readyToJump = false;
                Invoke(nameof(ResetJump), jumpCooldown);
            }

        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

    #endregion Collision
}