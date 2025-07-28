using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AuthUI : MonoBehaviour
{
    [Header("Login")]
    [SerializeField] private TMP_InputField _loginInputField;
    [SerializeField] private TMP_InputField _passwordInputField;
    [SerializeField] private Button _loginButton;

    [Header("Register")]
    [SerializeField] private TMP_InputField _emailInputField;
    [SerializeField] private TMP_InputField _registerLoginInputField;
    [SerializeField] private TMP_InputField _registerPasswordInputField;
    [SerializeField] private Button _registerButton;

    [Header("Panels")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _registerPanel;

    private bool _isHandlerRegistered;

    private void Start()
    {
        _loginButton.onClick.AddListener(OnLoginButton);
        _registerButton.onClick.AddListener(OnRegisterButton);
        RegisterAuthResponseHandler();
    }

    private void OnDestroy()
    {
        UnregisterAuthResponseHandler();
    }

    private void RegisterAuthResponseHandler()
    {
        if (_isHandlerRegistered == false)
        {
            NetworkClient.ReplaceHandler<AuthResponseMessage>(OnAuthResponse, false);
            _isHandlerRegistered = true;
            Debug.Log("AuthUI: Registered handler for AuthResponseMessage");
        }
    }

    private void UnregisterAuthResponseHandler()
    {
        if (_isHandlerRegistered)
        {
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
            _isHandlerRegistered = false;
            Debug.Log("AuthUI: Unregistered handler for AuthResponseMessage");
        }
    }

    private void OnLoginButton()
    {
        if (NetworkClient.isConnected == false)
        {
            Debug.LogWarning("AuthUI: No connection to server for login request");
            return;
        }

        if (string.IsNullOrEmpty(_loginInputField.text) || string.IsNullOrEmpty(_passwordInputField.text))
        {
            Debug.LogWarning("AuthUI: Login or password field is empty");
            return;
        }

        Debug.Log($"AuthUI: Sending login request for {_loginInputField.text}");
        AuthRequestMessage message = new AuthRequestMessage
        {
            isRegister = false,
            login = _loginInputField.text,
            password = _passwordInputField.text
        };
        NetworkClient.Send(message);
    }

    private void OnRegisterButton()
    {
        if (NetworkClient.isConnected == false)
        {
            Debug.LogWarning("AuthUI: No connection to server for register request");
            return;
        }

        if (string.IsNullOrEmpty(_emailInputField.text) || string.IsNullOrEmpty(_registerLoginInputField.text) || string.IsNullOrEmpty(_registerPasswordInputField.text))
        {
            Debug.LogWarning("AuthUI: Email, login, or password field is empty");
            return;
        }

        _registerButton.interactable = false;
        Debug.Log($"AuthUI: Sending register request for {_registerLoginInputField.text}, email={_emailInputField.text}");
        AuthRequestMessage message = new AuthRequestMessage
        {
            isRegister = true,
            email = _emailInputField.text,
            login = _registerLoginInputField.text,
            password = _registerPasswordInputField.text
        };
        NetworkClient.Send(message);
    }

    private void OnAuthResponse(AuthResponseMessage message)
    {
        Debug.Log($"AuthUI: Received AuthResponseMessage (isRegister={message.isRegister}, success={message.success}, message={message.message})");
        _loginButton.interactable = true;
        _registerButton.interactable = true;

        if (message.success && !message.isRegister)
        {
            Debug.Log("AuthUI: Login successful, hiding panels");
            _loginPanel.SetActive(false);
            _registerPanel.SetActive(false);
        }
    }

    public void ShowLoginPanel()
    {
        _loginInputField.text = "";
        _passwordInputField.text = "";
        _loginPanel.SetActive(true);
        _registerPanel.SetActive(false);
        Debug.Log("AuthUI: Showing login panel");
    }

    public void ShowRegisterPanel()
    {
        _emailInputField.text = "";
        _registerLoginInputField.text = "";
        _registerPasswordInputField.text = "";
        _loginPanel.SetActive(false);
        _registerPanel.SetActive(true);
        Debug.Log("AuthUI: Showing register panel");
    }
}