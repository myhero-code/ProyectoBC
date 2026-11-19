using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace ProyectoBC.Vistas
{
    public partial class ReportesView : UserControl
    {
        public ReportesView()
        {
            InitializeComponent();
            CargarReportes();
        }

        private void CargarReportes()
        {
            try
            {
                string sqlMultas = @"
                    SELECT m.ID_Multa, l.Nombre + ' ' + l.Apellido AS Lector, m.Fecha_Emision, m.Monto, m.Pagado
                    FROM dbo.Multa m
                    INNER JOIN dbo.Prestamo p ON m.ID_Prestamo = p.ID_Prestamo
                    INNER JOIN dbo.Lector l ON p.ID_Lector = l.ID_Lector
                    ORDER BY m.ID_Multa DESC";
                DgMultas.ItemsSource = ConexionDB.EjecutarConsulta(sqlMultas).DefaultView;

                string sqlTop = @"
                    SELECT TOP 15 b.Titulo, ISNULL(c.Nombre_Categoria, 'General') AS Nombre_Categoria, COUNT(p.ID_Prestamo) AS TotalPrestamos
                    FROM dbo.Prestamo p
                    INNER JOIN dbo.Libro b ON p.ID_Libro = b.ID_Libro
                    LEFT JOIN dbo.Categoria c ON b.ID_Categoria = c.ID_Categoria
                    GROUP BY b.Titulo, c.Nombre_Categoria
                    ORDER BY TotalPrestamos DESC";
                DgTopLibros.ItemsSource = ConexionDB.EjecutarConsulta(sqlTop).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar reportes:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPagarMulta_Click(object sender, RoutedEventArgs e)
        {
            if (DgMultas.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Seleccione una multa de la lista.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int idMulta = Convert.ToInt32(row["ID_Multa"]);
            decimal monto = Convert.ToDecimal(row["Monto"]);

            try
            {
                ConexionDB.EjecutarComando("UPDATE dbo.Multa SET Pagado = @monto WHERE ID_Multa = @id",
                    new SqlParameter("@monto", monto),
                    new SqlParameter("@id", idMulta));

                MessageBox.Show("Multa marcada como pagada en su totalidad.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarReportes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar el pago:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}