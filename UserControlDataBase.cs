using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SimpleWebClient;
using System.Net.Http;
using System.Data.SQLite;


namespace Auto_Parking_v3
{
    public partial class UserControlDataBase : UserControl
    {
        public Form1 form1;
        public UserControlDataBase(Form1 fm)
        {
            InitializeComponent();
            form1 = fm;
        }

        private void UserControlDataBase_Load(object sender, EventArgs e)
        {
            Loading_base();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            form1.pictureBox1.Visible = true;
            form1.camera_in.ImageGrabbed += form1.camera_process;
            form1.camera_in.Start();
        }

       // -- DBase -- ///////////////////////////////////////////////////////////////////////////////////////////
       /// <summary>
       /// //////////////////////////////////////////////////////////////////////////////////////////////////////
       /// 
       /// </summary>
       /// 

       SQLiteConnection connection;

       private void connectDB()
       {
           connection = new SQLiteConnection("Data Source=Parking_DB.db;Version=3;");
           connection.Open();
       }
       private void closeDB()
       {
           connection.Close();
       }
       public void Loading_base()
       {
           dataGridView1.Rows.Clear();
           connectDB();
           string sql = "SELECT * FROM park_in";
           try
           {
               SQLiteCommand com = new SQLiteCommand(sql, connection);
               SQLiteDataReader reader = com.ExecuteReader();
               while (reader.Read())
               {
                   dataGridView1.Rows.Add(reader["id_in"], reader["park_id"], reader["date_t"], reader["nomber_car"], reader["tarif"], reader["status"]);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.ToString());
           }
           closeDB();
       }

       private void button3_Click(object sender, EventArgs e)
       {
           // delete button
           string query = "delete from park_in where `id_in` = '" + dataGridView1.SelectedCells[0].Value.ToString() + "';";
           //MessageBox.Show(query);
           try
           {
               connectDB();
               SQLiteCommand cmd = new SQLiteCommand(query, connection);
               int k = cmd.ExecuteNonQuery();
               if (k == 1)
               {
                   MessageBox.Show("Successfull query: " + query);
               }
               else
               {
                   MessageBox.Show("Error: deleting from db");
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.ToString());
           }
           Loading_base();
           closeDB();
       }

       private void button2_Click(object sender, EventArgs e)
       {
           // update button
       }

       private void button4_Click(object sender, EventArgs e)
       {
           // refresh button
           Loading_base();
       }
    }
}
