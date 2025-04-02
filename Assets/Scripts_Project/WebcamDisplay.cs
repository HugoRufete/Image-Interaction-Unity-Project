using UnityEngine;
using UnityEngine.UI;

public class WebcamDisplay : MonoBehaviour
{
    [SerializeField] private RawImage displayImage;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    private WebCamTexture webcamTexture;
    private bool isCameraInitialized = false;

    void Start()
    {
        InitializeWebcam();
    }

    void InitializeWebcam()
    {
        // Listar dispositivos de cámara disponibles
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("No se detectaron cámaras en el dispositivo");
            return;
        }

        // Crear textura con la primera cámara disponible
        webcamTexture = new WebCamTexture(devices[0].name, 1280, 720, 30);

        // Asignar la textura a la imagen
        displayImage.texture = webcamTexture;

        // Iniciar la cámara
        webcamTexture.Play();

        isCameraInitialized = true;
    }

    void Update()
    {
        if (isCameraInitialized && webcamTexture.isPlaying)
        {
            // Actualizar el aspect ratio para que coincida con la cámara
            float ratio = (float)webcamTexture.width / (float)webcamTexture.height;
            aspectRatioFitter.aspectRatio = ratio;

            // Corregir la orientación si es necesario
            displayImage.rectTransform.localEulerAngles = new Vector3(0, 0, -webcamTexture.videoRotationAngle);
        }
    }

    void OnDestroy()
    {
        // Liberar recursos al destruir el objeto
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}