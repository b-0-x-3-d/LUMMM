using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class MarioMovement : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float moveSpeed = 10f;
    public float runSpeed = 20f;
    public float slowDownForce = 5f;
    public Vector2 direction;
    public bool facingRight = true;
    private bool inCrouchState = false;

    [Header("Vertical Movement")]
    public float jumpSpeed = 15f;
    public float jumpDelay = 0.25f;
    public float terminalvelocity = 10f;
    public float startfallingspeed = 1f;
    private float jumpTimer;
    private bool jumpPressed = false;

    [Header("Components")]
    public Rigidbody2D rb;
    private Animator animator;
    public LayerMask groundLayer;
    private MarioAbility marioAbility;

    [Header("Child Objects")]
    public GameObject heldObjectPosition;
    private GameObject relPosObj;

    [Header("Physics")]
    public float maxSpeed = 7f;
    public float maxRunSpeed = 10f;
    public float linearDrag = 4f;
    public float runlinearDrag = 2f;
    public float gravity = 0;
    public float fallgravity = 5f;
    public float airtime = 1f;
    private float airtimer = 0;
    private bool changingDirections;

    [Header("Collision")]
    public bool onGround = false;
    public float groundLength = 0.6f;
    public bool onMovingPlatform = false;
    public float ceilingLength = 0.5f;

    float colliderY;
    float collideroffsetY;

    public Vector3 colliderOffset;

    public float damageinvinctime = 3f;
    public float invincetimeremain = 0f;

    public Vector2 groundPos;
    public enum PowerupState {

        small,
        big,
        power
    }

    [Header("Animation Events")]
    private bool isYeahAnimationPlaying = false;
    private bool hasEnteredAnimationYeahTrigger = false;

    [Header("Powerups")]

    public PowerupState powerupState = PowerupState.small;

    //public GameObject BigMario;
    public GameObject powerDownMario;

    // so that you can only hurt the player once per frame
    private bool damaged = false;

    [HideInInspector]
    public bool starPower = false;
    private Color[] StarColors = { Color.green, Color.yellow, Color.blue, Color.red };
    private int selectedStarColor = 0;

    [Header("Death")]

    public GameObject deadMario;

    private bool dead = false;

    [Header("Sound Effects")]

    public AudioClip damageSound;
    public AudioClip bonkSound;
    public AudioClip yeahAudioClip;
    public AudioClip MaaaamaMiaAudioClip;
    private bool mamaMiaAudioPlayed = false;
    private AudioSource audioSource;

    private bool frozen = false;

    [Header("Carrying")]

    public bool carrying = false;
    public bool pressRunToGrab = false;
    public bool crouchToGrab = false;
    public enum CarryMethod {
        inFront,
        onHand
    }
    public CarryMethod carryMethod = CarryMethod.onHand;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip throwSound;

    private float grabRaycastHeight => powerupState == PowerupState.small ? -0.1f : -0.4f;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        colliderY = GetComponent<BoxCollider2D>().size.y;
        collideroffsetY = GetComponent<BoxCollider2D>().offset.y;
        relPosObj = transform.GetChild(0).gameObject;
        animator = GetComponent<Animator>();
        animator.SetInteger("grabMethod", (int)carryMethod);
        if (powerupState == PowerupState.power) {
            marioAbility = GetComponent<MarioAbility>();
        }
    }

    // Update is called once per frame
    void Update()
    {

        direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();

        if (invincetimeremain > 0f) {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.5f);
            invincetimeremain -= Time.deltaTime;
        } else {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1.0f);
            invincetimeremain = 0f;
        }

        bool crouch = (direction.y < -0.5);

        // Picking up item
        if (((pressRunToGrab && Input.GetButtonDown("Fire3")) || (!pressRunToGrab && Input.GetButton("Fire3"))) && (!crouchToGrab || crouch) && !carrying) {
            checkForCarry();
        }

        // shoot fireball, etc
        if (!carrying && Input.GetButtonDown("Fire3") && powerupState == PowerupState.power) {
            //print("shoot!");
            marioAbility.shootProjectile();
        }

        // Throwing item
        if (carrying && !Input.GetButton("Fire3")) {
            if (crouch) {
                dropCarry();
            } else {
                throwCarry();
            }
        }

    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (groundLayer == (groundLayer | (1 << other.gameObject.layer))) {
            Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for (int i = 0; i < contactCount; i++) {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }

            if (impulse.y < 0) {
                audioSource.PlayOneShot(bonkSound, 0.5f);
            }
        }
    }

    private void changeRainbowColor() {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.color = StarColors[selectedStarColor];
        selectedStarColor += 1;
        if (selectedStarColor == StarColors.Length)
            selectedStarColor = 0;
    }



    public void startStarPower(float time) {
        // stop any current star power
        CancelInvoke(nameof(stopStarPower));
        CancelInvoke(nameof(changeRainbowColor));

        // start new star power
        InvokeRepeating(nameof(changeRainbowColor), 0, 0.1f);
        starPower = true;
        if (time != -1) {
            Invoke(nameof(stopStarPower), time);
        }
    }

    public void stopStarPower() {
        CancelInvoke(nameof(changeRainbowColor));
        starPower = false;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.color = new Color(1, 1, 1, sprite.color.a);
    }

    private void FixedUpdate() {

        if (frozen) {
            return;
        }

        // Movement
        moveCharacter(direction.x);
        if (jumpTimer > Time.time && onGround) {
            audioSource.Play();
            Jump();
        }

        // Floor detection
        onMovingPlatform = false;
        transform.parent = null;

        RaycastHit2D groundHit1 = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, groundLayer);
        RaycastHit2D groundHit2 = Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, groundLayer);

        onGround = groundHit1 || groundHit2;

        if (onGround) {

            RaycastHit2D hitRay = groundHit1;

            // print("ground1: " + groundHit1.transform.gameObject.tag);
            // print("ground2: " + groundHit2.transform.gameObject.tag);

            if (groundHit1) {
                hitRay = groundHit1;
                if (groundHit1.transform.gameObject.tag == "MovingPlatform") {
                    onMovingPlatform = true;
                }
            } else {
                hitRay = groundHit2;
                if (groundHit2.transform.gameObject.tag == "MovingPlatform") {
                    onMovingPlatform = true;
                }
            }

            groundPos = hitRay.point;

            if (onMovingPlatform) {
                transform.parent = hitRay.transform;
            } else {
                transform.parent = null;
            }

            if (hitRay.transform.gameObject.tag == "Damaging") {
                damageMario();
            }
        }

        // Ceiling detection
        RaycastHit2D ceilLeft = Physics2D.Raycast(transform.position - colliderOffset, Vector2.up, ceilingLength, groundLayer);
        RaycastHit2D ceilMid = Physics2D.Raycast(transform.position, Vector2.up, ceilingLength, groundLayer);
        RaycastHit2D ceilRight = Physics2D.Raycast(transform.position + colliderOffset, Vector2.up, ceilingLength, groundLayer);

        if (ceilLeft.collider != null || ceilMid.collider != null || ceilRight.collider != null) {

            RaycastHit2D hitRay = ceilMid;

            if (ceilMid) {
                hitRay = ceilMid;
            } else if (ceilLeft) {
                hitRay = ceilLeft;
            } else if (ceilRight) {
                hitRay = ceilRight;
            }


            //if (hitRay.collider.tag == "QuestionBlock") {
            //
            //    hitRay.collider.GetComponent<QuestionBlock>().QuestionBlockBounce();
            //} else if (hitRay.collider.tag == "BrickBlock") {
            //    if (powerupState == PowerupState.small) {
            //        hitRay.collider.GetComponent<QuestionBlock>().QuestionBlockBounce();
            //    } else {
            //        hitRay.collider.GetComponent<QuestionBlock>().BrickBlockBreak();
            //    }
            //}
        }

        // Physics
        modifyPhysics();


    }
    void moveCharacter(float horizontal) {
        bool crouch = (direction.y < -0.5);

        // Crouching
        if (crouch && powerupState != PowerupState.small && onGround && !carrying) {

            // Start Crouch
            animator.SetBool("isCrouching", true);
            inCrouchState = true;
            GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, 1.0f);
            GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, -0.5f);
            ceilingLength = 0.1f;

        } else if ((!crouch && onGround) || (powerupState == PowerupState.small) || carrying) {

            // Stop Crouch
            animator.SetBool("isCrouching", false);
            inCrouchState = false;
            if (powerupState != PowerupState.small) {
                GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, colliderY);
                GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, collideroffsetY);
                ceilingLength = 1.03f; // NOTE: THIS IS BAD PROGRAMMING BUT WHATEVER
            }

        }

        // Running or Walking
        if (!inCrouchState || !onGround) {
            if (Input.GetButton("Fire3")) {
                rb.AddForce(Vector2.right * horizontal * runSpeed);
            } else {
                if (Mathf.Abs(rb.velocity.x) <= maxSpeed) {
                    rb.AddForce(Vector2.right * horizontal * moveSpeed);
                } else {
                    rb.AddForce(Vector2.left * slowDownForce * Mathf.Sign(rb.velocity.x));
                }
            }
        }

        // Crouching
        if (onGround && Input.GetAxisRaw("Vertical") < 0)
        {
            inCrouchState = true;
            
        }
        else
        {
            inCrouchState = false;
            
        }

        // Changing Direction
        if (onGround) {
            if ((horizontal > 0 && !facingRight) || (horizontal < 0 && facingRight)) {
                Flip();
            }
            if (horizontal == 0) {
                if ((rb.velocity.x > 0 && !facingRight) || (rb.velocity.x < 0 && facingRight)) {
                    Flip();
                }
            }
        }

        if (Input.GetButton("Fire3")) {
            if (Mathf.Abs(rb.velocity.x) > maxRunSpeed) {
                rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxRunSpeed, rb.velocity.y);
            }
        } //else {
          //  if (Mathf.Abs(rb.velocity.x) > maxSpeed) {
          //      rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxSpeed, rb.velocity.y);
          //  }
          //}
        if (-rb.velocity.y > terminalvelocity) {
            rb.velocity = new Vector2(rb.velocity.x, -terminalvelocity);
        }
        animator.SetFloat("Horizontal", Mathf.Abs(rb.velocity.x) / 8);
        if (Mathf.Abs(rb.velocity.x) <= 0.5f) {
            animator.SetBool("isRunning", false);
        } else {
            animator.SetBool("isRunning", true);
        }

        if (onGround) {
            if (!inCrouchState) {
                animator.SetBool("isSkidding", changingDirections);
            }
            animator.SetBool("onGround", true);
        } else {
            animator.SetBool("isSkidding", false);
            animator.SetBool("onGround", false);
        }
    }

    // for jumping and also stomping enemies
    public void Jump() {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
        jumpTimer = 0;
        airtimer = Time.time + airtime;
    }

    void modifyPhysics() {
        changingDirections = (direction.x > 0 && rb.velocity.x < 0) || (direction.x < 0 && rb.velocity.x > 0);

        if (onGround) {
            // no crazy crouch sliding
            if (inCrouchState)
            {
                direction = new Vector2(0, direction.y);
                animator.SetBool("isCrouching", true);
            }
            else
            {
                animator.SetBool("isCrouching", false);
            }             

            // if not holding left or right all the way or changing directions
            if (Mathf.Abs(direction.x) < 0.4f || changingDirections) {

                // if running and holding left or right
                if ((Input.GetButton("Fire3")) && (direction.x != 0)) {
                    rb.drag = runlinearDrag;

                    // if not holding left or right
                } else if (direction.x == 0) {

                    //if ((facingRight && rb.velocity.x < 0) || (!facingRight && rb.velocity.x > 0)) {
                    if (Mathf.Abs(rb.velocity.x) < 5f) {
                        rb.drag = linearDrag;
                    } else {
                        rb.drag = 3 * runlinearDrag;
                    }
                    //rb.drag = 3 * runlinearDrag;

                    // if walking left or right (not all the way or changing directions)
                } else {
                    //if ((facingRight && rb.velocity.x < 0) || (!facingRight && rb.velocity.x > 0)) {
                    //    rb.drag = linearDrag * 2;
                    //} else {
                    rb.drag = linearDrag;
                    //}
                }

                // if holding left or right all the way and not changing directions
            } else {
                rb.drag = 0;
            }

            // 0 gravity on the ground
            rb.gravityScale = 0;

            // if falling while on the ground, set y velocity to 0
            if (rb.velocity.y < 0) {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            // Stick to ground
            if (!onMovingPlatform) {
                transform.position = new Vector3(transform.position.x, groundPos.y + groundLength - 0.01f, transform.position.z);
            }


        } else {
            // in the air
            rb.gravityScale = gravity;
            rb.drag = linearDrag * 0.15f;
            //if(rb.velocity.y < startfallingspeed){

            // Rising
            if (airtimer < Time.time || rb.velocity.y < startfallingspeed) {
                rb.gravityScale = fallgravity;

                // Falling
            } else if (rb.velocity.y > 0 && !jumpPressed) {
                rb.gravityScale = fallgravity;
                airtimer = Time.time - 1f;
                //rb.gravityScale = gravity * fallMultiplier;
            }
        }
    }
    
    void Flip() {
        facingRight = !facingRight;
        //transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
        GetComponent<SpriteRenderer>().flipX = !facingRight;
        float relScaleX = facingRight ? 1 : -1;

        // flip might be called before start, this fixes that
        if (relPosObj == null) {
            relPosObj = transform.GetChild(0).gameObject;
        }

        relPosObj.transform.localScale = new Vector3(relScaleX, 1, 1);
    }

    public bool IsBelowBlock(float blockYPosition)
    {
        // Check if Mario's height is below a certain point relative to the block
        return transform.position.y < blockYPosition - 0.5f; // You can adjust the value (0.5f) as needed
    }

    public void damageMario() {
        if (invincetimeremain == 0f) {
            if (damaged) {
                return;
            }
            // are we invincible mario?
            if (starPower) {
                return;
            }
            damaged = true;
            if (powerupState == PowerupState.small) {
                toDead();
            } else {
                powerDown();
            }
        }
    }

    private void powerDown() {
        GameObject newMario;
        if (powerupState == PowerupState.big) {
            // go down to Small Mario
            newMario = Instantiate(powerDownMario, new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z), transform.rotation);
        } else {
            // go down to Big Mario
            newMario = Instantiate(powerDownMario, new Vector3(transform.position.x, transform.position.y, transform.position.z), transform.rotation);
        }
        
        var newMarioMovement = transferProperties(newMario);

        newMarioMovement.invincetimeremain = damageinvinctime;
        newMarioMovement.playDamageSound();

        Destroy(gameObject);
    }

    public void ChangePowerup(GameObject newMarioObject) {
        GameObject newMario;
        if (powerupState == PowerupState.small) {
            // place big mario higher up than small mario
            newMario = Instantiate(newMarioObject, new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z), Quaternion.identity);
        } else {
            // no need to change position
            newMario = Instantiate(newMarioObject, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
        }
        
        transferProperties(newMario);

        Destroy(gameObject);
    }

    MarioMovement transferProperties(GameObject newMario) {
        newMario.GetComponent<Rigidbody2D>().velocity = gameObject.GetComponent<Rigidbody2D>().velocity;
        var newMarioMovement = newMario.GetComponent<MarioMovement>();

        if (!facingRight)
            newMarioMovement.Flip();

        newMarioMovement.pressRunToGrab = pressRunToGrab;
        newMarioMovement.crouchToGrab = crouchToGrab;
        newMarioMovement.carryMethod = carryMethod;
        
        if (carrying) {
            // move carried object to new mario
            GameObject carriedObject = heldObjectPosition.transform.GetChild(0).gameObject;
            carriedObject.transform.parent = newMarioMovement.heldObjectPosition.transform;
            carriedObject.transform.localPosition = Vector3.zero;
            newMarioMovement.carrying = true;
        }

        return newMarioMovement;
    }

    public void playDamageSound() {
        GetComponent<AudioSource>().PlayOneShot(damageSound);
    }

    private void toDead() {
        //print("death attempt");
        if (!dead) {
            dead = true;
            GameObject newMario = Instantiate(deadMario, transform.position, transform.rotation);
            Destroy(gameObject);
            //print("death success");
        }
    }

    public void PlayYeahAnimation()
    {
        if (!isYeahAnimationPlaying && !hasEnteredAnimationYeahTrigger)
        {
            StartCoroutine(PlayYeahAnimationWithAudio());
        }
    }

    private IEnumerator PlayYeahAnimationWithAudio()
    {
        isYeahAnimationPlaying = true;
        hasEnteredAnimationYeahTrigger = true;

        // Start playing the "yeah" animation
        animator.SetBool("yeah", true);

        // Play the audio clip
        audioSource.PlayOneShot(yeahAudioClip);

        // Wait for the audio clip to finish playing
        yield return new WaitForSeconds(yeahAudioClip.length + 0.2f);

        // Stop the "yeah" animation and set the parameter to false
        animator.SetBool("yeah", false);
        isYeahAnimationPlaying = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // firebar, flame, etc
        if (other.gameObject.tag == "Damaging")
        {
            damageMario();
        }
        // Lava
        if (other.gameObject.CompareTag("Deadly"))
        {
            toDead();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "AnimationWorried")
        {
            animator.SetBool("isWorried", true);

            // Play MaaaamaMiaAudioClip only if it has not been played before
            if (!mamaMiaAudioPlayed)
            {
                mamaMiaAudioPlayed = true;
                audioSource.PlayOneShot(MaaaamaMiaAudioClip);
            }
        }

        // Check if the trigger collider is the one you want to trigger the "yeah" animation
        if (other.gameObject.CompareTag("AnimationYeah"))
        {
            PlayYeahAnimation();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "AnimationWorried")
        {
            animator.SetBool("isWorried", false);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // spikes
        if (collision.gameObject.tag == "Damaging")
        {
            damageMario();
        }
    }

    private void OnDrawGizmos() {
        // Ground
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);

        // Ceiling
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * ceilingLength);
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.up * ceilingLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.up * ceilingLength);

        // Carry Raycast
        Gizmos.color = Color.blue;
        
        Vector3 start = transform.position + new Vector3(0, grabRaycastHeight, 0);
        Gizmos.DrawLine(start, start + (facingRight ? Vector3.right : Vector3.left) * 0.6f);

    }

    public void Move(InputAction.CallbackContext context)
    {

    }

    public void Run(InputAction.CallbackContext context)
    {

    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpTimer = Time.time + jumpDelay;
            jumpPressed = true;
        }
        if (context.canceled)
        {
            jumpPressed = false;
        }
        
    }

    public void Crouch(InputAction.CallbackContext context)
    {

    }

    public void Spin(InputAction.CallbackContext context)
    {

    }

    public void Shoot(InputAction.CallbackContext context)
    {

    }

    public void Freeze() {
        // pause animations
        animator.enabled = false;
        // pause physics
        rb.simulated = false;

        frozen = true;
    }

    public void Unfreeze() {
        // unpause animations
        animator.enabled = true;
        // unpause physics
        rb.simulated = true;

        frozen = false;
    }

    void checkForCarry() {
        //print("Check carry");

        // raycast in front of feet of mario
        RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position + new Vector3(0, grabRaycastHeight, 0), facingRight ? Vector2.right : Vector2.left, 0.6f);


        foreach(RaycastHit2D h in hit) {
            // if object has objectphysics script
            if (h.collider.gameObject.GetComponent<ObjectPhysics>() != null) {
                ObjectPhysics obj = h.collider.gameObject.GetComponent<ObjectPhysics>();
                // not carried and carryable
                if (!obj.carried && obj.carryable) {
                    carry(obj);
                    return;
                }
            }
        }
    }

    void carry(ObjectPhysics obj) {
        //print("carry!");
        carrying = true;

        animator.SetTrigger("grab");
        
        // set object to be child of mario's object holder
        obj.transform.parent = heldObjectPosition.transform;

        // sound
        if (pickupSound != null)
            audioSource.PlayOneShot(pickupSound);

        obj.getCarried();
    }

    public void dropCarry() {
        //print("drop!");
        carrying = false;

        animator.SetTrigger("grab");
        
        // get object from mario's object holder
        ObjectPhysics obj = heldObjectPosition.transform.GetChild(0).gameObject.GetComponent<ObjectPhysics>();
        obj.transform.parent = null;
        obj.transform.position = new Vector3(transform.position.x + (facingRight ? 1 : -1), transform.position.y + (powerupState == PowerupState.small ? 0f : -.5f), transform.position.z);
        
        // sound
        if (dropSound != null)
            audioSource.PlayOneShot(dropSound);

        obj.getDropped(facingRight);
    }

    void throwCarry() {
        //print("throw!");

        carrying = false;

        // check if heldObjectPosition has an object
        if (heldObjectPosition.transform.childCount == 0) {
            return;
        }

        // todo: throw animation

        // get object from mario's object holder
        ObjectPhysics obj = heldObjectPosition.transform.GetChild(0).gameObject.GetComponent<ObjectPhysics>();
        obj.transform.parent = null;
        obj.transform.position = new Vector3(transform.position.x + (facingRight ? 1 : -1), transform.position.y + (powerupState == PowerupState.small ? 0.1f : -.1f), transform.position.z);
        
        // sound
        if (throwSound != null)
            audioSource.PlayOneShot(throwSound);

        obj.getThrown(facingRight);
    }

}
