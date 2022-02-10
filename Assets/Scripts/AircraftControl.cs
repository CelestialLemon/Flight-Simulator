using UnityEngine;

public class AircraftControl : MonoBehaviour
{
    // thrust is the force with which the propeller pushes the plane forward

    [SerializeField] float thrust = 300f;

    //force to move plane on ground
    [SerializeField] float groundMoveForce = 2000f;
    [SerializeField] float groundRotateForce = 5000f;

    [SerializeField] bool isEngineOn = false;
    
    //rudderSpeed and elevatorSpeed are speed at which they turn / flap
    
    [SerializeField] float rudderSpeed = 0.01f;
    [SerializeField] float elevatorSpeed = 0.01f;

    // rudder and elevator force factor is how much force the flaps exert when wind hits them

    [SerializeField] float rudderForceFactor = 0.5f;
    [SerializeField] float elevatorForceFactor = 0.5f;

    // propeller component and speed of rotation
    [SerializeField] GameObject propeller;
    [SerializeField] float propellerSpeed = 75.0f;

    // current angle of the rudder and elevator flap

    float rudderAngle = 0.0f, elevatorAngle = 0.0f;

    // rigidbody of the aircraft
    Rigidbody rb;

    // is plane on ground or in air
    public bool isGrounded = true;

    // states to stores if the plane is moving forward, backward, left and right
    // only used when plane is on ground
    bool moveF, moveB, moveL, moveR;

    //non state variables
    Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
    Vector3 m_EulerAngleVelocity;

    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        
        if (!isGrounded)
            HandleRotationInAir();

        if (isEngineOn) 
            RotatePropeller();
    }
    void HandleInput()
    {
        //turn engine on and off
        if (Input.GetKeyDown(KeyCode.E)) isEngineOn = !isEngineOn ;

        /*
         * rudder input
         * if a is pressed rudder is moved to left
         * if d is pressed rudder is moved to right
         * if neither is pressed rudder slowly moves back to original position
         */
        if (Input.GetKey(KeyCode.A)) rudderAngle -= rudderSpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.D)) rudderAngle += rudderSpeed * Time.deltaTime;
        else
        {
            // if rudder angle is too close to zero make is zero
            // this avoids rudder oscillating between +ve and -ve values

            if (Mathf.Abs(rudderAngle) < 0.01f) rudderAngle = 0;
            else if (rudderAngle > 0) rudderAngle -= rudderSpeed * Time.deltaTime;
            else if (rudderAngle < 0) rudderAngle += rudderSpeed * Time.deltaTime;
        }
        rudderAngle = Mathf.Clamp(rudderAngle, -1.0f, 1.0f);

        /*
         * elevator flap input
         * if shift is pressed flap moves upward
         * if ctrl is pressed flap moves downward
         * if neither is pressed flap slowly moves back to original position
         */
        if (Input.GetKey(KeyCode.LeftShift)) elevatorAngle -= elevatorSpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.LeftControl)) elevatorAngle += elevatorSpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.Space)) elevatorAngle = 0;
        else
        {
            // if elevator angle is too close to zero make is zero
            // this avoids elevator oscillating between +ve and -ve values

            if (Mathf.Abs(elevatorAngle) < 0.01f) elevatorAngle = 0;
            else if (elevatorAngle > 0) elevatorAngle -= elevatorSpeed * Time.deltaTime;
            else if (elevatorAngle < 0) elevatorAngle += elevatorSpeed * Time.deltaTime;
        }
        elevatorAngle = Mathf.Clamp(elevatorAngle, -1.0f, 1.0f);

        /*
         * To move the aircraft when engine is off
         */
        if (Input.GetKey(KeyCode.UpArrow)) moveF = true;
        else moveF = false;
        if (Input.GetKey(KeyCode.DownArrow)) moveB = true;
        else moveB = false;
        if (Input.GetKey(KeyCode.LeftArrow)) moveL = true;
        else moveL = false;
        if (Input.GetKey(KeyCode.RightArrow)) moveR = true;
        else moveR = false;
    }

    /*
     * Calculates position of mouse relative to center of screen
     * converts the position to range {-1, 1}
     */
    Vector2 GetMousePosition()
    {
        Vector2 mPos = Input.mousePosition;
        mPos = mPos - screenCenter;
        mPos.x /= screenCenter.x;
        mPos.y /= screenCenter.y;
        return mPos;
    }

    /*
     * Has 2 movement controls. One when engine is off and another when engine is on
     * When engine is on turning is done using rudder and elevator flaps
     * When engine is off turning is done using wheels i.e normal force
     */
    void HandleMovement()
    {
        if (isEngineOn)
        {
            rb.AddForce(transform.forward * thrust, ForceMode.Impulse);
            
            // 0.19f is the balacing factor to keep the plane at steady height when travelling at top speed
            // it could be considered the default elevatorAngle
            
            rb.AddForce(transform.up * (-elevatorAngle + 0.19f) * elevatorForceFactor * rb.velocity.magnitude);
            rb.AddForce(transform.right * rudderAngle * rudderForceFactor * rb.velocity.magnitude);
        }
        else
        {
            // movement when engine is off
            if (moveF) rb.AddForce(transform.forward * groundMoveForce, ForceMode.Impulse);
            if (moveB) rb.AddForce(transform.forward * -groundMoveForce, ForceMode.Impulse);
            if (moveL) rb.AddTorque(new Vector3(0, -groundRotateForce, 0), ForceMode.Impulse);
            if (moveR) rb.AddTorque(new Vector3(0, groundRotateForce, 0), ForceMode.Impulse);
        }
    }

    /*
     * Takes the position of mouse in range -1 to +1
     * Rotates the plane depending on the position of mouse
     */
    void HandleRotationInAir()
    {
        Vector2 mPos = GetMousePosition();
        m_EulerAngleVelocity = new Vector3(-mPos.y, 0, -mPos.x);
        Quaternion deltaRot = Quaternion.Euler(m_EulerAngleVelocity);
        rb.MoveRotation(transform.rotation * deltaRot);
    }

    /*
     * Rotates the propeller when engine is on
     */
    void RotatePropeller()
    {
        propeller.transform.Rotate(new Vector3(0, propellerSpeed, 0));
    }

    /*
     * Ground check
     */
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) 
            isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) 
            isGrounded = false;
    }
}
