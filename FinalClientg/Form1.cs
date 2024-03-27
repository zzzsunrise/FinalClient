using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinalClientg
{
	public partial class Form1 : Form
	{
        private const int bufferSize = 1024; // 버퍼 크기 설정
        private TcpClient client;
        private NetworkStream stream;
        private byte[] buffer;

        public Form1()
		{
            InitializeComponent();
            buffer = new byte[bufferSize];
        }

		private void Form1_Load(object sender, EventArgs e)
		{

		}

        private void BTN_CONNECT_Click(object sender, EventArgs e)
        {
            try
            {
                // 이미 연결되어 있는 경우 연결 중복 방지
                if (client != null && client.Connected)
                {
                    MessageBox.Show("이미 서버에 연결되어 있습니다.");
                    return;
                }

                // TcpClient 객체 생성 및 서버에 연결
                client = new TcpClient();
                client.Connect(text_IP.Text, int.Parse(text_PORT.Text));
                //StatusLabel.Text = "Connected to server.";

                // 네트워크 스트림 얻기
                stream = client.GetStream();

                // 비동기로 서버로부터 데이터 수신 대기
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("연결 중 오류 발생: " + ex.Message);
            }
        }

        // 비동기 수신 완료 콜백 함수
        private void OnDataReceived(IAsyncResult ar)
        {
            try
            {
                int bytesRead = stream.EndRead(ar);
                if (bytesRead > 0)
                {
                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    // 받은 메시지를 UI에 표시하기 위해 Invoke 사용
                    Invoke(new Action(() => ListBox_MSG.Items.Add(dataReceived)));

                    // 다시 비동기로 데이터 수신 대기
                    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);
                }
                else
                {
                    // 연결 종료
                    stream.Close();
                    client.Close();
                    //StatusLabel.Text = "Disconnected from server.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("데이터 수신 중 오류 발생: " + ex.Message);
            }
        }
    

    }
}
