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
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using WMX3ApiCLR;



namespace FinalClientg
{
    #region
    public enum SettingParams
    {
        VELOCITY =0,
        PROFILETYPE,
        JOG,
        HOMETYPE
    }
    #endregion
    public partial class Form1 : Form
    {
        Motion.PosCommand posCommX;            //유저가 세팅한 param (이게 비어있으면 baseParam을 사용하여 움직일 것)
        Motion.PosCommand posCommY;            //유저가 세팅한 param (이게 비어있으면 baseParam을 사용하여 움직일 것)

        string pattern = @"\[(.*?)\]"; // 정규 표현식 패턴: [] 사이의 모든 문자열
        const int Xaxis = 2;
        const int Yaxis = 3;
        int BTN_MOVING_AXIS = 0;

        bool isNet = false;

        const int WaitTimeMilliseconds = 10000;
        const int AXIS0 = 2;
        const int AXIS1 = 3;

        const int SERVOON = 1;
        const int SERVOOFF = 0;

        int ret = 0;
        int err = 0;
        bool alreadyComm = false;
        bool[] digitalOuptputs;
        bool ableComm = false;
        int jogDirX = 1;
        int JogDirX
        {
            get
            {
                return jogDirX;
            }
            set
            {
                if(value != jogDirX)
                {
                    jogDirX = value;
                    posCommX.Target *= -1;
                }
            }
        }
        int jogDirY = 1;
        int JogDirY
        {
            get
            {
                return jogDirY;
            }
            set
            {
                if (value != jogDirY)
                {
                    jogDirY = value;
                    posCommY.Target *= -1;
                }
            }
        }

        private const int bufferSize = 1024; //메시지를 받을 버퍼의 크기
        private TcpClient client; //편리한 클래스의 client를 생성
        private NetworkStream stream; //네트워크 기반 메세지 전달 통신 수단
        private byte[] buffer; //stream으로부터 받아오는 메세지를 저장할 공간

        String dataReceived;

        WMX3Api wmxlib;
        Io iolib;
        CoreMotion cmlib;
        CoreMotionStatus cmStatus;

        double oldTargetX = 0;
        double oldTargetY = 0;
        
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

            posCommX = new Motion.PosCommand();
            posCommX.Axis = 2;
            posCommX.Target = 24470;
            posCommX.Profile.Velocity = 360;
            posCommX.Profile.Acc = 3600;
            posCommX.Profile.Dec = 3600;
            posCommX.Profile.Type = ProfileType.Trapezoidal;
              
            posCommY = new Motion.PosCommand();
            posCommY.Axis = 3;
            posCommY.Target = 23882;
            posCommY.Profile.Velocity = 360;
            posCommY.Profile.Acc = 3600;
            posCommY.Profile.Dec = 3600;
            posCommY.Profile.Type = ProfileType.Trapezoidal;


            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1초마다
            timer.Tick += timer1_Tick;
            timer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void BTN_CONNECT_Click(object sender, EventArgs e)
        {
            isNet = true;
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
            isNet = false;
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

                    if (dataReceived == "WMX3COMMUICATION")
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
                        MOTORHOME(Convert.ToInt32( dataReceived.Substring(10, 1)));
                    }
                    else if (dataReceived.Substring(0, 2) == "CH")
                    {
                        int tempAxis = 2 + Convert.ToInt32(dataReceived[6] == '3');
                        Match match = Regex.Match(dataReceived, pattern);
                        int value = Convert.ToInt32(match.Groups[1].Value);
                        if (dataReceived[7] == 'V' && match.Success)
                        {
                            SetParam(SettingParams.VELOCITY, value, tempAxis);
                        }
                        else if (dataReceived[7] == 'H' && match.Success)
                        {
                            SetParam(SettingParams.HOMETYPE, value, tempAxis);
                        }
                        else if (dataReceived[7] == 'P' && match.Success)
                        {
                            SetParam(SettingParams.PROFILETYPE, value, tempAxis);
                        }
                        else if ( dataReceived[7] == 'J' && match.Success)
                        {
                            if (tempAxis == 2)
                            {
                                JogDirX = (value == 0) ? -1 : 1;
                            }
                            else
                            {
                                JogDirY = (value == 0) ? -1 : 1;
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


        private void SetParam(SettingParams paramType, int num, int axis)
        {
            switch (paramType)
            {
                case SettingParams.VELOCITY:
                    if(axis == 2)
                    {
                        posCommX.Profile.Velocity = num;
                        posCommX.Profile.Acc = num*10;
                        posCommX.Profile.Dec = num*10;
                    }
                    else
                    {
                        posCommY.Profile.Velocity = num;
                        posCommY.Profile.Acc = num * 10;
                        posCommY.Profile.Dec = num * 10;
                    }
                    break;
                case SettingParams.HOMETYPE:
                    Config.HomeParam homeParam = new Config.HomeParam();
                    cmlib.Config.GetHomeParam(axis, ref homeParam);
                    switch (num)
                    {
                        case 0:
                            homeParam.HomeType = Config.HomeType.HS;
                            break;
                        case 1:
                            homeParam.HomeType = Config.HomeType.HSHS;
                            break;
                        case 2:
                            homeParam.HomeType = Config.HomeType.LS;
                            break;
                        default:
                            break;
                    }
                    cmlib.Config.SetHomeParam(axis, homeParam);
                    MOTORHOME(axis);
                    break;
                case SettingParams.PROFILETYPE:
                    switch (num)
                    {
                        case 0:
                            if(axis == 2)
                            {
                                posCommX.Profile.Type = ProfileType.Trapezoidal;
                            }
                            else
                            {
                                posCommY.Profile.Type = ProfileType.Trapezoidal;
                            }
                            break;
                        case 1:
                            if (axis == 2)
                            {
                                posCommX.Profile.Type = ProfileType.AdvancedS;
                            }
                            else
                            {
                                posCommY.Profile.Type = ProfileType.AdvancedS;
                            }
                            break;
                        case 2:
                            if (axis == 2)
                            {
                                posCommX.Profile.Type = ProfileType.JerkRatio;
                            }
                            else
                            {
                                posCommY.Profile.Type = ProfileType.JerkRatio;
                            }
                            break;
                        default:
                            break;
                    }
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
                if(num == 1577)
                {
                    if (cmStatus.AxesStatus[2].AmpAlarm)
                    {
                        SendMessage("[ALARMRESET2]");
                    }
                    else if(cmStatus.AxesStatus[3].AmpAlarm)
                    {
                        SendMessage("[ALARMRESET3]");
                    }
                }
                MessageBox.Show(errString);
            }
        }

        //private void ResetAlarm(int num)
        //{
        //    DisplayError(cmlib.Motion.ala)
        //}

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
                //cmlib.AxisControl.SetServoOn(Xaxis, 1);
                //cmlib.AxisControl.SetServoOn(Yaxis, 1);
                //cmlib.AxisControl.SetServoOn(0, 1);
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

            if (num == "2")
            {
                ret = cmlib.GetStatus(ref cmStatus);

                if (!cmStatus.AxesStatus[AXIS0].ServoOn)
                {
                    cmlib.AxisControl.SetServoOn(AXIS0, SERVOON);
                    cmlib.Config.SetGearRatio(Convert.ToInt32(num), 38364, 360);
                }
                else
                {
                    cmlib.AxisControl.SetServoOn(AXIS0, SERVOOFF);
                }
            }
            if (num == "3")
            {
                ret = cmlib.GetStatus(ref cmStatus);

                if (!cmStatus.AxesStatus[AXIS1].ServoOn)
                {
                    cmlib.AxisControl.SetServoOn(AXIS1, SERVOON);
                    cmlib.Config.SetGearRatio(Convert.ToInt32(num), 38364, 360);
                }
                else
                {
                    cmlib.AxisControl.SetServoOn(AXIS1, SERVOOFF);
                }
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (alreadyComm && isNet)
            {

                Thread.Sleep(500);
                DisplayError(cmlib.GetStatus(ref cmStatus));
                string message = "[X:";
                message += cmStatus.AxesStatus[Xaxis].ActualPos.ToString("0.00");
                message += "]";
                SendMessage(message);
                

                message = "[Y:";
                message += cmStatus.AxesStatus[Yaxis].ActualPos.ToString("0.00");
                message += "]";
                SendMessage(message);
                CheckBTN();
                xactualpos.Text = cmStatus.AxesStatus[Xaxis].ActualPos.ToString();
                jogdir.Text = (posCommX.Target < 0) ? "<<<<" : ">>>>";
            }
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
                        if(temp != '\0')            //버튼이 눌리거나 떼어진 상황중 눌린 상황일 때는 타이머를 시작하고
                        {
                            timer2.Interval = 50; // 1초마다
                            timer2.Tick += timer2_Tick;
                            timer2.Start();
                            timeronoff.Text = "ON";
                        }
                        else
                        //버튼이 눌리거나 떼어진 상황중 떼어진 상황일 때는 타이머를 멈춘다.
                        {
                            timer2.Stop();
                            timeronoff.Text = "OFF";
                        }
                        MoveJog(temp != '\0', i / 4, i);
                    }
                    else if(i == 2)
                    {
                        if (temp != '\0')
                        {
                            SendMessage("[GRAPHCLEAR]");
                        }
                    }
                    return;
                }
            }

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            DisplayError(cmlib.GetStatus(ref cmStatus));
            if (BTN_MOVING_AXIS == 2)
            {
                if (cmStatus.AxesStatus[AXIS0].ActualPos <= 0 && posCommX.Target < 0)
                {
                    MOTORSTOP("2");
                }

            }
            else if(BTN_MOVING_AXIS == 3)
            {
                if (cmStatus.AxesStatus[AXIS1].ActualPos <= 0 && posCommY.Target < 0)
                {
                    MOTORSTOP("3");
                }
            }
        }

        /// <summary>
        /// 이 함수는 왼쪽 네 개 버튼만
        /// </summary>
        /// <param name="isBTNpressed"></param>
        /// <param name="axis"></param>
        private void MoveJog(bool isBTNpressed, int axis, int btn)
        {
            if(btn > 3)
            {
                oldTargetY = posCommY.Target;
                BTN_MOVING_AXIS = 3;
            }
            else
            {
                oldTargetX = posCommX.Target;
                BTN_MOVING_AXIS = 2;

            }
            if (isBTNpressed)
            {
                switch (btn)
                {
                    case 0:
                        posCommX.Target = -Math.Abs(oldTargetX);
                        break;
                    case 1:
                        posCommX.Target = Math.Abs(oldTargetX);
                        break;
                    case 4:
                        posCommY.Target = -Math.Abs(oldTargetY);
                        break;
                    case 5:
                        posCommY.Target = Math.Abs(oldTargetY);
                        break;
                    default:
                        break;
                }

                if (btn < 3)
                {
                    DisplayError(cmlib.Motion.StartMov(posCommX));

                }
                else
                {
                    DisplayError(cmlib.Motion.StartMov(posCommY));

                }
            }
            else
            {
                if(btn < 3)
                {
                    posCommX.Target = oldTargetX;
                }
                else
                {
                    posCommY.Target = oldTargetY;
                }
                MOTORSTOP((axis+2).ToString());
            }
        }

        private void MOTORHOME(int num)
        {

            ////원점 파라미터 로드
            //err = wmxlib_cm.config->GetHomeParam(0, &homeParam);

            ////원점 파라미터 작성
            //err = wmxlib_cm.config->SetHomeParam(0, &homeParam);

            ////원점 복귀 동작 시작
            //err = wmxlib_cm.home->StartHome(0);

            if (alreadyComm)
            {
                ret = cmlib.GetStatus(ref cmStatus);
                if (num == 2)
                {
                    ret = cmlib.Home.StartHome(AXIS0);
                }
                if (num == 3)
                {
                    ret = cmlib.Home.StartHome(AXIS1);
                }
            }
        }

        private void MOTORSTOP(string num)
        {
            DisplayError(cmlib.Motion.Stop(Convert.ToInt32(num)));
            BTN_MOVING_AXIS = 0;

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
