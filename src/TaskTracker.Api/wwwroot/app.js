// TaskTracker Pro - Single Page Web Application

const state = {
  projects: [],
  selectedProjectId: null,
  tasks: [],
  taskFilter: {
    status: '',
    priority: '',
    search: ''
  },
  events: [],
  eventFilter: {
    search: '',
    dateFrom: '',
    dateTo: '',
    categoryId: '',
    sortBy: 'date',
    order: 'asc',
    page: 1,
    pageSize: 20
  },
  currentTask: null,
  currentEvent: null
};

// UI Utility: Toast
function showToast(message, type = 'success') {
  const container = document.getElementById('toast-container');
  const toast = document.createElement('div');
  toast.className = `toast toast-${type}`;
  toast.innerHTML = `<span>${type === 'success' ? '✓' : '⚠'}</span> <span>${message}</span>`;
  container.appendChild(toast);
  setTimeout(() => {
    toast.style.opacity = '0';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

// Navigation Tabs
function initTabs() {
  const tabs = document.querySelectorAll('.nav-tab');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');

      const targetSection = tab.getAttribute('data-tab');
      document.querySelectorAll('.section-content').forEach(s => s.classList.remove('active'));
      const activeSec = document.getElementById(`section-${targetSection}`);
      if (activeSec) activeSec.classList.add('active');

      if (targetSection === 'tasks') {
        loadTasks();
      } else if (targetSection === 'events') {
        loadEvents();
      } else if (targetSection === 'admin') {
        checkHealth();
      }
    });
  });
}

// Health Check Ping
async function checkHealth() {
  const badge = document.getElementById('health-indicator');
  const pingText = document.getElementById('admin-ping-status');
  try {
    const start = performance.now();
    const res = await fetch('/health');
    const elapsed = Math.round(performance.now() - start);
    if (res.ok) {
      badge.innerHTML = `<span class="health-dot"></span> API Онлайн (${elapsed}ms)`;
      badge.style.color = 'var(--success)';
      if (pingText) pingText.innerText = `Healthy (${elapsed}ms)`;
    } else {
      badge.innerHTML = `<span class="health-dot" style="background:var(--danger)"></span> API Degraded`;
      badge.style.color = 'var(--danger)';
      if (pingText) pingText.innerText = 'Degraded';
    }
  } catch (err) {
    badge.innerHTML = `<span class="health-dot" style="background:var(--danger)"></span> API Офлайн`;
    badge.style.color = 'var(--danger)';
    if (pingText) pingText.innerText = 'Offline';
  }
}

// ==========================================
// PROJECTS & TASKS
// ==========================================

async function loadProjects() {
  try {
    const res = await fetch('/api/projects');
    if (!res.ok) throw new Error('Не вдалося завантажити проєкти');
    state.projects = await res.json();

    const select = document.getElementById('project-select');
    const taskModalSelect = document.getElementById('task-project-select');

    select.innerHTML = '<option value="">Усі проєкти</option>';
    taskModalSelect.innerHTML = '';

    state.projects.forEach(p => {
      select.innerHTML += `<option value="${p.id}">${escapeHtml(p.name)}</option>`;
      taskModalSelect.innerHTML += `<option value="${p.id}">${escapeHtml(p.name)}</option>`;
    });

    if (state.selectedProjectId) {
      select.value = state.selectedProjectId;
    }
  } catch (err) {
    console.error(err);
  }
}

async function loadTasks() {
  try {
    const params = new URLSearchParams();
    if (state.selectedProjectId) params.append('projectId', state.selectedProjectId);
    if (state.taskFilter.status) params.append('status', state.taskFilter.status);
    if (state.taskFilter.priority) params.append('priority', state.taskFilter.priority);
    if (state.taskFilter.search) params.append('search', state.taskFilter.search);
    params.append('pageSize', '100');

    const res = await fetch(`/api/tasks?${params.toString()}`);
    if (!res.ok) throw new Error('Помилка завантаження задач');
    const data = await res.json();
    state.tasks = data.items || [];

    renderTasks();
    updateTaskStats();
  } catch (err) {
    console.error(err);
    showToast('Помилка завантаження задач', 'error');
  }
}

function updateTaskStats() {
  const total = state.tasks.length;
  const todo = state.tasks.filter(t => t.status === 'Todo').length;
  const inProgress = state.tasks.filter(t => t.status === 'InProgress').length;
  const done = state.tasks.filter(t => t.status === 'Done').length;

  document.getElementById('stat-total-tasks').innerText = total;
  document.getElementById('stat-todo-tasks').innerText = todo;
  document.getElementById('stat-progress-tasks').innerText = inProgress;
  document.getElementById('stat-done-tasks').innerText = done;
}

function renderTasks() {
  const columns = {
    Todo: document.getElementById('col-todo'),
    InProgress: document.getElementById('col-inprogress'),
    InReview: document.getElementById('col-inreview'),
    Done: document.getElementById('col-done')
  };

  const counts = { Todo: 0, InProgress: 0, InReview: 0, Done: 0 };
  Object.values(columns).forEach(col => col.innerHTML = '');

  state.tasks.forEach(task => {
    const col = columns[task.status] || columns.Todo;
    counts[task.status] = (counts[task.status] || 0) + 1;

    const card = document.createElement('div');
    card.className = 'task-card';

    let coverHtml = '';
    if (task.coverImagePath) {
      coverHtml = `<img src="${task.coverImagePath}" class="task-cover" alt="Cover" />`;
    }

    let locationHtml = '';
    if (task.location) {
      const coords = (task.latitude && task.longitude) ? ` [${task.latitude.toFixed(2)}, ${task.longitude.toFixed(2)}]` : '';
      locationHtml = `<span class="badge badge-location" title="Адреса${coords}">📍 ${escapeHtml(task.location)}</span>`;
    }

    let dueDateHtml = '';
    if (task.dueDate) {
      const d = new Date(task.dueDate);
      dueDateHtml = `<span class="badge badge-date">📅 ${d.toLocaleDateString()}</span>`;
    }

    card.innerHTML = `
      ${coverHtml}
      <div class="task-title">${escapeHtml(task.title)}</div>
      ${task.description ? `<div class="task-desc">${escapeHtml(task.description)}</div>` : ''}
      <div class="task-meta">
        <span class="badge badge-priority-${task.priority.toLowerCase()}">${task.priority}</span>
        <span class="badge badge-project">${escapeHtml(task.projectName || 'Проєкт')}</span>
        ${dueDateHtml}
        ${locationHtml}
      </div>
      <div class="task-actions">
        <span class="task-comments-count">💬 ${task.commentCount || 0} коментарів</span>
        <div class="quick-move-btns" onclick="event.stopPropagation()">
          ${task.status !== 'Todo' ? `<button class="quick-move-btn" title="Перемістити назад" onclick="changeTaskStatus('${task.id}', '${getPrevStatus(task.status)}')">←</button>` : ''}
          ${task.status !== 'Done' ? `<button class="quick-move-btn" title="Перемістити вперед" onclick="changeTaskStatus('${task.id}', '${getNextStatus(task.status)}')">→</button>` : ''}
        </div>
      </div>
    `;

    card.addEventListener('click', () => openTaskDetails(task.id));
    col.appendChild(card);
  });

  document.getElementById('count-todo').innerText = counts.Todo;
  document.getElementById('count-inprogress').innerText = counts.InProgress;
  document.getElementById('count-inreview').innerText = counts.InReview;
  document.getElementById('count-done').innerText = counts.Done;
}

function getNextStatus(current) {
  const flow = ['Todo', 'InProgress', 'InReview', 'Done'];
  const idx = flow.indexOf(current);
  return idx < flow.length - 1 ? flow[idx + 1] : current;
}

function getPrevStatus(current) {
  const flow = ['Todo', 'InProgress', 'InReview', 'Done'];
  const idx = flow.indexOf(current);
  return idx > 0 ? flow[idx - 1] : current;
}

async function changeTaskStatus(taskId, newStatus) {
  try {
    const res = await fetch(`/api/tasks/${taskId}`);
    if (!res.ok) throw new Error();
    const task = await res.json();

    const updateBody = {
      title: task.title,
      description: task.description,
      status: newStatus,
      priority: task.priority,
      dueDate: task.dueDate,
      location: task.location,
      assigneeId: task.assigneeId
    };

    const putRes = await fetch(`/api/tasks/${taskId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updateBody)
    });

    if (!putRes.ok) throw new Error('Не вдалося оновити статус');
    showToast(`Статус змінено на: ${newStatus}`);
    loadTasks();
  } catch (err) {
    showToast('Помилка оновлення статусу', 'error');
  }
}

// Task Details Modal
async function openTaskDetails(taskId) {
  try {
    const res = await fetch(`/api/tasks/${taskId}`);
    if (!res.ok) throw new Error('Помилка завантаження задачі');
    const task = await res.json();
    state.currentTask = task;

    document.getElementById('modal-task-title').innerText = task.title;
    document.getElementById('modal-task-desc').innerText = task.description || 'Немає опису';
    document.getElementById('modal-task-project').innerText = task.projectName || '—';
    document.getElementById('modal-task-priority').value = task.priority;
    document.getElementById('modal-task-status').value = task.status;
    document.getElementById('modal-task-location').innerText = task.location ? `${task.location} (${task.latitude || '—'}, ${task.longitude || '—'})` : 'Локація не задана';

    const coverPreview = document.getElementById('modal-task-cover-preview');
    if (task.coverImagePath) {
      coverPreview.src = task.coverImagePath;
      coverPreview.style.display = 'block';
      document.getElementById('modal-task-cover-delete').style.display = 'inline-flex';
    } else {
      coverPreview.style.display = 'none';
      document.getElementById('modal-task-cover-delete').style.display = 'none';
    }

    renderComments(task.comments || []);
    openModal('modal-task-details');
  } catch (err) {
    showToast('Помилка відкриття деталей', 'error');
  }
}

function renderComments(comments) {
  const container = document.getElementById('modal-task-comments-list');
  container.innerHTML = '';
  if (!comments.length) {
    container.innerHTML = '<div style="color:var(--text-muted); font-size:0.85rem">Коментарів поки немає.</div>';
    return;
  }

  comments.forEach(c => {
    const item = document.createElement('div');
    item.className = 'comment-item';
    const d = new Date(c.createdAt);
    item.innerHTML = `
      <div class="comment-author">
        <span>👤 ${escapeHtml(c.authorName || 'Користувач')}</span>
        <span class="comment-date">${d.toLocaleDateString()} ${d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
      </div>
      <div>${escapeHtml(c.text)}</div>
    `;
    container.appendChild(item);
  });
}

// Add comment
async function addComment() {
  if (!state.currentTask) return;
  const input = document.getElementById('modal-task-comment-input');
  const text = input.value.trim();
  if (!text) return;

  try {
    const res = await fetch(`/api/tasks/${state.currentTask.id}/comments`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        text: text,
        authorId: 1
      })
    });

    if (!res.ok) throw new Error('Помилка додавання коментаря');
    input.value = '';
    showToast('Коментар додано!');
    await openTaskDetails(state.currentTask.id);
    loadTasks();
  } catch (err) {
    showToast('Не вдалося додати коментар', 'error');
  }
}

// Upload cover
async function uploadCover(file) {
  if (!state.currentTask || !file) return;
  const formData = new FormData();
  formData.append('cover', file);

  try {
    const res = await fetch(`/api/tasks/${state.currentTask.id}/cover`, {
      method: 'POST',
      body: formData
    });

    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.detail || 'Помилка завантаження');
    }

    showToast('Обкладинку завантажено!');
    await openTaskDetails(state.currentTask.id);
    loadTasks();
  } catch (err) {
    showToast(err.message || 'Помилка завантаження обкладинки', 'error');
  }
}

// Delete cover
async function deleteCover() {
  if (!state.currentTask) return;
  try {
    const res = await fetch(`/api/tasks/${state.currentTask.id}/cover`, {
      method: 'DELETE'
    });
    if (!res.ok) throw new Error('Помилка видалення');
    showToast('Обкладинку видалено');
    await openTaskDetails(state.currentTask.id);
    loadTasks();
  } catch (err) {
    showToast('Помилка видалення обкладинки', 'error');
  }
}

// Delete task
async function deleteTask() {
  if (!state.currentTask) return;
  if (!confirm(`Ви дійсно бажаєте видалити задачу "${state.currentTask.title}"?`)) return;

  try {
    const res = await fetch(`/api/tasks/${state.currentTask.id}`, {
      method: 'DELETE'
    });
    if (!res.ok) throw new Error('Помилка видалення');
    closeModal('modal-task-details');
    showToast('Задачу видалено!');
    loadTasks();
  } catch (err) {
    showToast('Помилка видалення задачі', 'error');
  }
}

// Update task from modal (priority or status)
async function updateCurrentTaskFields() {
  if (!state.currentTask) return;
  const newStatus = document.getElementById('modal-task-status').value;
  const newPriority = document.getElementById('modal-task-priority').value;

  try {
    const updateBody = {
      title: state.currentTask.title,
      description: state.currentTask.description,
      status: newStatus,
      priority: newPriority,
      dueDate: state.currentTask.dueDate,
      location: state.currentTask.location,
      assigneeId: state.currentTask.assigneeId
    };

    const res = await fetch(`/api/tasks/${state.currentTask.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updateBody)
    });

    if (!res.ok) throw new Error();
    showToast('Зміни збережено!');
    loadTasks();
  } catch (err) {
    showToast('Помилка збереження', 'error');
  }
}

// Create Task
async function createNewTask(e) {
  e.preventDefault();
  const projectId = document.getElementById('task-project-select').value;
  const title = document.getElementById('create-task-title').value.trim();
  const desc = document.getElementById('create-task-desc').value.trim();
  const priority = document.getElementById('create-task-priority').value;
  const dueDate = document.getElementById('create-task-due').value;
  const location = document.getElementById('create-task-location').value.trim();

  if (!projectId || !title) {
    showToast('Заповніть назву та виберіть проєкт', 'error');
    return;
  }

  try {
    const body = {
      projectId: projectId,
      title: title,
      description: desc,
      priority: priority,
      dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      location: location || null
    };

    const res = await fetch('/api/tasks', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    if (!res.ok) throw new Error('Помилка створення задачі');
    showToast('Задачу створено!');
    closeModal('modal-create-task');
    document.getElementById('form-create-task').reset();
    loadTasks();
  } catch (err) {
    showToast('Не вдалося створити задачу', 'error');
  }
}

// Create Project
async function createNewProject(e) {
  e.preventDefault();
  const name = document.getElementById('create-project-name').value.trim();
  const desc = document.getElementById('create-project-desc').value.trim();

  if (!name) {
    showToast('Введіть назву проєкту', 'error');
    return;
  }

  try {
    const res = await fetch('/api/projects', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, description: desc })
    });

    if (!res.ok) throw new Error('Помилка створення проєкту');
    showToast('Проєкт успішно створено!');
    closeModal('modal-create-project');
    document.getElementById('form-create-project').reset();
    await loadProjects();
  } catch (err) {
    showToast('Не вдалося створити проєкт', 'error');
  }
}

// ==========================================
// EVENTS & GEOCODING
// ==========================================

async function loadEvents() {
  const grid = document.getElementById('events-grid');
  grid.innerHTML = '<div style="color:var(--text-muted); padding:2rem 0">Завантаження каталогу подій з оптимізацією B-Tree...</div>';

  try {
    const params = new URLSearchParams();
    if (state.eventFilter.search) params.append('search', state.eventFilter.search);
    if (state.eventFilter.dateFrom) params.append('dateFrom', state.eventFilter.dateFrom);
    if (state.eventFilter.dateTo) params.append('dateTo', state.eventFilter.dateTo);
    if (state.eventFilter.categoryId) params.append('categoryId', state.eventFilter.categoryId);
    params.append('sortBy', state.eventFilter.sortBy);
    params.append('order', state.eventFilter.order);
    params.append('page', state.eventFilter.page);
    params.append('pageSize', state.eventFilter.pageSize);

    const start = performance.now();
    const res = await fetch(`/events?${params.toString()}`);
    const timeMs = (performance.now() - start).toFixed(1);

    if (!res.ok) {
      if (res.status === 400) {
        showToast('Помилка валідації (перевірте межі)', 'error');
      }
      throw new Error();
    }

    const data = await res.json();
    state.events = data.items || [];

    document.getElementById('events-total-count').innerText = data.totalCount;
    document.getElementById('events-query-time').innerText = `${timeMs} ms`;

    renderEvents();
  } catch (err) {
    grid.innerHTML = '<div style="color:var(--danger); padding:2rem 0">Помилка завантаження каталогу подій.</div>';
  }
}

function renderEvents() {
  const grid = document.getElementById('events-grid');
  grid.innerHTML = '';

  if (!state.events.length) {
    grid.innerHTML = '<div style="color:var(--text-muted); grid-column:1/-1; padding:2rem 0; text-align:center;">Подій за заданими фільтрами не знайдено.</div>';
    return;
  }

  state.events.forEach(event => {
    const card = document.createElement('div');
    card.className = 'event-card';

    const d = new Date(event.date);
    const regCount = event.registrationCount || 0;
    const capacity = event.capacity || 1;
    const percent = Math.min(100, Math.round((regCount / capacity) * 100));

    let coordsHtml = '';
    if (event.latitude && event.longitude) {
      coordsHtml = `<div class="event-detail-item" title="Координати Geoapify">🌐 ${event.latitude.toFixed(4)}, ${event.longitude.toFixed(4)}</div>`;
    }

    card.innerHTML = `
      <div class="event-header">
        <div>
          <span class="badge badge-date">📅 ${d.toLocaleDateString()} ${d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
          <h3 class="event-title" style="margin-top:0.5rem">${escapeHtml(event.title)}</h3>
        </div>
        <span class="badge badge-project">${escapeHtml(event.categoryName || `Категорія #${event.categoryId}`)}</span>
      </div>

      <div class="event-details">
        ${event.description ? `<p style="font-size:0.85rem; color:var(--text-muted)">${escapeHtml(event.description)}</p>` : ''}
        <div class="event-detail-item">📍 ${escapeHtml(event.location || 'Онлайн')}</div>
        ${coordsHtml}
        <div>
          <div style="display:flex; justify-content:space-between; font-size:0.75rem; margin-top:0.25rem;">
            <span>Зареєстровано: <strong>${regCount}</strong> / ${capacity}</span>
            <span>${percent}%</span>
          </div>
          <div class="event-capacity-bar">
            <div class="event-capacity-fill" style="width: ${percent}%"></div>
          </div>
        </div>
      </div>

      <div style="display:flex; justify-content:space-between; align-items:center; margin-top:0.5rem;">
        <span style="font-size:0.75rem; color:var(--text-muted)">ID: ${event.id.substring(0, 8)}...</span>
        <button class="btn btn-primary btn-sm" onclick="openRegisterModal('${event.id}', '${escapeHtml(event.title)}')">
          ✍ Реєстрація
        </button>
      </div>
    `;

    grid.appendChild(card);
  });
}

function openRegisterModal(eventId, eventTitle) {
  state.currentEvent = { id: eventId, title: eventTitle };
  document.getElementById('modal-reg-event-title').innerText = eventTitle;
  document.getElementById('form-register-attendee').reset();
  openModal('modal-register-attendee');
}

async function registerAttendee(e) {
  e.preventDefault();
  if (!state.currentEvent) return;

  const name = document.getElementById('reg-name').value.trim();
  const email = document.getElementById('reg-email').value.trim();

  if (!name || !email) {
    showToast('Заповніть ім\'я та email', 'error');
    return;
  }

  try {
    const res = await fetch(`/events/${state.currentEvent.id}/registrations`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        attendeeName: name,
        attendeeEmail: email
      })
    });

    if (!res.ok) throw new Error('Помилка реєстрації');
    closeModal('modal-register-attendee');
    showToast(`Учасника ${name} успішно зареєстровано!`);
    loadEvents();
  } catch (err) {
    showToast('Не вдалося зареєструвати учасника', 'error');
  }
}

async function createNewEvent(e) {
  e.preventDefault();
  const title = document.getElementById('create-event-title').value.trim();
  const desc = document.getElementById('create-event-desc').value.trim();
  const location = document.getElementById('create-event-location').value.trim();
  const date = document.getElementById('create-event-date').value;
  const capacity = parseInt(document.getElementById('create-event-capacity').value, 10);
  const categoryId = parseInt(document.getElementById('create-event-category').value, 10);

  if (!title || !location || !date) {
    showToast('Заповніть обов\'язкові поля', 'error');
    return;
  }

  try {
    const body = {
      title,
      description: desc,
      location,
      date: new Date(date).toISOString(),
      capacity: capacity || 50,
      categoryId: categoryId || 1
    };

    const res = await fetch('/events', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    if (!res.ok) throw new Error();
    showToast('Подію створено (геокоординати визначено)!');
    closeModal('modal-create-event');
    document.getElementById('form-create-event').reset();
    loadEvents();
  } catch (err) {
    showToast('Помилка створення події', 'error');
  }
}

// ==========================================
// MODAL HELPERS
// ==========================================

function openModal(id) {
  const modal = document.getElementById(id);
  if (modal) modal.classList.add('active');
}

function closeModal(id) {
  const modal = document.getElementById(id);
  if (modal) modal.classList.remove('active');
}

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/[&<>"']/g, m => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[m]);
}

// ==========================================
// INITIALIZATION
// ==========================================

document.addEventListener('DOMContentLoaded', () => {
  initTabs();
  checkHealth();
  loadProjects();
  loadTasks();

  // Project selector change
  document.getElementById('project-select').addEventListener('change', (e) => {
    state.selectedProjectId = e.target.value || null;
    loadTasks();
  });

  // Task Filters
  document.getElementById('filter-task-status').addEventListener('change', (e) => {
    state.taskFilter.status = e.target.value;
    loadTasks();
  });
  document.getElementById('filter-task-priority').addEventListener('change', (e) => {
    state.taskFilter.priority = e.target.value;
    loadTasks();
  });
  document.getElementById('search-task-input').addEventListener('input', (e) => {
    state.taskFilter.search = e.target.value;
    loadTasks();
  });

  // Task Details Modal Changes
  document.getElementById('modal-task-status').addEventListener('change', updateCurrentTaskFields);
  document.getElementById('modal-task-priority').addEventListener('change', updateCurrentTaskFields);
  document.getElementById('modal-task-comment-btn').addEventListener('click', addComment);
  document.getElementById('modal-task-cover-input').addEventListener('change', (e) => {
    if (e.target.files.length) uploadCover(e.target.files[0]);
  });
  document.getElementById('modal-task-cover-delete').addEventListener('click', deleteCover);
  document.getElementById('modal-task-delete-btn').addEventListener('click', deleteTask);

  // Forms
  document.getElementById('form-create-task').addEventListener('submit', createNewTask);
  document.getElementById('form-create-project').addEventListener('submit', createNewProject);
  document.getElementById('form-create-event').addEventListener('submit', createNewEvent);
  document.getElementById('form-register-attendee').addEventListener('submit', registerAttendee);

  // Events Filters
  document.getElementById('event-search').addEventListener('input', (e) => {
    state.eventFilter.search = e.target.value;
    loadEvents();
  });
  document.getElementById('event-date-from').addEventListener('change', (e) => {
    state.eventFilter.dateFrom = e.target.value;
    loadEvents();
  });
  document.getElementById('event-date-to').addEventListener('change', (e) => {
    state.eventFilter.dateTo = e.target.value;
    loadEvents();
  });
  document.getElementById('event-category').addEventListener('change', (e) => {
    state.eventFilter.categoryId = e.target.value;
    loadEvents();
  });
  document.getElementById('event-sort-by').addEventListener('change', (e) => {
    state.eventFilter.sortBy = e.target.value;
    loadEvents();
  });
  document.getElementById('event-order').addEventListener('change', (e) => {
    state.eventFilter.order = e.target.value;
    loadEvents();
  });

  // Modals backdrop close
  document.querySelectorAll('.modal-backdrop').forEach(modal => {
    modal.addEventListener('click', (e) => {
      if (e.target === modal) modal.classList.remove('active');
    });
  });
});
