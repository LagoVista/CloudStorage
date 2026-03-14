using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Relational.Tests.Core.Seeds;
using System;

namespace Relational.Tests.Core.Utils
{
    public class TestAppConfig : IAppConfig
    {
        public PlatformTypes PlatformType => throw new NotImplementedException();

        public Environments Environment => throw new NotImplementedException();

        public AuthTypes AuthType => throw new NotImplementedException();

        public EntityHeader SystemOwnerOrg => OrganizationSeeds.SystemOwner.ToEntityHeader();

        public string WebAddress => throw new NotImplementedException();

        public string CompanyName => throw new NotImplementedException();

        public string CompanySiteLink => throw new NotImplementedException();

        public string AppName => throw new NotImplementedException();

        public string AppId => throw new NotImplementedException();

        public string APIToken => throw new NotImplementedException();

        public string AppDescription => throw new NotImplementedException();

        public string TermsAndConditionsLink => throw new NotImplementedException();

        public string PrivacyStatementLink => throw new NotImplementedException();

        public string ClientType => throw new NotImplementedException();

        public string AppLogo => throw new NotImplementedException();

        public string CompanyLogo => throw new NotImplementedException();

        public string InstanceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string InstanceAuthKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DeviceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DeviceRepoId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string DefaultDeviceLabel => throw new NotImplementedException();

        public string DefaultDeviceLabelPlural => throw new NotImplementedException();

        public bool EmitTestingCode => throw new NotImplementedException();

        public VersionInfo Version => throw new NotImplementedException();

        public string AnalyticsKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
