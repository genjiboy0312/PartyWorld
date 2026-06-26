using System;
using System.Collections.Generic;

/// <summary>
/// 방 하나의 메타데이터를 담는 데이터 클래스 (프로토타입)
/// </summary>
[Serializable]
public class RoomData
{
    public int roomNumber;
    public string roomName;
    public int playerCount;
    public int maxPlayers;
    public bool isOpen;
    public bool isVisible;
    public bool isVirtual;

    public RoomData() { }

    public RoomData(int roomNumber, string roomName)
    {
        this.roomNumber = roomNumber;
        this.roomName = roomName;
        this.playerCount = 0;
        this.maxPlayers = 8;
        this.isOpen = true;
        this.isVisible = true;
        this.isVirtual = false;
    }

    public RoomData(int roomNumber, string roomName, int playerCount, int maxPlayers, bool isOpen, bool isVisible, bool isVirtual)
    {
        this.roomNumber = roomNumber;
        this.roomName = roomName;
        this.playerCount = playerCount;
        this.maxPlayers = maxPlayers;
        this.isOpen = isOpen;
        this.isVisible = isVisible;
        this.isVirtual = isVirtual;
    }
}

/// <summary>
/// 방 목록 전체를 저장/불러오기 위한 래퍼 클래스
/// </summary>
[Serializable]
public class RoomDataList
{
    public List<RoomData> rooms = new List<RoomData>();
}
