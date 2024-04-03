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
    public partial class Form1 : Form
    {
        //가독성을 위한 SettingParam의 이름
        public enum SettingParams
        {
            VELOCITY = 0,
            PROFILETYPE,
            JOG,
            HOMETYPE
        }
        #region[Setting Variable]

        String dataReceived;

        Motion.PosCommand posCommX;            //유저가 세팅한 param (이게 비어있으면 baseParam을 사용하여 움직일 것)
        Motion.PosCommand posCommY;            //유저가 세팅한 param (이게 비어있으면 baseParam을 사용하여 움직일 것)

        string pattern = @"\[(.*?)\]";          // 정규 표현식 패턴: 모든 [] 사이의 문자열을 필터링할 수 있다.
        const int Xaxis = 2;
        const int Yaxis = 3;
        int BTN_MOVING_AXIS = 0;

        bool isNet = false;

        byte anal1 = 0;
        byte anal2 = 0;
        byte oldanal1 = 0;
        byte oldanal2 = 0;


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

        double oldTargetX = 0;
        double oldTargetY = 0;

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

    #endregion

        private const int bufferSize = 2048;        //메시지를 받을 버퍼의 크기
        private TcpClient client;                   //편리한 클래스의 client를 생성
        private NetworkStream stream;               //네트워크 기반 메세지 전달 통신 수단
        private byte[] buffer;                      //stream으로부터 받아오는 메세지를 저장할 공간

        WMX3Api wmxlib;
        Io iolib;
        CoreMotion cmlib;
        CoreMotionStatus cmStatus;
        public Form1()
        {
            InitializeComponent();

            buffer = new byte[bufferSize];          //buffer 객체 생성
            wmxlib = new WMX3Api();
            iolib = new Io(wmxlib);                 //IO는 wmx3Api의 모듈이라 wmxlib 객체를 매개변수로 넘김
            cmlib = new CoreMotion(wmxlib);         //CoreMotion은 wmx3Api의 모듈이라 wmxlib 객체를 매개변수로 넘김
            cmStatus = new CoreMotionStatus();

            digitalOuptputs = new bool[8];          //버튼의 상태를 저장

            //X축과 Y축의 전역 Command 변수의 초기값을 세팅
            #region[Frist Param]
            posCommX = new Motion.PosCommand();
            posCommX.Axis = 2;
            posCommX.Target = 20552;
            posCommX.Profile.Velocity = 360;
            posCommX.Profile.Acc = 3600;
            posCommX.Profile.Dec = 3600;
            posCommX.Profile.Type = ProfileType.Trapezoidal;
              
            posCommY = new Motion.PosCommand();
            posCommY.Axis = 3;
            posCommY.Target = 19973;
            posCommY.Profile.Velocity = 360;
            posCommY.Profile.Acc = 3600;
            posCommY.Profile.Dec = 3600;
            posCommY.Profile.Type = ProfileType.Trapezoidal;
            #endregion

            //1초마다 실행되는 타이머 시작
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += timer1_Tick;
            timer.Start();                          
        }

        /// <summary>
        /// 접속 버튼 클릭 시 실행
        /// </summary>
        /// <param name="sender">버튼 자체가 날아옴</param>
        /// <param name="e">마우스의 정보를 가진 클래스</param>
        private void BTN_CONNECT_Click(object sender, EventArgs e)
        {
            isNet = true;

            //try 구문 안에 오류 목록생기면 -> catch로 이동
            try
            {
                //client 객체가 생성되어 있고. Connected 프로퍼티가 True일 때
                if (client != null && client.Connected)
                {
                    MessageBox.Show("이미 서버에 연결되어 있습니다.");
                    return;
                }

                //TcpClient client 객체 생성해서 저장
                client = new TcpClient();
                
                //생성된 TcpClient 객체가 가진 Connect 함수를 이용하여 지정된 포트와 IP로 접속
                client.Connect(text_IP.Text, Convert.ToInt32(text_PORT.Text));

                //네트워크 스트림 얻기 [stream : client-server에서 Socket의 개념]
                stream = client.GetStream();

                //비동기로 서버로부터 데이터 수신을 시작. 메시지가 들어오면 OnDataReceived 함수를 Call
                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);
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
                    stream.Close();             // 네트워크 스트림 닫기
                    client.Close();             // 클라이언트 스트림 닫기

                    
                    BTN_CONNECT.Enabled = true; // 연결 버튼 다시 활성화
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

        /// <summary>
        /// 비동기 콜백 함수로서 BeginRead 스레드에서 메시지가 읽혔을 때 Call되는 함수
        /// </summary>
        /// <param name="ar">비동기 작업의 상태를 유지하는 데 사용</param>
        private void OnDataReceived(IAsyncResult ar)
        {
            try
            {
                //스트림으로부터 메시지를 읽어오고 해당 길이를 bytesRead에 저장
                int bytesRead = stream.EndRead(ar);

                //읽은 메시지의 길이가 0보다 클 때만 코드 진입
                if (bytesRead > 0)
                {
                    //읽은 메시지를 byte형태로 배열에 저장하고 byte 배열을 String으로 Encoding
                    dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    //받은 메시지를 UI에 표시
                    Invoke(new Action(() => ListBox_MSG.Items.Add(dataReceived)));


                    if (dataReceived == "WMX3COMMUICATION")
                    {
                        Init();
                    }
                    else if (dataReceived.Substring(0, 2) == "DO") 
                    {
                        //Substring(2, 2)은 00~15, dataReceived[5] == 'N'는 ON일 때만 TRUE
                        IOLED(dataReceived.Substring(2, 2), dataReceived[5] == 'N');
                    }
                    else if (dataReceived.Substring(5, 2) == "SE") 
                    {
                        //10번째 숫자는 축의 번호
                        MOTORSERVOONOFF(dataReceived.Substring(10, 1));
                    }
                    else if (dataReceived.Substring(0, 2) == "AL")
                    {
                        ResetAlarm(Convert.ToInt32(dataReceived[dataReceived.Length - 1]));
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
                        //6번째 글자가 2이면 tempAxis에 2가 저장되고 3이면 3이 저장
                        int tempAxis = 2 + Convert.ToInt32(dataReceived[6] == '3');

                        //정규식을 통해 [] 사이에 있는 문자열들을 match에 저장
                        Match match = Regex.Match(dataReceived, pattern);

                        //[]가 하나기 때문에 그 중 첫번째 값을 정수로 바꾸어 value에 저장
                        int value = Convert.ToInt32(match.Groups[1].Value);

                        //match가 성공했을 때마다 문자열을 분석하여 SetParam 함수의 enum을 다르게 보내 실행
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
                        //Jog방향을 설정할 때는 SetParam 함수를 이용하지 않고 바로 변경
                        else if ( dataReceived[7] == 'J' && match.Success)
                        {
                            if (tempAxis == 2)
                            {
                                JogDirX = (value == 0) ? -1 : 1;    //0일 때 음의 방향, 0이 아닐 때 양의 방향으로 설정
                            }
                            else
                            {
                                JogDirY = (value == 0) ? -1 : 1;    //0일 때 음의 방향, 0이 아닐 때 양의 방향으로 설정
                            }
                        }
                    }

                    // 다시 비동기로 데이터 수신 대기를 시작
                    // 이렇게 해주지 않으면 단 한 개의 메시지 수신만 가능
                    stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(OnDataReceived), null);
                }
                else
                {
                    stream.Close();         //연결종료
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("데이터 수신 중 오류 발생: " + ex.Message);
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
                if(num == 65556)
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

        private void ResetAlarm(int num)
        {
            DisplayError(cmlib.AxisControl.ClearAxisAlarm(num-48));
            DisplayError(cmlib.AxisControl.ClearAmpAlarm(num-48));
        }

        private void Init()
        {
            if (alreadyComm)  //이미 Communication 중이면 통신을 끊고 COMMU OFF 메시지를 보냄
            {
                DisplayError(wmxlib.StopCommunication());
                SendMessage("COMMU OFF");
            }
            else  //Communication 중이 아니면 디바이스와 coremotion 객체를 생성하고 통신을 시작해 COMMU ON 메시지 전송
            {
                wmxlib.CreateDevice("C:\\Program Files\\SoftServo\\WMX3", DeviceType.DeviceTypeNormal);
                DisplayError(wmxlib.StartCommunication(WaitTimeMilliseconds));
                cmlib = new CoreMotion(wmxlib);
                SendMessage("COMMU ON");
            }

            alreadyComm = !alreadyComm;  //해당 상태를 전환
        }

        private void MOTORSERVOONOFF(string num)  //축의 번호를 받아 그에 맞는 SERVOON을 실행
        {

            if (num == "2") //축 번호가 2일 때
            {
                ret = cmlib.GetStatus(ref cmStatus); //최신 상태 받아오기

                if (!cmStatus.AxesStatus[AXIS0].ServoOn) //X축이 ON 상태가 아닐 때는 서버를 키고 기어비를 설정
                {
                    cmlib.AxisControl.SetServoOn(AXIS0, SERVOON);
                    cmlib.Config.SetGearRatio(Convert.ToInt32(num), 38364, 360);
                }
                else  //ON 상태일 때는 서버를 끔
                {
                    cmlib.AxisControl.SetServoOn(AXIS0, SERVOOFF);
                }
            }
            if (num == "3")
            {
                ret = cmlib.GetStatus(ref cmStatus); //최신 상태 받아오기

                if (!cmStatus.AxesStatus[AXIS1].ServoOn)  //Y축이 ON 상태가 아닐 때는 서버를 키고 기어비를 설정
                {
                    cmlib.AxisControl.SetServoOn(AXIS1, SERVOON);
                    cmlib.Config.SetGearRatio(Convert.ToInt32(num), 38364, 360);
                }
                else //Y축 서버 OFF
                {
                    cmlib.AxisControl.SetServoOn(AXIS1, SERVOOFF);
                }
            }
        }

        private void SetParam(SettingParams paramType, int num, int axis)  //매개변수 1.바꿀 파라미터 enum 2.목표값 3.축 값
        {
            switch (paramType)
            {
                case SettingParams.VELOCITY:  //속도를 바꿀 때 축 번호에 따라 속도와 가감속(10배) 설정
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
                case SettingParams.HOMETYPE:  //현재 홈파람을 가져온 후 홈타입만 약속된 번호대로 바꿔서 Set한 후 홈을 실행
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
                    MOTORHOME(axis);            //바뀐 값대로 Homeing을 실행한다.
                    break;
                case SettingParams.PROFILETYPE:  //날아온 값과 축 번호에 따라 가속 방법을 설정
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
       
        private void timer1_Tick(object sender, EventArgs e)  //0.5초마다 x,y 좌표를 보내고 가변저항 값을 받아오고 버튼 상태를 체크
        {
            //접속 버튼을 눌렀고 communication상태일  때만 함수 진입
            if (alreadyComm && isNet)  
            {
                Thread.Sleep(500);  //다량의 메시지를 보내는 관계로 Sleep이 없으면 너무 많은 메시지가 서버에 쌓인다.
                DisplayError(cmlib.GetStatus(ref cmStatus));  //최근 상태 받아오기
                string message = "[X:";
                //현재 위치값을 소수점 2째자리까지 스트링으로 변환하여 추가
                message += cmStatus.AxesStatus[Xaxis].ActualPos.ToString("0.00"); 
                message += "]";
                SendMessage(message); //X축 위치 전송
                

                message = "[Y:";
                message += cmStatus.AxesStatus[Yaxis].ActualPos.ToString("0.00");
                message += "]";
                SendMessage(message);

                CheckBTN(); //주기적으로 버튼 상태 체크

                //WOS에서 페이지를 넘겨 가변저항 두 개의 어드레스를 왼쪽은 22, 오른쪽은 24로 설정했다.
                //왼쪽 가변저항 값을 받아옴
                DisplayError(iolib.GetInAnalogDataUChar(22, ref anal1));

                if (anal1 != oldanal1)  //그 값이 달라졌을 때만 메시지를 보냄
                {
                    SendAnalog(2);
                    oldanal1 = anal1; 
                }

                DisplayError(iolib.GetInAnalogDataUChar(24, ref anal2));
                if(anal2 != oldanal2)
                {
                    SendAnalog(3);
                    oldanal2 = anal2;
                }
            }
        }

        private void CheckBTN()
        {
            byte temp = 0;
            bool old = false;
            
            for (int i = 0; i < 8; i++)
            {
                old = digitalOuptputs[i];  //전역 bool 배열에 있는 원래 값을 담아둠

                iolib.GetInBit(20, i, ref temp); //파라미터 1.버튼 주소, 2.몇번째 버튼인지, 해당 값을 저장할 byte 변수

                //버튼이 떼어진 상태면 \0이 저장된다. 눌렸는지에 대한 bool값이고 그것이 원래 값과 다를 때만 코드 진입
                if (old != (temp != '\0')) 
                {
                    digitalOuptputs[i] = temp != '\0';  //지금 들어온 값으로 전역변수 배열의 값을 update

                    if (i == 0 || i == 1 || i == 4 || i == 5)  //왼쪽 네 개의 버튼이 눌렸을 때는 모터 움직임 명령
                    {
                        if(temp != '\0')            //버튼이 눌린 상황일 때는 타이머를 시작
                        {
                            timer2.Interval = 50; 
                            timer2.Tick += timer2_Tick;
                            timer2.Start();         //이 타이머에서는 모터의 위치를 체크하고 0이면 멈추도록 모니터링
                        }
                        else                        //버튼이 떼어진 상황일 때는 타이머를 멈춤
                        {
                            timer2.Stop();
                        }

                        //파라미터 [1.버튼이 눌렸으면 true] [2.첫번째 줄 버튼 두개면 0, 두 번째 줄 버튼 두 개면 2] [3.버튼 번호]
                        MoveJog(temp != '\0', i / 4, i); 
                    }
                    else if(i == 2)  //클리어 버튼인 2번 버튼이 떼어진 게 아니라 눌린 거면 메시지 전송
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

        private void timer2_Tick(object sender, EventArgs e) //버튼이 눌린 상태일 때만 돌아가는 타이머
        {
            byte temp = 0;
            DisplayError(cmlib.GetStatus(ref cmStatus));

            //++iolib.GetInAnalogDataUChar(22, ref temp); 여기서 할 필요 없어서 주석처리 했음 잘 되는지 확인해볼 것

            if (BTN_MOVING_AXIS == 2)  //현재 움직이는 축이 X축이면
            {
                if (cmStatus.AxesStatus[AXIS0].ActualPos <= 0 && posCommX.Target < 0)  //음의 방향이면서 X축이 0보다 위치가 작을 때 멈추기
                {
                    MOTORSTOP("2");
                }

            }
            else if(BTN_MOVING_AXIS == 3) //현재 움직이는 축이 Y축이면
            {
                if (cmStatus.AxesStatus[AXIS1].ActualPos <= 0 && posCommY.Target < 0)  //음의 방향이면서 Y축이 0보다 위치가 작을 때 멈추기
                {
                    MOTORSTOP("3");
                }
            }
        }

        /// <summary>
        /// 버튼을 누르는 동안에는 움직이고 떼었을 때 멈추게 하기 위해 절대 이동을 사용하는 함수
        /// </summary>
        /// <param name="isBTNpressed">버튼이 눌렸으면 true, 떼어졌으면 false가 날라옴</param>
        /// <param name="axis">1,2 버튼을 누르면 X축을 움직여야 하므로 2가 날라옴</param>
        /// <param name="btn">버튼의 번호</param>
        private void MoveJog(bool isBTNpressed, int axis, int btn)
        {
            if(btn > 3)  //버튼 번호가 두 번째 줄이면
            {
                oldTargetY = posCommY.Target;  //현재 Y축 Command의 target을 oldTargetY에 저장해두고
                BTN_MOVING_AXIS = 3;  //어떤 축이 움직이고 있는지 저장하고 있는 변수에 Y를 저장
            }
            else
            {
                oldTargetX = posCommX.Target;
                BTN_MOVING_AXIS = 2;

            }

            if (isBTNpressed) //버튼이 눌린 상태라면
            {
                switch (btn)
                {
                    case 0:  //0번 버튼이 눌렸을 때 X축 Command의 타겟 위치를 음의 방향으로 설정
                        posCommX.Target = -Math.Abs(oldTargetX);
                        break;
                    case 1:  //1번 버튼이 눌렸을 때 X축 Command의 타겟 위치를 양의 방향으로 설정
                        posCommX.Target = Math.Abs(oldTargetX);
                        break;
                    case 4:  //4번 버튼이 눌렸을 때 Y축 Command의 타겟 위치를 음의 방향으로 설정(이렇게 해야 위로 올라감)
                        posCommY.Target = -Math.Abs(oldTargetY);
                        break;
                    case 5:  //5번 버튼이 눌렸을 때 Y축 Command의 타겟 위치를 양의 방향으로 설정
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
            else  //버튼이 떼어진 상황이라면 버튼 이동을 위해 바꿨던 Target을 눌렀을 때 저장해둔 oldTarget값으로 변환한다.
            {
                if(btn < 3)
                {
                    posCommX.Target = oldTargetX;
                }
                else
                {
                    posCommY.Target = oldTargetY;
                }
                MOTORSTOP((axis+2).ToString());  //버튼이 떼어졌으므로 해당 축의 모터를 멈추는 함수 실행
            }
        }

        private void MOTORHOME(int num)  //홈타입이 바뀌었을 때 OR "MOTORHOME" 메시지가 왔을 때 실행되는 함수
        {
            if (alreadyComm)  //통신 중일 때만 코드 실행
            {
                ret = cmlib.GetStatus(ref cmStatus);
                if (num == 2)
                {
                    ret = cmlib.Home.StartHome(AXIS0);  //X축 홈
                }
                if (num == 3)
                {
                    ret = cmlib.Home.StartHome(AXIS1);  //Y축 홈
                }
            }
        }

        private void MOTORSTOP(string num)  //축 번호를 받고 모터를 멈춘다.
        {
            DisplayError(cmlib.Motion.Stop(Convert.ToInt32(num)));
            BTN_MOVING_AXIS = 0;  //모니터링하는 함수에 들어가지 않도록 움직이는 중이 아닐 때는 0으로 설정
        }

        private void SendAnalog(int v)  //timer1에서 아날로그 값이 바뀌었을 때 호출하는 함수 (왼쪽이면 2, 오른쪽이면 3이 들어온다.)
        {
            string msg = (v == 2) ? "[ANALOG2:" : "[ANALOG3:";  //왼쪽 가변저항 값이 바뀌었을 때는 "[ANALOG2:"를 넣고 오른쪽일 때는 3이 들어간다.
            msg += (v == 2) ? anal1.ToString("000") : anal2.ToString("000");  //왼쪽 가변저항 값이 바뀐 거면 anal1변수 값을, 오른쪽일 때는 anal2변수 값을 무조건 세자리로 바꾸어 저장
            msg += "]";
            SendMessage(msg);  //괄호 닫고 서버에 전송
        }

        private void IOLED(string num, bool isOn)  //파라미터 [1: 버튼의 번호 00 ~ 15 파라미터] [2: true면 키고 false면 끔]
        {
            int temp = Convert.ToInt32(num);  //00 ~ 15 스트링을 숫자로 변환

            iolib.SetOutBit(temp / 8, temp % 8, Convert.ToByte(isOn));  //몇번째 줄 몇번째 LED를 1이면 ON, 0이면 OFF할 건지 명령
        }
    }
}
