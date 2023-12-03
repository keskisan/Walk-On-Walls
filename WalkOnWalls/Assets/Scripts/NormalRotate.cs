
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class NormalRotate : UdonSharpBehaviour
{
    private VRCPlayerApi ownerPlayer = null; //owner if object in game

    [SerializeField]
    ChairController chairController;

    [SerializeField]
    float rayLength = 1.5f;

    [SerializeField]
    LayerMask floorLayer;

    [SerializeField]
    CapsuleCollider capsuleCollider;

    [SerializeField]
    float rotationSpeed = 40f;

    [SerializeField]
    float movementSpeedForward = 4f;

    [SerializeField]
    float movementSpeedSideways = 4f;

    [SerializeField]
    VRCStation vrcStation;

    private float forwardMovement = 0f;
    private float sidewayMovement = 0f;

    VRCPlayerApi localPlayer;

    [SerializeField]
    float applyGravityDistance = 0.2f;

    [SerializeField]
    float fallSpeed = 1f;

    [SerializeField]
    float jumpSpeed = 5f;

    [SerializeField]
    float rotationsSpeed = 5f;

    [SerializeField]
    float maxSpeed = 10f;

    [SerializeField]
    float jumpImpulse = 3f;

    bool isJumping = false;

    private Vector3 downVelocity;
    private Vector3 upVelocity;
    private Vector3 totalVelocity;

    Vector3 forward;
    Vector3 right;

    private Vector3 startPosition;

    bool onGround;

    private Quaternion desiredRotation;

    private void Start()
    {
        startPosition = transform.position;
        desiredRotation = transform.rotation;
        localPlayer = Networking.LocalPlayer;
        vrcStation.disableStationExit = true;
        SetOwnerPlayer();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        SetOwnerPlayer();
    }

    public void SetOwnerPlayer()
    {
        ownerPlayer = Networking.GetOwner(gameObject);
        if (ownerPlayer.isMaster)
        {
            if (this != chairController.chairs[0]) //returns to pool
            {
                ownerPlayer = null;
            }
        }
        else
        {
            //code for when someone gets it
        }
    }

    public void AddVelocity(Vector3 value)
    {
        totalVelocity += value;
    }

    private void FixedUpdate()
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player

        Vector3 transformDown = transform.up * -1f;

        CalculateNewforwardAndRightBasedOnOrientationAndPlayer();

        if (totalVelocity.magnitude > maxSpeed * Time.fixedDeltaTime) //limits max velocity
        {
            totalVelocity = totalVelocity.normalized * maxSpeed * Time.fixedDeltaTime;
        }

        CheckForCollisions(onGround);

        transform.position += totalVelocity;
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationsSpeed * Time.fixedDeltaTime);

        onGround = Physics.Raycast(transform.position + transform.up, transformDown, out RaycastHit hit, rayLength + 1f, floorLayer);
        if (onGround)
        {
            
            RotateToNormal(hit);


            ApplyGravity(hit);
            CheckJumping(hit);

            //newPosition += transform.forward * forwardMovement * movementSpeedForward * Time.deltaTime;
            //newPosition += transform.right * sidewayMovement * movementSpeedSideways * Time.deltaTime;

            totalVelocity = Vector3.zero;
            
            totalVelocity += downVelocity;
            totalVelocity += forward * forwardMovement * movementSpeedForward * Time.fixedDeltaTime;
            totalVelocity += right * sidewayMovement * movementSpeedSideways * Time.fixedDeltaTime;
            
            totalVelocity += upVelocity * Time.fixedDeltaTime;
        }

        Debug.DrawRay(transform.position, transformDown * rayLength, onGround? Color.green : Color.red); 

       
    }

    private void CalculateNewforwardAndRightBasedOnOrientationAndPlayer()
    {
        Quaternion playerRotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        Vector3 forwardRaw = playerRotation * Vector3.forward; //player head rotation space

        float projectedOnTransformUpScale = Vector3.Dot(forwardRaw, transform.up); //project on plane
        Vector3 projectedTransformUp = transform.up * projectedOnTransformUpScale;
        Vector3 newForward = forwardRaw - projectedTransformUp;

        Quaternion newRotation = Quaternion.LookRotation(newForward, transform.up);

        forward = newRotation * Vector3.forward; //player head rotation space
        right = newRotation * Vector3.right; //player head rotation space
    }

    private void CheckForCollisions(bool onGround)
    {
        float OffsetY = capsuleCollider.height * 0.5f - capsuleCollider.radius;
        Vector3 point1 = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y + OffsetY, capsuleCollider.center.z) + transform.position;
        Vector3 point2 = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y - OffsetY, capsuleCollider.center.z) + transform.position;
        Vector3 direction = totalVelocity;
        float rayLength = direction.magnitude;

        if (Physics.CapsuleCast(point1, point2, capsuleCollider.radius, direction.normalized, out RaycastHit hit, rayLength))
        {
            if (!onGround)
            {
                upVelocity = Vector3.zero;
                isJumping = false;
                RotateToNormal(hit); //flip around
            }

        }
    }

    /*private Vector3 CheckForCollisions(bool onGround)
    {
        float OffsetY = capsuleCollider.height * 0.5f - capsuleCollider.radius;
        Vector3 point1 = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y + OffsetY, capsuleCollider.center.z) + transform.position;
        Vector3 point2 = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y - OffsetY, capsuleCollider.center.z) + transform.position;
        Vector3 direction = totalVelocity;
        float rayLength = direction.magnitude;

        if (Physics.CapsuleCast(point1, point2, capsuleCollider.radius, direction.normalized, out RaycastHit hit, rayLength))
        {
            if (onGround)
            {
                //float magnitude = Vector3.Dot(direction, hit.normal); //project direction unto normal to get and eleminate part of vector that goes into collider
                //Vector3 collisionObstacleMove = hit.normal * -magnitude;

                Vector3 v = Vector3.Cross(transform.up, hit.normal);
                float magnitude = Vector3.Dot(direction, v);


                return -direction + magnitude * v;
            }
            else
            {
                upVelocity = Vector3.zero;
                isJumping = false;
                RotateToNormal(hit); //flip around
                return Vector3.zero;
            }
           
        }
        else
        {
            return Vector3.zero;
        }

    }*/

    private void CheckJumping(RaycastHit hit)
    {
        if (isJumping)
        {
            upVelocity = transform.up * jumpImpulse;
        }
    }

    private void RotateToNormal(RaycastHit hit)
    {
        float projectedOnNormalScale = Vector3.Dot(transform.forward, hit.normal); //project on plane
        Vector3 projectedNormal = hit.normal * projectedOnNormalScale;
        Vector3 newForward = transform.forward - projectedNormal;

        Quaternion newRotation = Quaternion.LookRotation(newForward, hit.normal);
        desiredRotation = newRotation;
    }

    private void ApplyGravity(RaycastHit hit)
    {
        if ( isJumping)
        {
            downVelocity = Vector3.zero;
            return;
        }

        if (hit.distance > applyGravityDistance + 1.2f)
        {
            downVelocity += transform.up * -1 * fallSpeed * Time.fixedDeltaTime;
            
        } else if (hit.distance < 1.2f) //below ground
        {
            downVelocity += transform.up * fallSpeed * Time.fixedDeltaTime;
        } else 
        {
            downVelocity = Vector3.zero;
        }
    }

    public override void InputLookHorizontal(float value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        float angle = value * Time.fixedDeltaTime * rotationSpeed;

        if (onGround)
        {
            transform.RotateAround(transform.position, transform.up, angle);
        }
        else //so can rotate when not on ground. on ground has 180 limit so transform rotate better there
        {
            Quaternion tmp = Quaternion.AngleAxis(angle, transform.up);
            desiredRotation *= tmp;
        }
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        forwardMovement = value;  
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        sidewayMovement = value;
        
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        if (value)
        {
            isJumping = true;
        }     
    }

    public void WalkOnWalls()
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        RescaleCapsuleCollider();

        totalVelocity = Vector3.zero;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        vrcStation.UseStation(localPlayer);

        isJumping = false;
        upVelocity = Vector3.zero;
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        
        transform.position = startPosition;
    }


    private void RescaleCapsuleCollider()
    {
        Vector3 feetPos = localPlayer.GetPosition();
        Vector3 EyePos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

        Vector3 height = EyePos - feetPos;

        capsuleCollider.height = height.magnitude;
        capsuleCollider.center = new Vector3(0f, height.magnitude * 0.5f, 0f);
    }

   
}




/*private VRCPlayerApi ownerPlayer = null; //owner if object in game

    [SerializeField]
    ChairController chairController;

    [SerializeField]
    float rayLength = 1.5f;

    [SerializeField]
    LayerMask floorLayer;

    [SerializeField]
    CapsuleCollider capsuleCollider;

    [SerializeField]
    float rotationSpeed = 40f;

    [SerializeField]
    float movementSpeedForward = 4f;

    [SerializeField]
    float movementSpeedSideways = 4f;

    [SerializeField]
    VRCStation vrcStation;

    private float forwardMovement = 0f;
    private float sidewayMovement = 0f;

    VRCPlayerApi localPlayer;

    [SerializeField]
    float applyGravityDistance = 0.1f;

    [SerializeField]
    float fallSpeed = 1f;

    [SerializeField]
    float jumpSpeed = 5f;

    [SerializeField]
    float rotationsSpeed = 5f;

    bool isJumping = false;

    private Vector3 downVelocity;
    private Vector3 upVelocity;
    private Vector3 totalVelocity;

    Vector3 forward;
    Vector3 right;

    bool onGround;

    private Quaternion desiredRotation;

    private void Start()
    {
        desiredRotation = transform.rotation;
        localPlayer = Networking.LocalPlayer;
        vrcStation.disableStationExit = true;
        SetOwnerPlayer();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        SetOwnerPlayer();
    }

    public void SetOwnerPlayer()
    {
        ownerPlayer = Networking.GetOwner(gameObject);
        if (ownerPlayer.isMaster)
        {
            if (this != chairController.chairs[0]) //returns to pool
            {
                ownerPlayer = null;
            }
        }
        else
        {
            //code for when someone gets it
        }
    }

    public void AddVelocity(Vector3 value)
    {
        totalVelocity += value;
    }

    private void Update()
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player

        Vector3 transformDown = transform.up * -1f;

        CalculateNewforwardAndRightBasedOnOrientationAndPlayer();

        totalVelocity += CheckForCollisions(onGround);

        transform.position += totalVelocity;
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationsSpeed * Time.deltaTime);

        onGround = Physics.Raycast(transform.position, transformDown, out RaycastHit hit, rayLength, floorLayer);
        if (onGround)
        {
            RotateToNormal(hit);


            ApplyGravity(hit);
            CheckJumping(hit);

            //newPosition += transform.forward * forwardMovement * movementSpeedForward * Time.deltaTime;
            //newPosition += transform.right * sidewayMovement * movementSpeedSideways * Time.deltaTime;

            totalVelocity = Vector3.zero;
            totalVelocity += forward * forwardMovement * movementSpeedForward * Time.deltaTime;
            totalVelocity += right * sidewayMovement * movementSpeedSideways * Time.deltaTime;

            totalVelocity += upVelocity * Time.deltaTime;
        }

        Debug.DrawRay(transform.position, transformDown * rayLength, onGround? Color.green : Color.red); 

       
    }

    private void CalculateNewforwardAndRightBasedOnOrientationAndPlayer()
    {
        Quaternion playerRotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        Vector3 forwardRaw = playerRotation * Vector3.forward; //player head rotation space

        float projectedOnTransformUpScale = Vector3.Dot(forwardRaw, transform.up); //project on plane
        Vector3 projectedTransformUp = transform.up * projectedOnTransformUpScale;
        Vector3 newForward = forwardRaw - projectedTransformUp;

        Quaternion newRotation = Quaternion.LookRotation(newForward, transform.up);

        forward = newRotation * Vector3.forward; //player head rotation space
        right = newRotation * Vector3.right; //player head rotation space
    }

    private Vector3 CheckForCollisions(bool onGround)
    {
        float OffsetY = capsuleCollider.height * 0.5f - capsuleCollider.radius;
        Vector3 point1 = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y + OffsetY, capsuleCollider.center.z) + transform.position;
        Vector3 point2 = new Vector3(capsuleCollider.center.x, capsuleCollider.center.y - OffsetY, capsuleCollider.center.z) + transform.position;
        Vector3 direction = totalVelocity;
        float rayLength = direction.magnitude;

        if (Physics.CapsuleCast(point1, point2, capsuleCollider.radius, direction.normalized, out RaycastHit hit, rayLength))
        {
            if (onGround)
            {
                //float magnitude = Vector3.Dot(direction, hit.normal); //project direction unto normal to get and eleminate part of vector that goes into collider
                //Vector3 collisionObstacleMove = hit.normal * -magnitude;

                Vector3 v = Vector3.Cross(transform.up, hit.normal);
                float magnitude = Vector3.Dot(direction, v);


                return -direction + magnitude * v;
            }
            else
            {
                upVelocity = Vector3.zero;
                isJumping = false;
                RotateToNormal(hit); //flip around
                return Vector3.zero;
            }
           
        }
        else
        {
            return Vector3.zero;
        }

    }

    private void CheckJumping(RaycastHit hit)
    {
        if (isJumping)
        {
            upVelocity = transform.up * 5f;
        }
    }

    private void RotateToNormal(RaycastHit hit)
    {
        float projectedOnNormalScale = Vector3.Dot(transform.forward, hit.normal); //project on plane
        Vector3 projectedNormal = hit.normal * projectedOnNormalScale;
        Vector3 newForward = transform.forward - projectedNormal;

        Quaternion newRotation = Quaternion.LookRotation(newForward, hit.normal);
        desiredRotation = newRotation;
    }

    private void ApplyGravity(RaycastHit hit)
    {
        if ( isJumping)
        {
            downVelocity = Vector3.zero;
            return;
        }

        if (hit.distance > applyGravityDistance)
        {
            downVelocity += transform.up * -1 * fallSpeed * Time.deltaTime;
            transform.position += downVelocity;
        } else
        {
            downVelocity = Vector3.zero;
        }
    }

    public override void InputLookHorizontal(float value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        float angle = value * Time.deltaTime * rotationSpeed;

        if (onGround)
        {
            transform.RotateAround(transform.position, transform.up, angle);
        }
        else //so can rotate when not on ground. on ground has 180 limit so transform rotate better there
        {
            Quaternion tmp = Quaternion.AngleAxis(angle, transform.up);
            desiredRotation *= tmp;
        }
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        forwardMovement = value;  
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        sidewayMovement = value;
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        if (value)
        {
            isJumping = true;
        }     
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (ownerPlayer == null) return;
        if (ownerPlayer != Networking.LocalPlayer) return; //only for local player
        RescaleCapsuleCollider();

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        vrcStation.UseStation(localPlayer);

        isJumping = false;
        upVelocity = Vector3.zero;
    }

    private void RescaleCapsuleCollider()
    {
        Vector3 feetPos = localPlayer.GetPosition();
        Vector3 EyePos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

        Vector3 height = EyePos - feetPos;

        capsuleCollider.height = height.magnitude;
        capsuleCollider.center = new Vector3(0f, height.magnitude, 0f);
    }
*/
