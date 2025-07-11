using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Messenger.WinForms.Core.Interfaces;
using Messenger.WinForms.Presentation.Forms;
using Messenger.WinForms.Presentation.ViewModels;

namespace Messenger.WinForms.Infrastructure.Services;

public class NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger) : INavigationService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<NavigationService> _logger = logger;
    private Form? _currentForm;

    public void ShowLoginForm()
    {
        try
        {
            var viewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            var form = new LoginForm(viewModel);
            
            ShowForm(form);
            _logger.LogInformation("Login form displayed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show login form");
        }
    }

    public void ShowMainForm()
    {
        try
        {
            var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var form = new MainForm(viewModel);
            
            ShowForm(form);
            _logger.LogInformation("Main form displayed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show main form");
        }
    }

    public void ShowChatForm(Guid chatId)
    {
        try
        {
            _logger.LogInformation("Attempting to show chat form for chat: {ChatId}", chatId);
            
            var viewModelFactory = _serviceProvider.GetRequiredService<Func<Guid, ChatViewModel>>();
            var viewModel = viewModelFactory(chatId);
            var form = _serviceProvider.GetRequiredService<ChatForm>();
            form.SetViewModel(viewModel);
            ShowForm(form);
            _logger.LogInformation("Chat form displayed for chat: {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show chat form for chat: {ChatId}", chatId);
            MessageBox.Show($"Ошибка при открытии чата: {ex.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ShowUserSearchForm()
    {
        try
        {
            var viewModel = _serviceProvider.GetRequiredService<UserSearchViewModel>();
            var form = new UserSearchForm(viewModel);
            
            ShowForm(form);
            _logger.LogInformation("User search form displayed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show user search form");
            MessageBox.Show($"Ошибка при открытии поиска пользователей: {ex.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ShowCreateChatForm()
    {
        try
        {
            var viewModel = _serviceProvider.GetRequiredService<CreateChatViewModel>();
            var form = new CreateChatForm(viewModel);
            
            ShowForm(form);
            _logger.LogInformation("Create chat form displayed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show create chat form");
            MessageBox.Show($"Ошибка при открытии создания чата: {ex.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void CloseApplication()
    {
        try
        {
            _logger.LogInformation("Application shutdown requested");
            Application.Exit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close application");
        }
    }

    private void ShowForm(Form form)
    {
        if (_currentForm != null)
        {
            _currentForm.Close();
            _currentForm.Dispose();
        }
        
        _currentForm = form;
        
        form.Show();
    }
} 