using System;
using UnityEngine;

namespace Horror.Player
{
    /// <summary>
    /// Heavy first-person movement on a CharacterController: slow acceleration and braking,
    /// limited air control, a weighty jump, sprinting that costs stamina, and three stances
    /// (standing, crouching, prone) with ceiling checks so the player can't stand up into geometry.
    /// The GameObject's pivot is treated as the player's feet.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        public enum Stance { Standing, Crouching, Prone }

        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerStamina stamina;

        [Header("Speeds (m/s)")]
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float sprintSpeed = 4.8f;
        [SerializeField] private float crouchSpeed = 1.4f;
        [SerializeField] private float proneSpeed = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float backwardMultiplier = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float strafeMultiplier = 0.85f;
        [Tooltip("Speed multiplier while exhausted (stamina ran dry).")]
        [Range(0f, 1f)] [SerializeField] private float exhaustedMultiplier = 0.8f;

        [Header("Inertia (m/s²)")]
        [Tooltip("How fast the player speeds up. Lower feels heavier.")]
        [SerializeField] private float acceleration = 6f;
        [Tooltip("How fast the player brakes when letting go of the keys.")]
        [SerializeField] private float deceleration = 7f;
        [Tooltip("Fraction of acceleration available in the air.")]
        [Range(0f, 1f)] [SerializeField] private float airControl = 0.15f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 0.85f;
        [SerializeField] private float gravity = -22f;
        [Tooltip("Extra gravity while falling so jumps drop like a heavy body instead of floating.")]
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float jumpCooldown = 0.45f;
        [Tooltip("Grace period after walking off a ledge during which a jump still works.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("How long a jump press is remembered before landing.")]
        [SerializeField] private float jumpBufferTime = 0.15f;
        [Tooltip("Landing faster than this (m/s) costs horizontal momentum.")]
        [SerializeField] private float hardLandingSpeed = 6f;
        [Range(0f, 1f)] [SerializeField] private float hardLandingSpeedKept = 0.35f;

        [Header("Stamina Costs")]
        [SerializeField] private float sprintDrainPerSecond = 18f;
        [SerializeField] private float jumpCost = 15f;
        [SerializeField] private float crouchRegenMultiplier = 1.25f;
        [SerializeField] private float proneRegenMultiplier = 1.6f;

        [Header("Stances")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [Tooltip("CharacterController capsules stay upright, so prone is a short capsule. Can't be lower than 2 x radius.")]
        [SerializeField] private float proneHeight = 0.6f;
        [Tooltip("How fast the body height changes between stances (m/s). Lower feels heavier.")]
        [SerializeField] private float stanceChangeSpeed = 2.4f;
        [Tooltip("Pressing sprint while crouched stands the player up (if there's room).")]
        [SerializeField] private bool sprintCancelsCrouch = true;

        [Header("Ceiling Check")]
        [Tooltip("Layers that block standing up. Exclude the player's own layer if it has one.")]
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Footsteps")]
        [SerializeField] private float walkStepDistance = 1.6f;
        [SerializeField] private float sprintStepDistance = 2.2f;
        [SerializeField] private float crouchStepDistance = 1.1f;
        [SerializeField] private float proneStepDistance = 0.8f;

        public Stance CurrentStance { get; private set; } = Stance.Standing;
        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public float CurrentHeight { get; private set; }
        public Vector3 PlanarVelocity => planarVelocity;
        public float VerticalVelocity => verticalVelocity;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public PlayerStamina Stamina => stamina;

        /// <summary>Fires on touchdown with the downward impact speed in m/s.</summary>
        public event Action<float> Landed;
        public event Action Jumped;
        public event Action<Stance> StanceChanged;
        /// <summary>Fires each step while moving on the ground. Hook footstep sounds and AI hearing up here.</summary>
        public event Action<Stance, bool> Footstep;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressTime = float.NegativeInfinity;
        private float lastJumpTime = float.NegativeInfinity;
        private float stepDistanceAccumulator;
        private bool jumpUsedForStance;
        private readonly Collider[] overlapBuffer = new Collider[16];

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (stamina == null) stamina = GetComponent<PlayerStamina>();

            proneHeight = Mathf.Max(proneHeight, controller.radius * 2f + 0.01f);
            CurrentHeight = standHeight;
            ApplyHeight(CurrentHeight);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            HandleStanceInput();
            UpdateHeight(dt);
            UpdatePlanarVelocity(dt);
            UpdateVerticalVelocity(dt);

            bool wasGrounded = IsGrounded;
            float impactSpeed = -verticalVelocity;

            CollisionFlags flags = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            IsGrounded = controller.isGrounded;

            // Bumped our head: kill upward speed so we don't stick to the ceiling.
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;

            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGrounded) OnLanded(impactSpeed);
            }

            UpdateFootsteps(dt);
        }

        // ---------------------------------------------------------------- stances

        private void HandleStanceInput()
        {
            jumpUsedForStance = false;
            if (input == null || !IsGrounded) return;

            if (input.PronePressed)
            {
                if (CurrentStance == Stance.Prone)
                {
                    if (!TrySetStance(Stance.Standing)) TrySetStance(Stance.Crouching);
                }
                else
                {
                    TrySetStance(Stance.Prone);
                }
            }
            else if (input.CrouchPressed)
            {
                switch (CurrentStance)
                {
                    case Stance.Standing: TrySetStance(Stance.Crouching); break;
                    case Stance.Crouching: TrySetStance(Stance.Standing); break;
                    case Stance.Prone: TrySetStance(Stance.Crouching); break;
                }
            }
            else if (input.JumpPressed && CurrentStance != Stance.Standing)
            {
                // Jump while low gets you up one stance instead of jumping.
                TrySetStance(CurrentStance == Stance.Prone ? Stance.Crouching : Stance.Standing);
                jumpUsedForStance = true;
            }
            else if (sprintCancelsCrouch && input.SprintHeld && input.Move.y > 0.1f && CurrentStance == Stance.Crouching
                     && (stamina == null || !stamina.IsExhausted))
            {
                TrySetStance(Stance.Standing);
            }
        }

        /// <summary>Changes stance if there's room for the new height. Lowering always succeeds.</summary>
        public bool TrySetStance(Stance stance)
        {
            if (stance == CurrentStance) return true;

            float target = HeightFor(stance);
            if (target > CurrentHeight && !CanFitHeight(target)) return false;

            CurrentStance = stance;
            StanceChanged?.Invoke(stance);
            return true;
        }

        private void UpdateHeight(float dt)
        {
            float target = HeightFor(CurrentStance);
            if (Mathf.Approximately(CurrentHeight, target)) return;

            float next = Mathf.MoveTowards(CurrentHeight, target, stanceChangeSpeed * dt);

            // Something moved over us mid-stand: stay where we are until there's room.
            if (next > CurrentHeight && !CanFitHeight(next)) return;

            CurrentHeight = next;
            ApplyHeight(CurrentHeight);
        }

        private void ApplyHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
        }

        private float HeightFor(Stance stance)
        {
            switch (stance)
            {
                case Stance.Crouching: return crouchHeight;
                case Stance.Prone: return proneHeight;
                default: return standHeight;
            }
        }

        /// <summary>True if a capsule of the given height fits at the current position without hitting anything.</summary>
        public bool CanFitHeight(float height)
        {
            float radius = controller.radius * 0.95f;
            Vector3 feet = transform.position;
            // Lift the bottom sphere a little so the floor itself doesn't count as an obstruction.
            Vector3 bottom = feet + Vector3.up * (controller.radius + controller.stepOffset * 0.5f + 0.05f);
            Vector3 top = feet + Vector3.up * (height - controller.radius);
            if (top.y < bottom.y) top = bottom;

            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, obstacleMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit = overlapBuffer[i];
                if (hit == controller || hit.transform.IsChildOf(transform)) continue;
                return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- movement

        private void UpdatePlanarVelocity(float dt)
        {
            Vector2 move = input != null ? input.Move : Vector2.zero;

            bool wantsSprint = input != null && input.SprintHeld && move.y > 0.1f;
            IsSprinting = wantsSprint && IsGrounded && CurrentStance == Stance.Standing
                          && (stamina == null || (!stamina.IsExhausted && stamina.Current > 0f));

            float speed = SpeedFor(CurrentStance);
            if (IsSprinting) speed = sprintSpeed;
            if (stamina != null && stamina.IsExhausted) speed *= exhaustedMultiplier;

            Vector3 local = new Vector3(move.x * strafeMultiplier, 0f, move.y * (move.y < 0f ? backwardMultiplier : 1f));
            Vector3 desired = transform.TransformDirection(local) * speed;

            bool speedingUp = desired.sqrMagnitude > planarVelocity.sqrMagnitude;
            float rate = speedingUp ? acceleration : deceleration;
            if (!IsGrounded) rate *= airControl;

            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, rate * dt);

            if (stamina != null)
            {
                // Only drain while actually moving at speed, not while pushing against a wall.
                if (IsSprinting && controller.velocity.magnitude > walkSpeed * 0.75f)
                    stamina.Drain(sprintDrainPerSecond * dt);

                stamina.RegenMultiplier = CurrentStance == Stance.Prone ? proneRegenMultiplier
                    : CurrentStance == Stance.Crouching ? crouchRegenMultiplier : 1f;
            }
        }

        private float SpeedFor(Stance stance)
        {
            switch (stance)
            {
                case Stance.Crouching: return crouchSpeed;
                case Stance.Prone: return proneSpeed;
                default: return walkSpeed;
            }
        }

        private void UpdateVerticalVelocity(float dt)
        {
            if (input != null && input.JumpPressed && !jumpUsedForStance && CurrentStance == Stance.Standing)
                lastJumpPressTime = Time.time;

            bool canJump = Time.time - lastGroundedTime <= coyoteTime
                           && Time.time - lastJumpPressTime <= jumpBufferTime
                           && Time.time - lastJumpTime >= jumpCooldown
                           && CurrentStance == Stance.Standing
                           && Mathf.Approximately(CurrentHeight, standHeight);

            if (canJump && (stamina == null || stamina.TryConsume(jumpCost)))
            {
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
                lastJumpTime = Time.time;
                lastJumpPressTime = float.NegativeInfinity;
                lastGroundedTime = float.NegativeInfinity;
                Jumped?.Invoke();
                return;
            }

            if (IsGrounded && verticalVelocity < 0f)
            {
                // Small constant push keeps the controller glued to slopes and steps.
                verticalVelocity = -2f;
                return;
            }

            float g = verticalVelocity < 0f ? gravity * fallGravityMultiplier : gravity;
            verticalVelocity += g * dt;
        }

        private void OnLanded(float impactSpeed)
        {
            if (impactSpeed > hardLandingSpeed)
            {
                float severity = Mathf.InverseLerp(hardLandingSpeed, hardLandingSpeed * 2f, impactSpeed);
                planarVelocity *= Mathf.Lerp(1f, hardLandingSpeedKept, severity);
            }

            stepDistanceAccumulator = 0f;

            // isGrounded flickers on stairs and slopes; only report real drops.
            if (impactSpeed > 3f)
                Landed?.Invoke(impactSpeed);
        }

        private void UpdateFootsteps(float dt)
        {
            if (!IsGrounded) return;

            Vector3 v = controller.velocity;
            v.y = 0f;
            stepDistanceAccumulator += v.magnitude * dt;

            float stride = IsSprinting ? sprintStepDistance
                : CurrentStance == Stance.Crouching ? crouchStepDistance
                : CurrentStance == Stance.Prone ? proneStepDistance
                : walkStepDistance;

            if (stepDistanceAccumulator >= stride)
            {
                stepDistanceAccumulator -= stride;
                Footstep?.Invoke(CurrentStance, IsSprinting);
            }
        }
    }
}
