using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using log4net;

using scte_104_inserter.vo;
using System.Windows.Controls.Primitives;
using System.Net;
using System.Diagnostics;

namespace scte_104_inserter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private ArrayList m_logList;
        //private int _event_id;

        private bool _ShouldStop;
        private int _eventID;

        public String _ip { get; set; }
        public Int16 _port { get; set; }

        public enum SpliceInsertType
        {
            Reserve = 0,
            Start_Normal = 1,
            Start_Immediate = 2,
            End_Normal = 3,
            End_Immediate = 4,
            Cancel = 5
        }

        //클래스당 1개 명시?? (확실한가?)
        private static readonly ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            InitializeComponent();
            InitializeOthers();
            LoadConfig();
            logger.Info("Program Started...");
        }

        private bool LoadConfig()
        {
            // config.json 읽기
            String jsonString;
            try
            {
                JsonConfig jsonConfig = JsonConfig.getInstance();
                if (!File.Exists(jsonConfig.configFileName))
                {
                    MessageBox.Show("config.json 파일이 없습니다.\n환경설정 파일을 읽지 못했습니다.\n기본값으로 설정합니다.", "경고", MessageBoxButton.OK);
                    jsonConfig.eventId = 0;
                    TbEventID.Text = "0";

                    jsonString = JsonSerializer.Serialize(jsonConfig);
                    File.WriteAllText(jsonConfig.configFileName, jsonString);
                }
                else
                {
                    jsonString = File.ReadAllText(jsonConfig.configFileName);
                    jsonConfig = JsonSerializer.Deserialize<JsonConfig>(jsonString);

                    TbIpaddr.Text = jsonConfig.ipAddr;
                    TbPort.Text = jsonConfig.port.ToString();

                    _ip = jsonConfig.ipAddr;
                    _port = jsonConfig.port;

                    TbEventID.Text = jsonConfig.eventId.ToString();
                    _eventID = jsonConfig.eventId;
                    TbUnqProgramID.Text = jsonConfig.uniquePid.ToString();
                    TbPreroolTime.Text = jsonConfig.preRollTime.ToString();
                    TbBreakDuration.Text = jsonConfig.breakDuration.ToString();

                    /*
					TxtBoxInputPath.Text = jsonConfig.CurrentInputPath;
					TxtBoxOutputPath.Text = jsonConfig.CurrentOutputPath;
					ChkIsInputPathReculsive.IsChecked = jsonConfig.IsRecursive;
					*/
                }
            }
            catch (FileLoadException e)
            {
                MessageBox.Show(e.ToString() + "\n\n프로그램을 종료합니다.", "경고", MessageBoxButton.OK);
            }

            return true;
        }

        private void InitializeOthers()
        {
            _ShouldStop = false;
        }

        private Byte[] MakePayload(Scte104 scte104)
        {
            Byte[] payload = new Byte[30];

            payload[0] = 0xFF;
            payload[1] = 0xFF;
            //message size
            payload[2] = 0x00;
            payload[3] = 0x1E;
            //protocol_version
            payload[4] = 0x00;
            //AS_index
            payload[5] = 0x00;
            //message_number
            payload[6] = 0x00;
            //DPI_PID_index
            payload[7] = 0x00;
            payload[8] = 0x00;
            //SCTE35_protocol_version
            payload[9] = 0x00;
            //timestamp = 0x00;
            payload[10] = 0x00;
            //num_ops
            payload[11] = 0x01;
            //opID 0x0101 splice_request_data()
            payload[12] = 0x01;
            payload[13] = 0x01;
            //data length 0E = 14
            payload[14] = 0x00;
            payload[15] = 0x0e;

            // splice_insert_type
            payload[16] = (byte)scte104.insertType;
            //splice event id : 0x1234
            int event_id = scte104.eventId;
            payload[17] = (byte)(event_id >> 24);
            payload[18] = (byte)(event_id >> 16);
            payload[19] = (byte)(event_id >> 8);
            payload[20] = (byte)event_id;

            // unique program id : 0x4567
            Int16 uid = Convert.ToInt16(scte104.uniquePid);
            payload[21] = (byte)(uid >> 8);
            payload[22] = (byte)uid;
            // pre_roll_time
            Int16 pre_roll_time = Convert.ToInt16(scte104.prerollTime);
            payload[23] = (byte)(pre_roll_time >> 8);
            payload[24] = (byte)pre_roll_time;
            // break duration
            Int16 break_duration = Convert.ToInt16(scte104.breakDuration);
            payload[25] = (byte)(break_duration >> 8);
            payload[26] = (byte)break_duration;
            //avail_num //fixed
            payload[27] = 0x01;
            //avais_expected //fixed
            payload[28] = 0x02;
            //auto return flag //fixed
            payload[29] = 0x01;

            return payload;
        }

        private void BtnCue_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            String btnName = button.Name;

            Scte104 scte104 = new Scte104();

            switch (btnName)
            {
                case "BtnCue_1":
                    try
                    {
                        _eventID++;
                        //TbEventID.Text = (Convert.ToInt32(TbEventID.Text) + 4).ToString();
                        TbEventID.Text = _eventID.ToString();
                        //Clock clk = new Clock();
                        //lvLog.eventTime = clk.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        //log.ipAddress = common.Util.GetLocalIpAddress();
                        //scte104.ipAddress = TbIpaddr.Text;
                        //scte104.port = Convert.ToInt32(TbPort.Text);

                        scte104.ipAddress = TbIpaddr.Text;
                        scte104.port = Convert.ToInt16(TbPort.Text);
                        _ip = TbIpaddr.Text;
                        _port = Convert.ToInt16(TbPort.Text);

                        RadioButton eventRB = (from element in EventTypePanel.Children.Cast<UIElement>()
                                               where element is RadioButton && (element as RadioButton).IsChecked.Value
                                               select element).SingleOrDefault() as RadioButton;
                        bool check = eventRB.IsChecked.Value;
                        String content = eventRB.Content as String;
                        if (check == true)
                        {
                            if (content == "Reserve")
                            {
                                scte104.insertType = (int)SpliceInsertType.Reserve;
                            }
                            else if (content == "Start Normal")
                            {
                                scte104.insertType = (int)SpliceInsertType.Start_Normal;
                            }
                            else if (content == "Start Immediate")
                            {
                                scte104.insertType = (int)SpliceInsertType.Start_Immediate;
                            }
                            else if (content == "End Normal")
                            {
                                scte104.insertType = (int)SpliceInsertType.End_Normal;
                            }
                            else if (content == "End Immediate")
                            {
                                scte104.insertType = (int)SpliceInsertType.End_Immediate;
                            }
                            else if (content == "Cancel")
                            {
                                scte104.insertType = (int)SpliceInsertType.Cancel;
                            }
                            //_eventType = content;
                            scte104.eventType = content;
                        }

                        scte104.eventId = _eventID;
                        scte104.uniquePid = Convert.ToInt16(TbUnqProgramID.Text);
                        scte104.prerollTime = Convert.ToInt16(TbPreroolTime.Text);
                        scte104.breakDuration = Convert.ToInt16(TbBreakDuration.Text);
                    }
                    catch (Exception exFormat)
                    {
                        MessageBox.Show(exFormat.Message, "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (SendToCard(scte104))
                    {
                        scte104.status = "Completed";
                    }
                    else
                    {
                        scte104.status = "Error";
                    }
                    DisplayListView(scte104);
                    break;

                case "BtnOther":
                    break;
            }
        }

        private void DisplayListView(Scte104 scte104)
        {
            scte104.eventTime = DateTime.Now.AddMilliseconds(16).ToString("yyyy-MM-dd HH:mm:ss.fff");
            scte104.SetIncrease();
            Scte104.GetList().Add(scte104.ShallowCopy());
            scte104.WriteLvLog();

            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(
                    delegate
                    {
                        LvLog_1.ItemsSource = null;
                        LvLog_1.ItemsSource = Scte104.GetList();
                        LvLog_1.ScrollIntoView(LvLog_1.Items[LvLog_1.Items.Count - 1]);
                    })
                );

            JsonConfig jsonConfig = JsonConfig.getInstance();
            jsonConfig.ipAddr = scte104.ipAddress;
            jsonConfig.port = scte104.port;
            jsonConfig.eventId = Convert.ToInt32(scte104.eventId);
            jsonConfig.uniquePid = Convert.ToInt16(scte104.uniquePid);
            jsonConfig.preRollTime = Convert.ToInt16(scte104.prerollTime);
            jsonConfig.breakDuration = Convert.ToInt16(scte104.breakDuration);

            String jsonString = JsonSerializer.Serialize(jsonConfig);
            File.WriteAllText(jsonConfig.configFileName, jsonString);
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("test");
        }

        private bool SendToCard(Scte104 scte104)
        {
            Byte[] payload = MakePayload(scte104);
            try
            {
                util.Network conn = new util.Network();
                //conn.SetConnection(TbIpaddr.Text, Convert.ToInt32(TbPort.Text));
                conn.SetConnection(scte104.ipAddress, scte104.port);
                conn.SetTimeout(10);
                conn.SetPayload(payload);
                if (conn.Send())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void TbPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
            {
                MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
            }
        }

        private void TbUnqProgramID_TextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
            {
                MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
            }
        }

        private void TbPreroolTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
            {
                MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
            }
        }

        private void TbBreakDuration_TextChanged(object sender, TextChangedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
            {
                MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
                thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
            }
        }

        private void TbUnqProgramID_GotFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(
                    delegate
                    {
                        thisTextBox.SelectAll();
                    })
                );
        }

        private void TbPreroolTime_GotFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(
                    delegate
                    {
                        thisTextBox.SelectAll();
                    })
                );
        }

        private void TbBreakDuration_GotFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(
                    delegate
                    {
                        thisTextBox.SelectAll();
                    })
                );
        }

        private void TbIpaddr_GotFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(
                    delegate
                    {
                        thisTextBox.SelectAll();
                    })
                );
        }

        private void TbPort_GotFocus(object sender, RoutedEventArgs e)
        {
            var thisTextBox = sender as System.Windows.Controls.TextBox;
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ContextIdle,
                new Action(
                    delegate
                    {
                        thisTextBox.SelectAll();
                    })
                );
        }

        private Scte104 GetLvItem(RoutedEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (!(dep is System.Windows.Controls.ListViewItem))
            {
                try
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                catch
                {
                    return null;
                }
            }
            ListViewItem item = (ListViewItem)dep;
            Scte104 content = (Scte104)item.Content;
            return content;
        }

        private void lvBtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Scte104 scte104 = GetLvItem(e);
            //MessageBox.Show(String.Format("{0} {1}", scte104.eventId, scte104.index));
            scte104.insertType = (int)SpliceInsertType.Cancel;
            scte104.eventType = "Cancel";
            scte104.prerollTime = 0;
            scte104.breakDuration = 0;
            Byte[] payload = MakePayload(scte104);

            //tcp
            util.Network conn = new util.Network();
            conn.SetConnection(TbIpaddr.Text, Convert.ToInt32(TbPort.Text));
            conn.SetPayload(payload);
            if (conn.Send())
            {
                scte104.status = "Completed";
            }
            else
            {
                scte104.status = "Error";
            }

            scte104.eventTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Scte104.GetList()[scte104.index] = scte104;

            LvLog_1.ItemsSource = null;
            LvLog_1.ItemsSource = Scte104.GetList();
        }

        private void BtnLogClear_Click(object sender, RoutedEventArgs e)
        {
            LvLog_1.ItemsSource = null;
            Scte104.GetList().Clear();
            Scte104._index = 0;
        }

        private void ToggleAuto_Changed(object sender, RoutedEventArgs e)
        {
            ToggleButton obj = sender as ToggleButton;
            switch (obj.IsChecked)
            {
                case true:
                    String stateText = "자동 실행 중...";
                    obj.Content = "중단";
                    BtnCue_1.Content = stateText;
                    BtnCue_1.IsEnabled = false;
                    _ShouldStop = false;
                    LvLog_1.IsEnabled = false;
                    Automation();
                    break;

                case false:
                    _ShouldStop = true;
                    obj.Content = "프리셋";
                    break;
            }
        }

        private async void Automation()
        {
            var taskAutomation = Task.Run(() => SendPresetAsync());
            bool result = await taskAutomation;
            if (result)
            {
                LvLog_1.IsEnabled = true;
                BtnCue_1.IsEnabled = true;
                BtnCue_1.Content = "Cue";
                MessageBox.Show("프리셋 실행이 중단 되었습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool SendPresetAsync()
        {
            List<Scte104> scte104s = new List<Scte104>();

            _eventID++;
            Scte104 scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.Start_Immediate.ToString(), insertType = (int)SpliceInsertType.Start_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.End_Immediate.ToString(), insertType = (int)SpliceInsertType.End_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.Start_Immediate.ToString(), insertType = (int)SpliceInsertType.Start_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.End_Immediate.ToString(), insertType = (int)SpliceInsertType.End_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.Start_Immediate.ToString(), insertType = (int)SpliceInsertType.Start_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.End_Immediate.ToString(), insertType = (int)SpliceInsertType.End_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.Start_Immediate.ToString(), insertType = (int)SpliceInsertType.Start_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.End_Immediate.ToString(), insertType = (int)SpliceInsertType.End_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.Start_Immediate.ToString(), insertType = (int)SpliceInsertType.Start_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.End_Immediate.ToString(), insertType = (int)SpliceInsertType.End_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.Start_Immediate.ToString(), insertType = (int)SpliceInsertType.Start_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            _eventID++;
            scte104 = new Scte104() { ipAddress = _ip, port = _port, eventType = SpliceInsertType.End_Immediate.ToString(), insertType = (int)SpliceInsertType.End_Immediate, eventId = _eventID, uniquePid = 1234, prerollTime = 0, breakDuration = 0 };
            scte104s.Add(scte104);

            int[] sleepArray = { 10, 100, 30, 100, 100, 100, 1, 1, 10, 10, 10, 0 };

            //sample
            //int[] sleepArray = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 };

            foreach (Scte104 scte104_elem in scte104s)
            {
                int index = scte104s.IndexOf(scte104_elem);

                Debug.WriteLine(index, sleepArray[index].ToString());

                if (SendToCard(scte104_elem))
                {
                    scte104_elem.status = "Completed";
                }
                else
                {
                    scte104_elem.status = "Error";
                }
                DisplayListView(scte104_elem);
                Thread.Sleep(sleepArray[index] * 1000);
            }

            return true;
        }

        private bool SendAutoAync()
        {
            List<Scte104> scte104_job = new List<Scte104>();

            Scte104 scte104;
            scte104 = new Scte104();

            scte104.ipAddress = _ip;
            scte104.port = _port;

            scte104.eventType = SpliceInsertType.Start_Normal.ToString();
            scte104.insertType = (int)SpliceInsertType.Start_Normal;
            scte104.eventId = _eventID;
            scte104.uniquePid = 1234;
            scte104.prerollTime = 3000;
            scte104.breakDuration = 50;

            scte104_job.Add(scte104);

            scte104 = new Scte104();
            scte104.ipAddress = _ip;
            scte104.port = _port;
            scte104.eventType = SpliceInsertType.Start_Immediate.ToString();
            scte104.insertType = (int)SpliceInsertType.Start_Immediate;
            scte104.uniquePid = 2345;
            scte104.prerollTime = 0;
            scte104.breakDuration = 0;
            scte104_job.Add(scte104);

            scte104 = new Scte104();

            scte104.ipAddress = _ip;
            scte104.port = _port;
            scte104.eventType = SpliceInsertType.End_Normal.ToString();
            scte104.insertType = (int)SpliceInsertType.End_Normal;
            scte104.uniquePid = 3456;
            scte104.prerollTime = 3000;
            scte104.breakDuration = 0;
            scte104_job.Add(scte104);

            scte104 = new Scte104();

            scte104.ipAddress = _ip;
            scte104.port = _port;
            scte104.eventType = SpliceInsertType.End_Immediate.ToString();
            scte104.insertType = (int)SpliceInsertType.End_Immediate;
            scte104.uniquePid = 4567;
            scte104.prerollTime = 0;
            scte104.breakDuration = 0;
            scte104_job.Add(scte104);

            scte104 = new Scte104();

            scte104.ipAddress = _ip;
            scte104.port = _port;
            scte104.eventType = SpliceInsertType.End_Immediate.ToString();
            scte104.insertType = (int)SpliceInsertType.End_Immediate;
            scte104.uniquePid = 4567;
            scte104.prerollTime = 0;
            scte104.breakDuration = 0;
            scte104_job.Add(scte104);

            scte104 = new Scte104();
            scte104.ipAddress = _ip;
            scte104.port = _port;
            scte104.eventType = SpliceInsertType.Cancel.ToString();
            scte104.insertType = (int)SpliceInsertType.Cancel;
            scte104.uniquePid = 4567;
            scte104.prerollTime = 0;
            scte104.breakDuration = 0;
            scte104_job.Add(scte104);

            int tick = -1;
            while (!_ShouldStop)
            {
                foreach (Scte104 scte104_elem in scte104_job)
                {
                    while (!_ShouldStop)
                    {
                        tick++;
                        if (tick % (10 * 60 * 30) != 0)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (_ShouldStop)
                    {
                        break;
                    }
                    if (scte104_elem.insertType != (int)SpliceInsertType.Cancel)
                    {
                        _eventID++;
                    }
                    scte104_elem.eventId = _eventID;
                    if (SendToCard(scte104_elem))
                    {
                        scte104_elem.status = "Completed";
                    }
                    else
                    {
                        scte104_elem.status = "Error";
                    }
                    DisplayListView(scte104_elem);
                }
                tick = 0;
            }
            return _ShouldStop;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _ShouldStop = true;
        }
    }
}