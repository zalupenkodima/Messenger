using System.Windows.Input;
using Messenger.WinForms.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Presentation.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IMessengerService _messengerService;
    private readonly IUserSessionService _userSessionService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<LoginViewModel> _logger;
    private readonly ILocalAuthStorageService _localAuthStorageService;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _email = string.Empty;
    private bool _isRegistering;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _isError;

    public LoginViewModel(
        IMessengerService messengerService,
        IUserSessionService userSessionService,
        INavigationService navigationService,
        ILogger<LoginViewModel> logger,
        ILocalAuthStorageService localAuthStorageService)
    {
        _messengerService = messengerService;
        _userSessionService = userSessionService;
        _navigationService = navigationService;
        _logger = logger;
        _localAuthStorageService = localAuthStorageService;

        LoginCommand = new RelayCommand(async () => await LoginAsync(), () => CanLogin());
        RegisterCommand = new RelayCommand(async () => await RegisterAsync(), () => CanRegister());
        ToggleModeCommand = new RelayCommand(() => IsRegistering = !IsRegistering);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public bool IsRegistering
    {
        get => _isRegistering;
        set => SetProperty(ref _isRegistering, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand ToggleModeCommand { get; }

    private bool CanLogin()
    {
        return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    private bool CanRegister()
    {
        return !IsLoading && !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(Email);
    }

    private async Task LoginAsync()
    {
        if (!CanLogin()) return;

        IsLoading = true;
        StatusMessage = "Вход в систему...";
        IsError = false;

        try
        {
            var success = await _messengerService.AuthenticateAsync(Username, Password);
            if (success)
            {
                if (!string.IsNullOrEmpty(_messengerService.Token))
                {
                    _localAuthStorageService.SaveToken(_messengerService.Token);
                    _logger.LogInformation("Token saved for user: {Username}", Username);
                }
                else
                {
                    _logger.LogWarning("No token received after successful authentication for user: {Username}", Username);
                }

                var currentUser = await _messengerService.GetCurrentUserAsync();
                
                if (currentUser != null)
                {
                    _logger.LogInformation("Retrieved current user: {Username} with ID: {UserId}", currentUser.Username, currentUser.Id);
                    _logger.LogInformation("Current user ID type: {UserIdType}", currentUser.Id.GetType().Name);
                    _userSessionService.SetUserSession(currentUser, _messengerService.Token!);
                    await _messengerService.ConnectAsync();
                    _navigationService.ShowMainForm();
                }
                else
                {
                    _logger.LogWarning("Failed to get current user information");
                    StatusMessage = "Не удалось получить информацию о пользователе";
                    IsError = true;
                }
            }
            else
            {
                StatusMessage = "Неверное имя пользователя или пароль";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user: {Username}", Username);
            StatusMessage = "Ошибка при входе в систему";
            IsError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RegisterAsync()
    {
        if (!CanRegister()) return;

        IsLoading = true;
        StatusMessage = "Регистрация...";
        IsError = false;

        try
        {
            var success = await _messengerService.RegisterAsync(Username, Email, Password);
            if (success)
            {
                StatusMessage = "Регистрация успешна! Теперь вы можете войти в систему.";
                IsError = false;
                IsRegistering = false;
            }
            else
            {
                StatusMessage = "Ошибка при регистрации. Возможно, пользователь уже существует.";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user: {Username}", Username);
            StatusMessage = "Ошибка при регистрации";
            IsError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void LogoutAndClearToken()
    {
        _localAuthStorageService.ClearToken();
    }
}

public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<T?, bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);
} 