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
using System.Windows.Forms.DataVisualization.Charting;

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
            this.dane_testow();
            this.dane_zakazen();
            this.dane_szczepien();
            this.dane_zgonow();
            this.wczytaj_wojewodztwa();
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
                    label7.Text = data_z_api_2;
                    label22.Text = data_z_api_2;
                    label27.Text = data_z_api_2;
                    dane_z_dnia_zgony.Text = data_z_api_2;
                    dane_z_dnia_mapa.Text = data_z_api_2;
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

            //dashboard
            testy_panel_label.Text = json["today"]["tests"]["tests"]["all"].ToString();
            zakazenia_panel_label.Text = json["today"]["tests"]["infections"].ToString();
            zgony_panel_label.Text = json["today"]["tests"]["deaths"]["deaths"].ToString();
            szczepienia_panel_label.Text = json["today"]["vaccinations"]["vaccinations"].ToString();

            //dane taba testy
        }

        private void dane_testow()
        {
            string sql = "select wszystkie, format(data,'dd-MM') as data from Testy where FORMAT(data,'yyyy-MM-dd') >= FORMAT(DATEADD(day,-7,GETDATE()), 'yyyy-MM-dd') and FORMAT(data,'yyyy-MM-dd') <= FORMAT(GETDATE(), 'yyyy-MM-dd') ORDER BY Id desc";
            SqlCommand sqlquery = this.con.CreateCommand();
            this.con.Open();
            sqlquery.CommandText = sql;
            try
            {
                sqlquery.ExecuteNonQuery();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(sqlquery);
                da.Fill(dt);
                //row["data"].ToString();
                float testy_procent;
                int wszystkie_testy = 0;
                int[] testy = new int[8];
                string[] daty = new string[8];
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    wszystkie_testy += Int32.Parse(row["wszystkie"].ToString());
                    testy[i] = Int32.Parse(row["wszystkie"].ToString());
                    daty[i] = row["data"].ToString();
                    i++;
                }
                testy_dzis.Text = testy[0].ToString();
                testy_wczoraj.Text = testy[1].ToString();
                testy_tydzien_temu.Text = testy[7].ToString();
                float t_tydzien_temu = testy[7];
                float t_dzis = testy[0];

                //tutaj spadek/wzrost testów z t/t
                if (t_tydzien_temu > t_dzis)
                {
                    testy_procent = (((t_tydzien_temu - t_dzis) / t_dzis) * 100);
                    info_spadek_wzrost_testy.ForeColor = Color.Green;
                    info_spadek_wzrost_testy.Text = "spadek o: " + Math.Round(testy_procent, 2) + "%";

                }
                else
                {
                    testy_procent = (((t_dzis - t_tydzien_temu) / t_tydzien_temu) * 100);
                    info_spadek_wzrost_testy.ForeColor = Color.Red;
                    info_spadek_wzrost_testy.Text = "wzrost o: " + Math.Round(testy_procent, 2) + "%";

                }
                //nazwa wykresu: wykres_testy
                try
                {
                    Series wykres_t = wykres_testy.Series.Add("Testy");
                    wykres_testy.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                    wykres_testy.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
                    wykres_t.Color = Color.FromArgb(134, 75, 243);

                    for (int j = 7; j >= 0; j--)
                    {
                        wykres_t.Points.AddXY(daty[j], testy[j]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
                this.con.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void dane_zakazen()
        {
            string sql = "select nowe_zakazenia, format(data,'dd-MM') as data from Zakazenia where FORMAT(data,'yyyy-MM-dd') >= FORMAT(DATEADD(day,-7,GETDATE()), 'yyyy-MM-dd') and FORMAT(data,'yyyy-MM-dd') <= FORMAT(GETDATE(), 'yyyy-MM-dd') ORDER BY Id desc";
            SqlCommand sqlquery = this.con.CreateCommand();
            sqlquery.CommandText = sql;
            this.con.Open();
            try
            {
                sqlquery.ExecuteNonQuery();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(sqlquery);
                da.Fill(dt);
                //row["data"].ToString();
                float zakazenia_procent;
                int wszystkie_testy = 0;
                int[] zakazenia = new int[8];
                string[] daty = new string[8];
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    wszystkie_testy += Int32.Parse(row["nowe_zakazenia"].ToString());
                    zakazenia[i] = Int32.Parse(row["nowe_zakazenia"].ToString());
                    daty[i] = row["data"].ToString();
                    i++;
                }
                zakazenia_dzis.Text = zakazenia[0].ToString();
                zakazenia_wczoraj.Text = zakazenia[1].ToString();
                zakazenia_tydzien_temu.Text = zakazenia[7].ToString();
                float z_tydzien_temu = zakazenia[7];
                float z_dzis = zakazenia[0];

                //tutaj spadek/wzrost zakazen z t/t
                if (z_tydzien_temu > z_dzis)
                {
                    zakazenia_procent = (((z_tydzien_temu - z_dzis) / z_dzis) * 100);
                    info_spadek_wzrost_zakazenia.ForeColor = Color.Green;
                    info_spadek_wzrost_zakazenia.Text = "spadek o: " + Math.Round(zakazenia_procent, 2) + "%";

                }
                else
                {
                    zakazenia_procent = (((z_dzis - z_tydzien_temu) / z_tydzien_temu) * 100);
                    info_spadek_wzrost_zakazenia.ForeColor = Color.Red;
                    info_spadek_wzrost_zakazenia.Text = "wzrost o: " + Math.Round(zakazenia_procent, 2) + "%";

                }
                //nazwa wykresu: wykres_zakazenia
                try
                {
                    Series wykres_z = wykres_zakazenia.Series.Add("Zakazenia");
                    wykres_zakazenia.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                    wykres_zakazenia.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
                    wykres_z.Color = Color.FromArgb(134, 75, 243);

                    for (int j = 7; j >= 0; j--)
                    {
                        wykres_z.Points.AddXY(daty[j], zakazenia[j]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
                this.con.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void dane_szczepien()
        {
            string sql = "select wszystkie, format(data,'dd-MM') as data from Szczepienia where FORMAT(data,'yyyy-MM-dd') >= FORMAT(DATEADD(day,-7,GETDATE()), 'yyyy-MM-dd') and FORMAT(data,'yyyy-MM-dd') <= FORMAT(GETDATE(), 'yyyy-MM-dd') ORDER BY Id desc";
            SqlCommand sqlquery = this.con.CreateCommand();
            sqlquery.CommandText = sql;
            this.con.Open();
            try
            {
                sqlquery.ExecuteNonQuery();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(sqlquery);
                da.Fill(dt);
                //row["data"].ToString();
                float szczepienia_procent;
                int wszystkie_szczepienia = 0;
                int[] szczepienia = new int[8];
                string[] daty = new string[8];
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {

                    wszystkie_szczepienia += Int32.Parse(row["wszystkie"].ToString());
                    szczepienia[i] = Int32.Parse(row["wszystkie"].ToString());
                    daty[i] = row["data"].ToString();
                    i++;
                }
                szczepienia_dzis.Text = szczepienia[0].ToString();
                szczepienia_wczoraj.Text = szczepienia[1].ToString();
                szczepienia_tydzien_temu.Text = szczepienia[7].ToString();
                float z_tydzien_temu = szczepienia[7];
                float z_dzis = szczepienia[0];

                //tutaj spadek/wzrost szczepien z t/t
                if (z_tydzien_temu > z_dzis)
                {
                    szczepienia_procent = (((z_tydzien_temu - z_dzis) / z_dzis) * 100);
                    info_spadek_wzrost_szczepienia.ForeColor = Color.Green;
                    info_spadek_wzrost_szczepienia.Text = "spadek o: " + Math.Round(szczepienia_procent, 2) + "%";

                }
                else
                {
                    szczepienia_procent = (((z_dzis - z_tydzien_temu) / z_tydzien_temu) * 100);
                    info_spadek_wzrost_szczepienia.ForeColor = Color.Red;
                    info_spadek_wzrost_szczepienia.Text = "wzrost o: " + Math.Round(szczepienia_procent, 2) + "%";

                }


                //nazwa wykresu: wykres_szczepienia
                try
                {
                    Series wykres_s = wykres_szczepienia.Series.Add("Szczepienia");
                    wykres_szczepienia.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                    wykres_szczepienia.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
                    wykres_s.Color = Color.FromArgb(134, 75, 243);

                    for (int j = 7; j >= 0; j--)
                    {
                        wykres_s.Points.AddXY(daty[j], szczepienia[j]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                    MessageBox.Show("gowno");
                }
                this.con.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        private void dane_zgonow()
        {
            string sql = "select nowe_zgony, format(data,'dd-MM') as data from Zakazenia where FORMAT(data,'yyyy-MM-dd') >= FORMAT(DATEADD(day,-7,GETDATE()), 'yyyy-MM-dd') and FORMAT(data,'yyyy-MM-dd') <= FORMAT(GETDATE(), 'yyyy-MM-dd') ORDER BY Id desc";
            SqlCommand sqlquery = this.con.CreateCommand();
            sqlquery.CommandText = sql;
            this.con.Open();
            try
            {
                sqlquery.ExecuteNonQuery();
                DataTable dt = new DataTable();
                SqlDataAdapter da = new SqlDataAdapter(sqlquery);
                da.Fill(dt);
                //row["data"].ToString();
                float zgony_procent;
                int wszystkie_zgony = 0;
                int[] zgony = new int[8];
                string[] daty = new string[8];
                int i = 0;
                foreach (DataRow row in dt.Rows)
                {

                    wszystkie_zgony += Int32.Parse(row["nowe_zgony"].ToString());
                    zgony[i] = Int32.Parse(row["nowe_zgony"].ToString());
                    daty[i] = row["data"].ToString();
                    i++;
                }
                zgony_dzis.Text = zgony[0].ToString();
                zgony_wczoraj.Text = zgony[1].ToString();
                zgony_tydzien_temu.Text = zgony[7].ToString();
                float z_tydzien_temu = zgony[7];
                float z_dzis = zgony[0];

                //tutaj spadek/wzrost szczepien z t/t
                if (z_tydzien_temu > z_dzis)
                {
                    zgony_procent = (((z_tydzien_temu - z_dzis) / z_dzis) * 100);
                    info_spadek_wzrost_zgony.ForeColor = Color.Green;
                    info_spadek_wzrost_zgony.Text = "spadek o: " + Math.Round(zgony_procent, 2) + "%";

                }
                else
                {
                    zgony_procent = (((z_dzis - z_tydzien_temu) / z_tydzien_temu) * 100);
                    info_spadek_wzrost_zgony.ForeColor = Color.Red;
                    info_spadek_wzrost_zgony.Text = "wzrost o: " + Math.Round(zgony_procent, 2) + "%";

                }


                //nazwa wykresu: wykres_szczepienia
                try
                {
                    Series wykres_s = wykres_zgony.Series.Add("Zgony");
                    wykres_zgony.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                    wykres_zgony.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
                    wykres_s.Color = Color.FromArgb(134, 75, 243);

                    for (int j = 7; j >= 0; j--)
                    {
                        wykres_s.Points.AddXY(daty[j], zgony[j]);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                    MessageBox.Show("gowno");
                }
                this.con.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        private void wczytaj_wojewodztwa()
        {
            Dictionary<string,  string> wojewodztwa = new Dictionary<string, string>();
            wojewodztwa.Add("podkarpackie", "podkarpackie");
            wojewodztwa.Add("małopolskie", "malopolskie");
            wojewodztwa.Add("śląskie", "slaskie");
            wojewodztwa.Add("opolskie", "opolskie");
            wojewodztwa.Add("świętokrzyskie", "swietokrzyskie");
            wojewodztwa.Add("łódzkie", "lodzkie");
            wojewodztwa.Add("dolnośląskie", "dolnoslaskie");
            wojewodztwa.Add("lubelskie", "lubelskie");
            wojewodztwa.Add("mazowieckie", "mazowieckie");
            wojewodztwa.Add("kujawsko-pomorskie", "kujpom");
            wojewodztwa.Add("wielkopolskie", "wielkopolskie");
            wojewodztwa.Add("lubuskie", "lubuskie");
            wojewodztwa.Add("podlaskie", "podlaskie");
            wojewodztwa.Add("warmińsko-mazurskie", "warmaz");
            wojewodztwa.Add("pomorskie", "pomorskie");
            wojewodztwa.Add("zachodniopomorskie", "zachpom");

            WebRequest wrGETURL = WebRequest.Create("https://koronawirus-api.herokuapp.com/api/covid19/province");
            wrGETURL.Method = "GET";
            Stream objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);
            string sLine = "";
            sLine = objReader.ReadLine();
            JArray json = JArray.Parse(sLine);
            for  (int i  =  0;  i  <  16; i++)
            {
                JObject data = JObject.Parse(json[i].ToString());
                Label label = Controls.Find($"zgony_{wojewodztwa[data["province"].ToString()]}", true).OfType<Label>().FirstOrDefault();
                label.Text = data["today"]["deaths"]["deaths"].ToString();
                label = Controls.Find($"zakazenia_{wojewodztwa[data["province"].ToString()]}", true).OfType<Label>().FirstOrDefault();
                label.Text = data["today"]["infections"].ToString();
            }

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
