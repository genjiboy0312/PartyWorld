using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  아이템 정보 저장 Script
public enum ItemType
{
    None,
    Shoes,
    Clothe,
    Hat,
    Acc
}
[System.Serializable]
public class Item
{
    public string _itemName;
    public int _itemID;
    public ItemType _itemType;
}
