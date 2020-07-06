using UnityEngine;

public class NormalsReplacementShader : MonoBehaviour
{
    [SerializeField]
    Shader normalsShader = null;

    private RenderTexture m_renderTexture;
    private Camera m_camera;

    private void Start()
    {
        Camera thisCamera = GetComponent<Camera>();

        // Create a render texture matching the main camera's current dimensions.
        m_renderTexture = new RenderTexture(thisCamera.pixelWidth, thisCamera.pixelHeight, 24);
        // Surface the render texture as a global variable, available to all shaders.
        Shader.SetGlobalTexture("_CameraNormalsTexture", m_renderTexture);

        // Setup a copy of the camera to render the scene using the normals shader.
        GameObject copy = new GameObject("Normals camera");
        m_camera = copy.AddComponent<Camera>();
        m_camera.CopyFrom(thisCamera);
        m_camera.transform.SetParent(transform);
        m_camera.targetTexture = m_renderTexture;
        m_camera.SetReplacementShader(normalsShader, "RenderType");
        m_camera.depth = thisCamera.depth - 1;
    }
}
