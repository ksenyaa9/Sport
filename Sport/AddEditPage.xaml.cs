using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Data.Entity;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Sport
{
    public partial class AddEditPage : Page
    {
        private Clients _currentClient = new Clients();
        private bool _isEditing = false;
        private Subscriptions _currentSubscription;

        public AddEditPage(Clients selectedClient)
        {
            InitializeComponent();

            // Загружаем типы подписок
            LoadSubscriptionTypes();

            if (selectedClient != null)
            {
                _currentClient = selectedClient;
                _isEditing = true;

                // Загружаем текущую подписку клиента
                LoadClientSubscription();
            }
            else
            {
                _currentClient.RegistrationDate = DateTime.Today;
                _isEditing = false;
            }

            DataContext = _currentClient;
            LoadData();

            // Добавляем обработчик для валидации ввода телефона
            TxtPhone.PreviewTextInput += TxtPhone_PreviewTextInput;
            TxtPhone.TextChanged += TxtPhone_TextChanged;
        }

        private void TxtPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, плюс и управляющие символы
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '+' && !char.IsControl(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void TxtPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Автоматически форматируем номер телефона
            string text = TxtPhone.Text;

            // Удаляем все нецифровые символы, кроме +
            string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Проверяем, начинается ли номер с +7
            if (digits.StartsWith("+7") && digits.Length > 2)
            {
                // Форматируем номер: +7(XXX)XXX-XX-XX
                string cleanDigits = digits.Substring(2); // Убираем +7

                if (cleanDigits.Length > 0)
                {
                    string formatted = "+7";

                    if (cleanDigits.Length > 0)
                        formatted += $"({cleanDigits.Substring(0, Math.Min(3, cleanDigits.Length))})";

                    if (cleanDigits.Length > 3)
                        formatted += $"{cleanDigits.Substring(3, Math.Min(3, cleanDigits.Length - 3))}";

                    if (cleanDigits.Length > 6)
                        formatted += $"-{cleanDigits.Substring(6, Math.Min(2, cleanDigits.Length - 6))}";

                    if (cleanDigits.Length > 8)
                        formatted += $"-{cleanDigits.Substring(8, Math.Min(2, cleanDigits.Length - 8))}";

                    // Устанавливаем отформатированный текст только если он отличается
                    if (formatted != text)
                    {
                        int caretIndex = TxtPhone.CaretIndex;
                        TxtPhone.Text = formatted;
                        TxtPhone.CaretIndex = Math.Min(caretIndex, formatted.Length);
                    }
                }
            }
        }

        private void LoadSubscriptionTypes()
        {
            using (var context = new SportComplexYurmatyEntities())
            {
                var subscriptionTypes = context.SubscriptionTypes.ToList();
                CmbSubscriptionType.ItemsSource = subscriptionTypes;
            }
        }

        private void LoadClientSubscription()
        {
            using (var context = new SportComplexYurmatyEntities())
            {
                _currentSubscription = context.Subscriptions
                    .Include(s => s.SubscriptionTypes)
                    .FirstOrDefault(s => s.ClientId == _currentClient.ClientId && s.Status == "Активен");

                if (_currentSubscription != null)
                {
                    // Нужно найти соответствующий элемент в ItemsSource ComboBox
                    var subscriptionTypes = CmbSubscriptionType.ItemsSource as System.Collections.IList;
                    if (subscriptionTypes != null)
                    {
                        foreach (SubscriptionTypes type in subscriptionTypes)
                        {
                            if (type.SubscriptionTypeId == _currentSubscription.SubscriptionTypeId)
                            {
                                CmbSubscriptionType.SelectedItem = type;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void LoadData()
        {
            if (_isEditing)
            {
                TxtLastName.Text = _currentClient.LastName;
                TxtFirstName.Text = _currentClient.FirstName;
                TxtMiddleName.Text = _currentClient.MiddleName ?? "";
                DpBirthDate.SelectedDate = _currentClient.DateBith;
                TxtPhone.Text = _currentClient.Phone;
                DpRegistrationDate.SelectedDate = _currentClient.RegistrationDate;
            }
            else
            {
                DpRegistrationDate.SelectedDate = DateTime.Today;
                DpRegistrationDate.IsEnabled = false;
            }
        }

        private bool ValidatePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Удаляем все символы, кроме цифр и +
            string cleanPhone = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Проверяем формат: +7XXXXXXXXXX (всего 12 символов с +)
            if (cleanPhone.Length != 12 || !cleanPhone.StartsWith("+7"))
                return false;

            // Проверяем, что после +7 идут только цифры
            string digitsOnly = cleanPhone.Substring(1); // Убираем +
            if (!digitsOnly.All(char.IsDigit))
                return false;

            // Проверяем, что первая цифра после +7 - 9 (по российскому формату)
            if (cleanPhone[2] != '9')
                return false;

            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(TxtLastName.Text) ||
                string.IsNullOrWhiteSpace(TxtFirstName.Text) ||
                string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                MessageBox.Show("Заполните обязательные поля: Фамилия, Имя, Телефон!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Валидация даты рождения
            if (!DpBirthDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Дата рождения не может быть пустой!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка, что дата рождения не в будущем
            if (DpBirthDate.SelectedDate.Value > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка возраста (например, минимальный возраст 14 лет)
            int minAge = 14;
            DateTime minBirthDate = DateTime.Today.AddYears(-minAge);
            if (DpBirthDate.SelectedDate.Value > minBirthDate)
            {
                MessageBox.Show($"Клиент должен быть старше {minAge} лет!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверка, что дата рождения не слишком старая (например, не старше 100 лет)
            int maxAge = 100;
            DateTime maxBirthDate = DateTime.Today.AddYears(-maxAge);
            if (DpBirthDate.SelectedDate.Value < maxBirthDate)
            {
                MessageBox.Show($"Дата рождения не может быть раньше {maxAge} лет назад!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Валидация номера телефона
            string phoneNumber = TxtPhone.Text;
            if (!ValidatePhoneNumber(phoneNumber))
            {
                MessageBox.Show("Номер телефона должен быть в формате: +79XXXXXXXXX!\n" +
                               "Пример: +79123456789", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new SportComplexYurmatyEntities())
                {
                    // Находим клиента в базе (для редактирования) или создаем нового
                    Clients clientToSave;
                    if (_isEditing)
                    {
                        clientToSave = context.Clients.Find(_currentClient.ClientId);
                        if (clientToSave == null)
                        {
                            MessageBox.Show("Клиент не найден в базе данных!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        clientToSave = new Clients();
                        context.Clients.Add(clientToSave);
                    }

                    // Обновляем данные клиента
                    clientToSave.LastName = TxtLastName.Text;
                    clientToSave.FirstName = TxtFirstName.Text;
                    clientToSave.MiddleName = string.IsNullOrWhiteSpace(TxtMiddleName.Text) ? null : TxtMiddleName.Text;

                    // Дата рождения уже проверена
                    clientToSave.DateBith = DpBirthDate.SelectedDate.Value;

                    // Телефон уже проверен
                    clientToSave.Phone = phoneNumber;

                    if (!_isEditing)
                    {
                        clientToSave.RegistrationDate = DateTime.Today;
                    }

                    // Обработка подписки
                    if (CmbSubscriptionType.SelectedItem is SubscriptionTypes selectedType)
                    {
                        // Находим активную подписку клиента
                        var existingSubscription = context.Subscriptions
                            .FirstOrDefault(s => s.ClientId == clientToSave.ClientId && s.Status == "Активен");

                        if (existingSubscription == null)
                        {
                            // Создаем новую подписку
                            var newSubscription = new Subscriptions
                            {
                                ClientId = clientToSave.ClientId,
                                SubscriptionTypeId = selectedType.SubscriptionTypeId,
                                StartDate = DateTime.Today,
                                EndDate = DateTime.Today.AddDays(selectedType.DurationDays),
                                Status = "Активен"
                            };
                            context.Subscriptions.Add(newSubscription);
                        }
                        else
                        {
                            // Обновляем существующую подписку
                            existingSubscription.SubscriptionTypeId = selectedType.SubscriptionTypeId;
                            existingSubscription.EndDate = DateTime.Today.AddDays(selectedType.DurationDays);
                            existingSubscription.Status = "Активен";
                        }
                    }

                    context.SaveChanges();

                    // Обновляем ID клиента для нового клиента
                    if (!_isEditing)
                    {
                        _currentClient.ClientId = clientToSave.ClientId;
                    }
                }

                MessageBox.Show(_isEditing ? "Данные клиента обновлены!" : "Клиент добавлен!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Все несохраненные изменения будут потеряны. Продолжить?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Manager.MainFrame.GoBack();
            }
        }
    }
}