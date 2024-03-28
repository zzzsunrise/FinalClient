using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMX3ApiCLR;



namespace FinalClientg
{
    public enum SettingParams
    {
        VELOCITY =0,
        PROFILETYPE,
        JOG,
        HOMETYPE
    }
    public struct Params
    {

        public bool isSetting;
        public int velocity;
        public int target;
        

        public Params(int velocity1, int target1)
        {
            velocity = velocity1;
            target = target1;
            isSetting = true;
        }
    }
    public partial class Form1 : Form
    {
        Params baseParam;           //유저가 세팅하기 전 미리 기본값을 넣어둔 param
        Params settingParam;            //유저가 세팅한 param (이게 비어있으면 baseParam을 사용하여 움직일 것)

        string pattern = @"\[(.*?)\]"; // 정규 표현식 패턴: [] 사이의 모든 문자열
        const int Xaxis = 2;
        const int Yaxis = 3;

        const int WaitTimeMilliseconds = 10000;
        const int AXIS0 = 0;
        const int AXIS1 = 1;

        const int SERVOON = 1;
        const int SERVOOFF = 0;

        int ret = 0;
        int err = 0;
        bool alreadyComm = false;
        bool[] digitalOuptputs;
        bool ableComm = false;

        private const int bufferSize = 1024; //메시지를 받을 버퍼의 크기
        private TcpClient client; //편리한 클래스의 client를 생성
        private NetworkStream stream; //네트워크 기반 메세지 전달 통신 수단
        private byte[] buffer; //stream으로부터 받아오는 메세지를 저장할 공간

        String dataReceived;

        WMX3Api wmxlib;
        Io iolib;
        CoreMotion cmlib;
        CoreMotionStatus cmStatus;

        public Form1()
        {
            InitializeComponent();
            buffer = new byte[bufferSize];   //buffer 객체 생성
            wmxlib = new WMX3Api();
            iolib = new Io(wmxlib);
            cmlib = new CoreMotion(wmxlib);
            cmStatus = new CoreMotionStatus();

            // 1초마다 메시지 전송을 위한 타이머 설정

            digitalOuptputs = new bool[8];

            baseParam = new Params(3600, 360);

            Timer timer = new Timer();
            timer.Interval = 1000; // 1초마다
            timer.Tick += timer1_Tick;
            timer.Start();
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
                //ListBox_MSG.Items.Add("접속완료");

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
        private void BTN_DISCONNECT_Click(object sender, EventArgs e)
        {
            try
            {
                // 클라이언트가 연결되어 있으면 연결 종료
                if (client != null && client.Connected)
                {
                    stream.Close(); // 네트워크 스트림 닫기
                    client.Close(); // 클라이언트 소켓 닫기
                   // ListBox_MSG.Items.Add("연결 종료");

                    // 연결 버튼 다시 활성화
                    BTN_CONNECT.Enabled = true;
                }
                else
                {
                    MessageBox.Show("서버에 연결되어 있지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("연결 종료 중 오류 발생: " + ex.Message);
            }
        }
        // 비동기 수신 완료 콜백 함수
        private void OnDataReceived(IAsyncResult ar)        //IAsyncResult 객체는 비동기 작업의 상태를 유지하는 데 사용
        {
            try
            {
                int bytesRead = stream.EndRead(ar); // stream은 데이터를 읽는 스트림
                if (bytesRead > 0)
                {
                    dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // 받은 메시지를 UI에 표시하기 위해 Invoke 사용
                    Invoke(new Action(() => ListBox_MSG.Items.Add(dataReceived)));

                    if (dataReceived == "WMX3COMUICATION")
                    {
                        Init();
                    }
                    else if (dataReceived.Substring(0, 2) == "DO")
                    {
                        IOLED(dataReceived.Substring(2, 2), dataReceived[5] == 'N');
                    }
                    else if (dataReceived.Substring(5, 2) == "SE")
                    {
                        MOTORSERVOON1(dataReceived.Substring(10, 1));
                    }
                    else if (dataReceived.Substring(5, 2) == "ST")
                    {
                        MOTORSTOP(dataReceived.Substring(9, 1));
                    }
                    else if (dataReceived.Substring(5, 2) == "HO")
                    {
                        MOTORHOME(dataReceived.Substring(10, 1));
                    }
                    else if(dataReceived.Substring(0,2) == "CH")
                    {
                        if (dataReceived[6] == 'H')
                        {
                            Match match = Regex.Match(dataReceived, pattern);
                            if (match.Success)
                            {
                                SetParam(SettingParams.PROFILETYPE, Convert.ToInt32(match.Groups[1].Value));
                            }
                        }
                        else if (dataReceived[6] == 'V')
                        {
                            Match match = Regex.Match(dataReceived, pattern);
                            if (match.Success)
                            {
                                SetParam(SettingParams.VELOCITY, Convert.ToInt32(match.Groups[1].Value));
                            }

                        }
                        else if (dataReceived[6] == 'P')
                        {
                            Match match = Regex.Match(dataReceived, pattern);
                            if (match.Success)
                            {
                                SetParam(SettingParams.PROFILETYPE, Convert.ToInt32(match.Groups[1].Value));
                            }

                        }
                        else if (dataReceived[6] == 'J')
                        {
                            Match match = Regex.Match(dataReceived, pattern);
                            if (match.Success)
                            {
                                SetParam(SettingParams.JOG, Convert.ToInt32(match.Groups[1].Value));
                            }
                        }

                    }

                    // 다시 비동기로 데이터 수신 대기
                    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);   // OnDataReceived의 재귀함수 형태로 만들어서 메세지를 계속 받을 수 있도록 함
                }
                else
                {
                    // 연결 종료
                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("데이터 수신 중 오류 발생: " + ex.Message);
            }
        }

        private void SetParam(SettingParams paramType, int num)
        {
            switch (paramType)
            {
                case SettingParams.VELOCITY:
                    settingParam.velocity = num;
                    break;
                case SettingParams.HOMETYPE:
                    if(num == 0)
                    {
                        
                    }
                    break;
                case SettingParams.JOG:
                    break;
                case SettingParams.PROFILETYPE:
                    break;

            }
        }

        private void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("메시지 전송 중 오류 발생: " + ex.Message);
            }
        }

        private void DisplayError(int num)
        {
            if (num != ErrorCode.None)
            {
                string errString = WMX3Api.ErrorToString(num);
                MessageBox.Show(errString);
            }
        }

        private void Init()
        {
            if (alreadyComm)
            {
                DisplayError(wmxlib.StopCommunication());
                SendMessage("COMMU OFF");
            }
            else
            {
                wmxlib.CreateDevice("C:\\Program Files\\SoftServo\\WMX3", DeviceType.DeviceTypeNormal);
                DisplayError(wmxlib.StartCommunication(WaitTimeMilliseconds));
                cmlib = new CoreMotion(wmxlib);
                SendMessage("COMMU ON");
                cmlib.AxisControl.SetServoOn(Xaxis, 1);
                cmlib.AxisControl.SetServoOn(Yaxis, 1);
                cmlib.AxisControl.SetServoOn(0, 1);
            }

            alreadyComm = !alreadyComm;
        }

        private void IOLED(string num, bool isOn)
        {
            int temp = Convert.ToInt32(num);

            iolib.SetOutBit(temp / 8, temp % 8, Convert.ToByte(isOn));
        }

        private void MOTORSERVOON1(string num)
        {

            if (num == "1")
            {
                ret = cmlib.GetStatus(ref cmStatus);

                if (!cmStatus.AxesStatus[AXIS0].ServoOn && !cmStatus.AxesStatus[AXIS1].ServoOn)
                {
                    cmlib.AxisControl.SetServoOn(AXIS0, SERVOON);
                    cmlib.Config.SetGearRatio(Convert.ToInt32(num), 8388608, 360);
                }
                else
                {
                    cmlib.AxisControl.SetServoOn(AXIS0, SERVOOFF);
                }
            }
            if (num == "2")
            {
                ret = cmlib.GetStatus(ref cmStatus);

                if (!cmStatus.AxesStatus[AXIS0].ServoOn && !cmStatus.AxesStatus[AXIS1].ServoOn)
                {
                    cmlib.AxisControl.SetServoOn(AXIS1, SERVOON);
                    cmlib.Config.SetGearRatio(Convert.ToInt32(num), 8388608, 360);
                }
                else
                {
                    cmlib.AxisControl.SetServoOn(AXIS1, SERVOOFF);
                }
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (alreadyComm)
            {

                //DisplayError(cmlib.GetStatus(ref cmStatus));
                //string message = "X : ";
                //message += cmStatus.AxesStatus[Xaxis].ActualPos.ToString("0.00");
                //message += " ,Y : ";
                //message += cmStatus.AxesStatus[Yaxis].ActualPos.ToString("0.00");
                //SendMessage(message);
                CheckBTN();

            }
            //-----------------------
            // try
            //{
            //    // 메시지 생성
            //    // 메시지를 바이트 배열로 변환하여 서버로 전송
            //    // 전송한 메시지를 리스트박스에 표시
            //    ListBox_MSG.Items.Add("나: " + message);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("메시지 전송 중 오류 발생: " + ex.Message);
            //    }
        }

        private void CheckBTN()
        {
            byte temp = 0;
            bool old = false;
            
            for (int i = 0; i < 8; i++)
            {
                old = digitalOuptputs[i];
                //0,1일 때는 Xaxis가
                //4,5일 때는 Yaxis가 들어가야 한다.
                iolib.GetInBit(20, i, ref temp);
                if (old != (temp != '\0'))
                {
                    digitalOuptputs[i] = temp != '\0';
                    if (i == 0 || i == 1 || i == 4 || i == 5)
                    {
                        MoveJog(temp != '\0', i / 4);
                    }
                    return;
                }
            }

        }

        /// <summary>
        /// 이 함수는 왼쪽 네 개 버튼만
        /// </summary>
        /// <param name="isBTNpressed"></param>
        /// <param name="axis"></param>
        private void MoveJog(bool isBTNpressed, int axis)
        {
            Motion.PosCommand pos = new Motion.PosCommand();
            pos.Axis = axis+2;

            pos.Profile.Type = ProfileType.Trapezoidal;
            Params tempParam = (settingParam.isSetting) ? settingParam : baseParam;
            pos.Profile.Velocity = tempParam.velocity;
            pos.Profile.Acc = tempParam.velocity * 10;
            pos.Profile.Dec = tempParam.velocity * 10;
            pos.Target = tempParam.target;


            if (isBTNpressed)
            {
                DisplayError(cmlib.Motion.StartMov(pos));
            }
            else
            {
                MOTORSTOP(axis.ToString());
            }
        }

        private void MOTORHOME(string num)
        {

            ////원점 파라미터 로드
            //err = wmxlib_cm.config->GetHomeParam(0, &homeParam);
            //if (err != ErrorCode::None)
            //{
            //    wmxlib_cm.ErrorToString(err, errString, sizeof(errString));
            //    printf("Failed to read home parameters. Error=%d (%s)\n", err, errString);
            //    goto exit;
            //}
            ////원점 파라미터 작성
            //err = wmxlib_cm.config->SetHomeParam(0, &homeParam);
            //if (err != ErrorCode::None)
            //{
            //    wmxlib_cm.ErrorToString(err, errString, sizeof(errString));
            //    printf("Failed to write home parameters. Error=%d (%s)\n", err, errString);
            //    goto exit;
            //}

            ////원점 복귀 동작 시작
            //err = wmxlib_cm.home->StartHome(0);
            //if (err != ErrorCode::None)
            //{
            //    wmxlib_cm.ErrorToString(err, errString, sizeof(errString));
            //    printf("Failed to start homing. Error=%d (%s)\n", err, errString);
            //    goto exit;
            //}
            if (alreadyComm)
            {
                Config.HomeParam homeParam = new Config.HomeParam();
                DisplayError(cmlib.Config.GetHomeParam(Convert.ToInt32(num), ref homeParam));
                homeParam.HomeType = Config.HomeType.HS; //홈 유형을 홈 스위치를 찾도록 설정
                DisplayError(cmlib.Config.SetHomeParam(0, homeParam));
                DisplayError(cmlib.Home.StartHome(0));

                ret = cmlib.GetStatus(ref cmStatus);
                if (num == "1")
                {
                    ret = cmlib.Home.StartHome(AXIS0);
                }
                if (num == "2")
                {
                    ret = cmlib.Home.StartHome(AXIS1);
                }
            }
        }

        private void MOTORSTOP(string num)
        {
            DisplayError(cmlib.Motion.Stop(Convert.ToInt32(num)));

            //int err = wmxlib_cm.motion->Stop(0);
            //if (err != ErrorCode::None)
            //{
            //    wmxlib_cm.ErrorToString(err, errString, sizeof(errString));
            //    printf("Failed to stop motion. Error=%d (%s)\n", err, errString);
            //    goto exit;
            //}
        }
    }
}
