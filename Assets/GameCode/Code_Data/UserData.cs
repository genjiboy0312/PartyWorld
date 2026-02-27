using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserData
{
    // --- 유저 기본 정보 ---
    public string userId;          // Firebase UID (고유 식별자)
    public string nickname;        // 유저 닉네임
    public int money;              // 보유 재화 (골드 등)
    public int level;              // 현재 레벨
    public int exp;                // 현재 경험치

    // --- 아이템 관련 ---
    public List<string> inventory; // 보유 중인 아이템 ID 리스트

    // --- 기타 상태 (필요 시 확장) ---
    public int lastLoginTime;      // 마지막 로그인 타임스탬프

    // 기본 생성자 (객체 초기화용)
    public UserData()
    {
        userId = "";
        nickname = "NewPlayer";
        money = 0;
        level = 1;
        exp = 0;
        inventory = new List<string>();
        lastLoginTime = 0;
    }

    // JSON으로 변환 (Firebase 저장용)
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    // JSON 문자열에서 UserData 객체 생성 (데이터 로드용)
    public static UserData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return new UserData();
        return JsonUtility.FromJson<UserData>(json);
    }
}
