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
			this.text_IP = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.text_PORT = new System.Windows.Forms.TextBox();
			this.BTN_CONNECT = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.BTN_DISCONNECT = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// text_IP
			// 
			this.text_IP.Location = new System.Drawing.Point(167, 98);
			this.text_IP.Name = "text_IP";
			this.text_IP.Size = new System.Drawing.Size(264, 35);
			this.text_IP.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(76, 101);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(85, 24);
			this.label1.TabIndex = 1;
			this.label1.Text = "IP 주소";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(51, 142);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(110, 24);
			this.label2.TabIndex = 3;
			this.label2.Text = "Port 번호";
			// 
			// text_PORT
			// 
			this.text_PORT.Location = new System.Drawing.Point(167, 139);
			this.text_PORT.Name = "text_PORT";
			this.text_PORT.Size = new System.Drawing.Size(264, 35);
			this.text_PORT.TabIndex = 2;
			// 
			// BTN_CONNECT
			// 
			this.BTN_CONNECT.Location = new System.Drawing.Point(167, 181);
			this.BTN_CONNECT.Name = "BTN_CONNECT";
			this.BTN_CONNECT.Size = new System.Drawing.Size(264, 42);
			this.BTN_CONNECT.TabIndex = 4;
			this.BTN_CONNECT.Text = "접속";
			this.BTN_CONNECT.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.BTN_DISCONNECT);
			this.groupBox1.Location = new System.Drawing.Point(28, 43);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(465, 257);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "CONNECT";
			// 
			// BTN_DISCONNECT
			// 
			this.BTN_DISCONNECT.Location = new System.Drawing.Point(139, 186);
			this.BTN_DISCONNECT.Name = "BTN_DISCONNECT";
			this.BTN_DISCONNECT.Size = new System.Drawing.Size(264, 42);
			this.BTN_DISCONNECT.TabIndex = 6;
			this.BTN_DISCONNECT.Text = "접속 해제";
			this.BTN_DISCONNECT.UseVisualStyleBackColor = true;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 24F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(524, 357);
			this.Controls.Add(this.BTN_CONNECT);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.text_PORT);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.text_IP);
			this.Controls.Add(this.groupBox1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.groupBox1.ResumeLayout(false);
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
	}
}

