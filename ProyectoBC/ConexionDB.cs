using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ProyectoBC
{
    public static class ConexionDB
    {
        // Cadena de conexión hacia tu instancia local de SQL Server Express
        public static readonly string CadenaConexion =
            @"Server=MYHERO\SQLEXPRESS;Database=Biblioteca_Municipal;Integrated Security=True;TrustServerCertificate=True;";

        /// <summary>
        /// Devuelve una nueva instancia de SqlConnection lista para usar con 'using'.
        /// </summary>
        public static SqlConnection ObtenerConexion()
        {
            return new SqlConnection(CadenaConexion);
        }

        /// <summary>
        /// Prueba si la conexión a la base de datos responde correctamente.
        /// </summary>
        public static bool ProbarConexion(out string mensajeError)
        {
            try
            {
                using (SqlConnection cn = ObtenerConexion())
                {
                    cn.Open();
                    mensajeError = string.Empty;
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensajeError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Ejecuta una consulta SELECT y devuelve los resultados en un DataTable (útil para llenar DataGrids o ComboBoxes).
        /// </summary>
        public static DataTable EjecutarConsulta(string sql, params SqlParameter[] parametros)
        {
            DataTable tabla = new DataTable();
            using (SqlConnection cn = ObtenerConexion())
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    if (parametros != null && parametros.Length > 0)
                    {
                        cmd.Parameters.AddRange(parametros);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(tabla);
                    }
                }
            }
            return tabla;
        }

        /// <summary>
        /// Ejecuta comandos INSERT, UPDATE o DELETE y devuelve el número de filas afectadas.
        /// </summary>
        public static int EjecutarComando(string sql, params SqlParameter[] parametros)
        {
            using (SqlConnection cn = ObtenerConexion())
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    if (parametros != null && parametros.Length > 0)
                    {
                        cmd.Parameters.AddRange(parametros);
                    }

                    return cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
