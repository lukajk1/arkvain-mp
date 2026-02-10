using UnityEngine;

public class TestRenderFeatureToggle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.G)) {
            ScreenspaceEffectManager.SetGrayscale(true);
        }
    }
}
