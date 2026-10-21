using UnityEngine;

namespace Horror.Player
{
    /// <summary>
    /// Bare-bones on-screen readout of stamina, stance and speed for testing.
    /// Remove it (or replace it with a real UI) once you have a proper HUD.
    /// </summary>
    public class PlayerDebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;

        private Texture2D pixel;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            pixel = Texture2D.whiteTexture;
        }

        private void OnGUI()
        {
            if (movement == null) return;

            const float x = 20f, width = 220f, height = 12f;
            float y = Screen.height - 70f;

            PlayerStamina stamina = movement.Stamina;
            if (stamina != null)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(x, y, width, height), pixel);
                GUI.color = stamina.IsExhausted ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.85f, 0.85f, 0.8f);
                GUI.DrawTexture(new Rect(x, y, width * stamina.Normalized, height), pixel);
                GUI.color = Color.white;
            }

            Vector3 v = movement.PlanarVelocity;
            GUI.Label(new Rect(x, y + 18f, 400f, 22f),
                $"{movement.CurrentStance}{(movement.IsSprinting ? " (sprinting)" : "")}   {v.magnitude:0.0} m/s   " +
                $"{(movement.IsGrounded ? "grounded" : "airborne")}");
        }
    }
}
