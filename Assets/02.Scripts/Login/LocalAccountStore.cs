using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

// 실습용 로컬 계정 저장소. 실제 서비스의 서버 인증과는 별개다.
public class LocalAccountStore
{
    [Serializable]
    private class Account
    {
        public string id;
        public string salt;
        public string passwordHash;
    }

    [Serializable]
    private class AccountFile
    {
        public List<Account> accounts = new List<Account>();
    }

    private readonly string _path;

    public LocalAccountStore(string path)
    {
        _path = path;
    }

    public bool Register(string id, string password, string confirmation, out string message)
    {
        id = id.Trim();
        if (id.Length < 3 || id.Length > 20)
        {
            message = "아이디는 3~20자로 입력해주세요.";
            return false;
        }
        if (password.Length < 6 || password.Length > 64)
        {
            message = "비밀번호는 6~64자로 입력해주세요.";
            return false;
        }
        if (password != confirmation)
        {
            message = "비밀번호 확인이 일치하지 않습니다.";
            return false;
        }

        AccountFile file = Read();
        if (file.accounts.Exists(account => account.id == id))
        {
            message = "이미 사용 중인 아이디입니다.";
            return false;
        }

        byte[] salt = new byte[16];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(salt);
        }
        file.accounts.Add(new Account
        {
            id = id,
            salt = Convert.ToBase64String(salt),
            passwordHash = Hash(password, salt)
        });
        Directory.CreateDirectory(Path.GetDirectoryName(_path));
        File.WriteAllText(_path, JsonUtility.ToJson(file));
        message = "가입이 완료되었습니다. 로그인해주세요.";
        return true;
    }

    public bool Login(string id, string password, out string message)
    {
        Account account = Read().accounts.Find(item => item.id == id.Trim());
        if (account == null || account.passwordHash != Hash(password, Convert.FromBase64String(account.salt)))
        {
            message = "아이디 또는 비밀번호를 확인해주세요.";
            return false;
        }
        message = "로그인 성공!";
        return true;
    }

    private AccountFile Read()
    {
        if (!File.Exists(_path)) return new AccountFile();
        AccountFile file = JsonUtility.FromJson<AccountFile>(File.ReadAllText(_path));
        if (file == null || file.accounts == null) throw new InvalidDataException();
        return file;
    }

    private static string Hash(string password, byte[] salt)
    {
        using (var key = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
        {
            return Convert.ToBase64String(key.GetBytes(32));
        }
    }
}
