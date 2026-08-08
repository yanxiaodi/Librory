# Implementation Plan

## Step 1: Project Skeleton

- Create a React + Vite + TypeScript web app shell
- Create the ASP.NET Core API
- Add shared models and DTOs
- Add localization resource files
- Add Tailwind, Radix, and a shadcn/ui-style component layer

## Step 2: Domain Model

- Implement Work / Edition / Copy entities
- Add family and member entities
- Add wishlist and scan session entities
- Add preferred language support

## Step 3: API and Persistence Foundation

- Add PostgreSQL persistence through EF Core
- Add a design-time DbContext and generated migrations
- Apply EF Core migrations automatically on API startup in Development
- Add Scalar API docs
- Add local development auth support for debugging
- Add PostgreSQL-backed test infrastructure for API integration coverage

## Step 4: Core Flows

- Temporary scan session workflow as the next implementation slice
- Candidate correction within a scan session
- Catalog resolution and candidate enrichment for scan results
- Scan candidate promotion into new canonical work and edition records
- Scan candidate discard from temporary scan sessions
- Manual book intake from a resolved edition into the current family/member context, with purchase metadata kept optional
- Family library browse and search
- External metadata lookup and normalization for ISBN/title search
- Promotion of confirmed external metadata into canonical catalog records
- Duplicate detection shared domain slice and scan-review warning output

## Step 5: AI Integration

- Add Microsoft Agent Framework workflows inside the API project
- Add metadata enrichment pipeline
- Add catalog resolution pipeline for scan candidates
- Add recommendation reasoning pipeline
- Keep AI workflow code modular inside the API instead of splitting it into a separate service

## Step 6: Azure Readiness

- Configure Azure deployment target
- Configure storage for images and artifacts
- Configure relational database hosting
- Add environment-based secrets and settings
