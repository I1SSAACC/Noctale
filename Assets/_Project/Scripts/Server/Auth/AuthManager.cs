using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private string _accountsFilePath;
    private string _playerDataDirectory;
    private readonly Dictionary<string, NetworkConnectionToClient> loggedInAccounts = new();
    private readonly Dictionary<NetworkConnectionToClient, string> connectionToAccount = new();
    private readonly object fileLock = new object();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (transform.parent != null)
            {
                Debug.LogWarning($"AuthManager: Moving to root from parent {transform.parent.name} to apply DontDestroyOnLoad");
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
            Debug.Log($"AuthManager: Applied DontDestroyOnLoad to root GameObject in scene {SceneManager.GetActiveScene().name}");
            InitializePaths();
        }
        else
        {
            Debug.LogWarning($"AuthManager: Another instance already exists in scene {SceneManager.GetActiveScene().name}, destroying this one");
            Destroy(gameObject);
        }
    }

    private void InitializePaths()
    {
        string basePath = Application.dataPath;

        if (string.IsNullOrEmpty(basePath))
            basePath = Directory.GetCurrentDirectory();

        _accountsFilePath = Path.Combine(basePath, Constants.AccountsFileName);
        _playerDataDirectory = Path.Combine(basePath, Constants.PlayerDataDirectoryName);

        lock (fileLock)
        {
            if (Directory.Exists(_playerDataDirectory) == false)
            {
                Directory.CreateDirectory(_playerDataDirectory);
                Debug.Log($"AuthManager: Created directory {_playerDataDirectory}");
            }

            if (File.Exists(_accountsFilePath) == false)
            {
                AccountsDatabase emptyDatabase = new();
                string json = JsonUtility.ToJson(emptyDatabase, true);
                File.WriteAllText(_accountsFilePath, json);
                Debug.Log($"AuthManager: Created file {_accountsFilePath}");
            }
        }
    }

    public bool Register(string email, string login, string password, out string message)
    {
        if (Instance == null)
        {
            message = "AuthManager не инициализирован.";
            Debug.LogError(message);
            return false;
        }

        if (!NetworkServer.active)
        {
            message = "Регистрация возможна только на сервере.";
            Debug.LogWarning(message);
            return false;
        }

        try
        {
            AccountsDatabase database = LoadAccountsDatabase();

            if (database.accounts.Exists(a => a.Login == login))
            {
                message = "Логин уже занят.";
                return false;
            }

            if (database.accounts.Exists(a => a.Email == email))
            {
                message = "Email уже используется.";
                return false;
            }

            string id = Guid.NewGuid().ToString();
            byte[] saltBytes = new byte[16];
            RandomNumberGenerator.Create().GetBytes(saltBytes);
            string salt = Convert.ToBase64String(saltBytes);
            string passwordHash = Utils.ComputeSHA512Hash(password + salt);

            AccountInfo newAccount = new()
            {
                ID = id,
                Email = email,
                Login = login,
                PasswordHash = passwordHash,
                Salt = salt
            };

            database.accounts.Add(newAccount);
            SaveAccountsDatabase(database);

            PlayerData newPlayerData = new()
            {
                ID = id,
                Nickname = login
            };

            SavePlayerData(newPlayerData);

            message = "Аккаунт успешно создан.";
            Debug.Log($"AuthManager: Успешная регистрация для логина {login}");
            return true;
        }
        catch (Exception exception)
        {
            message = $"Ошибка при регистрации: {exception.Message}";
            Debug.LogError(message);
            return false;
        }
    }

    public bool Login(string login, string password, out PlayerData playerData, out string message)
    {
        playerData = null;

        if (Instance == null)
        {
            message = "AuthManager не инициализирован.";
            Debug.LogError(message);
            return false;
        }

        if (!NetworkServer.active)
        {
            message = "Авторизация возможна только на сервере.";
            Debug.LogWarning(message);
            return false;
        }

        try
        {
            AccountsDatabase database = LoadAccountsDatabase();
            AccountInfo account = database.accounts.Find(a => a.Login == login);

            if (account == null)
            {
                message = "Аккаунт не найден.";
                return false;
            }

            string passwordHash = Utils.ComputeSHA512Hash(password + account.Salt);

            if (account.PasswordHash != passwordHash)
            {
                message = "Неверный пароль.";
                return false;
            }

            playerData = LoadPlayerData(account.ID);

            if (playerData == null)
            {
                message = "Не удалось загрузить данные игрока.";
                return false;
            }

            message = "Авторизация успешна.";
            Debug.Log($"AuthManager: Успешная авторизация для логина {login}");
            return true;
        }
        catch (Exception exception)
        {
            message = $"Ошибка при авторизации: {exception.Message}";
            Debug.LogError(message);
            return false;
        }
    }

    private AccountsDatabase LoadAccountsDatabase()
    {
        lock (fileLock)
        {
            try
            {
                string json = File.ReadAllText(_accountsFilePath);
                return JsonUtility.FromJson<AccountsDatabase>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при загрузке базы данных аккаунтов: {ex.Message}");
                return new();
            }
        }
    }

    private void SaveAccountsDatabase(AccountsDatabase database)
    {
        lock (fileLock)
        {
            try
            {
                string json = JsonUtility.ToJson(database, true);
                File.WriteAllText(_accountsFilePath, json);
                Debug.Log($"AuthManager: Сохранен файл {_accountsFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при сохранении базы данных аккаунтов: {ex.Message}");
            }
        }
    }

    private PlayerData LoadPlayerData(string accountId)
    {
        string filePath = Path.Combine(_playerDataDirectory, accountId + Constants.JsonExtension);

        lock (fileLock)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonUtility.FromJson<PlayerData>(json);
                }
                else
                {
                    Debug.LogWarning($"Файл данных игрока не найден: {filePath}");
                    return null;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Ошибка при загрузке данных игрока: {exception.Message}");
                return null;
            }
        }
    }

    private void SavePlayerData(PlayerData playerData)
    {
        string filePath = Path.Combine(_playerDataDirectory, playerData.ID + Constants.JsonExtension);

        lock (fileLock)
        {
            try
            {
                string json = JsonUtility.ToJson(playerData, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"AuthManager: Сохранен файл данных игрока {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при сохранении данных игрока: {ex.Message}");
            }
        }
    }

    public bool IsAccountLoggedIn(string login)
    {
        if (Instance == null)
        {
            Debug.LogError("AuthManager: Instance is null in IsAccountLoggedIn");
            return false;
        }

        bool isLoggedIn = loggedInAccounts.ContainsKey(login);
        Debug.Log($"AuthManager: Checking if account {login} is logged in: {isLoggedIn}");
        return isLoggedIn;
    }

    public NetworkConnectionToClient GetConnectionByLogin(string login)
    {
        if (loggedInAccounts.TryGetValue(login, out var conn))
        {
            Debug.Log($"AuthManager: Found connection {conn.connectionId} for login {login}");
            return conn;
        }

        Debug.Log($"AuthManager: No connection found for login {login}");
        return null;
    }

    public void AssociateConnectionWithAccount(NetworkConnectionToClient conn, string login)
    {
        if (Instance == null)
        {
            Debug.LogError("AuthManager: Instance is null in AssociateConnectionWithAccount");
            return;
        }

        if (loggedInAccounts.ContainsKey(login))
        {
            Debug.LogWarning($"AuthManager: Логин {login} уже связан с другим соединением");
            return;
        }

        loggedInAccounts[login] = conn;
        connectionToAccount[conn] = login;
        Debug.Log($"AuthManager: Соединение {conn.connectionId} связано с логином {login}");
    }

    public void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (Instance == null)
        {
            Debug.LogError("AuthManager: Instance is null in OnServerDisconnect");
            return;
        }

        if (connectionToAccount.TryGetValue(conn, out string login))
        {
            loggedInAccounts.Remove(login);
            connectionToAccount.Remove(conn);
            Debug.Log($"AuthManager: Клиент с логином {login} отключился, удален из активных аккаунтов.");
        }
    }
}