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
        private const int bufferSize = 1024; //메시지를 받을 버퍼의 크기
        private TcpClient client; //편리한 클래스의 client를 생성
        private NetworkStream stream; //네트워크 기반 메세지 전달 통신 수단
        private byte[] buffer; //stream으로부터 받아오는 메세지를 저장할 공간

        public Form1()
		{
            InitializeComponent();
            buffer = new byte[bufferSize];   //buffer 객체 생성        
         }

		private void Form1_Load(object sender, EventArgs e)
		{

		}

        private void BTN_CONNECT_Click(object sender, EventArgs e)
        {
            try   //try 구문 안에 오류 목록생기면 -> catch로 이동 -> Exception의 Message proprity에서 무슨 오류인지 알려줌 
            {
                // 이미 연결되어 있는 경우 연결 중복 방지
                if (client != null && client.Connected) //client 객체가 생성되어 있고. Connected 프로퍼티가 True일 때
                {
                    MessageBox.Show("이미 서버에 연결되어 있습니다.");
                    return;
                }

                // TcpClient 객체 생성 및 서버에 연결
                client = new TcpClient();      //TcpClient client 객체 생성해서 저장
                client.Connect(text_IP.Text, Convert.ToInt32(text_PORT.Text));  // Convert.ToInt32= int.Parse : string을 int로 변환

                // 네트워크 스트림 얻기
                stream = client.GetStream();      //stream : client-server와의 연결(가상 선이라고 생각)

                // 비동기로 서버로부터 데이터 수신 대기
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);  //stream에서 메세지 읽어오기
            }
            catch (Exception ex)
            {
                MessageBox.Show("연결 중 오류 발생: " + ex.Message);
            }
        }

        // 비동기 수신 완료 콜백 함수
        private void OnDataReceived(IAsyncResult ar)        //IAsyncResult 객체는 비동기 작업의 상태를 유지하는 데 사용
        {
            try
            {
                int bytesRead = stream.EndRead(ar); // 해당 줄에서 stream은 데이터를 읽는 스트림
                if (bytesRead > 0)
                {
                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    // 받은 메시지를 UI에 표시하기 위해 Invoke 사용
                    Invoke(new Action(() => ListBox_MSG.Items.Add(dataReceived)));

                    // 다시 비동기로 데이터 수신 대기
                    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);   // OnDataReceived의 재귀함수 형태로 만들어서 메세지를 계속 받을 수 있도록 함
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
