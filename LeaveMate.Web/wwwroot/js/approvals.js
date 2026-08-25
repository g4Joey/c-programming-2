(function () {
    'use strict';

    const modal = document.getElementById('approvalModal');
    const message = document.getElementById('approvalModalMessage');
    const comment = document.getElementById('approvalComment');
    const confirm = document.getElementById('approvalConfirm');
    const alertArea = document.getElementById('approvalAlert');
    let selectedRow;
    let selectedAction;

    function closeModal() {
        modal.hidden = true;
        document.body.classList.remove('has-modal');
    }
    function openModal(row, action) {
        if (!row) return;
        selectedRow = row;
        selectedAction = action;
        const employee = row.querySelector('td').textContent.trim();
        const verb = action === 'approve' ? 'approve' : 'reject';
        message.textContent = 'Are you sure you want to ' + verb + ' ' + employee + '\'s request?';
        confirm.textContent = action === 'approve' ? 'Approve request' : 'Reject request';
        confirm.className = 'btn ' + (action === 'approve' ? 'btn--success' : 'btn--danger');
        comment.value = '';
        modal.hidden = false;
        document.body.classList.add('has-modal');
        comment.focus();
    }
    function showAlert(action, employee, note) {
        const verb = action === 'approve' ? 'approved' : 'rejected';
        const noteText = note ? ' Comment recorded locally: ' + note : '';

        const alert = document.createElement('div');
        alert.className = 'alert ' + (action === 'approve' ? 'alert--success' : 'alert--danger');
        alert.setAttribute('role', 'alert');

        const icon = document.createElement('span');
        icon.className = 'alert__icon';
        icon.textContent = action === 'approve' ? '✓' : '✕';

        const body = document.createElement('div');
        body.className = 'alert__body';

        const title = document.createElement('strong');
        title.className = 'alert__title';
        title.textContent = 'Mock action complete';

        const messageText = document.createElement('span');
        messageText.className = 'alert__message';
        messageText.textContent = employee + '\'s request was ' + verb + ' locally.' + noteText;

        const closeButton = document.createElement('button');
        closeButton.className = 'alert__close';
        closeButton.type = 'button';
        closeButton.setAttribute('data-alert-dismiss', '');
        closeButton.setAttribute('aria-label', 'Dismiss');
        closeButton.textContent = '✕';

        body.appendChild(title);
        body.appendChild(messageText);
        alert.appendChild(icon);
        alert.appendChild(body);
        alert.appendChild(closeButton);

        alertArea.textContent = '';
        alertArea.appendChild(alert);
    }

    document.querySelectorAll('[data-approval-action]').forEach(function (button) {
        button.addEventListener('click', function () { openModal(button.closest('tr'), button.dataset.approvalAction); });
    });
    document.querySelectorAll('[data-approval-close]').forEach(function (button) { button.addEventListener('click', closeModal); });
    confirm.addEventListener('click', function () {
        const employee = selectedRow.querySelector('td').textContent.trim();
        const note = comment.value.trim();
        selectedRow.remove();
        closeModal();
        showAlert(selectedAction, employee, note);
        if (!document.querySelector('#approvalTable tbody tr')) document.getElementById('approvalEmpty').hidden = false;
    });
    document.addEventListener('keydown', function (event) { if (event.key === 'Escape' && !modal.hidden) closeModal(); });
})();
