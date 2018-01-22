using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCameraPositioning : MonoBehaviour {

    SphereCoords sphere;
    BoxCollider marginBox; 
    Camera cam;

    private bool isMarginClear = true; 
    private bool isZoomMode = false;
    public GameObject target;
    public GameObject targetZoom; // LookAt when isZoom true 
    private GameObject currentTarget;
    private Vector3 lastDelta; // keeping track of yoyoing 

    public float minRadius = 0.5f;
    public float maxRadius = 10f;
    public float idealRadius = 5f;
    public float idealPitch = 50f;
    public float maxAbsPitch = 70f; // maximum positive/negative pitch--beyond 90 camera-up flips 
    public float growthRatio = 0.95f; // coeff for lerping radius/pitch 
    public float tweenMargin = 0.75f;
    public float marginBoxFactor = 1.5f;
    public float autoYawSpeed = 180f; // 180 degrees in 1 second

    // for lerping cam position (or any Vec3); can only lerp 1 val at a time
    private class Vec3Lerper
    {
        Vector3 vecToLerp;
        Vector3 targetVec;
        float maxTime;
        float elapsedTime; 

        public Vec3Lerper(Vector3 vecToLerp, Vector3 targetVec, float maxTime)
        {
            this.vecToLerp = vecToLerp;
            this.targetVec = targetVec;
            this.maxTime = maxTime;
            this.elapsedTime = 0f; 
        }

        public Vector3 GetCurrent()
        {
            return Vector3.Lerp(vecToLerp, targetVec, elapsedTime / maxTime);
        }

        public void Update() // call this from parent's update method
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > maxTime)
            { // clamp elapsedTime to maximum
                elapsedTime = maxTime;
            }
        }
    }


    // initialization
    void Start() {
        sphere = GetComponent<SphereCoords>();
        marginBox = GetComponent<BoxCollider>();
        cam = GetComponent<Camera>();
        maxRadius = sphere.radius;

        lastDelta = sphere.GetDelta();
        
    }

    // camera update should always come at end of frame, after player update
    void LateUpdate() {

        // when zoomed in, focus directly on player
        // 'zoomed in' = camera is closer to player than midpoint of min/max cam positions
        currentTarget = sphere.radius < (minRadius + maxRadius) / 2 ? targetZoom : target;

        // tweak camera coords
        GeometryAvoidance();

        // apply camera coords
        UpdateTransform();

        lastDelta = sphere.GetDelta(); 

    }

    // sphere around camera is padding
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("LevelGeometry"))
        {
            Debug.Log("margin now obstructed"); 
            isMarginClear = false;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("LevelGeometry"))
        {
            Debug.Log("margin now clear");
            isMarginClear = true;
        }
    }

    void GeometryAvoidance()
    {

        // cast ray from player to camera, looking for obstruction; ignore all but level geometry
        // Vector3 dir = transform.position - target.transform.position;

        CamCollisionInfo info; 

        if (!CheckIsClear(out info))
        {
            // first check: based on edge hits, pitch up, or zoom?
            // check top and bottom--if both are hits, we're facing a wall/in a surface, so zoom
            if (!(info.isEdgeHit[(int)Edges.Top] && info.isEdgeHit[(int)Edges.Bottom]))
            {
                if (info.isEdgeHit[(int)Edges.Bottom]) // only bottom is hit-- so pitch up
                {
                    if (Mathf.Abs(sphere.pitch) <= maxAbsPitch)
                    {
                        Debug.Log("elevating pitch");
                        sphere.pitch /= growthRatio; // elevate the pitch 
                    }
                }
                else if (info.isEdgeHit[(int)Edges.Top]) // only top hit-- so pitch down 
                {
                    if (Mathf.Abs(sphere.pitch) > 0)
                    {
                        Debug.Log("lowering pitch");
                        sphere.pitch *= growthRatio;
                    }
                }
            }
            // both top and bottom obstructed
            else 
            {
                Debug.Log("yaw clockwise");
                sphere.yaw -= autoYawSpeed * Time.deltaTime;

                if (Mathf.Abs(sphere.radius) > Mathf.Abs(minRadius))
                {
                    Debug.Log("zoom in");
                    sphere.radius *= growthRatio;
                }
            }

            // lateral check--adjust yaw if left/right are occluded
            /*if (info.isEdgeHit[(int)Edges.Left] && !info.isEdgeHit[(int)Edges.Right])
            {
                // yaw counterclockwise
                Debug.Log("yaw countclock");
                sphere.yaw += autoYawSpeed * Time.deltaTime;
                
            }
            else if (info.isEdgeHit[(int)Edges.Right] && !info.isEdgeHit[(int)Edges.Left])
            {
                // yaw clockwise
                Debug.Log("yaw clock");
                sphere.yaw -= autoYawSpeed * Time.deltaTime;
            }*/

        }
        else // if (isMarginClear) // move towards ideals if no obstructions 
        {
            // save this step's coords before transform
            Vector3 tempCoords = sphere.GetCurrentSphereCoords();
           
            // now tweak
            if (sphere.pitch > idealPitch + tweenMargin)
            {
                sphere.pitch *= growthRatio;
            }
            else if (sphere.pitch < idealPitch - tweenMargin)
            {
                sphere.pitch /= growthRatio;
            }

            if (sphere.radius > idealRadius + tweenMargin)
            {
                sphere.radius *= growthRatio;
            }
            else if (sphere.radius < idealRadius - tweenMargin)
            {
                sphere.radius /= growthRatio;
            }

            // apply tweaks
            UpdateTransform();

            // run all-or-nothing isClear
            if (!CheckIsClear())
            {
                // test failed, go back to last state
                sphere.SetSphereCoords(tempCoords);
                Debug.Log("tweak disregarded"); 
            }

            // final UpdateTransform will be called from LateUpdate

        }

    }

    void UpdateTransform()
    {
        transform.position = target.transform.position + sphere.GetRectFromSphere();
        transform.LookAt(currentTarget.transform.position);
    }

    enum Corners
    {
        Center,
        LeftDown,
        LeftUp,
        RightUp,
        RightDown
    };

    // version without -out returns false on first ray that fails; 
    // use overload with -out if you want more information 
    bool CheckIsClear()
    {
        // so instead of casting from player to camera,
        // we cast player to ray--so that the position of the viewport doesn't have to be translated

        bool isClear = true;

        // corners of viewport array
        Vector3[] corners = new Vector3[5];

        // center of viewport; use to get slope all other rays will use 
        corners[(int)Corners.Center] = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cam.nearClipPlane));
        Vector3 cornerDir = currentTarget.transform.position - corners[(int)Corners.Center];

        // corners
        corners[(int)Corners.LeftDown] = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        corners[(int)Corners.RightDown] = cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane));
        corners[(int)Corners.RightUp] = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        corners[(int)Corners.LeftUp] = cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane));

        for (int i = 0; i < corners.Length; i++)
        {
            Color col = Color.green;

            if (Physics.Raycast(corners[i], cornerDir.normalized, cornerDir.magnitude, 1 << LayerMask.NameToLayer("LevelGeometry")))
            {
                isClear = false;
                col = Color.red;
            }

            // debug ray draw
            // Debug.DrawRay(corners[i], cornerDir.normalized, col);
        }

        return isClear;
    }

    // checkClear version that goes through all rays always
    // reporting back which passed/failed in camcollision info struct

    enum Edges
    {
        Left,
        Top,
        Right,
        Bottom
    };

    struct CamCollisionInfo {
        public bool[] isCornHit;
        public bool[] isEdgeHit;

        public CamCollisionInfo(bool[] isCornHit, bool[] isEdgeHit)
        {
            this.isCornHit = isCornHit;
            this.isEdgeHit = isEdgeHit; 
        }
    }

    bool CheckIsClear(out CamCollisionInfo info)
    {
        // so instead of casting from player to camera,
        // we cast player to ray--so that the position of the viewport doesn't have to be translated

        bool isClear = true;

        // corners of viewport array
        Vector3[] corners = new Vector3[5];

        // center of viewport; use to get slope all other rays will use 
        corners[(int)Corners.Center] = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cam.nearClipPlane));
        Vector3 cornerDir = currentTarget.transform.position - corners[(int)Corners.Center];

        // corners
        corners[(int)Corners.LeftDown] = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        corners[(int)Corners.RightDown] = cam.ViewportToWorldPoint(new Vector3(1, 0, cam.nearClipPlane));
        corners[(int)Corners.RightUp] = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        corners[(int)Corners.LeftUp] = cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane));

        // temp arrays for collision info 
        bool[] isCornHit = new bool[5];
        bool[] isEdgeHit = new bool[4];

        // check the individual corners
        for (int i = 0; i < corners.Length; i++)
        {
            isCornHit[i] = false;

            Color col = Color.green; 

            if (Physics.Raycast(corners[i], cornerDir.normalized, cornerDir.magnitude, 1 << LayerMask.NameToLayer("LevelGeometry")))
            {
                isClear = false;
                isCornHit[i] = true;

                col = Color.red; 
            }

            // debug ray draw
            Debug.DrawRay(corners[i], cornerDir.normalized, col); 
        }

        // now, based on corners, determine the edge statuses

        // debug -- tried switching || to &&-- make edge detection more restrictive (WHOLE viewport needs to be occluded)
        // top = upleft || upright
        // bottom = downleft || downright
        // left = upleft || downleft
        // right = upright || downright

        isEdgeHit[(int)Edges.Left] = isCornHit[(int)Corners.LeftDown] && isCornHit[(int)Corners.LeftUp];
        isEdgeHit[(int)Edges.Right] = isCornHit[(int)Corners.RightDown] && isCornHit[(int)Corners.RightUp];
        isEdgeHit[(int)Edges.Top] = isCornHit[(int)Corners.LeftUp] && isCornHit[(int)Corners.RightUp];
        isEdgeHit[(int)Edges.Bottom] = isCornHit[(int)Corners.LeftDown] && isCornHit[(int)Corners.RightDown];

        if (isEdgeHit[(int)Edges.Bottom]) Debug.Log("Bottom");
        if (isEdgeHit[(int)Edges.Top]) Debug.Log("Top");
        if (isEdgeHit[(int)Edges.Right]) Debug.Log("Right");
        if (isEdgeHit[(int)Edges.Left]) Debug.Log("Left");

        info = new CamCollisionInfo(isCornHit, isEdgeHit); 

        return isClear;
    }
}
