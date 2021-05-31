using UnityEngine;

public class Mass : MonoBehaviour
{
    public void Update()
    {
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
    }
}
