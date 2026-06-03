import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const httpController = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    httpController.expectOne('http://localhost:5203/api/v1/tasks').flush([]);
    httpController.verify();
    expect(app).toBeTruthy();
  });

  it('should render the dashboard heading', async () => {
    const fixture = TestBed.createComponent(App);
    const httpController = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    httpController.expectOne('http://localhost:5203/api/v1/tasks').flush([]);
    httpController.verify();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Pulseboard');
  });
});
