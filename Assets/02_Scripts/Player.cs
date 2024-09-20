using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Asegura que el objeto de juego tiene un Controller2D adjunto.
[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    // Variables para el sistema de salto y movimiento
    public float maxJumpHeight = 4;// Altura máxima que el jugador puede alcanzar al saltar
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;  // Tiempo en alcanzar la altura máxima del salto
    float accelerationTimeAirborne = .2f;  // Tiempo para alcanzar la velocidad máxima en el aire
    float accelerationTimeGrounded = .1f;  // Tiempo para alcanzar la velocidad máxima en el suelo
    float moveSpeed = 6;  // Velocidad de movimiento horizontal

    // Variables para la gravedad y la velocidad del salto
    float gravity;  // Gravedad aplicada al jugador
    float maxJumpVelocity;  // Velocidad inicial del salto
    float minJumpVelocity;

    // Control de la velocidad del personaje
    Vector3 velocity;  // Velocidad actual del jugador
    float velocityXSmoothing;  // Valor para suavizar el cambio de velocidad en el eje X

    // Referencia al controlador de colisiones 2D
    Controller2D controller;

    Vector2 directionalInput;
    bool wallSliding;
    int wallDirX;

    // Variables para el salto en la pared
    public Vector2 wallJumpClimb;  // Vector para el salto cuando se está escalando una pared
    public Vector2 wallJumpOff;  // Vector para el salto cuando el jugador salta desde una pared sin moverse hacia ella
    public Vector2 wallLeap;  // Vector para el salto cuando el jugador salta lejos de la pared

    public float wallSlideSpeedMax = 3;  // Velocidad máxima al deslizarse por una pared
    public float wallStickTime = .25f;  // Tiempo que el jugador puede quedarse pegado a la pared
    float timeToWallUnstick;  // Tiempo restante antes de despegarse de la pared

    // Configuración inicial para la gravedad y la velocidad de salto
    void Start()
    {
        // Obtiene la referencia al componente Controller2D
        controller = GetComponent<Controller2D>();

        // Calcula la gravedad basándose en la altura del salto y el tiempo para alcanzar el ápice
        gravity = -2 * maxJumpHeight / Mathf.Pow(timeToJumpApex, 2);

        // Calcula la velocidad inicial necesaria para alcanzar la altura máxima del salto
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;

        minJumpVelocity = Mathf.Sqrt(2*Mathf.Abs(gravity)*minJumpHeight);
        // Imprime la gravedad y la velocidad de salto en la consola
        print("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
    }
    // Actualiza cada frame para manejar entradas, movimiento y físicas
    void Update()
    {
        CalulateVelocity();
        HandleWallSliding();
        // Mueve al jugador en función de la velocidad calculada
        controller.Move(velocity * Time.deltaTime, directionalInput);

        // Resetea la velocidad vertical si el jugador colisiona con algo por arriba o por abajo
        if (controller.collisions.above || controller.collisions.below)
        {
            if(controller.collisions.slidingDownMaxSlope) 
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity *Time.deltaTime;
            }
            else
            {
                velocity.y = 0;
            }
        }
    }
    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {
        if (wallSliding)  // Si está deslizándose por una pared, maneja el salto en la pared
        {
            if (wallDirX == directionalInput.x)  // Si el jugador se está moviendo en dirección a la pared
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0)  // Si el jugador no se está moviendo horizontalmente
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else  // Si el jugador está saltando lejos de la pared
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }

        // Si el jugador está en el suelo, permite el salto
        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
            {
                if(directionalInput.x !=Mathf.Sign(controller.collisions.slopeNormal.x))
                {
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
            }   
        }
    }
    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }
    void HandleWallSliding()
    {
        // Determina la dirección de la pared en función de si el jugador está colisionando con una pared a la izquierda o a la derecha
        wallDirX = (controller.collisions.left) ? -1 : 1;
        // Lógica para el deslizamiento por paredes
        wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            // Limita la velocidad de deslizamiento en la pared
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // Controla el tiempo que el jugador puede quedarse pegado a la pared antes de despegarse
            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
                velocity.x = 0;

                // Reduce el tiempo de pegado a la pared si el jugador intenta moverse en dirección contraria a la pared
                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }

    }
    void CalulateVelocity()
    {
        // Calcula la velocidad objetivo en el eje X basándose en la entrada del jugador
        float targetVelocityX = directionalInput.x * moveSpeed;

        // Suaviza la transición de la velocidad del jugador
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        // Aplica gravedad al jugador cada frame
        velocity.y += gravity * Time.deltaTime;
    }
}
