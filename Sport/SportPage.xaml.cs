using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Data.Entity;

namespace Sport
{
    public partial class SportPage : Page
    {
        private Clients _currentClient = new Clients();

        public SportPage()
        {
            InitializeComponent();
            Loaded += SportPage_Loaded; // Подписываемся на событие загрузки страницы
        }

        private void SportPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            SetButtonsVisibility(); // Устанавливаем видимость кнопок в зависимости от роли
        }

        private void SetButtonsVisibility()
        {
            // Проверяем роль пользователя
            if (App.UserRole == "Менеджер")
            {
                // Скрываем кнопки для менеджера
                AddBtn.Visibility = Visibility.Collapsed;
                DltBtn.Visibility = Visibility.Collapsed;

                // Скрываем кнопку редактирования в DataGrid
                // Нам нужно скрыть всю колонку действий
                if (DGidSports.Columns.Count > 0)
                {
                    // Ищем колонку с кнопкой "Редактировать" (последняя колонка)
                    var lastColumn = DGidSports.Columns[DGidSports.Columns.Count - 1];
                    if (lastColumn.Header?.ToString() == "Действия")
                    {
                        lastColumn.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else if (App.UserRole == "Администратор")
            {
                // Показываем все кнопки для администратора
                AddBtn.Visibility = Visibility.Visible;
                DltBtn.Visibility = Visibility.Visible;

                // Показываем колонку действий
                if (DGidSports.Columns.Count > 0)
                {
                    var lastColumn = DGidSports.Columns[DGidSports.Columns.Count - 1];
                    lastColumn.Visibility = Visibility.Visible;
                }
            }
        }

        private void LoadData()
        {
            using (var context = new SportComplexYurmatyEntities())
            {
                // Загружаем клиентов с подписками и типами подписок
                var clients = context.Clients
                    .AsNoTracking() // Отключаем отслеживание изменений
                    .Include(c => c.Subscriptions.Select(s => s.SubscriptionTypes))
                    .ToList();
                DGidSports.ItemsSource = clients;
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (DGidSports.SelectedItem != null)
            {
                var selectedClient = DGidSports.SelectedItem as Clients;
                Manager.MainFrame.Navigate(new AddEditPage(selectedClient));
            }
            else
            {
                MessageBox.Show("Выберите клиента для редактирования!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void DltBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DGidSports.SelectedItem != null)
            {
                var selectedClient = DGidSports.SelectedItem as Clients;

                if (MessageBox.Show($"Вы уверены, что хотите удалить клиента {selectedClient.LastName} {selectedClient.FirstName}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new SportComplexYurmatyEntities())
                        {
                            // Находим клиента в контексте
                            var clientToDelete = context.Clients.Find(selectedClient.ClientId);
                            if (clientToDelete != null)
                            {
                                // Сначала удаляем связанные подписки
                                var subscriptions = context.Subscriptions
                                    .Where(s => s.ClientId == clientToDelete.ClientId)
                                    .ToList();
                                context.Subscriptions.RemoveRange(subscriptions);

                                // Затем удаляем клиента
                                context.Clients.Remove(clientToDelete);
                                context.SaveChanges();
                            }
                        }

                        LoadData(); // Перезагружаем данные
                        MessageBox.Show("Клиент успешно удален!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении клиента: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите клиента для удаления!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            using (var context = new SportComplexYurmatyEntities())
            {
                var currentClient = context.Clients
                    .AsNoTracking()
                    .Include(c => c.Subscriptions.Select(s => s.SubscriptionTypes))
                    .ToList();

                string searchText = TBoxSearch.Text.ToLower().Trim();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // Очищаем номер телефона от форматирования для поиска
                    string cleanSearchText = searchText
                        .Replace("+", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace(" ", "")
                        .Replace("-", "");

                    currentClient = currentClient.Where(p =>
                        (p.LastName != null && p.LastName.ToLower().Contains(searchText)) ||
                        (p.FirstName != null && p.FirstName.ToLower().Contains(searchText)) ||
                        (p.MiddleName != null && p.MiddleName.ToLower().Contains(searchText)) ||
                        (p.Phone != null && p.Phone.Replace("+", "")
                                                  .Replace("(", "")
                                                  .Replace(")", "")
                                                  .Replace(" ", "")
                                                  .Replace("-", "")
                                                  .ToLower()
                                                  .Contains(cleanSearchText)))
                        .ToList();
                }

                DGidSports.ItemsSource = currentClient;
            }
        }
    }

    public class SubscriptionConverter : System.Windows.Data.IValueConverter
    {
        public static SubscriptionConverter Instance = new SubscriptionConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is System.Collections.Generic.ICollection<Subscriptions> subscriptions)
            {
                if (subscriptions == null || !subscriptions.Any())
                    return "Нет подписки";

                var activeSubscription = subscriptions.FirstOrDefault(s => s.Status == "Активен");
                if (activeSubscription?.SubscriptionTypes != null)
                    return activeSubscription.SubscriptionTypes.Name;

                return subscriptions.First()?.SubscriptionTypes?.Name ?? "Нет подписки";
            }
            return "Нет подписки";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}