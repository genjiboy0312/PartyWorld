using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterScript : MonoBehaviour
{
    public Animator anim;
    public GameObject player;
    public GameObject Hit_Muzzle;
    public GameObject Monsters;
    public Transform Muzzle_Pos;

    public int Health;
    public int MaxHealth;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        Monsters = GameObject.FindGameObjectWithTag("Monster");
        MaxHealth = Health;                                     // 체력 설정, 재 생성 될때 초기화 시키기 위해서

        anim = GetComponent<Animator>();
        player.GetComponent<PlayerScript>().Monster = gameObject;

        //GetComponent<GameManager>();
        Regen();

    }


    public void GetDamage(int damage)
    {
        // 파티클 생성
        GameObject muz = Instantiate(Hit_Muzzle, Muzzle_Pos.position, Quaternion.identity);
        Destroy(muz, 1f);

        Health -= damage;
        if (Health <= 0)
        {
            DEAD_Anim();
            player.GetComponent<PlayerScript>().Monster = null;
            //Destroy(gameObject, 2f);

            GameManager.Stage++;
            Invoke("Monster_Hide", 2f);
            Invoke("Regen", 1f);
        }
    }

    // 몬스터 죽었을 때, 애니메이션
    void DEAD_Anim()
    {
        anim.SetInteger("doDie", 1);
    }

    // object Hide -> 오브젝트가 파괴 된 것 처럼
    public void Monster_Hide()
    {
        Health = MaxHealth;                   // 체력 설정 값으로 초기화
        gameObject.SetActive(false);            // 오브젝트 끔
    }

    // 다음 몬스터 생성 & 리젠 -> NextStageMonsterMake
    public void Regen()
    {
        for (int i = 1; i < Monsters.transform.childCount; i++)
        {
            if (GameManager.Stage == i)
            {
                Monsters.transform.GetChild(GameManager.Stage - 1).gameObject.SetActive(true);
                break;
            }
        }
    }
}