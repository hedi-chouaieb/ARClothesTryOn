using UnityEngine;
using Mediapipe.Unity.Tutorial;

public class FaceTracker : MonoBehaviour
{
    public FaceMesh faceMesh;
    public Transform headTransform;
    
    private void Update()
    {
        // Vector3[] landmarks = faceMesh.vertices;
        
        // Quaternion headRotation = GetFaceRotation(landmarks);
        // headTransform.rotation = headRotation;
    }

    private Quaternion GetFaceRotation(Vector3[] landmarks)
    {
        // Find the vectors between the nose and right eye and between the nose and left eye
        Vector3 nosePos = landmarks[168];
        Vector3 rightEyePos = landmarks[33];
        Vector3 leftEyePos = landmarks[263];
        Vector3 rightToNose = (nosePos - rightEyePos).normalized;
        Vector3 leftToNose = (nosePos - leftEyePos).normalized;

        // Calculate the average of the two vectors to get the forward direction of the head
        Vector3 headForward = (rightToNose + leftToNose) / 2f;

        // Calculate the up direction of the head by taking the cross product of the forward direction and the world up vector
        Vector3 headUp = Vector3.Cross(headForward, Vector3.up).normalized;

        // Calculate the right direction of the head by taking the cross product of the forward and up directions
        Vector3 headRight = Vector3.Cross(headForward, headUp).normalized;

        // Create a rotation matrix based on the forward, up, and right directions of the head
        Matrix4x4 rotationMatrix = new Matrix4x4();
        rotationMatrix.SetColumn(0, new Vector4(headRight.x, headRight.y, headRight.z, 0));
        rotationMatrix.SetColumn(1, new Vector4(headUp.x, headUp.y, headUp.z, 0));
        rotationMatrix.SetColumn(2, new Vector4(headForward.x, headForward.y, headForward.z, 0));
        rotationMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

        // Convert the rotation matrix to a quaternion and return it
        Quaternion headRotation = rotationMatrix.rotation;
        return headRotation;
    }
}