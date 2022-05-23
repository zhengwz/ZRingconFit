using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZRingconFit
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread searthThread;
        private Thread aliveThread;
        private Thread getStatusThread;
        private Thread getRinconValueThread;
        private Thread autoStartThread;
        private bool ConfigMode = false;
        private bool RingconStarted = false;
        private bool checkDriver = false;
        private double RingconValue = 0.00;

        private HashSet<string> added = new HashSet<string>();

        private BluetoothDeviceInfo JC_Left;
        private BluetoothDeviceInfo JC_Right;


        [DllImport("ZRingDriver.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int APIStart();


        [DllImport("ZRingDriver.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ChangeMode(bool config);


        [DllImport("ZRingDriver.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetStatus();


        [DllImport("ZRingDriver.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetRingconReferenceValue();


        [DllImport("ZRingDriver.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetRingconValue();


        public MainWindow()
        {
            Global.LoadConfig();
            InitializeComponent();
            VersionLabel.Content = "V" + Application.ResourceAssembly.GetName().Version.ToString(2);
            btn_AutoStartGame.IsChecked = Global.AutoStartGame;
            ShowNotifyIcon();

            Task.Factory.StartNew(() =>
            {
                CheckDrivers();

                Dispatcher.Invoke(() =>
                {
                    searthThread = new Thread(SearchBluetooth);
                    searthThread.IsBackground = true;
                    searthThread.Start();

                    aliveThread = new Thread(AliveBluetooth);
                    aliveThread.IsBackground = true;
                    aliveThread.Start();

                    getRinconValueThread = new Thread(GetRingconValueThread);
                    getRinconValueThread.IsBackground = true;
                    getRinconValueThread.Start();

                    autoStartThread = new Thread(ProcessBarThread);
                    autoStartThread.IsBackground = true;
                    autoStartThread.Start();
                });

            });


        }

        private void ProcessBarThread()
        {
            double value = 0;
            while(true)
            {
                if(RingconStarted)
                {
                    if (RingconValue > 3)
                    {
                        if (value < 50)
                            value += 1;
                    }
                    else if (value > 0)
                    {
                        value -= 1;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        pb_StartGame.Value = value;                        
                    });
                    if (value >= 50 && Global.AutoStartGame)
                    {
                        break;
                    }
                    Thread.Sleep(20);
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
            StartGame(ConfigMode);
        }
        //C:\Windows\System32\drivers\ViGEmBus.sys
        //C:\Windows\System32\DriverStore\FileRepository

        private void CheckDrivers()
        {
            checkDriver = false;
            bool has86 = false, has64 = false;
            bool first = true;
            recheck:
            //if (File.Exists(@"C:\Windows\System32\drivers\ViGEmBus.sys"))//未知情况下判断不到，不论是否以管理员身份启动
            {
                var folders = Directory.GetDirectories(@"C:\Windows\System32\DriverStore\FileRepository").Where(x => x.IndexOf("vigembus.inf") >= 0);
                foreach(var folder in folders)
                {
                    if (Directory.Exists(folder + "\\x64"))
                    {
                        if (has86 == false)
                            first = true;
                        has86 = true;
                    }
                    else
                    {
                        if (has64 == false)
                            first = true;
                        has64 = true;
                    }
                }
            }
            try
            {
                //if (!has86 || !has64)
                if (!has86 && !has64)
                {
                    if (!first)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("未安装驱动不允许运行");
                            notifyIcon.Visible = false;
                            Application.Current.Shutdown();
                        });
                    }
                    else
                    {
                        first = false;
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("请安装弹出的驱动程序，两个驱动均要安装（x86、x64）");
                            lb_JC.Content = "请先安装驱动";
                        });
                        Process proc = Process.Start(System.Environment.CurrentDirectory + $"\\Drivers\\ViGEmBusSetup_x{(!has86 ? "86" : "64")}.msi");
                        if (proc != null)
                        {
                            proc.WaitForExit();
                            has86 = false;
                            goto recheck;
                        }
                    }
                }
                //else
                //{
                    checkDriver = true;
                //}
            }
            catch
            { }
        }

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private void ShowNotifyIcon()
        {
            this.notifyIcon = new System.Windows.Forms.NotifyIcon();
            this.notifyIcon.BalloonTipText = "ZRingconDriver";
            this.notifyIcon.Text = "ZRingconDriver";
            this.notifyIcon.Icon = Properties.Resources.icon;
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
        }

        private void AliveBluetooth()
        {
            while (true)
            {
                if (JC_Left != null)
                {
                    JC_Left.Refresh();
                    if (JC_Left.Connected)
                    {
                        if (!added.Contains(JC_Left.DeviceName))
                        {
                            added.Add(JC_Left.DeviceName);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                image_Left.Source = new BitmapImage(new Uri("pack://application:,,,/ZRingconFit;component/Resources/Left1.png"));
                            }));
                        }
                    }
                    else
                    {
                        if (added.Contains(JC_Left.DeviceName))
                        {
                            added.Remove(JC_Left.DeviceName);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                image_Left.Source = new BitmapImage(new Uri("pack://application:,,,/ZRingconFit;component/Resources/Left0.png"));
                            }));
                        }
                        JC_Left = null;
                    }
                }


                if (JC_Right != null)
                {
                    JC_Right.Refresh();
                    if (JC_Right.Connected)
                    {
                        if (!added.Contains(JC_Right.DeviceName))
                        {
                            added.Add(JC_Right.DeviceName);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                image_Right.Source = new BitmapImage(new Uri("pack://application:,,,/ZRingconFit;component/Resources/Right1.png"));
                            }));
                        }
                    }
                    else
                    {
                        if (added.Contains(JC_Right.DeviceName))
                        {
                            added.Remove(JC_Right.DeviceName);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                image_Right.Source = new BitmapImage(new Uri("pack://application:,,,/ZRingconFit;component/Resources/Right0.png"));
                            }));
                        }
                        JC_Right = null;
                    }
                }

                if ((JC_Left != null && JC_Left.Connected) && (JC_Right != null && JC_Right.Connected))
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "手柄已连接";
                    });

                    Thread.Sleep(1000);

                    Dispatcher.Invoke(() =>
                    {
                        {
                            grid_JC.Visibility = Visibility.Hidden;
                            grid_Ringcon.Visibility = Visibility.Visible;

                            if (getStatusThread == null)
                            {
                                getStatusThread = new Thread(GetStatusThread);
                                getStatusThread.IsBackground = true;
                                getStatusThread.Start();
                            }

                        }
                    });
                    Task.Factory.StartNew(() =>
                    {
                        StartRingcon();
                    });
                    break;
                }
                else if (JC_Left != null && JC_Left.Connected)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "请连接右手柄";
                        grid_JC.Visibility = Visibility.Visible;
                        grid_Ringcon.Visibility = Visibility.Hidden;
                    });
                }
                else if (JC_Right != null && JC_Right.Connected)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "请连接左手柄";
                        grid_JC.Visibility = Visibility.Visible;
                        grid_Ringcon.Visibility = Visibility.Hidden;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "请按手柄任意键进行连接";
                        grid_JC.Visibility = Visibility.Visible;
                        grid_Ringcon.Visibility = Visibility.Hidden;
                    });
                }

                Thread.Sleep(500);
            }
        }
        private void SearchBluetooth()
        {
            BluetoothClient client = new BluetoothClient();
            bool pair_left = false, pair_right = false;
            while (true)
            {
                var devices = client.PairedDevices;
                foreach (var device in devices)
                {
                    device.Refresh();
                    if (device.DeviceName == "Joy-Con (R)")
                    {
                        if (device.Connected && JC_Right == null)
                        {
                            JC_Right = device;
                        }
                        pair_left = true;
                    }
                    else if (device.DeviceName == "Joy-Con (L)")
                    {
                        if (device.Connected && JC_Left == null)
                        {
                            JC_Left = device;
                        }
                        pair_right = true;
                    }
                }
                //if (JC_Left != null && JC_Right != null)
                //    break;
                if (!pair_left && !pair_right)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "请先配对手柄";
                        grid_JC.Visibility = Visibility.Visible;
                        grid_Ringcon.Visibility = Visibility.Hidden;
                    });
                }
                else if (!pair_left)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "左手柄还未配对，请先配对手柄";
                        grid_JC.Visibility = Visibility.Visible;
                        grid_Ringcon.Visibility = Visibility.Hidden;
                    });
                }
                else if (!pair_right)
                {
                    Dispatcher.Invoke(() =>
                    {
                        lb_JC.Content = "右手柄还未配对，请先配对手柄";
                        grid_JC.Visibility = Visibility.Visible;
                        grid_Ringcon.Visibility = Visibility.Hidden;
                    });
                }
                Thread.Sleep(500);
            }
        }

        private void OpenBluetoothSet()
        {
            var process = new Process { StartInfo = { FileName = "control", Arguments = "bthprops.cpl" } };
            process.Start();
        }

        private void GetStatusThread()
        {
            while (true)
            {
                int status = GetStatus();
                bool complete = false;
                switch (status)
                {
                    case -1:
                        Dispatcher.Invoke(() =>
                        {
                            lb_Ringcon.Content = "检测到手柄但未检测到环";
                        });
                        break;
                    case 0:
                        Dispatcher.Invoke(() =>
                        {
                            lb_Ringcon.Content = "正在校验手柄数据";
                        });
                        break;
                    case 1:
                        Dispatcher.Invoke(() =>
                        {
                            lb_Ringcon.Content = "成功读取到环数据";
                        });
                        break;
                    case 2:
                        Dispatcher.Invoke(() =>
                        {
                            lb_Ringcon.Content = "正在校准环默认值，请静置环...";
                        });
                        break;
                    case 3:
                        Dispatcher.Invoke(() =>
                        {
                            double ReferenceValue = GetRingconReferenceValue();
                            lb_Ringcon.Content = $"校准成功（{ReferenceValue:0.00}）";
                        });
                        complete = true;
                        RingconStarted = true;
                        break;
                }
                if (complete)
                    break;
                Thread.Sleep(50);
            }
        }

        private void GetRingconValueThread()
        {
            while (true)
            {
                if (RingconStarted)
                {
                    RingconValue = GetRingconValue();

                    Dispatcher.Invoke(() =>
                    {
                        lb_RingconValue.Content = RingconValue.ToString("0.00");
                    });
                    Thread.Sleep(20);
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void StartRingcon()
        {
            Thread thread = new Thread(() =>
            {
                APIStart();
            });
            thread.IsBackground = true;
            thread.Start();
            //Task.Factory.StartNew(() =>
            //{
            //    APIStart();
            //});
        }

        private void btn_ConnectBluetooth_Click(object sender, RoutedEventArgs e)
        {
            OpenBluetoothSet();
        }

        private void btn_ChangeMode_Click(object sender, RoutedEventArgs e)
        {
            ConfigMode = !ConfigMode;
            ChangeMode(ConfigMode);
            if (ConfigMode)
            {
                btn_ChangeMode.Content = "配置模式";
                btn_OpenYuzu.Content = "打开yuzu";
            }
            else
            {
                btn_ChangeMode.Content = "游戏模式";
                btn_OpenYuzu.Content = "运行游戏";
            }
        }

        private void btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            var setting = new SettingWindow();
            setting.Owner = this;
            setting.StartGame += Setting_StartGame;
            setting.ShowDialog();
        }

        private void Setting_StartGame()
        {
            if (RingconStarted)
                StartGame();
        }

        private void ReplaceConfig()
        {
            if (Global.UserUri == "")
                return;
            string folder = System.Environment.CurrentDirectory + "\\YuzuConfig\\";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string configPath = Global.UserUri + "config\\qt-config.ini";
            if (File.Exists(configPath) && File.Exists(folder + "\\qt-config.ini"))
            {
                File.Copy(folder + "\\qt-config.ini", configPath, true);
            }
        }

        private void StartGame(bool config = false)
        {
            if (Global.YuzuUri == "" || Global.GameUri == "")
                return;
            StopProcess("yuzu");
            if (Global.ReplaceConfig)
            {
                ReplaceConfig();
            }
            Process process = new Process
            {
                StartInfo =
                {
                    FileName = Global.YuzuUri,
                    Arguments = config ? "" : $"-f -g \"{Global.GameUri}\""
                }
            }; 

            Task.Factory.StartNew(() =>
            {
                restart:
                try
                {
                    process.Start();
                }
                catch { }
                Loading(true);
                int i = 0;
                while(true)
                {
                    i++;
                    Thread.Sleep(1000);
                    if(CheckProcess("yuzu"))
                    {
                        break;
                    }
                    else if (i >= 10)
                    {
                        goto restart;
                    }
                }
                Thread.Sleep(15000);
                Loading(false);
                Dispatcher.Invoke(() =>
                {
                    this.Hide();
                });
                process.WaitForExit();
                Dispatcher.Invoke(() =>
                {
                    searthThread.Abort();
                    aliveThread.Abort();
                    getRinconValueThread.Abort();
                    notifyIcon.Visible = false;
                    this.Close();
                    //Application.Current.Shutdown();
                });
            });

        }

        private void btn_OpenYuzu_Click(object sender, RoutedEventArgs e)
        {
            StartGame(ConfigMode);
        }

        private void btn_AutoStartGame_Checked(object sender, RoutedEventArgs e)
        {
            Global.AutoStartGame = btn_AutoStartGame.IsChecked.Value;
            Global.SaveConfig();
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

        private void btn_InstallDriver_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                CheckDrivers();

                Process proc86 = Process.Start(Environment.CurrentDirectory + $"\\Drivers\\ViGEmBusSetup_x86.msi");
                if (proc86 != null)
                {
                    proc86.WaitForExit();
                }
                Process proc64 = Process.Start(Environment.CurrentDirectory + $"\\Drivers\\ViGEmBusSetup_x64.msi");
                if (proc64 != null)
                {
                    proc64.WaitForExit();
                }
                Dispatcher.Invoke(() =>
                {
                    //MessageBox.Show("驱动安装完成，重启程序后生效");
                    RestartSelf();
                });
            });
        }

        private void btn_Exit_Click(object sender, RoutedEventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Current.Shutdown();
        }

        private void btn_HideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private bool CheckProcess(string processName)
        {
            try
            {
                System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName(processName);
                if (ps.Count() > 0)
                    return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        private void StopProcess(string processName)
        {
            try
            {
                System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcessesByName(processName);
                foreach (System.Diagnostics.Process p in ps)
                {
                    p.Kill();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void RestartSelf()
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Assembly.GetExecutingAssembly().Location;

            try
            {
                System.Diagnostics.Process.Start(startInfo);
            }
            catch
            {
                return;
            }
            notifyIcon.Visible = false;
            //退出
            Application.Current.Shutdown();
        }
    }
}
