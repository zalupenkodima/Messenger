using Messenger.WinForms.Presentation.ViewModels;
using Messenger.Shared;

namespace Messenger.WinForms.Presentation.Forms;

public partial class MainForm : Form
{
    private readonly MainViewModel _viewModel;

    public MainForm(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        SetupBindings();
        SetupFormProperties();
    }

    private void InitializeComponent()
    {
        this.Text = "Messenger - Главная";
        this.Size = new Size(850, 600);
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
            Text = "Messenger",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 20),
            AutoSize = true
        };

        var lblUsername = new Label
        {
            Name = "lblUsername",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            Location = new Point(20, 50),
            AutoSize = true
        };

        var lblConnectionStatus = new Label
        {
            Name = "lblConnectionStatus",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.White,
            Location = new Point(600, 20),
            AutoSize = true
        };

        var btnLogout = new Button
        {
            Name = "btnLogout",
            Text = "Выйти",
            Size = new Size(80, 30),
            Location = new Point(700, 20),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        headerPanel.Controls.AddRange([lblTitle, lblUsername, lblConnectionStatus, btnLogout]);

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
            Width = 300,
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Поиск чатов..."
        };

        var btnRefresh = new Button
        {
            Name = "btnRefresh",
            Text = "Обновить",
            Size = new Size(100, 35),
            Location = new Point(320, 12),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        var btnCreateChat = new Button
        {
            Name = "btnCreateChat",
            Text = "Создать чат",
            Size = new Size(120, 35),
            Location = new Point(430, 12),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        var btnSearchUsers = new Button
        {
            Name = "btnSearchUsers",
            Text = "Найти пользователей",
            Size = new Size(150, 35),
            Location = new Point(560, 12),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat
        };

        searchPanel.Controls.AddRange([txtSearch, btnRefresh, btnCreateChat, btnSearchUsers]);

        var listView = new ListView
        {
            Name = "listView",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White
        };

        listView.Columns.Add("Название", 200);
        listView.Columns.Add("Описание", 250);
        listView.Columns.Add("Тип", 80);
        listView.Columns.Add("Участники", 80);
        listView.Columns.Add("Непрочитано", 100);
        listView.Columns.Add("Последнее сообщение", 150);
        listView.Columns.Add("Статус участников", 200);

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
            Location = new Point(600, 5),
            Visible = false
        };

        statusPanel.Controls.AddRange([lblStatus, progressBar]);

        this.Controls.Add(listView);
        this.Controls.Add(statusPanel);
        this.Controls.Add(searchPanel);
        this.Controls.Add(headerPanel);

        this.listView = listView;
        this.txtSearch = txtSearch;
        this.btnRefresh = btnRefresh;
        this.btnCreateChat = btnCreateChat;
        this.btnSearchUsers = btnSearchUsers;
        this.btnLogout = btnLogout;
        this.lblUsername = lblUsername;
        this.lblConnectionStatus = lblConnectionStatus;
        this.lblStatus = lblStatus;
        this.progressBar = progressBar;
    }

    private void SetupBindings()
    {
        btnRefresh.Click += (s, e) => _viewModel.RefreshCommand.Execute(null);
        btnCreateChat.Click += (s, e) => _viewModel.CreateChatCommand.Execute(null);
        btnSearchUsers.Click += (s, e) => _viewModel.SearchUsersCommand.Execute(null);
        btnLogout.Click += (s, e) => 
        {
            Console.WriteLine("Logout button clicked");
            _viewModel.LogoutCommand.Execute(null);
        };
        listView.DoubleClick += (s, e) => 
        {
            Console.WriteLine($"Double click detected. SelectedChat: {_viewModel.SelectedChat?.Name ?? "null"}");
            _viewModel.OpenChatCommand.Execute(null);
        };

        txtSearch.DataBindings.Add("Text", _viewModel, nameof(_viewModel.SearchQuery), true, DataSourceUpdateMode.OnPropertyChanged);

        _viewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.StatusMessage):
                    UpdateStatus();
                    break;
                case nameof(_viewModel.CurrentUsername):
                    UpdateUsername();
                    break;
                case nameof(_viewModel.IsConnected):
                    UpdateConnectionStatus();
                    break;
                case nameof(_viewModel.Chats):
                    UpdateChatsList();
                    break;
                case nameof(_viewModel.SelectedChat):
                    UpdateSelectedChat();
                    break;
            }
        };

        _viewModel.Chats.CollectionChanged += (s, e) => UpdateChatsList();

        UpdateLoadingState();
        UpdateStatus();
        UpdateUsername();
        UpdateConnectionStatus();
        UpdateChatsList();
    }

    private void SetupFormProperties()
    {
        txtSearch.KeyPress += (s, e) => { if (e.KeyChar == (char)13) _viewModel.RefreshCommand.Execute(null); };
        
        listView.SelectedIndexChanged += (s, e) =>
        {
            if (listView.SelectedItems.Count > 0)
            {
                var item = listView.SelectedItems[0];
                if (item.Tag is ChatDto chat)
                    _viewModel.SelectedChat = chat;
            }
            else
            {
                _viewModel.SelectedChat = null;
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

        btnRefresh.Enabled = !_viewModel.IsLoading;
        btnCreateChat.Enabled = !_viewModel.IsLoading;
        btnSearchUsers.Enabled = !_viewModel.IsLoading;
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

    private void UpdateUsername()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateUsername));
            return;
        }

        lblUsername.Text = $"Пользователь: {_viewModel.CurrentUsername}";
    }

    private void UpdateConnectionStatus()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateConnectionStatus));
            return;
        }

        lblConnectionStatus.Text = _viewModel.IsConnected ? "Подключено" : "Отключено";
        lblConnectionStatus.ForeColor = _viewModel.IsConnected ? Color.LightGreen : Color.LightCoral;
    }

    private void UpdateChatsList()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateChatsList));
            return;
        }

        listView.Items.Clear();
        foreach (var chat in _viewModel.Chats)
        {
            var item = new ListViewItem(chat.Name);
            item.SubItems.Add(chat.Description ?? "");
            item.SubItems.Add(chat.Type.ToString());
            item.SubItems.Add(chat.MemberCount.ToString());
            item.SubItems.Add(chat.UnreadCount.ToString());
            item.SubItems.Add(chat.LastMessageAt.ToString("dd.MM.yyyy HH:mm"));
            item.Tag = chat;
            
            if (chat.Members.Any())
            {
                var onlineMembers = chat.Members.Where(m => m.IsOnline).ToList();
                if (onlineMembers.Any())
                {
                    var onlineText = string.Join(", ", onlineMembers.Select(m => m.Username));
                    item.SubItems.Add($"[ON] {onlineText}");
                }
                else
                {
                    item.SubItems.Add("[OFF] Все оффлайн");
                }
            }
            
            listView.Items.Add(item);
        }
    }

    private void UpdateSelectedChat()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateSelectedChat));
            return;
        }

        if (_viewModel.SelectedChat != null)
        {
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Tag is ChatDto chat && chat.Id == _viewModel.SelectedChat.Id)
                {
                    item.Selected = true;
                    break;
                }
            }
        }
    }

    private ListView listView = null!;
    private TextBox txtSearch = null!;
    private Button btnRefresh = null!;
    private Button btnCreateChat = null!;
    private Button btnSearchUsers = null!;
    private Button btnLogout = null!;
    private Label lblUsername = null!;
    private Label lblConnectionStatus = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
} 