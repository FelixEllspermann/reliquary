using UnityEngine;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Dreht eine Schaufenster-Karte langsam um die Y-Achse. Vorder- und
    /// Rückseite sind getrennte Kinder: ab 90° übernimmt die Rückseite (das
    /// Sleeve des Profilinhabers), horizontal gespiegelt, damit sie beim
    /// Rückseiten-Rendering richtig herum steht.
    /// </summary>
    public class ShowcaseSpin : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 42f;

        private GameObject front;
        private GameObject back;
        private float angle;

        public void Bind(GameObject frontSide, GameObject backSide, float startAngle = 0f)
        {
            front = frontSide;
            back = backSide;
            angle = startAngle;
            if (back != null)
            {
                var scale = back.transform.localScale;
                back.transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
            }
            Apply();
        }

        private void Update()
        {
            angle = (angle + degreesPerSecond * Time.deltaTime) % 360f;
            Apply();
        }

        private void Apply()
        {
            transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            bool showingBack = angle > 90f && angle < 270f;
            if (front != null && front.activeSelf == showingBack) front.SetActive(!showingBack);
            if (back != null && back.activeSelf != showingBack) back.SetActive(showingBack);
        }
    }
}
