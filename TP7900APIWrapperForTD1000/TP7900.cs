using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TP7900APIWrapperForTD1000
{
    public class TP7900
    {
        [DllImport("TP7900.dll")]
        public static extern int GetNumOfConnectedDevices(int nPortType, int[] pNumOfDevices);

        [DllImport("TP7900.dll")]
        public static extern int Get_DeviceName(int nIndex, StringBuilder pszDeviceName);

        [DllImport("TP7900.dll")]
        public static extern int OpenDevice(int ConnectionType, int iParam1, int iParam2);

        [DllImport("TP7900.dll")]
        public static extern int CloseDevice();

        [DllImport("TP7900.dll")]
        public static extern int InitDevice();

        [DllImport("TP7900.dll")]
        public static extern int Get_VersionInfo(StringBuilder InfoBuffer);

        [DllImport("TP7900.dll")]
        public static extern int Get_Status(string st1, string st2);

        [DllImport("TP7900.dll")]
        public static extern int SupplyCard(int nTrayNum);

        [DllImport("TP7900.dll")]
        public static extern int EjectCard();

        [DllImport("TP7900.dll")]
        public static extern int RejectCard();

        [DllImport("TP7900.dll")]
        public static extern int GetSensorStatus(byte[] pRailStatus, byte[] pFeedRollerStatus, byte[] pTraySensorStatus);

        [DllImport("TP7900.dll")]
        public static extern int RF_Ready_Position(byte byTrayNumber, bool bFront);

        [DllImport("TP7900.dll")]
        public static extern int RFM_CardCheck(int nAntenna, int nCardType);

        [DllImport("TP7900.dll")]
        public static extern int RFM_Power(int nAntenna, int nCardType, int nOnTime, byte[] pRxData, int[] pRxSize);

        [DllImport("TP7900.dll")]
        public static extern int RFM_Direct(int nAntenna, int nCardType, byte[] pTxData, int nTxSize, byte[] pRxData, int[] pRxSize);

        [DllImport("TP7900.dll")]
        public static extern int RFM_DUAL_Power(bool bIsPower, int CardType, byte[] pRxData, int[] pRxSize);

        [DllImport("TP7900.dll")]
        public static extern int Get_TraySchedule(byte[] pTotalNumOfTray, byte[] pLoopFlag, ref uint pEjectLength, ref uint pEjectSpeed, byte[] pTrayInfo);

        [DllImport("TP7900.dll")]
        public static extern int Set_TraySchedule(byte nTotalNumOfTray, byte pLoopFlag, uint pEjectLength, uint pEjectSpeed, byte[] pTrayInfo);


    }
}
