using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace ProyectoBC.Vistas
{
    public partial class PrestamosView : UserControl
    {
        public PrestamosView()
        {
            InitializeComponent();
            DpFechaPrestamo.SelectedDate = DateTime.Today;
            DpFechaDevolucion.SelectedDate = DateTime.Today.AddDays(7);
            CargarCombos();
            CargarPrestamos();
        }

        private void CargarCombos()
        {
            try
            {
                string sqlLectores = "SELECT ID_Lector, DNI_CI + ' - ' + Nombre + ' ' + Apellido AS LectorCompleto FROM dbo.Lector ORDER BY Nombre";
                CmbLector.ItemsSource = ConexionDB.EjecutarConsulta(sqlLectores).DefaultView;

                string sqlLibros = "SELECT ID_Libro, Titulo FROM dbo.Libro ORDER BY Titulo";
                CmbLibro.ItemsSource = ConexionDB.EjecutarConsulta(sqlLibros).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarPrestamos()
        {
            if (DgPrestamos == null) return;
            try
            {
                string estado = (CmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
                string sql = @"
                    SELECT p.ID_Prestamo, l.Nombre + ' ' + l.Apellido + ' (' + l.DNI_CI + ')' AS Lector,
                           b.Titulo, p.Fecha_Prestamo, p.Fecha_Devolucion, p.Estado
                    FROM dbo.Prestamo p
                    INNER JOIN dbo.Lector l ON p.ID_Lector = l.ID_Lector
                    INNER JOIN dbo.Libro b ON p.ID_Libro = b.ID_Libro
                    WHERE (@estado = 'Todos' OR p.Estado = @estado)
                    ORDER BY p.ID_Prestamo DESC";

                DgPrestamos.ItemsSource = ConexionDB.EjecutarConsulta(sql, new SqlParameter("@estado", estado)).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar préstamos:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegistrarPrestamo_Click(object sender, RoutedEventArgs e)
        {
            if (CmbLector.SelectedValue == null || CmbLibro.SelectedValue == null || DpFechaDevolucion.SelectedDate == null)
            {
                MessageBox.Show("Seleccione un lector, un libro y la fecha de devolución.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string sql = @"
                    INSERT INTO dbo.Prestamo (ID_Lector, ID_Libro, Fecha_Prestamo, Fecha_Devolucion, Estado)
                    VALUES (@idLector, @idLibro, @fPrestamo, @fDevolucion, 'Activo')";

                ConexionDB.EjecutarComando(sql,
                    new SqlParameter("@idLector", Convert.ToInt32(CmbLector.SelectedValue)),
                    new SqlParameter("@idLibro", Convert.ToInt32(CmbLibro.SelectedValue)),
                    new SqlParameter("@fPrestamo", DpFechaPrestamo.SelectedDate ?? DateTime.Today),
                    new SqlParameter("@fDevolucion", DpFechaDevolucion.SelectedDate.Value));

                MessageBox.Show("Préstamo registrado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                CmbLector.SelectedIndex = -1;
                CmbLibro.SelectedIndex = -1;
                CargarPrestamos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar préstamo:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDevolver_Click(object sender, RoutedEventArgs e)
        {
            if (DgPrestamos.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Seleccione un préstamo de la tabla.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (row["Estado"].ToString() == "Devuelto")
            {
                MessageBox.Show("Este libro ya fue devuelto anteriormente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int idPrestamo = Convert.ToInt32(row["ID_Prestamo"]);
            DateTime fechaLimite = Convert.ToDateTime(row["Fecha_Devolucion"]);

            try
            {
                ConexionDB.EjecutarComando("UPDATE dbo.Prestamo SET Estado = 'Devuelto' WHERE ID_Prestamo = @id",
                    new SqlParameter("@id", idPrestamo));

                // Verificar si hubo retraso para registrar multa automática (5 Bs por día)
                int diasRetraso = (DateTime.Today - fechaLimite.Date).Days;
                if (diasRetraso > 0)
                {
                    decimal montoMulta = diasRetraso * 5.00m;
                    string sqlMulta = @"
                        INSERT INTO dbo.Multa (ID_Prestamo, Fecha_Emision, Monto, Pagado)
                        VALUES (@idP, CAST(GETDATE() AS DATE), @monto, 0)";
                    ConexionDB.EjecutarComando(sqlMulta,
                        new SqlParameter("@idP", idPrestamo),
                        new SqlParameter("@monto", montoMulta));

                    MessageBox.Show($"Libro devuelto con {diasRetraso} día(s) de retraso.\nSe generó una multa de {montoMulta:N2} Bs.",
                        "Devolución con Multa", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Devolución registrada a tiempo.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CargarPrestamos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar devolución:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e) => CargarPrestamos();
    }
}