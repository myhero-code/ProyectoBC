using System;
using System.Windows;
using System.Windows.Controls;

namespace ProyectoBC
{
    public partial class TicketReservaWindow : Window
    {
        public TicketReservaWindow(int idReserva, string nombreLector, string ciLector, int idLibro, string tituloLibro, DateTime fechaEmision, DateTime fechaLimite)
        {
            InitializeComponent();

            TxtCodigoReserva.Text = $"N° RESERVA: #{idReserva:D4}";
            TxtLectorTicket.Text = nombreLector;
            TxtCiTicket.Text = $"Cédula de Identidad (CI): {ciLector}";
            TxtLibroTicket.Text = tituloLibro;
            TxtCodigoLibroTicket.Text = $"Código de sala: LIB-{idLibro}";
            TxtHoraLimite.Text = $"{fechaLimite:dd/MM/yyyy} hasta las {fechaLimite:HH:mm} hrs";
            TxtFechaEmision.Text = $"Emitido el {fechaEmision:dd/MM/yyyy} a las {fechaEmision:HH:mm}";
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(AreaImpresionTicket, "Comprobante de Reserva - Biblioteca Municipal");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}