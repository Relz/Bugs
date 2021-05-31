using UnityEngine;
using UnityEngine.UI;

public class Hint : MonoBehaviour
{
    public Graphic[] graphics;
    private const float duration = 1.5f;
    private float _alpha = 0f;

    public void Update()
    {
        float lerp = Mathf.PingPong(Time.time, duration) / duration;
        _alpha = Mathf.Lerp(0.1f, 0.5f, lerp);

        foreach (Graphic graphic in graphics)
        {
            graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, _alpha);
        }
    }
}
