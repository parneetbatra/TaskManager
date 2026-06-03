# TaskManager

A simple task management application built with a .NET backend and an Angular frontend. The app supports creating, viewing, editing, deleting, and filtering tasks, with clear task status tracking and timestamp metadata.

## Functional requirements

The user must be able to:

- View a list of tasks
- Create a new task
- Edit an existing task
- Delete a task
- Mark a task as complete or incomplete
- Filter tasks by status: all, active, completed, on hold, cancelled, blocked

Each task includes at minimum:

- title
- description
- status
- created date
- updated date

## Requirements

- .NET SDK 9 (required for the backend)
- Visual Studio Code with the C# extension and .NET SDK installed
- Node.js 18+ (recommended)
- npm 11.x (project is configured with `packageManager: "npm@11.12.0"`)
- A terminal / command prompt
- SQLite (used by the backend; database file is stored in `backend/TaskManager/TaskManager.db`)

## Running the project

### Backend (VS Code)

Run the .NET backend from Visual Studio Code:

- Open the repository in VS Code
- Open the integrated terminal
- Run the backend project from `backend\TaskManager`

```bash
cd backend\TaskManager
dotnet run
```

This launches the ASP.NET Core API using the configured launch settings. If you use the `https` launch profile, the backend will run at `http://localhost:5203`, and the API is exposed at `/api/v1/tasks`.

### Frontend (command line)

Run the Angular frontend in a terminal:

```bash
cd frontend
npm install
npm start
```

This starts the Angular development server and runs the frontend locally.

Then open the browser at the Angular dev server address (usually `http://localhost:4200`). The frontend calls the backend API for task data.

## Build

### Frontend production build

From the `frontend` folder:

```bash
npm run build
```

## Notes

- The backend uses SQLite and automatically applies database migrations at startup.
- The frontend is configured as a normal tracked directory in the repo.
- If your browser does not automatically open, navigate to `http://localhost:4200` after running `npm start`.

## Future improvements

- Add authentication and user accounts
- Improve API error handling and validation feedback
- Add task search, sorting, and more advanced filtering
- Add API rate limiting for production readiness
- Add unit/integration tests for the Angular frontend
- Add Docker support for backend and frontend
- Add production-ready environment configuration and deployment scripts

## Project structure

- `backend/TaskManager` — ASP.NET Core API project
- `backend/TaskManager.Application` — application logic and DTOs
- `backend/TaskManager.Infrastructure` — EF Core repository and DB context
- `backend/TaskManager.Domain` — domain entities, enums, and exceptions
- `backend/TaskManager.Tests` — unit tests
- `frontend` — Angular frontend application
