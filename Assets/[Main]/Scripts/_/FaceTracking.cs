using System.Collections;
using UnityEngine;

public class FaceTracking : MonoBehaviour
{
    // [SerializeField] private ObjectDetection objectDetection;
    // [SerializeField] private FaceDetection faceDetection;
    // [SerializeField] private GameObject sphere;
    // [SerializeField] private float sphereSizeMultiplier = 1.0f;

    // private void Start()
    // {
    //     // Configure the webcam
    //     // var webCam = GetComponent<WebCamInput>();
    //     // webCam.FlipHorizontal = true;
    //     // webCam.Play();
    // }

    // private void Update()
    // {
    //     // Check if the face is detected
    //     if (faceDetection.FaceDetections != null && faceDetection.FaceDetections.Count > 0)
    //     {
    //         // Get the bounding box of the face
    //         var boundingBox = faceDetection.FaceDetections[0].BoundingBox;

    //         // Get the center point of the bounding box
    //         var centerX = boundingBox.x + boundingBox.width / 2;
    //         var centerY = boundingBox.y + boundingBox.height / 2;

    //         // Convert the center point from image space to world space
    //         var centerWorld = objectDetection.ImageToWorldPoint(new Vector2(centerX, centerY));

    //         // Update the position and size of the sphere based on the face's position and size
    //         sphere.transform.position = centerWorld;
    //         sphere.transform.localScale = new Vector3(boundingBox.width, boundingBox.height, boundingBox.width) * sphereSizeMultiplier;
    //     }
    // }
}
