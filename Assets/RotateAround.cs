using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public float speed;
    public float radius;
    public float height;

    void Update()
    {
        this.transform.position = new Vector3(
            Mathf.Cos(Time.time * speed) * radius,
            height,
            Mathf.Sin(Time.time * speed) * radius
        );

        this.transform.LookAt(Vector3.zero, Vector3.up);
    }
}
