using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SportingGoodsApp
{
    public partial class MainWindow : Window
    {
        private int currentUserId;
        private string currentFullName;
        private string currentRole;
        private DataTable productsTable;
        private string connString = @"Data Source=DEKO-PC;Initial Catalog=V2;Integrated Security=True;";
        private Border selectedBorder = null;
        private DataRowView selectedProduct = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(int userId, string fullName, string role) : this()
        {
            currentUserId = userId;
            currentFullName = fullName;
            currentRole = role;

            if (tbUserInfo != null)
                tbUserInfo.Text = fullName;

            Title = $"Спортивные товары - {fullName} ({role})";

            if (panelSearchFilter != null)
                panelSearchFilter.Visibility = (role == "Администратор" || role == "Менеджер") ? Visibility.Visible : Visibility.Collapsed;

            if (panelAdminButtons != null)
                panelAdminButtons.Visibility = (role == "Администратор") ? Visibility.Visible : Visibility.Collapsed;

            if (role == "Администратор" || role == "Менеджер")
                LoadSuppliers();

            LoadProducts();
        }

        private void LoadSuppliers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string sql = "SELECT ID_Поставщика, Название FROM Поставщики";
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    DataTable newDt = dt.Clone();
                    newDt.Rows.Add(0, "Все поставщики");
                    foreach (DataRow row in dt.Rows)
                        newDt.Rows.Add(row.ItemArray);

                    cmbSupplier.ItemsSource = newDt.DefaultView;
                    cmbSupplier.DisplayMemberPath = "Название";
                    cmbSupplier.SelectedValuePath = "ID_Поставщика";
                    cmbSupplier.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки поставщиков: " + ex.Message);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT DISTINCT 
                            p.ID_Товара,
                            p.Артикул, 
                            p.Наименование, 
                            p.ЕдиницаИзмерения,
                            p.Стоимость AS БазоваяЦена,
                            p.ДействующаяСкидка,
                            p.КолНаСкладе,
                            p.Описание,
                            m.ID_Производители AS ID_Производитель,
                            m.Название AS Производитель,
                            s.ID_Поставщика AS ID_Поставщик,
                            s.Название AS Поставщик,
                            cat.ID_Категории AS ID_Категория,
                            cat.Название AS Категория,
                            p.ПутьКИзображению
                        FROM Товары p
                        LEFT JOIN Производители m ON p.FK_Производитель = m.ID_Производители
                        LEFT JOIN Поставщики s ON p.FK_Поставщик = s.ID_Поставщика
                        LEFT JOIN Категории cat ON p.FK_Категория = cat.ID_Категории
                        WHERE 1=1";

                    if (currentRole == "Администратор" || currentRole == "Менеджер")
                    {
                        if (txtSearch != null && !string.IsNullOrWhiteSpace(txtSearch.Text))
                        {
                            string search = txtSearch.Text.Replace("'", "''");
                            sql += $" AND (p.Наименование LIKE '%{search}%' OR p.Описание LIKE '%{search}%' OR m.Название LIKE '%{search}%' OR s.Название LIKE '%{search}%')";
                        }

                        if (cmbSupplier != null && cmbSupplier.SelectedValue != null && cmbSupplier.SelectedValue.ToString() != "0")
                        {
                            sql += $" AND s.ID_Поставщика = {cmbSupplier.SelectedValue}";
                        }

                        if (cmbSort != null && cmbSort.SelectedIndex == 1)
                            sql += " ORDER BY p.КолНаСкладе ASC";
                        else if (cmbSort != null && cmbSort.SelectedIndex == 2)
                            sql += " ORDER BY p.КолНаСкладе DESC";
                    }

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    productsTable = new DataTable();
                    da.Fill(productsTable);

                    if (!productsTable.Columns.Contains("Цена"))
                        productsTable.Columns.Add("Цена", typeof(string));
                    if (!productsTable.Columns.Contains("СтараяЦена"))
                        productsTable.Columns.Add("СтараяЦена", typeof(string));
                    if (!productsTable.Columns.Contains("ПоказатьСтаруюЦену"))
                        productsTable.Columns.Add("ПоказатьСтаруюЦену", typeof(Visibility));

                    foreach (DataRow row in productsTable.Rows)
                    {
                        decimal basePrice = Convert.ToDecimal(row["БазоваяЦена"]);
                        int discount = Convert.ToInt32(row["ДействующаяСкидка"]);
                        if (discount > 0)
                        {
                            decimal finalPrice = basePrice * (100 - discount) / 100;
                            row["СтараяЦена"] = basePrice.ToString("F2");
                            row["Цена"] = finalPrice.ToString("F2");
                            row["ПоказатьСтаруюЦену"] = Visibility.Visible;
                        }
                        else
                        {
                            row["СтараяЦена"] = DBNull.Value;
                            row["Цена"] = basePrice.ToString("F2");
                            row["ПоказатьСтаруюЦену"] = Visibility.Collapsed;
                        }

                        row["ПутьКИзображению"] = "Resources/picture.png";
                    }

                    if (icProducts != null)
                    {
                        icProducts.ItemsSource = null;
                        icProducts.ItemsSource = productsTable.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров: " + ex.Message);
            }
        }

        // Обработчик клика по карточке – выделение рамкой
        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            if (border == null) return;

            if (selectedBorder != null)
            {
                selectedBorder.BorderBrush = Brushes.DarkGray;
                selectedBorder.BorderThickness = new Thickness(1);
            }

            selectedBorder = border;
            selectedBorder.BorderBrush = Brushes.Blue;
            selectedBorder.BorderThickness = new Thickness(3);

            selectedProduct = border.DataContext as DataRowView;

            if (currentRole == "Администратор")
            {
                btnEdit.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadProducts();
        private void CmbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadProducts();
        private void CmbSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadProducts();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProductEditWindow editWindow = new ProductEditWindow(connString);
            if (editWindow.ShowDialog() == true)
                LoadProducts();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null) return;
            int productId = Convert.ToInt32(selectedProduct["ID_Товара"]);
            ProductEditWindow editWindow = new ProductEditWindow(connString, productId);
            if (editWindow.ShowDialog() == true)
                LoadProducts();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null) return;
            int productId = Convert.ToInt32(selectedProduct["ID_Товара"]);
            string productName = selectedProduct["Наименование"].ToString();

            if (MessageBox.Show($"Удалить товар '{productName}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM СоставЗаказа WHERE FK_Товар = @id", conn);
                    cmd.Parameters.AddWithValue("@id", productId);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        MessageBox.Show("Нельзя удалить товар, который присутствует в заказах.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    cmd = new SqlCommand("DELETE FROM Товары WHERE ID_Товара = @id", conn);
                    cmd.Parameters.AddWithValue("@id", productId);
                    cmd.ExecuteNonQuery();

                    LoadProducts();
                    selectedBorder = null;
                    selectedProduct = null;
                    btnEdit.IsEnabled = false;
                    btnDelete.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Заказы");
    }
}