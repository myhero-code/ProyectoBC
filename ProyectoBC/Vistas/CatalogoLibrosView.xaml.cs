using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace ProyectoBC.Vistas
{
    public class AutorSeleccionadoItem
    {
        public int IdAutor { get; set; }
        public string NombreAutor { get; set; } = string.Empty;
    }

    public partial class CatalogoLibrosView : UserControl
    {
        private int? _idLibroSeleccionado = null;
        private string? _rutaPortadaActual = null;
        private readonly ObservableCollection<AutorSeleccionadoItem> _autoresDelLibro = new ObservableCollection<AutorSeleccionadoItem>();

        public CatalogoLibrosView()
        {
            InitializeComponent();
            LstAutoresSeleccionados.ItemsSource = _autoresDelLibro;
            CargarCategorias();
            CargarAutoresDisponibles();
            CargarLibros();
        }

        private void CargarCategorias()
        {
            try
            {
                DataTable dt = ConexionDB.EjecutarConsulta("SELECT ID_Categoria, Nombre_Categoria FROM dbo.Categoria ORDER BY Nombre_Categoria");
                CmbCategoria.ItemsSource = dt.DefaultView;
                CmbCategoria.DisplayMemberPath = "Nombre_Categoria";
                CmbCategoria.SelectedValuePath = "ID_Categoria";
            }
            catch { }
        }

        private void CargarAutoresDisponibles()
        {
            try
            {
                DataTable dt = ConexionDB.EjecutarConsulta("SELECT ID_Autor, Nombre_Autor FROM dbo.Autor ORDER BY Nombre_Autor");
                CmbAutoresDisponibles.ItemsSource = dt.DefaultView;
                CmbAutoresDisponibles.DisplayMemberPath = "Nombre_Autor";
                CmbAutoresDisponibles.SelectedValuePath = "ID_Autor";
            }
            catch { }
        }

        private void CargarLibros(string filtro = "")
        {
            try
            {
                string sql = @"
                    SELECT l.ID_Libro, l.Titulo, l.Editorial, l.Anio_Publicacion, l.Genero, l.ID_Categoria, l.Portada,
                           ISNULL(c.Nombre_Categoria, 'Sin categoría') AS Nombre_Categoria,
                           ISNULL(STRING_AGG(a.Nombre_Autor, ', '), 'Sin autor') AS Autores
                    FROM dbo.Libro l
                    LEFT JOIN dbo.Categoria c ON l.ID_Categoria = c.ID_Categoria
                    LEFT JOIN dbo.Libro_Autor la ON l.ID_Libro = la.ID_Libro
                    LEFT JOIN dbo.Autor a ON la.ID_Autor = a.ID_Autor
                    GROUP BY l.ID_Libro, l.Titulo, l.Editorial, l.Anio_Publicacion, l.Genero, l.ID_Categoria, l.Portada, c.Nombre_Categoria
                    HAVING (@filtro = '' OR l.Titulo LIKE '%' + @filtro + '%' OR ISNULL(STRING_AGG(a.Nombre_Autor, ', '), '') LIKE '%' + @filtro + '%' OR ISNULL(l.Editorial, '') LIKE '%' + @filtro + '%')
                    ORDER BY l.ID_Libro DESC";

                DgLibros.ItemsSource = ConexionDB.EjecutarConsulta(sql, new SqlParameter("@filtro", filtro)).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar libros:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSeleccionarImagen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Seleccionar portada del libro",
                Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string carpetaPortadas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Portadas");
                    Directory.CreateDirectory(carpetaPortadas);

                    string nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(dlg.FileName)}";
                    string rutaDestino = Path.Combine(carpetaPortadas, nombreArchivo);
                    File.Copy(dlg.FileName, rutaDestino, true);

                    _rutaPortadaActual = rutaDestino;
                    TxtRutaImagen.Text = Path.GetFileName(dlg.FileName);
                    MostrarPreviewImagen(rutaDestino);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo cargar la imagen:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MostrarPreviewImagen(string? ruta)
        {
            if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta))
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.EndInit();
                ImgPortadaPreview.Source = bmp;
            }
            else
            {
                ImgPortadaPreview.Source = null;
            }
        }

        private void BtnAgregarAutorALista_Click(object sender, RoutedEventArgs e)
        {
            if (CmbAutoresDisponibles.SelectedItem is DataRowView filaAutor)
            {
                int idAutor = Convert.ToInt32(filaAutor["ID_Autor"]);
                string nombreAutor = filaAutor["Nombre_Autor"].ToString() ?? "";

                if (_autoresDelLibro.Any(a => a.IdAutor == idAutor))
                {
                    MessageBox.Show("Ese autor ya está agregado a este libro.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _autoresDelLibro.Add(new AutorSeleccionadoItem { IdAutor = idAutor, NombreAutor = nombreAutor });
            }
            else
            {
                MessageBox.Show("Seleccione un autor de la lista (si aún no existe, créelo en la sección Autores del menú lateral).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnQuitarAutorDeLista_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AutorSeleccionadoItem item)
            {
                _autoresDelLibro.Remove(item);
            }
        }

        private void BtnGuardarLibro_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitulo.Text))
            {
                MessageBox.Show("El título del libro es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_autoresDelLibro.Count == 0)
            {
                MessageBox.Show("Agregue al menos un autor para el libro.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? anio = int.TryParse(TxtAnio.Text.Trim(), out int a) ? a : null;
            object idCat = CmbCategoria.SelectedValue ?? DBNull.Value;
            object portadaVal = string.IsNullOrEmpty(_rutaPortadaActual) ? DBNull.Value : _rutaPortadaActual;

            try
            {
                int idLibroFinal;

                if (_idLibroSeleccionado == null)
                {
                    DataTable dt = ConexionDB.EjecutarConsulta(@"
                        INSERT INTO dbo.Libro (Titulo, Editorial, Anio_Publicacion, Genero, ID_Categoria, Portada)
                        OUTPUT INSERTED.ID_Libro
                        VALUES (@titulo, @editorial, @anio, @genero, @idCat, @portada)",
                        new SqlParameter("@titulo", TxtTitulo.Text.Trim()),
                        new SqlParameter("@editorial", string.IsNullOrWhiteSpace(TxtEditorial.Text) ? DBNull.Value : TxtEditorial.Text.Trim()),
                        new SqlParameter("@anio", (object?)anio ?? DBNull.Value),
                        new SqlParameter("@genero", string.IsNullOrWhiteSpace(TxtGenero.Text) ? DBNull.Value : TxtGenero.Text.Trim()),
                        new SqlParameter("@idCat", idCat),
                        new SqlParameter("@portada", portadaVal));

                    idLibroFinal = Convert.ToInt32(dt.Rows[0][0]);
                }
                else
                {
                    idLibroFinal = _idLibroSeleccionado.Value;
                    ConexionDB.EjecutarComando(@"
                        UPDATE dbo.Libro
                        SET Titulo = @titulo, Editorial = @editorial, Anio_Publicacion = @anio, Genero = @genero, ID_Categoria = @idCat, Portada = @portada
                        WHERE ID_Libro = @id",
                        new SqlParameter("@titulo", TxtTitulo.Text.Trim()),
                        new SqlParameter("@editorial", string.IsNullOrWhiteSpace(TxtEditorial.Text) ? DBNull.Value : TxtEditorial.Text.Trim()),
                        new SqlParameter("@anio", (object?)anio ?? DBNull.Value),
                        new SqlParameter("@genero", string.IsNullOrWhiteSpace(TxtGenero.Text) ? DBNull.Value : TxtGenero.Text.Trim()),
                        new SqlParameter("@idCat", idCat),
                        new SqlParameter("@portada", portadaVal),
                        new SqlParameter("@id", idLibroFinal));

                    ConexionDB.EjecutarComando("DELETE FROM dbo.Libro_Autor WHERE ID_Libro = @id", new SqlParameter("@id", idLibroFinal));
                }

                foreach (var autor in _autoresDelLibro)
                {
                    ConexionDB.EjecutarComando("INSERT INTO dbo.Libro_Autor (ID_Libro, ID_Autor) VALUES (@idL, @idA)",
                        new SqlParameter("@idL", idLibroFinal),
                        new SqlParameter("@idA", autor.IdAutor));
                }

                MessageBox.Show("Libro guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                CargarLibros(TxtBuscarLibro.Text.Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar libro:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                CmbCategoria.SelectedValue = row["ID_Categoria"] == DBNull.Value ? null : row["ID_Categoria"];

                _rutaPortadaActual = row["Portada"] == DBNull.Value ? null : row["Portada"].ToString();
                TxtRutaImagen.Text = string.IsNullOrEmpty(_rutaPortadaActual) ? "Sin imagen seleccionada" : Path.GetFileName(_rutaPortadaActual);
                MostrarPreviewImagen(_rutaPortadaActual);

                _autoresDelLibro.Clear();
                DataTable dtAutores = ConexionDB.EjecutarConsulta(@"
                    SELECT a.ID_Autor, a.Nombre_Autor
                    FROM dbo.Libro_Autor la
                    INNER JOIN dbo.Autor a ON la.ID_Autor = a.ID_Autor
                    WHERE la.ID_Libro = @id", new SqlParameter("@id", _idLibroSeleccionado.Value));

                foreach (DataRow r in dtAutores.Rows)
                {
                    _autoresDelLibro.Add(new AutorSeleccionadoItem
                    {
                        IdAutor = Convert.ToInt32(r["ID_Autor"]),
                        NombreAutor = r["Nombre_Autor"].ToString() ?? ""
                    });
                }

                BtnGuardarLibro.Content = "Actualizar Libro";
            }
        }

        private void BtnEliminarLibro_Click(object sender, RoutedEventArgs e)
        {
            if (_idLibroSeleccionado == null)
            {
                MessageBox.Show("Seleccione un libro de la tabla para eliminar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("¿Seguro que desea eliminar este libro?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    ConexionDB.EjecutarComando("DELETE FROM dbo.Libro_Autor WHERE ID_Libro = @id", new SqlParameter("@id", _idLibroSeleccionado.Value));
                    ConexionDB.EjecutarComando("DELETE FROM dbo.Libro WHERE ID_Libro = @id", new SqlParameter("@id", _idLibroSeleccionado.Value));
                    LimpiarFormulario();
                    CargarLibros(TxtBuscarLibro.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se puede eliminar (puede tener préstamos asociados):\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            _idLibroSeleccionado = null;
            _rutaPortadaActual = null;
            TxtTitulo.Clear();
            TxtEditorial.Clear();
            TxtAnio.Clear();
            TxtGenero.Clear();
            CmbCategoria.SelectedIndex = -1;
            CmbAutoresDisponibles.SelectedIndex = -1;
            _autoresDelLibro.Clear();
            ImgPortadaPreview.Source = null;
            TxtRutaImagen.Text = "Sin imagen seleccionada";
            BtnGuardarLibro.Content = "Guardar Libro";
            DgLibros.SelectedIndex = -1;
        }

        private void TxtBuscarLibro_TextChanged(object sender, TextChangedEventArgs e) => CargarLibros(TxtBuscarLibro.Text.Trim());

        private void BtnImprimirReporte_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable dtReporte = ObtenerDataTableCatalogo();

                if (dtReporte == null || dtReporte.Rows.Count == 0)
                {
                    MessageBox.Show("No hay libros para imprimir en el catálogo.", "Aviso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ReporteHelper.ImprimirDataTable(
                    dtReporte,
                    "Catálogo de Libros - Biblioteca Municipal",
                    $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el reporte: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable ObtenerDataTableCatalogo()
        {
            DataTable dt = new DataTable("CatalogoLibros");
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("Título", typeof(string));
            dt.Columns.Add("Autores", typeof(string));
            dt.Columns.Add("Categoría", typeof(string));
            dt.Columns.Add("Año", typeof(string));

            if (DgLibros?.Items == null)
                return dt;

            foreach (var item in DgLibros.Items)
            {
                if (item == null || item == CollectionView.NewItemPlaceholder)
                    continue;

                if (item is DataRowView rowView)
                {
                    dt.Rows.Add(
                        rowView["ID_Libro"]?.ToString() ?? "",
                        rowView["Titulo"]?.ToString() ?? "",
                        rowView["Autores"]?.ToString() ?? "",
                        rowView["Nombre_Categoria"]?.ToString() ?? "",
                        rowView["Anio_Publicacion"]?.ToString() ?? ""
                    );
                }
                else
                {
                    var tipo = item.GetType();
                    string LeerProp(params string[] nombres)
                    {
                        foreach (var n in nombres)
                        {
                            var prop = tipo.GetProperty(n);
                            if (prop != null)
                                return prop.GetValue(item)?.ToString() ?? "";
                        }
                        return "";
                    }

                    dt.Rows.Add(
                        LeerProp("ID_Libro", "Id_Libro", "ID"),
                        LeerProp("Titulo", "Título"),
                        LeerProp("Autores"),
                        LeerProp("Nombre_Categoria", "Categoria"),
                        LeerProp("Anio_Publicacion", "Anio", "Año")
                    );
                }
            }

            return dt;
        }
    }
}