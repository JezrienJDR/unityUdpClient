using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeControls : MonoBehaviour
{
    // Start is called before the first frame update

    Material mat;

    public string id;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }


    [SerializeField]
    public void SetColor(float R, float G, float B)
    {
        Color newColor = new Color(R, G, B);
        mat.SetColor("_Color", newColor);
    }
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, 10 * Time.deltaTime, 0));
        mat = GetComponent<Renderer>().material;
        if (Input.GetKeyDown("space"))
        { SetColor(1, 0, 0); }
        
    }
}
