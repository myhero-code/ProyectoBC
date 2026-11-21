using System;
using System.Data;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace ProyectoBC
{
    public partial class RegistroLector : Window
    {
        public RegistroLector()
        {
            InitializeComponent();
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string ci = TxtCI.Text.Trim();
            string nombre = TxtNombre.Text.Trim();
            string apellido = TxtApellido.Text.Trim();
            string telefono = TxtTelefono.Text.Trim();
            string pass = TxtPassword.Password;
            string confirmar = TxtConfirmarPassword.Password;

            if (string.IsNullOrEmpty(ci) || string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Complete todos los campos obligatorios (*).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (pass != confirmar)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DataTable existe = ConexionDB.EjecutarConsulta(
                    "SELECT ID_Lector FROM dbo.Lector WHERE DNI_CI = @ci",
                    new SqlParameter("@ci", ci));

                if (existe.Rows.Count > 0)
                {
                    MessageBox.Show("Ya existe un lector registrado con ese número de CI.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ConexionDB.EjecutarComando(
                    "INSERT INTO dbo.Lector (DNI_CI, Nombre, Apellido, Telefono, Fecha_Inicio, Contrasena) VALUES (@ci, @nom, @ape, @tel, GETDATE(), @pass)",
                    new SqlParameter("@ci", ci),
                    new SqlParameter("@nom", nombre),
                    new SqlParameter("@ape", apellido),
                    new SqlParameter("@tel", string.IsNullOrEmpty(telefono) ? DBNull.Value : telefono),
                    new SqlParameter("@pass", pass));

                MessageBox.Show($"¡Registro exitoso, {nombre}!\nAhora puedes iniciar sesión usando tu CI ({ci}) y tu contraseña.", "Bienvenido", MessageBoxButton.OK, MessageBoxImage.Information);
                VolverAlLogin();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            VolverAlLogin();
        }

        private void VolverAlLogin()
        {
            Login login = new Login();
            login.WindowState = this.WindowState;
            if (this.WindowState == WindowState.Normal)
            {
                login.Width = this.Width;
                login.Height = this.Height;
                login.Left = this.Left;
                login.Top = this.Top;
            }
            login.Show();
            this.Close();
        }
    }
}