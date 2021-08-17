using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine.UI
{
    // Attached to the UI canvas that holds the hover health bar, handles displaying the hover health bar at the main camera.
    public class HoverHealthBar : MonoBehaviour
    {
        //to make sure the hover health bar is always looking at the camera
        [SerializeField, Tooltip("Main camera in the game.")]
        private Transform mainCamTransform = null;

        void Update()
        {
            //move the canvas in order to face the camera and look at it
            transform.LookAt(transform.position + mainCamTransform.rotation * Vector3.forward,
                mainCamTransform.rotation * Vector3.up);
        }
    }
}
