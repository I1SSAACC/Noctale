using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Cinemachine;

namespace StarterAssets
{
    public class SyncController : NetworkBehaviour
    {
        [SerializeField] private Transform target;

        private void Start()
        {
            if (isLocalPlayer)
            {
                CharacterController characterController = GetComponent<CharacterController>();
                characterController.enabled = true;

                ThirdPersonController thirdPersonController = GetComponent<ThirdPersonController>();
                thirdPersonController.enabled = true;

                PlayerInput playerInput = GetComponent<PlayerInput>();
                playerInput.enabled = true;

                GameObject playerFollowCamera = GameObject.Find("PlayerFollowCamera");

                CinemachineVirtualCamera camera = playerFollowCamera.GetComponent<CinemachineVirtualCamera>();
                camera.Follow = target;
            }
        }
    }
}