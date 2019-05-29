namespace Microsoft.Pfe.Xrm.Sdk
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using System.Configuration;
    using System.Net;

    [TestClass]
    public class RetrieveTests
    {
        private readonly IOrganizationService _service;

        public RetrieveTests()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var organizationUrl = ConfigurationManager.AppSettings["OrganizationUrl"];
            var username = ConfigurationManager.AppSettings["Username"];
            var password = ConfigurationManager.AppSettings["Password"];

            var credential = new NetworkCredential(username, password);

            _service = new WebApiService(organizationUrl, credential.UserName, credential.SecurePassword);
        }

        [TestMethod]
        public void RetrieveReturnsEntity()
        {
            var query = new QueryExpression("account");
            query.ColumnSet = new ColumnSet("name");
            var accounts = _service.RetrieveMultiple(query);

            Assert.IsNotNull(accounts);
        }
    }
}
