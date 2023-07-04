using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class QuestionBlock : MonoBehaviour
{
    public float bounceHeight = 0.5f;
    public float bounceSpeed = 4f;

    public bool brickBlock;

    public bool noCoinIfNoItem = false;

    public GameObject spawnItem;

    public GameObject objectToMove; // Object to move (must be present in the scene)
    public bool moveObjectOnHit = false; // Variable to control whether the object should be moved or not
    public Vector3 targetPosition; // Target position of the object to move
    public float moveSpeed; // Speed of movement for the object

    public float coinMoveSpeed = 8f;
    public float coinMoveHeight = 3f;
    public float coinFallDistance = 2f;

    public float itemMoveHeight = 1f;
    public float itemMoveSpeed = 1f;
    public bool activated = false;
    private Vector2 originalPosition;

    public Sprite emptyBlockSprite;

    private bool canBounce = true;

    public AudioClip itemRiseSound;

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.localPosition;
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (other.gameObject.tag == "Player") {
            Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for(int i = 0; i < contactCount; i++) {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }

            //print(impulse);

            // position comparison is to stop a weird bug where the player can hit the top corner of the block and activate it
            if (impulse.y <= 0 || other.transform.position.y > transform.position.y) {
                return;
            }

            if (!brickBlock) {
                QuestionBlockBounce();
            } else {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                if (playerScript.powerupState == MarioMovement.PowerupState.small) {
                    QuestionBlockBounce();
                } else {
                    BrickBlockBreak();
                }
            }

            if (!moveObjectOnHit)
            {
               // Debug.Log();
            }
        }
    }

    // this is called when a koopa shell hits the block for example
    public void Activate() {
        if (canBounce) {
            if (brickBlock) {
                BrickBlockBreak();
            } else {
                QuestionBlockBounce();
            }
        }
    }

    public void QuestionBlockBounce() {

        if (canBounce) {

            if (spawnItem || !brickBlock) {
                canBounce = false;
            }

            // do whatever timeline stuff here
            if (GetComponent<PlayableDirector>() != null) {
                GetComponent<PlayableDirector>().Play();
            }

            StartCoroutine(Bounce());
        }
    }

    public void BrickBlockBreak () {
        if (spawnItem) {
            QuestionBlockBounce();
        } else {
            GetComponent<BreakableBlocks>().Break();
        }

    }

    void ChangeSprite () {

        if (!brickBlock)
            GetComponent<Animator>().enabled = false;

        GetComponent<SpriteRenderer> ().sprite = emptyBlockSprite;

    }

    void PresentCoin () {

        activated = true;

        if (spawnItem) {
            //print("custom item");
            audioSource.PlayOneShot(itemRiseSound);
            GameObject spawnedItem = (GameObject)Instantiate(spawnItem) as GameObject;
            MonoBehaviour[] scripts = spawnedItem.GetComponents<MonoBehaviour>();
            foreach(MonoBehaviour script in scripts)
            {
                script.enabled = false;
            }

            string ogTag = spawnedItem.tag;
            int ogLayer = spawnedItem.GetComponent<SpriteRenderer>().sortingLayerID;
            spawnedItem.tag = "RisingItem";
            spawnedItem.transform.SetParent (this.transform.parent);
            spawnedItem.GetComponent<SpriteRenderer>().sortingLayerID = 0;
            spawnedItem.GetComponent<SpriteRenderer>().sortingOrder = -1;
            spawnedItem.transform.localPosition = new Vector3 (originalPosition.x, originalPosition.y, 0);
            StartCoroutine (RiseUp (spawnedItem, ogTag, ogLayer, scripts));

        } else if (!noCoinIfNoItem){
            audioSource.Play();
            GameObject spinningCoin = (GameObject)Instantiate (Resources.Load("Prefabs/Spinning_Coin", typeof(GameObject)));

            spinningCoin.transform.SetParent (this.transform.parent);

            spinningCoin.transform.localPosition = new Vector2 (originalPosition.x, originalPosition.y + 1);

            StartCoroutine (MoveCoin (spinningCoin)); 
            GameManager.Instance.AddCoin(1); // The coin counter iterates after the coroutine
        }
    }

    IEnumerator Bounce () {

        //print(spawnItem != null);

        if (spawnItem || !brickBlock) {
            ChangeSprite ();
        }
        
        if (!spawnItem && !brickBlock) {
            PresentCoin();
        }
        else if (spawnItem) {
            Invoke("PresentCoin", 0.25f);
        }

        while (true) {

            transform.localPosition = new Vector2 (transform.localPosition.x, transform.localPosition.y + bounceSpeed * Time.deltaTime);

            if (transform.localPosition.y >= originalPosition.y + bounceHeight)
                break;

            yield return null;
        }

        while (true) {

            transform.localPosition = new Vector2 (transform.localPosition.x, transform.localPosition.y - bounceSpeed * Time.deltaTime);

            if (transform.localPosition.y <= originalPosition.y) {

                transform.localPosition = originalPosition;
                break;
            }

            yield return null;
        }
    }

    IEnumerator MoveCoin (GameObject coin) {

        while (true) {

            coin.transform.localPosition = new Vector2 (coin.transform.localPosition.x, coin.transform.localPosition.y + coinMoveSpeed * Time.deltaTime);

            if (coin.transform.localPosition.y >= originalPosition.y + coinMoveHeight + 1)
                break;
            
            yield return null;
        }

        while (true) {

            coin.transform.localPosition = new Vector2 (coin.transform.localPosition.x, coin.transform.localPosition.y - coinMoveSpeed * Time.deltaTime);

            if (coin.transform.localPosition.y <= originalPosition.y + coinFallDistance + 1) {

                Destroy(coin.gameObject);
                break;
            }

            yield return null;
        }
    }

    IEnumerator RiseUp(GameObject item, string ogTag, int ogLayer, MonoBehaviour[] scripts) {

        while (true) {

            //item.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            
            item.transform.localPosition = new Vector3 (item.transform.localPosition.x, item.transform.localPosition.y + itemMoveSpeed * Time.deltaTime, 0);
            //print(item.transform.localPosition.y + "vs" + originalPosition.y);
            if (item.transform.localPosition.y >= originalPosition.y + itemMoveHeight) {
                foreach(MonoBehaviour script in scripts)
                {
                    script.enabled = true;
                }
                item.tag = ogTag;
                item.GetComponent<SpriteRenderer>().sortingLayerID = ogLayer;
                item.GetComponent<SpriteRenderer>().sortingOrder = 0;
                break;
            }
            yield return null;
        }
    }
}
