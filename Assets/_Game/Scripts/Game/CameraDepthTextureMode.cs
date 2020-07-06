using UnityEngine;

public class CameraDepthTextureMode : MonoBehaviour 
{
    [SerializeField]
    DepthTextureMode depthTextureMode = DepthTextureMode.Depth;

    private void OnValidate()
    {
        SetCameraDepthTextureMode();
    }

    private void Awake()
    {
        SetCameraDepthTextureMode();
    }

    private void SetCameraDepthTextureMode()
    {
        GetComponent<Camera>().depthTextureMode = depthTextureMode;
    }
}
