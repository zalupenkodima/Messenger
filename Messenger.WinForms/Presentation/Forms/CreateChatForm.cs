using Messenger.WinForms.Presentation.ViewModels;
using Messenger.Shared;

namespace Messenger.WinForms.Presentation.Forms;

public partial class CreateChatForm : Form
{
    private readonly CreateChatViewModel _viewModel;

    public CreateChatForm(CreateChatViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        SetupBindings();
        SetupFormProperties();
    }

    private void InitializeComponent()
    {
        this.Text = "Messenger - Создание чата";
        this.Size = new Size(500, 400);
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
            Text = "Создание нового чата",
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
            Location = new Point(400, 20),
            Font = new Font("Segoe UI", 9),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        headerPanel.Controls.AddRange(new Control[] { lblTitle, btnBack });

        var formPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(40, 20, 40, 40),
            AutoScroll = true
        };

        var lblName = new Label
        {
            Text = "Название чата:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 20),
            AutoSize = true
        };

        var txtName = new TextBox
        {
            Name = "txtName",
            Location = new Point(16, 45),
            Size = new Size(400, 30),
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Введите название чата..."
        };

        var lblDescription = new Label
        {
            Text = "Описание (необязательно):",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 85),
            AutoSize = true
        };

        var txtDescription = new TextBox
        {
            Name = "txtDescription",
            Location = new Point(16, 110),
            Size = new Size(400, 30),
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Введите описание чата..."
        };

        var lblType = new Label
        {
            Text = "Тип чата:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 150),
            AutoSize = true
        };

        var comboType = new ComboBox
        {
            Name = "comboType",
            Location = new Point(16, 175),
            Size = new Size(400, 30),
            Font = new Font("Segoe UI", 12),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        comboType.Items.AddRange(new object[]
        {
            new { Text = "Приватный чат", Value = ChatType.Private },
            new { Text = "Групповой чат", Value = ChatType.Group },
        });

        comboType.DisplayMember = "Text";
        comboType.ValueMember = "Value";
        comboType.SelectedIndex = 0;

        var btnCreate = new Button
        {
            Name = "btnCreate",
            Text = "Создать чат",
            Location = new Point(16, 225),
            Size = new Size(400, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        var lblStatus = new Label
        {
            Name = "lblStatus",
            Text = "",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 275),
            Size = new Size(400, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };

        var progressBar = new ProgressBar
        {
            Name = "progressBar",
            Location = new Point(16, 325),
            Size = new Size(400, 6),
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };

        formPanel.Controls.AddRange(
        [
            lblName, txtName,
            lblDescription, txtDescription,
            lblType, comboType,
            btnCreate, lblStatus, progressBar
        ]);

        this.Controls.Add(lblTitle);
        this.Controls.Add(formPanel);
        this.Controls.Add(headerPanel);

        this.txtName = txtName;
        this.txtDescription = txtDescription;
        this.comboType = comboType;
        this.btnCreate = btnCreate;
        this.btnBack = btnBack;
        this.lblStatus = lblStatus;
        this.progressBar = progressBar;
    }

    private void SetupBindings()
    {
        btnCreate.Click += (s, e) => _viewModel.CreateChatCommand.Execute(null);
        btnBack.Click += (s, e) => _viewModel.BackCommand.Execute(null);

        txtName.DataBindings.Add("Text", _viewModel, nameof(_viewModel.ChatName), true, DataSourceUpdateMode.OnPropertyChanged);
        txtDescription.DataBindings.Add("Text", _viewModel, nameof(_viewModel.ChatDescription), true, DataSourceUpdateMode.OnPropertyChanged);

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
                case nameof(_viewModel.SelectedChatType):
                    UpdateChatType();
                    break;
            }
        };

        UpdateLoadingState();
        UpdateStatus();
        UpdateChatType();
    }

    private void SetupFormProperties()
    {
        txtName.KeyPress += (s, e) => { if (e.KeyChar == (char)13) txtDescription.Focus(); };
        txtDescription.KeyPress += (s, e) => { if (e.KeyChar == (char)13) _viewModel.CreateChatCommand.Execute(null); };
    }

    private void UpdateLoadingState()
    {
        btnCreate.Enabled = !_viewModel.IsLoading;
        txtName.Enabled = !_viewModel.IsLoading;
        txtDescription.Enabled = !_viewModel.IsLoading;
        comboType.Enabled = !_viewModel.IsLoading;
        progressBar.Visible = _viewModel.IsLoading;
    }

    private void UpdateStatus()
    {
        lblStatus.Text = _viewModel.StatusMessage;
    }

    private void UpdateChatType()
    {
        for (int i = 0; i < comboType.Items.Count; i++)
        {
            var item = comboType.Items[i];
            var property = item.GetType().GetProperty("Value");
            if (property != null && property.GetValue(item) is ChatType chatType && chatType == _viewModel.SelectedChatType)
            {
                comboType.SelectedIndex = i;
                break;
            }
        }
    }

    private TextBox txtName = null!;
    private TextBox txtDescription = null!;
    private ComboBox comboType = null!;
    private Button btnCreate = null!;
    private Button btnBack = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
} 