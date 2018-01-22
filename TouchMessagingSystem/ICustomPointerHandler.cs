using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ICustomPointerHandler : IEventSystemHandler
{
    // functions that can be called via the messaging system
    void OnCustomPointerSingleTap();
    void OnCustomPointerDoubleTap();
    void OnCustomPointerSwipe(Vector2 swipe); 
}