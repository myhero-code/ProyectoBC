using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace ProyectoBC
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        // Mostrar contraseña
        private void ChkVerPassword_Checked(object sender, RoutedEventArgs e)
        {
            TxtPasswordVisible.Text = TxtPassword.Password;
            TxtPassword.Visibility = Visibility.Collapsed;
            TxtPasswordVisible.Visibility = Visibility.Visible;
        }

        // Ocultar contraseña
        private void ChkVerPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtPassword.Password = TxtPasswordVisible.Text;
            TxtPasswordVisible.Visibility = Visibility.Collapsed;
            TxtPassword.Visibility = Visibility.Visible;
        }

        // Mantener sincronizados los campos de contraseña
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ChkVerPassword.IsChecked != true)
                TxtPasswordVisible.Text = TxtPassword.Password;
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ChkVerPassword.IsChecked == true)
                TxtPassword.Password = TxtPasswordVisible.Text;
        }

        // Botón Entrar
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = TxtUsuario.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Ingrese su usuario y contraseña.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection cn = ConexionDB.ObtenerConexion())
                {
                    cn.Open();

                    string sql = @"
                        SELECT Cargo
                        FROM dbo.Login
                        WHERE Nombre_Usuario = @usuario
                        AND Contraseña = @password";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@usuario", usuario);
                        cmd.Parameters.AddWithValue("@password", password);

                        object resultado = cmd.ExecuteScalar();

                        if (resultado != null && resultado != DBNull.Value)
                        {
                            MessageBox.Show("Inicio de sesión exitoso.", "Bienvenido", MessageBoxButton.OK, MessageBoxImage.Information);

                            MenuPrincipal menu = new MenuPrincipal();
                            if (this.WindowState == WindowState.Maximized)
                            {
                                menu.WindowState = WindowState.Maximized;
                            }
                            else
                            {
                                menu.WindowState = WindowState.Normal;
                                menu.Width = this.Width;
                                menu.Height = this.Height;
                                menu.Left = this.Left;
                                menu.Top = this.Top;
                            }

                            menu.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                "Usuario o contraseña incorrectos.",
                                "Acceso denegado",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            TxtPassword.Clear();
                            TxtPasswordVisible.Clear();
                            TxtUsuario.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al conectar con la base de datos:\n" + ex.Message,
                    "Error de Conexión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TxtUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}