using UnityEngine;

// 계정 데이터 보관소. PlayerPrefs에 아이디별로 비밀번호를 저장한다.
public class PlayerPrefsAccountRepository
{
    // 다른 저장 값과 이름표가 겹치지 않게 아이디 앞에 붙인다.
    private const string KeyPrefix = "Account_";

    private string GetKeyID(string id)
    {
        return KeyPrefix + id;
    }

    // 회원가입
    public void Register(string id, string password)
    {
        PlayerPrefs.SetString(GetKeyID(id), password);
        PlayerPrefs.Save(); // 게임이 갑자기 꺼져도 남도록 바로 쓴다.
    }

    // 아이디 중복 검사
    public bool Exists(string id)
    {
        return PlayerPrefs.HasKey(GetKeyID(id));
    }

    // 비밀번호 검증
    public bool CheckPassword(string id, string password)
    {
        return Exists(id) && PlayerPrefs.GetString(GetKeyID(id)) == password;
    }
}
