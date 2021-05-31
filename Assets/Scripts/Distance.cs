using UnityEngine;

public class Distance : MonoBehaviour
{
    public void Update()
    {
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
    }
}
