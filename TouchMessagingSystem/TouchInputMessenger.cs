using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchInputMessenger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    public GameObject target; // gameobject whose components will be messaged touch events; 
                              // This target must implement ICustomPointerHandler to receive messages. 
                              // Documentation on the messsaging system: 
                              // https://docs.unity3d.com/Manual/MessagingSystem.html

    enum InputType
    {
        Single,
        Double,
        Swipe,
        None
    };

    int typeBeingTracked = (int)InputType.None;
    bool isTrackActive = false;

    float trackStartTime = 0f;
    float maxDoubleTapTime = 0.300f;
    float maxPointerDelta = 4f; // if tap moved more than this many pixels from down to up, it's a swipe

    Vector2 pointerPositionStart = Vector2.zero;


    void Update()
    {
        // fire off tap event if was tracked
        if (isTrackActive && Time.time - trackStartTime >= maxDoubleTapTime)
        {
            if (typeBeingTracked == (int)InputType.Single)
            {
                Debug.Log("single tap");
                ExecuteEvents.Execute<ICustomPointerHandler>(target, null, (x, y) => x.OnCustomPointerSingleTap());
            }
            else if (typeBeingTracked == (int)InputType.Double)
            {
                Debug.Log("double tap");
                ExecuteEvents.Execute<ICustomPointerHandler>(target, null, (x, y) => x.OnCustomPointerDoubleTap());
            }

            ClearTrackedData(); 
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // on first engagement with panel, assume a single tap is being tracked
        if (!isTrackActive)
        {
            isTrackActive = true;
            typeBeingTracked = (int)InputType.Single;
            pointerPositionStart = eventData.position;
            trackStartTime = Time.time; 
        }
        else {
            // only let check for double if coming explicitly single; swipes discluded
            if (typeBeingTracked == (int)InputType.Single)
            {
                // now need to check position delta--where is this tap landing relative to first?
                // (is it a double-tap, or two single taps?)
                if ((eventData.position - pointerPositionStart).magnitude < maxPointerDelta)
                {
                    // this could be a double-tap; final judge will be Update()
                    typeBeingTracked = (int)InputType.Double;
                }
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Debug.Log("pointer up"); 
        if (typeBeingTracked == (int)InputType.Single)
        {
            // Debug.Log("checking for swipe"); 
            float delta = (eventData.position - pointerPositionStart).magnitude;
            // Debug.Log(delta); 
            
            // check for swipe; did up move far from down? 
            if (delta > maxPointerDelta)
            {
                typeBeingTracked = (int)InputType.Swipe;
                // Debug.Log("swipe");
                ExecuteEvents.Execute<ICustomPointerHandler>(target, null, (x, y) => x.OnCustomPointerSwipe(eventData.position - pointerPositionStart));
                ClearTrackedData(); 
            }
        }
        
    }

    void ClearTrackedData()
    {
        isTrackActive = false;
        trackStartTime = 0f;
        typeBeingTracked = (int)InputType.None;
        pointerPositionStart = Vector2.zero;
    }
    
}
