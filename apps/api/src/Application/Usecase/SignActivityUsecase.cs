using Api.Application.Auth;
using Api.Application.DTO;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;

namespace Api.Application.Usecase;

public sealed class SignActivityUsecase : IUsecase<SignActivityRequest, Activity>
{
    private readonly IActivityRepository _repository;
    private readonly IAffiliationRepository _affiliations;
    private readonly IAuthenticator _authenticator;

    public SignActivityUsecase(IActivityRepository repository, IAffiliationRepository affiliations, IAuthenticator authenticator)
    {
        _repository = repository;
        _affiliations = affiliations;
        _authenticator = authenticator;
    }

    public async Task<Activity> ExecuteAsync(SignActivityRequest request, CancellationToken cancellationToken)
    {
        var auth = _authenticator.Authenticate(request.AuthToken);
        if (auth is null)
        {
            throw new UnauthorizedAccessException("auth_required");
        }

        if (string.IsNullOrWhiteSpace(request.ActivityId))
        {
            throw new ArgumentException("activity_id is required");
        }
        if (string.IsNullOrWhiteSpace(request.AffiliationId))
        {
            throw new ArgumentException("affiliation_id is required");
        }

        var activity = await _repository.GetActivityAsync(request.ActivityId, cancellationToken);
        if (activity == null)
        {
            throw new KeyNotFoundException("activity_not_found");
        }

        if (!activity.RequiredSignatures.Contains(request.AffiliationId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("signer_not_in_cocreators");
        }
        var alreadySigned = activity.Signatures.Contains(request.AffiliationId, StringComparer.Ordinal);

        var affiliation = await _affiliations.GetAffiliationAsync(request.AffiliationId, cancellationToken);
        if (affiliation == null)
        {
            throw new KeyNotFoundException("affiliation_not_found");
        }
        if (!string.Equals(affiliation.OwnerId, auth.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("ownership_mismatch");
        }

        if (alreadySigned)
        {
            return new Activity
            {
                Id = activity.Id,
                AffiliationId = activity.AffiliationId,
                WorldId = activity.WorldId,
                OwnerId = activity.OwnerId,
                Content = activity.Content,
                Status = activity.Status,
                CreatedAt = activity.CreatedAt,
                ExpiresAt = activity.ExpiresAt,
                CoCreatorIds = activity.CoCreators,
                SignatureIds = activity.Signatures
            };
        }

        var newSignatures = new List<string>(activity.Signatures) { request.AffiliationId };
        var allSigned = activity.RequiredSignatures.All(req => newSignatures.Contains(req, StringComparer.Ordinal));
        var newStatus = allSigned ? ActivityStatus.Published : activity.Status;

        var updated = await _repository.UpdateActivitySignaturesAsync(activity, newSignatures, newStatus, cancellationToken);
        if (allSigned)
        {
            await _repository.PublishActivityAsync(updated, cancellationToken);
        }

        return new Activity
        {
            Id = updated.Id,
            AffiliationId = updated.AffiliationId,
            WorldId = updated.WorldId,
            OwnerId = updated.OwnerId,
            Content = updated.Content,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            ExpiresAt = updated.ExpiresAt,
            CoCreatorIds = updated.CoCreators,
            SignatureIds = updated.Signatures
        };
    }
}
