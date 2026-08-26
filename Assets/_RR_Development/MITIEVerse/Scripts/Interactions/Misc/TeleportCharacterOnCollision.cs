using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TeleportCharacterOnCollision : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private List<Collider> colliders;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (colliders.Contains(hit.collider))
        {
            transform.position = destination.position;
        }
    }
}
