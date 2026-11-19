using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace ProyectoBC.Vistas
{
    public partial class CatalogoLibrosView : UserControl
    {
        private int? _idLibroSeleccionado = null;

        public CatalogoLibrosView()
        {
            InitializeComponent();
            CargarCombos();
            CargarLibros();
        }

        private void CargarCombos()
        {
            try
            {
                CmbCategoria.ItemsSource = ConexionDB.EjecutarConsulta("SELECT ID_Categoria, Nombre_Categoria FROM dbo.Categoria ORDER BY Nombre_Categoria").DefaultView;
                CmbAutor.ItemsSource = ConexionDB.EjecutarConsulta("SELECT ID_Autor, Nombre_Autor FROM dbo.Autor ORDER BY Nombre_Autor").DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar listas:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarLibros(string filtro = "")
        {
            try
            {
                string sql = @"
                    SELECT l.ID_Libro, l.Titulo, l.Editorial, l.Anio_Publicacion, l.Genero, l.ID_Categoria,
                           ISNULL(a.Nombre_Autor, 'Sin autor') AS Nombre_Autor, la.ID_Autor,
                           ISNULL(c.Nombre_Categoria, 'General') AS Nombre_Categoria
                    FROM dbo.Libro l
                    LEFT JOIN dbo.Categoria c ON l.ID_Categoria = c.ID_Categoria
                    LEFT JOIN dbo.Libro_Autor la ON l.ID_Libro = la.ID_Libro
                    LEFT JOIN dbo.Autor a ON la.ID_Autor = a.ID_Autor
                    WHERE (@filtro = '' OR l.Titulo LIKE '%' + @filtro + '%' OR a.Nombre_Autor LIKE '%' + @filtro + '%' OR l.Editorial LIKE '%' + @filtro + '%')
                    ORDER BY l.ID_Libro DESC";

                DgLibros.ItemsSource = ConexionDB.EjecutarConsulta(sql, new SqlParameter("@filtro", filtro)).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar libros:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitulo.Text))
            {
                MessageBox.Show("El título del libro es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection cn = ConexionDB.ObtenerConexion())
                {
                    cn.Open();

                    // Obtener o crear el autor si escribió uno nuevo en el ComboBox
                    int? idAutor = CmbAutor.SelectedValue != null ? Convert.ToInt32(CmbAutor.SelectedValue) : null;
                    if (idAutor == null && !string.IsNullOrWhiteSpace(CmbAutor.Text))
                    {
                        string sqlNuevoAutor = "INSERT INTO dbo.Autor (Nombre_Autor) OUTPUT INSERTED.ID_Autor VALUES (@nombre)";
                        using (SqlCommand cmdA = new SqlCommand(sqlNuevoAutor, cn))
                        {
                            cmdA.Parameters.AddWithValue("@nombre", CmbAutor.Text.Trim());
                            idAutor = Convert.ToInt32(cmdA.ExecuteScalar());
                        }
                    }

                    object idCat = CmbCategoria.SelectedValue ?? DBNull.Value;
                    object anio = int.TryParse(TxtAnio.Text, out int a) ? a : DBNull.Value;

                    if (_idLibroSeleccionado == null)
                    {
                        string sqlInsert = @"
                            INSERT INTO dbo.Libro (Titulo, Editorial, Anio_Publicacion, Genero, ID_Categoria)
                            OUTPUT INSERTED.ID_Libro
                            VALUES (@titulo, @editorial, @anio, @genero, @idCat)";

                        int nuevoIdLibro;
                        using (SqlCommand cmd = new SqlCommand(sqlInsert, cn))
                        {
                            cmd.Parameters.AddWithValue("@titulo", TxtTitulo.Text.Trim());
                            cmd.Parameters.AddWithValue("@editorial", (object)TxtEditorial.Text.Trim() ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@anio", anio);
                            cmd.Parameters.AddWithValue("@genero", (object)TxtGenero.Text.Trim() ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@idCat", idCat);
                            nuevoIdLibro = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (idAutor != null)
                        {
                            using (SqlCommand cmdLA = new SqlCommand("INSERT INTO dbo.Libro_Autor (ID_Libro, ID_Autor) VALUES (@idL, @idA)", cn))
                            {
                                cmdLA.Parameters.AddWithValue("@idL", nuevoIdLibro);
                                cmdLA.Parameters.AddWithValue("@idA", idAutor.Value);
                                cmdLA.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Libro registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string sqlUpdate = @"
                            UPDATE dbo.Libro 
                            SET Titulo = @titulo, Editorial = @editorial, Anio_Publicacion = @anio, Genero = @genero, ID_Categoria = @idCat
                            WHERE ID_Libro = @id";

                        using (SqlCommand cmd = new SqlCommand(sqlUpdate, cn))
                        {
                            cmd.Parameters.AddWithValue("@id", _idLibroSeleccionado.Value);
                            cmd.Parameters.AddWithValue("@titulo", TxtTitulo.Text.Trim());
                            cmd.Parameters.AddWithValue("@editorial", (object)TxtEditorial.Text.Trim() ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@anio", anio);
                            cmd.Parameters.AddWithValue("@genero", (object)TxtGenero.Text.Trim() ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@idCat", idCat);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmdDel = new SqlCommand("DELETE FROM dbo.Libro_Autor WHERE ID_Libro = @idL", cn))
                        {
                            cmdDel.Parameters.AddWithValue("@idL", _idLibroSeleccionado.Value);
                            cmdDel.ExecuteNonQuery();
                        }

                        if (idAutor != null)
                        {
                            using (SqlCommand cmdLA = new SqlCommand("INSERT INTO dbo.Libro_Autor (ID_Libro, ID_Autor) VALUES (@idL, @idA)", cn))
                            {
                                cmdLA.Parameters.AddWithValue("@idL", _idLibroSeleccionado.Value);
                                cmdLA.Parameters.AddWithValue("@idA", idAutor.Value);
                                cmdLA.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Libro actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                LimpiarFormulario();
                CargarCombos();
                CargarLibros();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el libro:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_idLibroSeleccionado == null)
            {
                MessageBox.Show("Seleccione un libro de la tabla para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("¿Seguro que desea eliminar este libro?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    ConexionDB.EjecutarComando("DELETE FROM dbo.Libro_Autor WHERE ID_Libro = @id", new SqlParameter("@id", _idLibroSeleccionado.Value));
                    ConexionDB.EjecutarComando("DELETE FROM dbo.Libro WHERE ID_Libro = @id", new SqlParameter("@id", _idLibroSeleccionado.Value));
                    LimpiarFormulario();
                    CargarLibros();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se puede eliminar el libro (puede tener préstamos asociados):\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgLibros.SelectedItem is DataRowView row)
            {
                _idLibroSeleccionado = Convert.ToInt32(row["ID_Libro"]);
                TxtTitulo.Text = row["Titulo"].ToString();
                TxtEditorial.Text = row["Editorial"].ToString();
                TxtAnio.Text = row["Anio_Publicacion"].ToString();
                TxtGenero.Text = row["Genero"].ToString();
                CmbCategoria.SelectedValue = row["ID_Categoria"] != DBNull.Value ? row["ID_Categoria"] : null;
                CmbAutor.SelectedValue = row["ID_Autor"] != DBNull.Value ? row["ID_Autor"] : null;
                BtnGuardar.Content = "Actualizar Libro";
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            _idLibroSeleccionado = null;
            TxtTitulo.Clear();
            TxtEditorial.Clear();
            TxtAnio.Clear();
            TxtGenero.Clear();
            CmbCategoria.SelectedIndex = -1;
            CmbAutor.SelectedIndex = -1;
            CmbAutor.Text = string.Empty;
            BtnGuardar.Content = "Guardar Libro";
            DgLibros.SelectedIndex = -1;
        }

        private void TxtBuscarLibro_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarLibros(TxtBuscarLibro.Text.Trim());
        }
    }
}