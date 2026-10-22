using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class DictionaryExample : MonoBehaviour
{
    // 지금까지 배웠던 배열이나 List 인덱스를 이용해 요소에 접근했다.
    // 하지만 딕셔너리(사전)은 요소를 저장할 때 "키"와 "값"을 쌍으로 저장한다.
    // HashTable(=Dictionary)은 키와 값이 쌍으로 이루어진 데이터를 CRUDS 할 때 사용하는 자료구조

    private void Start()
    {
        List<int> ages = new List<int>() { 22, 21, 20, 35 };

        Dictionary<string, int> students = new Dictionary<string, int>()
        {
            { "나현", 21 },
            { "철규", 21 },
            { "재원", 20 }
        };

        Debug.Log(students["나현"]);
        students["철규"] = 23;
        students.Add("재욱", 17);
        students.Remove("재원");
        bool has = students.ContainsKey("송재욱");
    }
}