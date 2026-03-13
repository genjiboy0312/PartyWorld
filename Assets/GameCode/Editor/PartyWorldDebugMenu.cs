#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PartyWorldDebugMenu
{
    private const string DEBUG_PREF_AUTO_QUICKPLAY = "PW_DEBUG_AUTO_QUICKPLAY";
    private const string DEBUG_PREF_FORCE_NICKNAME = "PW_DEBUG_FORCE_NICKNAME";

    [MenuItem("Tools/PartyWorld/Debug/Auto QuickPlay")]
    private static void ToggleAutoQuickPlay()
    {
        // WaitingRoom/룸 로비에서 자동 매칭을 켜고 끔
        int current = PlayerPrefs.GetInt(DEBUG_PREF_AUTO_QUICKPLAY, 0);
        int next = current == 1 ? 0 : 1;
        PlayerPrefs.SetInt(DEBUG_PREF_AUTO_QUICKPLAY, next);
        PlayerPrefs.Save();
        Menu.SetChecked("Tools/PartyWorld/Debug/Auto QuickPlay", next == 1);
    }

    [MenuItem("Tools/PartyWorld/Debug/Auto QuickPlay", true)]
    private static bool ToggleAutoQuickPlayValidate()
    {
        Menu.SetChecked("Tools/PartyWorld/Debug/Auto QuickPlay", PlayerPrefs.GetInt(DEBUG_PREF_AUTO_QUICKPLAY, 0) == 1);
        return true;
    }

    [MenuItem("Tools/PartyWorld/Debug/Force Dev NickName")]
    private static void ToggleForceNickName()
    {
        // 디버그 닉네임을 OS 유저명 기반으로 강제
        int current = PlayerPrefs.GetInt(DEBUG_PREF_FORCE_NICKNAME, 0);
        int next = current == 1 ? 0 : 1;
        PlayerPrefs.SetInt(DEBUG_PREF_FORCE_NICKNAME, next);
        PlayerPrefs.Save();
        Menu.SetChecked("Tools/PartyWorld/Debug/Force Dev NickName", next == 1);
    }

    [MenuItem("Tools/PartyWorld/Debug/Force Dev NickName", true)]
    private static bool ToggleForceNickNameValidate()
    {
        Menu.SetChecked("Tools/PartyWorld/Debug/Force Dev NickName", PlayerPrefs.GetInt(DEBUG_PREF_FORCE_NICKNAME, 0) == 1);
        return true;
    }

    [MenuItem("Tools/PartyWorld/Debug/Reset Debug Prefs")]
    private static void ResetDebugPrefs()
    {
        // 디버그 플래그를 기본값으로 초기화
        PlayerPrefs.DeleteKey(DEBUG_PREF_AUTO_QUICKPLAY);
        PlayerPrefs.DeleteKey(DEBUG_PREF_FORCE_NICKNAME);
        PlayerPrefs.Save();

        Menu.SetChecked("Tools/PartyWorld/Debug/Auto QuickPlay", false);
        Menu.SetChecked("Tools/PartyWorld/Debug/Force Dev NickName", false);

        Debug.Log("[PartyWorldDebug] Debug Prefs reset.");
    }
}
#endif
