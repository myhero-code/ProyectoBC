using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace ProyectoBC.Vistas
{
    public partial class ConfiguracionView : UserControl
    {
        public ConfiguracionView()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                DgCategorias.ItemsSource = ConexionDB.EjecutarConsulta("SELECT ID_Categoria, Nombre_Categoria, Descripcion FROM dbo.Categoria ORDER BY ID_Categoria DESC").DefaultView;
                DgUsuariosLogin.ItemsSource = ConexionDB.EjecutarConsulta("SELECT Id_Usuario, Nombre_Usuario, Cargo FROM dbo.Login ORDER BY Id_Usuario DESC").DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar configuración:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAgregarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombreCategoria.Text))
            {
                MessageBox.Show("Ingrese el nombre de la categoría.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ConexionDB.EjecutarComando("INSERT INTO dbo.Categoria (Nombre_Categoria, Descripcion) VALUES (@nombre, @desc)",
                    new SqlParameter("@nombre", TxtNombreCategoria.Text.Trim()),
                    new SqlParameter("@desc", string.IsNullOrWhiteSpace(TxtDescCategoria.Text) ? DBNull.Value : TxtDescCategoria.Text.Trim()));

                TxtNombreCategoria.Clear();
                TxtDescCategoria.Clear();
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar categoría:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCrearUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNuevoUsuario.Text) || string.IsNullOrEmpty(TxtNuevaPass.Password))
            {
                MessageBox.Show("Ingrese usuario y contraseña.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string cargo = (CmbCargo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Bibliotecario";
                ConexionDB.EjecutarComando("INSERT INTO dbo.Login (Nombre_Usuario, Contraseña, Cargo) VALUES (@u, @p, @c)",
                    new SqlParameter("@u", TxtNuevoUsuario.Text.Trim()),
                    new SqlParameter("@p", TxtNuevaPass.Password),
                    new SqlParameter("@c", cargo));

                MessageBox.Show("Usuario creado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtNuevoUsuario.Clear();
                TxtNuevaPass.Clear();
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al crear usuario:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnProbarConexion_Click(object sender, RoutedEventArgs e)
        {
            if (ConexionDB.ProbarConexion(out string error))
            {
                MessageBox.Show("Conexión exitosa con SQL Server (Biblioteca_Municipal).", "Conexión Activa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Fallo en la conexión:\n" + error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}