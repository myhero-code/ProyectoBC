using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace ProyectoBC.Vistas
{
    public partial class AutoresView : UserControl
    {
        private int? _idAutorSeleccionado = null;

        public AutoresView()
        {
            InitializeComponent();
            CargarAutores();
        }

        private void CargarAutores(string filtro = "")
        {
            try
            {
                string sql = @"
                    SELECT a.ID_Autor, a.Nombre_Autor, ISNULL(a.Nacionalidad, 'No especificada') AS Nacionalidad,
                           COUNT(la.ID_Libro) AS TotalLibros
                    FROM dbo.Autor a
                    LEFT JOIN dbo.Libro_Autor la ON a.ID_Autor = la.ID_Autor
                    WHERE (@filtro = '' OR a.Nombre_Autor LIKE '%' + @filtro + '%' OR ISNULL(a.Nacionalidad, '') LIKE '%' + @filtro + '%')
                    GROUP BY a.ID_Autor, a.Nombre_Autor, a.Nacionalidad
                    ORDER BY a.ID_Autor DESC";

                DgAutores.ItemsSource = ConexionDB.EjecutarConsulta(sql, new SqlParameter("@filtro", filtro)).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar autores:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuardarAutor_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombreAutor.Text))
            {
                MessageBox.Show("El nombre del autor es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                object nacionalidad = string.IsNullOrWhiteSpace(TxtNacionalidad.Text) ? DBNull.Value : TxtNacionalidad.Text.Trim();

                if (_idAutorSeleccionado == null)
                {
                    ConexionDB.EjecutarComando("INSERT INTO dbo.Autor (Nombre_Autor, Nacionalidad) VALUES (@nom, @nac)",
                        new SqlParameter("@nom", TxtNombreAutor.Text.Trim()),
                        new SqlParameter("@nac", nacionalidad));
                    MessageBox.Show("Autor registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ConexionDB.EjecutarComando("UPDATE dbo.Autor SET Nombre_Autor = @nom, Nacionalidad = @nac WHERE ID_Autor = @id",
                        new SqlParameter("@nom", TxtNombreAutor.Text.Trim()),
                        new SqlParameter("@nac", nacionalidad),
                        new SqlParameter("@id", _idAutorSeleccionado.Value));
                    MessageBox.Show("Autor actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                LimpiarFormulario();
                CargarAutores(TxtBuscarAutor.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar autor:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgAutores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgAutores.SelectedItem is DataRowView row)
            {
                _idAutorSeleccionado = Convert.ToInt32(row["ID_Autor"]);
                TxtNombreAutor.Text = row["Nombre_Autor"].ToString();
                string nac = row["Nacionalidad"].ToString() ?? "";
                TxtNacionalidad.Text = nac == "No especificada" ? "" : nac;
                BtnGuardarAutor.Content = "Actualizar Autor";
            }
        }

        private void BtnEliminarAutor_Click(object sender, RoutedEventArgs e)
        {
            if (_idAutorSeleccionado == null)
            {
                MessageBox.Show("Seleccione un autor de la tabla para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("¿Seguro que desea eliminar este autor?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    ConexionDB.EjecutarComando("DELETE FROM dbo.Autor WHERE ID_Autor = @id", new SqlParameter("@id", _idAutorSeleccionado.Value));
                    LimpiarFormulario();
                    CargarAutores(TxtBuscarAutor.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se puede eliminar porque tiene libros asociados:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            _idAutorSeleccionado = null;
            TxtNombreAutor.Clear();
            TxtNacionalidad.Clear();
            BtnGuardarAutor.Content = "Registrar Autor";
            DgAutores.SelectedIndex = -1;
        }

        private void TxtBuscarAutor_TextChanged(object sender, TextChangedEventArgs e) => CargarAutores(TxtBuscarAutor.Text.Trim());
    }
}