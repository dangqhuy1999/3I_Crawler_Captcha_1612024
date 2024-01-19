using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Appnet_2
{
    public partial class Form1 : Form
    {
        String pathFile = @"H:\3i-Intern\NewProjectWinForm_SmartWork\AppNet2\AppNet\Appnet_2\bin\Debug\P2PCom.json";
        String pathImg = @"H:\3i-Intern\NewProjectWinForm_SmartWork\AppNet2\Appy_version\dist\capchaTool\capcha.png";
        String pathJson = @"H:\3i-Intern\NewProjectWinForm_SmartWork\AppNet2\Appy_version\Parameters.json";
        private FileSystemWatcher fileWatcher;
        private BackgroundWorker backgroundWorker;
        private Process exeProcess;
        private bool shouldStop = false;
        private DateTime lastJsonChangeTime;
        public Form1()
        {
            InitializeComponent();
            fileWatcher = new FileSystemWatcher();
            fileWatcher.Path = Path.GetDirectoryName(pathJson);
            fileWatcher.Filter = Path.GetFileName(pathJson);
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            fileWatcher.Changed += FileWatcher_Changed;
            fileWatcher.EnableRaisingEvents = true;
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += backgroundWorker1_DoWork;
            backgroundWorker.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            this.FormClosing += Form1_FormClosing;
            
        }
        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string jsonContent;
            bool check = false;
            using (var fileStream = new FileStream(pathJson, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    jsonContent = streamReader.ReadToEnd();
                }
            }
            if (!CheckJsonNotifyImg(jsonContent, check))
            {
                string latestImagePath = FindLatestImageInFolder(@"H:\3i-Intern\NewProjectWinForm_SmartWork\AppNet2\Appy_version\dist\capchaTool");

                if (!string.IsNullOrEmpty(latestImagePath))
                {
                    pictureBox1.ImageLocation = latestImagePath;
                }
            }
            lastJsonChangeTime = DateTime.Now;
        }
        private string FindLatestImageInFolder(string folderPath)
        {
            string[] imageExtensions = { ".png" };
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            FileInfo[] files = directory.GetFiles()
                .Where(f => imageExtensions.Contains(f.Extension.ToLower()))
                .OrderByDescending(f => f.LastWriteTime)
                .ToArray();

            if (files.Length > 0)
            {
                return files[0].FullName;
            }

            return null;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Xóa tệp tin ảnh capcha
            if (File.Exists(pathImg))
            {
                File.Delete(pathImg);
            }
        }
        private bool CheckJsonNotifyImg(string jsonContent, bool shouldStop)
        {
            try
            {
                JArray jsonArray = JArray.Parse(jsonContent);

                if (jsonArray.Count > 0)
                {
                    JObject lastObject = (JObject)jsonArray[jsonArray.Count - 1];
                    JToken notifyToken = lastObject["notify"];
                    JToken user = lastObject["user"];

                    if (notifyToken != null && notifyToken.ToString() == "img" && user.ToString() == "B")
                    {

                        pictureBox1.ImageLocation = pathImg;
                        return true;

                    }
                    else if (notifyToken != null && notifyToken.ToString() == "fail")
                    {
                        MessageBox.Show("Nhập mã sai mã capcha,vui lòng nhập lại  ");

                    }
                    else if (notifyToken != null && notifyToken.ToString() == "over")
                    {
                        MessageBox.Show("Vượt quá rồi hạn nhập mã capcha ");
                        shouldStop = true;
                        return true;
                    }
                    else if (notifyToken != null && notifyToken.ToString() == "correct")
                    {
                        MessageBox.Show("Đăng  nhập thành công ");
                        shouldStop = true;
                        return true;
                    }
                }
            }
            catch (JsonReaderException)
            {
            }

            return false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void Comfirm_Click(object sender, EventArgs e)
        {
            String codeCapcha = textBox1.Text;
            DateTime currentTime = DateTime.Now;
            string jsonContent = File.ReadAllText(pathFile);
            string json_py = File.ReadAllText(pathJson);

            string user_id = "A";
            int id = 0;

            try
            {
                List<dynamic> jsonArrayObject = JsonConvert.DeserializeObject<List<dynamic>>(jsonContent);

                JArray jsonArray = JArray.Parse(jsonContent);

                if (jsonArray.Count > 0)
                {
                    JObject lastObject = (JObject)jsonArray[jsonArray.Count - 1];
                    id = (int)lastObject.GetValue("id") + 1;
                }
                var newJson = new
                {
                    id = id,
                    user = user_id,
                    notify = codeCapcha,
                    datetime = currentTime
                };
                jsonArrayObject.Add(newJson);

                string newJsonString = JsonConvert.SerializeObject(jsonArrayObject);
                using (var fileStream = new FileStream(pathFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        streamWriter.Write(newJsonString);
                    }
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show("Lỗi định dạng tệp tin JSON.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DateTime startTime = DateTime.Now;
            bool shouldStop = false; // Thêm biến shouldStop để kiểm tra xem có nên dừng chương trình hay không

            if (!CheckJsonNotifyImg(json_py, shouldStop)) // Truyền giá trị shouldStop vào phương thức CheckJsonNotifyImg
            {
                System.Threading.Thread.Sleep(1000);
                TimeSpan elapsed = DateTime.Now - startTime;
                pictureBox1.ImageLocation = pathImg;

                if (elapsed.TotalSeconds >= 60)
                {
                    MessageBox.Show("pyThon không phản hồi", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            if (shouldStop)
            {
                Environment.Exit(0);
                return;
            }
        }
        private async void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            await Task.Run(() =>
            {
                fileWatcher.Changed += FileWatcher_Changed;
                fileWatcher.EnableRaisingEvents = true;
            });

            await Task.Run(() =>
            {


                ExecuteExeFile(@"H:\3i-Intern\NewProjectWinForm_SmartWork\AppNet2\Appy_version\dist\capchaTool\capchaTool.exe");
            });
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }
        private void ExecuteExeFile(string filePath)
        {
            Process process = new Process();
            process.StartInfo.FileName = filePath;
            process.StartInfo.Arguments = "-n";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            process.Start();
            process.WaitForExit();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (exeProcess == null || exeProcess.HasExited)
            {
                string executablePath = @"H:\3i-Intern\NewProjectWinForm_SmartWork\AppNet2\Appy_version\dist\capchaTool\capchaTool.exe";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = Path.GetDirectoryName(executablePath),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                exeProcess = new Process { StartInfo = startInfo };

                await Task.Run(() =>
                {
                    exeProcess.Start();
                    exeProcess.WaitForExit();
                });

                exeProcess = null;
                fileWatcher.Changed += FileWatcher_Changed;
                fileWatcher.EnableRaisingEvents = true;
            }
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
