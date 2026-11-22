using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ProyectoBC
{
    public static class ReporteHelper
    {
        public static void ImprimirDataTable(DataTable dt, string titulo, string subtitulo = "")
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para imprimir.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PrintDialog dlg = new PrintDialog();
            if (dlg.ShowDialog() != true)
                return;

            double margenHorizontal = 28;
            double margenVertical = 28;
            double anchoUtil = Math.Max(300, dlg.PrintableAreaWidth - (margenHorizontal * 2));

            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(margenHorizontal, margenVertical, margenHorizontal, margenVertical),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10.5,
                PageWidth = dlg.PrintableAreaWidth,
                ColumnWidth = anchoUtil
            };

            Paragraph pSistema = new Paragraph(new Run("BIBLIOTECA MUNICIPAL - SISTEMA DE GESTIÓN"))
            {
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Margin = new Thickness(0, 0, 0, 2)
            };
            doc.Blocks.Add(pSistema);

            Paragraph pTitulo = new Paragraph(new Run(titulo))
            {
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(26, 32, 44)),
                Margin = new Thickness(0, 0, 0, 2)
            };
            doc.Blocks.Add(pTitulo);

            string textoSub = string.IsNullOrWhiteSpace(subtitulo)
                ? $"Total de registros: {dt.Rows.Count}"
                : $"{subtitulo}   |   Total de registros: {dt.Rows.Count}";

            Paragraph pSubtitulo = new Paragraph(new Run(textoSub))
            {
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            doc.Blocks.Add(pSubtitulo);

            Table tabla = new Table
            {
                CellSpacing = 0,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(0.8)
            };

            double[] pesos = CalcularPesosColumnas(dt);
            double sumaPesos = 0;
            foreach (double p in pesos) sumaPesos += p;

            for (int c = 0; c < dt.Columns.Count; c++)
            {
                double anchoCol = (pesos[c] / sumaPesos) * anchoUtil;
                tabla.Columns.Add(new TableColumn { Width = new GridLength(anchoCol) });
            }

            TableRowGroup headerGroup = new TableRowGroup();
            TableRow headerRow = new TableRow
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 58, 138))
            };

            foreach (DataColumn col in dt.Columns)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(col.ColumnName))
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 10,
                    Foreground = Brushes.White,
                    Margin = new Thickness(7, 6, 7, 6)
                })
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 138)),
                    BorderThickness = new Thickness(0.5)
                });
            }
            headerGroup.Rows.Add(headerRow);
            tabla.RowGroups.Add(headerGroup);

            TableRowGroup bodyGroup = new TableRowGroup();
            Color colorPar = Color.FromRgb(248, 250, 252);
            Brush bordeCelda = new SolidColorBrush(Color.FromRgb(226, 232, 240));

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];
                TableRow dataRow = new TableRow();

                if (i % 2 == 1)
                {
                    dataRow.Background = new SolidColorBrush(colorPar);
                }

                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    string valor = row[c]?.ToString() ?? "";
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(valor))
                    {
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                        Margin = new Thickness(7, 5, 7, 5),
                        LineHeight = 14
                    })
                    {
                        BorderBrush = bordeCelda,
                        BorderThickness = new Thickness(0.5)
                    });
                }
                bodyGroup.Rows.Add(dataRow);
            }
            tabla.RowGroups.Add(bodyGroup);

            doc.Blocks.Add(tabla);

            IDocumentPaginatorSource paginator = doc;
            dlg.PrintDocument(paginator.DocumentPaginator, titulo);
        }

        private static double[] CalcularPesosColumnas(DataTable dt)
        {
            double[] pesos = new double[dt.Columns.Count];

            for (int c = 0; c < dt.Columns.Count; c++)
            {
                string nombre = dt.Columns[c].ColumnName.ToLowerInvariant();
                if (nombre == "id" || nombre.StartsWith("id_") || nombre == "año" || nombre == "anio")
                    pesos[c] = 0.7;
                else if (nombre.Contains("título") || nombre.Contains("titulo") || nombre.Contains("autor") || nombre.Contains("lector") || nombre.Contains("nombre"))
                    pesos[c] = 2.2;
                else
                    pesos[c] = 1.4;
            }

            return pesos;
        }
    }
}