#region *** copyright ***
/* 
 * Copyright (c) 2012 Hiroaki Goto
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
 * IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace StoneDot.BasicLibrary
{
    public class HotKeyUtilities : IDisposable
    {
        /// <summary>
        /// The RegisterHotKey method registers a hotkey with a system.
        /// </summary>
        /// <param name="hWnd">The window handle that receives messages.</param>
        /// <param name="id">The id of a hotkey.</param>
        /// <param name="modKey">The combination of modifier keys(shift, control, and so on).</param>
        /// <param name="key">The typical key(a, b, 1, 2, :, etc).</param>
        /// <returns>0 if fail to register(it means that another application uses specified combination of keys).</returns>
        [DllImport("user32.dll")]
        private extern static int RegisterHotKey(IntPtr hWnd, int id, int modKey, int key);

        /// <summary>
        /// The UnregisterHotKey method unregisters a hotkey with a system.
        /// </summary>
        /// <param name="hWnd">The window handle that relates to a hotkey</param>
        /// <param name="id">The id of a hotkey.</param>
        /// <returns>0 if fail to unregister.</returns>
        [DllImport("user32.dll")]
        private extern static int UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// The WM_HOTKEY indicates a message id of a hotkey event.
        /// </summary>
        private const int WM_HOTKEY = 0x0312;

        /// <summary>
        /// The range of identifications of hotkeys.
        /// </summary>
        private const int HOTKEY_ID_START = 0x0000;
        private const int HOTKEY_ID_END = 0xbfff;

        private Window _window = null;
        private IntPtr _windowHandle = IntPtr.Zero;
        private HwndSource _windowSource = null;

        /// <summary>
        /// The constrctor of HotKeyUtilities class.
        /// </summary>
        /// <param name="window">Specifiy a window you would like to relate to hotkeys.</param>
        public HotKeyUtilities(Window window)
        {
            AddHookAsWndProc(window);
        }

        private void AddHookAsWndProc(Window window)
        {
            if (window == null) throw new ArgumentNullException();
            _window = window;
            _windowHandle = new WindowInteropHelper(window).EnsureHandle();
            _windowSource = HwndSource.FromHwnd(_windowHandle);
            _windowSource.AddHook(new HwndSourceHook(WndProc));
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkey_id = (int)wParam;
                Debug.Assert(_idIndex.ContainsKey(hotkey_id));
                var action = _idIndex[hotkey_id];
                action();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private class Pair<T1, T2>
        {
            public T1 First { get; private set; }
            public T2 Second { get; private set; }

            public Pair(T1 first, T2 second)
            {
                First = first; Second = second;
            }

            public override int GetHashCode()
            {
                return (First.GetHashCode() << 16) | (Second.GetHashCode() & 0x0000ffff);
            }

            public override bool Equals(object obj)
            {
                var pair = obj as Pair<T1, T2>;
                if (object.ReferenceEquals(pair, null)) return false;
                return First.Equals(pair.First) && Second.Equals(pair.Second);
            }
        }
        private Dictionary<Pair<int, int>, int> _keyPairIndex = new Dictionary<Pair<int, int>, int>();
        private static Dictionary<int, Action> _idIndex = new Dictionary<int, Action>();
        private static int _currentIndex = HOTKEY_ID_START;

        private int GetNextId()
        {
            int startIndex = _currentIndex;
            while (startIndex != ++_currentIndex)
            {
                if (_currentIndex > HOTKEY_ID_END) _currentIndex = HOTKEY_ID_START;
                if (_idIndex.ContainsKey(_currentIndex)) continue;
                return _currentIndex;
            }
            throw new InvalidOperationException("Cannot register the hotkey because of shortage of id.");
        }

        /// <summary>
        /// RegisterHotKey method registers a hotkey with a system.
        /// </summary>
        /// <param name="modKeys">Specify a combination of modifier keys.</param>
        /// <param name="key">Specify a typical key.</param>
        /// <param name="action">Specify an action that is executed when the hotkey is pressed.</param>
        /// <returns>true if the hotkey is registered successfully; otherwise, false</returns>
        public bool RegisterHotKey(ModifierKeys modKeys, Key key, Action action)
        {
            try
            {
                int id = GetNextId();
                _keyPairIndex.Add(new Pair<int, int>((int)modKeys, (int)key), id);
                _idIndex.Add(id, action);
                int result = 0;
                _window.Dispatcher.Invoke((Action)(() =>{
                    result = RegisterHotKey(_windowHandle, id, (int)modKeys, KeyInterop.VirtualKeyFromKey(key));
                }), null);
                return result != 0;
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// UnregisterHotKey method unregisters a hotkey with a system.
        /// </summary>
        /// <param name="modKeys">Specify a combination of modifier keys.</param>
        /// <param name="key">Specify a typical key.</param>
        /// <returns>true if the hotkey is unregistered successfully; otherwise false</returns>
        public bool UnregisterHotKey(ModifierKeys modKeys, Key key)
        {
            var pair = new Pair<int, int>((int)modKeys, (int)key);
            if (!_keyPairIndex.ContainsKey(pair)) return false;
            var id = _keyPairIndex[pair];
            int result = 0;
            _window.Dispatcher.Invoke((Action)(() =>
            {
                result = UnregisterHotKey(_windowHandle, id);
            }), null);
            _keyPairIndex.Remove(pair);
            _idIndex.Remove(id);
            return result != 0;
        }

        public void Dispose()
        {
            foreach (var pair in _keyPairIndex.Keys)
            {
                UnregisterHotKey((ModifierKeys)pair.First, (Key)pair.Second);
            }
            _keyPairIndex.Clear();
            if (_windowSource != null) _windowSource.Dispose();
        }
    }
}
