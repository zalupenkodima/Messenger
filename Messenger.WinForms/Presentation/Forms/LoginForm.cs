using Messenger.WinForms.Presentation.ViewModels;

namespace Messenger.WinForms.Presentation.Forms;

public partial class LoginForm : Form
{
    private readonly LoginViewModel _viewModel;

    public LoginForm(LoginViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        SetupBindings();
        SetupFormProperties();
    }

    private void InitializeComponent()
    {
        this.Text = "Messenger - Вход";
        this.Size = new Size(400, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 240, 240);
        this.CenterToScreen();

        lblTitle = new Label
        {
            Text = "Messenger",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.FromArgb(64, 64, 64),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 60
        };

        panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(40, 20, 40, 40),
            AutoScroll = true
        };

        lblUsername = new Label
        {
            Text = "Имя пользователя:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 86),
            AutoSize = true
        };

        txtUsername = new TextBox
        {
            Location = new Point(16, 111),
            Size = new Size(320, 30),
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle
        };

        lblPassword = new Label
        {
            Text = "Пароль:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 151),
            AutoSize = true
        };

        txtPassword = new TextBox
        {
            Location = new Point(16, 176),
            Size = new Size(320, 30),
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true
        };

        lblEmail = new Label
        {
            Text = "Email:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 216),
            AutoSize = true
        };

        txtEmail = new TextBox
        {
            Location = new Point(16, 241),
            Size = new Size(320, 30),
            Font = new Font("Segoe UI", 12),
            BorderStyle = BorderStyle.FixedSingle
        };

        btnLogin = new Button
        {
            Text = "Войти",
            Location = new Point(16, 286),
            Size = new Size(320, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnRegister = new Button
        {
            Text = "Зарегистрироваться",
            Location = new Point(16, 286), 
            Size = new Size(320, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnToggleMode = new Button
        {
            Text = "Создать аккаунт",
            Location = new Point(16, 336), 
            Size = new Size(320, 35),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(0, 120, 215),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 }
        };

        lblStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(64, 64, 64),
            Location = new Point(16, 386),
            Size = new Size(320, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false
        };

        progressBar = new ProgressBar
        {
            Location = new Point(16, 436),
            Size = new Size(320, 6),
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };

        panel.Controls.AddRange(
        [
            lblUsername, txtUsername,
            lblPassword, txtPassword,
            lblEmail, txtEmail,
            btnLogin, btnRegister, btnToggleMode,
            lblStatus, progressBar
        ]);

        this.Controls.Add(lblTitle);
        this.Controls.Add(panel);
    }

    private void SetupBindings()
    {
        txtUsername.DataBindings.Add("Text", _viewModel, nameof(_viewModel.Username), true, DataSourceUpdateMode.OnPropertyChanged);
        txtPassword.DataBindings.Add("Text", _viewModel, nameof(_viewModel.Password), true, DataSourceUpdateMode.OnPropertyChanged);
        txtEmail.DataBindings.Add("Text", _viewModel, nameof(_viewModel.Email), true, DataSourceUpdateMode.OnPropertyChanged);

        btnLogin.Click += (s, e) => _viewModel.LoginCommand.Execute(null);
        btnRegister.Click += (s, e) => _viewModel.RegisterCommand.Execute(null);
        btnToggleMode.Click += (s, e) => _viewModel.ToggleModeCommand.Execute(null);

        _viewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.IsRegistering):
                    UpdateFormMode();
                    break;
                case nameof(_viewModel.IsLoading):
                    UpdateLoadingState();
                    break;
                case nameof(_viewModel.StatusMessage):
                    UpdateStatus();
                    break;
                case nameof(_viewModel.IsError):
                    UpdateStatusColor();
                    break;
            }
        };

        UpdateFormMode();
        UpdateLoadingState();
        UpdateStatus();
    }

    private void SetupFormProperties()
    {
        txtUsername.KeyPress += (s, e) => { if (e.KeyChar == (char)13) txtPassword.Focus(); };
        txtPassword.KeyPress += (s, e) => { if (e.KeyChar == (char)13) _viewModel.LoginCommand.Execute(null); };
        txtEmail.KeyPress += (s, e) => { if (e.KeyChar == (char)13) _viewModel.RegisterCommand.Execute(null); };
    }

    private void UpdateFormMode()
    {
        if (_viewModel.IsRegistering)
        {
            lblEmail.Visible = true;
            txtEmail.Visible = true;
            btnLogin.Visible = false;
            btnRegister.Visible = true;
            btnToggleMode.Text = "Уже есть аккаунт?";
            
            btnRegister.Location = new Point(16, 286);
            btnToggleMode.Location = new Point(16, 336);
            lblStatus.Location = new Point(16, 386);
            progressBar.Location = new Point(16, 436);
        }
        else
        {
            lblEmail.Visible = false;
            txtEmail.Visible = false;
            btnLogin.Visible = true;
            btnRegister.Visible = false;
            btnToggleMode.Text = "Создать аккаунт";
            
            btnLogin.Location = new Point(16, 286);
            btnToggleMode.Location = new Point(16, 336);
            lblStatus.Location = new Point(16, 386);
            progressBar.Location = new Point(16, 436);
        }
    }

    private void UpdateLoadingState()
    {
        btnLogin.Enabled = !_viewModel.IsLoading;
        btnRegister.Enabled = !_viewModel.IsLoading;
        btnToggleMode.Enabled = !_viewModel.IsLoading;
        txtUsername.Enabled = !_viewModel.IsLoading;
        txtPassword.Enabled = !_viewModel.IsLoading;
        txtEmail.Enabled = !_viewModel.IsLoading;
        progressBar.Visible = _viewModel.IsLoading;

        lblStatus.Visible = _viewModel.IsLoading && !string.IsNullOrEmpty(_viewModel.StatusMessage);
    }

    private void UpdateStatus()
    {
        lblStatus.Text = _viewModel.StatusMessage;
    }

    private void UpdateStatusColor()
    {
        lblStatus.ForeColor = _viewModel.IsError ? Color.Red : Color.FromArgb(64, 64, 64);
    }

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private TextBox txtEmail = null!;
    private Button btnLogin = null!;
    private Button btnRegister = null!;
    private Button btnToggleMode = null!;
    private Label lblStatus = null!;
    private ProgressBar progressBar = null!;
    private Label lblTitle = null!;
    private Panel panel = null!;
    private Label lblUsername = null!;
    private Label lblPassword = null!;
    private Label lblEmail = null!;
} 