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
        alertArea.innerHTML = '<div class="alert ' + (action === 'approve' ? 'alert--success' : 'alert--danger') + '" role="alert"><span class="alert__icon">' + (action === 'approve' ? '✓' : '✕') + '</span><div class="alert__body"><strong class="alert__title">Mock action complete</strong><span class="alert__message">' + employee + '\'s request was ' + verb + ' locally.' + noteText + '</span></div><button class="alert__close" type="button" data-alert-dismiss aria-label="Dismiss">✕</button></div>';
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
