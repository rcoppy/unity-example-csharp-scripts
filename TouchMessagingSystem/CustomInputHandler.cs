using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomInputHandler : MonoBehaviour, ICustomPointerHandler {

    public void OnCustomPointerSingleTap()
    {
        Debug.Log("received single tap"); 
    }

    public void OnCustomPointerDoubleTap()
    {
        Debug.Log("received double tap");
    }

    public void OnCustomPointerSwipe(Vector2 swipe)
    {
        Debug.Log("received swipe");
    }
}
