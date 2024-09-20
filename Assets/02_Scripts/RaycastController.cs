using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]  // Asegura que el objeto tiene un componente BoxCollider2D.
public class RaycastController : MonoBehaviour
{
    public LayerMask collisionMask;  // Capa que define con qué objetos debería colisionar.
    public const float skinWidth = .015f;   // Pequeño offset para los rayos para evitar problemas de colisión en superficies.
    const float dtsBetweenRays = .25f;
    [HideInInspector]
    public int horizontalRayCount;  // Cantidad de rayos horizontales para detectar colisiones.
    [HideInInspector]
    public int verticalRayCount;    // Cantidad de rayos verticales para detectar colisiones.

    [HideInInspector] // Oculta la variable en el inspector para que no sea visible.
    public float horizontalRaySpacing; // Espacio entre cada rayo horizontal.
    [HideInInspector]
    public float verticalRaySpacing;   // Espacio entre cada rayo vertical.

    [HideInInspector]
    public BoxCollider2D collider;  // Referencia al BoxCollider2D del objeto.
    public RaycastOrigins raycastOrigins;  // Orígenes de los rayos para colisiones.

    // Método que se llama al iniciar. Se sobreescribe en clases derivadas.
    public virtual void Awake()
    {
        collider = GetComponent<BoxCollider2D>();  // Obtiene el componente BoxCollider2D adjunto.
    }

    public virtual void Start()
    {
        CalculateRaySpacing();  // Calcula el espaciado entre los rayos basado en el tamaño del collider y la cantidad de rayos.
    }
    // Actualiza los puntos de origen para el raycasting basado en la posición actual y el tamaño del collider.
    public void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;  // Obtiene los límites del collider.
        bounds.Expand(skinWidth * -2);  // Contrae ligeramente los límites para ajustar el skinWidth, reduciendo el tamaño del collider para el raycasting.

        // Define los puntos de origen para los rayos en las esquinas del collider.
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);  // Esquina inferior izquierda.
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);  // Esquina inferior derecha.
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);  // Esquina superior izquierda.
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);  // Esquina superior derecha.
    }

    // Calcula el espaciado entre los rayos horizontales y verticales basado en el número deseado de rayos y el tamaño del collider.
    public void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;  // Obtiene los límites del collider.
        bounds.Expand(skinWidth * -2);  // Contrae ligeramente los límites para ajustar el skinWidth, reduciendo el tamaño.

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        // Asegura que haya al menos dos rayos horizontales y verticales.
        horizontalRayCount = Mathf.RoundToInt(boundsHeight/dtsBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth/dtsBetweenRays);

        // Calcula el espaciado entre rayos basándose en el tamaño del collider.
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);  // Calcula el espaciado horizontal entre los rayos.
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);  // Calcula el espaciado vertical entre los rayos.
    }

    // Estructura para almacenar los puntos de origen de los rayos para el raycasting.
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;  // Orígenes de los rayos en las esquinas superiores.
        public Vector2 bottomLeft, bottomRight;  // Orígenes de los rayos en las esquinas inferiores.
    }
}
