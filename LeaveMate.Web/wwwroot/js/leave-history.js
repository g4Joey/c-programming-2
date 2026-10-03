(function () {
    'use strict';

    const search = document.getElementById('historySearch');
    const type = document.getElementById('historyType');
    const status = document.getElementById('historyStatus');
    const tableBody = document.querySelector('#historyTable tbody');
    const empty = document.getElementById('historyEmpty');
    let sortKey = 'start';
    let sortAscending = false;

    function applyFilters() {
        const searchText = search.value.trim().toLowerCase();
        const rows = Array.from(tableBody.querySelectorAll('tr'));
        const visibleRows = rows.filter(function (row) {
            const matchesSearch = !searchText || row.textContent.toLowerCase().includes(searchText);
            const matchesType = !type.value || row.dataset.type === type.value;
            const matchesStatus = !status.value || row.dataset.status === status.value;
            row.hidden = !(matchesSearch && matchesType && matchesStatus);
            return !row.hidden;
        });
        empty.hidden = visibleRows.length > 0;
    }

    document.querySelectorAll('[data-sort]').forEach(function (button) {
        button.addEventListener('click', function () {
            const nextKey = button.dataset.sort;
            sortAscending = sortKey === nextKey ? !sortAscending : true;
            sortKey = nextKey;
            const rows = Array.from(tableBody.querySelectorAll('tr'));
            rows.sort(function (left, right) {
                let leftValue = left.dataset[sortKey];
                let rightValue = right.dataset[sortKey];
                if (sortKey === 'days') {
                    leftValue = Number(leftValue);
                    rightValue = Number(rightValue);
                }
                return (leftValue > rightValue ? 1 : leftValue < rightValue ? -1 : 0) * (sortAscending ? 1 : -1);
            });
            rows.forEach(function (row) { tableBody.appendChild(row); });
            applyFilters();
        });
    });

    [search, type, status].forEach(function (control) { control.addEventListener('input', applyFilters); });
    applyFilters();
})();
