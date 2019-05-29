namespace Microsoft.Pfe.Xrm.Sdk.Models
{
    using System.Collections.Generic;

    public class Response<TModel>
    {
        public IEnumerable<TModel> Value { get; set; }
    }
}