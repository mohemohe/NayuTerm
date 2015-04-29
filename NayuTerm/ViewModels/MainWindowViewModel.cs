using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
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

        private ProcessStartInfo _processStartInfo;
        private Process _cmd;

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
                    _cmd.StandardInput.WriteLine(StdIn);
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

        public void Initialize()
        {
            BackBuffer += @"NayuTerm    " + Assembly.GetExecutingAssembly().GetName().Version + "\r\n";

            _processStartInfo = new ProcessStartInfo
            {
                FileName = @"cmd.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                Arguments = @"/k /Q"
            };
            
            _cmd = Process.Start(_processStartInfo);
            _cmd.OutputDataReceived += CmdDataReceived;
            _cmd.ErrorDataReceived += CmdDataReceived; 
            _cmd.BeginOutputReadLine();

            _cmd.StandardInput.WriteLine();
        }

        public void Dispose()
        {
            _cmd.Dispose();
        }

        private void CmdDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            var stdout = dataReceivedEventArgs.Data;
            var regex = new Regex(@"([a-zA-Z]:\\[^>]*)(>$)", RegexOptions.Multiline);
            var matches = regex.Matches(stdout);
            if (matches.Count != 0)
            {
                CurrentDir = matches[0].ToString();
                if (CurrentDir.EndsWith(">"))
                {
                    CurrentDir = CurrentDir.Substring(0, CurrentDir.Length - 1);
                }
                stdout = "[" + CurrentDir + "] " + "(な・ω・ゆ)" + " $ ";
            }
            BackBuffer += stdout.Replace(CurrentDir + ">", "") + "\r\n";
        }

        public void PreviewKeyDown()
        {
            CursorPosition = FrontBuffer.LongCount() + 1;
        }
    }
}
