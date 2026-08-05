using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>Kontinuierliche Z-Rotation (Such-Spinner), unscaled time.</summary>
    public class RotateMotion : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = -240f;

        private void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
