using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Messenger.Client;
using Messenger.WinForms.Core.Interfaces;
using Messenger.WinForms.Infrastructure.Services;
using Messenger.WinForms.Presentation.ViewModels;
using Messenger.Client.Interfaces;
using Messenger.WinForms.Presentation.Forms;

namespace Messenger.WinForms;

static class Program
{
    [STAThread]
    static async Task Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder().Build();
        
        try
        {
            var navigationService = host.Services.GetRequiredService<INavigationService>();
            var localAuthStorageService = host.Services.GetRequiredService<ILocalAuthStorageService>();
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
            
            var token = localAuthStorageService.LoadToken();
            if (!string.IsNullOrEmpty(token))
            {
                logger.LogInformation("Found saved token, attempting auto-login");
                
                var messengerService = host.Services.GetRequiredService<IMessengerService>();
                var userSessionService = host.Services.GetRequiredService<IUserSessionService>();
                
                messengerService.SetToken(token);
                
                try
                {
                    var connectTask = messengerService.ConnectAsync();
                    if (Task.WaitAny([connectTask], TimeSpan.FromSeconds(5)) == 0)
                    {
                        var currentUser = await messengerService.GetCurrentUserAsync();
                        if (currentUser != null)
                        {
                            userSessionService.SetUserSession(currentUser, token);
                            logger.LogInformation("Auto-login successful for user: {Username} with ID: {UserId}", currentUser.Username, currentUser.Id);
                            
                            navigationService.ShowMainForm();
                        }
                        else
                        {
                            logger.LogWarning("Failed to get current user during auto-login");
                            localAuthStorageService.ClearToken();
                            navigationService.ShowLoginForm();
                        }
                    }
                    else
                    {
                        logger.LogWarning("Auto-login timeout");
                        localAuthStorageService.ClearToken();
                        navigationService.ShowLoginForm();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Auto-login failed");
                    localAuthStorageService.ClearToken();
                    navigationService.ShowLoginForm();
                }
            }
            else
            {
                logger.LogInformation("No saved token found, showing login form");
                navigationService.ShowLoginForm();
            }
            
            Application.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка запуска приложения: {ex.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            host.Dispose();
        }
    }

    static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var baseUrl = context.Configuration["MessengerApi:BaseUrl"] ?? "https://localhost:5001";

                services.AddSingleton<IMessengerClient>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Client.Services.MessengerClient>>();
                    return MessengerClientFactory.CreateClient(baseUrl);
                });

                services.AddScoped<IMessengerService, MessengerService>();
                services.AddScoped<IUserSessionService, UserSessionService>();
                services.AddScoped<INavigationService, NavigationService>();
                services.AddSingleton<ILocalAuthStorageService, LocalAuthStorageService>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<UserSearchViewModel>();
                services.AddTransient<CreateChatViewModel>();
                services.AddTransient<ChatForm>();
                services.AddTransient<Func<Guid, ChatViewModel>>(provider => chatId =>
                    new ChatViewModel(
                        chatId,
                        provider.GetRequiredService<IMessengerService>(),
                        provider.GetRequiredService<IUserSessionService>(),
                        provider.GetRequiredService<INavigationService>(),
                        provider.GetRequiredService<ILogger<ChatViewModel>>()
                    ));

                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            });
} 