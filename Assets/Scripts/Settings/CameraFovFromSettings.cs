using UnityEngine;

namespace Horror.Settings
{
    /// <summary>
    /// Put this on the player's Camera and it follows the FOV slider in Settings.
    /// The setting is horizontal FOV (what players expect from PC games); Unity cameras use
    /// vertical FOV, so it's converted using the screen's aspect ratio.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFovFromSettings : MonoBehaviour
    {
        [Tooltip("Added on top of the setting, e.g. a small zoom-out while sprinting. Other scripts can change this.")]
        public float fovOffset;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        // Every frame is cheap and also handles the window being resized (aspect ratio changes).
        private void LateUpdate()
        {
            float horizontal = Mathf.Clamp(GameSettings.Current.fieldOfView + fovOffset, 30f, 170f);
            float vertical = Camera.HorizontalToVerticalFieldOfView(horizontal, cam.aspect);
            if (!Mathf.Approximately(cam.fieldOfView, vertical))
                cam.fieldOfView = vertical;
        }
    }
}
