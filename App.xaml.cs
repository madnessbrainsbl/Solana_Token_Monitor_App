using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Windows;
using TokenMonitorApp.Services;

namespace TokenMonitorApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Во время окна ввода ключа запрещаем авто-завершение приложения
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            // Запускаем процесс авторизации
            CheckAuthorization();
        }
        
        private async void CheckAuthorization()
        {
            try
            {
                // 1) Пытаемся взять сохраненный ключ
                var saved = CredentialStore.GetKey();
                if (!string.IsNullOrEmpty(saved))
                {
                    TryAuthorize(saved);
                    return;
                }

                // 2) Если нет сохраненного — спрашиваем у пользователя
                var keyWindow = new KeyInputWindow();
                var dialogResult = keyWindow.ShowDialog();
                
                if (dialogResult == true && !string.IsNullOrEmpty(keyWindow.Key))
                {
                    var key = keyWindow.Key;
                    
                    // ТЕСТОВЫЙ РЕЖИМ: ключ для отладки
                    if (key == "TEST-DEBUG-2024")
                    {
                        CredentialStore.SaveKey(key);
                        MessageBox.Show("Тестовый режим активирован!\nДоступ предоставлен для отладки.", "Debug Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Создаем и показываем главное окно
                        var mainWindow = new MainWindow();
                        this.MainWindow = mainWindow;
                        
                        // После успешной авторизации возвращаем стандартный режим завершения
                        this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                        
                        mainWindow.Show();
                        return;
                    }
                    
                    // Обычная проверка через Firebase (пока не работает)
                    try 
                    {
                        var firebase = new FirebaseClient("https://your-firebase-project.firebaseio.com/");
                        var user = await firebase.Child("users").Child(key).OnceSingleAsync<UserAccess>();
                        
                        if (user != null && user.Expiry > DateTime.UtcNow && !user.Revoked)
                        {
                            if (user.IsAdmin)
                            {
                                // Optionally show admin window
                            }
                            
                            CredentialStore.SaveKey(key);
                            var mainWindow = new MainWindow();
                            this.MainWindow = mainWindow;
                            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                            mainWindow.Show();
                        }
                        else
                        {
                            MessageBox.Show("Недействительный или истекший ключ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            this.Shutdown();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка проверки ключа: {ex.Message}\n\nИспользуйте ключ TEST-DEBUG-2024 для тестирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Shutdown();
                    }
                }
                else
                {
                    // Пользователь закрыл окно без ввода ключа
                    this.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка приложения: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown();
            }
        }

        private void TryAuthorize(string key)
        {
            // Используем ту же логику, что и для ручного ввода ключа в тестовом режиме или Firebase
            if (key == "TEST-DEBUG-2024")
            {
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
                return;
            }

            // В будущем: здесь можно повторить вызов к Firebase
            var main = new MainWindow();
            this.MainWindow = main;
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
    }

    public class UserAccess
    {
        public DateTime Expiry { get; set; }
        public bool Revoked { get; set; }
        public bool IsAdmin { get; set; }
    }
}
