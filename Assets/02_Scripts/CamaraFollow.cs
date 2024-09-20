using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamaraFollow : MonoBehaviour
{
    public Controller2D target;
    public Vector2 focusAreaSize;

    public Vector2 verticalOffset;
    public float lookAheadDstX;
    public float lookSmoothTimeX;
    public float verticalSmoothTime;

    FocusArea focusArea;

    float currentLookAheadX;
    float targetLookAheadX;
    float lookAheadDirX;
    float smoothLookVelocityX;
    float smoothVelocityY;

    bool lookAheadStopped;

    private void Start()
    {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
    }

    void LateUpdate()
    {
        focusArea.Update(target.collider.bounds);    
        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        if(focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
            if(Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x !=0)
            {
                lookAheadStopped = false;
                targetLookAheadX = lookAheadDirX * lookAheadDstX;
            }
            else
            {
                if(!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDstX - currentLookAheadX) / 4f;
                }
            }
        }
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
        focusPosition += Vector2.right * currentLookAheadX;
        transform.position = (Vector3)focusPosition + Vector3.forward * -10; 
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1,0,0, .5f);
        Gizmos.DrawCube(focusArea.centre, focusAreaSize);
    }
    struct FocusArea
    {
        public Vector2 centre;
        public Vector2 velocity;
        float left, rigth;
        float top, bottom;

        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x/2;
            rigth = targetBounds.center.x + size.x/2;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            centre = new Vector2 ((left+ rigth)/2, (top + bottom)/2);
        }

        // Update is called once per frame
        public void Update(Bounds targetBounds)
        {
            float shiftX = 0;
            if (targetBounds.min.x<left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if(targetBounds.max.x>rigth)
            {
                shiftX = targetBounds.max.x-rigth;
            }
            left += shiftX;
            rigth += shiftX;

            float shiftY = 0;
            if(targetBounds.min.y<bottom) 
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.x > top)
            {
                shiftY = targetBounds.max.x - top;
            }
            top += shiftY;
            bottom += shiftY;
            centre = new Vector2((left + rigth) / 2, (top + bottom) / 2);
            velocity = new Vector2 (shiftX,shiftY);
        }
    }
    
}
