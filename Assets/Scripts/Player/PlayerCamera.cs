using UnityEngine;

namespace Horror.Player
{
    /// <summary>
    /// Heavy first-person camera. Put this on a "CameraRoot" child of the player, with the Camera
    /// as a child of CameraRoot. Yaw turns the player body, pitch turns CameraRoot, and all the
    /// feel effects (headbob, sway, strafe tilt, landing dip, breathing) go on the Camera itself
    /// so they never fight the aim.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerInputReader input;
        [Tooltip("The Camera (child of this object) that receives bob and sway.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Look")]
        [SerializeField] private float sensitivity = 2f;
        [SerializeField] private bool invertY;
        [Tooltip("How quickly the view catches up with the mouse. Lower feels heavier, higher is snappier.")]
        [SerializeField] private float lookSmoothing = 14f;
        [SerializeField] private float maxLookUp = 80f;
        [SerializeField] private float maxLookDown = 80f;
        [Tooltip("Prone limits how far up you can look (you're lying on your belly).")]
        [SerializeField] private float proneMaxLookUp = 35f;
        [Range(0f, 1f)] [SerializeField] private float proneLookMultiplier = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float sprintLookMultiplier = 0.85f;

        [Header("Eye Height")]
        [Tooltip("Distance from the top of the capsule down to the eyes.")]
        [SerializeField] private float eyeOffsetFromTop = 0.12f;
        [SerializeField] private float eyeHeightSmoothing = 8f;

        [Header("Headbob")]
        [Tooltip("Bob cycles per metre travelled, so the rhythm follows actual speed. Each cycle is two steps.")]
        [SerializeField] private float bobCyclesPerMetre = 0.32f;
        [SerializeField] private float bobVertical = 0.045f;
        [SerializeField] private float bobHorizontal = 0.03f;
        [SerializeField] private float bobRoll = 0.6f;
        [SerializeField] private float sprintBobMultiplier = 1.6f;
        [SerializeField] private float crouchBobMultiplier = 0.6f;
        [SerializeField] private float proneBobMultiplier = 0.9f;
        [SerializeField] private float bobBlendSpeed = 6f;

        [Header("Look Sway")]
        [Tooltip("Degrees the camera lags behind fast mouse movement.")]
        [SerializeField] private float swayAmount = 1.2f;
        [SerializeField] private float swayMaxAngle = 4f;
        [SerializeField] private float swaySmoothing = 6f;

        [Header("Strafe Tilt")]
        [SerializeField] private float strafeTiltAngle = 1.6f;
        [SerializeField] private float strafeTiltSmoothing = 5f;

        [Header("Landing")]
        [Tooltip("Metres the camera dips per m/s of landing speed.")]
        [SerializeField] private float landingDipPerSpeed = 0.025f;
        [SerializeField] private float landingMaxDip = 0.35f;
        [SerializeField] private float landingSpringStiffness = 90f;
        [SerializeField] private float landingSpringDamping = 14f;
        [SerializeField] private float jumpKick = 0.04f;

        [Header("Breathing")]
        [SerializeField] private float breatheRate = 0.25f;
        [SerializeField] private float breatheAmount = 0.008f;
        [Tooltip("Breathing gets this much stronger and faster as stamina runs out.")]
        [SerializeField] private float exhaustedBreatheMultiplier = 3.5f;

        private float targetYaw, targetPitch;
        private float yaw, pitch;
        private float eyeHeight;
        private float bobPhase, bobWeight;
        private float breathePhase;
        private Vector2 sway;
        private float strafeTilt;
        private float landingOffset, landingVelocity;
        private Vector3 cameraRestPosition;

        private void Awake()
        {
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (input == null) input = GetComponentInParent<PlayerInputReader>();
            if (cameraTransform == null && GetComponentInChildren<Camera>() != null)
                cameraTransform = GetComponentInChildren<Camera>().transform;

            yaw = targetYaw = movement.transform.eulerAngles.y;
            pitch = targetPitch = 0f;
            if (cameraTransform != null) cameraRestPosition = cameraTransform.localPosition;
        }

        private void Start()
        {
            // In Start rather than Awake so PlayerMovement has set its height first.
            eyeHeight = movement.CurrentHeight - eyeOffsetFromTop;
        }

        private void OnEnable()
        {
            movement.Landed += OnLanded;
            movement.Jumped += OnJumped;
        }

        private void OnDisable()
        {
            movement.Landed -= OnLanded;
            movement.Jumped -= OnJumped;
        }

        // LateUpdate so the camera reads the body after it has moved this frame.
        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector2 look = input != null ? input.Look : Vector2.zero;

            UpdateLook(look, dt);
            UpdateEyeHeight(dt);
            UpdateEffects(look, dt);
        }

        private void UpdateLook(Vector2 look, float dt)
        {
            float scale = sensitivity;
            if (movement.CurrentStance == PlayerMovement.Stance.Prone) scale *= proneLookMultiplier;
            if (movement.IsSprinting) scale *= sprintLookMultiplier;

            targetYaw += look.x * scale;
            targetPitch += look.y * scale * (invertY ? 1f : -1f);

            float lookUp = movement.CurrentStance == PlayerMovement.Stance.Prone ? proneMaxLookUp : maxLookUp;
            // Pitch is negative when looking up in Unity, so the "up" limit is the negative bound.
            targetPitch = Mathf.Clamp(targetPitch, -lookUp, maxLookDown);

            float t = 1f - Mathf.Exp(-lookSmoothing * dt);
            yaw = Mathf.LerpAngle(yaw, targetYaw, t);
            pitch = Mathf.Lerp(pitch, targetPitch, t);

            movement.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateEyeHeight(float dt)
        {
            float target = movement.CurrentHeight - eyeOffsetFromTop;
            eyeHeight = Mathf.Lerp(eyeHeight, target, 1f - Mathf.Exp(-eyeHeightSmoothing * dt));
            transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        }

        private void UpdateEffects(Vector2 look, float dt)
        {
            if (cameraTransform == null) return;

            // Headbob, driven by distance covered on the ground.
            Vector3 velocity = movement.PlanarVelocity;
            float speed = velocity.magnitude;
            bool moving = movement.IsGrounded && speed > 0.1f;

            float stanceMul = movement.CurrentStance == PlayerMovement.Stance.Crouching ? crouchBobMultiplier
                : movement.CurrentStance == PlayerMovement.Stance.Prone ? proneBobMultiplier
                : movement.IsSprinting ? sprintBobMultiplier : 1f;

            float targetWeight = moving ? Mathf.Clamp01(speed / Mathf.Max(movement.WalkSpeed, 0.01f)) * stanceMul : 0f;
            bobWeight = Mathf.Lerp(bobWeight, targetWeight, 1f - Mathf.Exp(-bobBlendSpeed * dt));
            if (moving) bobPhase += speed * bobCyclesPerMetre * dt * Mathf.PI * 2f;

            // Vertical bob runs at twice the side-to-side rate: one dip per footstep.
            Vector3 bob = new Vector3(
                Mathf.Cos(bobPhase) * bobHorizontal,
                -Mathf.Abs(Mathf.Sin(bobPhase)) * bobVertical,
                0f) * bobWeight;
            float bobRollAngle = Mathf.Cos(bobPhase) * bobRoll * bobWeight;

            // Breathing, heavier when out of breath.
            float exhaustion = movement.Stamina != null ? 1f - movement.Stamina.Normalized : 0f;
            float breatheMul = Mathf.Lerp(1f, exhaustedBreatheMultiplier, exhaustion * exhaustion);
            breathePhase += breatheRate * breatheMul * dt * Mathf.PI * 2f;
            float breathe = Mathf.Sin(breathePhase) * breatheAmount * breatheMul;

            // Sway: the camera trails behind quick mouse moves, like a heavy head on a neck.
            Vector2 targetSway = Vector2.ClampMagnitude(new Vector2(-look.y, look.x) * swayAmount, swayMaxAngle);
            if (invertY) targetSway.x = -targetSway.x;
            sway = Vector2.Lerp(sway, -targetSway, 1f - Mathf.Exp(-swaySmoothing * dt));

            // Lean slightly into strafes.
            float lateral = Vector3.Dot(velocity, movement.transform.right);
            float targetTilt = -lateral / Mathf.Max(movement.SprintSpeed, 0.01f) * strafeTiltAngle * 2f;
            strafeTilt = Mathf.Lerp(strafeTilt, targetTilt, 1f - Mathf.Exp(-strafeTiltSmoothing * dt));

            // Landing dip: damped spring back to rest.
            float springForce = -landingSpringStiffness * landingOffset - landingSpringDamping * landingVelocity;
            landingVelocity += springForce * dt;
            landingOffset += landingVelocity * dt;

            cameraTransform.localPosition = cameraRestPosition + bob + Vector3.up * (breathe + landingOffset);
            cameraTransform.localRotation = Quaternion.Euler(sway.x, sway.y, strafeTilt + bobRollAngle + sway.y * 0.5f);
        }

        private void OnLanded(float impactSpeed)
        {
            float dip = Mathf.Min(impactSpeed * landingDipPerSpeed, landingMaxDip);
            // Kick the spring downward; the spring does the rest.
            landingVelocity -= dip * landingSpringDamping;
        }

        private void OnJumped()
        {
            landingVelocity -= jumpKick * landingSpringDamping;
        }
    }
}
