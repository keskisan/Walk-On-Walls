
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class FireExtinguisher : UdonSharpBehaviour
{
    [SerializeField]
    ChairController chairController;

    [SerializeField]
    float velocity = 3f;

    [SerializeField]
    VRCPickup thisPickup;

    VRCPlayerApi localPlayer;

    public void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }


    public override void OnPickupUseDown()
    {
        if (chairController.LocalPlayerChair != null)
        {
            chairController.LocalPlayerChair.AddVelocity(transform.forward * velocity * Time.deltaTime);
            /*if (localPlayer.IsUserInVR())
            {
                if (thisPickup.currentHand == VRC_Pickup.PickupHand.Left)
                {
                    Quaternion handRotation = localPlayer.GetBoneRotation(HumanBodyBones.LeftHand);
                    chairController.LocalPlayerChair.AddVelocity(handRotation * Vector3.forward * velocity * Time.deltaTime);
                }
                else
                {
                    Quaternion handRotation = localPlayer.GetBoneRotation(HumanBodyBones.RightHand);
                    chairController.LocalPlayerChair.AddVelocity(handRotation * Vector3.forward * velocity * Time.deltaTime);
                }
            }
            else
            {
                Quaternion playerView = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                chairController.LocalPlayerChair.AddVelocity(playerView * Vector3.forward * velocity * Time.deltaTime);
            }   */
        }
    }
}
