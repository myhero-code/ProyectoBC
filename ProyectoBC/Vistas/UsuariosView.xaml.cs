using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.SqlClient;

namespace ProyectoBC.Vistas
{
    public partial class UsuariosView : UserControl
    {
        private int? _idLectorSeleccionado = null;

        public UsuariosView()
        {
            InitializeComponent();
            DpFechaInicio.SelectedDate = DateTime.Today;
            CargarLectores();
        }

        private void CargarLectores(string filtro = "")
        {
            try
            {
                string sql = @"
                    SELECT ID_Lector, DNI_CI, Nombre, Apellido, Telefono, Fecha_Inicio
                    FROM dbo.Lector
                    WHERE (@filtro = '' OR DNI_CI LIKE '%' + @filtro + '%' OR Nombre LIKE '%' + @filtro + '%' OR Apellido LIKE '%' + @filtro + '%')
                    ORDER BY ID_Lector DESC";

                DgLectores.ItemsSource = ConexionDB.EjecutarConsulta(sql, new SqlParameter("@filtro", filtro)).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar lectores:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuardarLector_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCI.Text) || string.IsNullOrWhiteSpace(TxtNombre.Text) || string.IsNullOrWhiteSpace(TxtApellido.Text))
            {
                MessageBox.Show("CI, Nombre y Apellido son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SqlParameter[] parametros =
                {
                    new SqlParameter("@ci", TxtCI.Text.Trim()),
                    new SqlParameter("@nombre", TxtNombre.Text.Trim()),
                    new SqlParameter("@apellido", TxtApellido.Text.Trim()),
                    new SqlParameter("@telefono", string.IsNullOrWhiteSpace(TxtTelefono.Text) ? DBNull.Value : TxtTelefono.Text.Trim()),
                    new SqlParameter("@fecha", DpFechaInicio.SelectedDate ?? DateTime.Today)
                };
                if (_idLectorSeleccionado == null)
                {
                    string sql = @"
                        INSERT INTO dbo.Lector (DNI_CI, Nombre, Apellido, Telefono, Fecha_Inicio)
                        VALUES (@ci, @nombre, @apellido, @telefono, @fecha)";
                    ConexionDB.EjecutarComando(sql, parametros);
                    MessageBox.Show("Lector registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string sql = @"
                        UPDATE dbo.Lector 
                        SET DNI_CI = @ci, Nombre = @nombre, Apellido = @apellido, Telefono = @telefono, Fecha_Inicio = @fecha
                        WHERE ID_Lector = @id";

                    Array.Resize(ref parametros, parametros.Length + 1);
                    parametros[^1] = new SqlParameter("@id", _idLectorSeleccionado.Value);

                    ConexionDB.EjecutarComando(sql, parametros);
                    MessageBox.Show("Datos del lector actualizados.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Limpiar();
                CargarLectores();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar lector:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_idLectorSeleccionado == null) return;

            if (MessageBox.Show("¿Desea eliminar este lector?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    ConexionDB.EjecutarComando("DELETE FROM dbo.Lector WHERE ID_Lector = @id", new SqlParameter("@id", _idLectorSeleccionado.Value));
                    Limpiar();
                    CargarLectores();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se puede eliminar el lector porque tiene préstamos registrados:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DgLectores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgLectores.SelectedItem is DataRowView row)
            {
                _idLectorSeleccionado = Convert.ToInt32(row["ID_Lector"]);
                TxtCI.Text = row["DNI_CI"].ToString();
                TxtNombre.Text = row["Nombre"].ToString();
                TxtApellido.Text = row["Apellido"].ToString();
                TxtTelefono.Text = row["Telefono"].ToString();
                if (row["Fecha_Inicio"] != DBNull.Value)
                    DpFechaInicio.SelectedDate = Convert.ToDateTime(row["Fecha_Inicio"]);

                BtnGuardarLector.Content = "Actualizar Lector";
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => Limpiar();

        private void Limpiar()
        {
            _idLectorSeleccionado = null;
            TxtCI.Clear();
            TxtNombre.Clear();
            TxtApellido.Clear();
            TxtTelefono.Clear();
            DpFechaInicio.SelectedDate = DateTime.Today;
            BtnGuardarLector.Content = "Registrar Lector";
            DgLectores.SelectedIndex = -1;
        }

        private void TxtBuscarLector_TextChanged(object sender, TextChangedEventArgs e) => CargarLectores(TxtBuscarLector.Text.Trim());

        private void BtnImprimirReporte_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable dtReporte = ObtenerDataTableLectores();

                if (dtReporte == null || dtReporte.Rows.Count == 0)
                {
                    MessageBox.Show("No hay lectores para imprimir.", "Aviso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string filtroActual = TxtBuscarLector.Text.Trim();
                string subtitulo = string.IsNullOrWhiteSpace(filtroActual)
                    ? $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"
                    : $"Filtro: \"{filtroActual}\"   |   Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";

                ReporteHelper.ImprimirDataTable(
                    dtReporte,
                    "Directorio de Lectores - Biblioteca Municipal",
                    subtitulo
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el reporte: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable ObtenerDataTableLectores()
        {
            DataTable dt = new DataTable("Lectores");
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("CI / DNI", typeof(string));
            dt.Columns.Add("Nombre", typeof(string));
            dt.Columns.Add("Apellido", typeof(string));
            dt.Columns.Add("Teléfono", typeof(string));
            dt.Columns.Add("Fecha Inicio", typeof(string));

            if (DgLectores?.Items == null)
                return dt;

            foreach (var item in DgLectores.Items)
            {
                if (item == null || item == CollectionView.NewItemPlaceholder)
                    continue;

                if (item is DataRowView rowView)
                {
                    string fechaInicio = rowView["Fecha_Inicio"] is DateTime fi
                        ? fi.ToString("dd/MM/yyyy")
                        : rowView["Fecha_Inicio"]?.ToString() ?? "";

                    dt.Rows.Add(
                        rowView["ID_Lector"]?.ToString() ?? "",
                        rowView["DNI_CI"]?.ToString() ?? "",
                        rowView["Nombre"]?.ToString() ?? "",
                        rowView["Apellido"]?.ToString() ?? "",
                        rowView["Telefono"]?.ToString() ?? "",
                        fechaInicio
                    );
                }
            }

            return dt;
        }
    }
}