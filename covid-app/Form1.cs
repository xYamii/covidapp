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
    public partial class S : Form
    {
        private SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..")) + @"\Database1.mdf; Integrated Security = True;");
        private string APIUrl = "https://koronawirus-api.herokuapp.com/api/covid-vaccinations-tests/daily";
        public S()
        {
            InitializeComponent();
            if (!this.isActual())
            {

                this.getAcutalData();
            };
            this.dane_panelu();
        }
        private bool isActual()
        {
            string sql = "SELECT top 1 FORMAT(data, 'dd.MM.yyyy') as data from Szczepienia order by id desc";
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
                    this.con.Close();
                    WebRequest wrGETURL = WebRequest.Create(this.APIUrl);
                    wrGETURL.Method = "GET";
                    Stream objStream = wrGETURL.GetResponse().GetResponseStream();
                    StreamReader objReader = new StreamReader(objStream);
                    string sLine = "";
                    sLine = objReader.ReadLine();
                    JObject json = JObject.Parse(sLine);
                    DateTime data_z_api = DateTime.Parse(json["reportDate"].ToString());
                    string data_z_api_2 = data_z_api.ToString("dd.MM.yyyy");
                    label_dashboard_data.Text = data_z_api_2;
                    if (row["data"].ToString() != data_z_api_2)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
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
                    sqlquery.Parameters.AddWithValue("@dzisiejsza_data", DateTime.Parse(json["reportDate"].ToString()));
                    sqlquery.Parameters.AddWithValue("@wszystkie_dawki", json["today"]["vaccinations"]["vaccinations"].ToString());
                    sqlquery.Parameters.AddWithValue("@pierwsza_dawka", json["today"]["vaccinations"]["firstDoses"].ToString());
                    sqlquery.Parameters.AddWithValue("@druga_dawka", json["today"]["vaccinations"]["secondDoses"].ToString());
                    sqlquery.ExecuteNonQuery();
                    sqlquery.Parameters.Clear();

                    sqlquery.CommandText = "insert into dbo.[Testy] (data, wszystkie, pozytywne) VALUES (@dzisiejsza_data, @wszystkie_testy, @pozytywne_testy)";
                    sqlquery.Parameters.AddWithValue("@dzisiejsza_data", DateTime.Parse(json["reportDate"].ToString()));
                    sqlquery.Parameters.AddWithValue("@wszystkie_testy", json["today"]["tests"]["tests"]["all"].ToString());
                    sqlquery.Parameters.AddWithValue("@pozytywne_testy", json["today"]["tests"]["tests"]["positive"].ToString());
                    sqlquery.ExecuteNonQuery();
                    sqlquery.Parameters.Clear();

                    sqlquery.CommandText = "insert into dbo.[Zakazenia] (data,nowe_zakazenia,nowe_zgony,ozdrowiency) VALUES(@dzisiejsza_data,@nowe_zakazenia,@nowe_zgony,@ozdrowiency)";
                    sqlquery.Parameters.AddWithValue("@dzisiejsza_data", DateTime.Parse(json["reportDate"].ToString()));
                    sqlquery.Parameters.AddWithValue("@nowe_zakazenia", json["today"]["tests"]["infections"].ToString());
                    sqlquery.Parameters.AddWithValue("@nowe_zgony", json["today"]["tests"]["deaths"]["deaths"].ToString());
                    sqlquery.Parameters.AddWithValue("@ozdrowiency", json["today"]["tests"]["recovered"].ToString());
                    sqlquery.ExecuteNonQuery();
                    this.con.Close();

                }
            }
        }

        private void dane_panelu()
        {
            WebRequest wrGETURL = WebRequest.Create(this.APIUrl);
            wrGETURL.Method = "GET";
            Stream objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);
            string sLine = "";
            sLine = objReader.ReadLine();
            JObject json = JObject.Parse(sLine);
            testy_panel_label.Text = json["today"]["tests"]["tests"]["all"].ToString();
            zakazenia_panel_label.Text = json["today"]["tests"]["infections"].ToString();
            zgony_panel_label.Text = json["today"]["tests"]["deaths"]["deaths"].ToString();
            szczepienia_panel_label.Text = json["today"]["vaccinations"]["vaccinations"].ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void minimize_button_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void exit_button_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void dashboard_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = panel_page;
            dashboard_button.Focus();
        }

        private void testy_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = testy_page;
            testy_button.Focus();

        }

        private void zakazenia_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = zakazenia_page;
            zakazenia_button.Focus();
        }

        private void szczepienia_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = szczepienia_page;
            szczepienia_button.Focus();
        }

        private void zgony_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = zgony_page;
            zgony_button.Focus();
        }

        private void mapa_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = mapa_page;
            mapa_button.Focus();
        }

        private void autorzy_button_Click(object sender, EventArgs e)
        {
            pages.SelectedTab = autorzy_page;
            autorzy_button.Focus();
        }

        private void github_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/xYamii/covidapp");
        }
    }
}
