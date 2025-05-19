using UnityEngine;
using TMPro;
using NUnit.Compatibility;
using UnityEngine.TextCore.Text;


public class FloatingText : MonoBehaviour
{

    public float moveUpSpeed = 1f;
    public float fadeOutSpeed = 1f;
    private TextMeshProUGUI text;
    private Canvas canvas;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
            Debug.LogError("TextMeshProUGUI not found in children of FloatingText prefab.");
    
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("Canvas not found in parent of FloatingText prefab.");
    }

    // Update is called once per frame
    void Update()
    {
        // Vector3.up = position(0,1,0)
        // (0,1,0) * 2 * 0.016 (time for 60fps) = new Vector3(0,0.032,0).
        // So we're adding 0.032 to the y position each frame
        transform.position += Vector3.up * moveUpSpeed * Time.deltaTime;

        Color c = text.color;

        // c.a is opacity
        c.a -= fadeOutSpeed * Time.deltaTime;
        text.color = c;

        if (text.color.a <= 0)
        {
            Destroy(canvas.gameObject); // Destroys whole canvas
        }
    }

    public void SetText(string value)
    {
        Debug.Log($"Setting floating text to: {value}");
        text.text = value;
    }
}
