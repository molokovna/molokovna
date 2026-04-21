using System;
using System.Data.SqlClient;
using System.Windows;

namespace SportingGoodsApp
{
    public partial class LoginWindow : Window
    {
        string connString = @"Data Source=DEKO-PC;Initial Catalog=V2;Integrated Security=True;";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"SELECT u.ID_Пользователя, u.ФИО, r.Название 
                                   FROM Пользователи u
                                   JOIN Роли r ON u.FK_Роль = r.ID_Роль
                                   WHERE u.Логин = @login AND u.Пароль = @pass";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@pass", password);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32(0);
                        string fullName = reader.GetString(1);
                        string roleName = reader.GetString(2);

                        reader.Close();

                        MainWindow main = new MainWindow(userId, fullName, roleName);
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message, "Критично", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            // Гость: userId = -1, ФИО = "Гость", роль = "Гость"
            MainWindow main = new MainWindow(-1, "Гость", "Гость");
            main.Show();
            this.Close();
        }
    }
}