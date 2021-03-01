using UnityEngine;

public class SimpleCameraController : MonoBehaviour {
    [Header("Movement Settings")]
    [Tooltip("Multiplactor factor for movement speed")]
    public float movementSpeed = 1;
    [Tooltip("Multiplactor factor for movement damping")]
    public float movementDamping = 15;
    [Tooltip("Jump Force")]
    public float jumpForce = 5.0f;

    [Header("Physics Settings")]
    [Tooltip("Mass")]
    public float mass = 1.0f;
    [Tooltip("ground friction (lower is higher)")]
    public float groundfriction = 0.1f;
    [Tooltip("air friction (lower is higher)")]
    public float airfriction = 0.99f;
    [Tooltip("Gravity")]
    public Vector3 gravity = new Vector3(0, -0.02f, 0);

    [Header("Camera Settings")]
    [Tooltip("Default camera (third/first")]
    public bool thirdPerson = true;
    [Tooltip("Multiplactor factor for camera mouse speed")]
    public float lookSpeed = 1;
    [Tooltip("FP Distance from player")]
    public float offsetFirst = -0.1f;
    [Tooltip("TP Distance from player")]
    public float offsetMin = 1;
    public float offsetMax = 4;
    [Tooltip("Multiplactor factor for camera offset damping")]
    public float cameraDamping = 15;
    [Tooltip("Collision search size for camera")]
    public float collisionOffset = 0.2f;

    [Header("Link Settings")]
    [Tooltip("Camera root the main camera pivots around")]
    public Transform playerCameraParent;
    [Tooltip("Player Object to apply transformations to")]
    public Transform playerParent;
    [Tooltip("Player Model Object to apply head transformations to")]
    public Transform playerHeadParent;

    [Header("Collider Settings")]
    [Tooltip("Collision Backstep")]
    public float collisionBackstep = 0.25f;
    [Tooltip("Capsule bottom sphere offset")]
    public float capsuleBotHeight = -0.5f;
    [Tooltip("Capsule top sphere offset")]
    public float capsuleTopHeight = 0.5f;
    [Tooltip("Capsule radius")]
    public float capusleRadius = 0.5f;
    [Tooltip("Feet to ground detection direction")]
    public Vector3 feetDirection = new Vector3(0, -1, 0);
    [Tooltip("Feet to ground detection distance")]
    public float feetDistance = 0.75f;


    [HideInInspector]
    Vector3 force = Vector3.zero;
    Vector3 accel = Vector3.zero;
    Vector3 velocity = Vector3.zero;
    Vector2 rotation = Vector2.zero;
    float offset;
    bool handledCamToggle = false;
    float lastJump = 0;

    void GetInput(){
        //menu
        if (Input.GetKey(KeyCode.Escape)){
            Cursor.lockState = CursorLockMode.Confined;
        }

        //camera rotation
        rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
        rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        playerCameraParent.localRotation = Quaternion.Euler(rotation.x, 0, 0);
        playerParent.transform.localRotation = Quaternion.Euler(0, rotation.y, 0);

        //camera zoom toggle
        if (Input.GetKey(KeyCode.F)){
            if (!handledCamToggle){
                Vector3 defaultPos = transform.localPosition;
                Vector3 directionNormalized = defaultPos.normalized;
                handledCamToggle = true;

                if (thirdPerson){
                    thirdPerson = false;

                    Vector3 currentPos = (directionNormalized * offsetFirst);
                    transform.localPosition = currentPos;
                }else{
                    thirdPerson = true;

                    if (offsetFirst < 0){directionNormalized *= -1;};
                    Vector3 currentPos = (directionNormalized * offset);
                    transform.localPosition = currentPos;
                }
            }
        }else{
            handledCamToggle = false;
        }

        //camera scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (thirdPerson){
            if (offset <= offsetMin && scroll > 0){
                thirdPerson = false;

                Vector3 defaultPos = transform.localPosition;
                Vector3 directionNormalized = defaultPos.normalized;
                Vector3 currentPos = (directionNormalized * offsetFirst);
                transform.localPosition = currentPos;
            }else{
                offset -= scroll;
                if (offset < offsetMin){
                    offset = offsetMin;
                }
                if (offset > offsetMax){
                    offset = offsetMax;
                }

                //camera collision
                Vector3 defaultPos = transform.localPosition;
                Vector3 directionNormalized = defaultPos.normalized;
                Vector3 currentPos = (directionNormalized * offset);
                float defaultDistance = offset;

                RaycastHit camhit;
                Vector3 dirTmp = transform.parent.TransformPoint(defaultPos) - playerCameraParent.position;
                if (Physics.SphereCast(playerCameraParent.position, collisionOffset, dirTmp, out camhit, defaultDistance)){
                    currentPos = (directionNormalized * (camhit.distance - collisionOffset));
                }
                transform.localPosition = Vector3.Lerp(transform.localPosition, currentPos, Time.deltaTime * cameraDamping);
            }
        }else{
            if (scroll < 0){
                thirdPerson = true;

                Vector3 defaultPos = transform.localPosition;
                Vector3 directionNormalized = defaultPos.normalized;
                if (offsetFirst < 0){directionNormalized *= -1;};
                Vector3 currentPos = (directionNormalized * offset);
                transform.localPosition = currentPos;
            }
        }

        //standing on surface
        RaycastHit feethit;
        Transform surface = null;
        Vector3 surfaceNorm = Vector3.zero;
        if(Physics.CapsuleCast(playerParent.transform.TransformPoint(new Vector3(0, capsuleBotHeight, 0)) - feetDirection * feetDistance * collisionBackstep, 
            playerParent.transform.TransformPoint(new Vector3(0, capsuleTopHeight, 0)) - playerParent.transform.TransformPoint(feetDirection) * feetDistance * collisionBackstep, 
            capusleRadius/2, Vector3.Normalize(feetDirection), out feethit, feetDistance*2 + collisionBackstep)){
            surface = feethit.transform;
            surfaceNorm = feethit.normal;
        }

        //walk
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W)){
            movement += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S)){
            movement += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A)){
            movement += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D)){
            movement += Vector3.right;
        }
        //jump
        if (surface != null && lastJump > 15 && Input.GetKey(KeyCode.Space)){
            lastJump = 0;
            force += Vector3.up * jumpForce;
            force += Quaternion.AngleAxis(rotation.y, Vector3.up) * Vector3.Normalize(movement) * jumpForce * jumpForce * movementSpeed;
        }else{
            if (lastJump < 16){
                lastJump++;
            }
        }

        //physics on player
        accel = force / mass;
        accel += gravity;
        force = Vector3.zero;
        velocity += accel;

        //friction
        if (surface != null){
            Vector3 slide = velocity - surfaceNorm * Vector3.Dot(surfaceNorm, velocity);
            velocity = velocity - slide * (1 - groundfriction);
        }
        velocity = velocity * airfriction;

        //move player object
        movement = Vector3.Normalize(movement);
        movement = Quaternion.AngleAxis(rotation.y, Vector3.up) * movement;
        movement = Vector3.Normalize(movement + velocity);
        Vector3 startPos = playerParent.transform.position - movement * collisionBackstep;
        float dist = 0.0f;
        float maxdist = movementSpeed + collisionBackstep + Vector3.Magnitude(velocity);
        Vector3 movePos = startPos + movement * maxdist;

        //handle player movement collision
        while (maxdist > 0 && dist < maxdist){
            RaycastHit movehit;
            if (Physics.CapsuleCast(playerParent.transform.TransformPoint(new Vector3(0, capsuleBotHeight, 0)) - movement * collisionBackstep, 
                playerParent.transform.TransformPoint(new Vector3(0, capsuleTopHeight, 0)) - movement * collisionBackstep, 
                capusleRadius, movement, out movehit, maxdist-dist)){
                //update to where we hit
                dist += movehit.distance;
                movePos = startPos + movement * (movehit.distance);
                startPos = movePos;

                //change movement so we slide along wall
                Vector3 slide = movehit.normal * Vector3.Dot(movehit.normal, movement);
                movement = movement - slide;

                //apply friction from surface
                slide = velocity - movehit.normal * Vector3.Dot(movehit.normal, velocity);
                velocity = velocity - slide * (1 - groundfriction);
            } else{
                dist = maxdist;
                movePos = startPos + movement * maxdist;
            }
        }
        playerParent.transform.position = Vector3.Lerp(playerParent.transform.position, movePos, Time.deltaTime * movementDamping);
    }

    void Start(){
        offset = offsetMax;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update(){
        GetInput();
    }
}

