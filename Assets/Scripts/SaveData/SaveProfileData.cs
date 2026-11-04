using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserProfileData
{
    // 유저가 지은 프로필 이름 (게임 기본은 Darkest)
    public string ProfileName;

    // 데이터를 식별하기 위한 아이디
    public string ProfileID;

    // 마지막 데이터 저장 시간
    public string LastSaveTime;


    // TODO : 맵 진행도, 노드 진행도, 역마차 상태, 영웅 데이터 변수 등 추가해야한다.


    // TODO : 관련 클래스에서 데이터를 저장하는 함수를 만들어야한다.
    [System.Serializable]
    public class NodeSaveData
    {

    }

    [System.Serializable]
    public class HeroSaveData
    {

    }



}
