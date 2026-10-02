function badgeClassFor(status) {
    switch (status) {
        case "Approved": return "badge badge--approved";
        case "Rejected": return "badge badge--rejected";
        case "PendingSupervisorApproval":
        case "PendingHrApproval": return "badge badge--pending";
        default: return "badge badge--other";
    }
}

const appShell = document.querySelector(".app-shell");
const navToggle = document.querySelector(".nav-toggle");

if (appShell && navToggle) {
    const collapsedStorageKey = "leavemate-sidebar-collapsed";
    const setNavCollapsed = (collapsed) => {
        appShell.classList.toggle("app-shell--nav-collapsed", collapsed);
        navToggle.setAttribute("aria-expanded", String(!collapsed));
        navToggle.setAttribute("aria-label", collapsed ? "Open sidebar" : "Collapse sidebar");
        navToggle.setAttribute("title", collapsed ? "Open sidebar" : "Collapse sidebar");
        navToggle.textContent = collapsed ? "Open sidebar" : "Collapse sidebar";
    };

    setNavCollapsed(localStorage.getItem(collapsedStorageKey) === "true");
    navToggle.addEventListener("click", () => {
        const collapsed = !appShell.classList.contains("app-shell--nav-collapsed");
        localStorage.setItem(collapsedStorageKey, String(collapsed));
        setNavCollapsed(collapsed);
    });
}
