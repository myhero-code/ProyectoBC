using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Data.SqlClient;

namespace ProyectoBC
{
    public class TarjetaLibro
    {
        public int IdLibro { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string EditorialAnio { get; set; } = string.Empty;
        public ImageSource? ImagenPortada { get; set; }
        public string EstadoDisponibilidad { get; set; } = "Disponible";
        public string ColorEstado { get; set; } = "#38A169";
        public bool PuedeReservar { get; set; } = true;
        public string TextoBoton { get; set; } = "📌 Reservar (2h para recoger)";
        public string ColorBoton { get; set; } = "#2B6CB0";
    }

    public partial class PortalLectorWindow : Window
    {
        private readonly int _idLector;
        private readonly string _nombreLector;
        private readonly string _ciLector;

        public PortalLectorWindow(int idLector, string nombreCompleto, string ci)
        {
            InitializeComponent();
            _idLector = idLector;
            _nombreLector = nombreCompleto;
            _ciLector = ci;

            TxtNombreLectorHeader.Text = $"Hola, {_nombreLector}";
            TxtCiLectorHeader.Text = $"CI: {_ciLector}";

            LiberarReservasExpiradas();
            CargarCategorias();
            CargarCatalogoTarjetas();
            CargarMisPrestamosYMultas();
        }

        private void LiberarReservasExpiradas()
        {
            try
            {
                ConexionDB.EjecutarComando(
                    "UPDATE dbo.Prestamo SET Estado = 'Expirado' WHERE Estado = 'Reservado' AND Fecha_Limite_Recojo < GETDATE()");
            }
            catch { }
        }

        private void CargarCategorias()
        {
            try
            {
                DataTable dt = ConexionDB.EjecutarConsulta("SELECT ID_Categoria, Nombre_Categoria FROM dbo.Categoria ORDER BY Nombre_Categoria");
                DataRow todas = dt.NewRow();
                todas["ID_Categoria"] = 0;
                todas["Nombre_Categoria"] = "Todas las categorías";
                dt.Rows.InsertAt(todas, 0);

                CmbFiltroCategoria.ItemsSource = dt.DefaultView;
                CmbFiltroCategoria.DisplayMemberPath = "Nombre_Categoria";
                CmbFiltroCategoria.SelectedValuePath = "ID_Categoria";
                CmbFiltroCategoria.SelectedIndex = 0;
            }
            catch { }
        }

        private static ImageSource? CargarImagenDesdeRuta(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                return null;

            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private void CargarCatalogoTarjetas()
        {
            try
            {
                LiberarReservasExpiradas();

                string filtroTexto = TxtBuscarLibro.Text.Trim();
                int idCat = CmbFiltroCategoria.SelectedValue != null ? Convert.ToInt32(CmbFiltroCategoria.SelectedValue) : 0;

                string sql = @"
                    SELECT l.ID_Libro, l.Titulo, l.Portada,
                           ISNULL(l.Editorial, 'Editorial s/d') AS Editorial,
                           ISNULL(CAST(l.Anio_Publicacion AS VARCHAR), 's/f') AS Anio,
                           ISNULL(c.Nombre_Categoria, 'General') AS Categoria,
                           ISNULL(STRING_AGG(a.Nombre_Autor, ', '), 'Autor desconocido') AS Autores,
                           ISNULL((
                               SELECT TOP 1 p.Estado FROM dbo.Prestamo p
                               WHERE p.ID_Libro = l.ID_Libro AND p.Estado IN ('Activo', 'Reservado')
                           ), 'Disponible') AS Disponibilidad
                    FROM dbo.Libro l
                    LEFT JOIN dbo.Categoria c ON l.ID_Categoria = c.ID_Categoria
                    LEFT JOIN dbo.Libro_Autor la ON l.ID_Libro = la.ID_Libro
                    LEFT JOIN dbo.Autor a ON la.ID_Autor = a.ID_Autor
                    WHERE (@cat = 0 OR l.ID_Categoria = @cat)
                    GROUP BY l.ID_Libro, l.Titulo, l.Portada, l.Editorial, l.Anio_Publicacion, c.Nombre_Categoria
                    HAVING (@txt = '' OR l.Titulo LIKE '%' + @txt + '%' OR ISNULL(STRING_AGG(a.Nombre_Autor, ', '), '') LIKE '%' + @txt + '%' OR ISNULL(l.Editorial, '') LIKE '%' + @txt + '%')
                    ORDER BY l.Titulo ASC";

                DataTable dt = ConexionDB.EjecutarConsulta(sql,
                    new SqlParameter("@cat", idCat),
                    new SqlParameter("@txt", filtroTexto));

                var tarjetas = new List<TarjetaLibro>();
                foreach (DataRow row in dt.Rows)
                {
                    string disp = row["Disponibilidad"].ToString() ?? "Disponible";
                    bool libre = disp == "Disponible";
                    string? rutaPortada = row["Portada"] == DBNull.Value ? null : row["Portada"].ToString();

                    tarjetas.Add(new TarjetaLibro
                    {
                        IdLibro = Convert.ToInt32(row["ID_Libro"]),
                        Titulo = row["Titulo"].ToString() ?? "",
                        Autor = "✍️ " + (row["Autores"].ToString() ?? ""),
                        Categoria = row["Categoria"].ToString() ?? "General",
                        EditorialAnio = $"{row["Editorial"]} • {row["Anio"]} • LIB-{row["ID_Libro"]}",
                        ImagenPortada = CargarImagenDesdeRuta(rutaPortada),
                        EstadoDisponibilidad = libre ? "Disponible" : (disp == "Reservado" ? "Reservado" : "Prestado"),
                        ColorEstado = libre ? "#38A169" : (disp == "Reservado" ? "#D69E2E" : "#E53E3E"),
                        PuedeReservar = libre,
                        TextoBoton = libre ? "📌 Reservar (2h para recoger)" : "No disponible",
                        ColorBoton = libre ? "#2B6CB0" : "#CBD5E0"
                    });
                }

                ListaTarjetasLibros.ItemsSource = tarjetas;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catálogo:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReservarLibro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TarjetaLibro libro)
            {
                var confirmar = MessageBox.Show(
                    $"¿Deseas reservar \"{libro.Titulo}\"?\n\nTendrás un límite de 2 HORAS para recogerlo en la Biblioteca Municipal con tu comprobante y CI.",
                    "Confirmar Reserva", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmar != MessageBoxResult.Yes) return;

                try
                {
                    LiberarReservasExpiradas();

                    DataTable ocupado = ConexionDB.EjecutarConsulta(
                        "SELECT ID_Prestamo FROM dbo.Prestamo WHERE ID_Libro = @idLibro AND Estado IN ('Activo', 'Reservado')",
                        new SqlParameter("@idLibro", libro.IdLibro));

                    if (ocupado.Rows.Count > 0)
                    {
                        MessageBox.Show("Lo sentimos, este ejemplar acaba de ser reservado o prestado.", "No disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CargarCatalogoTarjetas();
                        return;
                    }

                    DateTime ahora = DateTime.Now;
                    DateTime limiteRecojo = ahora.AddHours(2);

                    DataTable dtNuevo = ConexionDB.EjecutarConsulta(@"
                        INSERT INTO dbo.Prestamo (ID_Lector, ID_Libro, Fecha_Prestamo, Fecha_Devolucion, Estado, Fecha_Limite_Recojo)
                        OUTPUT INSERTED.ID_Prestamo
                        VALUES (@idLector, @idLibro, CAST(@ahora AS DATE), DATEADD(DAY, 7, CAST(@ahora AS DATE)), 'Reservado', @limite)",
                        new SqlParameter("@idLector", _idLector),
                        new SqlParameter("@idLibro", libro.IdLibro),
                        new SqlParameter("@ahora", ahora),
                        new SqlParameter("@limite", limiteRecojo));

                    int idReserva = Convert.ToInt32(dtNuevo.Rows[0][0]);

                    CargarCatalogoTarjetas();
                    CargarMisPrestamosYMultas();

                    TicketReservaWindow ticket = new TicketReservaWindow(idReserva, _nombreLector, _ciLector, libro.IdLibro, libro.Titulo, ahora, limiteRecojo);
                    ticket.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al procesar la reserva:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnReimprimirTicket_Click(object sender, RoutedEventArgs e)
        {
            if (DgMisPrestamos.SelectedItem is DataRowView fila)
            {
                string estado = fila["Estado"].ToString() ?? "";
                if (estado != "Reservado")
                {
                    MessageBox.Show("Selecciona una fila con estado 'Reservado' para ver o imprimir su ticket.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int idReserva = Convert.ToInt32(fila["ID_Prestamo"]);
                int idLibro = Convert.ToInt32(fila["ID_Libro"]);
                string titulo = fila["Titulo"].ToString() ?? "";
                DateTime limite = Convert.ToDateTime(fila["Fecha_Limite_Recojo"]);

                TicketReservaWindow ticket = new TicketReservaWindow(idReserva, _nombreLector, _ciLector, idLibro, titulo, limite.AddHours(-2), limite);
                ticket.ShowDialog();
            }
            else
            {
                MessageBox.Show("Selecciona una reserva de la tabla primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CargarMisPrestamosYMultas()
        {
            try
            {
                string sqlPrestamos = @"
                    SELECT p.ID_Prestamo, p.ID_Libro, l.Titulo, p.Fecha_Limite_Recojo,
                           CASE WHEN p.Estado = 'Reservado' THEN CONVERT(VARCHAR(16), p.Fecha_Limite_Recojo, 120)
                                ELSE CONVERT(VARCHAR(10), p.Fecha_Devolucion, 103) END AS FechaReferencia,
                           CASE WHEN p.Estado = 'Activo' AND p.Fecha_Devolucion < CAST(GETDATE() AS DATE)
                                THEN 'Atrasado' ELSE p.Estado END AS Estado
                    FROM dbo.Prestamo p
                    INNER JOIN dbo.Libro l ON p.ID_Libro = l.ID_Libro
                    WHERE p.ID_Lector = @id
                    ORDER BY p.ID_Prestamo DESC";

                DgMisPrestamos.ItemsSource = ConexionDB.EjecutarConsulta(sqlPrestamos,
                    new SqlParameter("@id", _idLector)).DefaultView;

                string sqlMultas = @"
                    SELECT m.Fecha_Emision, m.Monto,
                           CASE WHEN ISNULL(m.Pagado, 0) >= m.Monto THEN 'Pagada' ELSE 'Pendiente en caja' END AS EstadoPago
                    FROM dbo.Multa m
                    INNER JOIN dbo.Prestamo p ON m.ID_Prestamo = p.ID_Prestamo
                    WHERE p.ID_Lector = @id
                    ORDER BY m.ID_Multa DESC";

                DgMisMultas.ItemsSource = ConexionDB.EjecutarConsulta(sqlMultas,
                    new SqlParameter("@id", _idLector)).DefaultView;
            }
            catch { }
        }

        private void FiltroCatalogo_Changed(object sender, EventArgs e)
        {
            if (PlaceholderLibro != null)
                PlaceholderLibro.Visibility = string.IsNullOrEmpty(TxtBuscarLibro.Text) ? Visibility.Visible : Visibility.Collapsed;

            CargarCatalogoTarjetas();
        }

        private void BtnNavCatalogo_Click(object sender, RoutedEventArgs e)
        {
            VistaCatalogo.Visibility = Visibility.Visible;
            VistaMisPrestamos.Visibility = Visibility.Collapsed;
            BtnNavCatalogo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B6CB0"));
            BtnNavCatalogo.Foreground = Brushes.White;
            BtnNavMisPrestamos.Background = Brushes.Transparent;
            BtnNavMisPrestamos.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E0"));
        }

        private void BtnNavMisPrestamos_Click(object sender, RoutedEventArgs e)
        {
            VistaCatalogo.Visibility = Visibility.Collapsed;
            VistaMisPrestamos.Visibility = Visibility.Visible;
            BtnNavMisPrestamos.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B6CB0"));
            BtnNavMisPrestamos.Foreground = Brushes.White;
            BtnNavCatalogo.Background = Brushes.Transparent;
            BtnNavCatalogo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E0"));
            CargarMisPrestamosYMultas();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
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