﻿namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;
    using GoogleARCore;

    /// <summary>
    /// Controlls the HelloAR example.
    /// </summary>
    public class ScanningAndTrackingManager : MonoBehaviour
    {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera.
        /// </summary>
        public Camera m_firstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject m_trackedPlanePrefab;

        /// <summary>
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        /// </summary>
        public GameObject m_searchingForPlaneUI;

        public GameObject m_goOutOfFocusUI;

        public GameObject m_goCameraNotCenterUI;

        public GameObject m_goScanningSuccesfullUI;

        private List<TrackedPlane> m_newPlanes = new List<TrackedPlane>();

        private List<TrackedPlane> m_allPlanes = new List<TrackedPlane>();

        // bool to check, if the player has the cube for the scanning in focus
        [SerializeField]
        private bool m_bIsHitObjectFocus = false;

        public void HitObjectIsFocus()
        {
            m_bIsHitObjectFocus = true;
        }

        public void HitObjectIsNotFocus()
        {
            m_bIsHitObjectFocus = false;
        }

        public bool IsFocus()
        {
            return m_bIsHitObjectFocus;
        }

        // the camera must be in the center for the scanning
        private bool m_bIsCameraInTheCenter = false;

        public bool IsCameraInCenter()
        {
            return m_bIsCameraInTheCenter;
        }

        // will be true after the scanning went full circle (360°)
        private bool m_bIsFullCircleCheck = false;

        public CameraFocusOnRaycastObject m_scrCameraFocusRaycast;
        public GameObject m_goFocusObject;
        public GameObject m_goCameraInCenter;

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            _QuitOnConnectionErrors();

            // The tracking state must be FrameTrackingState.Tracking in order to access the Frame.
            if (Frame.TrackingState != FrameTrackingState.Tracking)
            {
                const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
                Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Frame.GetNewPlanes(ref m_newPlanes);

            if (m_bIsHitObjectFocus && !m_bIsFullCircleCheck && m_bIsCameraInTheCenter)
            {
                // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
                for (int i = 0; i < m_newPlanes.Count; i++)
                {
                    // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                    // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                    // coordinates.
                    GameObject planeObject = Instantiate(m_trackedPlanePrefab, Vector3.zero, Quaternion.identity,
                        transform);
                    planeObject.GetComponent<PlaneVisualizer>().SetTrackedPlane(m_newPlanes[i]);

                    // Apply a grid rotation.
                    planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
                }
            }

            m_goOutOfFocusUI.SetActive(m_bIsHitObjectFocus);

            // Disable the snackbar UI when no planes are valid.
            bool showSearchingUI = true;
            Frame.GetAllPlanes(ref m_allPlanes);
            for (int i = 0; i < m_allPlanes.Count; i++)
            {
                if (m_allPlanes[i].IsValid)
                {
                    showSearchingUI = false;
                    break;
                }
            }

            m_searchingForPlaneUI.SetActive(showSearchingUI);
        }

        public void CameraEnterCenter()
        {
            Debug.Log("Camera in Center");
            m_goCameraNotCenterUI.SetActive(false);
            m_bIsCameraInTheCenter = true;
        }

        public void CameraExitCenter()
        {
            Debug.Log("Camera not in Center");
            m_goCameraNotCenterUI.SetActive(true);
            m_bIsCameraInTheCenter = false;
        }

        public void FullCircleCompleted()
        {
            m_bIsFullCircleCheck = true;

            Destroy(m_goFocusObject);
            Destroy(m_goCameraInCenter);
            Destroy(m_scrCameraFocusRaycast);

            m_goScanningSuccesfullUI.SetActive(true);
            m_goOutOfFocusUI.SetActive(false);
            m_goCameraNotCenterUI.SetActive(false);
            m_searchingForPlaneUI.SetActive(false);

            Destroy(gameObject);
        }


        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            // Do not update if ARCore is not tracking.
            if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                Application.Quit();
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                Application.Quit();
            }
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        /// <param name="length">Toast message time length.</param>
        private static void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}
