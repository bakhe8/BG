"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveNotification", (notification) => {
    console.log("New notification received:", notification);
    addNotificationToUi(notification, true);
    showBrowserNotification(notification.message);
});

async function startSignalR() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        await fetchInitialNotifications();
    } catch (err) {
        console.log("SignalR Connection Error: ", err);
        setTimeout(startSignalR, 5000);
    }
}

async function fetchInitialNotifications() {
    try {
        const response = await fetch('/api/notifications');
        if (response.ok) {
            const notifications = await response.json();
            const list = document.getElementById("notification-list");
            if (list) list.innerHTML = ''; // Clear empty state

            if (notifications.length === 0) {
                showEmptyState();
            } else {
                notifications.forEach(n => addNotificationToUi(n, false));
                updateBadgeCount(notifications.length);
            }
        }
    } catch (err) {
        console.error("Error fetching notifications:", err);
    }
}

function addNotificationToUi(notification, isNew) {
    const list = document.getElementById("notification-list");
    if (!list) return;

    if (isNew) {
        const emptyMsg = list.querySelector(".text-center.text-muted.p-4");
        if (emptyMsg) emptyMsg.remove();
        updateBadgeCount(1, true);
    }

    const item = document.createElement("a");
    item.href = notification.link || "#";
    item.className = `dropdown-item p-3 border-bottom d-flex align-items-start gap-3 ${isNew ? 'bg-light' : ''}`;
    item.onclick = (e) => markAsRead(notification.id, e);

    const time = new Date(notification.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    item.innerHTML = `
        <div class="rounded-circle bg-brand-green p-2 text-white flex-shrink-0" style="width: 32px; height: 32px; display: flex; align-items: center; justify-content: center;">
            <i class="bi bi-bell-fill x-small"></i>
        </div>
        <div class="flex-grow-1">
            <div class="small fw-bold text-wrap">${notification.message}</div>
            <div class="x-small text-muted">${time}</div>
        </div>
    `;

    if (isNew) {
        list.prepend(item);
    } else {
        list.appendChild(item);
    }
}

async function markAsRead(id, event) {
    if (!id) return;
    try {
        await fetch(`/api/notifications/${id}/read`, { method: 'POST' });
    } catch (err) {
        console.error("Error marking as read:", err);
    }
}

function updateBadgeCount(amount, isIncrement = false) {
    const badge = document.getElementById("notification-badge");
    if (!badge) return;

    let currentCount = isIncrement ? (parseInt(badge.innerText) || 0) : 0;
    let newCount = currentCount + amount;

    if (newCount > 0) {
        badge.innerText = newCount;
        badge.classList.remove("d-none");
    } else {
        badge.classList.add("d-none");
    }
}

function showEmptyState() {
    const list = document.getElementById("notification-list");
    if (!list) return;
    const emptyMessage = list.dataset.emptyMessage || "No new notifications";
    list.innerHTML = `
        <div class="text-center text-muted p-4">
            <i class="bi bi-bell-slash fs-1 opacity-25 d-block mb-2"></i>
            <span class="small">${emptyMessage}</span>
        </div>
    `;
}

function showBrowserNotification(message) {
    if (!("Notification" in window)) return;
    const appName = document.getElementById("notification-list")?.dataset.appName ?? "BG";
    if (Notification.permission === "granted") {
        new Notification(appName, { body: message });
    } else if (Notification.permission !== "denied") {
        Notification.requestPermission().then(permission => {
            if (permission === "granted") {
                new Notification(appName, { body: message });
            }
        });
    }
}

document.addEventListener("DOMContentLoaded", startSignalR);
