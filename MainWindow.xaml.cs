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

using scte104_cue_inserter.util;

namespace scte_104_inserter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ArrayList m_logList;
		//private int _event_id;
		private String _eventType;

		//파일당 1개 명시?? (확실한가?)
		private static readonly ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public MainWindow()
		{
			InitializeComponent();
			LoadConfig();
			InitializeOthers();
			logger.Info("Program Started...");
		}

		private bool LoadConfig()
		{			
			// config.json 읽기
			String jsonString;
			try
			{
				vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
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
					jsonConfig = JsonSerializer.Deserialize<vo.JsonConfig>(jsonString);

					TbIpaddr.Text = jsonConfig.ipAddr;
					TbPort.Text = jsonConfig.port.ToString();
					//TbEventID.Text = jsonConfig.eventId.ToString();
					TbEventID.Text = jsonConfig.eventId.ToString();
					//_event_id = jsonConfig.eventId;					
					TbEventID.Text = jsonConfig.eventId.ToString();
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
				MessageBox.Show(e.ToString()+"\n\n프로그램을 종료합니다.", "경고", MessageBoxButton.OK);
			}

			return true;
		}		

		private void InitializeOthers()
		{
			m_logList = new ArrayList();
			for (int i=0;i<4;i++)
			{
				vo.LvLog listitem = new vo.LvLog();
				m_logList.Add(listitem.GetList());
			}
		}
		
		private Byte[] MakePayload()
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
			int splice_insert_type = 0;

#if false
			if (cbEventType_1.Text == "Reserve")
			{
				splice_insert_type = 0;

			}
			else if (cbEventType_1.Text == "Start Normal")
			{
				splice_insert_type = 1;
			}
			else if (cbEventType_1.Text == "Start Immediate")
			{
				splice_insert_type = 2;
			}
			else if (cbEventType_1.Text == "End Normal")
			{
				splice_insert_type = 3;
			}
			else if (cbEventType_1.Text == "End Immediate")
			{
				splice_insert_type = 4;
			}
			else if (cbEventType_1.Text == "Cancel")
			{
				splice_insert_type = 5;
			}
#else
			RadioButton eventRB = (from element in EventTypePanel.Children.Cast<UIElement>()
								   where element is RadioButton && (element as RadioButton).IsChecked.Value
								   select element).SingleOrDefault() as RadioButton;
			bool check = eventRB.IsChecked.Value;
			String content = eventRB.Content as String;
			if ( check == true)
			{				
				if (content == "Reserve")
				{
					splice_insert_type = 0;

				}
				else if (content == "Start Normal")
				{
					splice_insert_type = 1;
				}
				else if (content == "Start Immediate")
				{
					splice_insert_type = 2;
				}
				else if (content == "End Normal")
				{
					splice_insert_type = 3;
				}
				else if (content == "End Immediate")
				{
					splice_insert_type = 4;
				}
				else if (content == "Cancel")
				{
					splice_insert_type = 5;
				}
				_eventType = content;
			}
#endif

			payload[16] = (byte)splice_insert_type;
			//splice event id : 0x1234
			int event_id = Convert.ToInt32(TbEventID.Text);
			payload[17] = (byte)(event_id >> 24);
			payload[18] = (byte)(event_id >> 16);
			payload[19] = (byte)(event_id >> 8);
			payload[20] = (byte)event_id;

			// unique program id : 0x4567
			int uid = Convert.ToInt16(TbUnqProgramID.Text);
			payload[21] = (byte)(uid >> 8);
			payload[22] = (byte)uid;
			// pre_roll_time 
			int pre_roll_time = Convert.ToInt16(TbPreroolTime.Text);
			payload[23] = (byte)(pre_roll_time >> 8);
			payload[24] = (byte)pre_roll_time;
			// break duration
			int break_duration = Convert.ToInt16(TbBreakDuration.Text);
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

			List<vo.LvLog> lvLogItem;
			vo.LvLog lvLog = new vo.LvLog();

			switch (btnName)
			{
				case "BtnCue_1":
					TbEventID.Text = (Convert.ToInt32(TbEventID.Text)+4).ToString();
					//_event_id += + 4;
					//Byte[] payload = File.ReadAllBytes(@"message/spliceStart_immediate.bin");
					Byte[] payload = MakePayload();

					//tcp
					util.Network conn = new util.Network();
					conn.SetConnection(TbIpaddr.Text, Convert.ToInt32(TbPort.Text));					
					conn.SetPayload(payload);
					if (conn.Connect())
					{
						lvLog.status = "Completed";
					} else
					{
						lvLog.status = "Error";
					}

					//Clock clk = new Clock();

					lvLog.eventTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
					//lvLog.eventTime = clk.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
					//log.ipAddress = common.Util.GetLocalIpAddress();
					lvLog.ipAddress = TbIpaddr.Text;
					lvLog.port = Convert.ToInt32(TbPort.Text);
					//lvLog.eventType = cbEventType_1.Text;
					lvLog.eventType = _eventType;
					lvLog.eventId = TbEventID.Text;
					lvLog.uniquePid = TbUnqProgramID.Text;
					lvLog.prerollTime = TbPreroolTime.Text;
					lvLog.breakDuration = TbBreakDuration.Text;

					lvLogItem = (List<vo.LvLog>)m_logList[0];
					lvLogItem.Add(lvLog);
					lvLog.WriteLvLog();

					LvLog_1.ItemsSource = null;
					LvLog_1.ItemsSource = lvLogItem;
					LvLog_1.ScrollIntoView(LvLog_1.Items[LvLog_1.Items.Count - 1]);

					vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
					jsonConfig.ipAddr = TbIpaddr.Text;
					jsonConfig.port = Convert.ToInt32(lvLog.port);
					jsonConfig.eventId = Convert.ToInt32(lvLog.eventId);
					jsonConfig.uniquePid = Convert.ToInt32(lvLog.uniquePid);
					jsonConfig.preRollTime = Convert.ToInt32(lvLog.prerollTime);
					jsonConfig.breakDuration = Convert.ToInt32(lvLog.breakDuration);

					String jsonString = JsonSerializer.Serialize(jsonConfig);
					File.WriteAllText(jsonConfig.configFileName, jsonString);
					break;
				case "BtnOther":
					/*
					logitem = (List<vo.Log>)m_logList[1];
					logitem.Add(log);
					LvLog_2.ItemsSource = null;
					LvLog_2.ItemsSource = logitem;
					*/
					break;                
			}
		}
	  
		private void ContextMenu_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("click");
		}

		private void TbPort_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;

			if ( String.IsNullOrEmpty(thisTextBox.Text))
			{
				thisTextBox.Text = "0";
			}

			if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
			{
				MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK);
				thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
			}
		}		

		private void TbUnqProgramID_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;

			if (String.IsNullOrEmpty(thisTextBox.Text))
			{
				thisTextBox.Text = "0";
			}

			if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
			{
				MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK);
				thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
			}
		}

		private void TbPreroolTime_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;

			if (String.IsNullOrEmpty(thisTextBox.Text))
			{
				thisTextBox.Text = "0";
			}

			if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
			{
				MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK);
				thisTextBox.Text = thisTextBox.Text.Remove(thisTextBox.Text.Length - 1);
			}			
		}

		private void TbBreakDuration_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;

			if (String.IsNullOrEmpty(thisTextBox.Text))
			{
				thisTextBox.Text = "0";
			}

			if (Regex.IsMatch(thisTextBox.Text, "[^0-9]"))
			{
				MessageBox.Show("숫자만 넣을 수 있습니다.", "경고", MessageBoxButton.OK);
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
	}
}
