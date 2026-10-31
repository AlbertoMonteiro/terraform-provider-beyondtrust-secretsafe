using BeyondTrust.SecretSafeProvider.Proto;
using BeyondTrust.SecretSafeProvider.Serialization;

namespace BeyondTrust.SecretSafeProvider.Services.Resources;

public static class ResourcePlanner
{
    private const string ID = "id";

    public static PlanResourceChange.Types.Response Plan(
        PlanResourceChange.Types.Request request,
        Schema schema,
        IReadOnlySet<string> replaceAttributes)
    {
        var proposed = TerraformObject.Read(request.ProposedNewState);

        // Terraform proposes a null object when destroying
        if (proposed is null)
            return new PlanResourceChange.Types.Response { PlannedState = request.ProposedNewState };

        var prior = TerraformObject.Read(request.PriorState);

        if (prior is not null && proposed.Keys.All(name => TerraformObject.HaveSameValue(prior, proposed, name)))
            return new PlanResourceChange.Types.Response { PlannedState = request.ProposedNewState };

        var response = new PlanResourceChange.Types.Response();

        if (prior is not null)
            foreach (var name in replaceAttributes.Where(name => !TerraformObject.HaveSameValue(prior, proposed, name)))
                response.RequiresReplace.Add(new AttributePath { Steps = { new AttributePath.Types.Step { AttributeName = name } } });

        var config = TerraformObject.Read(request.Config);

        foreach (var attribute in schema.Block.Attributes.Where(attribute => attribute.Computed))
        {
            if (!TerraformObject.IsNull(config, attribute.Name))
                continue;

            // On update the id never changes and an owner chosen earlier stays
            var keepsPriorValue = prior is not null && (attribute.Name == ID || attribute.Optional);

            if (!keepsPriorValue)
                TerraformObject.MarkUnknown(proposed, attribute.Name);
        }

        response.PlannedState = TerraformObject.Write(proposed);

        return response;
    }
}
