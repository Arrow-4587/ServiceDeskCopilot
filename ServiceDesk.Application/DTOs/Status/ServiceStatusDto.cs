namespace ServiceDesk.Application.DTOs.Status;

public record ServiceStatusDto(
    string ServiceName,
    string Status, // e.g. "Operational", "DegradedPerformance", "MajorOutage"
    string Description,
    DateTime LastChecked
);
