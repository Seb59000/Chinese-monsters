using UnityEngine;

namespace Game
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 4.3f;
        public float aimDistance = 1.9f;
        public float height = 1.62f;
        public float shoulder = 0.55f;
        public float aimShoulder = 0.72f;
        public float fov = 62f;
        public float aimFov = 46f;
        public float minPitch = -40f, maxPitch = 70f;

        public bool IsAiming;
        public float Yaw => yaw;
        public float Pitch => pitch;

        float yaw, pitch = 12f, blend, shake, recoil;
        Camera cam;

        void Awake() => cam = GetComponent<Camera>();

        public void SnapBehind(float targetYaw, float targetPitch = 12f)
        {
            yaw = targetYaw;
            pitch = targetPitch;
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);
        public void Kick(float degrees) => recoil += degrees;

        void LateUpdate()
        {
            if (target == null || GameInput.Instance == null) return;
            float dt = Time.unscaledDeltaTime;

            var look = GameInput.Instance.LookDelta();
            if (Time.timeScale > 0f)
            {
                float aimScale = IsAiming ? 0.6f : 1f;
                yaw += look.x * aimScale;
                pitch = Mathf.Clamp(pitch - look.y * aimScale, minPitch, maxPitch);
            }

            pitch = Mathf.Clamp(pitch - recoil, minPitch, maxPitch);
            recoil = 0f;

            blend = Mathf.MoveTowards(blend, IsAiming ? 1f : 0f, dt * 6f);
            float dist = Mathf.Lerp(distance, aimDistance, blend);
            float side = Mathf.Lerp(shoulder, aimShoulder, blend);

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var pivot = target.position + Vector3.up * height;
            var shoulderPoint = pivot + rot * Vector3.right * side;
            if (Physics.SphereCast(pivot, 0.2f, rot * Vector3.right, out var sideHit, side, Layers.CameraMask, QueryTriggerInteraction.Ignore))
                shoulderPoint = pivot + rot * Vector3.right * Mathf.Max(0f, sideHit.distance - 0.05f);

            var back = rot * Vector3.back;
            var desired = shoulderPoint + back * dist;
            if (Physics.SphereCast(shoulderPoint, 0.22f, back, out var hit, dist, Layers.CameraMask, QueryTriggerInteraction.Ignore))
                desired = shoulderPoint + back * Mathf.Max(0.3f, hit.distance - 0.05f);

            var shakeOffset = Vector3.zero;
            if (shake > 0f)
            {
                shakeOffset = Random.insideUnitSphere * shake * 0.25f;
                shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
            }

            transform.SetPositionAndRotation(desired + shakeOffset, rot);
            if (cam != null) cam.fieldOfView = Mathf.Lerp(fov, aimFov, blend);
        }
    }
}
