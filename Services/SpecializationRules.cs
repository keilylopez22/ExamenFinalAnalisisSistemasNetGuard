namespace NetGuardGT.Api.Services;

// Maps IncidentType to required Specialization
public static class SpecializationRules
{
    private static readonly Dictionary<Models.IncidentType, Models.Specialization> _map = new()
    {
        { Models.IncidentType.FiberOptic,  Models.Specialization.FiberOptic  },
        { Models.IncidentType.Microwave,   Models.Specialization.Microwave   },
        { Models.IncidentType.Electrical,  Models.Specialization.Electrical  },
        { Models.IncidentType.Network,     Models.Specialization.Network     },
        { Models.IncidentType.Other,       Models.Specialization.General     },
    };

    public static Models.Specialization Required(Models.IncidentType type) => _map[type];
}
