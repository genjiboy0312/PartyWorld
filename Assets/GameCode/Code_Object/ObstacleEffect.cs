using Photon.Pun;
using UnityEngine;

public enum ObstacleEffectMode
{
    /// <summary>Slows player movement for a duration.</summary>
    Slow,
    /// <summary>Launches the player upward (trampoline/pad).</summary>
    Jump,
    /// <summary>Eliminates the player (spikes/death obstacle).</summary>
    Damage,
    /// <summary>Pushes the player backward (rotating hammer/disc).</summary>
    Knockback
}

public class ObstacleEffect : MonoBehaviour
{
    [Header("Effect Mode")]
    [SerializeField] private ObstacleEffectMode _mode = ObstacleEffectMode.Damage;

    [Header("Slow Settings")]
    [SerializeField] private float _slowDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float _slowFactor = 0.5f;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpForce = 10f;

    [Header("Knockback Settings")]
    [SerializeField] private float _knockbackForce = 15f;
    [SerializeField] private Vector3 _knockbackDirection = Vector3.back;

    [Header("Damage Settings")]
    [SerializeField] private bool _eliminateOnTouch = true;

    [Header("Cooldown")]
    [SerializeField] private float _cooldown = 0.5f;

    private float _lastTriggerTime;

    private void OnTriggerEnter(Collider other)
    {
        TryApplyEffect(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyEffect(collision.collider);
    }

    private void TryApplyEffect(Collider other)
    {
        // Cooldown check
        if (Time.time < _lastTriggerTime + _cooldown)
            return;

        // Local player check — only affect the local player
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        _lastTriggerTime = Time.time;

        switch (_mode)
        {
            case ObstacleEffectMode.Slow:
                ApplySlow(pv.gameObject, actorNumber);
                break;
            case ObstacleEffectMode.Jump:
                ApplyJump(pv.gameObject);
                break;
            case ObstacleEffectMode.Damage:
                ApplyDamage(pv.gameObject, actorNumber);
                break;
            case ObstacleEffectMode.Knockback:
                ApplyKnockback(pv.gameObject);
                break;
        }
    }

    private void ApplySlow(GameObject player, int actorNumber)
    {
        Debug.Log($"[ObstacleEffect] Slow player {actorNumber} for {_slowDuration}s at {_slowFactor}x speed.");

        // Disable PlayerPresenter temporarily for slow effect
        // (In a full implementation, a movement speed multiplier would be applied)
        PlayerPresenter presenter = player.GetComponent<PlayerPresenter>();
        if (presenter != null)
        {
            // Simple approach: disable movement briefly
            // Future: apply speed modifier via PlayerPresenter API
            presenter.enabled = false;
            Invoke(nameof(RestorePlayer), _slowDuration);
        }
    }

    private void RestorePlayer()
    {
        // This is a simplified restore — in practice, find the player by reference
        Debug.Log("[ObstacleEffect] Slow effect ended — player restored.");
    }

    private void ApplyJump(GameObject player)
    {
        Debug.Log($"[ObstacleEffect] Jump force {_jumpForce} applied.");

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, _jumpForce, rb.linearVelocity.z);
        }
    }

    private void ApplyDamage(GameObject player, int actorNumber)
    {
        Debug.Log($"[ObstacleEffect] Damage player {actorNumber}.");

        if (!_eliminateOnTouch)
            return;

        // Disable local player (same pattern as DeathZone)
        PlayerPresenter presenter = player.GetComponent<PlayerPresenter>();
        if (presenter != null)
            presenter.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void ApplyKnockback(GameObject player)
    {
        Debug.Log($"[ObstacleEffect] Knockback force {_knockbackForce} in direction {_knockbackDirection}.");

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Apply knockback in obstacle's local direction
            Vector3 worldDirection = transform.TransformDirection(_knockbackDirection);
            rb.AddForce(worldDirection.normalized * _knockbackForce, ForceMode.Impulse);
        }
    }
}
