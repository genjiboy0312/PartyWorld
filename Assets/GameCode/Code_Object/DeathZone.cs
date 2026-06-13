using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _eliminateOnTouch = true;
    [SerializeField] private bool _respawnOnTouch;

    private void OnTriggerEnter(Collider other)
    {
        // Local player check — only the local player should report themselves
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[DeathZone] Player {actorNumber} entered death zone.");

        // Report to RoundManager
        RoundManager.Instance?.ReportPlayerEliminated(actorNumber);

        // Handle respawn or disable
        if (_respawnOnTouch)
        {
            // Move back to spawn point (handled by PlayerPresenter or spawn system)
            TryRespawn(pv.gameObject);
        }
        else if (_eliminateOnTouch)
        {
            // Disable local control so player can't move
            SetPlayerInactive(pv.gameObject);
        }
    }

    private void TryRespawn(GameObject player)
    {
        // Simple respawn: move to origin + random offset
        Vector3 respawnPos = new Vector3(
            Random.Range(-2f, 2f),
            1f,
            Random.Range(-2f, 2f)
        );
        player.transform.position = respawnPos;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SetPlayerInactive(GameObject player)
    {
        // Disable movement scripts, enable ragdoll or spectator cam
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
}
