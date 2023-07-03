//  Copyright (C) 2015, The Duplicati Team
//  http://www.duplicati.com, info@duplicati.com
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using Duplicati.Library.Backend.OpenStack;
using Duplicati.Library.Interface;
using Duplicati.Library.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Duplicati.Library.Backend.HubiC
{
    // ReSharper disable once UnusedMember.Global
    // This class is instantiated dynamically in the BackendLoader.
    public class HubiCBackend : IBackend, IBackendPagination
    {
        private const string AUTHID_OPTION = "authid";
        private const string HUBIC_API_URL = "https://api.hubic.com/1.0/";
        private const string HUBIC_API_CREDENTIAL_URL = HUBIC_API_URL + "account/credentials";

        private OpenStackHelper m_openstack;

        private class HubiCAuthResponse
        {
            public string token { get; set; }
            public string endpoint { get; set; }
            public DateTime? expires { get; set; }
        }

        private class OpenStackHelper : OpenStackStorage
        {
            private readonly OAuthHelper m_helper;
            private HubiCAuthResponse m_token;

            public OpenStackHelper(string authid, string url)
                : base(url, MockOptions())
            {
                m_helper = new OAuthHelper(authid, "hubic") { AutoAuthHeader = true };
            }

            private static Dictionary<string, string> MockOptions()
            {
                var res = new Dictionary<string, string>();
                res["openstack-authuri"] = "invalid://dont-use";
                res["openstack-apikey"] = "invalid";
                res["auth-username"] = "invalid";

                return res;
            }

            private HubiCAuthResponse AuthToken
            {
                get
                {
                    if (m_token == null
                        || (m_token.expires != null && (m_token.expires.Value - DateTime.UtcNow).TotalSeconds < 30))
                    {
                        m_token = m_helper.ReadJSONResponseAsync<HubiCAuthResponse>(
                            HUBIC_API_CREDENTIAL_URL, null, "GET", CancellationToken.None).Await();
                    }
                    return m_token;
                }
            }

            protected override string AccessToken
            {
                get { return AuthToken.token; }
            }

            protected override string SimpleStorageEndPoint
            {
                get { return AuthToken.endpoint; }
            }

            public string EndPointDnsName
            {
                get
                {
                    if (m_token == null || string.IsNullOrWhiteSpace(m_token.endpoint))
                        return null;

                    return new System.Uri(m_token.endpoint).Host;
                }
            }
        }

        public HubiCBackend()
        {
        }

        public HubiCBackend(string url, Dictionary<string, string> options)
        {
            string authid = null;
            if (options.ContainsKey(AUTHID_OPTION))
                authid = options[AUTHID_OPTION];

            m_openstack = new OpenStackHelper(authid, url);
        }

        #region IBackendPagination implementation

        public IAsyncEnumerable<IFileEntry> ListEnumerableAsync(CancellationToken cancelToken)
        {
            return m_openstack.ListEnumerableAsync(cancelToken);
        }

        #endregion

        #region IBackend implementation

        public Task<IList<IFileEntry>> ListAsync(CancellationToken cancelToken)
        {
            return m_openstack.ListAsync(cancelToken);
        }

        public Task PutAsync(string remotename, Stream source, CancellationToken cancelToken)
        {
            return m_openstack.PutAsync(remotename, source, cancelToken);
        }

        public Task GetAsync(string remotename, Stream destination, CancellationToken cancelToken)
        {
            return m_openstack.GetAsync(remotename, destination, cancelToken);
        }

        public Task DeleteAsync(string remotename, CancellationToken cancelToken)
        {
            return m_openstack.DeleteAsync(remotename, cancelToken);
        }

        public Task TestAsync(CancellationToken cancelToken)
        {
            return m_openstack.TestAsync(cancelToken);
        }

        public Task CreateFolderAsync(CancellationToken cancelToken)
        {
            return m_openstack.CreateFolderAsync(cancelToken);
        }


        public string DisplayName
        {
            get
            {
                return Strings.HubiC.DisplayName;
            }
        }

        public string ProtocolKey
        {
            get
            {
                return "hubic";
            }
        }

        public System.Collections.Generic.IList<ICommandLineArgument> SupportedCommands
        {
            get
            {
                return new List<ICommandLineArgument>(new ICommandLineArgument[] {
                    new CommandLineArgument(AUTHID_OPTION, CommandLineArgument.ArgumentType.Password, Strings.HubiC.AuthidShort, Strings.HubiC.AuthidLong(OAuthHelper.OAUTH_LOGIN_URL("hubic"))),
                });
            }
        }

        public string Description
        {
            get
            {
                return Strings.HubiC.Description;
            }
        }

        public string[] DNSName
        {
            get { return new string[] { new System.Uri(HUBIC_API_URL).Host, m_openstack.EndPointDnsName }; }
        }

        public bool SupportsStreaming => m_openstack.SupportsStreaming;

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (m_openstack != null)
            {
                m_openstack.Dispose();
                m_openstack = null;
            }
        }

        #endregion
    }
}
