import { CommonModule, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { CreateTaskDto, TaskApiService, TaskItemDto, UpdateTaskDto } from './task-api.service';

type TaskFilter = 'all' | 'active' | 'completed' | 'onhold' | 'cancelled' | 'blocked';

interface TaskFilterOption {
  value: TaskFilter;
  label: string;
}

interface TaskStatusOption {
  value: number;
  label: string;
  tone: string;
}

const TASK_FILTERS: TaskFilterOption[] = [
  { value: 'all', label: 'All' },
  { value: 'active', label: 'Active' },
  { value: 'completed', label: 'Completed' },
  { value: 'onhold', label: 'On Hold' },
  { value: 'cancelled', label: 'Cancelled' },
  { value: 'blocked', label: 'Blocked' }
];

const TASK_STATUSES: TaskStatusOption[] = [
  { value: 0, label: 'Active', tone: 'status-active' },
  { value: 1, label: 'Completed', tone: 'status-completed' },
  { value: 2, label: 'On Hold', tone: 'status-onhold' },
  { value: 3, label: 'Cancelled', tone: 'status-cancelled' },
  { value: 4, label: 'Blocked', tone: 'status-blocked' }
];

@Component({
  selector: 'app-task-board',
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './task-board.html',
  styleUrl: './task-board.css',
})
export class TaskBoard {
  private readonly api = inject(TaskApiService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly title = 'Tasksboard';
  protected readonly subtitle = 'Task orchestration for the work that cannot slip.';
  protected readonly filters = TASK_FILTERS;
  protected readonly statusOptions = TASK_STATUSES;
  protected readonly tasks = signal<TaskItemDto[]>([]);
  protected readonly activeFilter = signal<TaskFilter>('all');
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly deletingTaskId = signal<string | null>(null);
  protected readonly selectedTaskId = signal<string | null>(null);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly currentPage = signal(1);
  protected readonly pageSize = signal(5);
  protected readonly totalPages = signal(1);
  protected readonly totalTasks = signal(0);
  protected readonly pageSizeOptions = [5, 10, 20];

  protected readonly createForm = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]]
  });

  protected readonly editForm = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    status: [0, [Validators.required]]
  });

  protected readonly selectedTask = computed(
    () => this.tasks().find((task) => task.id === this.selectedTaskId()) ?? null
  );

  protected readonly taskCount = computed(() => this.tasks().length);
  protected readonly completedCount = computed(() => this.tasks().filter((task) => task.status === 1).length);
  protected readonly activeCount = computed(() => this.tasks().filter((task) => task.status === 0).length);
  protected readonly completionRate = computed(() => {
    const total = this.taskCount();
    return total === 0 ? 0 : Math.round((this.completedCount() / total) * 100);
  });

  constructor() {
    this.loadTasks();
  }

  protected setFilter(filter: TaskFilter): void {
    if (this.activeFilter() === filter) {
      return;
    }

    this.activeFilter.set(filter);
    this.currentPage.set(1);
    this.selectedTaskId.set(null);
    this.loadTasks();
  }

  protected reloadTasks(): void {
    this.loadTasks();
  }

  protected goToPreviousPage(): void {
    if (this.currentPage() <= 1) {
      return;
    }

    this.currentPage.update((current) => current - 1);
    this.loadTasks();
  }

  protected goToNextPage(): void {
    if (this.currentPage() >= this.totalPages()) {
      return;
    }

    this.currentPage.update((current) => current + 1);
    this.loadTasks();
  }

  protected setPageSize(value: string): void {
    const size = Number(value);
    if (size <= 0 || size === this.pageSize()) {
      return;
    }

    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadTasks();
  }

  protected createTask(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const payload: CreateTaskDto = {
      title: this.createForm.controls.title.getRawValue().trim(),
      description: normalizeOptionalText(this.createForm.controls.description.getRawValue())
    };

    this.saving.set(true);
    this.errorMessage.set('');

    this.api.createTask(payload)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (createdTask) => {
          this.createForm.reset({ title: '', description: '' });
          this.successMessage.set(`Created "${createdTask.title}".`);
          this.cancelEditing();

          if (this.activeFilter() !== 'all' && this.activeFilter() !== 'active') {
            this.activeFilter.set('all');
          }

          this.currentPage.set(1);
          this.loadTasks();
        },
        error: (error: unknown) => this.errorMessage.set(extractErrorMessage(error))
      });
  }

  protected startEditing(task: TaskItemDto): void {
    this.selectedTaskId.set(task.id);
    this.editForm.reset({
      title: task.title,
      description: task.description ?? '',
      status: task.status
    });
    this.successMessage.set('');
    this.errorMessage.set('');
  }

  protected cancelEditing(): void {
    this.selectedTaskId.set(null);
    this.editForm.reset({ title: '', description: '', status: 0 });
  }

  protected saveTask(): void {
    const task = this.selectedTask();
    if (!task) {
      return;
    }

    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const payload: UpdateTaskDto = {
      title: this.editForm.controls.title.getRawValue().trim(),
      description: normalizeOptionalText(this.editForm.controls.description.getRawValue()),
      status: Number(this.editForm.controls.status.getRawValue())
    };

    this.saving.set(true);
    this.errorMessage.set('');

    this.api.updateTask(task.id, payload)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updatedTask) => {
          this.successMessage.set(`Updated "${updatedTask.title}".`);
          this.cancelEditing();
          this.loadTasks();
        },
        error: (error: unknown) => this.errorMessage.set(extractErrorMessage(error))
      });
  }

  protected deleteTask(task: TaskItemDto): void {
    const shouldDelete = window.confirm(`Delete task '${task.title}'? This cannot be undone.`);
    if (!shouldDelete) {
      return;
    }

    this.deletingTaskId.set(task.id);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.api.deleteTask(task.id)
      .pipe(finalize(() => this.deletingTaskId.set(null)))
      .subscribe({
        next: () => {
          if (this.selectedTaskId() === task.id) {
            this.cancelEditing();
          }

          this.successMessage.set(`Deleted "${task.title}".`);
          this.loadTasks();
        },
        error: (error: unknown) => this.errorMessage.set(extractErrorMessage(error))
      });
  }

  protected trackTask(_: number, task: TaskItemDto): string {
    return task.id;
  }

  protected statusLabel(status: number): string {
    return TASK_STATUSES.find((option) => option.value === status)?.label ?? 'Unknown';
  }

  protected statusTone(status: number): string {
    return TASK_STATUSES.find((option) => option.value === status)?.tone ?? 'status-unknown';
  }

  protected emptyStateTitle(): string {
    const activeFilter = this.activeFilter();
    if (activeFilter === 'all') {
      return 'No tasks yet';
    }

    const label = this.filters.find((filter) => filter.value === activeFilter)?.label.toLowerCase() ?? activeFilter;
    return `No ${label} tasks right now`;
  }

  private loadTasks(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.api.getTasks(this.activeFilter(), this.currentPage(), this.pageSize())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          const totalPages = Math.max(1, response.totalPages);
          this.totalPages.set(totalPages);
          this.totalTasks.set(response.totalCount);

          if (this.currentPage() > totalPages) {
            this.currentPage.set(totalPages);
            this.loadTasks();
            return;
          }

          const orderedTasks = [...response.items].sort((left, right) =>
            Date.parse(right.updatedAt) - Date.parse(left.updatedAt)
          );

          this.tasks.set(orderedTasks);

          const selectedId = this.selectedTaskId();
          if (!selectedId) {
            return;
          }

          const selectedTask = orderedTasks.find((task) => task.id === selectedId);
          if (!selectedTask) {
            this.cancelEditing();
            return;
          }

          this.editForm.reset({
            title: selectedTask.title,
            description: selectedTask.description ?? '',
            status: selectedTask.status
          });
        },
        error: (error: unknown) => this.errorMessage.set(extractErrorMessage(error))
      });
  }
}

function normalizeOptionalText(value: string): string | null {
  const trimmedValue = value.trim();
  return trimmedValue.length > 0 ? trimmedValue : null;
}

function extractErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (typeof error.error === 'object' && error.error !== null) {
      const problem = error.error as { detail?: string; title?: string };
      return problem.detail ?? problem.title ?? 'The request could not be completed.';
    }

    if (typeof error.error === 'string' && error.error.trim().length > 0) {
      return error.error;
    }
  }

  return 'The request could not be completed.';
}
