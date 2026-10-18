using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public float walkSpeed = 4.6f;
        public float sprintSpeed = 8.8f;
        public float aimSpeed = 2.8f;
        public float jumpHeight = 1.25f;
        public float gravity = -22f;

        [Header("Stamina")]
        public float staminaMax = 100f;
        public float staminaDrain = 13f;
        public float staminaRegen = 24f;

        /// <summary>Set by the mission (cutscenes, menus).</summary>
        public bool ControlsEnabled = true;
        /// <summary>Set while the player is busy with a host (extraction).</summary>
        public bool InteractionLocked;
        public bool IsAiming;

        public float Stamina { get; private set; }
        public float Stamina01 => Stamina / staminaMax;
        public bool IsSprinting { get; private set; }
        public Vector3 PlanarVelocity => planarVelocity;
        public HumanoidRig Rig { get; set; }
        public Health Health { get; private set; }
        public CharacterController Controller { get; private set; }

        bool CanMove => ControlsEnabled && !InteractionLocked && !Health.IsDead;

        Vector3 planarVelocity, impulse;
        float verticalVelocity, exhaustedUntil, lastSprintTime;
        bool sprintToggle;

        void Awake()
        {
            Instance = this;
            Controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Stamina = staminaMax;
            Health.Died += _ =>
            {
                if (Rig != null) Rig.SetPose(HumanoidPose.Lying);
            };
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Teleport(Vector3 position, float yaw)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(position + Vector3.up * 0.05f, Quaternion.Euler(0f, yaw, 0f));
            Controller.enabled = true;
            planarVelocity = impulse = Vector3.zero;
            verticalVelocity = 0f;
        }

        public void AddImpulse(Vector3 velocity) => impulse += velocity;
        public void CancelSprint() => sprintToggle = false;

        void Update()
        {
            var input = GameInput.Instance;
            var cam = Camera.main;
            if (input == null || cam == null) return;
            float dt = Time.deltaTime;

            Vector2 move = CanMove ? input.MoveValue : Vector2.zero;
            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            var wish = Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);

            // Keyboard: hold Shift. Gamepad: click the left stick to toggle until you stop.
            if (input.UsingGamepad && CanMove && input.Pressed(input.Sprint)) sprintToggle = !sprintToggle;
            if (move.sqrMagnitude < 0.04f) sprintToggle = false;
            bool wantSprint = CanMove && !IsAiming && move.sqrMagnitude > 0.1f &&
                              (input.UsingGamepad ? sprintToggle : input.Held(input.Sprint));
            IsSprinting = wantSprint && Stamina > 0f && Time.time > exhaustedUntil;

            if (IsSprinting)
            {
                Stamina -= staminaDrain * dt;
                lastSprintTime = Time.time;
                if (Stamina <= 0f)
                {
                    Stamina = 0f;
                    exhaustedUntil = Time.time + 1.5f;
                    sprintToggle = false;
                }
            }
            else if (Time.time - lastSprintTime > 0.6f)
            {
                Stamina = Mathf.Min(staminaMax, Stamina + staminaRegen * dt);
            }

            float speed = IsAiming ? aimSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            bool grounded = Controller.isGrounded;
            planarVelocity = Vector3.MoveTowards(planarVelocity, wish * speed, (grounded ? 45f : 12f) * dt);

            if (grounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (CanMove && input.Pressed(input.Jump))
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * dt;
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, 20f * dt);

            Controller.Move((planarVelocity + impulse + Vector3.up * verticalVelocity) * dt);

            if (IsAiming)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward), 20f * dt);
            }
            else if (planarVelocity.sqrMagnitude > 0.2f)
            {
                var target = Quaternion.LookRotation(planarVelocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 12f * dt);
            }

            if (Rig != null) Rig.Animate(new Vector3(planarVelocity.x, 0f, planarVelocity.z).magnitude);

            // Safety net if the player somehow falls through the world.
            if (transform.position.y < -20f) Teleport(new Vector3(transform.position.x, 2f, transform.position.z), transform.eulerAngles.y);
        }
    }
}
