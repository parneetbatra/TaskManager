import { Component } from '@angular/core';
import { TaskBoard } from './task-board/task-board';

@Component({
  selector: 'app-root',
  imports: [TaskBoard],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
