using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class UserData
{
    public string userId;          // Firebase UID (고유 식별자)
    public string nickname;        // 유저 닉네임
    public int money;              // 보유 재화 (골드 등)
    public int level;              // 현재 레벨
    public int exp;                // 현재 경험치
    public string selectedCharacterId;

    public List<string> inventory; // 보유 중인 아이템 ID 리스트

    public int dataVersion;
    public long createdAt;
    public long updatedAt;

    [FormerlySerializedAs("lastLoginTime")]
    public long lastLoginAt;

    public UserData()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        userId = "";
        nickname = "NewPlayer";
        money = 0;
        level = 1;
        exp = 0;
        selectedCharacterId = "";
        inventory = new List<string>();
        dataVersion = 1;
        createdAt = now;
        updatedAt = now;
        lastLoginAt = now;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static UserData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return new UserData();

        UserData data = JsonUtility.FromJson<UserData>(json);
        if (data == null)
            return new UserData();

        if (data.inventory == null)
            data.inventory = new List<string>();

        if (data.dataVersion <= 0)
            data.dataVersion = 1;

        if (data.createdAt <= 0)
            data.createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (data.updatedAt <= 0)
            data.updatedAt = data.createdAt;

        if (data.lastLoginAt <= 0)
            data.lastLoginAt = data.updatedAt;

        if (data.selectedCharacterId == null)
            data.selectedCharacterId = "";

        return data;
    }
}
