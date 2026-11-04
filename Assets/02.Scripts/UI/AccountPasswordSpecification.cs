using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호: 공백과 한글을 뺀 출력 가능한 아스키로 쓰고,
    // 대문자, 소문자, 숫자, 기호를 각각 하나 이상 넣은 8~30자
    // 예) Zombie01!, Fps_Game#2
    private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[\x21-\x7E]{8,30}\z");

    public string ErrorMessage => "비밀번호는 대문자, 소문자, 숫자, 기호를 각각 하나 이상 넣은 8~30자여야 합니다.";

    public bool IsSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}