using Microsoft.Data.SqlClient;
using ProyectoBC.Vistas;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ProyectoBC
{
    public class PrestamoResumen
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Libro { get; set; } = string.Empty;
        public string FechaPrestamo { get; set; } = string.Empty;
        public string FechaDevolucion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public partial class MenuPrincipal : Window
    {
        public MenuPrincipal()
        {
            InitializeComponent();
            CargarDatosDashboard();
        }

        public void CargarDatosDashboard(string filtro = "")
        {
            try
            {
                using (SqlConnection cn = ConexionDB.ObtenerConexion())
                {
                    cn.Open();

                    // 1. Total de libros
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Libro", cn))
                    {
                        TxtTotalLibros.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString("N0");
                    }

                    // 2. Préstamos activos
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Prestamo WHERE Estado = 'Activo'", cn))
                    {
                        TxtPrestamosActivos.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString("N0");
                    }

                    // 3. Usuarios / Lectores registrados
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Lector", cn))
                    {
                        TxtTotalLectores.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString("N0");
                    }

                    // 4. Devoluciones atrasadas
                    string sqlAtrasados = "SELECT COUNT(*) FROM dbo.Prestamo WHERE Estado = 'Activo' AND Fecha_Devolucion < CAST(GETDATE() AS DATE)";
                    using (SqlCommand cmd = new SqlCommand(sqlAtrasados, cn))
                    {
                        TxtAtrasados.Text = Convert.ToInt32(cmd.ExecuteScalar()).ToString("N0");
                    }

                    // 5. Últimos préstamos registrados
                    string sqlTabla = @"
                        SELECT TOP 20 
                            p.ID_Prestamo,
                            l.Nombre + ' ' + l.Apellido + ' (' + l.DNI_CI + ')' AS LectorInfo,
                            b.Titulo,
                            p.Fecha_Prestamo,
                            p.Fecha_Devolucion,
                            CASE 
                                WHEN p.Estado = 'Activo' AND p.Fecha_Devolucion < CAST(GETDATE() AS DATE) THEN 'Atrasado'
                                ELSE ISNULL(p.Estado, 'Activo')
                            END AS EstadoReal
                        FROM dbo.Prestamo p
                        INNER JOIN dbo.Lector l ON p.ID_Lector = l.ID_Lector
                        INNER JOIN dbo.Libro b ON p.ID_Libro = b.ID_Libro
                        WHERE (@filtro = '' 
                               OR l.Nombre LIKE '%' + @filtro + '%' 
                               OR l.Apellido LIKE '%' + @filtro + '%' 
                               OR l.DNI_CI LIKE '%' + @filtro + '%' 
                               OR b.Titulo LIKE '%' + @filtro + '%')
                        ORDER BY p.ID_Prestamo DESC";

                    List<PrestamoResumen> listaPrestamos = new List<PrestamoResumen>();

                    using (SqlCommand cmd = new SqlCommand(sqlTabla, cn))
                    {
                        cmd.Parameters.AddWithValue("@filtro", filtro);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                listaPrestamos.Add(new PrestamoResumen
                                {
                                    Id = Convert.ToInt32(dr["ID_Prestamo"]),
                                    Usuario = dr["LectorInfo"].ToString() ?? "",
                                    Libro = dr["Titulo"].ToString() ?? "",
                                    FechaPrestamo = dr["Fecha_Prestamo"] != DBNull.Value
                                        ? Convert.ToDateTime(dr["Fecha_Prestamo"]).ToString("dd/MM/yyyy") : "-",
                                    FechaDevolucion = dr["Fecha_Devolucion"] != DBNull.Value
                                        ? Convert.ToDateTime(dr["Fecha_Devolucion"]).ToString("dd/MM/yyyy") : "-",
                                    Estado = dr["EstadoReal"].ToString() ?? "Activo"
                                });
                            }
                        }
                    }

                    dgPrestamos.ItemsSource = listaPrestamos;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos del panel:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderBuscar.Visibility = string.IsNullOrEmpty(TxtBuscar.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (PanelInicio.Visibility == Visibility.Visible)
            {
                CargarDatosDashboard(TxtBuscar.Text.Trim());
            }
        }

        private void BtnNavegacion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botonPresionado)
            {
                ActivarBoton(botonPresionado);

                if (botonPresionado.Name == "BtnPanelPrincipal")
                {
                    ContenedorVistas.Content = null;
                    ContenedorVistas.Visibility = Visibility.Collapsed;
                    PanelInicio.Visibility = Visibility.Visible;
                    CargarDatosDashboard(TxtBuscar.Text.Trim());
                }
                else
                {
                    PanelInicio.Visibility = Visibility.Collapsed;
                    ContenedorVistas.Visibility = Visibility.Visible;

                    switch (botonPresionado.Name)
                    {
                        case "BtnCatalogoLibros":
                            ContenedorVistas.Content = new CatalogoLibrosView();
                            break;
                        case "BtnAutores":
                            ContenedorVistas.Content = new AutoresView();
                            break;
                        case "BtnPrestamos":
                            ContenedorVistas.Content = new PrestamosView();
                            break;
                        case "BtnUsuarios":
                            ContenedorVistas.Content = new UsuariosView();
                            break;
                        case "BtnReportes":
                            ContenedorVistas.Content = new ReportesView();
                            break;
                        case "BtnConfiguracion":
                            ContenedorVistas.Content = new ConfiguracionView();
                            break;
                    }
                }
            }
        }

        private void ActivarBoton(Button botonActivo)
        {
            foreach (var child in PanelBotonesMenu.Children)
            {
                if (child is Button btn)
                {
                    btn.Style = (Style)FindResource("SidebarButtonStyle");
                }
            }
            botonActivo.Style = (Style)FindResource("ActiveSidebarButtonStyle");
        }

        private void BtnNuevoPrestamoRapido_Click(object sender, RoutedEventArgs e)
        {
            ActivarBoton(BtnPrestamos);
            PanelInicio.Visibility = Visibility.Collapsed;
            ContenedorVistas.Visibility = Visibility.Visible;
            ContenedorVistas.Content = new PrestamosView();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            if (this.WindowState == WindowState.Maximized)
            {
                loginWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                loginWindow.WindowState = WindowState.Normal;
                loginWindow.Width = this.Width;
                loginWindow.Height = this.Height;
                loginWindow.Left = this.Left;
                loginWindow.Top = this.Top;
            }

            loginWindow.Show();
            this.Close();
        }
    }
}