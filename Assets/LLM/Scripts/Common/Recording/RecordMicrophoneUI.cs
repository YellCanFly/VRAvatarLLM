using UnityEngine;
using UnityEngine.UI;

public class RecordMicrophoneUI : MonoBehaviour
{
    public float forwardOffset = 0.5f; // Offset to move the microphone forward
    public float upwardOffset = -0.3f; // Offset to move the microphone upward
    public float rightwardOffset = 0f; // Offset to move the microphone to the right

    public float blinkSpeed = 2f;

    private Image iconImage;
    private Color originalColor;

    private void Awake()
    {
        iconImage = GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            originalColor = iconImage.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.main.transform.position +
                             Camera.main.transform.forward * forwardOffset +
                             Camera.main.transform.up * upwardOffset +
                             Camera.main.transform.right * rightwardOffset;

        transform.LookAt(Camera.main.transform);

        if (iconImage != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            Color newColor = originalColor;
            newColor.a = alpha;
            iconImage.color = newColor;
        }
    }
}
