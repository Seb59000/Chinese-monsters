using UnityEngine;

namespace Game
{
    public enum HumanoidPose { Standing, Kneeling, Lying }

    /// <summary>Primitive-based body with a procedural walk cycle. Swap for a real model later.</summary>
    public class HumanoidRig : MonoBehaviour
    {
        public Transform visualRoot, torso, head, leftLeg, rightLeg, leftArm, rightArm, rightHand;
        public Renderer[] renderers;

        HumanoidPose pose;
        float phase;
        float poseBlend;
        Vector3 rootBase;

        public struct Look
        {
            public Color clothes, pants, skin, hair;
            public float height;
            public bool longHair, cap, eyes;
            public Color eyeColor;
        }

        public static HumanoidRig Build(Transform parent, Look look)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<HumanoidRig>();
            rig.visualRoot = root.transform;

            var clothes = MaterialFactory.Lit(look.clothes, 0.25f);
            var pants = MaterialFactory.Lit(look.pants, 0.2f);
            var skin = MaterialFactory.Lit(look.skin, 0.35f);
            var hair = MaterialFactory.Lit(look.hair, 0.5f);

            Transform Pivot(string name, Vector3 pos)
            {
                var t = new GameObject(name).transform;
                t.SetParent(root.transform, false);
                t.localPosition = pos;
                return t;
            }

            rig.leftLeg = Pivot("LegL", new Vector3(-0.12f, 0.9f, 0f));
            rig.rightLeg = Pivot("LegR", new Vector3(0.12f, 0.9f, 0f));
            MeshUtil.Visual(PrimitiveType.Capsule, rig.leftLeg, new Vector3(0f, -0.45f, 0f), new Vector3(0.2f, 0.46f, 0.2f), pants);
            MeshUtil.Visual(PrimitiveType.Capsule, rig.rightLeg, new Vector3(0f, -0.45f, 0f), new Vector3(0.2f, 0.46f, 0.2f), pants);

            rig.torso = MeshUtil.Visual(PrimitiveType.Capsule, root.transform, new Vector3(0f, 1.18f, 0f), new Vector3(0.48f, 0.4f, 0.3f), clothes).transform;

            rig.leftArm = Pivot("ArmL", new Vector3(-0.31f, 1.45f, 0f));
            rig.rightArm = Pivot("ArmR", new Vector3(0.31f, 1.45f, 0f));
            MeshUtil.Visual(PrimitiveType.Capsule, rig.leftArm, new Vector3(0f, -0.33f, 0f), new Vector3(0.14f, 0.35f, 0.14f), clothes);
            MeshUtil.Visual(PrimitiveType.Capsule, rig.rightArm, new Vector3(0f, -0.33f, 0f), new Vector3(0.14f, 0.35f, 0.14f), clothes);
            rig.rightHand = new GameObject("HandR").transform;
            rig.rightHand.SetParent(rig.rightArm, false);
            rig.rightHand.localPosition = new Vector3(0f, -0.66f, 0f);

            rig.head = MeshUtil.Visual(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.7f, 0f), Vector3.one * 0.3f, skin).transform;
            if (look.longHair)
            {
                MeshUtil.Visual(PrimitiveType.Sphere, rig.head, new Vector3(0f, 0.25f, -0.05f), new Vector3(1.08f, 0.7f, 1.08f), hair);
                MeshUtil.Visual(PrimitiveType.Cube, rig.head, new Vector3(0f, -0.25f, -0.38f), new Vector3(0.95f, 1.2f, 0.3f), hair);
            }
            else
            {
                MeshUtil.Visual(PrimitiveType.Sphere, rig.head, new Vector3(0f, 0.3f, -0.05f), new Vector3(1.05f, 0.55f, 1.05f), hair);
            }
            if (look.cap)
            {
                var capMat = MaterialFactory.Lit(new Color(0.05f, 0.07f, 0.15f), 0.4f);
                MeshUtil.Visual(PrimitiveType.Cylinder, rig.head, new Vector3(0f, 0.42f, 0f), new Vector3(1.12f, 0.12f, 1.12f), capMat);
                MeshUtil.Visual(PrimitiveType.Cube, rig.head, new Vector3(0f, 0.32f, 0.5f), new Vector3(0.9f, 0.05f, 0.45f), capMat);
            }
            if (look.eyes)
            {
                var eyeMat = MaterialFactory.Emissive(look.eyeColor, look.eyeColor, 4f);
                MeshUtil.Visual(PrimitiveType.Sphere, rig.head, new Vector3(-0.17f, 0.05f, 0.44f), Vector3.one * 0.14f, eyeMat);
                MeshUtil.Visual(PrimitiveType.Sphere, rig.head, new Vector3(0.17f, 0.05f, 0.44f), Vector3.one * 0.14f, eyeMat);
            }
            else
            {
                // A small nose so the facing direction is readable.
                MeshUtil.Visual(PrimitiveType.Cube, rig.head, new Vector3(0f, -0.05f, 0.48f), new Vector3(0.12f, 0.18f, 0.12f), skin);
            }

            float s = look.height / 1.8f;
            root.transform.localScale = Vector3.one * s;
            rig.rootBase = Vector3.zero;
            rig.renderers = root.GetComponentsInChildren<Renderer>();
            return rig;
        }

        public void SetPose(HumanoidPose p) => pose = p;

        /// <summary>Call every frame with the horizontal speed in m/s.</summary>
        public void Animate(float speed)
        {
            float target = pose == HumanoidPose.Standing ? 0f : 1f;
            poseBlend = Mathf.MoveTowards(poseBlend, target, Time.deltaTime * 2.5f);

            float stride = Mathf.Clamp01(speed / 2f);
            phase += Time.deltaTime * (4f + speed * 1.6f);
            float swing = Mathf.Sin(phase) * 38f * stride;
            float run = Mathf.Clamp01((speed - 4f) / 4f);

            if (pose == HumanoidPose.Standing || poseBlend < 1f)
            {
                leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
                rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
                leftArm.localRotation = Quaternion.Euler(-swing * 0.8f - run * 30f, 0f, 0f);
                if (!holdingWeapon) rightArm.localRotation = Quaternion.Euler(swing * 0.8f - run * 30f, 0f, 0f);
            }

            var bob = Mathf.Abs(Mathf.Sin(phase)) * 0.06f * stride;
            switch (pose)
            {
                case HumanoidPose.Standing:
                    visualRoot.localPosition = rootBase + Vector3.up * bob;
                    visualRoot.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(run * 12f, 0f, 0f), 0.5f);
                    break;
                case HumanoidPose.Kneeling:
                    visualRoot.localPosition = Vector3.Lerp(rootBase, rootBase + new Vector3(0f, -0.5f, 0f), poseBlend);
                    visualRoot.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(18f, 0f, 0f), poseBlend);
                    leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(-80f, 0f, 0f), poseBlend);
                    rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-10f, 0f, 0f), poseBlend);
                    leftArm.localRotation = Quaternion.Euler(-20f + Mathf.Sin(Time.time * 3f) * 6f, 0f, 20f);
                    rightArm.localRotation = Quaternion.Euler(-25f, 0f, -20f);
                    break;
                case HumanoidPose.Lying:
                    visualRoot.localPosition = Vector3.Lerp(rootBase, rootBase + new Vector3(0f, 0.18f, -0.9f), poseBlend);
                    visualRoot.localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(-88f, 0f, 0f), poseBlend);
                    break;
            }
        }

        bool holdingWeapon;
        float aimPitch;

        public void HoldWeapon(bool hold, float pitch = 0f)
        {
            holdingWeapon = hold;
            aimPitch = pitch;
            if (hold) rightArm.localRotation = Quaternion.Euler(-90f + aimPitch, 0f, 0f);
        }

        public void SetTint(Color emission)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var m = r.material; // instanced on purpose
                m.EnableKeyword("_EMISSION");
                MaterialFactory.SetEmission(m, emission);
            }
        }
    }
}
