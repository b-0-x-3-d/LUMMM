using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe : MonoBehaviour
{
    public Transform connection;
    public Vector3 enterDirection = Vector3.down;
    public Vector3 exitDirection = Vector3.zero;
    public AudioClip warpEnterSound;

    private Vector3 originalScale;
    private AudioSource audioSource;
    private bool isEnteringPipe = false; // Track if the player is already entering the pipe

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isEnteringPipe && connection != null && other.CompareTag("Player"))
        {
            KeyCode warpKey = GetWarpKeyFromRotation();
            if (Input.GetKey(warpKey))
            {
                StartCoroutine(Enter(other.transform));
            }
        }
    }

    private IEnumerator Enter(Transform player)
    {
        isEnteringPipe = true; // Set the flag to prevent re-entry

        PlayWarpEnterSound();

        // Disable the player's movement and gravity
        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.isKinematic = true;
        }

        // Disable the player's collider
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        player.GetComponent<MarioMovement>().enabled = false;

        Vector3 enteredPosition = transform.position + enterDirection;
        originalScale = player.localScale; // Store the original scale
        Vector3 enteredScale = Vector3.one * 0.5f;

        yield return Move(player, enteredPosition, enteredScale);
        yield return new WaitForSeconds(1f);

        if (exitDirection != Vector3.zero)
        {
            player.position = connection.position - exitDirection;
            yield return Move(player, connection.position + exitDirection, originalScale);
        }
        else
        {
            player.position = connection.position;
            player.localScale = originalScale; // Restore the original scale
        }

        // Re-enable the player's movement and gravity
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }

        // Re-enable the player's collider
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        player.GetComponent<MarioMovement>().enabled = true;

        isEnteringPipe = false; // Reset the flag for the next entry
    }


    private IEnumerator Move(Transform player, Vector3 endPosition, Vector3 endScale)
    {
        float elapsed = 0f;
        float duration = 1f;

        Vector3 startPosition = player.position;
        Vector3 startScale = player.localScale;
        PlayWarpEnterSound();

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            player.position = Vector3.Lerp(startPosition, endPosition, t);
            player.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;

            yield return null;
        }
        player.position = endPosition;
        player.localScale = endScale;
    }

    // This is the added method from the pipeSystem script
    private KeyCode GetWarpKeyFromRotation()
    {
        float angle = transform.eulerAngles.z;

        if (angle < 45f || angle >= 315f)
        {
            return KeyCode.DownArrow; // Facing down
        }
        else if (angle >= 45f && angle < 135f)
        {
            return KeyCode.RightArrow; // Facing right
        }
        else if (angle >= 135f && angle < 225f)
        {
            return KeyCode.UpArrow; // Facing up
        }
        else
        {
            return KeyCode.LeftArrow; // Facing left
        }
    }

    // This is the added audio functionality to play the warp enter sound
    private void PlayWarpEnterSound()
    {
        if (warpEnterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(warpEnterSound);
        }
    }
}
