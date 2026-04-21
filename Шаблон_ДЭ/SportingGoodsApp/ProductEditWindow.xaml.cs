using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SportingGoodsApp
{
    public partial class ProductEditWindow : Window
    {
        private string connString;
        private int? productId = null;
        private string imagePath = null;
        private string imageFileName = null;

        public ProductEditWindow(string connString, int? productId = null)
        {
            InitializeComponent();
            this.connString = connString;
            this.productId = productId;

            LoadComboBoxes();
            if (productId.HasValue)
                LoadProductData();
        }

        private void LoadComboBoxes()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT ID_Производители, Название FROM Производители", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cmbManufacturer.ItemsSource = dt.DefaultView;

                da = new SqlDataAdapter("SELECT ID_Поставщика, Название FROM Поставщики", conn);
                dt = new DataTable();
                da.Fill(dt);
                cmbSupplier.ItemsSource = dt.DefaultView;
            }
        }

        private void LoadProductData()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string sql = "SELECT Наименование, Описание, FK_Производитель, FK_Поставщик, Стоимость, ЕдиницаИзмерения, КолНаСкладе, ДействующаяСкидка, ПутьКИзображению FROM Товары WHERE ID_Товара = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", productId.Value);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtName.Text = reader["Наименование"].ToString();
                    txtDescription.Text = reader["Описание"].ToString();
                    cmbManufacturer.SelectedValue = reader["FK_Производитель"];
                    cmbSupplier.SelectedValue = reader["FK_Поставщик"];
                    txtPrice.Text = reader["Стоимость"].ToString();
                    txtUnit.Text = reader["ЕдиницаИзмерения"].ToString();
                    txtStock.Text = reader["КолНаСкладе"].ToString();
                    txtDiscount.Text = reader["ДействующаяСкидка"].ToString();
                    string img = reader["ПутьКИзображению"].ToString();
                    if (!string.IsNullOrEmpty(img) && File.Exists(img))
                    {
                        imagePath = img;
                        imgProduct.Source = new BitmapImage(new Uri(img, UriKind.RelativeOrAbsolute));
                    }
                }
                reader.Close();
            }
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp";
            if (ofd.ShowDialog() == true)
            {
                string destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
                string destFile = Path.Combine(destDir, Guid.NewGuid().ToString() + Path.GetExtension(ofd.FileName));
                File.Copy(ofd.FileName, destFile, true);
                imagePath = destFile;
                imgProduct.Source = new BitmapImage(new Uri(destFile));
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите наименование");
                return;
            }
            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Цена должна быть числом >=0");
                return;
            }
            if (!int.TryParse(txtStock.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Количество должно быть целым >=0");
                return;
            }
            if (!int.TryParse(txtDiscount.Text, out int discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Скидка от 0 до 100");
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlCommand cmd;
                if (productId.HasValue)
                {
                    string sql = @"UPDATE Товары SET 
                                    Наименование = @name,
                                    Описание = @desc,
                                    FK_Производитель = @manuf,
                                    FK_Поставщик = @supp,
                                    Стоимость = @price,
                                    ЕдиницаИзмерения = @unit,
                                    КолНаСкладе = @stock,
                                    ДействующаяСкидка = @disc,
                                    ПутьКИзображению = @img
                                  WHERE ID_Товара = @id";
                    cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", productId.Value);
                }
                else
                {
                    string sql = @"INSERT INTO Товары (Наименование, Описание, FK_Производитель, FK_Поставщик, Стоимость, ЕдиницаИзмерения, КолНаСкладе, ДействующаяСкидка, ПутьКИзображению, Артикул) 
                                   VALUES (@name, @desc, @manuf, @supp, @price, @unit, @stock, @disc, @img, @art)";
                    cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@art", Guid.NewGuid().ToString().Substring(0, 8));
                }
                cmd.Parameters.AddWithValue("@name", txtName.Text);
                cmd.Parameters.AddWithValue("@desc", txtDescription.Text);
                cmd.Parameters.AddWithValue("@manuf", cmbManufacturer.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@supp", cmbSupplier.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@unit", txtUnit.Text);
                cmd.Parameters.AddWithValue("@stock", stock);
                cmd.Parameters.AddWithValue("@disc", discount);
                cmd.Parameters.AddWithValue("@img", imagePath ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}