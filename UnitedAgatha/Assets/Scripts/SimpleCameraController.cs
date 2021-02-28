using UnityEngine;

public class SimpleCameraController : MonoBehaviour {
    [Header("Movement Settings")]
    [Tooltip("Multiplactor factor for movement speed")]
    public float movementSpeed = 1;
    [Tooltip("Multiplactor factor for movement damping")]
    public float movementDamping = 15;

    [Header("Camera Settings")]
    [Tooltip("Multiplactor factor for camera mouse speed")]
    public float lookSpeed = 1;
    [Tooltip("Distance from player")]
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
    [Tooltip("Capsule bottom sphere height")]
    public float capsuleBotHeight = 0.5f;
    [Tooltip("Capsule top sphere height")]
    public float capsuleTopHeight = 1.5f;
    [Tooltip("Capsule radius")]
    public float capusleRadius = 0.5f;


    [HideInInspector]
    Vector2 rotation = Vector2.zero;
    Vector3 movement = Vector3.zero;
    float offset;

    void GetInput(){
        if (Input.GetKey(KeyCode.W))
        {
            movement += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += Vector3.right;
        }

        //camera rotation
        rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
        rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
        playerCameraParent.localRotation = Quaternion.Euler(rotation.x, 0, 0);
        playerParent.transform.localRotation = Quaternion.Euler(0, rotation.y, 0);

        //camera zoom
        offset -= Input.GetAxis("Mouse ScrollWheel");
        if (offset > offsetMax){
            offset = offsetMax;
        }else if (offset < offsetMin) {
            offset = 1;
        }

        Vector3 defaultPos = transform.localPosition;
        Vector3 directionNormalized = defaultPos.normalized;
        Vector3 currentPos = (directionNormalized * offset);
        float defaultDistance = offset;

        RaycastHit camhit;
        Vector3 dirTmp = transform.parent.TransformPoint(defaultPos) - playerCameraParent.position;
        if (Physics.SphereCast(playerCameraParent.position, collisionOffset, dirTmp, out camhit, defaultDistance)) {
            currentPos = (directionNormalized * (camhit.distance - collisionOffset));
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, currentPos, Time.deltaTime * cameraDamping);

        //movement
        movement = Vector3.Normalize(movement);
        movement = Quaternion.AngleAxis(rotation.y, Vector3.up) * movement;
        Vector3 startPos = playerParent.transform.position;
        Vector3 movePos = startPos + movement * movementSpeed;
        float dist = 0.0f;
        float maxdist = movementSpeed;
        while(maxdist > 0 && dist < maxdist){
            RaycastHit movehit;
            if (Physics.CapsuleCast(startPos + new Vector3(0,capsuleBotHeight,0), startPos + new Vector3(0,capsuleTopHeight,0), 
                capusleRadius, movement, out movehit, maxdist-dist)){
                Vector3 incomingVec = movehit.point - startPos;
                Vector3 reflectVec = Vector3.Reflect(incomingVec, movehit.normal);

                dist += movehit.distance;
                movePos = startPos + reflectVec * maxdist * 0.1f + movement * movehit.distance;
                startPos = movePos;
            }else{
                dist = maxdist;
                movePos = startPos + movement * (maxdist);
            }
        }
        playerParent.transform.position = Vector3.Lerp(playerParent.transform.position, movePos, Time.deltaTime * movementDamping);
        movement = Vector3.zero;
    }

    void Start(){
        offset = offsetMax;
    }

    void Update(){
        GetInput();
    }
}

