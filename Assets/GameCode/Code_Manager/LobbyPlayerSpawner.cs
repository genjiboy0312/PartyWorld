using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class LobbyPlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform _fallbackSpawnPoint;
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private bool _forceRespawnOnLobbyEnter = true;
    [SerializeField] private float _spawnRetrySeconds = 10f;

    private void Start()
    {
        if (_spawnOnStart)
            StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        float elapsed = 0f;
        while (elapsed < _spawnRetrySeconds)
        {
            if (PhotonNetwork.InRoom && NetworkAuthorityManager.Instance != null)
            {
                if (SpawnLocal())
                    yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (PhotonNetwork.InRoom && NetworkAuthorityManager.Instance != null)
            SpawnLocal();
    }

    public bool SpawnLocal()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return false;

        NetworkAuthorityManager manager = NetworkAuthorityManager.Instance;
        if (manager == null)
            return false;

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        ResolveSpawnPose(out spawnPosition, out spawnRotation);

        GameObject spawned = manager.SpawnLocalSelectedCharacter(spawnPosition, spawnRotation, _forceRespawnOnLobbyEnter);
        return spawned != null;
    }

    private void ResolveSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (_spawnPoints != null && _spawnPoints.Count > 0)
        {
            int idx = Mathf.Abs(PhotonNetwork.LocalPlayer.ActorNumber - 1) % _spawnPoints.Count;
            Transform point = _spawnPoints[idx];
            if (point != null)
            {
                position = point.position;
                rotation = point.rotation;
                return;
            }
        }

        if (_fallbackSpawnPoint != null)
        {
            position = _fallbackSpawnPoint.position;
            rotation = _fallbackSpawnPoint.rotation;
        }
    }
}
