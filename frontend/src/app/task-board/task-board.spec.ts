import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { TaskBoard } from './task-board';

describe('TaskBoard', () => {
  let component: TaskBoard;
  let fixture: ComponentFixture<TaskBoard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskBoard],
      providers: [provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskBoard);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    const httpController = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    httpController.expectOne('http://localhost:5203/api/v1/tasks').flush([]);
    httpController.verify();
    expect(component).toBeTruthy();
  });

  it('should render the dashboard heading', async () => {
    const httpController = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    httpController.expectOne('http://localhost:5203/api/v1/tasks').flush([]);
    httpController.verify();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Pulseboard');
  });
});
