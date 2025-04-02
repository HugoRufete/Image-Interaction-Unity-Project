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
        // Listar dispositivos de c�mara disponibles
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("No se detectaron c�maras en el dispositivo");
            return;
        }

        // Crear textura con la primera c�mara disponible
        webcamTexture = new WebCamTexture(devices[0].name, 1280, 720, 30);

        // Asignar la textura a la imagen
        displayImage.texture = webcamTexture;

        // Iniciar la c�mara
        webcamTexture.Play();

        isCameraInitialized = true;
    }

    void Update()
    {
        if (isCameraInitialized && webcamTexture.isPlaying)
        {
            // Actualizar el aspect ratio para que coincida con la c�mara
            float ratio = (float)webcamTexture.width / (float)webcamTexture.height;
            aspectRatioFitter.aspectRatio = ratio;

            // Corregir la orientaci�n si es necesario
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