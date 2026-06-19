using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _disablePlayerOnFinish = true;

    // Track who finished to prevent double-reporting
    private HashSet<int> _finishedActors = new HashSet<int>();

    private void Reset()
    {
        _finishedActors.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Local player check only
        PhotonView pv = other.GetComponentInParent<PhotonView>();
        if (pv == null || !pv.IsMine)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        // Prevent double-reporting
        if (!_finishedActors.Add(actorNumber))
            return;

        Debug.Log($"[FinishLine] Player {actorNumber} finished!");

        // Report to RaceStageManager (preferred) or RoundManager
        if (RaceStageManager.Instance != null)
        {
            RaceStageManager.Instance.PlayerReachedFinishLine(PhotonNetwork.LocalPlayer);
        }
        else if (RoundManager.Instance != null)
        {
            RoundManager.Instance.ReportPlayerFinished(actorNumber);
        }

        // Disable local player control on finish
        if (_disablePlayerOnFinish)
        {
            PlayerPresenter presenter = pv.GetComponent<PlayerPresenter>();
            if (presenter != null)
                presenter.enabled = false;

            Rigidbody rb = pv.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }
}
