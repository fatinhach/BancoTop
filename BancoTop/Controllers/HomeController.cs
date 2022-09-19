using ClosedXML.Excel;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BancoTop.Controllers
{
    public class HomeController : Controller
    {
        private SqlConnection _connection;
        public ActionResult controledecadastros()
        {
            return View();
        }
            public ActionResult ExportarDados(string Data)
        {
            //estabelecendo conexão com o BD já configurada em webconfig 
            _connection = new SqlConnection(ConfigurationManager.ConnectionStrings["conBancoTop"].ConnectionString);
            DataTable dt = new DataTable();
            SqlCommand cmd = new SqlCommand();
            SqlDataAdapter da = new SqlDataAdapter();

            var DataS = "";
            //setar o CommandText e a connection para o SqlCommand
            cmd = new SqlCommand("[dbo].[BancoSantanderContratos]", _connection);
            //definindo o tipo de comando como Store procedure
            cmd.CommandType = CommandType.StoredProcedure;
            //adcionei o parametro data pois na nossa procedure é necessário
            cmd.Parameters.Add("@Data", SqlDbType.VarChar).Value = Data;
            
            da.SelectCommand = cmd;

            //popular o DataTable
            da.Fill(dt);
            _connection.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                DataS = reader["dataabertura"].ToString();
                da.SelectCommand = cmd;
            }
            if (dt.Rows.Count > 0)
            {
                dt.TableName = "Contratos";
                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt);
                    wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; 
                    wb.Style.Font.Bold = true;
                    Response.Clear();
                    Response.Buffer = true;
                    Response.Charset = "";
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("content-disposition", "attachment;filename=RemessaContratos.xlsx");
                    using(MemoryStream MyMemoryStream = new MemoryStream())
                    {
                        //armazena os Dados "crus" em memória.
                        wb.SaveAs(MyMemoryStream);
                        MyMemoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();   
                        Response.End();
                    }
                }
                ViewBag.Mensagem = "Planilha Exportada com Sucesso";
            }
            else if (dt.Rows.Count <= 0)
            {
                ViewBag.Mensagem = "Não existem dados para serem exportados nesta DATA!";
            }

            return View();

        }
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult controledeImportacao()
        {
            return View();
        }
        public ActionResult ImportarDados(string Data)
        {
           Models.conexaoTOPDataContext dc = new Models.conexaoTOPDataContext(_connection);
            try
            {
                //obtém a planilha com os dados novos
                var vDsNome = Request.Files[0].FileName;
                var vInputStream = Request.Files[0].InputStream;
                var reader = ExcelReaderFactory.CreateReader(vInputStream);
                var conf = new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                };
                var dataSet = reader.AsDataSet(conf);
                var dataTable = dataSet.Tables[0];
                //GRAVA DADOS DA PLANILHA ROA
                foreach (DataRow row in dataTable.Rows)
                {
                    Models.Contratos tb = new Models.Contratos();
                    tb.Contrato = row[0].ToString();
                    tb.Nome = row[1].ToString();
                    tb.cpf = row[2].ToString();
                    tb.codloja = row[3].ToString();
                    tb.nomeloja = row[4].ToString();
                    tb.dataabertura = Convert.ToDateTime(row[5].ToString());
                    tb.bco = row[6].ToString();
                    tb.agen = row[7].ToString();
                    dc.Contratos.InsertOnSubmit(tb);
                    dc.SubmitChanges();
                }
                ViewBag.Mensagem = "Planilha Importada com sucesso";
                return View();
            }catch(Exception ex)
            {
                ViewBag.Mensagem = "Erro: " + ex.Message + "" +ex.InnerException;
                return View();
            }
            

        }
    }
}