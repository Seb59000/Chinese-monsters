using UnityEngine;

namespace Game
{
    /// <summary>Slow spin and bob for pickups.</summary>
    public class Spinner : MonoBehaviour
    {
        Vector3 basePos;
        void Start() => basePos = transform.localPosition;

        void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.12f;
        }
    }
}
