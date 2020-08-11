using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TP7900APIWrapperForTD1000
{
    public class TD1000CardDispenser : IDisposable
    {
        public Action<string> LoggingAction;
        public delegate void TP7900InsertedCardUidReceivedHandler(object sender, TP7900InsertedCardUidEventArgs e);
        public event TP7900InsertedCardUidReceivedHandler UidReceived;

        public bool IsInitialized { get; private set; }

        private async Task<bool> BoolReturnPromise(Func<int> func)
        {
            var promise = new TaskCompletionSource<bool>();
            await Task.Run(() =>
            {
                var res = func.Invoke();
                promise.SetResult(res == 0);
            });
            return await promise.Task;
        }

        public async Task<bool> Initialize()
        {
            var openResult = await BoolReturnPromise(() => TP7900.OpenDevice(2, 0, 1));
            if (openResult)
            {
                IsInitialized = true;
                return await BoolReturnPromise(() => TP7900.InitDevice());
            }
            return false;
        }

        public void Close()
        {
            IsInitialized = false;
            Task.Run(() =>
            {
                TP7900.CloseDevice();
            });
        }

        public async Task<bool> CanDispense()
        {
            var pRailStatus = new byte[8];
            var pFeedRollerStatus = new byte[8];
            var pTraySensorStatus = new byte[8];
            var statResult = await BoolReturnPromise(() => TP7900.GetSensorStatus(pRailStatus, pFeedRollerStatus, pTraySensorStatus));
            if (statResult)
            {
                return pTraySensorStatus[0] == 2;
            }
            else
            {
                return false;
            }
        }

        public async Task<byte[]> WaitForInsertCard()
        {
            var insertResult= await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, false));
            if (!insertResult)
            {
                return null;
            }

            var data = new byte[100];
            var size = new int[100];
            var getValueResult = await BoolReturnPromise(() => TP7900.RFM_DUAL_Power(true, 0, data, size));

            if (!getValueResult)
            {
                return null;
            }

            return data.Skip(1).Take(size[0] - 1).ToArray();
        }

        public async Task<byte[]> WaitForDispenseCard()
        {
            var insertResult = await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, true));
            if (!insertResult)
            {
                return null;
            }

            var data = new byte[100];
            var size = new int[100];
            var getValueResult = await BoolReturnPromise(() => TP7900.RFM_DUAL_Power(true, 0, data, size));

            if (!getValueResult)
            {
                return null;
            }

            return data.Skip(1).Take(size[0] - 1).ToArray();
        }

        public void StartWaitingForCardInsertion()
        {
            Task.Run(async() =>
            {
                var insertResult = await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, false));
                if (!insertResult)
                {
                    UidReceived(this, null);
                }

                var data = new byte[100];
                var size = new int[100];
                var getValueResult = await BoolReturnPromise(() => TP7900.RFM_DUAL_Power(true, 0, data, size));

                if (!getValueResult)
                {
                    UidReceived(this, new TP7900InsertedCardUidEventArgs(null));
                }

                UidReceived(this,new TP7900InsertedCardUidEventArgs(data.Skip(1).Take(size[0] - 1).ToArray()));
            });
        }

        public async Task<bool> DispenseCurrentCard()
        {
            return await BoolReturnPromise(() => TP7900.EjectCard());
        }

        public async Task<bool> RejectCurrentCard()
        {
            return await BoolReturnPromise(() => TP7900.RejectCard());
        }

        public async Task<bool> AcceptCurrentCard()
        {
            return await BoolReturnPromise(() => TP7900.RejectCard());
        }

        public async Task<bool> RevertCurrentCard()
        {
            return await BoolReturnPromise(() => TP7900.EjectCard());
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (IsInitialized)
                    {
                        Close();
                    }
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        // ~TD1000CardDispenser() {
        //   // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
        //   Dispose(false);
        // }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            // GC.SuppressFinalize(this);
        }
        #endregion
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
