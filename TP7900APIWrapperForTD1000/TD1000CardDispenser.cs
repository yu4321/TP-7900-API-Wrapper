using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TP7900APIWrapperForTD1000
{
    public class TD1000CardDispenser
    {
        public Action<string> LoggingAction;
        public delegate void TP7900InsertedCardUidReceivedHandler(object sender, TP7900InsertedCardUidEventArgs e);
        public event TP7900InsertedCardUidReceivedHandler UidReceived;

        public bool IsInitialized { get; private set; }

        public async Task<bool> Initialize()
        {

        }

        public async Task<bool> WaitForInsertCard()
        {

        }

        public async Task<bool> StartWaitingForCardInsertion()
        {

        }

        public async Task<bool> DispenseCurrentCard()
        {

        }

        public async Task<bool> AcceptCurrentCard()
        {

        }
    }

    public class TP7900InsertedCardUidEventArgs : EventArgs
    {
        public TP7900InsertedCardUidEventArgs(byte[] receivedData)
        {
            ReceivedData = receivedData;
        }

        public byte[] ReceivedData { get; set; }

    }
}
