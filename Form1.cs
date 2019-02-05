using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using openalprnet;
using System.IO;
using SimpleWebClient;
using System.Net.Http;
using System.Data.SQLite;
using System.Threading;
using System.Reflection;

namespace Auto_Parking_v3
{
    public partial class Form1 : Form
    {
        UserControl uc = null;
        public VideoCapture camera_in;
        Mat mt1 = new Mat();

        SQLiteConnection connection;
        String config_file = Path.Combine(AssemblyDirectory, "openalpr.conf");
        String runtime_data_dir = Path.Combine(AssemblyDirectory, "runtime_data");

        private String numberCar = "";
        public bool savePhoto = false;

        public Form1()
        {
            InitializeComponent();
            connectDB();
        }

        // -- cam 1 -- //
        public void kam_in()
        {
            camera_in = new VideoCapture(0);
            //camera_in = new Emgu.CV.VideoCapture("rtsp://test:test123321@192.168.8.113/live/stream");
            camera_in.ImageGrabbed += camera_process;
            camera_in.Grab();
            camera_in.Start();
        }
        public void camera_process(object sender, EventArgs e)
        {
            try
            {
                camera_in.Retrieve(mt1);
                Bitmap bmp = mt1.Bitmap;
                CvInvoke.Rectangle(mt1, new Rectangle(new Point(bmp.Width / 10 * 3, bmp.Height / 10 * 3),
                    new Size(bmp.Width / 10 * 7 - bmp.Width / 10 * 3, bmp.Height / 2 - bmp.Height / 10 * 3)),
                    new Bgr(0, 0, 255).MCvScalar, 2, LineType.EightConnected, 0);
                pictureBox1.Image = mt1.Bitmap;
                bmp = bmp.Clone(new Rectangle(new Point(bmp.Width / 10 * 3, bmp.Height / 10 * 3), 
                    new Size(bmp.Width/10*7 - bmp.Width/10*3, bmp.Height/2 - bmp.Height/10*3)), bmp.PixelFormat);
                processImageFile(bmp);
                pictureBox3.Image = bmp;
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void save_mt()
        {
            try
            {
                string path = @"C:\Rasmlar";
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }
                string guid = Guid.NewGuid().ToString();
                pictureBox1.Image.Save(@"C:\Rasmlar\image" + guid + ".jpg");
            }
            catch { }
        }

        private void processImageFile(Bitmap image)
        {
            using (var alpr = new AlprNet("eu", config_file, runtime_data_dir))
            {
                if (!alpr.IsLoaded())
                {
                    return;
                }
                var results = alpr.Recognize(image);
                if (results.Plates.Count() > 0)
                {
                    var images = new List<Image>(results.Plates.Count());
                    foreach (var result in results.Plates)
                    {
                        var minX = result.PlatePoints.Min(p => p.X);
                        var minY = result.PlatePoints.Min(p => p.Y);
                        var maxX = result.PlatePoints.Max(p => p.X);
                        var maxY = result.PlatePoints.Max(p => p.Y);
                        
                        if (minX < 0) minX = 0;
                        if (minY < 0) minY = 0;
                        if (maxX < minX) maxX = minX + 10;
                        if (maxY < minY) maxY = minY + 10;
                        if (maxX > image.Width) maxX = image.Width;
                        if (maxY > image.Height) maxY = image.Height;

                        
                        var rect = new Rectangle(new Point(minX, minY), new Size(maxX - minX, maxY - minY));
                        var cropped = cropImage(image, rect);
                        images.Add(cropped);
                        numberCar = EnBenzeyenPlakayiGetir(result.TopNPlates).Trim();
                    }
                    CvInvoke.PutText(mt1, numberCar, new System.Drawing.Point(10, (mt1.Height - 40)), FontFace.HersheyDuplex, 1.3, new Bgr(0, 255, 255).MCvScalar);
                    CvInvoke.PutText(mt1, DateTime.Now.ToString(" dd.MM.yyyy HH:mm:ss"), new System.Drawing.Point(10, (mt1.Height - 25)), FontFace.HersheyDuplex, 0.5, new Bgr(0, 255, 255).MCvScalar);
                    pictureBox3.Image = mt1.Bitmap;
                    if (images.Any())
                    {
                        pictureBox2.Image = combineImages(images);
                    }
                }
                else
                {
                    return;
                }
            }
            if (numberCar.Length >= 8)
            {
                camera_in.Stop();
                camera_in.ImageGrabbed -= camera_process;
                timeStop = true;
            }
        }

        public static Bitmap combineImages(List<Image> images)
        {
            Bitmap finalImage = null;
            try
            {
                var width = 0;
                var height = 0;

                foreach (var bmp in images)
                {
                    width += bmp.Width;
                    height = bmp.Height > height ? bmp.Height : height;
                }
                finalImage = new Bitmap(width, height);
                using (var g = Graphics.FromImage(finalImage))
                {
                    g.Clear(Color.Red);
                    var offset = 0;
                    foreach (Bitmap image in images)
                    {
                        g.DrawImage(image, new Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }
                return finalImage;
            }
            catch (Exception ex)
            {
                if (finalImage != null)
                    finalImage.Dispose();
                throw ex;
            }
            finally
            {
                foreach (var image in images)
                {
                    image.Dispose();
                }
            }
        }

        private string EnBenzeyenPlakayiGetir(List<AlprPlateNet> plakalar)
        {
            foreach (var item in plakalar)
            {
                return item.Characters.PadRight(12);
            }
            return "";
        }

        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public Rectangle boundingRectangle(List<Point> points)
        {
            var minX = points.Min(p => p.X);
            var minY = points.Min(p => p.Y);
            var maxX = points.Max(p => p.X);
            var maxY = points.Max(p => p.Y);
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            var sizeX = maxX - minX;
            var sizeY = maxY - minY;
            if (sizeX < 0) sizeX = 0;
            if (sizeY < 0) sizeY = 0;
            if (sizeX < minX) sizeX = minX;
            if (sizeY < minY) sizeY = minY;
            return new Rectangle(new Point(minX, minY), new Size(sizeX, sizeY));
        }

        private static Image cropImage(Bitmap bmpImage, Rectangle cropArea)
        {
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            kam_in();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs ex)
        {
            if (MessageBox.Show("Вы хотите закрыть это приложение?", "Выхода приложения", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                camera_in.Stop();
                closeDB();
            }
            else
            {
                ex.Cancel = true;
            }
        }

        public bool timeStop = false;
        public int k_timeTick = 0;
        bool timeTickbegin = false;

        private void timer_vaqt_Tick(object sender, EventArgs e)
        {
            if (timeStop == true)
            {
                toolStripProgressBar1.Value = 1;
                label2.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                label1.Text = numberCar;
                if (label1.Text.Length >= 8) 
                    insertDB();
                timeTickbegin = true;
                timeStop = false;
            }
            if (timeTickbegin)
            {
                k_timeTick++;
            }
            if (k_timeTick > 4)
            {
                k_timeTick = 0;
                timeTickbegin = false;
                toolStripProgressBar1.Value = 0;
                camera_in.ImageGrabbed += camera_process;
                camera_in.Start();
            }

            toolStripStatusLabel_Data.Text = (DateTime.Now.ToString("dd.MM.yyyy" + " HH:mm:ss"));
        }

       

        private void exitToolStripMenuItem_Click(object sender, FormClosingEventArgs e)
        {
            Form1_FormClosing(sender, e);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "JPG files(*.jpg)|*.jpg";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Image img = Image.FromFile(ofd.FileName);
                Bitmap tempImage = new Bitmap(img);
                processImageFile(tempImage);
            }
        }

        private void saveToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            save_mt();
        }

        private void startToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            camera_in.ImageGrabbed += camera_process;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            camera_in.ImageGrabbed -= camera_process;
        }

        private void dataBaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.pictureBox1.Visible = false;
            camera_in.ImageGrabbed -= camera_process;
            camera_in.Stop();
            if (uc == null)
            {
                uc = new UserControlDataBase(this);
            }
            uc.Visible = true;
            uc.Dock = DockStyle.Fill;
            uc.Parent = panel2;
        }

        private void connectDB()
        {
            connection = new SQLiteConnection("Data Source=Parking_DB.db;Version=3;");
            connection.Open();
        }
        private void closeDB()
        {
            connection.Close();
        }

        private void insertDB()
        {
            String querycmd = "insert into park_in(`park_id`, `date_t`, `nomber_car`, `tarif`, `status`) values ( " +
                                "'12', '"+label2.Text+"', '"+label1.Text+"', 'simple', '1');";
            try
            {
                SQLiteCommand cmd = new SQLiteCommand(querycmd, connection);
                int k = cmd.ExecuteNonQuery();
                if (k == 1)
                {
                    toolStripStatusLabel1.Text = "successfull: insert data to db";
                }
                else
                {
                    toolStripStatusLabel1.Text = "Error: insert data to db";
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel1.Text = ex.ToString();
            }
        }
    }
}
