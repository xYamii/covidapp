using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace covid_app
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\yamster\source\repos\covid-app\covid-app\Database1.mdf;Integrated Security=True");
        private string APIUrl = "https://koronawirus-api.herokuapp.com/api/covid-vaccinations-tests/daily";
        public Form1()
        {
            InitializeComponent();
            if (!this.isActual())
            {
                MessageBox.Show("tak xd");
                this.getAcutalData();
            };
        }
        private bool isActual()
        {
            string sql = "SELECT top 1 FORMAT(data, 'dd/MM/yyyy') as data from Szczepienia order by data desc";
            SqlCommand sqlquery = this.con.CreateCommand();
            this.con.Open();
            sqlquery.CommandText = sql;
            try
            {
                sqlquery.ExecuteNonQuery();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(sqlquery);
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string localDate = DateTime.Today.ToString("dd/MM/yyyy");
                    this.con.Close();

                    if (row["data"].ToString() == localDate)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    this.con.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return false;
            }
        }
        private void getAcutalData()
        {
            WebRequest wrGETURL = WebRequest.Create(this.APIUrl);
            wrGETURL.Method = "GET";
            Stream objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);
            string sLine = "";
            int i = 0;

            while (sLine != null)
            {
                i++;
                sLine = objReader.ReadLine();
                if (sLine != null)
                {
                    JObject json = JObject.Parse(sLine);


                    this.con.Open();
                    SqlCommand sqlquery = this.con.CreateCommand();

                    sqlquery.CommandText = "insert into dbo.[Szczepienia] (data,wszystkie,pierwsza_dawka,druga_dawka) VALUES(@dzisiejsza_data,@wszystkie_dawki,@pierwsza_dawka,@druga_dawka)";
                    sqlquery.Parameters.AddWithValue("@dzisiejsza_data", json["reportDate"].ToString());
                    sqlquery.Parameters.AddWithValue("@wszystkie_dawki", json["today"]["vaccinations"]["vaccinations"].ToString());
                    sqlquery.Parameters.AddWithValue("@pierwsza_dawka", json["today"]["vaccinations"]["firstDoses"].ToString());
                    sqlquery.Parameters.AddWithValue("@druga_dawka", json["today"]["vaccinations"]["secondDoses"].ToString());
                    sqlquery.ExecuteNonQuery();
                    sqlquery.Parameters.Clear();

                    sqlquery.CommandText = "insert into dbo.[Testy] (data, wszystkie, pozytywne) VALUES (@dzisiejsza_data, @wszystkie_testy, @pozytywne_testy)";
                    sqlquery.Parameters.AddWithValue("@dzisiejsza_data", json["reportDate"].ToString());
                    sqlquery.Parameters.AddWithValue("@wszystkie_testy", json["today"]["tests"]["tests"]["all"].ToString());
                    sqlquery.Parameters.AddWithValue("@pozytywne_testy", json["today"]["tests"]["tests"]["positive"].ToString());
                    sqlquery.ExecuteNonQuery();
                    sqlquery.Parameters.Clear();

                    sqlquery.CommandText = "insert into dbo.[Zakazenia] (data,nowe_zakazenia,nowe_zgony,ozdrowiency) VALUES(@dzisiejsza_data,@nowe_zakazenia,@nowe_zgony,@ozdrowiency)";
                    sqlquery.Parameters.AddWithValue("@dzisiejsza_data", json["reportDate"].ToString());
                    sqlquery.Parameters.AddWithValue("@nowe_zakazenia", json["today"]["tests"]["infections"].ToString());
                    sqlquery.Parameters.AddWithValue("@nowe_zgony", json["today"]["tests"]["deaths"]["deaths"].ToString());
                    sqlquery.Parameters.AddWithValue("@ozdrowiency", json["today"]["tests"]["recovered"].ToString());
                    sqlquery.ExecuteNonQuery();
                    this.con.Close();

                }
            }
        }
    }
}
