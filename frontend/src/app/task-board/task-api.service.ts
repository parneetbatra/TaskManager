import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TaskItemDto {
  id: string;
  title: string;
  description: string | null;
  status: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaskDto {
  title: string;
  description?: string | null;
}

export interface UpdateTaskDto extends CreateTaskDto {
  status: number;
}

export interface PaginatedResponse<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: T[];
}

@Injectable({ providedIn: 'root' })
export class TaskApiService {
  private readonly http = inject(HttpClient);
  private readonly tasksApiUrl = buildTasksApiUrl(environment.apiBaseUrl);

  getTasks(statusFilter: string, page: number, pageSize: number): Observable<PaginatedResponse<TaskItemDto>> {
    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));

    if (statusFilter !== 'all') {
      params = params.set('status', statusFilter);
    }

    return this.http.get<PaginatedResponse<TaskItemDto>>(this.tasksApiUrl, { params });
  }

  createTask(payload: CreateTaskDto): Observable<TaskItemDto> {
    return this.http.post<TaskItemDto>(this.tasksApiUrl, payload);
  }

  updateTask(taskId: string, payload: UpdateTaskDto): Observable<TaskItemDto> {
    return this.http.put<TaskItemDto>(`${this.tasksApiUrl}/${taskId}`, payload);
  }

  deleteTask(taskId: string): Observable<void> {
    return this.http.delete<void>(`${this.tasksApiUrl}/${taskId}`);
  }
}

function buildTasksApiUrl(apiBaseUrl: string): string {
  return `${apiBaseUrl.replace(/\/$/, '')}/tasks`;
}
