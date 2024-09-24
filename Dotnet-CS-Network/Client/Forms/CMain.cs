using Client.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client.Forms
{
	public partial class CMain : Form
	{
		public CMain()
		{
			InitializeComponent();
			this.Load += CMain_Load;
			this.FormClosing += CMain_FormClosing;
		}

		private void CMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			NetworkController.Instance.Stop();
		}

		private void CMain_Load(object sender, EventArgs e)
		{
            Console.WriteLine("네트워크 초기화");
			NetworkController.Instance.InitializeNetwork();
        }
	}
}
