using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * ButtonFeedback.cs
 *
 * Da interacción visual a los botones de los menús:
 *  - Al pasar el ratón por encima: el botón crece y aparece una flecha indicadora.
 *  - Al presionarlo: se achica.
 *  - Al hacer clic: suena un "clic".
 * El cambio de color lo hace el propio Button (transición Color Tint).
 * Usa tiempo sin escala, así funciona también en el menú de pausa (timeScale = 0).
 *
 * Autor: [Marcos Astudillo]
 * Fecha: [18-11-2026]
 */
[RequireComponent(typeof(Button))]
public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Animación")]
    public float hoverScale = 1.1f;    // Escala con el ratón encima
    public float pressedScale = 0.92f; // Escala mientras se presiona
    public float animationSpeed = 12f; // Rapidez de la animación
    public GameObject hoverIndicator;  // Imagen (flecha) que aparece con el ratón encima

    private Vector3 baseScale;          // Escala original del botón
    private float targetMultiplier = 1f; // Escala hacia donde se anima
    private bool isHovered = false;     // Si el ratón está encima

    void Awake()
    {
        baseScale = transform.localScale;
    }

    // Al mostrarse un menú, el botón vuelve a su estado normal
    void OnEnable()
    {
        isHovered = false;
        targetMultiplier = 1f;
        if (baseScale != Vector3.zero)
            transform.localScale = baseScale;
        if (hoverIndicator != null)
            hoverIndicator.SetActive(false);
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetMultiplier, Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetMultiplier = hoverScale;
        if (hoverIndicator != null)
            hoverIndicator.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetMultiplier = 1f;
        if (hoverIndicator != null)
            hoverIndicator.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetMultiplier = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetMultiplier = isHovered ? hoverScale : 1f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlayButton();
    }
}
