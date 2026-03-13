import tkinter as tk
from tkinter import ttk, messagebox
import sqlite3
import hashlib
from datetime import datetime

class PasswordManagerApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Менеджер паролей")
        self.root.geometry("600x500")
        self.current_user = None
        
        # Инициализация базы данных
        self.init_database()
        
        # Создание главного фрейма
        self.main_frame = ttk.Frame(root, padding="20")
        self.main_frame.pack(fill=tk.BOTH, expand=True)
        
        self.show_main_menu()
    
    def init_database(self):
        """Инициализация базы данных"""
        self.conn = sqlite3.connect('passwords.db', check_same_thread=False)
        self.cursor = self.conn.cursor()
        
        # Таблица пользователей
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        
        # Таблица паролей
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS passwords (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                service_name TEXT NOT NULL,
                login TEXT NOT NULL,
                password TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users (id)
            )
        ''')
        
        self.conn.commit()
    
    def hash_password(self, password):
        """Хеширование пароля"""
        return hashlib.sha256(password.encode()).hexdigest()
    
    def clear_frame(self):
        """Очистка фрейма"""
        for widget in self.main_frame.winfo_children():
            widget.destroy()
    
    def show_main_menu(self):
        """Показ главного меню"""
        self.clear_frame()
        
        title_label = ttk.Label(
            self.main_frame, 
            text="Менеджер паролей", 
            font=("Arial", 24, "bold")
        )
        title_label.pack(pady=(50, 100))
        
        # Кнопка регистрации
        register_btn = ttk.Button(
            self.main_frame, 
            text="Регистрация", 
            command=self.show_register_window,
            width=20
        )
        register_btn.pack(pady=10)
        
        # Кнопка авторизации
        login_btn = ttk.Button(
            self.main_frame, 
            text="Авторизация", 
            command=self.show_login_window,
            width=20
        )
        login_btn.pack(pady=10)
    
    def show_register_window(self):
        """Окно регистрации"""
        self.clear_frame()
        
        title_label = ttk.Label(
            self.main_frame, 
            text="Регистрация нового пользователя", 
            font=("Arial", 18, "bold")
        )
        title_label.pack(pady=(30, 30))
        
        # Фрейм для формы
        form_frame = ttk.Frame(self.main_frame)
        form_frame.pack(pady=20)
        
        # Поле имени пользователя
        ttk.Label(form_frame, text="Имя пользователя:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.register_username = ttk.Entry(form_frame, width=30)
        self.register_username.grid(row=0, column=1, pady=5, padx=10)
        
        # Поле пароля
        ttk.Label(form_frame, text="Пароль:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.register_password = ttk.Entry(form_frame, width=30, show="*")
        self.register_password.grid(row=1, column=1, pady=5, padx=10)
        
        # Поле подтверждения пароля
        ttk.Label(form_frame, text="Подтвердите пароль:").grid(row=2, column=0, sticky=tk.W, pady=5)
        self.register_confirm_password = ttk.Entry(form_frame, width=30, show="*")
        self.register_confirm_password.grid(row=2, column=1, pady=5, padx=10)
        
        # Кнопки
        button_frame = ttk.Frame(self.main_frame)
        button_frame.pack(pady=30)
        
        register_btn = ttk.Button(
            button_frame, 
            text="Зарегистрироваться", 
            command=self.register_user,
            width=20
        )
        register_btn.grid(row=0, column=0, padx=10)
        
        back_btn = ttk.Button(
            button_frame, 
            text="Назад", 
            command=self.show_main_menu,
            width=20
        )
        back_btn.grid(row=0, column=1, padx=10)
    
    def register_user(self):
        """Регистрация пользователя"""
        username = self.register_username.get().strip()
        password = self.register_password.get()
        confirm_password = self.register_confirm_password.get()
        
        if not username or not password:
            messagebox.showerror("Ошибка", "Все поля обязательны для заполнения!")
            return
        
        if password != confirm_password:
            messagebox.showerror("Ошибка", "Пароли не совпадают!")
            return
        
        if len(password) < 6:
            messagebox.showerror("Ошибка", "Пароль должен быть не менее 6 символов!")
            return
        
        try:
            password_hash = self.hash_password(password)
            self.cursor.execute(
                "INSERT INTO users (username, password_hash) VALUES (?, ?)",
                (username, password_hash)
            )
            self.conn.commit()
            messagebox.showinfo("Успех", "Пользователь успешно зарегистрирован!")
            self.show_main_menu()
        except sqlite3.IntegrityError:
            messagebox.showerror("Ошибка", "Пользователь с таким именем уже существует!")
    
    def show_login_window(self):
        """Окно авторизации"""
        self.clear_frame()
        
        title_label = ttk.Label(
            self.main_frame, 
            text="Авторизация", 
            font=("Arial", 18, "bold")
        )
        title_label.pack(pady=(30, 30))
        
        # Фрейм для формы
        form_frame = ttk.Frame(self.main_frame)
        form_frame.pack(pady=20)
        
        # Поле имени пользователя
        ttk.Label(form_frame, text="Имя пользователя:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.login_username = ttk.Entry(form_frame, width=30)
        self.login_username.grid(row=0, column=1, pady=5, padx=10)
        
        # Поле пароля
        ttk.Label(form_frame, text="Пароль:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.login_password = ttk.Entry(form_frame, width=30, show="*")
        self.login_password.grid(row=1, column=1, pady=5, padx=10)
        
        # Кнопки
        button_frame = ttk.Frame(self.main_frame)
        button_frame.pack(pady=30)
        
        login_btn = ttk.Button(
            button_frame, 
            text="Войти", 
            command=self.login_user,
            width=20
        )
        login_btn.grid(row=0, column=0, padx=10)
        
        back_btn = ttk.Button(
            button_frame, 
            text="Назад", 
            command=self.show_main_menu,
            width=20
        )
        back_btn.grid(row=0, column=1, padx=10)
    
    def login_user(self):
        """Авторизация пользователя"""
        username = self.login_username.get().strip()
        password = self.login_password.get()
        
        if not username or not password:
            messagebox.showerror("Ошибка", "Все поля обязательны для заполнения!")
            return
        
        password_hash = self.hash_password(password)
        
        self.cursor.execute(
            "SELECT id, username FROM users WHERE username = ? AND password_hash = ?",
            (username, password_hash)
        )
        
        user = self.cursor.fetchone()
        
        if user:
            self.current_user = {"id": user[0], "username": user[1]}
            self.show_password_manager()
        else:
            messagebox.showerror("Ошибка", "Неверное имя пользователя или пароль!")
    
    def show_password_manager(self):
        """Основной интерфейс менеджера паролей"""
        self.clear_frame()
        
        # Заголовок
        header_frame = ttk.Frame(self.main_frame)
        header_frame.pack(fill=tk.X, pady=(0, 20))
        
        title_label = ttk.Label(
            header_frame, 
            text=f"Менеджер паролей - {self.current_user['username']}", 
            font=("Arial", 16, "bold")
        )
        title_label.pack(side=tk.LEFT)
        
        logout_btn = ttk.Button(
            header_frame, 
            text="Выйти", 
            command=self.logout,
            width=10
        )
        logout_btn.pack(side=tk.RIGHT)
        
        # Фрейм для добавления пароля
        add_frame = ttk.LabelFrame(self.main_frame, text="Добавить новый пароль", padding="10")
        add_frame.pack(fill=tk.X, pady=(0, 20))
        
        ttk.Label(add_frame, text="Сервис:").grid(row=0, column=0, sticky=tk.W, pady=5, padx=5)
        self.service_name = ttk.Entry(add_frame, width=20)
        self.service_name.grid(row=0, column=1, pady=5, padx=5)
        
        ttk.Label(add_frame, text="Логин:").grid(row=0, column=2, sticky=tk.W, pady=5, padx=5)
        self.service_login = ttk.Entry(add_frame, width=20)
        self.service_login.grid(row=0, column=3, pady=5, padx=5)
        
        ttk.Label(add_frame, text="Пароль:").grid(row=1, column=0, sticky=tk.W, pady=5, padx=5)
        self.service_password = ttk.Entry(add_frame, width=20)
        self.service_password.grid(row=1, column=1, pady=5, padx=5)
        
        add_btn = ttk.Button(
            add_frame, 
            text="Добавить", 
            command=self.add_password,
            width=15
        )
        add_btn.grid(row=1, column=2, columnspan=2, pady=5, padx=5)
        
        # Таблица паролей
        list_frame = ttk.LabelFrame(self.main_frame, text="Сохраненные пароли", padding="10")
        list_frame.pack(fill=tk.BOTH, expand=True)
        
        # Создание Treeview
        columns = ("service", "login", "password", "date")
        self.password_tree = ttk.Treeview(list_frame, columns=columns, show="headings", height=10)
        
        self.password_tree.heading("service", text="Сервис")
        self.password_tree.heading("login", text="Логин")
        self.password_tree.heading("password", text="Пароль")
        self.password_tree.heading("date", text="Дата добавления")
        
        self.password_tree.column("service", width=150)
        self.password_tree.column("login", width=150)
        self.password_tree.column("password", width=150)
        self.password_tree.column("date", width=150)
        
        # Скроллбар
        scrollbar = ttk.Scrollbar(list_frame, orient=tk.VERTICAL, command=self.password_tree.yview)
        self.password_tree.configure(yscrollcommand=scrollbar.set)
        
        self.password_tree.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        
        # Кнопка удаления
        delete_btn = ttk.Button(
            list_frame, 
            text="Удалить выбранный", 
            command=self.delete_password,
            width=20
        )
        delete_btn.pack(pady=(10, 0))
        
        self.load_passwords()
    
    def load_passwords(self):
        """Загрузка паролей пользователя"""
        # Очистка таблицы
        for item in self.password_tree.get_children():
            self.password_tree.delete(item)
        
        # Загрузка из БД
        self.cursor.execute(
            "SELECT service_name, login, password, created_at FROM passwords WHERE user_id = ?",
            (self.current_user['id'],)
        )
        
        passwords = self.cursor.fetchall()
        
        for password in passwords:
            self.password_tree.insert("", tk.END, values=password)
    
    def add_password(self):
        """Добавление нового пароля"""
        service = self.service_name.get().strip()
        login = self.service_login.get().strip()
        password = self.service_password.get()
        
        if not service or not login or not password:
            messagebox.showerror("Ошибка", "Все поля обязательны для заполнения!")
            return
        
        self.cursor.execute(
            "INSERT INTO passwords (user_id, service_name, login, password) VALUES (?, ?, ?, ?)",
            (self.current_user['id'], service, login, password)
        )
        self.conn.commit()
        
        # Очистка полей
        self.service_name.delete(0, tk.END)
        self.service_login.delete(0, tk.END)
        self.service_password.delete(0, tk.END)
        
        self.load_passwords()
        messagebox.showinfo("Успех", "Пароль успешно добавлен!")
    
    def delete_password(self):
        """Удаление выбранного пароля"""
        selected = self.password_tree.selection()
        
        if not selected:
            messagebox.showwarning("Предупреждение", "Выберите запись для удаления!")
            return
        
        item = self.password_tree.item(selected[0])
        service = item['values'][0]
        login = item['values'][1]
        
        confirm = messagebox.askyesno("Подтверждение", f"Удалить пароль для сервиса '{service}'?")
        
        if confirm:
            self.cursor.execute(
                "DELETE FROM passwords WHERE user_id = ? AND service_name = ? AND login = ?",
                (self.current_user['id'], service, login)
            )
            self.conn.commit()
            self.load_passwords()
            messagebox.showinfo("Успех", "Пароль успешно удален!")
    
    def logout(self):
        """Выход из системы"""
        self.current_user = None
        self.show_main_menu()
    
    def on_closing(self):
        """Обработка закрытия приложения"""
        if self.conn:
            self.conn.close()
        self.root.destroy()


def main():
    root = tk.Tk()
    app = PasswordManagerApp(root)
    root.protocol("WM_DELETE_WINDOW", app.on_closing)
    root.mainloop()


if __name__ == "__main__":
    main()
