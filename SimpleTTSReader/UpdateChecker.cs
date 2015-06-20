﻿#region

using System;
using System.Windows.Threading;
using SimpleTTSReader.Properties;

#endregion

namespace SimpleTTSReader
{
    internal class UpdateChecker
    {
        private static readonly DispatcherTimer _updateTimer = new DispatcherTimer();

        static UpdateChecker()
        {
            _updateTimer.Tick += (sender, args) => ClickOnceHelper.CheckForUpdates(true);
            _updateTimer.Interval = new TimeSpan(1, 0, 0);
        }

        public static void Start()
        {
            if (ClickOnceHelper.IsUpdateable && Settings.Default.CheckForUpdates)
                _updateTimer.Start();
        }

        public static void Stop()
        {
            _updateTimer.Stop();
        }
    }
}