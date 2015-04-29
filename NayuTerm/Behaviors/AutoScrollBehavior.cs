﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Threading;
using Livet;

namespace NayuTerm.Behaviors
{
    public class AutoScrollBehavior : Behavior<ScrollViewer>
    {
        private ScrollViewer _scrollViewer;
        private double _beforeHeight;

        protected override void OnAttached()
        {
            base.OnAttached();

            _scrollViewer = AssociatedObject;
            _scrollViewer.LayoutUpdated += _scrollViewer_LayoutUpdated;
        }

        private void _scrollViewer_LayoutUpdated(object sender, EventArgs e)
        {
            if (_scrollViewer.ExtentHeight != _beforeHeight)
            {
                _beforeHeight = _scrollViewer.ExtentHeight;

                DispatcherHelper.UIDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    _scrollViewer.ScrollToEnd();
                    Thread.Sleep(1);
                }));
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_scrollViewer != null)
            {
                _scrollViewer.LayoutUpdated -= _scrollViewer_LayoutUpdated;
            }
        }
    }
}
