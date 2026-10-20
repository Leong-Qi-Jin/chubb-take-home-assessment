using Claims.Api.Features.Claims.CancelInformationRequest;
using Claims.Api.Features.Claims.GetDetails;
using Claims.Api.Features.Claims.GetHistory;
using Claims.Api.Features.Claims.List;
using Claims.Api.Features.Claims.ProvideInformation;
using Claims.Api.Features.Claims.Reject;
using Claims.Api.Features.Claims.RequestInformation;
using Claims.Api.Features.Claims.Settle;
using Claims.Api.Features.Claims.Submit;
using Claims.Api.Features.Claims.Take;
using Claims.Api.Features.Claims.UpdateEstimatedLiability;

namespace Claims.Api.Features.Claims;

public static class ClaimsEndpoints
{
    public static IEndpointRouteBuilder MapClaimsEndpoints(this IEndpointRouteBuilder api)
    {
        var claims = api.MapGroup("/claims").WithTags("Claims");

        SubmitClaimEndpoint.Map(claims);
        ListClaimsEndpoint.Map(claims);
        GetClaimDetailsEndpoint.Map(claims);
        GetClaimHistoryEndpoint.Map(claims);
        TakeClaimEndpoint.Map(claims);
        UpdateEstimatedLiabilityEndpoint.Map(claims);
        RequestInformationEndpoint.Map(claims);
        ProvideInformationEndpoint.Map(claims);
        CancelInformationRequestEndpoint.Map(claims);
        SettleClaimEndpoint.Map(claims);
        RejectClaimEndpoint.Map(claims);

        return api;
    }
}
