using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// camera orbits in a circle around an arbitrary (undefined) center point;
// its movement is driven by a velocity given an angle, instead of a position given an angle
// (which means you can nudge the object off-center at runtime and it will still move continuously in a circle)

public class CameraOrbit : MonoBehaviour {

    public float orbitRadius = 10f;
    public float orbitSpeed = 1f;
    public float verticalPanHeight = 3f;
    private float theta = 0f;

    public GameObject target;
	
	// Update is called once per frame
	void FixedUpdate () {

        // beginning of update
        transform.position += DistanceMoved(); 
        transform.LookAt(target.transform);

        // end of update
        theta += orbitSpeed * Time.deltaTime;
        theta %= 360;
        Debug.Log(theta);
	}

    Vector3 DistanceMoved()
    {
        /* 
         * so basically this came from a bunch of trial-and-error calculus
         * and picking rate-of-change curves whose antiderivatives were also continuous 
         * (so it ended up all being sines/cosines)
         * behavior of camera is it orbits in a circle 
         * while also easing into and out of rising upward, cyclically 
         * parametrization!!
        
        */

        float oR = orbitRadius;
        float tR = theta * Mathf.Deg2Rad;
        float dTR = orbitSpeed * Mathf.Deg2Rad * Time.deltaTime; // change in theta for change in time, converted to radians

        float xspeed = dTR * -1 * oR * Mathf.Sin(tR); // deltaTheta multiplied by rate of change at specific theta, which we need to compute deltaX
        float yspeed = dTR * -1 * verticalPanHeight/2 * Mathf.Sin(tR - Mathf.PI); // amplitude of wave is 2, so 2*r-->r means 2*r/2= r
        float zspeed = dTR * oR * Mathf.Cos(tR);
        return new Vector3(xspeed, yspeed, zspeed);
    }
}
