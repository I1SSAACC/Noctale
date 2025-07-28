using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

public class ServerConsoleCommands : MonoBehaviour
{
    private const string MaintenanceOn = "maintenance on";
    private const string MaintenanceOff = "maintenance off";

    private readonly ConcurrentQueue<string> commandQueue = new();
    private CustomNetworkManager _networkManager;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _networkManager = FindFirstObjectByType<CustomNetworkManager>();
    }

    private void Start()
    {
        Thread consoleThread = new(ConsoleInputLoop)
        {
            IsBackground = true
        };

        consoleThread.Start();
    }

    private void ConsoleInputLoop()
    {
        while (true)
        {
            string command = System.Console.ReadLine();

            if (string.IsNullOrEmpty(command) == false)
                commandQueue.Enqueue(command);
        }
    }

    private void Update()
    {
        while (commandQueue.TryDequeue(out string command))
            HandleCommand(command);
    }

    private void HandleCommand(string command)
    {
        if (command.Equals(MaintenanceOn, System.StringComparison.OrdinalIgnoreCase))
            ExecuteMaintenanceCommand(true, MaintenanceOn);
        else if (command.Equals(MaintenanceOff, System.StringComparison.OrdinalIgnoreCase))
            ExecuteMaintenanceCommand(false, MaintenanceOff);
        else
            Debug.Log($"Неизвестная команда: {command}");
    }

    private void ExecuteMaintenanceCommand(bool enable, string successMessage)
    {
        if (_networkManager == null)
            return;

        //_networkManager.ToggleMaintenanceMode(enable);
        Debug.Log($"Команда {successMessage} принята");
    }
}