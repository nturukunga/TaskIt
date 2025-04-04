// site.js - Main JavaScript for TaskIt application

// Document ready function
document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Check for notification badge update
    updateNotificationBadge();
    
    // Set up periodic notification check (every 30 seconds)
    setInterval(updateNotificationBadge, 30000);
});

// Function to update notification badge
function updateNotificationBadge() {
    fetch('/Notifications/GetUnreadCount')
        .then(response => response.json())
        .then(data => {
            const badge = document.getElementById('notification-badge');
            if (badge) {
                if (data.count > 0) {
                    badge.textContent = data.count;
                    badge.classList.remove('d-none');
                } else {
                    badge.classList.add('d-none');
                }
            }
        })
        .catch(error => console.error('Error updating notification badge:', error));
}

// Function to handle task status change
function handleTaskItemStatusChange(taskId, newStatus) {
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = `/Tasks/UpdateStatus/${taskId}`;
    
    const statusInput = document.createElement('input');
    statusInput.type = 'hidden';
    statusInput.name = 'status';
    statusInput.value = newStatus;
    
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]');
    if (antiForgeryToken) {
        const tokenInput = antiForgeryToken.cloneNode(true);
        form.appendChild(tokenInput);
    }
    
    form.appendChild(statusInput);
    document.body.appendChild(form);
    form.submit();
}

// Function to confirm delete
function confirmDelete(message, formId) {
    if (confirm(message || 'Are you sure you want to delete this item?')) {
        document.getElementById(formId).submit();
    }
    return false;
}

// Function to preview task description
function previewDescription() {
    const description = document.getElementById('Description').value;
    const previewElement = document.getElementById('description-preview');
    
    if (previewElement) {
        previewElement.innerHTML = description.replace(/\n/g, '<br>');
        document.getElementById('preview-container').classList.remove('d-none');
    }
}
