# RehabAI Frontend

React frontend foundation for the RehabAI stroke rehabilitation web platform.

## Stack

- React
- TypeScript
- Vite
- Tailwind CSS
- React Router
- Axios
- TanStack Query
- React Hook Form
- Zod
- Lucide React
- Recharts

## Local Setup

```powershell
npm install
npm run dev
```

The API base URL is read from `VITE_API_BASE_URL`.

Default local backend:

```text
https://localhost:7007
```

Create a local `.env` from `.env.example` if the backend URL changes.

## Project Structure

```text
src/
  api/
  components/
  layouts/
  pages/
  routes/
  types/
  utils/
```

## Current Scope

This frontend contains the initial app shell, routing, API client, JWT storage for MVP, route protection, shared components, and starter pages. It does not implement every business workflow yet.
