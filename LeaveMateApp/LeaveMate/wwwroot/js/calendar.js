async function loadCoverage() {
    const grid = document.getElementById("coverageGrid");
    const lastRefreshed = document.getElementById("lastRefreshed");

    try {
        const response = await fetch("/api/coverage");
        if (!response.ok) throw new Error("Coverage endpoint returned " + response.status);
        const data = await response.json();

        lastRefreshed.textContent = data.lastRefreshedUtc
            ? "Last refreshed: " + new Date(data.lastRefreshedUtc).toLocaleTimeString()
            : "";

        grid.innerHTML = "";
        data.days.slice(0, 28).forEach(day => {
            const cell = document.createElement("div");
            cell.className = "coverage-cell" + (day.employeesOnLeave > 0 ? " coverage-cell--busy" : "");
            const date = new Date(day.date);
            const names = day.employeeNames.join(", ");
            cell.title = names;
            cell.innerHTML = `<strong>${date.toLocaleDateString(undefined, { month: "short", day: "numeric" })}</strong><br/>${day.employeesOnLeave} on leave`;
            grid.appendChild(cell);
        });
    } catch (err) {
        grid.textContent = "Could not load coverage data: " + err.message;
    }
}

loadCoverage();
setInterval(loadCoverage, 30000);
