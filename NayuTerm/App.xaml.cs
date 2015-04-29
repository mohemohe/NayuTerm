using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Livet;
using NayuTerm.Views;
using StoneDot.BasicLibrary;

namespace NayuTerm
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private MainWindow _mainWindow;
        public HotKeyUtilities HotKey { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            DispatcherHelper.UIDispatcher = Dispatcher;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _mainWindow = new MainWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Top = 0, 
                Left = 0
            };
            _mainWindow.Show();

            //ホットキー周りをよしなにやってくれる神ライブラリで神すぎて神
            ApplicationUtilities.ProcessCommand += ProcessCommand;
            HotKey = new HotKeyUtilities(_mainWindow);
            var ret = HotKey.RegisterHotKey(ModifierKeys.Alt, Key.F12, SwitchWindowState);
            if (!ret) MessageBox.Show("Fail to register a hotkey.");
        }

        private void ProcessCommand(object sender, ProcessEventArgs e)
        {
            if (_mainWindow != null) SwitchWindowState();
        }

        private void SwitchWindowState()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Activate();
                }
                else
                {
                    _mainWindow.WindowState = WindowState.Minimized;
                }
            }), null);
        }

        //集約エラーハンドラ
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //TODO:ロギング処理など
            MessageBox.Show(
                "不明なエラーが発生しました。アプリケーションを終了します。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Environment.Exit(1);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            //TODO: nothing
        }
    }
}
