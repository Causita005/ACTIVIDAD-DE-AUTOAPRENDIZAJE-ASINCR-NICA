using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

public class PlataformController : RaycastController
{
    // Define qué capas serán consideradas como pasajeros (objetos que se moverán con la plataforma)
    public LayerMask passengerMask;

    // Array de waypoints locales y globales por donde se moverá la plataforma
    public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;

    // Velocidad a la que la plataforma se mueve
    public float speed;
    // Define si la plataforma se mueve de manera cíclica
    public bool cyclic;
    // Tiempo de espera en cada waypoint
    public float waitTime;
    // Valor para suavizar el movimiento entre waypoints
    [Range(0, 2)]
    public float easeAmount;

    // Índice del waypoint actual
    int fromWaypointIndex;
    // Porcentaje del movimiento completado entre los waypoints
    float percentBetweenWaypoints;
    // Tiempo hasta que la plataforma se mueva nuevamente
    float nextMoveTime;

    // Lista que almacena los movimientos de los pasajeros
    List<PassengerMovement> passengerMovement;
    // Diccionario para almacenar a los pasajeros y sus Controladores 2D
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();

    // Inicializa los waypoints globales en base a los locales
    public override void Start()
    {
        base.Start();

        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position; // Convierte waypoints locales a globales
        }
    }

    // Actualiza la plataforma en cada frame
    void Update()
    {
        UpdateRaycastOrigins();  // Actualiza los orígenes de raycast para detección de colisiones

        // Calcula el movimiento de la plataforma
        Vector3 velocity = CalculatePlatformMovement();

        // Calcula el movimiento de los pasajeros en función del movimiento de la plataforma
        CalculatePassengerMovement(velocity);

        // Mueve a los pasajeros antes de mover la plataforma
        MovePassengers(true);
        // Mueve la plataforma
        transform.Translate(velocity);
        // Mueve a los pasajeros después de mover la plataforma
        MovePassengers(false);
    }

    // Función para suavizar el movimiento de la plataforma entre waypoints
    float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    // Calcula el movimiento de la plataforma entre los waypoints
    Vector3 CalculatePlatformMovement()
    {
        // Si es tiempo de esperar antes de moverse
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero; // No se mueve
        }

        // Calcula el siguiente waypoint
        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        // Calcula el porcentaje completado entre los waypoints
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);

        // Calcula la nueva posición en base al porcentaje completado
        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

        // Si se ha alcanzado el siguiente waypoint
        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;

            // Si la plataforma no es cíclica, invierte la dirección cuando llega al final
            if (!cyclic)
            {
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints); // Invierte el array de waypoints
                }
            }
            // Establece el tiempo de espera antes del próximo movimiento
            nextMoveTime = Time.time + waitTime;
        }

        return newPos - transform.position; // Retorna la diferencia de posición para mover la plataforma
    }

    // Mueve a los pasajeros en función de si deben moverse antes o después de la plataforma
    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovement)
        {
            // Si el pasajero no está en el diccionario, se añade
            if (!passengerDictionary.ContainsKey(passenger.transform))
            {
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
            }

            // Mueve al pasajero antes o después de la plataforma según el parámetro beforeMovePlatform
            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    // Calcula el movimiento de los pasajeros en función de los raycasts
    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();

        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // Plataforma moviéndose verticalmente
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit && hit.distance!=0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        // Añade el movimiento del pasajero
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true));
                    }
                }
            }
        }

        // Plataforma moviéndose horizontalmente
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
                    }
                }
            }
        }

        // Pasajero encima de una plataforma en movimiento horizontal o descendente
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            }
        }
    }

    // Estructura para definir el movimiento de los pasajeros
    struct PassengerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

    // Dibuja gizmos para visualizar los waypoints en la escena
    void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = .3f;

            // Dibuja una cruz en cada waypoint para visualización en la escena
            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypointPos = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }
}
