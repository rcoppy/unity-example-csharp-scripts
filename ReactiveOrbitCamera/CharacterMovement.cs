using UnityEngine;
using Rewired;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    // comes from example at https://docs.unity3d.com/ScriptReference/CharacterController.Move.html
    // relative camera movement from https://forum.unity.com/threads/changing-movement-based-on-camera-direction.102709/

    [Header("Rewired/Controls")]
    public int playerId = 0; // The Rewired player id of this character
    private Player player; // The Rewired Player
    public string XAxis = "Horizontal";
    public string YAxis = "Vertical";
    public string JumpButton = "Jump";

    [Header("Physics Parameters")]
    public float speed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;

    private Vector2 moveVector = Vector2.zero;
    private Vector3 moveDirection = Vector3.zero; 

    public GameObject camSystem;
    private GameObject cam; // of type Camera
    public float lateralPanDegrees = 70f; // max degrees pan per second; how gradually shift yaw of player as moves left or right relative to camera
    public float latVertFactor = 2f;
    public float negYFudge = 0.5f;
    public float minYStickComp = -0.99f;

    // player model object
    private GameObject playerModel; 

    private void Start()
    {
        // Get the Rewired Player object for this player and keep it for the duration of the character's lifetime
        player = ReInput.players.GetPlayer(playerId);
        playerModel = gameObject.transform.Find("characterModel").gameObject; 
        cam = camSystem.transform.Find("Main Camera").gameObject;
    }

    void Update()
    {
        CharacterController controller = GetComponent<CharacterController>();

        // raw axis input--will get transformed to correspond with camera orientation 
        moveVector = new Vector2(player.GetAxis(XAxis), player.GetAxis(YAxis)).normalized;

        // camera yaw update
        SphereCoords coords = cam.GetComponent<SphereCoords>();

        // x-axis displacement; minYStickComp is deadzone
        float compX = moveVector.y > minYStickComp ? lateralPanDegrees * moveVector.x : 0f;

        // y-axis displacement--only pan if player running towards camera
        // this means compY will always be negative or 0 
        float compY = moveVector.y < 0f ? lateralPanDegrees * latVertFactor * moveVector.y : 0f;

        // y component will only increase magnitude of x--signs should match
        compX += compX < 0 ? compY : -1 * compY; 
        
        coords.yaw -= compX * Time.deltaTime;

        if (controller.isGrounded)
        {
            // compensate for pitch/roll of camera 
            Vector3 compForward = cam.transform.forward;
            Vector3 compRight = cam.transform.right;

            compForward.y = 0;
            compRight.y = 0;

            compForward.Normalize();
            compRight.Normalize();

            // ternion is a test for camera navigation--neg y axis just for yawing, not movement (experimental)
            moveDirection = moveVector.y > 0 ? moveVector.y * compForward + moveVector.x * compRight : negYFudge * moveVector.y * compForward + moveVector.x * compRight;

            // rotate player model towards direction of movement
            if (moveDirection.sqrMagnitude > 0.025f)
            {
                Quaternion targetDir = Quaternion.LookRotation(moveDirection);
                targetDir = Quaternion.Euler(targetDir.eulerAngles + new Vector3(0, -90, 0));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetDir, 180 * Time.deltaTime);
            }
            
            moveDirection *= speed;
            if (player.GetButton(JumpButton))
                moveDirection.y = jumpSpeed;

        }
        moveDirection.y -= gravity * Time.deltaTime;
        // Debug.Log(moveDirection);
        controller.Move(moveDirection * Time.deltaTime);

        // set IsWalking in child animator controller to true if moving 
        Animator anim = playerModel.GetComponent<Animator>();
        anim.SetBool("IsWalking", moveDirection.sqrMagnitude > 1 ? true : false);
    }
}