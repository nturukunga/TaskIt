// dashboard.js - JavaScript for the dashboard functionality

// Function to initialize task status chart
function initTaskItemStatusChart(chartId, labels, data, backgroundColors) {
    const ctx = document.getElementById(chartId).getContext('2d');
    
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: backgroundColors,
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 20,
                        boxWidth: 15
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.formattedValue;
                            const total = context.dataset.data.reduce((acc, val) => acc + val, 0);
                            const percentage = Math.round((context.raw / total) * 100);
                            return `${label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

// Function to initialize task progress chart
function initTaskProgressChart(chartId, data) {
    const ctx = document.getElementById(chartId).getContext('2d');
    
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: [{
                label: 'Task Count',
                data: data.values,
                backgroundColor: [
                    'rgba(40, 167, 69, 0.7)',
                    'rgba(13, 110, 253, 0.7)',
                    'rgba(108, 117, 125, 0.7)',
                    'rgba(220, 53, 69, 0.7)'
                ],
                borderColor: [
                    'rgb(40, 167, 69)',
                    'rgb(13, 110, 253)',
                    'rgb(108, 117, 125)',
                    'rgb(220, 53, 69)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

// Function to load recent tasks via AJAX
function loadRecentTasks() {
    fetch('/Tasks/GetRecentTasks')
        .then(response => response.json())
        .then(data => {
            const container = document.getElementById('recent-tasks-container');
            if (container && data && data.length) {
                let html = '';
                data.forEach(task => {
                    // Define status and priority classes
                    const statusClass = getStatusClass(task.status);
                    const priorityClass = getPriorityClass(task.priority);
                    
                    html += `
                    <tr>
                        <td>${escapeHtml(task.title)}</td>
                        <td><span class="badge ${statusClass}">${task.status}</span></td>
                        <td><span class="badge ${priorityClass}">${task.priority}</span></td>
                        <td>${task.dueDate ? new Date(task.dueDate).toLocaleDateString() : '-'}</td>
                        <td>
                            <a href="/Tasks/Details/${task.id}" class="btn btn-sm btn-outline-primary">
                                <i class="bi bi-eye"></i>
                            </a>
                            <a href="/Tasks/Edit/${task.id}" class="btn btn-sm btn-outline-primary">
                                <i class="bi bi-pencil"></i>
                            </a>
                        </td>
                    </tr>`;
                });
                container.innerHTML = html;
            } else if (container) {
                container.innerHTML = '<tr><td colspan="5" class="text-center py-3">No tasks found</td></tr>';
            }
        })
        .catch(error => console.error('Error loading recent tasks:', error));
}

// Helper function to get the status badge class
function getStatusClass(status) {
    switch (status) {
        case 'ToDo': return 'bg-secondary';
        case 'InProgress': return 'bg-primary';
        case 'OnHold': return 'bg-warning';
        case 'Completed': return 'bg-success';
        case 'Cancelled': return 'bg-danger';
        default: return 'bg-secondary';
    }
}

// Helper function to get the priority badge class
function getPriorityClass(priority) {
    switch (priority) {
        case 'Low': return 'bg-success';
        case 'Medium': return 'bg-info';
        case 'High': return 'bg-warning';
        case 'Critical': return 'bg-danger';
        default: return 'bg-secondary';
    }
}

// Helper function to escape HTML
function escapeHtml(unsafe) {
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

// Load tasks when page is ready
document.addEventListener('DOMContentLoaded', function() {
    // Charts are initialized with data from the server via inline script in the dashboard view
    
    // Setup periodic refresh for dashboard data (every 60 seconds)
    // This is commented out to avoid excessive API calls during development
    // Uncomment for production use
    
    // setInterval(function() {
    //     loadRecentTasks();
    //     updateTaskStatistics();
    // }, 60000);
});
