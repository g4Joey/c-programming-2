function badgeClassFor(status) {
    switch (status) {
        case "Approved": return "badge badge--approved";
        case "Rejected": return "badge badge--rejected";
        case "PendingSupervisorApproval":
        case "PendingHrApproval": return "badge badge--pending";
        default: return "badge badge--other";
    }
}
