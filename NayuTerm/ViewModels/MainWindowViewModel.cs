using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using NayuTerm.Models;

namespace NayuTerm.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ 
         * 
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *  
         * を使用してください。
         * 
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         * 
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         * 
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         * 
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         * 
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         * 
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        private static readonly string HomeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

        #region FrontBuffer変更通知プロパティ
        private string _FrontBuffer = "";

        public string FrontBuffer
        {
            get
            { return _FrontBuffer; }
            set
            {
                if (value.Length - BackBuffer.Length < 0)
                {
                    _FrontBuffer = BackBuffer;
                    CursorPosition = BackBuffer.LongCount();
                }
                else
                {
                    _FrontBuffer = value;
                }

                StdIn = FrontBuffer.Substring(BackBuffer.Length, FrontBuffer.Length - BackBuffer.Length);

                var backBufferLines = BackBuffer.Count(c => c.Equals('\n')) + 1;
                var frontBufferLines = FrontBuffer.Count(c => c.Equals('\n')) + 1;
                if (frontBufferLines > backBufferLines)
                {
                    var stdin = StdIn;
                    foreach (var alias in RunCommand.Environment.AliasList)
                    {
                        if (stdin.StartsWith(alias.Before))
                        {
                            stdin = alias.After +
                                    stdin.Substring(alias.Before.Length, stdin.Length - alias.Before.Length);
                            break;
                        }
                    }
                    if (stdin == "reload\r\n")
                    {
                        BackBuffer += stdin;
                        RunCommand.Initialize();
                        stdin = "\r";
                    }
                    if (stdin == "\r\n")
                    {
                        //MEMO:\r\nではなく\nのみ
                        BackBuffer += "\n";
                        stdin = "";
                    }
                    ShellHost.Shell.StandardInput.WriteLine(stdin);
                }
                RaisePropertyChanged();
            }
        }
        #endregion FrontBuffer変更通知プロパティ

        #region BackBuffer変更通知プロパティ
        private string _BackBuffer = "";

        public string BackBuffer
        {
            get
            { return _BackBuffer; }
            set
            { 
                if (_BackBuffer == value)
                    return;
                if (value.EndsWith("$ \r\n"))
                {
                    _BackBuffer = value.Substring(0, value.Length - 2);
                }
                else
                {
                    _BackBuffer = value;
                }
                FrontBuffer = _BackBuffer;
            }
        }
        #endregion BackBuffer変更通知プロパティ

        #region CursorPosition変更通知プロパティ
        private long _CursorPosition;

        public long CursorPosition
        {
            get
            { return _CursorPosition; }
            set
            { 
                if (_CursorPosition == value)
                    return;
                _CursorPosition = value;
                RaisePropertyChanged();
            }
        }
        #endregion CursorPosition変更通知プロパティ

        #region CurrentDir変更通知プロパティ
        private string _CurrentDir;

        public string CurrentDir
        {
            get
            { return _CurrentDir; }
            set
            { 
                if (_CurrentDir == value)
                    return;
                _CurrentDir = value;
                RaisePropertyChanged();
            }
        }
        #endregion CurrentDir変更通知プロパティ

        #region StdIn変更通知プロパティ
        private string _StdIn;

        public string StdIn
        {
            get
            { return _StdIn; }
            set
            { 
                if (_StdIn == value)
                    return;
                _StdIn = value;
                RaisePropertyChanged();
            }
        }
        #endregion StdIn変更通知プロパティ

        #region ForegroundColor変更通知プロパティ
        private string _ForegroundColor;

        public string ForegroundColor
        {
            get
            { return _ForegroundColor; }
            set
            { 
                if (_ForegroundColor == value)
                    return;
                _ForegroundColor = value;
                RaisePropertyChanged();
            }
        }
        #endregion ForegroundColor変更通知プロパティ

        #region BackgroundColor変更通知プロパティ
        private string _BackgroundColor;

        public string BackgroundColor
        {
            get
            { return _BackgroundColor; }
            set
            { 
                if (_BackgroundColor == value)
                    return;
                _BackgroundColor = value;
                RaisePropertyChanged();
            }
        }
        #endregion BackgroundColor変更通知プロパティ

        public void Initialize()
        {
            BackBuffer += @"NayuTerm    " + Assembly.GetExecutingAssembly().GetName().Version + "\r\n";

            ShellHost.Initialize(@"cmd.exe");
            ShellHost.Shell.OutputDataReceived += DataReceived;
            ShellHost.Shell.ErrorDataReceived += DataReceived;
            ShellHost.StartReadStdOut();

            RunCommand.Initialized += RunCommand_Initialized;
            ForegroundColor = RunCommand.Environment.ForegroundColorARGB;
            BackgroundColor = RunCommand.Environment.BackgroundColorARGB;
        }

        void RunCommand_Initialized(object sender, System.EventArgs e)
        {
            ForegroundColor = RunCommand.Environment.ForegroundColorARGB;
            BackgroundColor = RunCommand.Environment.BackgroundColorARGB;
        }

        public new void Dispose()
        {
            ShellHost.Dispose();
            base.Dispose();
        }

        private void DataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            var stdout = dataReceivedEventArgs.Data;
            if (stdout == null)
            {
                return;
            }
            var regex = new Regex(@"(?<currentdir>[a-zA-Z]:\\[^>]*)(>$)", RegexOptions.Multiline);
            var match = regex.Match(stdout);
            if (match.Success)
            {
                CurrentDir = match.Groups["currentdir"].ToString();
                if (CurrentDir.Contains(HomeDir))
                {
                    stdout = "[" + CurrentDir.Replace(HomeDir, "~") + "] " + "(な・ω・ゆ)" + " $ ";
                }
                else
                {
                    stdout = "[" + CurrentDir + "] " + "(な・ω・ゆ)" + " $ ";
                }
            }
            BackBuffer += stdout.Replace(CurrentDir + ">", "") + "\r\n";
        }

        public void PreviewKeyDown()
        {
            if (CursorPosition < BackBuffer.LongCount())
            {
                CursorPosition = FrontBuffer.LongCount() + 1;
            }
        }
    }
}
