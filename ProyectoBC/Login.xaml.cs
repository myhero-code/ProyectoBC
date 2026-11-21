using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

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
            string password = ChkVerPassword.IsChecked == true ? TxtPasswordVisible.Text : TxtPassword.Password;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor ingrese usuario/CI y contraseña.", "Campos vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Verificar si es Personal (Administrador / Bibliotecario)
                DataTable dtAdmin = ConexionDB.EjecutarConsulta(
                    "SELECT Id_Usuario, Nombre_Usuario, Cargo FROM dbo.Login WHERE Nombre_Usuario = @u AND Contraseña = @p",
                    new SqlParameter("@u", usuario),
                    new SqlParameter("@p", password));

                if (dtAdmin.Rows.Count > 0)
                {
                    MenuPrincipal menu = new MenuPrincipal();
                    menu.WindowState = this.WindowState;
                    if (this.WindowState == WindowState.Normal)
                    {
                        menu.Width = this.Width;
                        menu.Height = this.Height;
                        menu.Left = this.Left;
                        menu.Top = this.Top;
                    }
                    menu.Show();
                    this.Close();
                    return;
                }

                // 2. Si no es personal, verificar si es un Lector registrado (CI + Contrasena)
                DataTable dtLector = ConexionDB.EjecutarConsulta(
                    "SELECT ID_Lector, Nombre, Apellido, DNI_CI FROM dbo.Lector WHERE DNI_CI = @ci AND Contrasena = @p",
                    new SqlParameter("@ci", usuario),
                    new SqlParameter("@p", password));

                if (dtLector.Rows.Count > 0)
                {
                    DataRow fila = dtLector.Rows[0];
                    int idLector = Convert.ToInt32(fila["ID_Lector"]);
                    string nombreCompleto = $"{fila["Nombre"]} {fila["Apellido"]}";
                    string ci = fila["DNI_CI"].ToString() ?? usuario;

                    PortalLectorWindow portal = new PortalLectorWindow(idLector, nombreCompleto, ci);
                    portal.WindowState = this.WindowState;
                    if (this.WindowState == WindowState.Normal)
                    {
                        portal.Width = this.Width;
                        portal.Height = this.Height;
                        portal.Left = this.Left;
                        portal.Top = this.Top;
                    }
                    portal.Show();
                    this.Close();
                    return;
                }

                // 3. Si no coincide en ninguna de las dos tablas
                MessageBox.Show("Usuario/CI o contraseña incorrectos.", "Acceso denegado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al intentar iniciar sesión:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void BtnRegistroLector_Click(object sender, RoutedEventArgs e)
        {
            RegistroLector registro = new RegistroLector();
            registro.WindowState = this.WindowState;
            if (this.WindowState == WindowState.Normal)
            {
                registro.Width = this.Width;
                registro.Height = this.Height;
                registro.Left = this.Left;
                registro.Top = this.Top;
            }
            registro.Show();
            this.Close();
        }
    }
}