using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Service
{
    // Dodaje CORS headere na svaki WCF response
    // Omogucava React dev serveru (localhost:5173) da poziva WCF
    public class CorsMessageInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel,
            InstanceContext instanceContext)
        {
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if (reply == null) return;

            HttpResponseMessageProperty httpResponse;
            if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                httpResponse = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];
            }
            else
            {
                httpResponse = new HttpResponseMessageProperty();
                reply.Properties.Add(HttpResponseMessageProperty.Name, httpResponse);
            }

            httpResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            httpResponse.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            httpResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
        }
    }

    public class CorsBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }
        public void Validate(ServiceEndpoint endpoint) { }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CorsMessageInspector());
        }
    }
}