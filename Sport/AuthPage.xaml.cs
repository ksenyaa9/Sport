using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace Sport
{
    public partial class AuthPage : Page
    {
        public AuthPage()
        {
            InitializeComponent();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Заполните все поля";
                return;
            }

            try
            {
                using (var context = new SportComplexYurmatyEntities())
                {
                    var user = context.User
                        .Include(u => u.Role)  // Загружаем связанную роль
                        .FirstOrDefault(u => u.Login == login && u.Password == password);

                    if (user != null)
                    {
                        // Проверяем роль пользователя
                        if (user.Role.Name == "Тренер")
                        {
                            ErrorText.Text = "Тренерам вход в приложение запрещен";
                            return;
                        }

                        // Сохраняем информацию о пользователе
                        App.CurrentUser = user;
                        App.UserRole = user.Role.Name;

                        // ОЧИЩАЕМ ПОЛЯ ПЕРЕД ПЕРЕХОДОМ
                        ClearAuthFields();

                        // Переходим на страницу клиентов
                        Manager.MainFrame.Navigate(new SportPage());
                    }
                    else
                    {
                        ErrorText.Text = "Неверный логин или пароль";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Ошибка авторизации: {ex.Message}";
            }
        }

        // Очистка полей при показе страницы авторизации
        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                ClearAuthFields();
            }
        }

        private void ClearAuthFields()
        {
            // Очищаем поля авторизации
            EmailTextBox.Text = string.Empty;
            PasswordBox.Password = string.Empty;
            ErrorText.Text = string.Empty;
        }
    }
}