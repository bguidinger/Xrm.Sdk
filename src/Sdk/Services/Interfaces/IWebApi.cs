namespace Microsoft.Pfe.Xrm.Sdk.Services
{
    using Microsoft.Xrm.Sdk;
    using System.Threading.Tasks;

    public interface IWebApi
    {
        Task<OrganizationResponse> Send(OrganizationRequest request);
    }
}