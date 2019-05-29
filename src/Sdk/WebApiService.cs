namespace Microsoft.Pfe.Xrm.Sdk
{
    using Microsoft.Pfe.Xrm.Sdk.Services;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Security;

    public class WebApiService : IOrganizationService
    {
        private readonly IWebApi _api;

        public WebApiService(string organizationUrl, string username, SecureString password)
        {
            _api = new WebApi(organizationUrl, username, password);
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            var request = new RetrieveRequest()
            {
                Target = new EntityReference(entityName, id),
                ColumnSet = columnSet
            };
            var response = Execute(request) as RetrieveResponse;
            return response.Entity;
        }
        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            var request = new RetrieveMultipleRequest()
            {
                Query = query
            };
            var response = Execute(request) as RetrieveMultipleResponse;
            return response.EntityCollection;
        }
        public Guid Create(Entity entity)
        {
            var request = new CreateRequest()
            {
                Target = entity
            };
            var response = Execute(request) as CreateResponse;
            return response.id;
        }
        public void Update(Entity entity)
        {
            var request = new UpdateRequest()
            {
                Target = entity
            };
            var response = Execute(request) as UpdateResponse;
        }
        public void Delete(string entityName, Guid id)
        {
            var request = new DeleteRequest()
            {
                Target = new EntityReference(entityName, id)
            };
            var response = Execute(request) as DeleteResponse;
        }
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            var request = new AssociateRequest()
            {
                Target = new EntityReference(entityName, entityId),
                Relationship = relationship,
                RelatedEntities = relatedEntities
            };
            var response = Execute(request) as AssociateResponse;
        }
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            var request = new DisassociateRequest()
            {
                Target = new EntityReference(entityName, entityId),
                Relationship = relationship,
                RelatedEntities = relatedEntities
            };
            var response = Execute(request) as DisassociateResponse;
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            return _api.Send(request).Result;
        }
    }
}