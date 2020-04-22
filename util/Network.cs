using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Windows;

namespace uplus_nms.util
{
	class Network
	{
		String _serverIpAddr;
		Int32 _port;
		Byte[] _payload;
		public Network()
		{
			_serverIpAddr = String.Empty;
			_port = -1;            
		}
		public void SetConnection(String ip, Int32 port)
		{
			this._serverIpAddr = ip;
			this._port = port;
		}

		public void SetPayload(Byte[] payload)
		{
			_payload = payload;
		}

		public bool Connect()
		{
			if (String.IsNullOrEmpty(this._serverIpAddr))
			{
				MessageBox.Show("서버 ip를 입력하세요", "경고", MessageBoxButton.OK);
				return false;
			} else if (this._port == -1)
			{
				MessageBox.Show("서버 port를 입력하세요", "경고", MessageBoxButton.OK);
				return false;
			}
			else
			{
				try
				{
					TcpClient client = new TcpClient(this._serverIpAddr, this._port);
					client.SendTimeout = 1000;

					//byte[] data = Encoding.UTF8.GetBytes("Hello");
					byte[] data = this._payload;

					NetworkStream ns = client.GetStream();
					ns.Write(data, 0, data.Length);

					byte[] recvbuff = new Byte[256];
					Int32 recvLength = ns.Read(recvbuff, 0, recvbuff.Length);

					MessageBox.Show(Encoding.UTF8.GetString(recvbuff), "Recv", MessageBoxButton.OK);

					// Close everything
					ns.Close();
					client.Close();
				}
				catch(SocketException se)
				{
					MessageBox.Show(se.ToString());
					return false;
				}								
				return true;
			}
		}

		public void Connect(String ip, Int32 port)
		{
			this._serverIpAddr = ip;
			this._port = port;
			Connect();
		}
	}
}
