// <copyright file="WopiFile.cs" company="Bit, LLC">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author>ock</author>
// <date></date>
// <summary></summary>


using Cobalt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WopiBasicEditor
{
    public class WopiSession
    {
        private readonly string _accessId;
        private readonly string _login;
        private readonly string _name;
        private readonly string _email;
        private readonly bool _isAnonymous;
        private readonly FileInfo _fileinfo;
        private readonly CobaltFile _cobaltFile;
        private readonly DisposalEscrow _disposal;
        private DateTime _lastUpdated;

        public WopiSession(string accessId, string filePath, string login = "Anonymous", string name = "Anonymous", string email = "", bool isAnonymous = true)
        {
            _accessId = accessId;
            _fileinfo = new FileInfo(filePath);
            _name = name;
            _login = login;
            _email = email;
            _isAnonymous = isAnonymous;
            _disposal = new DisposalEscrow(_accessId);

            CobaltFilePartitionConfig content = new CobaltFilePartitionConfig();
            content.IsNewFile = true;
            content.HostBlobStore = new TemporaryHostBlobStore(new TemporaryHostBlobStore.Config(), _disposal, _accessId + @".Content");
            content.cellSchemaIsGenericFda = true;
            content.CellStorageConfig = new CellStorageConfig();
            content.Schema = CobaltFilePartition.Schema.ShreddedCobalt;
            content.PartitionId = FilePartitionId.Content;

            CobaltFilePartitionConfig coauth = new CobaltFilePartitionConfig();
            coauth.IsNewFile = true;
            coauth.HostBlobStore = new TemporaryHostBlobStore(new TemporaryHostBlobStore.Config(), _disposal, _accessId + @".CoauthMetadata");
            coauth.cellSchemaIsGenericFda = false;
            coauth.CellStorageConfig = new CellStorageConfig();
            coauth.Schema = CobaltFilePartition.Schema.ShreddedCobalt;
            coauth.PartitionId = FilePartitionId.CoauthMetadata;

            CobaltFilePartitionConfig wacupdate = new CobaltFilePartitionConfig();
            wacupdate.IsNewFile = true;
            wacupdate.HostBlobStore = new TemporaryHostBlobStore(new TemporaryHostBlobStore.Config(), _disposal, _accessId + @".WordWacUpdate");
            wacupdate.cellSchemaIsGenericFda = false;
            wacupdate.CellStorageConfig = new CellStorageConfig();
            wacupdate.Schema = CobaltFilePartition.Schema.ShreddedCobalt;
            wacupdate.PartitionId = FilePartitionId.WordWacUpdate;

            Dictionary<FilePartitionId, CobaltFilePartitionConfig> pd = new Dictionary<FilePartitionId, CobaltFilePartitionConfig>();
            pd.Add(FilePartitionId.Content, content);
            pd.Add(FilePartitionId.WordWacUpdate, wacupdate);
            pd.Add(FilePartitionId.CoauthMetadata, coauth);

            _cobaltFile = new CobaltFile(_disposal, pd, new WopiHostLockingStore(this), null);

            if (_fileinfo.Exists)
            {
                var src = FileAtom.FromExisting(_fileinfo.FullName, _disposal);
                Cobalt.Metrics o1;
                _cobaltFile.GetCobaltFilePartition(FilePartitionId.Content).SetStream(RootId.Default.Value, src, out o1);
                _cobaltFile.GetCobaltFilePartition(FilePartitionId.Content).GetStream(RootId.Default.Value).Flush();
            }
        }

        public string AccessId
        {
            get { return _accessId; }
            set {}
        }

        public string Login
        {
            get { return _login; }
            set {}
        }

        public string Name
        {
            get { return _name; }
            set {}
        }

        public string Email
        {
            get { return _email; }
            set {}
        }

        public bool IsAnonymous
        {
            get { return _isAnonymous; }
            set {}
        }

        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set {}
        }

        public WopiCheckFileInfo GetCheckFileInfo()
        {
            WopiCheckFileInfo cfi = new WopiCheckFileInfo();

            cfi.BaseFileName = _fileinfo.Name;
            cfi.OwnerId = _login;

            lock (_fileinfo)
            {
                if (_fileinfo.Exists)
                {
                    cfi.Size = _fileinfo.Length;
                }
                else
                {
                    cfi.Size = 0;
                }
            }

            cfi.Version = _fileinfo.LastWriteTimeUtc.ToString("s");
            cfi.SupportsCoauth = false;
            cfi.SupportsCobalt = true;
            cfi.SupportsFolders = true;
            cfi.SupportsLocks = false;
            cfi.SupportsScenarioLinks = false;
            cfi.SupportsSecureStore = false;
            cfi.SupportsUpdate = true;
            cfi.UserCanWrite = true;

            return cfi;
        }

        public Bytes GetFileContent()
        {
            return new GenericFda(_cobaltFile.CobaltEndpoint, null).GetContentStream();
        }

        public long FileLength
        {
            get
            {
                return _fileinfo.Length;
            }
        }

        public void Save()
        {
            lock (_fileinfo)
            {
                using (FileStream fileStream = _fileinfo.OpenWrite())
                {
                    new GenericFda(_cobaltFile.CobaltEndpoint, null).GetContentStream().CopyTo(fileStream);
                }
            }
        }

        public void ExecuteRequestBatch(RequestBatch requestBatch)
        {
            _cobaltFile.CobaltEndpoint.ExecuteRequestBatch(requestBatch);
            _lastUpdated = DateTime.Now;
        }

        internal void Dispose()
        {
            _disposal.Dispose();
        }
    }
}
