using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Other Settings")]
    public bool lockCursor = false;
    public bool drawClipPointLines = false;

    [Header("Target Info")]
    public Transform target;
    public LayerMask collisionLayer;
    public float distanceToTarget = 4f;
    public float distanceOffset = 0.2f;

    [Header("Control Settings")]
    public Vector2 sensitivity;
    public bool invertX;
    public bool invertY;
    public Vector2 pitchMixMax = new Vector2(-30, 60);
    public float rotationSmoothTime = 0.2f;
    public float camSmoothTime = 0.05f;


    float pitch, yaw, adjustedDistance;
    bool colliding = false;
    Camera cam;

    Vector3 rotationSmoothVelocity;
    Vector3 desiredCamPosition;
    Vector3 currentRotation;
    Vector3 targetPosition;
    Vector3[] desiredClipPoints = new Vector3[4];

    void Start()
    {
        //Initialise camera position and rotation at the start
        UpdateCameraPosition();
        UpdateCameraRotation();

        cam = Camera.main;
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        UpdateClipPoints(transform.position, transform.rotation, ref desiredClipPoints);
    }

    void Update()
    {
        CalculateMouseInput();

        for (int i = 0; i < 4; i++)
        {
            if (drawClipPointLines)
                Debug.DrawLine(targetPosition, desiredClipPoints[i], Color.green);
        }

        UpdateCameraRotation();
        targetPosition = target.position;
        desiredCamPosition = target.position - transform.forward * distanceToTarget;

        UpdateClipPoints(desiredCamPosition, transform.rotation, ref desiredClipPoints);

        CheckClipPointsCollision(desiredClipPoints, targetPosition);

        colliding = CheckClipPointsCollision(desiredClipPoints, targetPosition);
        adjustedDistance = CalculateAdjustedDistance(targetPosition);
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
    }

    #region Methods
    //Retrieves the mouse input and calculates new yaw and pitch value
    void CalculateMouseInput()
    {
        yaw += Input.GetAxis("Mouse X") * sensitivity.x * (invertX ? -1 : 1);
        pitch += Input.GetAxis("Mouse Y") * sensitivity.y * (invertY ? -1 : 1);

        pitch = Mathf.Clamp(pitch, pitchMixMax.x, pitchMixMax.y);
    }

    //Updates camera position based on whether the camera has collided or not
    void UpdateCameraPosition()
    {
        if (colliding)
            transform.position = targetPosition - transform.forward * adjustedDistance;
        else
            transform.position = desiredCamPosition;
    }

    //Updates camera rotation based on mouse input
    void UpdateCameraRotation()
    {
        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector2(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothTime);
        transform.eulerAngles = currentRotation;
    }

    //Updates the four clips points of the camera based on camera position
    void UpdateClipPoints(Vector3 cameraPos, Quaternion cameraRotation, ref Vector3[] toStore)
    {
        toStore = new Vector3[4];

        float z = cam.nearClipPlane;
        float x = Mathf.Tan(cam.fieldOfView / 3.41f) * z;
        float y = x / cam.aspect;

        //Top left 
        toStore[0] = (cameraRotation * new Vector3(-x, y, z)) + cameraPos;

        //Top right
        toStore[1] = (cameraRotation * new Vector3(x, y, z)) + cameraPos;

        //Bottom left
        toStore[2] = (cameraRotation * new Vector3(-x, -y, z)) + cameraPos;

        //Bottom right
        toStore[3] = (cameraRotation * new Vector3(x, -y, z)) + cameraPos;
    }

    //Return true if any one of the clip points collide with the specified layer using raycasting
    bool CheckClipPointsCollision(Vector3[] clipPoints, Vector3 fromPos)
    {
        foreach (Vector3 clipPoint in clipPoints)
        {
            Ray ray = new Ray(fromPos, clipPoint - fromPos);
            float distance = Vector3.Distance(clipPoint, fromPos);
            if (Physics.Raycast(ray, distance, collisionLayer))
                return true;
        }
        return false;
    }

    //Calculate a new distance for the camera to be in
    float CalculateAdjustedDistance(Vector3 fromPos)
    {
        float distance = int.MaxValue;

        //Find the minimum distance of the clip points that collided with an obstacle
        foreach (Vector3 clipPoint in desiredClipPoints)
        {
            Ray ray = new Ray(fromPos, clipPoint - fromPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
                distance = Mathf.Min(distance, hit.distance);
        }

        //If distance remains unchanged, it means it hasn't collide with anything
        if (distance == int.MaxValue)
            return 0;

        //This is to fix a bug where the camera isn't responsive enough to reset to its 
        //original distance when the calculated min distance is greater
        if (distance > distanceToTarget)
            return distanceToTarget;

        //If we have a min distance, return it with an offset
        return distance + distanceOffset;
    }
    #endregion


}
