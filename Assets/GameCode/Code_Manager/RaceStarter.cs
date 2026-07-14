using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 레이스 시작 전 카운트다운(10…1 → Ready… → Start!)을 오케스트레이션합니다.
/// 
/// [네트워크 동기화 방식]
/// - MasterClient가 PhotonNetwork.Time 기준 시작 시각을 RPC로 전송
/// - 모든 클라이언트가 동일한 시각 기준으로 카운트다운을 로컬 실행
/// - 종료 시점에 RaceStageManager.StartRace() + StartLineController.OpenBarrier() 호출
/// 
/// [오프라인 모드]
/// - UNITY_EDITOR + !PhotonNetwork.InRoom 환경에서 Time.time 기반 카운트다운 실행
/// </summary>
public class RaceStarter : MonoBehaviourPun
{
    [Header("Settings")]
    [SerializeField] private float _countdownDuration = 10f;
    [SerializeField] private float _spawnGracePeriod = 2.5f;
    [SerializeField] private float _safetyTimeout = 8f;

    [Header("References")]
    [SerializeField] private StartLineController _startLine;
    [SerializeField] private UI_RaceCountdown _countdownUI;

    [Header("Debug")]
    [SerializeField] private bool _debugSkipCountdown = false;

    private bool _countdownStarted;
    private bool _raceStarted;
    private Coroutine _countdownCoroutine;
    private Coroutine _graceCoroutine;
    private Coroutine _safetyCoroutine;
    private double _countdownEndTime; // PhotonNetwork.Time 기준

    private void Start()
    {
        TryAutoFindReferences();

        if (PhotonNetwork.InRoom)
        {
            // 모든 플레이어가 씬에 로드되었음이 보장된 상태(LoadingGate 완료) -> 잠시 대기 후 시작
            _graceCoroutine = StartCoroutine(WaitForGraceAndBegin());
        }
#if UNITY_EDITOR
        else
        {
            Debug.Log("[RaceStarter] Offline mode -- starting countdown.");
            _graceCoroutine = StartCoroutine(OfflineCountdown());
        }
#endif
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _countdownCoroutine = null;
        _graceCoroutine = null;
        _safetyCoroutine = null;
    }

    private void TryAutoFindReferences()
    {
        if (_startLine == null)
        {
            _startLine = GetComponentInChildren<StartLineController>(true);
            if (_startLine != null)
                Debug.Log("[RaceStarter] Auto-found StartLineController in children.");
            else
                Debug.LogWarning("[RaceStarter] StartLineController not assigned and not found in children.");
        }

        if (_countdownUI == null)
        {
            _countdownUI = FindFirstObjectByType<UI_RaceCountdown>();
            if (_countdownUI != null)
                Debug.Log("[RaceStarter] Auto-found UI_RaceCountdown in scene.");
            else
                Debug.LogWarning("[RaceStarter] UI_RaceCountdown not assigned and not found in scene.");
        }
    }

    /// <summary>
    /// 안전장치: RPC 도착을 기다리다가 _safetyTimeout 초과 시 로컬에서 직접 시작.
    /// MasterClient 이탈, 네트워크 RPC 유실 등에 대비.
    /// </summary>
    private IEnumerator SafetyTimeout()
    {
        yield return new WaitForSeconds(_safetyTimeout);

        if (_countdownStarted || _raceStarted)
            yield break;

        Debug.LogWarning("[RaceStarter] Safety timeout triggered -- countdown RPC never arrived. Starting locally.");
        StartLocalCountdown(PhotonNetwork.Time + 0.5);
    }

    // ─── 네트워크 모드 ────────────────────────────────────────

    private IEnumerator WaitForGraceAndBegin()
    {
        yield return new WaitForSeconds(_spawnGracePeriod);

        // MasterClient가 아니면 안전장치 타임아웃만 실행
        if (!PhotonNetwork.IsMasterClient)
        {
            _safetyCoroutine = StartCoroutine(SafetyTimeout());
            yield break;
        }

        if (photonView == null)
        {
            Debug.LogError("[RaceStarter] photonView is null. Cannot start countdown via RPC.");
            StartLocalCountdown(PhotonNetwork.Time + 0.5);
            yield break;
        }

        double startTime = PhotonNetwork.Time;
        photonView.RPC(nameof(RPC_StartCountdown), RpcTarget.All, startTime);
    }

    [PunRPC]
    private void RPC_StartCountdown(double serverStartTime)
    {
        if (_countdownStarted)
            return;

        StartLocalCountdown(serverStartTime);
    }

    /// <summary>RPC/안전장치 모두 이 진입점으로 통합.</summary>
    private void StartLocalCountdown(double serverStartTime)
    {
        _countdownStarted = true;

        // 안전장치 코루틴 제거 (있었다면)
        if (_safetyCoroutine != null)
        {
            StopCoroutine(_safetyCoroutine);
            _safetyCoroutine = null;
        }

        _countdownEndTime = serverStartTime + _countdownDuration;

        if (_debugSkipCountdown)
        {
            FinalizeRaceStart();
            return;
        }

        _countdownCoroutine = StartCoroutine(RunCountdown());
    }

    private IEnumerator RunCountdown()
    {
        while (true)
        {
            double remaining = _countdownEndTime - PhotonNetwork.Time;
            if (remaining <= 0.0)
                break;

            int secondsLeft = Mathf.CeilToInt((float)remaining);

            if (_countdownUI != null)
                _countdownUI.ShowNumber(Mathf.Clamp(secondsLeft, 1, 999));

            yield return null;
        }

        // 카운트다운 종료 -> Ready... -> Start!
        yield return StartCoroutine(PlayReadyStartSequence());
        FinalizeRaceStart();
    }

    private IEnumerator PlayReadyStartSequence()
    {
        if (_countdownUI != null)
        {
            _countdownUI.ShowReady();
            yield return new WaitForSeconds(_countdownUI.ReadyDuration);

            _countdownUI.ShowStart();
            yield return new WaitForSeconds(_countdownUI.StartDuration);

            _countdownUI.Hide();
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }
    }

    private void FinalizeRaceStart()
    {
        if (_raceStarted)
            return;

        _raceStarted = true;
        _countdownStarted = false;

        // StartLine 개방
        if (_startLine != null)
            _startLine.OpenBarrier();
        else
            Debug.LogWarning("[RaceStarter] _startLine is null. Cannot open barrier.");

        // RaceStageManager 시작
        if (RaceStageManager.Instance != null)
        {
            RaceStageManager.Instance.StartRace();
        }
        else
        {
            Debug.LogWarning("[RaceStarter] RaceStageManager.Instance is null. Race cannot start.");
        }

        Debug.Log("[RaceStarter] Race started!");
    }

    // ─── 오프라인(에디터) 모드 ────────────────────────────────

#if UNITY_EDITOR
    private IEnumerator OfflineCountdown()
    {
        yield return new WaitForSeconds(_spawnGracePeriod);

        float endTime = Time.time + _countdownDuration;

        while (Time.time < endTime)
        {
            int secondsLeft = Mathf.CeilToInt(endTime - Time.time);
            if (_countdownUI != null)
                _countdownUI.ShowNumber(Mathf.Clamp(secondsLeft, 1, 999));
            yield return null;
        }

        yield return StartCoroutine(PlayReadyStartSequence());
        FinalizeRaceStart();
    }
#endif
}
