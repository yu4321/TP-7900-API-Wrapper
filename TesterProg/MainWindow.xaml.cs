using System;
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
using TP7900APIWrapperForTD1000;

namespace TesterProg
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GetNum_Click(object sender, RoutedEventArgs e)
        {
            int[] res = new int[20];
            TP7900.GetNumOfConnectedDevices(2, res);
        }

        private void GetName_Click(object sender, RoutedEventArgs e)
        {
            var ar = new StringBuilder(13);
            TP7900.Get_DeviceName(0, ar);
            MessageBox.Show(ar.ToString());
        }

        private void OpenDev_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.OpenDevice(2, 0, 1);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());

        }

        private void CloseDev_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.CloseDevice();
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void InitDev_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.InitDevice();
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void Get_VersionInfo_Click(object sender, RoutedEventArgs e)
        {
            var buf = new StringBuilder(13);
            TP7900.Get_VersionInfo(buf);
            MessageBox.Show(buf.ToString());
        }

        private void Get_Status_Click(object sender, RoutedEventArgs e)
        {
            string st1 = "";
            string st2 = "";
            var res = TP7900.Get_Status(st1, st2);
            MessageBox.Show($"{st1}\n\n{st2}");
        }

        private void SupplyCard_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.SupplyCard(0x31);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void EjectCard_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.EjectCard();
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void RejectCard_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.RejectCard();
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void GetSensorStatus_Click(object sender, RoutedEventArgs e)
        {
            var pRailStatus = new byte[8];
            var pFeedRollerStatus = new byte[8];
            var pTraySensorStatus = new byte[8];

            TP7900.GetSensorStatus(pRailStatus, pFeedRollerStatus, pTraySensorStatus);

            MessageBox.Show($"{pRailStatus.Cast<string>()}\n\n{pFeedRollerStatus.Cast<string>()}\n\n{pTraySensorStatus.Cast<string>()}");
        }

        private void RF_Ready_PositionF_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.RF_Ready_Position(0x31, true);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void RF_Ready_PositionB_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.RF_Ready_Position(0x31, false);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void RFM_CardCheck_Click(object sender, RoutedEventArgs e)
        {
            var res = TP7900.RFM_CardCheck(1, 0);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void RFM_PowerOn_Click(object sender, RoutedEventArgs e)
        {
            var data = new byte[100];
            var size = new int[100];
            var res = TP7900.RFM_DUAL_Power(true, 0, data, size);
            //var res =TP7900.RFM_Power(1, 0, 100, data, size);
            if (res == 0)
            {
                var d = data.Skip(1).Take(size[0] - 1);
                MessageBox.Show(BitConverter.ToString(d.ToArray()));
                var data1 = BitConverter.ToString(d.Reverse().ToArray()).Replace("-", string.Empty).ToUpper();
                var uid = Int64.Parse(data1, System.Globalization.NumberStyles.HexNumber).ToString();
                if (uid.Length < 10)
                    uid = new string('0', 10 - uid.Length) + uid;
                MessageBox.Show(uid);
            }
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void RFM_PowerOff_Click(object sender, RoutedEventArgs e)
        {
            var data = new byte[100];
            var size = new int[100];
            var res = TP7900.RFM_DUAL_Power(false, 0, data, size);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }

        private void RFM_Direct_Click(object sender, RoutedEventArgs e)
        {
            var data1 = new byte[100];
            var data2 = new byte[100];
            var size = new int[30];
            var res = TP7900.RFM_Direct(1, 0, data1, 100, data2, size);
            MessageBox.Show(res == 0 ? "Success" : res.ToString());
        }
    }
}
