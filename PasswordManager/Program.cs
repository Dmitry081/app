using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;
using BCrypt.Net;

namespace PasswordManager
{
    public partial class MainForm : Form
    {
        private string dbPath = "passwords.db";
        private string? currentUser = null;

        public MainForm()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            SQLiteConnection.CreateFile(dbPath);
            
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                
                string createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        PasswordHash TEXT NOT NULL
                    )";
                
                string createPasswordsTable = @"
                    CREATE TABLE IF NOT EXISTS Passwords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        ServiceName TEXT NOT NULL,
                        Login TEXT NOT NULL,
                        Password TEXT NOT NULL,
                        FOREIGN KEY(UserId) REFERENCES Users(Id))";
                
                using (var command = new SQLiteCommand(createUsersTable, connection))
                    command.ExecuteNonQuery();
                
                using (var command = new SQLiteCommand(createPasswordsTable, connection))
                    command.ExecuteNonQuery();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Менеджер паролей";
            this.Size = new System.Drawing.Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Label приветствия
            var welcomeLabel = new Label
            {
                Text = "Добро пожаловать в менеджер паролей!",
                AutoSize = true,
                Font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };

            // Кнопки регистрации и входа
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true
            };

            var registerButton = new Button
            {
                Text = "Регистрация",
                Size = new System.Drawing.Size(150, 40),
                Margin = new Padding(10)
            };
            registerButton.Click += RegisterButton_Click;

            var loginButton = new Button
            {
                Text = "Авторизация",
                Size = new System.Drawing.Size(150, 40),
                Margin = new Padding(10)
            };
            loginButton.Click += LoginButton_Click;

            buttonPanel.Controls.Add(registerButton);
            buttonPanel.Controls.Add(loginButton);

            // Список паролей
            var passwordList = new ListView
            {
                Name = "passwordList",
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill,
                Enabled = false
            };
            passwordList.Columns.Add("Сервис", 150);
            passwordList.Columns.Add("Логин", 150);
            passwordList.Columns.Add("Пароль", 200);

            // Панель управления паролями
            var controlPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                Visible = false,
                Name = "controlPanel"
            };

            var addPasswordButton = new Button
            {
                Text = "Добавить пароль",
                Size = new System.Drawing.Size(120, 35),
                Margin = new Padding(5)
            };
            addPasswordButton.Click += AddPasswordButton_Click;

            var deletePasswordButton = new Button
            {
                Text = "Удалить выбранный",
                Size = new System.Drawing.Size(120, 35),
                Margin = new Padding(5)
            };
            deletePasswordButton.Click += DeletePasswordButton_Click;

            var logoutButton = new Button
            {
                Text = "Выйти",
                Size = new System.Drawing.Size(100, 35),
                Margin = new Padding(5)
            };
            logoutButton.Click += LogoutButton_Click;

            controlPanel.Controls.Add(addPasswordButton);
            controlPanel.Controls.Add(deletePasswordButton);
            controlPanel.Controls.Add(logoutButton);

            mainPanel.Controls.Add(welcomeLabel, 0, 0);
            mainPanel.Controls.Add(passwordList, 0, 1);
            mainPanel.Controls.Add(buttonPanel, 0, 2);
            mainPanel.Controls.Add(controlPanel, 0, 2);

            this.Controls.Add(mainPanel);
        }

        private void RegisterButton_Click(object? sender, EventArgs e)
        {
            var registerForm = new RegisterForm(dbPath);
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show("Регистрация успешна! Теперь вы можете войти.", "Успех", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoginButton_Click(object? sender, EventArgs e)
        {
            var loginForm = new LoginForm(dbPath);
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                currentUser = loginForm.Username;
                ShowLoggedInInterface();
            }
        }

        private void ShowLoggedInInterface()
        {
            var mainPanel = (TableLayoutPanel)this.Controls[0];
            var buttonPanel = (FlowLayoutPanel)((mainPanel.GetControlFromPosition(0, 2) is FlowLayoutPanel) ? 
                mainPanel.GetControlFromPosition(0, 2) : ((FlowLayoutPanel)((TableLayoutPanel)mainPanel.Controls[2]).Controls[0]));
            var controlPanel = (FlowLayoutPanel)mainPanel.Controls.Find("controlPanel", true)[0];
            var passwordList = (ListView)mainPanel.GetControlFromPosition(0, 1)!;
            var welcomeLabel = (Label)mainPanel.GetControlFromPosition(0, 0)!;

            welcomeLabel.Text = $"Вы вошли как: {currentUser}";
            buttonPanel.Visible = false;
            controlPanel.Visible = true;
            passwordList.Enabled = true;
            
            LoadPasswords();
        }

        private void LoadPasswords()
        {
            var passwordList = (ListView)((TableLayoutPanel)this.Controls[0]).GetControlFromPosition(0, 1)!;
            passwordList.Items.Clear();

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                
                string getUserIdQuery = "SELECT Id FROM Users WHERE Username = @username";
                int userId = 0;
                
                using (var command = new SQLiteCommand(getUserIdQuery, connection))
                {
                    command.Parameters.AddWithValue("@username", currentUser);
                    var result = command.ExecuteScalar();
                    if (result != null)
                        userId = Convert.ToInt32(result);
                }

                if (userId > 0)
                {
                    string query = "SELECT ServiceName, Login, Password FROM Passwords WHERE UserId = @userId";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ListViewItem(reader["ServiceName"].ToString());
                                item.SubItems.Add(reader["Login"].ToString());
                                item.SubItems.Add(reader["Password"].ToString());
                                passwordList.Items.Add(item);
                            }
                        }
                    }
                }
            }
        }

        private void AddPasswordButton_Click(object? sender, EventArgs e)
        {
            var addForm = new AddPasswordForm(dbPath, currentUser!);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadPasswords();
            }
        }

        private void DeletePasswordButton_Click(object? sender, EventArgs e)
        {
            var passwordList = (ListView)((TableLayoutPanel)this.Controls[0]).GetControlFromPosition(0, 1)!;
            
            if (passwordList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedItem = passwordList.SelectedItems[0];
            string serviceName = selectedItem.SubItems[0].Text;
            string login = selectedItem.SubItems[1].Text;

            if (MessageBox.Show($"Удалить пароль для {serviceName} ({login})?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    
                    string getUserIdQuery = "SELECT Id FROM Users WHERE Username = @username";
                    int userId = 0;
                    
                    using (var command = new SQLiteCommand(getUserIdQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", currentUser);
                        var result = command.ExecuteScalar();
                        if (result != null)
                            userId = Convert.ToInt32(result);
                    }

                    if (userId > 0)
                    {
                        string deleteQuery = "DELETE FROM Passwords WHERE UserId = @userId AND ServiceName = @service AND Login = @login";
                        using (var command = new SQLiteCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            command.Parameters.AddWithValue("@service", serviceName);
                            command.Parameters.AddWithValue("@login", login);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                LoadPasswords();
            }
        }

        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            currentUser = null;
            
            var mainPanel = (TableLayoutPanel)this.Controls[0];
            var buttonPanel = (FlowLayoutPanel)mainPanel.Controls[2];
            var controlPanel = (FlowLayoutPanel)mainPanel.Controls.Find("controlPanel", true)[0];
            var passwordList = (ListView)mainPanel.GetControlFromPosition(0, 1)!;
            var welcomeLabel = (Label)mainPanel.GetControlFromPosition(0, 0)!;

            welcomeLabel.Text = "Добро пожаловать в менеджер паролей!";
            buttonPanel.Visible = true;
            controlPanel.Visible = false;
            passwordList.Enabled = false;
            passwordList.Items.Clear();
        }
    }

    public partial class RegisterForm : Form
    {
        private string dbPath;
        private TextBox usernameTextBox = null!;
        private TextBox passwordTextBox = null!;
        private TextBox confirmPasswordTextBox = null!;

        public RegisterForm(string dbPath)
        {
            this.dbPath = dbPath;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Регистрация";
            this.Size = new System.Drawing.Size(350, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                Padding = new Padding(20)
            };

            for (int i = 0; i < 5; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < 2; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Имя пользователя:", AutoSize = true }, 0, 0);
            usernameTextBox = new TextBox { Width = 200 };
            layout.Controls.Add(usernameTextBox, 1, 0);

            layout.Controls.Add(new Label { Text = "Пароль:", AutoSize = true }, 0, 1);
            passwordTextBox = new TextBox { Width = 200, PasswordChar = '*' };
            layout.Controls.Add(passwordTextBox, 1, 1);

            layout.Controls.Add(new Label { Text = "Подтвердите пароль:", AutoSize = true }, 0, 2);
            confirmPasswordTextBox = new TextBox { Width = 200, PasswordChar = '*' };
            layout.Controls.Add(confirmPasswordTextBox, 1, 2);

            var buttonPanel = new FlowLayoutPanel { ColumnSpan = 2, AutoSize = true };
            var okButton = new Button { Text = "Зарегистрироваться", Width = 130, DialogResult = DialogResult.OK };
            okButton.Click += OkButton_Click;
            var cancelButton = new Button { Text = "Отмена", Width = 100, DialogResult = DialogResult.Cancel };
            
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 4);

            this.Controls.Add(layout);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            string username = usernameTextBox.Text.Trim();
            string password = passwordTextBox.Text;
            string confirmPassword = confirmPasswordTextBox.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Пароль должен быть не менее 6 символов", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    
                    string insertQuery = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @password)";
                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", hashedPassword);
                        command.ExecuteNonQuery();
                    }
                }
                
                this.DialogResult = DialogResult.OK;
            }
            catch (SQLiteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
            {
                MessageBox.Show("Пользователь с таким именем уже существует", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }

    public partial class LoginForm : Form
    {
        private string dbPath;
        public string? Username { get; private set; }
        private TextBox usernameTextBox = null!;
        private TextBox passwordTextBox = null!;

        public LoginForm(string dbPath)
        {
            this.dbPath = dbPath;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Авторизация";
            this.Size = new System.Drawing.Size(300, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 2,
                Padding = new Padding(20)
            };

            for (int i = 0; i < 4; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < 2; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Имя пользователя:", AutoSize = true }, 0, 0);
            usernameTextBox = new TextBox { Width = 200 };
            layout.Controls.Add(usernameTextBox, 1, 0);

            layout.Controls.Add(new Label { Text = "Пароль:", AutoSize = true }, 0, 1);
            passwordTextBox = new TextBox { Width = 200, PasswordChar = '*' };
            layout.Controls.Add(passwordTextBox, 1, 1);

            var buttonPanel = new FlowLayoutPanel { ColumnSpan = 2, AutoSize = true };
            var okButton = new Button { Text = "Войти", Width = 100, DialogResult = DialogResult.OK };
            okButton.Click += OkButton_Click;
            var cancelButton = new Button { Text = "Отмена", Width = 100, DialogResult = DialogResult.Cancel };
            
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 3);

            this.Controls.Add(layout);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            string username = usernameTextBox.Text.Trim();
            string password = passwordTextBox.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    
                    string query = "SELECT PasswordHash FROM Users WHERE Username = @username";
                    string? hashedPassword = null;
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = command.ExecuteScalar();
                        if (result != null)
                            hashedPassword = result.ToString();
                    }

                    if (hashedPassword != null && BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                    {
                        Username = username;
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show("Неверное имя пользователя или пароль", "Ошибка", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.DialogResult = DialogResult.None;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }

    public partial class AddPasswordForm : Form
    {
        private string dbPath;
        private string username;
        private TextBox serviceNameTextBox = null!;
        private TextBox loginTextBox = null!;
        private TextBox passwordTextBox = null!;

        public AddPasswordForm(string dbPath, string username)
        {
            this.dbPath = dbPath;
            this.username = username;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Добавить пароль";
            this.Size = new System.Drawing.Size(350, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                Padding = new Padding(20)
            };

            for (int i = 0; i < 5; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            for (int i = 0; i < 2; i++)
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Сервис:", AutoSize = true }, 0, 0);
            serviceNameTextBox = new TextBox { Width = 200 };
            layout.Controls.Add(serviceNameTextBox, 1, 0);

            layout.Controls.Add(new Label { Text = "Логин:", AutoSize = true }, 0, 1);
            loginTextBox = new TextBox { Width = 200 };
            layout.Controls.Add(loginTextBox, 1, 1);

            layout.Controls.Add(new Label { Text = "Пароль:", AutoSize = true }, 0, 2);
            passwordTextBox = new TextBox { Width = 200 };
            layout.Controls.Add(passwordTextBox, 1, 2);

            var buttonPanel = new FlowLayoutPanel { ColumnSpan = 2, AutoSize = true };
            var okButton = new Button { Text = "Сохранить", Width = 100, DialogResult = DialogResult.OK };
            okButton.Click += OkButton_Click;
            var cancelButton = new Button { Text = "Отмена", Width = 100, DialogResult = DialogResult.Cancel };
            
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            layout.Controls.Add(buttonPanel, 0, 4);

            this.Controls.Add(layout);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            string serviceName = serviceNameTextBox.Text.Trim();
            string login = loginTextBox.Text.Trim();
            string password = passwordTextBox.Text;

            if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    
                    string getUserIdQuery = "SELECT Id FROM Users WHERE Username = @username";
                    int userId = 0;
                    
                    using (var command = new SQLiteCommand(getUserIdQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = command.ExecuteScalar();
                        if (result != null)
                            userId = Convert.ToInt32(result);
                    }

                    if (userId > 0)
                    {
                        string insertQuery = "INSERT INTO Passwords (UserId, ServiceName, Login, Password) VALUES (@userId, @service, @login, @password)";
                        using (var command = new SQLiteCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            command.Parameters.AddWithValue("@service", serviceName);
                            command.Parameters.AddWithValue("@login", login);
                            command.Parameters.AddWithValue("@password", password);
                            command.ExecuteNonQuery();
                        }
                        
                        this.DialogResult = DialogResult.OK;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
