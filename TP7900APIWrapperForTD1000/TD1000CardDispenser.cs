using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TP7900APIWrapperForTD1000
{
    public class TD1000CardDispenser : IDisposable
    {
        public Action<string> LoggingAction;
        public delegate void TP7900InsertedCardUidReceivedHandler(object sender, TP7900InsertedCardUidEventArgs e);
        public event TP7900InsertedCardUidReceivedHandler UidReceived;

        public bool IsInitialized { get; private set; }

        public bool IsDetecting
        {
            get
            {
                return CardEntryWatcher == null ? false : CardEntryWatcher.Enabled;
            }
        }

        private System.Timers.Timer CardEntryWatcher;

        private int lastEntryStatus = 0;

        bool isRunning = false;

        bool ignoreInsertDetection = false;

        bool ignoreRfModule = false;

        TaskCompletionSource<bool> CardExtractedSource;

        private async Task<bool> BoolReturnPromise(Func<int> func, int timeout=0)
        {
            var promise = new TaskCompletionSource<bool>();

            if (timeout != 0)
            {
                var cancelTokenSource = new CancellationTokenSource();

                cancelTokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

                using (cancelTokenSource.Token.Register(() => {
                    promise.TrySetCanceled();
                }))
                {
                    Task.Run(() =>
                    {
                        var res = func.Invoke();
                        promise.SetResult(res == 0);
                    }, cancelTokenSource.Token);

                    try
                    {
                        return await promise.Task;
                    }
                    catch (Exception e)
                    {
                        LoggingAction($"Promise Timeout. {e.Message}. canceled");
                        return false;
                    }
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    var res = func.Invoke();
                    promise.SetResult(res == 0);
                });

                try
                {
                    return await promise.Task;
                }
                catch (Exception e)
                {
                    LoggingAction($"Non Timeout Promise Failed - "+e.Message);
                    return false;
                }
            }
        }

        #region Init and Final
        public async Task<bool> Initialize(bool tryRejectBeforeInit, bool isRfModuleIgnored, int port = 0)
        {
            ignoreRfModule = isRfModuleIgnored;
            var openResult = port == 0 ? await BoolReturnPromise(() => TP7900.OpenDevice(2, 0, 1)) : await BoolReturnPromise(() => TP7900.OpenDevice(1, port, 1));
            if (openResult)
            {
                if (tryRejectBeforeInit)
                {
                    await BoolReturnPromise(() => TP7900.RejectCard(), 3);
                }

                bool isInit = await BoolReturnPromise(() => TP7900.InitDevice(), 10);
                if (isInit)
                {
                    LoggingAction($"Initialize Success - tryRejectBeforeInit {tryRejectBeforeInit}, isRfModuleIgnored {isRfModuleIgnored}, port {port}");
                    IsInitialized = true;
                    return true;
                }
                else
                {
                    LoggingAction("Initialize Failed  - tryRejectBeforeInit {tryRejectBeforeInit}, isRfModuleIgnored {isRfModuleIgnored}, port {port}");
                    IsInitialized = false;
                    return false;
                }
            }
            lastEntryStatus = 0;
            return false;
        }

        public void Close()
        {
            IsInitialized = false;
            LoggingAction("Device Closes");
            Task.Run(() =>
            {
                TP7900.CloseDevice();
            });
        }
        #endregion

        #region Basic Card IO

        public async Task<byte[]> ReadCurrentCardData()
        {
            if (ignoreRfModule)
            {
                return null;
            }
            else
            {
                var data = new byte[100];
                var size = new int[100];

                var getValueResult = await BoolReturnPromise(() => TP7900.RFM_DUAL_Power(true, 0, data, size));

                if (!getValueResult)
                {
                    return null;
                }

                return data.Skip(1).Take(size[0] - 1).ToArray();
            }
        }

        public async Task<bool> JustConsumeCard()
        {
            return await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, false));
        }

        public async Task<bool> ReadyDispenseCard()
        {
            return await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, true));
        }


        #endregion


        #region Card Commands
        public async Task<byte[]> WaitForInsertCard()
        {
            var insertResult = await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, false));
            if (!insertResult)
            {
                return null;
            }

            if (ignoreRfModule)
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

            if (ignoreRfModule)
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

        public async Task<bool> DispenseCurrentCard()
        {
            lastEntryStatus = 1;
            return await BoolReturnPromise(() => TP7900.EjectCard());
        }

        public async Task<bool> RejectCurrentCard()
        {
            lastEntryStatus = 1;
            return await BoolReturnPromise(() => TP7900.RejectCard());
        }

        public async Task<bool> AcceptCurrentCard()
        {
            return await RejectCurrentCard();
        }

        public async Task<bool> RevertCurrentCard()
        {
            return await DispenseCurrentCard();
        }

        #endregion

        #region StatusCheck
        /// <summary>
        /// 1: pRailStatus 2: pFeedRollerStatus 3: pTraySensorStatus
        /// </summary>
        /// <returns></returns>
        private async Task<SensorStatus> GetSensorStatus()
        {
            var pRailStatus = new byte[8];
            var pFeedRollerStatus = new byte[8];
            var pTraySensorStatus = new byte[8];
            var statResult = await BoolReturnPromise(() => TP7900.GetSensorStatus(pRailStatus, pFeedRollerStatus, pTraySensorStatus));
            if (statResult)
            {
                //LoggingAction($"GetSensorStatus Result : Rail {BitConverter.ToString(pRailStatus)}, Roller {BitConverter.ToString(pFeedRollerStatus)}, Tray {BitConverter.ToString(pTraySensorStatus)}");
                return new SensorStatus(pRailStatus, pFeedRollerStatus, pTraySensorStatus);
            }
            else
            {
                LoggingAction($"GetSensorStatus Result False");
                return null;
            }
        }

        public async Task<bool> CanDispense()
        {
            LoggingAction("CanDispense - Try");
            var status = await GetSensorStatus();
            if (status != null)
            {
                if (status.TraySensorStatus[0] > 1)
                {
                    LoggingAction("CanDispense True");
                    return true;
                }
            }
            LoggingAction("CanDispense False");
            return false;
        }

        public async Task<bool?> IsCardInserted()
        {
            //LoggingAction("IsCardInserted - Try");
            var status = await GetSensorStatus();
            if (status != null)
            {
                if (lastEntryStatus != status.RailStatus[0] && (status.RailStatus[0] == 1 || status.RailStatus[0] == 0)) 
                {
                    lastEntryStatus = status.RailStatus[0];
                    if (lastEntryStatus == 1)
                    {
                        //LoggingAction("IsCardInserted True");
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            //LoggingAction("IsCardInserted False");
            return null;
        }


        public EjectSpeedInfo GetCurrentEjectSpeed()
        {
            byte[] pTotalNumOfTray = new byte[100];
            byte[] pLoopFlag = new byte[100];
            uint pEjectLength = 0;
            uint pEjectSpeed = 0;
            byte[] pTrayInfo = new byte[100];
            var res = TP7900.Get_TraySchedule(pTotalNumOfTray, pLoopFlag, ref pEjectLength, ref pEjectSpeed, pTrayInfo);
            return new EjectSpeedInfo()
            {
                EjectLength = pEjectLength,
                EjectSpeed = pEjectSpeed
            };
        }

        public bool SetCurrentEjectSpeed(EjectSpeedInfo speed)
        {
            byte[] pTotalNumOfTray = new byte[100];
            byte[] pLoopFlag = new byte[100];
            uint pEjectLength = 0;
            uint pEjectSpeed = 0;
            byte[] pTrayInfo = new byte[100];
            var res = TP7900.Get_TraySchedule(pTotalNumOfTray, pLoopFlag, ref pEjectLength, ref pEjectSpeed, pTrayInfo);
            var res2 = TP7900.Set_TraySchedule(pTotalNumOfTray[0], pLoopFlag[0], speed.EjectLength, speed.EjectSpeed, pTrayInfo);
            return res2 == 0;
        }

        #endregion

        #region AutoCheckMode
        public void StartDetectCardMode()
        {
            if (CardEntryWatcher == null)
            {
                LoggingAction("CardEntryWatcher Created");
                CardEntryWatcher = new System.Timers.Timer(500);
                CardEntryWatcher.Elapsed += CardEntryWatcher_Elapsed;
            }
            LoggingAction("StartDetectCardMode - Method");
            CardEntryWatcher.Start();
        }

        public void EndDetectCardMode()
        {
            LoggingAction("EndDetectCardMode");
            if (CardEntryWatcher != null)
            {
                CardEntryWatcher.Stop();
            }
        }

        #endregion

        #region Card Pickup Waiter

        /// <summary>
        /// CannotWork Without CardEntryWatcher_Elapsed Working
        /// </summary>
        /// <returns></returns>
        public async Task<bool> WaitForCardPickup()
        {
            LoggingAction("Start Wait Card Pick Up. ignoreInsertDetecion");
            ignoreInsertDetection = true;
            CardExtractedSource = new TaskCompletionSource<bool>();
            var res = await CardExtractedSource.Task;
            LoggingAction("Finish Wait Card Pick Up");
            CardExtractedSource = null;
            Task.Run(async () =>
            {
                LoggingAction("IgnoreInsertDetection Release after 1 sec");
                await Task.Delay(1000);
                ignoreInsertDetection = false;
                LoggingAction("IgnoreInsertDetection Released");
            });
            return res;
        }

        #endregion

        #region Misc.

        private async void CardEntryWatcher_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isRunning)
            {
                //LoggingAction("Elapsed 중복 실행 방지");
                return;
            }

            isRunning = true;

            var statResult = await GetSensorStatus();
            if (statResult != null)
            {
                if(lastEntryStatus!=statResult.RailStatus[0] && (statResult.RailStatus[0] == 1 || statResult.RailStatus[0] == 0))
                {
                    lastEntryStatus = statResult.RailStatus[0];
                    if(lastEntryStatus==1 && CardEntryWatcher.Enabled)
                    {
                        LoggingAction("CardInsertDetected");
                        if (ignoreInsertDetection)
                        {
                            LoggingAction("Ignored by Pickup Ready");
                        }
                        else
                        {
                            var insertResult = await JustConsumeCard();
                            if (!insertResult)
                            {
                                UidReceived?.Invoke(this, null);
                            }
                            else
                            {
                                UidReceived?.Invoke(this, new TP7900InsertedCardUidEventArgs(await ReadCurrentCardData()));
                            }
                        }
                    }
                    else
                    {
                        LoggingAction("CardPickedDetected");
                        if (CardExtractedSource != null)
                        {
                            LoggingAction("CardPick Task Resolved");
                            CardExtractedSource.TrySetResult(true);
                        }
                        UidReceived(this, new TP7900InsertedCardUidEventArgs(TP7900SignalTypes.PickUp));
                    }
                }
            }
            isRunning = false;
        }

        public void StartWaitingForCardInsertion()
        {
            Task.Run(async () =>
            {
                var insertResult = await BoolReturnPromise(() => TP7900.RF_Ready_Position(0x31, false));
                if (!insertResult)
                {
                    UidReceived?.Invoke(this, null);
                }

                if (ignoreRfModule)
                {
                    UidReceived?.Invoke(this, new TP7900InsertedCardUidEventArgs(null));
                    return;
                }

                var data = new byte[100];
                var size = new int[100];
                var getValueResult = await BoolReturnPromise(() => TP7900.RFM_DUAL_Power(true, 0, data, size));

                if (!getValueResult)
                {
                    UidReceived?.Invoke(this, new TP7900InsertedCardUidEventArgs(null));
                    return;
                }

                UidReceived?.Invoke(this, new TP7900InsertedCardUidEventArgs(data.Skip(1).Take(size[0] - 1).ToArray()));
            });
        }
        #endregion

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
                        if (CardEntryWatcher != null)
                        {
                            CardEntryWatcher.Dispose();
                            CardEntryWatcher = null;
                        }
                        CardExtractedSource = null;
                        isRunning = false;
                        lastEntryStatus = 0;
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

    public enum TP7900SignalTypes { Inserted, PickUp }

    public class TP7900InsertedCardUidEventArgs : EventArgs
    {
        public TP7900InsertedCardUidEventArgs(byte[] receivedData)
        {
            ReceivedData = receivedData;
            EventType = TP7900SignalTypes.Inserted;
        }

        public TP7900InsertedCardUidEventArgs(TP7900SignalTypes type, byte[] receivedData = null)
        {
            ReceivedData = receivedData;
            EventType = type;
        }

        public TP7900SignalTypes EventType { get; set; }

        public byte[] ReceivedData { get; set; }

    }

    public class EjectSpeedInfo
    {
        public uint EjectLength { get; set; }
        public uint EjectSpeed { get; set; }
    }

    public class SensorStatus
    {
        public byte[] RailStatus { get; set; }
        public byte[] FeedRollerStatus { get; set; }
        public byte[] TraySensorStatus { get; set; }

        public SensorStatus()
        {
            RailStatus = null;
            FeedRollerStatus = null;
            TraySensorStatus = null;
        }

        public SensorStatus(byte[] pRailStatus, byte[] pFeedRollerStatus, byte[] pTraySensorStatus)
        {
            RailStatus = pRailStatus;
            FeedRollerStatus = pFeedRollerStatus;
            TraySensorStatus = pTraySensorStatus;
        }
    }
}
