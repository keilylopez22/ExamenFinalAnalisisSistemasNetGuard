namespace NetGuardGT.Api.Models;

public enum Severity { Low, Medium, High, Critical, Urgent }

public enum IncidentStatus { Registered, Assigned, InProgress, Resolved, Closed }

public enum IncidentType { FiberOptic, Microwave, Electrical, Network, Other }

public enum Specialization { FiberOptic, Microwave, Electrical, Network, General }
