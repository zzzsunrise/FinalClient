namespace FinalClientg
{
	partial class Form1
	{
		/// <summary>
		/// 필수 디자이너 변수입니다.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 사용 중인 모든 리소스를 정리합니다.
		/// </summary>
		/// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form 디자이너에서 생성한 코드

		/// <summary>
		/// 디자이너 지원에 필요한 메서드입니다. 
		/// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.text_IP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.text_PORT = new System.Windows.Forms.TextBox();
            this.BTN_CONNECT = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.BTN_DISCONNECT = new System.Windows.Forms.Button();
            this.ListBox_MSG = new System.Windows.Forms.ListBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // text_IP
            // 
            this.text_IP.Location = new System.Drawing.Point(90, 49);
            this.text_IP.Margin = new System.Windows.Forms.Padding(2);
            this.text_IP.Name = "text_IP";
            this.text_IP.Size = new System.Drawing.Size(144, 21);
            this.text_IP.TabIndex = 0;
            this.text_IP.Text = "172.30.1.64";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 50);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "IP 주소";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 71);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port 번호";
            // 
            // text_PORT
            // 
            this.text_PORT.Location = new System.Drawing.Point(75, 55);
            this.text_PORT.Margin = new System.Windows.Forms.Padding(2);
            this.text_PORT.Name = "text_PORT";
            this.text_PORT.Size = new System.Drawing.Size(144, 21);
            this.text_PORT.TabIndex = 2;
            this.text_PORT.Text = "8888";
            // 
            // BTN_CONNECT
            // 
            this.BTN_CONNECT.Location = new System.Drawing.Point(75, 80);
            this.BTN_CONNECT.Margin = new System.Windows.Forms.Padding(2);
            this.BTN_CONNECT.Name = "BTN_CONNECT";
            this.BTN_CONNECT.Size = new System.Drawing.Size(142, 21);
            this.BTN_CONNECT.TabIndex = 4;
            this.BTN_CONNECT.Text = "접속";
            this.BTN_CONNECT.UseVisualStyleBackColor = true;
            this.BTN_CONNECT.Click += new System.EventHandler(this.BTN_CONNECT_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.BTN_CONNECT);
            this.groupBox1.Controls.Add(this.text_PORT);
            this.groupBox1.Controls.Add(this.BTN_DISCONNECT);
            this.groupBox1.Location = new System.Drawing.Point(15, 22);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(250, 145);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "CONNECT";
            // 
            // BTN_DISCONNECT
            // 
            this.BTN_DISCONNECT.Location = new System.Drawing.Point(75, 111);
            this.BTN_DISCONNECT.Margin = new System.Windows.Forms.Padding(2);
            this.BTN_DISCONNECT.Name = "BTN_DISCONNECT";
            this.BTN_DISCONNECT.Size = new System.Drawing.Size(142, 21);
            this.BTN_DISCONNECT.TabIndex = 6;
            this.BTN_DISCONNECT.Text = "접속 해제";
            this.BTN_DISCONNECT.UseVisualStyleBackColor = true;
            this.BTN_DISCONNECT.Click += new System.EventHandler(this.BTN_DISCONNECT_Click);
            // 
            // ListBox_MSG
            // 
            this.ListBox_MSG.FormattingEnabled = true;
            this.ListBox_MSG.ItemHeight = 12;
            this.ListBox_MSG.Location = new System.Drawing.Point(15, 182);
            this.ListBox_MSG.Name = "ListBox_MSG";
            this.ListBox_MSG.Size = new System.Drawing.Size(250, 148);
            this.ListBox_MSG.TabIndex = 6;
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(282, 394);
            this.Controls.Add(this.ListBox_MSG);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.text_IP);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox text_IP;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox text_PORT;
		private System.Windows.Forms.Button BTN_CONNECT;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button BTN_DISCONNECT;
        private System.Windows.Forms.ListBox ListBox_MSG;
        private System.Windows.Forms.Timer timer1;
    }
}

