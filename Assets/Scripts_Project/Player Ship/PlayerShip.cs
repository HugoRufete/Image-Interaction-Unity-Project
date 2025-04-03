using UnityEngine;
using UnityEngine.UI;

public class PlayerShip : MonoBehaviour
{
    [SerializeField] private Image shipImage;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private ColorDetector colorDetector;

    private Vector3 targetPosition;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.position;
    }

    private void Update()
    {
        rectTransform.position = Vector3.Lerp(rectTransform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    // Método público para establecer la posición objetivo de la nave
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }
}