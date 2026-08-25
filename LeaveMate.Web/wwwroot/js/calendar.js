(function () {
    'use strict';

    const events = JSON.parse(document.getElementById('calendarEvents').textContent);
    const grid = document.getElementById('calendarGrid');
    const heading = document.getElementById('calendarHeading');
    const monthFormatter = new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' });
    const dateFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    const now = new Date();
    let visibleMonth = new Date(now.getFullYear(), now.getMonth(), 1);

    function dateKey(date) { return date.toISOString().slice(0, 10); }
    function parseDate(value) { const parts = value.slice(0, 10).split('-'); return new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2])); }
    function statusClass(status) { return status === 'Approved' ? 'badge--approved' : status === 'Rejected' ? 'badge--rejected' : 'badge--pending'; }

    function showDetails(event) {
        const modal = document.getElementById('calendarEventModal');
        const body = modal.querySelector('.modal__body');

        const list = document.createElement('dl');
        list.className = 'detail-list';

        const detailRows = [
            ['Employee', event.employeeName],
            ['Leave type', event.leaveType],
            ['Start date', dateFormatter.format(parseDate(event.startDate))],
            ['End date', dateFormatter.format(parseDate(event.endDate))],
            ['Duration', event.durationInDays + ' day' + (event.durationInDays === 1 ? '' : 's')]
        ];

        detailRows.forEach(function (entry) {
            const key = document.createElement('dt');
            key.textContent = entry[0];
            const value = document.createElement('dd');
            value.textContent = entry[1];
            list.appendChild(key);
            list.appendChild(value);
        });

        const statusKey = document.createElement('dt');
        statusKey.textContent = 'Status';
        const statusValue = document.createElement('dd');
        const statusBadge = document.createElement('span');
        statusBadge.className = 'badge ' + statusClass(event.status);

        const statusDot = document.createElement('span');
        statusDot.className = 'badge__dot';

        const statusText = document.createElement('span');
        statusText.textContent = event.status;

        statusBadge.appendChild(statusDot);
        statusBadge.appendChild(statusText);
        statusValue.appendChild(statusBadge);
        list.appendChild(statusKey);
        list.appendChild(statusValue);

        body.textContent = '';
        body.appendChild(list);
        modal.hidden = false;
        document.body.classList.add('has-modal');
        modal.querySelector('[data-modal-close]').focus();
    }

    function render() {
        const firstDay = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth(), 1);
        const lastDay = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + 1, 0);
        const start = new Date(firstDay);
        start.setDate(start.getDate() - firstDay.getDay());
        const totalCells = Math.ceil((firstDay.getDay() + lastDay.getDate()) / 7) * 7;
        heading.textContent = monthFormatter.format(visibleMonth);
        grid.innerHTML = '';

        for (let index = 0; index < totalCells; index += 1) {
            const date = new Date(start);
            date.setDate(start.getDate() + index);
            const cell = document.createElement('div');
            cell.className = 'calendar-day' + (date.getMonth() !== visibleMonth.getMonth() ? ' is-outside' : '');
            cell.innerHTML = '<span class="calendar-day__number">' + date.getDate() + '</span>';
            events.filter(function (event) { return dateKey(parseDate(event.startDate)) <= dateKey(date) && dateKey(parseDate(event.endDate)) >= dateKey(date); }).forEach(function (event) {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'calendar-event ' + statusClass(event.status);
                button.textContent = event.employeeName + ' - ' + event.leaveType;
                button.addEventListener('click', function () { showDetails(event); });
                cell.appendChild(button);
            });
            grid.appendChild(cell);
        }
    }

    document.getElementById('previousMonth').addEventListener('click', function () { visibleMonth.setMonth(visibleMonth.getMonth() - 1); render(); });
    document.getElementById('nextMonth').addEventListener('click', function () { visibleMonth.setMonth(visibleMonth.getMonth() + 1); render(); });
    document.getElementById('todayMonth').addEventListener('click', function () { const now = new Date(); visibleMonth = new Date(now.getFullYear(), now.getMonth(), 1); render(); });
    render();
})();
