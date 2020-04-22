using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace uplus_nms
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ArrayList m_logList;
		public MainWindow()
		{
			InitializeComponent();
			LoadConfig();
			InitializeOthers();
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

					jsonString = JsonSerializer.Serialize(jsonConfig);
					File.WriteAllText(jsonConfig.configFileName, jsonString);
				}
				else
				{
					jsonString = File.ReadAllText(jsonConfig.configFileName);
					jsonConfig = JsonSerializer.Deserialize<vo.JsonConfig>(jsonString);

					TbIpaddr.Text = jsonConfig.ipAddr;
					TbPort.Text = jsonConfig.port.ToString();
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
			catch (Exception e)
			{

			}

			return true;
		}
		

		private void InitializeOthers()
		{
			m_logList = new ArrayList();
			for (int i=0;i<4;i++)
			{
				vo.Log listitem = new vo.Log();
				m_logList.Add(listitem.GetList());
			}
		}

		private void BtnCue_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			String btnName = button.Name;

			List<vo.Log> logitem;
			vo.Log log = new vo.Log();

			switch (btnName)
			{
				case "BtnCue_1":

					Byte[] payload = File.ReadAllBytes(@"message/spliceStart_immediate.bin");
					//tcp
					util.Network conn = new util.Network();
					conn.SetConnection(TbIpaddr.Text, Convert.ToInt32(TbPort.Text));					
					conn.SetPayload(payload);
					if (conn.Connect())
					{
						log.status = "Completed";
					} else
					{
						log.status = "Error";
					}

					log.eventTime = DateTime.Now;
					//log.ipAddress = common.Util.GetLocalIpAddress();
					log.ipAddress = TbIpaddr.Text;
					log.port = Convert.ToInt32(TbPort.Text);
					log.eventType = cbEventType_1.Text;
					log.eventId = TbEventID.Text;
					log.uniquePid = TbUnqProgramID.Text;
					log.prerollTime = TbPreroolTime.Text;
					log.breakDuration = TbBreakDuration.Text;					

					logitem = (List<vo.Log>)m_logList[0];
					logitem.Add(log);

					LvLog_1.ItemsSource = null;
					LvLog_1.ItemsSource = logitem;                    
					LvLog_1.ScrollIntoView(LvLog_1.Items[LvLog_1.Items.Count - 1]);
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

		private void TbIpaddr_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;
			vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
			jsonConfig.ipAddr = thisTextBox.Text;
			String jsonString = JsonSerializer.Serialize(jsonConfig);
			File.WriteAllText(jsonConfig.configFileName, jsonString);
		}

		private void TbPort_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;
			vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
			jsonConfig.port = Convert.ToInt32(thisTextBox.Text);
			String jsonString = JsonSerializer.Serialize(jsonConfig);
			File.WriteAllText(jsonConfig.configFileName, jsonString);
		}

		private void TbEventID_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;
			vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
			jsonConfig.eventId = Convert.ToInt32(thisTextBox.Text);
			String jsonString = JsonSerializer.Serialize(jsonConfig);
			File.WriteAllText(jsonConfig.configFileName, jsonString);
		}

		private void TbUnqProgramID_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;
			vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
			jsonConfig.uniquePid = Convert.ToInt32(thisTextBox.Text);
			String jsonString = JsonSerializer.Serialize(jsonConfig);
			File.WriteAllText(jsonConfig.configFileName, jsonString);
		}

		private void TbPreroolTime_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;
			vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
			jsonConfig.preRollTime = Convert.ToInt32(thisTextBox.Text);
			String jsonString = JsonSerializer.Serialize(jsonConfig);
			File.WriteAllText(jsonConfig.configFileName, jsonString);
		}

		private void TbBreakDuration_TextChanged(object sender, TextChangedEventArgs e)
		{
			var thisTextBox = sender as System.Windows.Controls.TextBox;
			vo.JsonConfig jsonConfig = vo.JsonConfig.getInstance();
			jsonConfig.breakDuration = Convert.ToInt32(thisTextBox.Text);
			String jsonString = JsonSerializer.Serialize(jsonConfig);
			File.WriteAllText(jsonConfig.configFileName, jsonString);
		}
	}
}
