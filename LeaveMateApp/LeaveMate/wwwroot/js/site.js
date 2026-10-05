function badgeClassFor(status) {
    switch (status) {
        case "Approved": return "badge badge--approved";
        case "Rejected": return "badge badge--rejected";
        case "PendingSupervisorApproval":
        case "PendingHrApproval": return "badge badge--pending";
        default: return "badge badge--other";
    }
}

const sidebar = document.getElementById("primary-nav");
const scrim = document.getElementById("scrim");
const navToggle = document.getElementById("sidebarToggle");

function closeSidebar() {
    sidebar?.classList.remove("is-open");
    if (scrim) scrim.hidden = true;
    navToggle?.setAttribute("aria-expanded", "false");
    navToggle?.setAttribute("aria-label", "Open navigation");
}

function toggleSidebar() {
    const isOpen = sidebar?.classList.toggle("is-open") ?? false;
    if (scrim) scrim.hidden = !isOpen;
    navToggle?.setAttribute("aria-expanded", String(isOpen));
    navToggle?.setAttribute("aria-label", isOpen ? "Close navigation" : "Open navigation");
}

navToggle?.addEventListener("click", toggleSidebar);
scrim?.addEventListener("click", closeSidebar);
document.addEventListener("keydown", event => {
    if (event.key === "Escape") closeSidebar();
});

document.querySelector("[data-refresh]")?.addEventListener("click", () => window.location.reload());

const preferenceClasses = { compact: "is-compact", contrast: "is-high-contrast" };
const savedMessage = document.getElementById("settingsSaved");

document.querySelectorAll("[data-preference]").forEach(input => {
    const preference = input.dataset.preference;
    const className = preferenceClasses[preference];
    const storageKey = `leavemate-preference-${preference}`;
    input.checked = localStorage.getItem(storageKey) === "true";
    if (className) document.body.classList.toggle(className, input.checked);

    input.addEventListener("change", () => {
        localStorage.setItem(storageKey, String(input.checked));
        if (className) document.body.classList.toggle(className, input.checked);
        if (savedMessage) savedMessage.textContent = "Preference saved in this browser.";
    });
});
