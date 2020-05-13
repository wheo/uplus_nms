﻿using System;
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
using scte_104_inserter.vo;

namespace scte_104_inserter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//private ArrayList m_logList;
		//private int _event_id;

		public enum SpliceInsertType
		{
			Reserve = 0,
			Start_Normal = 1,
			Start_Immediate = 2,
			End_Normal = 3,
			End_Immediate = 4,
			Cancel = 5
		}

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
			/*
			m_logList = new ArrayList();
			for (int i=0;i<4;i++)
			{
				LvLog listitem = new LvLog();
				m_logList.Add(listitem.GetList());
			}
			*/
		}
		
		private Byte[] MakePayload(Scte104 lvLog)
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
			payload[16] = (byte)lvLog.insertType;
			//splice event id : 0x1234
			int event_id = Convert.ToInt32(lvLog.eventId);
			payload[17] = (byte)(event_id >> 24);
			payload[18] = (byte)(event_id >> 16);
			payload[19] = (byte)(event_id >> 8);
			payload[20] = (byte)event_id;

			// unique program id : 0x4567
			int uid = Convert.ToInt16(lvLog.uniquePid);
			payload[21] = (byte)(uid >> 8);
			payload[22] = (byte)uid;
			// pre_roll_time 
			int pre_roll_time = Convert.ToInt16(lvLog.prerollTime);
			payload[23] = (byte)(pre_roll_time >> 8);
			payload[24] = (byte)pre_roll_time;
			// break duration
			int break_duration = Convert.ToInt16(lvLog.breakDuration);
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
					TbEventID.Text = (Convert.ToInt32(TbEventID.Text)+4).ToString();
					//Clock clk = new Clock();					
					//lvLog.eventTime = clk.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
					//log.ipAddress = common.Util.GetLocalIpAddress();
					scte104.ipAddress = TbIpaddr.Text;
					scte104.port = Convert.ToInt32(TbPort.Text);

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
					scte104.index = Scte104.GetLastIndex();
					scte104.eventId = TbEventID.Text;
					scte104.uniquePid = TbUnqProgramID.Text;
					scte104.prerollTime = TbPreroolTime.Text;
					scte104.breakDuration = TbBreakDuration.Text;

					//_event_id += + 4;
					//Byte[] payload = File.ReadAllBytes(@"message/spliceStart_immediate.bin");
					Byte[] payload = MakePayload(scte104);

					//tcp
					util.Network conn = new util.Network();
					conn.SetConnection(TbIpaddr.Text, Convert.ToInt32(TbPort.Text));
					conn.SetTimeout(10);
					conn.SetPayload(payload);
					if (conn.Connect())
					{
						scte104.status = "Completed";
					} else
					{
						scte104.status = "Error";
					}

					scte104.eventTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
					Scte104.GetList().Add(scte104);
					scte104.WriteLvLog();

					Scte104.IncreseIndex();

					LvLog_1.ItemsSource = null;
					LvLog_1.ItemsSource = Scte104.GetList();
					LvLog_1.ScrollIntoView(LvLog_1.Items[LvLog_1.Items.Count - 1]);

					vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
					jsonConfig.ipAddr = TbIpaddr.Text;
					jsonConfig.port = Convert.ToInt32(scte104.port);
					jsonConfig.eventId = Convert.ToInt32(scte104.eventId);
					jsonConfig.uniquePid = Convert.ToInt32(scte104.uniquePid);
					jsonConfig.preRollTime = Convert.ToInt32(scte104.prerollTime);
					jsonConfig.breakDuration = Convert.ToInt32(scte104.breakDuration);

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

		private Scte104 GetLvItem(RoutedEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			while(!(dep is System.Windows.Controls.ListViewItem))
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
			//content.index = 
			return content;
		}

		private void lvBtnCancel_Click(object sender, RoutedEventArgs e)
		{
			Scte104 scte104 = GetLvItem(e);
			//MessageBox.Show(String.Format("{0} {1}", scte104.eventId, scte104.index));
			scte104.insertType = (int)SpliceInsertType.Cancel;
			scte104.eventType = "Cancel";
			scte104.prerollTime = "0";
			scte104.breakDuration = "0";
			Byte[] payload = MakePayload(scte104);
		
			//tcp
			util.Network conn = new util.Network();
			conn.SetConnection(TbIpaddr.Text, Convert.ToInt32(TbPort.Text));
			conn.SetPayload(payload);
			if (conn.Connect())
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
	}
}
