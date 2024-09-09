using AuroraPOS.Data;
using AuroraPOS.Models;
using iText.IO.Image;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace AuroraPOS.Services
{
	public class AlfaHelper
	{
		public static string Currency = "$";

        public static string GetMd5Hash(string input)
		{
			using MD5 md5Hash = MD5.Create();
			byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
			StringBuilder sBuilder = new StringBuilder();

			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}
			return sBuilder.ToString();
		}
		// Verify a hash against a string.  
		public static bool VerifyMd5HashWithMySecurity(string input, string hash)
		{
			// Hash the input.  
			string hashOfInput = GetMd5Hash(input);
			// Create a StringComparer an compare the hashes.  
			StringComparer comparer = StringComparer.OrdinalIgnoreCase;
			if (0 == comparer.Compare(hashOfInput, hash))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

        public static DateTime GetLocalTime(DateTime dt)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(dt);
        }

		public static decimal ConvertQtyToBase(decimal originQty, int UnitNum, List<ItemUnit> Units)
		{
			if (UnitNum <= 1) return originQty;

			try
			{
				var realrates = new List<decimal>();
				var units = Units.OrderBy(s => s.Number).ToList();
				int i = 0;
				decimal rate = 0;
				foreach (var unit in units)
				{
					if (i == 0)
					{
						realrates.Add(unit.Rate);
						rate = unit.Rate;
					}
					else
					{
						var realrate = rate * unit.Rate;
						realrates.Add(realrate);
						rate = realrate;
					}

					i++;
				}

				return originQty / realrates[UnitNum - 1] * realrates[0];
			}
			catch { }
			return originQty;
		}
		public static iText.Layout.Element.Image GetLogo(string logoBase64)
		{
			if (string.IsNullOrEmpty(logoBase64)) { return null; }
			var index = logoBase64.IndexOf("base64,");
			logoBase64 = logoBase64.Substring(index + 7);

			byte[] bytes = Convert.FromBase64String(logoBase64);

			try
			{
				//System.Drawing.Image image;
				using (MemoryStream ms = new MemoryStream(bytes))
				{
					//image = System.Drawing.Image.FromStream(ms);
					//if (image != null)
					//{
						ImageData imageData = ImageDataFactory.Create(bytes);
						iText.Layout.Element.Image itextImage = new iText.Layout.Element.Image(imageData);
						return itextImage;
					//}
				}
			}
			catch(Exception ex)
			{
				var m = ex;
			}


			return null;

		}
		public static decimal ConvertBaseQtyTo(decimal originQty, int UnitNum, List<ItemUnit> Units)
		{
			if (UnitNum <= 1) return originQty;

			try
			{
				var realrates = new List<decimal>();
				var units = Units.OrderBy(s => s.Number).ToList();
				int i = 0;
				decimal rate = 0;
				foreach (var unit in units)
				{
					if (i == 0)
					{
						realrates.Add(unit.Rate);
						rate = unit.Rate;
					}
					else
					{
						var realrate = rate * unit.Rate;
						realrates.Add(realrate);
						rate = realrate;
					}

					i++;
				}

				return originQty * realrates[UnitNum - 1] / realrates[0];
			}
			catch { }
			return originQty;
		}

        #region Codigo Nuevo
        //public static int guardaadb(string nombre, string extencion, byte[] cuerpo, string impresora, string id_estacion, int numero_copias, int status_impresion, ExtendedAppDbContext dbContext)
        //{          
            //var builder = WebApplication.CreateBuilder();
            //string sconn = string.Empty;
            //try
            //{

							


            //    sconn = builder.Configuration.GetConnectionString(Empresa);
            //    Npgsql.NpgsqlConnection conn = new Npgsql.NpgsqlConnection(sconn);
            //    Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
            //    cmd.Connection = conn;
            //    cmd.CommandText = "INSERT INTO \r\n  public.t_impresion\r\n(\r\n  \r\n  nombre,\r\n  extencion,\r\n  cuerpo,\r\n  impresora,\r\n  id_estacion,\r\n  status_impresion,\r\n  numero_copias\r\n)\r\nVALUES (\r\n \r\n  @nombre,\r\n  @extencion,\r\n  @cuerpo,\r\n  @impresora,\r\n @id_estacion,\r\n  @status_impresion,\r\n @numero_copias\r\n);";

            //    cmd.Parameters.AddWithValue("nombre", nombre);
            //    cmd.Parameters.AddWithValue("extencion", extencion);
            //    cmd.Parameters.AddWithValue("cuerpo", System.Convert.ToBase64String(cuerpo));
            //    cmd.Parameters.AddWithValue("impresora", impresora);
            //    cmd.Parameters.AddWithValue("id_estacion", id_estacion);
            //    cmd.Parameters.AddWithValue("status_impresion", status_impresion);
            //    cmd.Parameters.AddWithValue("numero_copias", numero_copias);
            //    conn.Open();
            //    int i = cmd.ExecuteNonQuery();
            //    conn.Close();
            //    return i;
            //}
            //catch (Exception e)
            //{
            //    return 0;
            //    throw e;
            //}
        //}

        //public static DataSet CrearQuery(string Sql, Npgsql.NpgsqlParameter[]? parametros, string nombredatasourse,string empresa)
        //{
        //    DataSet ds = new DataSet();
        //    string sconn = string.Empty;
        //    var builder = WebApplication.CreateBuilder();
        //    try
        //    {




        //        sconn = builder.Configuration.GetConnectionString(empresa);
        //        Npgsql.NpgsqlConnection conn = new Npgsql.NpgsqlConnection(sconn);
        //        Npgsql.NpgsqlCommand cmd = conn.CreateCommand();
        //        Npgsql.NpgsqlDataAdapter da = new Npgsql.NpgsqlDataAdapter(cmd);
        //        cmd.Connection = conn;
        //        cmd.CommandText = Sql;
        //        if (parametros != null)
        //            cmd.Parameters.AddRange(parametros);

        //        conn.Open();
        //        da.Fill(ds, nombredatasourse);
        //        conn.Close();
        //        //TextWriter tw = new StreamWriter($@"Reports/ultimo.xml");
        //        //TextWriter tw1 = new StreamWriter($@"Reports/ultimo.xsd");
        //        //ds.WriteXml(tw1, XmlWriteMode.WriteSchema);
        //        //ds.WriteXml(tw, XmlWriteMode.IgnoreSchema);
        //        //tw.Close();
        //        //tw1.Close();
        //        return ds;
        //    }
        //    catch (Exception e)
        //    {

        //        throw e;
        //    }
        //}

   //     internal static Stream GetReporte(int id, int tipo,string empresa)
   //     {
   //         DataSet ds = new DataSet();
   //         Npgsql.NpgsqlParameter[] parametros = new Npgsql.NpgsqlParameter[1];
   //         parametros[0] = new Npgsql.NpgsqlParameter("ID", id);
   //         string qry = string.Empty;

   //         switch (tipo)
   //         {
   //             case 1:
   //                 qry = "select f_archivo from t_formato_impresion_general  where f_id=@ID";
   //                 break;
   //             default:
   //                 qry = "select f_archivo from t_formato_impresion_reportes  where f_id=@ID";
   //                 break;
   //         }


   //         ds = CrearQuery(qry, parametros, "general",empresa);
			//if (ds.Tables[0].Rows.Count==0)
			//{
			//	if(tipo==1)
			//	Console.WriteLine("No se encontro el reporte " + id + " t_formato_impresion_general");
			//	else
   //                 Console.WriteLine("No se encontro el reporte " + id + " t_formato_impresion_reportes");
   //         }
   //         byte[] buffer = Convert.FromBase64String(ds.Tables[0].Rows[0][0].ToString());
			//using (MemoryStream stream = new MemoryStream(buffer))
			//{
   //             return stream;
   //         }                
   //         //return stream;
   //     }

   //     internal static List<string> GetPhysicalPrinters(string empresa)
   //     {
   //         DataSet ds = new DataSet();                   
   //         string qry = string.Empty;

   //         qry = "select f_impresora from t_impresoras";
            
   //         ds = CrearQuery(qry, null, "general", empresa);
   //         if (ds.Tables[0].Rows.Count == 0)
   //         {                
   //             Console.WriteLine("No hay impresoras cargadas en t_impresoras");
   //         }

   //         var printers = new List<string>();
			//foreach(DataRow item in ds.Tables[0].Rows) {
			//	printers.Add(item[0].ToString());

   //         }
		
   //         return printers;
   //     }
        #endregion



    }
}
