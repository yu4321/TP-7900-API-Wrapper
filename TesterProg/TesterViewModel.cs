using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TP7900APIWrapperForTD1000;

namespace TesterProg
{
    public class TesterViewModel : INotifyPropertyChanged
    {
        private string _loadingText;
        public string LoadingText
        {
            get
            {
                return _loadingText;
            }
            set
            {
                _loadingText = value;
                RaisePropertyChanged(nameof(LoadingText));
            }
        }

        private Visibility _loadingVisibility = Visibility.Collapsed;
        public Visibility LoadingVisibility
        {
            get
            {
                return _loadingVisibility;
            }
            set
            {
                _loadingVisibility = value;
                RaisePropertyChanged(nameof(LoadingVisibility));
            }
        }
        private string log;
        public string Log
        {
            get
            {
                return log;
            }
            set
            {
                log = value;
                RaisePropertyChanged(nameof(Log));
            }
        }

        public ICommand ConnectCommand { get; private set; }
        public ICommand StartReceiveCommand { get; private set; }
        public ICommand StartDispenseCommand { get; private set; }
        public ICommand RejectCommand { get; private set; }
        public ICommand SupplyCommand { get; private set; }

        private string _receivedUid;
        public string ReceivedUid
        {
            get
            {
                return _receivedUid;
            }
            set
            {
                _receivedUid = value;
                RaisePropertyChanged(nameof(ReceivedUid));
            }
        }

        TD1000CardDispenser Dispenser = new TD1000CardDispenser();

        public TesterViewModel()
        {
            ConnectCommand = new RelayCommand<object>(async(x) =>
              {
                  LoadingVisibility = Visibility.Visible;
                  LoadingText = "기기 초기화 작업중입니다!";
                  var res = await Dispenser.Initialize();
                  if (res)
                  {
                      WriteLog("접속 및 초기화 성공. 화면을 클릭해주세요");
                      Dispenser.StartDetectCardMode();
                  }
                  else
                  {
                      WriteLog("접속 및 초기화 실패");
                  }
                  LoadingVisibility = Visibility.Collapsed;
              }, (x) =>
              {
                  return !Dispenser.IsInitialized;
              });
            StartReceiveCommand = new RelayCommand<object>(async (x) =>
            {
                LoadingVisibility = Visibility.Visible;
                LoadingText = "카드를 넣어주세요!";
                var res = await Dispenser.WaitForInsertCard();
                if (res != null)
                {
                    ReceivedUid = BitConverter.ToString(res);

                    var data1 = BitConverter.ToString(res.Reverse().ToArray()).Replace("-", string.Empty).ToUpper();
                    var uid = Int64.Parse(data1, System.Globalization.NumberStyles.HexNumber).ToString();
                    if (uid.Length < 10)
                        uid = new string('0', 10 - uid.Length) + uid;
                    var data2 = BitConverter.ToString(res.ToArray()).Replace("-", string.Empty).ToUpper();
                    var uid2 = Int64.Parse(data2, System.Globalization.NumberStyles.HexNumber).ToString();
                    if (uid2.Length < 10)
                        uid2 = new string('0', 10 - uid2.Length) + uid;
                    WriteLog($"삽입 카드 UID 원문: \n---{ReceivedUid}---\n Reversed Decimal10DigitUid: \n---{uid}---\nDecimal10DigitUid: \n---{uid2}---\n");
                }
                else
                {
                    ReceivedUid = "";
                    WriteLog("카드 삽입 감지 실패");
                }
                LoadingVisibility = Visibility.Collapsed;
            }, (x) =>
            {
                return Dispenser.IsInitialized;
            });

            StartDispenseCommand = new RelayCommand<object>(async (x) =>
            {
                LoadingVisibility = Visibility.Visible;
                LoadingText = "카드를 준비합니다";
                var res = await Dispenser.WaitForDispenseCard();
                if (res != null)
                {
                    ReceivedUid = BitConverter.ToString(res);

                    var data1 = BitConverter.ToString(res.Reverse().ToArray()).Replace("-", string.Empty).ToUpper();
                    var uid = Int64.Parse(data1, System.Globalization.NumberStyles.HexNumber).ToString();
                    if (uid.Length < 10)
                        uid = new string('0', 10 - uid.Length) + uid;
                    WriteLog($"배출 준비 카드 UID 원문: \n---{ReceivedUid}---\n Decimal10DigitUid: \n---{uid}---");
                }
                else
                {
                    ReceivedUid = "";
                    WriteLog("카드 방출 준비 실패");
                }
                LoadingVisibility = Visibility.Collapsed;
            }, (x) =>
            {
                return Dispenser.IsInitialized;
            });

            RejectCommand = new RelayCommand<object>(async (x) =>
            {
                LoadingVisibility = Visibility.Visible;
                LoadingText = "카드를 수납합니다";
                var res = await Dispenser.RejectCurrentCard();
                if (res)
                {
                    ReceivedUid = "";
                    WriteLog($"카드 수납 성공");
                }
                else
                {
                    ReceivedUid = "";
                    WriteLog("카드 수납 실패");
                }
                LoadingVisibility = Visibility.Collapsed;
            }, (x) =>
            {
                return Dispenser.IsInitialized;
            });

            SupplyCommand = new RelayCommand<object>(async (x) =>
            {
                LoadingVisibility = Visibility.Visible;
                LoadingText = "카드를 방출합니다";
                var res = await Dispenser.DispenseCurrentCard();
                if (res)
                {
                    ReceivedUid = "";
                    WriteLog($"카드 방출 성공");
                }
                else
                {
                    ReceivedUid = "";
                    WriteLog("카드 방출 실패");
                }
                LoadingVisibility = Visibility.Collapsed;
            }, (x) =>
            {
                return Dispenser.IsInitialized;
            });
        }

        private void WriteLog(string s)
        {
            Log += $"{DateTime.Now} - {s}\n";
        }

        #region Impl PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            // ISSUE: reference to a compiler-generated field
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged((object)this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion
    }
}
