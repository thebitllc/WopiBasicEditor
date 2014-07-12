// <copyright file="WopiSessionManager.cs" company="Bit, LLC">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author>ock</author>
// <date></date>
// <summary></summary>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobalt;
using System.Timers;

namespace WopiBasicEditor
{
    public class WopiSessionManager
    {
        private static volatile WopiSessionManager _instance;
        private static object _syncRoot = new object();
        private Dictionary<String, WopiSession> _sessions;
        private Timer _timer;
        private readonly int _timeout = 60 * 60 * 1000;

        public static WopiSessionManager Instance
        {
            get
            {
                if (WopiSessionManager._instance == null)
                {
                    lock (WopiSessionManager._syncRoot)
                    {
                        if (WopiSessionManager._instance == null)
                            WopiSessionManager._instance = new WopiSessionManager();
                    }
                }
                return WopiSessionManager._instance;
            }
        }

        public WopiSessionManager()
        {
            _timer = new Timer(_timeout);
            _timer.AutoReset = true;
            _timer.Elapsed += CleanUp;
            _timer.Enabled = true;

            _sessions = new Dictionary<String, WopiSession>();
        }

        public WopiSession GetSession(string accessId)
        {
            WopiSession cf;

            lock (WopiSessionManager._syncRoot)
            {
                if (!_sessions.TryGetValue(accessId, out cf))
                {
                    return null;
                }
            }

            return cf;
        }

        public void AddSession(WopiSession session)
        {
            lock (WopiSessionManager._syncRoot)
            {
                _sessions.Add(session.AccessId, session);
            }
        }

        private void CleanUp(object sender, ElapsedEventArgs e)
        {
            foreach (var session in _sessions.Values)
            {
                if (session.LastUpdated.AddMilliseconds(_timeout) < DateTime.Now)
                {
                    // save the changes to the file
                    session.Save();

                    // clean up
                    session.Dispose();
                    _sessions.Remove(session.AccessId);
                }
            }
        }
    }
}
