using Messenger.Shared;
using Messenger.WinForms.Presentation.ViewModels;

namespace Messenger.WinForms.Presentation.Forms;

public partial class UserSearchForm : Form
{
    private readonly UserSearchViewModel _viewModel;

    public UserSearchForm(UserSearchViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        SetupBindings();
        SetupFormProperties();
    }

    private void InitializeComponent()
    {
        this.Text = "Messenger - Поиск пользователей";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(240, 240, 240);

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(0, 120, 215)
        };

        var lblTitle = new Label
        {
            Text = "Поиск пользователей",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 20),
            AutoSize = true
        };

        var btnBack = new Button
        {
            Name = "btnBack",
            Text = "← Назад",
            Size = new Size(80, 30),
            Location = new Point(500, 20),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        headerPanel.Controls.AddRange([lblTitle, btnBack]);

        var searchPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        var txtSearch = new TextBox
        {
            Name = "txtSearch",
            Dock = DockStyle.Left,
            Width = 400,
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Введите имя пользователя..."
        };

        var btnSearch = new Button
        {
            Name = "btnSearch",
            Text = "Найти",
            Size = new Size(100, 35),
            Location = new Point(420, 12),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        searchPanel.Controls.AddRange([txtSearch, btnSearch]);

        var listView = new ListView
        {
            Name = "listView",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            MultiSelect = false
        };

        listView.Columns.Add("Имя пользователя", 200);
        listView.Columns.Add("Email", 250);
        listView.Columns.Add("Статус", 100);
        listView.Columns.Add("Дата регистрации", 150);
        
        // Добавляем тестовый элемент для проверки отображения
        var testItem = new ListViewItem("Тестовый пользователь");
        testItem.SubItems.Add("test@example.com");
        testItem.SubItems.Add("[OFF] Оффлайн");
        testItem.SubItems.Add("01.01.2024");
        listView.Items.Add(testItem);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 10)
        };

        var btnCreateChat = new Button
        {
            Name = "btnCreateChat",
            Text = "Создать чат",
            Size = new Size(120, 40),
            Location = new Point(20, 10),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };

        buttonPanel.Controls.Add(btnCreateChat);

        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            BackColor = Color.White,
            Padding = new Padding(10, 5, 10, 5)
        };

        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "Готово",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(64, 64, 64),
            AutoSize = true
        };

        var progressBar = new ProgressBar
        {
            Name = "progressBar",
            Size = new Size(200, 20),
            Location = new Point(400, 5),
            Visible = false
        };

        statusPanel.Controls.AddRange([lblStatus, progressBar]);

        this.Controls.Add(listView);
        this.Controls.Add(buttonPanel);
        this.Controls.Add(statusPanel);
        this.Controls.Add(searchPanel);
        this.Controls.Add(headerPanel);

        this.listView = listView;
        this.txtSearch = txtSearch;
        this.btnSearch = btnSearch;
        this.btnBack = btnBack;
        this.btnCreateChat = btnCreateChat;
        this.lblStatus = lblStatus;
        this.progressBar = progressBar;
    }

    private void SetupBindings()
    {
        btnSearch.Click += (s, e) => _viewModel.SearchCommand.Execute(null);
        btnBack.Click += (s, e) => _viewModel.BackCommand.Execute(null);
        btnCreateChat.Click += (s, e) => _viewModel.CreateChatCommand.Execute(null);

        txtSearch.DataBindings.Add("Text", _viewModel, nameof(_viewModel.SearchQuery), true, DataSourceUpdateMode.OnPropertyChanged);

        _viewModel.PropertyChanged += (s, e) =>
        {
            Console.WriteLine($"PropertyChanged event fired: {e.PropertyName}");
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsLoading):
                    Console.WriteLine("Updating loading state");
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.StatusMessage):
                    Console.WriteLine("Updating status");
                    UpdateStatus();
                    break;
                case nameof(_viewModel.Users):
                    Console.WriteLine("Updating users list");
                    UpdateUsersList();
                    break;
            }
        };

        UpdateLoadingState();
        UpdateStatus();
    }

    private void SetupFormProperties()
    {
        txtSearch.KeyPress += (s, e) => { if (e.KeyChar == (char)13) _viewModel.SearchCommand.Execute(null); };

        listView.SelectedIndexChanged += (s, e) =>
        {
            if (listView.SelectedItems.Count > 0)
            {
                var item = listView.SelectedItems[0];
                _viewModel.SelectedUser = item.Tag as UserDto;
                btnCreateChat.Enabled = _viewModel.SelectedUser != null;
            }
            else
            {
                _viewModel.SelectedUser = null;
                btnCreateChat.Enabled = false;
            }
        };
    }

    private void UpdateLoadingState()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateLoadingState));
            return;
        }

        btnSearch.Enabled = !_viewModel.IsLoading;
        txtSearch.Enabled = !_viewModel.IsLoading;
        progressBar.Visible = _viewModel.IsLoading;
    }

    private void UpdateStatus()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateStatus));
            return;
        }

        lblStatus.Text = _viewModel.StatusMessage;
    }

    private void UpdateUsersList()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateUsersList));
            return;
        }

        Console.WriteLine($"UpdateUsersList called. Current ListView items: {listView.Items.Count}");
        listView.Items.Clear();
        Console.WriteLine($"ListView cleared. Items count: {listView.Items.Count}");
        
        var userCount = _viewModel.Users?.Count ?? 0;
        Console.WriteLine($"UpdateUsersList called. Found {userCount} users.");
        
        if (_viewModel.Users != null)
        {
            foreach (var user in _viewModel.Users)
        {
                Console.WriteLine($"Adding user to list: {user.Username} ({user.Email})");
                
                var item = new ListViewItem(user.Username ?? "Unknown");
                item.SubItems.Add(user.Email ?? "No email");
            item.SubItems.Add(user.IsOnline ? "[ON] Онлайн" : "[OFF] Оффлайн");
            item.SubItems.Add(user.CreatedAt.ToString("dd.MM.yyyy"));
            item.Tag = user;
            listView.Items.Add(item);
        }
        }
        
        if (userCount > 0)
        {
            lblStatus.Text = $"Найдено {userCount} пользователей";
        }
        else
        {
            lblStatus.Text = "Пользователи не найдены";
        }
        
        Console.WriteLine($"ListView now contains {listView.Items.Count} items");
        Console.WriteLine($"ListView visible: {listView.Visible}, Enabled: {listView.Enabled}");
        Console.WriteLine($"ListView bounds: {listView.Bounds}");
        
        // Принудительно обновляем ListView
        listView.Refresh();
    }

    private ListView listView = null!;
    private TextBox txtSearch = null!;
    private Button btnSearch = null!;
    private Button btnBack = null!;
    private Button btnCreateChat = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
} 