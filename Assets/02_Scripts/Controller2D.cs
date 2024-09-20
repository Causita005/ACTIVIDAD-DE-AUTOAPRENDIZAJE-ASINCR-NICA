using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class Controller2D : RaycastController
{
    public float maxSlopeAngle = 80;   // M�ximo �ngulo de pendiente que el personaje puede subir.

    public CollisionInfo collisions;  // Almacena la informaci�n de las colisiones.
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();  // Llama al m�todo Start de RaycastController.
        collisions.faceDir = 1;  // Establece la direcci�n inicial del personaje.
    }
    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    // M�todo para mover al personaje. Incluye detecci�n y respuesta a colisiones.
    public void Move(Vector2 moveAmount, Vector2 input,bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();  // Actualiza los puntos de origen de los rayos.
        collisions.Reset();  // Resetea la informaci�n de colisiones del frame anterior.
        collisions.velocityOld = moveAmount;  // Guarda la velocidad antes de modificarla por colisiones.
        playerInput = input;

        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);  // Llama a DescendSlope si el personaje se est� moviendo hacia abajo.
        }

        if (moveAmount.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);  // Actualiza la direcci�n en la que mira el personaje.
        }


        HorizontalCollisions(ref moveAmount);  // Gestiona las colisiones horizontales.
        if (moveAmount.y != 0)
        {
            VerticalCollisions(ref moveAmount);  // Gestiona las colisiones verticales si hay movimiento en el eje Y.
        }

        transform.Translate(moveAmount);  // Mueve el personaje en la escena con la nueva velocidad calculada.

        if (standingOnPlatform)
        {
            collisions.below = true;  // Si el personaje est� sobre una plataforma, se asegura de que se considera en el suelo.
        }
    }

    // Gestiona las colisiones verticales y ajusta la velocidad vertical.
    void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);  // Determina la direcci�n del movimiento vertical.
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;  // Calcula la longitud de los rayos.

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);  // Ajusta el origen de los rayos basado en la direcci�n y la velocidad.
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);  // Dibuja los rayos en la vista de Scene para depuraci�n.

            if (hit)  // Si hay una colisi�n
            {
                if(hit.collider.tag == "Through")
                {
                    if(directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if (collisions.fallingThroughPlatform)
                    {
                        continue;
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", 5f);
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY;  // Ajusta la velocidad vertical para detenerse en el punto de colisi�n.
                rayLength = hit.distance;  // Actualiza la longitud del rayo para evitar procesamiento innecesario.

                // Ajusta la velocidad horizontal si el personaje est� subiendo una pendiente.
                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                collisions.below = directionY == -1;  // Establece si hay colisi�n por debajo.
                collisions.above = directionY == 1;  // Establece si hay colisi�n por arriba.
            }
        }
        // Si el personaje est� subiendo una pendiente, se ajusta la velocidad horizontal en funci�n de la pendiente.
        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);  // Determina la direcci�n del movimiento horizontal.
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;  // Calcula la longitud del rayo horizontal.
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;  // Ajusta el origen del rayo.
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);  // Calcula el �ngulo de la pendiente.
                if (slopeAngle != collisions.slopeAngle)  // Si el �ngulo de la pendiente ha cambiado.
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;  // Ajusta la velocidad horizontal para detenerse en la colisi�n.
                    collisions.slopeAngle = slopeAngle;  // Actualiza el �ngulo de la pendiente.
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    // Gestiona las colisiones horizontales y ajusta la velocidad horizontal.
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir;  // Direcci�n horizontal del personaje.
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;  // Longitud del rayo, considerando el skinWidth.

        if (Mathf.Abs(moveAmount.x) < skinWidth)  // Si la velocidad horizontal es menor que el skinWidth, ajusta el rayo.
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);  // Ajusta la posici�n vertical de los rayos.
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);  // Dibuja los rayos en la vista de Scene para depuraci�n.

            if (hit)  // Si hay una colisi�n
            {
                if (hit.distance == 0)  
                {
                    continue;  // Ignora colisiones que ocurren a distancia cero.
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);  // Calcula el �ngulo de la pendiente en la colisi�n.

                if (i == 0 && slopeAngle <= maxSlopeAngle)  // Si es el primer rayo y el �ngulo permite subir la pendiente.
                {
                    if (collisions.descendingSlope)  // Si el personaje estaba descendiendo una pendiente.
                    {
                        collisions.descendingSlope = false;  // Deja de descender la pendiente.
                        moveAmount = collisions.velocityOld;  // Restaura la velocidad original.
                    }
                    float distanceToSlopeStart = 0;  // Inicializa la distancia hasta el inicio de la pendiente.
                    if (slopeAngle != collisions.slopeAngleOld)  // Si el �ngulo de la pendiente ha cambiado.
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;  // Calcula la distancia hasta el inicio de la pendiente.
                        moveAmount.x -= distanceToSlopeStart * directionX;  // Ajusta la velocidad horizontal para alinearse con la pendiente.
                    }
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);  // Maneja la subida de la pendiente.
                    moveAmount.x += distanceToSlopeStart * directionX;  // Ajusta la velocidad horizontal despu�s de alinearse con la pendiente.
                }

                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)  // Si no est� subiendo una pendiente o el �ngulo es muy empinado.
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;  // Ajusta la velocidad horizontal para detenerse en la colisi�n.
                    rayLength = hit.distance;  // Actualiza la longitud del rayo.

                    // Si el personaje est� subiendo una pendiente, ajusta la velocidad vertical.
                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);  // Ajusta la velocidad vertical en funci�n de la pendiente.
                    }

                    collisions.left = directionX == -1;  // Establece si hay colisi�n a la izquierda.
                    collisions.right = directionX == 1;  // Establece si hay colisi�n a la derecha.
                }
            }
        }
    }

    // Gestiona la subida por pendientes y ajusta las velocidades correspondientes.
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);  // La distancia a moverse basada en la velocidad horizontal.
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;  // Calcula la velocidad vertical necesaria para subir la pendiente.

        // Si la velocidad vertical actual es menor o igual a la necesaria para subir la pendiente:
        if (moveAmount.y <= climbVelocityY)
        {
            moveAmount.y = climbVelocityY;  // Establece la velocidad vertical para subir la pendiente.
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);  // Ajusta la velocidad horizontal para mantener la velocidad a lo largo de la pendiente.
            collisions.below = true;  // Confirma que el personaje est� en el suelo.
            collisions.climbingSlope = true;  // Establece que el personaje est� subiendo una pendiente.
            collisions.slopeAngle = slopeAngle;  // Actualiza el �ngulo de la pendiente actual.
            collisions.slopeNormal = slopeNormal;
        }
    }

    // Gestiona el descenso por pendientes y ajusta las velocidades correspondientes.
    void DescendSlope(ref Vector2 moveAmount)
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);

        if(maxSlopeHitLeft ^ maxSlopeHitRight)
        {
            SlideDoownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDoownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }

        if(!collisions.slidingDownMaxSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);  // Determina la direcci�n del movimiento horizontal.
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;  // Establece el origen del rayo basado en la direcci�n.
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);  // Realiza un raycast hacia arriba hasta el infinito.

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);  // Calcula el �ngulo de la pendiente de la colisi�n.
                                                                           // Si la pendiente no es horizontal y es transitable:
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == directionX)  // Si la pendiente desciende en la misma direcci�n en que se est� moviendo el personaje.
                    {
                        // Si el personaje est� lo suficientemente cerca para comenzar a descender:
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                        {
                            float moveDistance = Mathf.Abs(moveAmount.x);  // Calcula la distancia horizontal de movimiento.
                            float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;  // Calcula la velocidad vertical necesaria para descender.
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);  // Ajusta la velocidad horizontal para mantener la velocidad a lo largo de la pendiente.
                            moveAmount.y -= descendVelocityY;  // Ajusta la velocidad vertical para descender correctamente.

                            collisions.slopeAngle = slopeAngle;  // Actualiza el �ngulo de la pendiente actual.
                            collisions.descendingSlope = true;  // Establece que el personaje est� descendiendo una pendiente.
                            collisions.below = true;  // Confirma que el personaje est� en el suelo.
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    void SlideDoownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                moveAmount.x = hit.normal.x * (Mathf.Abs(moveAmount.y) - hit.distance / Mathf.Tan(slopeAngle*Mathf.Deg2Rad));

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatform = false;
    }

    // Estructura para mantener informaci�n sobre las colisiones detectadas.
    public struct CollisionInfo
    {
        public bool above, below;  // Si hay colisiones detectadas arriba o abajo.
        public bool left, right;  // Si hay colisiones detectadas a la izquierda o derecha.
        public bool climbingSlope;  // Si el personaje est� subiendo una pendiente.
        public bool descendingSlope;  // Si el personaje est� descendiendo una pendiente.
        public bool slidingDownMaxSlope;
        public float slopeAngle, slopeAngleOld;  // �ngulo de la pendiente actual y anterior.
        public Vector2 slopeNormal;
        public Vector3 velocityOld;  // Velocidad del frame anterior.
        public int faceDir;
        public bool fallingThroughPlatform;

        // Resetea la informaci�n de las colisiones al inicio de cada frame.
        public void Reset()
        {
            above = below = left = right = climbingSlope = descendingSlope = slidingDownMaxSlope=false;  // Resetea todas las colisiones.
            slopeAngleOld = slopeAngle;  // Guarda el �ngulo de la pendiente anterior.
            slopeAngle = 0;  // Resetea el �ngulo de la pendiente actual.
            slopeNormal = Vector2.zero;
        }
    }
}

