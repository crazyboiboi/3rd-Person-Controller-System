using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    [Header("Targets")]
    public List<Transform> targets;
    public float detectionRadius = 10f;
    public Transform nearestTarget;
    public Transform lockedOnTarget;

    PlayerController controller;
    Camera cam;
    Vector2 camCenter;
    int enemyLayer;

    bool mCanChangeTarget = true;

    void Start()
    {
        cam = Camera.main;
        controller = GetComponent<PlayerController>();
        camCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        targets = new List<Transform>();
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void Update()
    {
        HandleTargetAim();

        FindTargetsInPlayerFOV();
        UpdateNearestTarget();
    }


    void HandleTargetAim()
    {
        if (lockedOnTarget != null)
        {
            if(!IsInPlayerFOV(lockedOnTarget.position))
            {
                lockedOnTarget = null;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            mCanChangeTarget = true;
        }

        if(Input.GetMouseButton(1))
        {
            if(mCanChangeTarget && nearestTarget != null)
            {
                lockedOnTarget = nearestTarget;
            }
        }
    }


    //Finds all targets visible to player's FOV
    void FindTargetsInPlayerFOV()
    {
        //Detects and store colliders within a set radius (regardless of whether it is behind the player or not)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, 1 << enemyLayer);

        //When no targets are nearby, clears out the target just in case
        if (hitColliders.Length == 0)
            targets.Clear();

        //Check for all colliders if they are in the camera's FOV, we should add it to the target list
        foreach (Collider c in hitColliders)
        {
            if (IsInPlayerFOV(c.transform.position))
            {
                if (!targets.Contains(c.transform))
                    targets.Add(c.transform);
            }
            else
            {
                if (targets.Contains(c.transform))
                    targets.Remove(c.transform);
            }
        }
    }

    //Checks whether if the target is within player's FOV
    bool IsInPlayerFOV(Vector3 targetPos)
    {
        //Gets the target's position in world space in relation to camera
        Vector3 screenPoint = cam.WorldToViewportPoint(targetPos);

        //If it is within FOV, the return value should be between 0 and 1 only
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    //Constantly updates the nearest target
    void UpdateNearestTarget()
    {
        //If target list is empty, there should be no nearest target and we do not have to compute minimum distance
        if (targets.Count == 0)
        {
            nearestTarget = null;
            lockedOnTarget = null;
            return;
        }

        //If player is aiming at a target, lock the target if it is still within the player's FOV
        //if (controller._isAiming)
        //{
        //    if(nearestTarget != null)
        //    {
        //        if(!IsInPlayerFOV(nearestTarget.position))
        //        {
        //            nearestTarget = null;
        //        }
        //    }
        //    return;
        //}

        float minDistance = Mathf.Infinity;

        //Compare all the targets distance to player center FOV and set the nearest target
        foreach (Transform t in targets)
        {
            float distance = CalculateDistance(t.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTarget = t;
            }
        }
    }

    //Calculates distance of target nearest to the camera's center
    float CalculateDistance(Vector3 targetPos)
    {
        Vector2 screenPoint = cam.WorldToScreenPoint(targetPos);
        return Vector2.Distance(screenPoint, camCenter);
    }
}
