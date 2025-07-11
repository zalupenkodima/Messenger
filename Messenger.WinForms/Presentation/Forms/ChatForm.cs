using Messenger.WinForms.Presentation.ViewModels;
using Messenger.Shared;

namespace Messenger.WinForms.Presentation.Forms;

public partial class ChatForm : Form
{
    private ChatViewModel _viewModel;

    public ChatForm()
    {
        InitializeComponent();
        SetupFormProperties();
    }

    public void SetViewModel(ChatViewModel viewModel)
    {
        _viewModel = viewModel;
        SetupBindings();
    }

    private void InitializeComponent()
    {
        this.Text = "Messenger - Чат";
        this.Size = new Size(900, 700);
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
            Text = "Название чата",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 20),
            AutoSize = true
        };

        var lblDescription = new Label
        {
            Text = "Нет описания",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.White,
            Location = new Point(20, 45),
            AutoSize = true
        };

        var lblConnectionStatus = new Label
        {
            Name = "lblConnectionStatus",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.White,
            Location = new Point(700, 20),
            AutoSize = true
        };

        var btnBack = new Button
        {
            Name = "btnBack",
            Text = "← Назад",
            Size = new Size(80, 30),
            Location = new Point(800, 20),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        headerPanel.Controls.AddRange([lblTitle, lblDescription, lblConnectionStatus, btnBack]);

        var messagesPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };

        var listView = new ListView
        {
            Name = "listView",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        listView.Columns.Add("Время", 80);
        listView.Columns.Add("Пользователь", 120);
        listView.Columns.Add("Сообщение", 600);

        var contextMenu = new ContextMenuStrip();
        var editMenuItem = new ToolStripMenuItem("Редактировать");
        var deleteMenuItem = new ToolStripMenuItem("Удалить");
        
        contextMenu.Items.AddRange([editMenuItem, deleteMenuItem]);
        listView.ContextMenuStrip = contextMenu;

        messagesPanel.Controls.Add(listView);

        var inputPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            BackColor = Color.White,
            Padding = new Padding(10)
        };

        var txtMessage = new TextBox
        {
            Name = "txtMessage",
            Dock = DockStyle.Left,
            Width = 700,
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = true,
            Height = 60,
            PlaceholderText = "Введите сообщение..."
        };

        var btnSend = new Button
        {
            Name = "btnSend",
            Text = "Отправить",
            Size = new Size(100, 60),
            Location = new Point(720, 10),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        var lblTyping = new Label
        {
            Name = "lblTyping",
            Text = "",
            Font = new Font("Segoe UI", 9, FontStyle.Italic),
            ForeColor = Color.FromArgb(108, 117, 125),
            Location = new Point(10, 70),
            AutoSize = true
        };

        inputPanel.Controls.AddRange([txtMessage, btnSend, lblTyping]);

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
            Location = new Point(700, 5),
            Visible = false
        };

        statusPanel.Controls.AddRange([lblStatus, progressBar]);

        this.Controls.Add(messagesPanel);
        this.Controls.Add(inputPanel);
        this.Controls.Add(statusPanel);
        this.Controls.Add(headerPanel);

        this.listView = listView;
        this.txtMessage = txtMessage;
        this.btnSend = btnSend;
        this.btnBack = btnBack;
        this.lblTitle = lblTitle;
        this.lblDescription = lblDescription;
        this.lblConnectionStatus = lblConnectionStatus;
        this.lblStatus = lblStatus;
        this.progressBar = progressBar;
        this.lblTyping = lblTyping;
        this.contextMenu = contextMenu;
        this.editMenuItem = editMenuItem;
        this.deleteMenuItem = deleteMenuItem;
    }

    private void SetupBindings()
    {
        if (_viewModel == null)
            throw new InvalidOperationException("ViewModel is not set. Call SetViewModel first.");

        btnSend.Click += (s, e) => _viewModel.SendMessageCommand.Execute(null);
        btnBack.Click += (s, e) => _viewModel.BackCommand.Execute(null);

        editMenuItem.Click += (s, e) => 
        {
            if (listView.SelectedItems.Count > 0 && listView.SelectedItems[0].Tag is MessageDto message)
                _viewModel.EditMessageCommand.Execute(message);
        };

        deleteMenuItem.Click += (s, e) => 
        {
            if (listView.SelectedItems.Count > 0 && listView.SelectedItems[0].Tag is MessageDto message)
                _viewModel.DeleteMessageCommand.Execute(message);
        };

        contextMenu.Opening += (s, e) =>
        {
            Console.WriteLine($"[ContextMenu] Opening - SelectedItems count: {listView.SelectedItems.Count}");
            if (listView.SelectedItems.Count > 0 && listView.SelectedItems[0].Tag is MessageDto message)
            {
                Console.WriteLine($"[ContextMenu] Message found: {message.Id} from {message.SenderId}");
                
                var canEdit = _viewModel.EditMessageCommand.CanExecute(message);
                var canDelete = _viewModel.DeleteMessageCommand.CanExecute(message);
                editMenuItem.Enabled = canEdit;
                deleteMenuItem.Enabled = canDelete;
                Console.WriteLine($"[ContextMenu] Edit enabled: {canEdit}, Delete enabled: {canDelete}");
            }
            else
            {
                Console.WriteLine("[ContextMenu] No message selected or Tag is not MessageDto");
                editMenuItem.Enabled = false;
                deleteMenuItem.Enabled = false;
            }
        };

        listView.DoubleClick += (s, e) =>
        {
            if (listView.SelectedItems.Count > 0 && listView.SelectedItems[0].Tag is MessageDto message)
                _viewModel.EditMessageCommand.Execute(message);
        };

        listView.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                Console.WriteLine("[MouseClick] Right click detected");
                var item = listView.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    listView.SelectedItems.Clear();
                    item.Selected = true;
                    Console.WriteLine($"[MouseClick] Item selected: {item.Tag}");
                    contextMenu.Show(listView, e.Location);
                }
            }
        };

        txtMessage.DataBindings.Add("Text", _viewModel, nameof(_viewModel.NewMessageText), true, DataSourceUpdateMode.OnPropertyChanged);

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
                case nameof(_viewModel.IsConnected):
                    UpdateConnectionStatus();
                    break;
                case nameof(_viewModel.Messages):
                    UpdateMessagesList();
                    break;
                case nameof(_viewModel.TypingIndicator):
                    UpdateTypingIndicator();
                    break;
                case nameof(_viewModel.Chat):
                    UpdateChatInfo();
                    break;
            }
        };

        _viewModel.Messages.CollectionChanged += (s, e) => UpdateMessagesList();

        UpdateLoadingState();
        UpdateStatus();
        UpdateConnectionStatus();
        UpdateTypingIndicator();
        UpdateMessagesList();
        UpdateChatInfo();
    }

    private void SetupFormProperties()
    {
        txtMessage.KeyPress += (s, e) => 
        { 
            if (e.KeyChar == (char)13 && !ModifierKeys.HasFlag(Keys.Shift))
            {
                e.Handled = true;
                _viewModel.SendMessageCommand.Execute(null);
            }
        };

        this.FormClosing += (s, e) =>
        {
            _viewModel.Dispose();
        };
    }

    private void UpdateLoadingState()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateLoadingState));
            return;
        }

        btnSend.Enabled = !_viewModel.IsLoading;
        txtMessage.Enabled = !_viewModel.IsLoading;
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

    private void UpdateMessagesList()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateMessagesList));
            return;
        }

        Console.WriteLine($"UpdateMessagesList called. Messages count: {_viewModel.Messages.Count}");
        
        listView.Items.Clear();
        foreach (var message in _viewModel.Messages)
        {
            var timeText = message.CreatedAt.ToString("HH:mm");
            if (message.EditedAt.HasValue && message.EditedAt.Value != message.CreatedAt)
                timeText += " (ред.)";

            var item = new ListViewItem(timeText);
            item.SubItems.Add(message.SenderUsername);
            item.SubItems.Add(message.Content);
            item.Tag = message;
            listView.Items.Add(item);
            Console.WriteLine($"Added message to list: {message.SenderUsername} (ID: {message.Id}, SenderId: {message.SenderId}) - {message.Content}");
        }

        if (listView.Items.Count > 0)
            listView.Items[^1].EnsureVisible();
    }

    private void UpdateTypingIndicator()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateTypingIndicator));
            return;
        }

        lblTyping.Text = _viewModel.TypingIndicator;
        lblTyping.Visible = !string.IsNullOrEmpty(_viewModel.TypingIndicator);
    }

    private void UpdateChatInfo()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateChatInfo));
            return;
        }

        if (_viewModel.Chat != null)
        {
            lblTitle.Text = _viewModel.Chat.Name;
            lblDescription.Text = _viewModel.Chat.Description ?? "";
            
            if (_viewModel.Chat.Members.Count!=0)
            {
                var onlineMembers = _viewModel.Chat.Members.Where(m => m.IsOnline).ToList();
                if (onlineMembers.Count!=0)
                {
                    var onlineText = string.Join(", ", onlineMembers.Select(m => m.Username));
                    lblDescription.Text += $" | [ON] {onlineText}";
                }
                else
                {
                    lblDescription.Text += " | [OFF] Все оффлайн";
                }
            }
        }
    }

    private ListView listView = null!;
    private TextBox txtMessage = null!;
    private Button btnSend = null!;
    private Button btnBack = null!;
    private Label lblTitle = null!;
    private Label lblDescription = null!;
    private Label lblConnectionStatus = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
    private Label lblTyping = null!;
    private ContextMenuStrip contextMenu = null!;
    private ToolStripMenuItem editMenuItem = null!;
    private ToolStripMenuItem deleteMenuItem = null!;
} 