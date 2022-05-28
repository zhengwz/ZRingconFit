using BaseServer;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ZRingconFit
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        public delegate void StartGameHandler();
        public event StartGameHandler StartGame;

        private bool isEdit = false;
        public SettingWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            tb_YuzuUri.Text = Global.YuzuUri;
            tb_UserUri.Text = Global.UserUri;
            tb_GameUri.Text = Global.GameUri;
            cb_AutoStartGame.IsChecked = Global.AutoStartGame;
            cb_ReplaceConfig.IsChecked = Global.ReplaceConfig;
            isEdit = false;
        }

        private void btn_Backup_Click(object sender, RoutedEventArgs e)
        {
            if(isEdit)
            {
                if(System.Windows.MessageBox.Show("是否保存配置？","提示",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
                {
                    SaveConfig();
                }
            }
            this.Close();
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            System.Windows.MessageBox.Show("保存成功");
            isEdit = false;
        }

        private void SaveConfig()
        {
            Global.YuzuUri = tb_YuzuUri.Text;
            Global.UserUri = tb_UserUri.Text;
            Global.GameUri = tb_GameUri.Text;
            Global.YuzuName = System.IO.Path.GetFileNameWithoutExtension(Global.YuzuUri);
            Global.AutoStartGame = cb_AutoStartGame.IsChecked.Value;
            Global.ReplaceConfig = cb_ReplaceConfig.IsChecked.Value;
            Global.SaveConfig();
        }

        private void btn_SaveAndStart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            StartGame?.Invoke();
            this.Close();
        }

        private void btn_BrowserYuzu_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "请选择yuzu路径";
            openFileDialog.Filter = "yuzu|yuzu.exe";
            openFileDialog.FileName = String.Empty;
            openFileDialog.FilterIndex = 0;
            openFileDialog.Multiselect = false;
            openFileDialog.RestoreDirectory = false;
            if(openFileDialog.ShowDialog() == false)
            {
                return;
            }
            tb_YuzuUri.Text = openFileDialog.FileName;
            isEdit = true;
            CheckUserUri();
        }

        private void CheckUserUri()
        {
            string userFolder = tb_YuzuUri.Text.Substring(0, tb_YuzuUri.Text.Length - "yuzu.exe".Length) + "user\\";
            if (Directory.Exists(userFolder))
            {
                tb_UserUri.Text = userFolder;
            }
            else
            {
                string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                if (Directory.Exists($"{appData}\\yuzu"))
                {
                    tb_UserUri.Text = $"{appData}\\yuzu\\";
                }
                /*
                查找AppData中自动生成的user，待补充
                */
            }
        }

        private void btn_BrowserUser_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog m_Dialog = new FolderBrowserDialog();
            if (m_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            tb_UserUri.Text = m_Dialog.SelectedPath.Trim() + "\\";
            isEdit = true;
        }

        private void btn_BrowserGame_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "请选择游戏路径";
            openFileDialog.Filter = "游戏文件(NSP,XCI)|*.nsp;*.xci";
            openFileDialog.FileName = String.Empty;
            openFileDialog.FilterIndex = 0;
            openFileDialog.Multiselect = false;
            openFileDialog.RestoreDirectory = false;
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }
            tb_GameUri.Text = openFileDialog.FileName;
            isEdit = true;
        }

        private void btn_SaveYuzuConfig_Click(object sender, RoutedEventArgs e)
        {
            string folder = System.Environment.CurrentDirectory + "\\YuzuConfig\\";
            if(!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string configPath = Global.UserUri + "config\\qt-config.ini";
            if (File.Exists(configPath))
            {
                File.Copy(configPath, folder + "\\qt-config.ini", true);
            }
            System.Windows.MessageBox.Show("保存成功");
        }

        private void cb_AutoStartGame_Checked(object sender, RoutedEventArgs e)
        {
            isEdit = true;
        }

        private void AutoSearch()
        {
            Thread searchThread = new Thread(() =>
            {
                Loading(true);
                var disk = GetDisk();
                yuzuFile = "";
                gameFile = "";
                for (int i = 0; i < disk.Count; i++)
                {
                    if (disk[i] != "C:\\")
                    {
                        SearchFile(disk[i]);
                    }
                }
                Loading(false);
                Dispatcher.Invoke(() =>
                {
                    if (yuzuFile != "" && gameFile != "")
                    {
                        System.Windows.MessageBox.Show("已找到yuzu及游戏位置");
                    }
                    else if (yuzuFile != "")
                    {
                        System.Windows.MessageBox.Show("已找到yuzu位置，未找到游戏位置");
                    }
                    else if (gameFile != "")
                    {
                        System.Windows.MessageBox.Show("已找到游戏位置，未找到yuzu位置");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("未能找到yuzu及游戏位置");
                    }
                });

            });
            searchThread.IsBackground = true;
            searchThread.Start();
        }

        private string yuzuFile = "";
        private string gameFile = "";

        private void SearchFile(string directory)
        {
            Dispatcher.Invoke(() =>
            {
                loading.Text = directory;
            });
            if (File.Exists(directory + "yuzu.exe"))
            {
                yuzuFile = directory + "yuzu.exe";
                Dispatcher.Invoke(() =>
                {
                    tb_YuzuUri.Text = yuzuFile;
                    isEdit = true;
                    CheckUserUri();
                });
            }
            else
            {
                try
                {
                    var files = Directory.GetFiles(directory);
                    foreach (var tempFile in files)
                    {
                        string fileName = System.IO.Path.GetFileName(tempFile);
                        if ((fileName.IndexOf(".xci") > 0 || fileName.IndexOf(".nsp") > 0) && fileName.ToLower().Replace(" ","").IndexOf("ringfitadventure") >= 0)
                        {
                            gameFile = tempFile;
                            Dispatcher.Invoke(() =>
                            {
                                tb_GameUri.Text = gameFile;
                                isEdit = true;
                            });
                            break;
                        }
                    }
                }
                catch { }

            }
            if (yuzuFile != "" && gameFile != "")
                return;

            try
            {
                var folders = Directory.GetDirectories(directory);
                foreach (var folder in folders)
                {
                    SearchFile(folder + "\\");
                }
            }
            catch { }

        }

        public static List<string> GetDisk()
        {
            var drivers = DriveInfo.GetDrives();
            List<string> ls = new List<string>();
            foreach (var driver in drivers)
            {
                if (driver.DriveType != DriveType.Fixed)
                {
                    continue;
                }
                ls.Add(driver.Name);
            }
            return ls;
        }

        private void btn_Search_Click(object sender, RoutedEventArgs e)
        {
            AutoSearch();
        }

        private void Loading(bool load)
        {
            Dispatcher.Invoke(() =>
            {
                if (load)
                {
                    loading.Visibility = Visibility.Visible;
                }
                else
                {
                    loading.Visibility = Visibility.Hidden;
                }
            });
        }
    }
}
